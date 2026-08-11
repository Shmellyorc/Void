using Void.Packer;
using Void.Packer.Utils;

namespace Void.Engine.Assets.Mounts;

public sealed class PackMount : IMount, IDisposable
{
    private readonly SolidPackReader _reader;
    private readonly string _mountName;
    private readonly Dictionary<string, string> _pathCache;
    private readonly Lock _cacheLock = new();
    private bool _isDisposed;

    public PackMount(byte[] packData, byte[] key = null, string mountName = null)
    {
        if (packData.IsEmpty())
            throw new ArgumentException("Pack data cannot be null or empty.", nameof(packData));

        _reader = new SolidPackReader(packData, key);
        _mountName = mountName ?? $"Pack mount ({_reader.FileCount}) files";
        _pathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // warm up:
        BuildPathCache();
    }

    public string Name => _mountName;

    public bool HasFile(string virtualPath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(PackMount));

        var normalized = PathNormalizer.Normalize(virtualPath);

        lock (_cacheLock)
        {
            if (_pathCache.TryGetValue(normalized, out _))
                return true;

            bool exists = _reader.FileExists(normalized);
            if (exists)
                _pathCache[normalized] = normalized;

            return exists;
        }
    }

    public byte[] ReadFile(string virtualPath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(PackMount));

        var normalized = PathNormalizer.Normalize(virtualPath);

        lock (_cacheLock)
        {
            if (_pathCache.TryGetValue(normalized, out var orginalPath))
            {
                return _reader.ReadFile(orginalPath);
            }
        }

        if (_reader.FileExists(normalized))
        {
            return _reader.ReadFile(normalized);
        }

        throw new FileNotFoundException(
            $"File '{virtualPath}' not found in pack mount '{_mountName}'"
        );
    }

    public bool VerifyIntegrity() => _reader.VerifyIntegrity();

    public IEnumerable<string> ListFiles() => _reader.ListFiles();

    private void BuildPathCache()
    {
        lock (_cacheLock)
        {
            _pathCache.Clear();
            foreach (var filepath in _reader.ListFiles())
            {
                var normalized = PathNormalizer.Normalize(filepath);
                _pathCache[normalized] = filepath;
            }
        }
    }

    public void Dispose()
    {
        if(!_isDisposed)
        {
            _reader?.Dispose();
            _pathCache.Clear();
            _isDisposed=true;
        }
        GC.SuppressFinalize(this);
    }
}
