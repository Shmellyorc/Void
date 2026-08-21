// ============================================================================
//  FileHelper.cs
// ============================================================================
//  Utility methods for file system operations including path validation,
//  directory management, application data paths, and path normalization.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Helpers;

/// <summary>
/// Provides utility methods for file system operations including path validation,
/// file locking detection, directory management, application data paths, and
/// path normalization.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FileHelper"/> class provides a set of static helper methods
/// for common file system operations used throughout the engine, including:
/// <list type="bullet">
///   <item><description>Path validation and normalization</description></item>
///   <item><description>File locking detection</description></item>
///   <item><description>Directory creation with path parsing</description></item>
///   <item><description>Platform-specific application data paths</description></item>
///   <item><description>LDTK path remapping</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Validate a path
/// if (FileHelper.IsValidFilePath("myfile.txt"))
/// {
///     // Path is valid
/// }
/// 
/// // Check if a file is in use
/// if (FileHelper.IsFileInUse("myfile.txt"))
/// {
///     // File is locked by another process
/// }
/// 
/// // Get application data folder
/// string appData = FileHelper.GetApplicationData("MyCompany", "MyGame");
/// 
/// // Normalize a path
/// string normalized = FileHelper.Normalize("folder\\subfolder/../file.txt");
/// // Returns "folder/file.txt"
/// </code>
/// </para>
/// </remarks>
public static class FileHelper
{
    /// <summary>
    /// Determines whether the specified path is a valid file path.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns><see langword="true"/> if the path is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        try
        {
            Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines whether a file is currently locked or in use by another process.
    /// </summary>
    /// <param name="path">The path of the file to check.</param>
    /// <returns><see langword="true"/> if the file is in use; otherwise, <see langword="false"/>.</returns>
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
            return true;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Ensures that the directory for the specified path exists, creating it if necessary.
    /// </summary>
    /// <param name="path">The path to the file or directory.</param>
    /// <returns><see langword="true"/> if the directory was created; <see langword="false"/> if it already existed or could not be created.</returns>
    public static bool EnsureDirectoryExists(string path)
    {
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

    /// <summary>
    /// Gets the platform-specific application data folder path for the specified company and application.
    /// </summary>
    /// <param name="company">The company name (optional).</param>
    /// <param name="appName">The application name.</param>
    /// <returns>The full path to the application data folder.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="appName"/> is null or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// This method follows platform conventions:
    /// <list type="bullet">
    ///   <item><description><b>Windows:</b> %APPDATA%\Company\AppName</description></item>
    ///   <item><description><b>macOS:</b> ~/Library/Application Support/Company/AppName</description></item>
    ///   <item><description><b>Linux:</b> $XDG_CONFIG_HOME/Company/AppName or ~/.config/Company/AppName</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The folder is created automatically if it does not exist.
    /// </para>
    /// </remarks>
    public static string GetApplicationData(string company, string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            throw new ArgumentException("appName must be provided.", nameof(appName));

        string root;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "Library",
                "Application Support");
        }
        else
        {
            var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdgConfig))
            {
                root = xdgConfig;
            }
            else
            {
                root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    ".config");
            }
        }

        var folder = string.IsNullOrWhiteSpace(company)
            ? Path.Combine(root, appName)
            : Path.Combine(root, company, appName);

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        return folder;
    }

    /// <summary>
    /// Remaps an LDTK path to a logical path relative to the content root.
    /// </summary>
    /// <param name="ldtkPath">The LDTK path to remap.</param>
    /// <param name="contentRoot">The content root directory.</param>
    /// <returns>A logical path relative to the content root.</returns>
    public static string RemapLDTKPath(string ldtkPath, string contentRoot)
    {
        var logical = Normalize(ldtkPath);
        var root = Normalize(contentRoot);

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
    /// Normalizes a file path by converting separators, removing empty segments,
    /// and resolving "." and ".." segments.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>A normalized logical path.</returns>
    /// <remarks>
    /// <para>
    /// This method performs the following operations:
    /// <list type="bullet">
    ///   <item><description>Converts all backslashes to forward slashes</description></item>
    ///   <item><description>Removes empty segments</description></item>
    ///   <item><description>Resolves "." and ".." segments</description></item>
    ///   <item><description>Preserves absolute/relative path distinction</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Examples:</b>
    /// <list type="bullet">
    ///   <item><description><c>Normalize("folder\\subfolder/../file.txt")</c> → <c>"folder/file.txt"</c></description></item>
    ///   <item><description><c>Normalize("/folder/./subfolder/../file.txt")</c> → <c>"/folder/file.txt"</c></description></item>
    ///   <item><description><c>Normalize("C:\\folder\\file.txt")</c> → <c>"C:/folder/file.txt"</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
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