namespace Void.Engine.Inputs.Gamepads;

internal static class GamepadDatabase
{
    private static readonly Dictionary<string, GamepadMapping> _mappings = [];
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;

        string csv = EmbeddedResources.ReadAllText("Data/SDLDatabase.db");
        using var reader = new StringReader(csv);

#pragma warning disable CS8632 
        string? line;
#pragma warning restore CS8632 
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            int firstComma = line.IndexOf(',');
            if (firstComma < 0) continue;

            string guid = line[..firstComma].ToLowerInvariant();
            string rest = line[(firstComma + 1)..];

            int platformIndex = rest.LastIndexOf("platform:", StringComparison.OrdinalIgnoreCase);
            if (platformIndex < 0) continue;

            int platformEnd = rest.IndexOf(',', platformIndex);
            string platform = platformEnd > platformIndex
                ? rest[(platformIndex + 9)..platformEnd].Trim()
                : rest[(platformIndex + 9)..].Trim();

            if (!string.Equals(platform, "Windows", StringComparison.OrdinalIgnoreCase))
                continue;

            int mappingsEnd = rest.LastIndexOf(',', platformIndex);
            string mappingPart = rest[(firstComma + 1 + rest.IndexOf(',') + 1)..mappingsEnd];

            if (_mappings.ContainsKey(guid))
                continue;

            _mappings[guid] = ParseMapping(mappingPart);
        }

        _loaded = true;
    }

    private static GamepadMapping ParseMapping(string data)
    {
        var mapping = new GamepadMapping();
        var parts = data.Split(',');

        foreach (string part in parts)
        {
            int colon = part.IndexOf(':');
            if (colon <= 0) continue;

            string key = part[..colon];
            string value = part[(colon + 1)..];

            switch (key)
            {
                case "a": mapping.A = ParseInput(value); break;
                case "b": mapping.B = ParseInput(value); break;
                case "x": mapping.X = ParseInput(value); break;
                case "y": mapping.Y = ParseInput(value); break;
                case "back": mapping.Back = ParseInput(value); break;
                case "start": mapping.Start = ParseInput(value); break;
                case "guide": mapping.Guide = ParseInput(value); break;
                case "leftshoulder": mapping.LeftShoulder = ParseInput(value); break;
                case "rightshoulder": mapping.RightShoulder = ParseInput(value); break;
                case "leftstick": mapping.LeftStick = ParseInput(value); break;
                case "rightstick": mapping.RightStick = ParseInput(value); break;
                case "lefttrigger": mapping.LeftTrigger = ParseInput(value); break;
                case "righttrigger": mapping.RightTrigger = ParseInput(value); break;
                case "leftx": mapping.LeftX = ParseInput(value); break;
                case "lefty": mapping.LeftY = ParseInput(value); break;
                case "rightx": mapping.RightX = ParseInput(value); break;
                case "righty": mapping.RightY = ParseInput(value); break;
                case "dpup": mapping.DPadUp = ParseInput(value); break;
                case "dpdown": mapping.DPadDown = ParseInput(value); break;
                case "dpleft": mapping.DPadLeft = ParseInput(value); break;
                case "dpright": mapping.DPadRight = ParseInput(value); break;
                case "touchpad": mapping.Touchpad = ParseInput(value); break;
                case "paddle1": mapping.Paddle1 = ParseInput(value); break;
                case "paddle2": mapping.Paddle2 = ParseInput(value); break;
                case "paddle3": mapping.Paddle3 = ParseInput(value); break;
                case "paddle4": mapping.Paddle4 = ParseInput(value); break;
                case "misc1": mapping.Misc1 = ParseInput(value); break;
            }
        }

        return mapping;
    }

    private static GamepadInput ParseInput(string value)
    {
        // b0-b31 = button
        // a0-a5 = axis
        // h0.1-h0.8 = hat with mask
        // +a0, -a0 = axis direction
        // +a0~, -a0~ = inverted axis direction

        if (value.StartsWith('b'))
            return new GamepadInput { Type = InputType.Button, Index = int.Parse(value[1..]) };

        if (value.StartsWith('h'))
        {
            int dot = value.IndexOf('.');
            return new GamepadInput
            {
                Type = InputType.Hat,
                Index = int.Parse(value[1..dot]),
                HatMask = int.Parse(value[(dot + 1)..])
            };
        }

        if (value.StartsWith("+a") || value.StartsWith("-a"))
        {
            bool negative = value.StartsWith('-');
            string numStr = value[2..].TrimEnd('~');
            bool inverted = value.EndsWith('~');
            return new GamepadInput
            {
                Type = InputType.AxisDirection,
                Index = int.Parse(numStr),
                AxisNegative = negative,
                AxisInverted = inverted
            };
        }

        if (value.StartsWith('a'))
            return new GamepadInput { Type = InputType.Axis, Index = int.Parse(value[1..]) };

        return new GamepadInput { Type = InputType.None };
    }

#pragma warning disable CS8632 
    public static GamepadMapping? GetMapping(string guid)
#pragma warning restore CS8632 
    {
        Load();
        guid = guid.ToLowerInvariant();
        return _mappings.TryGetValue(guid, out var mapping) ? mapping : null;
    }
}

internal enum InputType { None, Button, Axis, AxisDirection, Hat }

internal struct GamepadInput
{
    public InputType Type;
    public int Index;
    public int HatMask;
    public bool AxisNegative;
    public bool AxisInverted;
}

internal class GamepadMapping
{
    public GamepadInput A, B, X, Y;
    public GamepadInput Back, Start, Guide;
    public GamepadInput LeftShoulder, RightShoulder;
    public GamepadInput LeftStick, RightStick;
    public GamepadInput LeftTrigger, RightTrigger;
    public GamepadInput LeftX, LeftY, RightX, RightY;
    public GamepadInput DPadUp, DPadDown, DPadLeft, DPadRight;
    public GamepadInput Touchpad;
    public GamepadInput Paddle1, Paddle2, Paddle3, Paddle4;
    public GamepadInput Misc1;
}