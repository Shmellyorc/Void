namespace Void.Engine.Inputs.Gamepads;

internal static class GamepadDatabase
{
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        string currentPlatform = GetCurrentPlatform();

        string csv = EmbeddedResources.ReadAllText("Data/SDLDatabase.db");
        using var reader = new StringReader(csv);

#pragma warning disable CS8632
        string? line;
#pragma warning restore CS8632
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            int platformIndex = line.LastIndexOf("platform:", StringComparison.OrdinalIgnoreCase);
            if (platformIndex < 0)
                continue;

            int platformEnd = line.IndexOf(',', platformIndex);
            string platform = platformEnd > platformIndex
                ? line[(platformIndex + 9)..platformEnd].Trim()
                : line[(platformIndex + 9)..].Trim();

            if (!string.Equals(platform, currentPlatform, StringComparison.OrdinalIgnoreCase))
                continue;

            SDL3.SDL.AddGamepadMapping(line);
        }

        _loaded = true;
    }

    internal static void Reset()
    {
        _loaded = false;
    }

    private static string GetCurrentPlatform() =>
        OperatingSystem.IsWindows() ? "Windows" :
        OperatingSystem.IsMacOS() ? "Mac OS X" :
        OperatingSystem.IsLinux() ? "Linux" :
        "Windows";
}
