namespace Void.Engine.Helpers;

public static class FileHelper
{
    public static bool IsValidFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        try
        {
            // Path.GetFullPath validates everything - invalid chars, structure, etc.
            Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsFileInUse(string path)
    {
        if (!File.Exists(path)) return false;

        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true; // File is locked
        }
        catch
        {
            return true; // Can't access for other reasons
        }
    }

    public static void EnsureDirectoryExists(string path)
    {
        // If path looks like it includes a filename, get its directory
        var directoryPath = path;
        if (!string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            directoryPath = Path.GetDirectoryName(path);
        }

        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    public static string GetApplicationData(string company, string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("appName must be provided.", nameof(appName));

        string root;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // e.g. C:\Users\Me\AppData\Roaming
            root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // e.g. /Users/me/Library/Application Support
            root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "Library",
                "Application Support");
        }
        else
        {
            // Linux/Unix: Follow XDG Base Directory Specification
            // XDG_CONFIG_HOME already points to the config directory (e.g., /home/me/.config)
            var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdgConfig))
            {
                root = xdgConfig;
            }
            else
            {
                // Default: ~/.config
                root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    ".config");
            }
        }

        // include optional company subfolder
        var folder = string.IsNullOrWhiteSpace(company)
            ? Path.Combine(root, appName)
            : Path.Combine(root, company, appName);

        // create if missing
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        return folder;
    }

    public static string RemapLDTKPath(string ldtkPath, string contentRoot)
    {
        var logical = ToLogical(ldtkPath);
        var root = ToLogical(contentRoot);

        // Strip contentRoot/ if it's already prefixed
        if (!string.IsNullOrEmpty(root))
        {
            var rootPrefix = root + "/";
            if (logical.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                logical = logical.Substring(rootPrefix.Length);
            else if (string.Equals(logical, root, StringComparison.OrdinalIgnoreCase))
                logical = string.Empty;
        }

        return logical; // content-root-relative path for AssetManager
    }

    private static string ToLogical(string p)
    {
        if (string.IsNullOrWhiteSpace(p)) return string.Empty;

        bool isAbsolute = p.StartsWith('/') || p.StartsWith('\\');

        var parts = p.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new Stack<string>();

        foreach (var part in parts)
        {
            if (part == ".") continue;
            if (part == "..")
            {
                if (stack.Count > 0) stack.Pop();
                continue;
            }
            stack.Push(part);
        }

        var result = string.Join('/', stack.Reverse());
        return isAbsolute ? "/" + result : result;
    }
}