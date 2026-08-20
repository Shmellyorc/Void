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

    public static bool EnsureDirectoryExists(string path)
    {
        // If path looks like it includes a filename, get its directory
        var directoryPath = path;
        if (!string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            directoryPath = Path.GetDirectoryName(path);
        }

        if (string.IsNullOrEmpty(directoryPath))
            return false;

        if (Directory.Exists(directoryPath))
            return false;

        Directory.CreateDirectory(directoryPath);
        return true;
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
        var logical = Normalize(ldtkPath);
        var root = Normalize(contentRoot);

        // Strip contentRoot/ if it's already prefixed
        if (!string.IsNullOrEmpty(root))
        {
            var rootPrefix = root + "/";
            if (logical.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                logical = logical.Substring(rootPrefix.Length);
            else if (string.Equals(logical, root, StringComparison.OrdinalIgnoreCase))
                logical = string.Empty;
        }

        return logical;
    }

    /// <summary>
    /// Normalizes a file path to a logical form by:
    /// - Converting all separators to forward slashes
    /// - Removing empty segments
    /// - Resolving "." and ".." segments
    /// - Preserving absolute/relative path distinction
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>A normalized logical path.</returns>
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        bool isAbsolute = path.StartsWith('/') ||
            path.StartsWith('\\') ||
            (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':');

        string driveLetter = "";
        if (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':')
        {
            driveLetter = path.Substring(0, 2);
            path = path.Substring(2);
        }

        var parts = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new Stack<string>();

        foreach (var part in parts)
        {
            if (part == ".")
                continue;

            if (part == "..")
            {
                if (stack.Count > 0 && stack.Peek() != "..")
                    stack.Pop();
                else if (!isAbsolute)
                    stack.Push("..");
                continue;
            }

            stack.Push(part);
        }

        var result = string.Join('/', stack.Reverse());

        if (!string.IsNullOrEmpty(driveLetter))
            return driveLetter + "/" + result;
        else if (isAbsolute)
            return "/" + result;

        return result;
    }
}