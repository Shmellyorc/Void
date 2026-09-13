// ============================================================================
//  FileHelper.cs
// ============================================================================
//  File-system paths, application-data folders, and logical path normalization.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Void.Engine.Helpers;

/// <summary>
/// Provides common file-system and logical-path helpers used by VOID.
/// </summary>
/// <remarks>
/// <see cref="Normalize"/> produces forward-slash logical paths and does not access
/// the file system. <see cref="GetApplicationData"/> and
/// <see cref="EnsureDirectoryExists"/> perform real file-system operations.
/// </remarks>
public static class FileHelper
{
    /// <summary>Determines whether a path can be converted to a full file-system path.</summary>
    /// <param name="path">Path to validate.</param>
    /// <returns><see langword="true"/> when .NET accepts the path; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            _ = Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Performs a best-effort check for whether an existing file is unavailable for exclusive reading.</summary>
    /// <param name="path">File to test.</param>
    /// <returns>
    /// <see langword="true"/> when opening the existing file for exclusive read access fails;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// File-sharing semantics vary by platform and file system, so this is an availability
    /// probe rather than a guaranteed cross-process lock detector.
    /// </remarks>
    public static bool IsFileInUse(string path)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>Ensures that the exact directory path exists.</summary>
    /// <param name="path">Directory path to create.</param>
    /// <returns><see langword="true"/> when a directory was created; <see langword="false"/> when it already existed or the path is empty.</returns>
    /// <exception cref="IOException">A file-system error prevents directory creation.</exception>
    /// <exception cref="UnauthorizedAccessException">The process does not have permission to create the directory.</exception>
    /// <remarks>
    /// This method treats <paramref name="path"/> as a directory even when its final
    /// segment contains a period. For a file path, pass <see cref="Path.GetDirectoryName(string)"/>
    /// to this method instead.
    /// </remarks>
    public static bool EnsureDirectoryExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (Directory.Exists(path))
            return false;

        Directory.CreateDirectory(path);
        return true;
    }

    /// <summary>Gets and creates the platform-specific application data folder.</summary>
    /// <param name="company">Optional company folder.</param>
    /// <param name="appName">Required application folder name.</param>
    /// <returns>The full application data folder path.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="appName"/> is null or whitespace.</exception>
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
            string xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            root = !string.IsNullOrEmpty(xdgConfig)
                ? xdgConfig
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config");
        }

        string folder = string.IsNullOrWhiteSpace(company)
            ? Path.Combine(root, appName)
            : Path.Combine(root, company, appName);

        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>Remaps an LDtk path to a normalized logical path relative to a content root.</summary>
    /// <param name="ldtkPath">LDtk-provided path.</param>
    /// <param name="contentRoot">Content-root prefix to remove when present.</param>
    /// <returns>The normalized logical path.</returns>
    /// <remarks>
    /// Root-prefix matching follows the host file-system convention: Windows matching is
    /// case-insensitive, while Linux and macOS matching is case-sensitive.
    /// </remarks>
    public static string RemapLDTKPath(string ldtkPath, string contentRoot)
    {
        string logical = Normalize(ldtkPath);
        string root = Normalize(contentRoot);

        if (string.IsNullOrEmpty(root))
            return logical;

        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        string rootPrefix = root.EndsWith('/') ? root : root + "/";
        if (logical.StartsWith(rootPrefix, comparison))
            return logical[rootPrefix.Length..];

        return string.Equals(logical, root, comparison) ? string.Empty : logical;
    }

    /// <summary>
    /// Normalizes a logical path by using forward slashes and resolving dot segments.
    /// </summary>
    /// <param name="path">Path to normalize.</param>
    /// <returns>A normalized logical path, or an empty string for null or whitespace input.</returns>
    /// <remarks>
    /// Absolute Unix roots, Windows drive roots, drive-relative prefixes, and UNC-style
    /// double-slash prefixes are preserved. The method performs lexical normalization only.
    /// </remarks>
    public static string Normalize(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        string normalized = path.Replace('\\', '/');
        bool hasDrive = normalized.Length >= 2 && char.IsLetter(normalized[0]) && normalized[1] == ':';
        string drive = hasDrive ? normalized[..2] : string.Empty;
        string remainder = hasDrive ? normalized[2..] : normalized;

        bool driveAbsolute = hasDrive && remainder.StartsWith('/');
        bool isUnc = !hasDrive && remainder.StartsWith("//", StringComparison.Ordinal);
        bool isAbsolute = !hasDrive && !isUnc && remainder.StartsWith('/');

        string[] parts = remainder.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new Stack<string>(parts.Length);

        foreach (string part in parts)
        {
            if (part == ".")
                continue;

            if (part == "..")
            {
                if (stack.Count > 0 && stack.Peek() != "..")
                {
                    stack.Pop();
                }
                else if (!driveAbsolute && !isAbsolute && !isUnc)
                {
                    stack.Push("..");
                }

                continue;
            }

            stack.Push(part);
        }

        string result = string.Join('/', stack.Reverse());

        if (hasDrive)
        {
            if (driveAbsolute)
                return result.Length == 0 ? drive + "/" : drive + "/" + result;

            return result.Length == 0 ? drive : drive + result;
        }

        if (isUnc)
            return result.Length == 0 ? "//" : "//" + result;

        if (isAbsolute)
            return result.Length == 0 ? "/" : "/" + result;

        return result;
    }
}
