// ============================================================================
//  AtlasManager.cs
// ============================================================================
//  Renderer-neutral texture atlas. Pages are CPU-shadowed RGBA8 buffers backed
//  by IGraphicsTexture resources owned by the active renderer.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Assets.Loaders;
using Void.Engine.Assets.Loaders.Fonts;
using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics.Atlas;

/// <summary>
/// Manages renderer-neutral texture atlas pages, cached regions, eviction, and
/// incremental defragmentation.
/// </summary>
/// <remarks>
/// <para>
/// Atlas pages are created lazily from the active renderer and retain a CPU-side
/// RGBA8 copy so their GPU resources can be recreated if the renderer device changes.
/// Page count, page size, packer type, and defragmentation settings come from
/// <see cref="GameSettings"/> when the singleton is first initialized. Results returned
/// by the configured <see cref="IAtlasPacker"/> are validated before atlas pixels are written.
/// </para>
/// <para>
/// <see cref="TryPack(Texture, Rect2, out Rect2, out int)"/> caches regions by
/// texture identifier and source rectangle. If a page is being defragmented, that
/// page is temporarily unavailable and callers can fall back to the original source.
/// </para>
/// <code>
/// var atlas = AtlasManager.Instance;
///
/// if (atlas.TryPack(texture, texture.Bounds, out Rect2 packedRect, out int pageId))
/// {
///     Texture pageTexture = atlas.GetPageTexture(pageId);
///     // Use pageTexture with packedRect for the atlas-backed draw.
/// }
///
/// AtlasMetrics metrics = atlas.GetMetrics();
/// </code>
/// </remarks>
public sealed class AtlasManager
{
    private struct AtlasSlot
    {
        public int PageId;
        public Rect2 PackedRect;
        public LinkedListNode<(uint, Rect2)> LruNode;
        public ulong LastUsedBatch;
    }

    private readonly struct PendingDefragMove
    {
        public int PageId { get; }
        public Rect2 OldRect { get; }
        public Rect2 NewRect { get; }

        public PendingDefragMove(int pageId, Rect2 oldRect, Rect2 newRect)
        {
            PageId = pageId;
            OldRect = oldRect;
            NewRect = newRect;
        }
    }

    private sealed class AtlasPage
    {
        public Texture Texture;
        public IGraphicsTexture GraphicsTexture;
        public IGraphicsDevice GraphicsDevice;
        public byte[] Pixels;
        public IAtlasPacker Packer;
        public bool IsActive;

        public AtlasPage(IAtlasPacker packer)
        {
            Packer = packer;
        }

        public void DisposeGraphics()
        {
            Texture?.Dispose();
            Texture = null;

            GraphicsTexture?.Dispose();
            GraphicsTexture = null;
            GraphicsDevice = null;
        }
    }

    private static readonly Lazy<AtlasManager> _instance = new(() => new AtlasManager());
    private readonly Dictionary<(uint TextureId, Rect2 SrcRect), AtlasSlot> _packedMap = [];
    private readonly List<AtlasPage> _pages = [];
    private readonly LinkedList<(uint TextureId, Rect2 SrcRect)> _lruList = [];
    private readonly Queue<PendingDefragMove> _pendingDefragMoves = [];
    private readonly HashSet<int> _pagesWithPendingMoves = [];

    private int _pageSize;
    private int _pageCount;
    private int _evictionCount;
    private ulong _usageBatch;
    private bool _isDefragging;

    /// <summary>
    /// Gets the shared atlas manager.
    /// </summary>
    public static AtlasManager Instance => _instance.Value;

    /// <summary>
    /// Gets whether one or more atlas pages are waiting for incremental defragmentation uploads.
    /// </summary>
    public bool IsDefragging => _isDefragging;

    /// <summary>
    /// Gets the number of queued defragmentation moves that have not yet been processed.
    /// </summary>
    public int PendingDefragMoves => _pendingDefragMoves.Count;

    private AtlasManager()
    {
        Initialize();
    }

