using DotNet.Globbing;

namespace Void.Packer.CLI.Utils;

public static class GlobMatcher
{
    public static bool Match(string path, string pattern)
    {
        var glob = Glob.Parse(pattern);
        return glob.IsMatch(path);
    }

    // public static bool Match(string path, string pattern)
    // {
    //     if (string.IsNullOrEmpty(pattern))
    //         return true;

    //     if (pattern == "**" || pattern == "**/*")
    //         return true;

    //     // Handle recursive wildcard: **/ 
    //     if (pattern.Contains("**/"))
    //     {
    //         // Split on **/ but keep the parts
    //         string[] parts = pattern.Split(new[] { "**/" }, StringSplitOptions.None);

    //         if (parts.Length == 2)
    //         {
    //             string prefix = parts[0];
    //             string suffix = parts[1];

    //             // Check prefix (before **/)
    //             if (!string.IsNullOrEmpty(prefix) && !path.StartsWith(prefix))
    //                 return false;

    //             // Check suffix using regex (not literal!)
    //             if (!string.IsNullOrEmpty(suffix))
    //             {
    //                 // Convert suffix to regex pattern
    //                 string suffixPattern = "^" + Regex.Escape(suffix)
    //                     .Replace("\\*", "[^/]*")
    //                     .Replace("\\?", ".") + "$";

    //                 // Check if any part of the path matches the suffix pattern
    //                 // Need to check each path segment after the prefix
    //                 string searchPath = string.IsNullOrEmpty(prefix) ? path : path.Substring(prefix.Length);
    //                 string[] segments = searchPath.Split('/');

    //                 foreach (var segment in segments)
    //                 {
    //                     if (Regex.IsMatch(segment, suffixPattern))
    //                         return true;
    //                 }
    //                 return false;
    //             }

    //             return true;
    //         }
    //     }

    //     // Handle simple wildcard: *
    //     if (pattern.Contains('*'))
    //     {
    //         string regexPattern = "^" + Regex.Escape(pattern)
    //             .Replace("\\*\\*", ".*")
    //             .Replace("\\*", "[^/]*")
    //             .Replace("\\?", ".") + "$";

    //         return Regex.IsMatch(path, regexPattern);
    //     }

    //     // Exact match
    //     return path == pattern;
    // }
}