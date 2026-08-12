namespace Void.Engine.Helpers;

public static class InstanceHelper
{
    private static readonly List<Assembly> GameAssemblies = [];
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, bool> FailedLookups = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ReaderWriterLockSlim AssemblyLock = new();

    static InstanceHelper()
    {
        LoadAssemblies();
    }

    public static void RefreshAssemblies()
    {
        AssemblyLock.EnterWriteLock();
        try
        {
            LoadAssemblies();
            TypeCache.Clear();
            FailedLookups.Clear();
        }
        finally
        {
            AssemblyLock.ExitWriteLock();
        }
    }

    private static void LoadAssemblies()
    {
        GameAssemblies.Clear();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name != null &&
                !name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) &&
                !name.Equals("netstandard", StringComparison.OrdinalIgnoreCase) &&
                !name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
            {
                GameAssemblies.Add(assembly);
            }
        }
    }

    public static bool TryCreateInstance<T>(string name, bool ignoreCase, object[] args, out T instance) where T : class
    {
        instance = CreateInstance<T>(name, ignoreCase, args);
        return instance != null;
    }

    public static T CreateInstance<T>(string name, bool ignoreCase, object[] args) where T : class
    {
        if (string.IsNullOrEmpty(name))
            return null!;

        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        string cacheKey = $"{typeof(T).FullName}:{name}";

        if (FailedLookups.ContainsKey(cacheKey))
            return null!;

        var type = TypeCache.GetOrAdd(cacheKey, key =>
        {
            var foundType = FindType<T>(name, comparison);
            if (foundType == null)
            {
                FailedLookups.TryAdd(cacheKey, true);
            }
            return foundType;
        });

        if (type == null)
            return null!;

        try
        {
            return (T)Activator.CreateInstance(type, args ?? Array.Empty<object>())!;
        }
        catch
        {
            return null!;
        }
    }

    private static Type FindType<T>(string name, StringComparison comparison)
    {
        AssemblyLock.EnterReadLock();
        try
        {
            foreach (var assembly in GameAssemblies)
            {
                try
                {
                    var type = assembly.GetType(name, false, comparison == StringComparison.OrdinalIgnoreCase);
                    if (type != null && typeof(T).IsAssignableFrom(type))
                        return type;
                }
                catch
                {
                    continue;
                }
            }

            foreach (var assembly in GameAssemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name.Equals(name, comparison) && typeof(T).IsAssignableFrom(type))
                            return type;
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    if (ex.Types != null)
                    {
                        foreach (var type in ex.Types)
                        {
                            if (type != null &&
                                type.Name.Equals(name, comparison) &&
                                typeof(T).IsAssignableFrom(type))
                                return type;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }
        finally
        {
            AssemblyLock.ExitReadLock();
        }
    }

    public static T CreateInstanceFromType<T>(Type type, object[] args) where T : class
    {
        if (type == null)
            return null;

        if (!typeof(T).IsAssignableFrom(type))
            return null;

        try
        {
            return Activator.CreateInstance(type, args ?? Array.Empty<object>()) as T;
        }
        catch
        {
            return null;
        }
    }

    public static bool TryCreateInstanceFromType<T>(Type type, object[] args, out T instance) where T : class
    {
        instance = CreateInstanceFromType<T>(type, args);
        return instance != null;
    }

    public static T CreateInstanceFromObject<T>(object obj, object[] args) where T : class
    {
        if (obj == null)
            return null;

        return CreateInstance<T>(obj.GetType().Name, true, args);
    }

    public static bool TryCreateInstanceFromObject<T>(object obj, object[] args, out T instance) where T : class
    {
        instance = CreateInstanceFromObject<T>(obj, args);
        return instance != null;
    }
}