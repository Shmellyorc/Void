// ============================================================================
//  IGraphicsBufferSource.cs
// ============================================================================
//  Internal access to renderer-owned graphics buffers used by draw submission.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using Void.Engine.Graphics.Rendering;

namespace Void.Engine.Graphics.RenderTargets;

internal interface IGraphicsBufferSource
{
    bool TryGetGraphicsBuffer(out IGraphicsBuffer buffer);
}