    private void Initialize()
    {
        var settings = GameSettings.Instance;
        _pageSize = settings.AtlasPageSize;
        _pageCount = settings.AtlasPageCount;

        Logger.Instance.InfoWithCategory("Atlas", "Initializing renderer atlas: {0} pages of {1}x{1}",
            _pageCount, _pageSize);

        Type packerType = settings.AtlasPacker ?? typeof(SkylinePacker);
        for (int i = 0; i < _pageCount; i++)
            _pages.Add(new AtlasPage(CreatePacker(packerType, _pageSize)));
    }

    private static IAtlasPacker CreatePacker(Type packerType, int pageSize)
    {
        try
        {
            object instance = Activator.CreateInstance(packerType, [pageSize, pageSize]);
            if (instance is IAtlasPacker packer)
                return packer;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to create atlas packer '{packerType.FullName ?? packerType.Name}' with constructor (int width, int height).",
                ex);
        }

        throw new InvalidOperationException(
            $"Atlas packer '{packerType.FullName ?? packerType.Name}' did not produce an IAtlasPacker instance.");
    }

    internal void BeginBatchUsage()
    {
        unchecked
        {
            _usageBatch++;
        }
    }

    /// <summary>
    /// Attempts to pack a texture region into an atlas page.
    /// </summary>
    /// <param name="texture">The source texture.</param>
    /// <param name="srcRect">The source region to pack.</param>
    /// <param name="packedRect">
    /// When successful, receives the region of the selected atlas page containing the copied pixels.
    /// </param>
    /// <param name="pageId">
    /// When successful, receives the atlas page index; otherwise, receives <c>-1</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the region is already cached or was packed successfully;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Packing can fail when the source region cannot be copied, no renderer device is
    /// available, the region exceeds the configured page size, the atlas cannot make
    /// room, or the selected page has entered incremental defragmentation.
    /// </remarks>
    public bool TryPack(Texture texture, Rect2 srcRect, out Rect2 packedRect, out int pageId)
    {
        if (texture == null)
        {
            packedRect = default;
            pageId = -1;
            return false;
        }

        if (TryGetCached(texture.Id, srcRect, out packedRect, out pageId))
            return true;

        if (!texture.TryCopyPixelRegion(srcRect, out byte[] pixels, out int width, out int height))
        {
            packedRect = default;
            pageId = -1;
            return false;
        }

        return TryPackNew(texture.Id, srcRect, pixels, width, height, out packedRect, out pageId);
    }

    internal bool TryPack(Font font, Rect2 srcRect, out Rect2 packedRect, out int pageId)
    {
        if (font == null)
        {
            packedRect = default;
            pageId = -1;
            return false;
        }

        if (TryGetCached(font.Id, srcRect, out packedRect, out pageId))
            return true;

        if (!font.TryCopyPixelRegion(srcRect, out byte[] pixels, out int width, out int height))
        {
            packedRect = default;
            pageId = -1;
            return false;
        }

        return TryPackNew(font.Id, srcRect, pixels, width, height, out packedRect, out pageId);
    }

    private bool TryGetCached(uint textureId, Rect2 srcRect, out Rect2 packedRect, out int pageId)
    {
        var key = (textureId, srcRect);
        if (_packedMap.TryGetValue(key, out var slot))
        {
            if (_pagesWithPendingMoves.Contains(slot.PageId))
            {
                packedRect = default;
                pageId = -1;
                return false;
            }

            packedRect = slot.PackedRect;
            pageId = slot.PageId;

            if (slot.LastUsedBatch != _usageBatch)
            {
                _lruList.Remove(slot.LruNode);
                _lruList.AddFirst(slot.LruNode);
                slot.LastUsedBatch = _usageBatch;
                _packedMap[key] = slot;
            }

            return true;
        }

        packedRect = default;
        pageId = -1;
        return false;
    }

    private bool TryPackNew(
        uint textureId,
        Rect2 srcRect,
        byte[] pixels,
        int width,
        int height,
        out Rect2 packedRect,
        out int pageId)
    {
        packedRect = default;
        pageId = -1;

        if (width <= 0 || height <= 0 || width > _pageSize || height > _pageSize)
        {
            Logger.Instance.WarningWithCategory("Atlas",
                "Texture {0}x{1} exceeds page size {2}x{2}", width, height, _pageSize);
            return false;
        }

        var key = (textureId, srcRect);

        for (int i = 0; i < _pages.Count; i++)
        {
            if (_pagesWithPendingMoves.Contains(i))
                continue;
            if (!EnsurePageResources(i))
                continue;

            if (TryReservePackedRect(i, width, height, out var rect, out bool contractViolation))
                return PackIntoPage(pixels, width, height, key, i, rect, out packedRect, out pageId);

            if (contractViolation)
                return false;
        }

        int bestPageIndex = -1;
        float highestFragmentation = 0f;

        for (int i = 0; i < _pages.Count; i++)
        {
            if (_pagesWithPendingMoves.Contains(i))
                continue;

            var page = _pages[i];
            if (page.Packer.Fragmentation > highestFragmentation)
            {
                highestFragmentation = page.Packer.Fragmentation;
                bestPageIndex = i;
            }
        }

        if (bestPageIndex >= 0 && highestFragmentation > GameSettings.Instance.AtlasDefragThreshold)
        {
            var page = _pages[bestPageIndex];

            // Defrag mutates the packer's layout, so renderer resources must be
            // available before we allow the packer to commit a new arrangement.
            if (EnsurePageResources(bestPageIndex))
            {
                var pageTextures = _packedMap
                    .Where(kvp => kvp.Value.PageId == bestPageIndex)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PackedRect);

                List<(Rect2 OldRect, Rect2 NewRect)> moves;
                try
                {
                    moves = page.Packer.Defrag();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Atlas packer '{page.Packer.GetType().FullName ?? page.Packer.GetType().Name}' threw while defragmenting page {bestPageIndex}.",
                        ex);
                }

                if (moves == null)
                {
                    throw new InvalidOperationException(
                        $"Atlas packer '{page.Packer.GetType().FullName ?? page.Packer.GetType().Name}' violated IAtlasPacker.Defrag on page {bestPageIndex}: Defrag returned null.");
                }

                if (moves.Count > 0)
                {
                    if (!TryValidateDefragMoves(
                            pageTextures,
                            moves,
                            out var relocationMap,
                            out string validationError))
                    {
                        throw new InvalidOperationException(
                            $"Atlas packer '{page.Packer.GetType().FullName ?? page.Packer.GetType().Name}' violated IAtlasPacker.Defrag on page {bestPageIndex}: {validationError}");
                    }

                    if (!TryPrepareAndQueueDefrag(
                            bestPageIndex,
                            moves,
                            out string preparationError))
                    {
                        throw new InvalidOperationException(
                            $"Atlas defragmentation for page {bestPageIndex} could not be prepared safely after the packer committed its new layout: {preparationError}");
                    }

                    foreach (var kvp in pageTextures)
                    {
                        if (!relocationMap.TryGetValue(kvp.Value, out Rect2 newRect))
                            continue;

                        var existingSlot = _packedMap[kvp.Key];
                        existingSlot.PackedRect = newRect;
                        _packedMap[kvp.Key] = existingSlot;
                    }

                    // The page is deliberately unavailable until its final layout has
                    // been uploaded over the configured number of frames. The caller
                    // falls back to the source texture for this draw.
                    return false;
                }
            }
        }

        if (EvictAndRepack(key, pixels, width, height, out packedRect, out pageId))
            return true;

        Logger.Instance.WarningWithCategory("Atlas",
            "Failed to pack texture {0}x{1} - atlas full or too fragmented", width, height);
        return false;
    }


    private bool TryValidateDefragMoves(
        IReadOnlyDictionary<(uint TextureId, Rect2 SrcRect), Rect2> pageTextures,
        IReadOnlyList<(Rect2 OldRect, Rect2 NewRect)> moves,
        out Dictionary<Rect2, Rect2> relocationMap,
        out string reason)
    {
        relocationMap = new Dictionary<Rect2, Rect2>(moves.Count);
        var liveRects = pageTextures.Values.ToList();
        var liveRectSet = new HashSet<Rect2>(liveRects);

        if (liveRectSet.Count != liveRects.Count)
        {
            reason = "the atlas manager contains duplicate live rectangles on the page";
            return false;
        }

        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];

            if (!liveRectSet.Contains(move.OldRect))
            {
                reason = $"move {i} references an OldRect that is not a live allocation";
                return false;
            }

            if (!relocationMap.TryAdd(move.OldRect, move.NewRect))
            {
                reason = $"move {i} repeats an OldRect that already has a relocation";
                return false;
            }

            if (move.OldRect == move.NewRect)
            {
                reason = $"move {i} reports a rectangle that did not actually move";
                return false;
            }

            if (move.OldRect.Width != move.NewRect.Width ||
                move.OldRect.Height != move.NewRect.Height)
            {
                reason = $"move {i} changes the allocation dimensions";
                return false;
            }

            if (!TryValidateDefragDestination(move.NewRect, out string destinationError))
            {
                reason = $"move {i} has an invalid NewRect: {destinationError}";
                return false;
            }
        }

        var finalRects = new List<Rect2>(liveRects.Count);
        foreach (var oldRect in liveRects)
        {
            finalRects.Add(relocationMap.TryGetValue(oldRect, out Rect2 newRect)
                ? newRect
                : oldRect);
        }

        for (int i = 0; i < finalRects.Count; i++)
        {
            for (int j = i + 1; j < finalRects.Count; j++)
            {
                if (finalRects[i].Intersects(finalRects[j]))
                {
                    reason = $"the proposed final layout overlaps live allocations {i} and {j}";
                    return false;
                }
            }
        }

        reason = string.Empty;
        return true;
    }

    private bool TryValidateDefragDestination(Rect2 rect, out string reason)
    {
        if (!float.IsFinite(rect.X) ||
            !float.IsFinite(rect.Y) ||
            !float.IsFinite(rect.Width) ||
            !float.IsFinite(rect.Height))
        {
            reason = "the rectangle contains a non-finite component";
            return false;
        }

        if (rect.Width <= 0f || rect.Height <= 0f)
        {
            reason = "the rectangle has a non-positive size";
            return false;
        }

        if (rect.X != MathF.Truncate(rect.X) ||
            rect.Y != MathF.Truncate(rect.Y) ||
            rect.Width != MathF.Truncate(rect.Width) ||
            rect.Height != MathF.Truncate(rect.Height))
        {
            reason = "the rectangle is not aligned to whole pixels";
            return false;
        }

        if (rect.X < 0f || rect.Y < 0f || rect.Right > _pageSize || rect.Bottom > _pageSize)
        {
            reason = $"the rectangle lies outside the {_pageSize}x{_pageSize} atlas page";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private bool TryReservePackedRect(
        int pageIndex,
        int width,
        int height,
        out Rect2 rect,
        out bool contractViolation)
    {
        contractViolation = false;
        var page = _pages[pageIndex];
        bool reserved;

        try
        {
            reserved = page.Packer.TryPack(width, height, out rect);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Atlas packer '{page.Packer.GetType().FullName ?? page.Packer.GetType().Name}' threw while packing {width}x{height} on page {pageIndex}.",
                ex);
        }

        if (!reserved)
        {
            rect = default;
            return false;
        }

        if (TryValidatePackedRect(pageIndex, rect, width, height, out string reason))
            return true;

        contractViolation = true;
        string packerName = page.Packer.GetType().FullName ?? page.Packer.GetType().Name;

        Logger.Instance.ErrorWithCategory("Atlas",
            "Atlas packer '{0}' violated IAtlasPacker.TryPack on page {1}: {2}. Request {3}x{4}; returned X={5}, Y={6}, Width={7}, Height={8}.",
            packerName,
            pageIndex,
            reason,
            width,
            height,
            rect.X,
            rect.Y,
            rect.Width,
            rect.Height);

        try
        {
            page.Packer.Free(rect);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Atlas packer '{packerName}' returned an invalid rectangle and then threw while releasing it.",
                ex);
        }

        rect = default;
        return false;
    }

    private bool TryValidatePackedRect(
        int pageIndex,
        Rect2 rect,
        int requestedWidth,
        int requestedHeight,
        out string reason)
    {
        if (!float.IsFinite(rect.X) ||
            !float.IsFinite(rect.Y) ||
            !float.IsFinite(rect.Width) ||
            !float.IsFinite(rect.Height))
        {
            reason = "the returned rectangle contains a non-finite component";
            return false;
        }

        if (rect.Width != requestedWidth || rect.Height != requestedHeight)
        {
            reason = "the returned dimensions do not match the requested dimensions";
            return false;
        }

        if (rect.X != MathF.Truncate(rect.X) || rect.Y != MathF.Truncate(rect.Y))
        {
            reason = "the returned position is not aligned to whole pixels";
            return false;
        }

        if (rect.X < 0f || rect.Y < 0f || rect.Right > _pageSize || rect.Bottom > _pageSize)
        {
            reason = $"the returned rectangle lies outside the {_pageSize}x{_pageSize} atlas page";
            return false;
        }

        foreach (var slot in _packedMap.Values)
        {
            if (slot.PageId == pageIndex && rect.Intersects(slot.PackedRect))
            {
                reason = "the returned rectangle overlaps an existing live atlas allocation";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private bool PackIntoPage(
        byte[] pixels,
        int width,
        int height,
        (uint TextureId, Rect2 SrcRect) key,
        int pageIndex,
        Rect2 rect,
        out Rect2 packedRect,
        out int pageId)
    {
        var page = _pages[pageIndex];
        if (!EnsurePageResources(pageIndex))
        {
            page.Packer.Free(rect);
            packedRect = default;
            pageId = -1;
            return false;
        }

        int dstX = (int)rect.X;
        int dstY = (int)rect.Y;
        RgbaPixelBuffer.Blit(pixels, width, height, page.Pixels, _pageSize, _pageSize, dstX, dstY);
        page.GraphicsDevice.UpdateTexture(
            page.GraphicsTexture,
            dstX,
            dstY,
            width,
            height,
            TextureFormat.RGBA8,
            pixels);

        var lruNode = _lruList.AddFirst(key);
        _packedMap[key] = new AtlasSlot
        {
            PageId = pageIndex,
            PackedRect = rect,
            LruNode = lruNode,
            LastUsedBatch = _usageBatch
        };

        Logger.Instance.DebugWithCategory("Atlas", "Packed {0}x{1} into renderer page {2} (total: {3})",
            width, height, pageIndex, _packedMap.Count);

        packedRect = rect;
        pageId = pageIndex;
        return true;
    }

    /// <summary>
    /// Processes a limited number of queued defragmentation uploads.
    /// </summary>
    /// <param name="maxMovesPerFrame">The maximum number of queued moves to process during this call.</param>
    /// <returns>
    /// <see langword="true"/> if defragmentation moves remain queued after this call;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxMovesPerFrame"/> is less than one.
    /// </exception>
    /// <remarks>
    /// Pages with pending moves remain unavailable through <see cref="GetPageTexture"/>
    /// and cached atlas lookups until the final queued move has been processed.
    /// A queued move is removed only after its required atlas regions have been
    /// uploaded successfully.
    /// </remarks>
    public bool ProcessPendingDefragMoves(int maxMovesPerFrame = 5)
    {
        if (maxMovesPerFrame <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxMovesPerFrame), "The move limit must be at least one.");

        if (_pendingDefragMoves.Count == 0)
        {
            _isDefragging = false;
            _pagesWithPendingMoves.Clear();
            return false;
        }

        int movesProcessed = 0;
        while (_pendingDefragMoves.Count > 0 && movesProcessed < maxMovesPerFrame)
        {
            var move = _pendingDefragMoves.Peek();

            // Do not consume the move until the page can actually be updated.
            // If renderer resources are temporarily unavailable, leave the work
            // queued so it can be retried on a later call.
            if (!EnsurePageResources(move.PageId))
                break;

            var page = _pages[move.PageId];
            if (!TryUploadRegion(page, move.OldRect))
            {
                throw new InvalidOperationException(
                    $"Atlas defragmentation could not upload the old region {move.OldRect} on page {move.PageId}.");
            }

            if (!TryUploadRegion(page, move.NewRect))
            {
                throw new InvalidOperationException(
                    $"Atlas defragmentation could not upload the new region {move.NewRect} on page {move.PageId}.");
            }

            _pendingDefragMoves.Dequeue();
            movesProcessed++;
        }

        if (_pendingDefragMoves.Count == 0)
        {
            _isDefragging = false;
            _pagesWithPendingMoves.Clear();
            Logger.Instance.DebugWithCategory("Atlas", "Renderer atlas defragmentation complete");
        }

        return _pendingDefragMoves.Count > 0;
    }

    private bool TryPrepareAndQueueDefrag(
        int pageId,
        IReadOnlyList<(Rect2 OldRect, Rect2 NewRect)> moves,
        out string reason)
    {
        var page = _pages[pageId];
        if (page.Pixels == null)
        {
            reason = "the CPU atlas shadow buffer is unavailable";
            return false;
        }

        byte[] sourceSnapshot = page.Pixels.ToArray();
        byte[] finalPixels = sourceSnapshot.ToArray();
        var pendingMoves = new List<PendingDefragMove>(moves.Count);

        // Build the complete final page from one stable snapshot before committing
        // any manager metadata or queued GPU work. This keeps overlapping source and
        // destination move regions safe.
        foreach (var move in moves)
            RgbaPixelBuffer.ClearRegion(finalPixels, _pageSize, _pageSize, move.OldRect);

        for (int i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            if (!RgbaPixelBuffer.TryCopyRegion(
                    sourceSnapshot,
                    _pageSize,
                    _pageSize,
                    move.OldRect,
                    out byte[] region,
                    out int width,
                    out int height))
            {
                reason = $"move {i} could not copy its OldRect from the CPU atlas shadow buffer";
                return false;
            }

            RgbaPixelBuffer.Blit(
                region,
                width,
                height,
                finalPixels,
                _pageSize,
                _pageSize,
                (int)move.NewRect.X,
                (int)move.NewRect.Y);

            pendingMoves.Add(new PendingDefragMove(pageId, move.OldRect, move.NewRect));
        }

        // Commit only after the whole proposed layout has been prepared.
        page.Pixels = finalPixels;
        foreach (var pendingMove in pendingMoves)
            _pendingDefragMoves.Enqueue(pendingMove);

        _pagesWithPendingMoves.Add(pageId);
        _isDefragging = true;

        Logger.Instance.DebugWithCategory("Atlas",
            "Queued {0} renderer atlas defrag moves for page {1} (total pending: {2})",
            moves.Count, pageId, _pendingDefragMoves.Count);

        reason = string.Empty;
        return true;
    }

    private bool EvictAndRepack(
        (uint TextureId, Rect2 SrcRect) key,
        byte[] pixels,
        int width,
        int height,
        out Rect2 packedRect,
        out int pageId)
    {
        packedRect = default;
        pageId = -1;

        // Eviction is the final recovery path after normal packing and any useful
        // defragmentation attempt have already failed. Global occupancy is not a
        // reliable indicator of whether a specific rectangle can fit, so do not
        // block LRU recovery behind an arbitrary fullness threshold.
        if (_lruList.Count == 0)
            return false;

        LinkedListNode<(uint TextureId, Rect2 SrcRect)> node = _lruList.Last;
        while (node != null)
        {
            var previous = node.Previous;
            if (_packedMap.TryGetValue(node.Value, out var slot) &&
                !_pagesWithPendingMoves.Contains(slot.PageId))
            {
                var page = _pages[slot.PageId];
                if (!EnsurePageResources(slot.PageId))
                {
                    node = previous;
                    continue;
                }

                RgbaPixelBuffer.ClearRegion(page.Pixels, _pageSize, _pageSize, slot.PackedRect);
                if (!TryUploadRegion(page, slot.PackedRect))
                {
                    throw new InvalidOperationException(
                        $"Atlas eviction could not upload the cleared region {slot.PackedRect} on page {slot.PageId}.");
                }

                page.Packer.Free(slot.PackedRect);
                _packedMap.Remove(node.Value);
                _lruList.Remove(node);
                _evictionCount++;

                if (TryReservePackedRect(
                        slot.PageId,
                        width,
                        height,
                        out var rect,
                        out bool contractViolation))
                {
                    bool packed = PackIntoPage(
                        pixels,
                        width,
                        height,
                        key,
                        slot.PageId,
                        rect,
                        out packedRect,
                        out pageId);

                    if (packed)
                    {
                        Logger.Instance.DebugWithCategory("Atlas",
                            "Evicted texture from renderer page {0} to make room (total evictions: {1})",
                            slot.PageId, _evictionCount);
                    }

                    return packed;
                }

                if (contractViolation)
                    return false;
            }

            node = previous;
        }

        return false;
    }

    /// <summary>
    /// Gets the game-facing texture wrapper for an active atlas page.
    /// </summary>
    /// <param name="pageId">The zero-based atlas page index.</param>
    /// <returns>
    /// The page texture when the page is active and available; otherwise, <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// A page is unavailable while it has pending defragmentation moves. The returned
    /// texture is owned by the atlas manager and should not be disposed by the caller.
    /// </remarks>
    public Texture GetPageTexture(int pageId)
    {
        if (pageId < 0 || pageId >= _pages.Count)
            return null;

        var page = _pages[pageId];
        if (!page.IsActive || _pagesWithPendingMoves.Contains(pageId))
            return null;

        if (!EnsurePageResources(pageId))
            return null;

        return page.Texture;
    }

    private bool EnsurePageResources(int pageId)
    {
        if (pageId < 0 || pageId >= _pages.Count)
            return false;
        if (!RendererRuntime.TryGetDevice(out IGraphicsDevice device))
            return false;

        var page = _pages[pageId];

        if (page.GraphicsTexture != null && !ReferenceEquals(page.GraphicsDevice, device))
            page.DisposeGraphics();

        page.Pixels ??= new byte[checked(_pageSize * _pageSize * 4)];

        if (page.GraphicsTexture == null)
        {
            var description = new TextureDescription(
                _pageSize,
                _pageSize,
                TextureFormat.RGBA8,
                TextureUsage.Sampled | TextureUsage.TransferDestination,
                TextureFilter.Nearest,
                TextureFilter.Nearest,
                TextureWrap.ClampToEdge,
                TextureWrap.ClampToEdge);

            page.GraphicsDevice = device;
            page.GraphicsTexture = device.CreateTexture(description, page.Pixels);
            page.Texture = new Texture(device, page.GraphicsTexture, AssetType.Atlas);
        }

        page.IsActive = true;
        return page.GraphicsTexture.IsValid;
    }

    private bool TryUploadRegion(AtlasPage page, Rect2 rect)
    {
        if (!RgbaPixelBuffer.TryCopyRegion(
                page.Pixels,
                _pageSize,
                _pageSize,
                rect,
                out byte[] region,
                out int width,
                out int height))
        {
            return false;
        }

        page.GraphicsDevice.UpdateTexture(
            page.GraphicsTexture,
            (int)rect.X,
            (int)rect.Y,
            width,
            height,
            TextureFormat.RGBA8,
            region);

        return true;
    }

    /// <summary>
    /// Gets a snapshot of the atlas usage counters and packing area.
    /// </summary>
    /// <returns>A snapshot of the current atlas metrics.</returns>
    public AtlasMetrics GetMetrics()
    {
        int totalPixelArea = 0;
        int usedPixelArea = 0;
        int usedPages = 0;

        foreach (var page in _pages)
        {
            totalPixelArea += page.Packer.TotalSpace;
            usedPixelArea += page.Packer.UsedSpace;

            if (page.Packer.UsedSpace > 0)
                usedPages++;
        }

        return new AtlasMetrics
        {
            TotalPages = _pages.Count,
            UsedPages = usedPages,
            TotalPixelArea = totalPixelArea,
            UsedPixelArea = usedPixelArea,
            PercentageFull = totalPixelArea > 0
                ? (float)usedPixelArea / totalPixelArea * 100f
                : 0f,
            TextureCount = _packedMap.Count,
            EvictionCount = _evictionCount
        };
    }

    /// <summary>
    /// Removes all packed entries and releases atlas page graphics resources.
    /// </summary>
    /// <remarks>
    /// The configured page packers are reset and retained, so the manager can be used
    /// again after clearing. Usage, eviction, and defragmentation state are also reset.
    /// </remarks>
    public void Clear()
    {
        Logger.Instance.InfoWithCategory("Atlas", "Clearing renderer atlas: {0} textures, {1} pages",
            _packedMap.Count, _pages.Count);

        _packedMap.Clear();
        _lruList.Clear();
        _pendingDefragMoves.Clear();
        _pagesWithPendingMoves.Clear();
        _usageBatch = 0;
        _isDefragging = false;

        foreach (var page in _pages)
        {
            page.Packer.Clear();
            page.DisposeGraphics();
            page.Pixels = null;
            page.IsActive = false;
        }

        _evictionCount = 0;
    }
}
