global using System.Reflection;

namespace Void.Engine.Resources;

public static class EmbeddedResources
{
    private static readonly Assembly _assembly = typeof(EmbeddedResources).Assembly;
    
    public static string ReadAllText(string resourcePath)
    {
        using var stream = GetStream(resourcePath);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    
    public static byte[] ReadAllBytes(string resourcePath)
    {
        using var stream = GetStream(resourcePath);
        byte[] buffer = new byte[stream.Length];
        stream.ReadExactly(buffer, 0, buffer.Length);
        return buffer;
    }
    
    public static Stream GetStream(string resourcePath)
    {
        // Converts "Data/SDLDatabase.db" to "Void.Engine.Resources.Data.SDLDatabase.db"
        string fullPath = $"{_assembly.GetName().Name}.Resources.{resourcePath.Replace('/', '.').Replace('\\', '.')}";

#pragma warning disable CS8632 
        Stream? stream = _assembly.GetManifestResourceStream(fullPath);
#pragma warning restore CS8632 
        if (stream == null)
            throw new FileNotFoundException($"Embedded resource '{fullPath}' not found.");
        
        return stream;
    }
    
    public static bool Exists(string resourcePath)
    {
        string fullPath = $"{_assembly.GetName().Name}.Resources.{resourcePath.Replace('/', '.').Replace('\\', '.')}";
        return _assembly.GetManifestResourceStream(fullPath) != null;
    }
}