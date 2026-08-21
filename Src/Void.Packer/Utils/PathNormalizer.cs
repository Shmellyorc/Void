// ============================================================================
//  PathNormalizer.cs
// ============================================================================
//  Utility for normalizing file paths to a consistent format for SolidPack.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Packer.Utils;

internal static class PathNormalizer
{
    public static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        path = path.Replace('\\', '/');

        if (path.StartsWith('/'))
            path = path[1..];

        path = path.Replace("..", "");

        while (path.Contains("//"))
            path = path.Replace("//", "/");

        return path;
    }
}