namespace Void.Engine.Helpers;

/// <summary>
/// Defines how discoverable assemblies are scanned.
/// </summary>
public enum AssemblyScanMode
{
    /// <summary>Scan all loaded assemblies.</summary>
    All,

    /// <summary>Exclude framework and Void assemblies. This is the default.</summary>
    ExcludeFramework,

    /// <summary>Use a custom assembly filter provided by the developer.</summary>
    Custom,

    /// <summary>Only scan assemblies explicitly added to the whitelist.</summary>
    Whitelist,

    /// <summary>Scan all assemblies except those explicitly added to the blacklist.</summary>
    Blacklist
}

/// <summary>
/// Provides reflection-based utility methods for working with types marked with the <see cref="DiscoverableAttribute"/>.
/// This static helper is used by the Snap engine to locate and process classes tagged as <c>[Discoverable]</c>
/// across loaded assemblies, including engine modules, scripts, and mod/plugin DLLs.
/// </summary>
public static class DiscoverableHelper
{
    private static int _loadVersion;
    private static int _scannedVersion = -1;
    private static List<Type> _allTypes;
    private static readonly Lock _allTypesLock = new();
    private static readonly ConcurrentDictionary<Type, DiscoverableAttribute> _metaCache = [];
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<Type>> _findAllCache = [];
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<(Type Type, DiscoverableAttribute Meta)>> _filterCache = [];

    static DiscoverableHelper()
    {
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
    }

    private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
        if (IsGameAssembly(args.LoadedAssembly))
            Interlocked.Increment(ref _loadVersion);
    }

    private static List<Type> AllTypes
    {
        get
        {
            var currentVersion = Volatile.Read(ref _loadVersion);
            if (_scannedVersion == currentVersion && _allTypes != null)
                return _allTypes;

            lock (_allTypesLock)
            {
                if (_scannedVersion == currentVersion && _allTypes != null)
                    return _allTypes;

                _allTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && IsGameAssembly(a))
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch (ReflectionTypeLoadException ex)
                        {
                            return ex.Types?.Where(t => t != null) ?? Array.Empty<Type>();
                        }
                        catch (TypeLoadException)
                        {
                            return Array.Empty<Type>();
                        }
                        catch (FileNotFoundException)
                        {
                            return Array.Empty<Type>();
                        }
                    })
                    .Where(t => t != null && t.IsClass && !t.IsAbstract)
                    .ToList();

                _scannedVersion = currentVersion;
                _filterCache.Clear();
                _findAllCache.Clear();

                return _allTypes;
            }
        }
    }

    private static bool IsGameAssembly(Assembly assembly)
    {
        var name = assembly.GetName().Name;
        if (name == null) return false;

        if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Void.", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
            return false;

        var settings = GameSettings.Instance;
        if (settings?.DiscoverableScanMode == null)
            return true;

        return settings.DiscoverableScanMode switch
        {
            AssemblyScanMode.All => true,
            AssemblyScanMode.ExcludeFramework => IsGameAssembly(assembly),
            AssemblyScanMode.Custom => settings.DiscoverableAssemblyFilter?.Invoke(assembly) ?? true,
            AssemblyScanMode.Whitelist => settings.DiscoverableAssemblies.Contains(name),
            AssemblyScanMode.Blacklist => !settings.DiscoverableAssemblies.Contains(name),
            _ => true
        };
    }

    private static IEnumerable<(Type Type, DiscoverableAttribute Meta)> AllWithMeta() =>
        AllTypes.Select(t => (Type: t, Meta: _metaCache.GetOrAdd(t, GetMeta)))
                .Where(x => x.Meta != null);

    private static IReadOnlyList<(Type Type, DiscoverableAttribute Meta)> Filter<T>() =>
        _filterCache.GetOrAdd(typeof(T), _ =>
            AllWithMeta()
                .Where(x =>
                    typeof(T).IsAssignableFrom(x.Type) &&
                    x.Type != typeof(T) &&
                    x.Meta.Enabled
                )
                .ToList()
                .AsReadOnly()
        );

    /// <summary>
    /// Retrieves all discoverable types implementing or inheriting T.
    /// Uses a cache per T to avoid repeated enumeration.
    /// </summary>
    public static IReadOnlyList<Type> FindAll<T>()
    {
        var key = typeof(T);
        return _findAllCache.GetOrAdd(key, _ =>
            Filter<T>()
                .OrderBy(x => x.Meta.Priority)
                .Select(x => x.Type)
                .ToList()
                .AsReadOnly()
        );
    }

    /// <summary>
    /// Attempts to find exactly one discoverable type by name; returns null otherwise.
    /// </summary>
    public static Type FindSingleByName<T>(string name)
    {
        var matches = FindManyByName<T>(name);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Enum overload for FindSingleByName; uses the enum's string representation.
    /// </summary>
    public static Type FindSingleByName<T>(Enum name)
        => FindSingleByName<T>(name.ToEnumString());

    /// <summary>
    /// Attempts to find exactly one discoverable type within the specified category.
    /// </summary>
    public static Type FindSingleByCategory<T>(string category)
    {
        var matches = FindManyByCategory<T>(category);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Enum overload for FindSingleByCategory; uses the enum's string representation.
    /// </summary>
    public static Type FindSingleByCategory<T>(Enum category)
        => FindSingleByCategory<T>(category.ToEnumString());

    /// <summary>
    /// Finds a single discoverable type whose name and category match the given values.
    /// </summary>
    public static Type FindSingleByNameAndCategory<T>(string name, string category)
    {
        var matches = FindManyByNameAndCategory<T>(name, category);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Finds a single discoverable type whose name and category match the given values.
    /// </summary>
    public static Type FindSingleByNameAndCategory<T>(string name, Enum category)
    {
        var matches = FindManyByNameAndCategory<T>(name, category);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Finds a single discoverable type whose name and category match the given values.
    /// </summary>
    public static Type FindSingleByNameAndCategory<T>(Enum name, string category)
    {
        var matches = FindManyByNameAndCategory<T>(name, category);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Finds a single discoverable type whose name and category match the given enum values.
    /// </summary>
    public static Type FindSingleByNameAndCategory<T>(Enum name, Enum category)
    {
        var matches = FindManyByNameAndCategory<T>(name, category);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Attempts to find exactly one discoverable type by name.
    /// </summary>
    public static bool TryFindSingleByName<T>(string name, out Type type)
    {
        type = FindSingleByName<T>(name);
        return type != null;
    }

    /// <summary>
    /// Enum overload for TryFindSingleByName.
    /// </summary>
    public static bool TryFindSingleByName<T>(Enum name, out Type type)
    {
        type = FindSingleByName<T>(name);
        return type != null;
    }

    /// <summary>
    /// Attempts to find exactly one discoverable type within the specified category.
    /// </summary>
    public static bool TryFindSingleByCategory<T>(string category, out Type type)
    {
        type = FindSingleByCategory<T>(category);
        return type != null;
    }

    /// <summary>
    /// Enum overload for TryFindSingleByCategory.
    /// </summary>
    public static bool TryFindSingleByCategory<T>(Enum category, out Type type)
    {
        type = FindSingleByCategory<T>(category);
        return type != null;
    }

    /// <summary>
    /// Attempts to find exactly one discoverable type by name and category.
    /// </summary>
    public static bool TryFindSingleByNameAndCategory<T>(string name, string category, out Type type)
    {
        type = FindSingleByNameAndCategory<T>(name, category);
        return type != null;
    }

    /// <summary>
    /// Attempts to find exactly one discoverable type by name and category.
    /// </summary>
    public static bool TryFindSingleByNameAndCategory<T>(string name, Enum category, out Type type)
    {
        type = FindSingleByNameAndCategory<T>(name, category);
        return type != null;
    }

    /// <summary>
    /// Attempts to find exactly one discoverable type by name and category.
    /// </summary>
    public static bool TryFindSingleByNameAndCategory<T>(Enum name, string category, out Type type)
    {
        type = FindSingleByNameAndCategory<T>(name, category);
        return type != null;
    }

    /// <summary>
    /// Attempts to find exactly one discoverable type by name and category.
    /// </summary>
    public static bool TryFindSingleByNameAndCategory<T>(Enum name, Enum category, out Type type)
    {
        type = FindSingleByNameAndCategory<T>(name, category);
        return type != null;
    }

    /// <summary>
    /// Retrieves all discoverable types matching the specified internal name.
    /// </summary>
    public static IReadOnlyList<Type> FindManyByName<T>(string name) =>
        Filter<T>()
            .Where(x => string.Equals(x.Meta.InternalName, name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Meta.Priority)
            .Select(x => x.Type)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Enum overload for FindManyByName; uses the enum's string representation.
    /// </summary>
    public static IReadOnlyList<Type> FindManyByName<T>(Enum name) =>
        FindManyByName<T>(name.ToEnumString());

    /// <summary>
    /// Retrieves all discoverable types within the specified category.
    /// </summary>
    public static IReadOnlyList<Type> FindManyByCategory<T>(string category) =>
        Filter<T>()
            .Where(x => string.Equals(x.Meta.InternalCategory, category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Meta.Priority)
            .Select(x => x.Type)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Enum overload for FindManyByCategory; uses the enum's string representation.
    /// </summary>
    public static IReadOnlyList<Type> FindManyByCategory<T>(Enum category) =>
        FindManyByCategory<T>(category.ToEnumString());

    /// <summary>
    /// Overload combining name and category filters for discoverable types.
    /// </summary>
    public static IReadOnlyList<Type> FindManyByNameAndCategory<T>(string name, string category) =>
        Filter<T>()
            .Where(x =>
                string.Equals(x.Meta.InternalName, name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Meta.InternalCategory, category, StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(x => x.Meta.Priority)
            .Select(x => x.Type)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Enum overload for FindManyByNameAndCategory; uses the enum's string representations.
    /// </summary>
    public static IReadOnlyList<Type> FindManyByNameAndCategory<T>(Enum name, Enum category) =>
        FindManyByNameAndCategory<T>(name.ToEnumString(), category.ToEnumString());

    /// <summary>
    /// Enum overload for FindManyByNameAndCategory; uses the enum's string representations.
    /// </summary>
    public static IReadOnlyList<Type> FindManyByNameAndCategory<T>(string name, Enum category) =>
        FindManyByNameAndCategory<T>(name, category.ToEnumString());

    /// <summary>
    /// Enum overload for FindManyByNameAndCategory; uses the enum's string representations.
    /// </summary>
    public static IReadOnlyList<Type> FindManyByNameAndCategory<T>(Enum name, string category) =>
        FindManyByNameAndCategory<T>(name.ToEnumString(), category);

    private static DiscoverableAttribute GetMeta(Type t) =>
        t.GetCustomAttribute<DiscoverableAttribute>(inherit: false);

    /// <summary>
    /// Invalidates all internal caches. Call this when assemblies are loaded/unloaded at runtime
    /// (e.g., hot-reload or dynamic mod loading).
    /// </summary>
    public static void InvalidateCaches()
    {
        Interlocked.Increment(ref _loadVersion);
        lock (_allTypesLock)
        {
            _allTypes = null;
            _scannedVersion = -1;
        }
        _metaCache.Clear();
        _findAllCache.Clear();
        _filterCache.Clear();
    }
}

/// <summary>
/// Marks an assembly as a library mod that provides infrastructure for other mods.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class LibraryModAttribute : Attribute
{
}

/// <summary>
/// Marks a class as discoverable by the Snap engine's reflection-based systems.
/// This attribute is used to identify types that should be automatically found and exposed
/// for editor tools, debug UIs, runtime registration, scripting, and mod/plugin support.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DiscoverableAttribute : Attribute
{
    /// <summary>
    /// The display name or identifier for this discoverable item.  
    /// Can be any object, but typically a string or enum value.
    /// </summary>
    public object Name { get; set; }

    /// <summary>
    /// The category under which this discoverable item should be grouped.  
    /// Can be any object, but typically a string or enum value.
    /// </summary>
    public object Category { get; set; }

    /// <summary>
    /// Ordering priority for this discoverable item within its category.  
    /// Lower numbers indicate higher priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Indicates whether this discoverable item is enabled and should be included in searches or registrations.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Developer-defined metadata associated with this discoverable item.
    /// </summary>
    public object Metadata { get; set; }

    internal string InternalName =>
        Name is Enum e
            ? $"{e.GetType().FullName}.{e}"
            : Name?.ToString() ?? "";

    internal string InternalCategory =>
        Category is Enum e
            ? $"{e.GetType().FullName}.{e}"
            : Category?.ToString() ?? "";

    /// <summary>
    /// Returns the metadata cast to the specified type, or default if not assignable.
    /// </summary>
    public T MetadataAs<T>() => Metadata is T typed ? typed : default;

    /// <summary>
    /// Returns the metadata cast to the specified type, or the provided default if not assignable.
    /// </summary>
    public T MetadataAs<T>(T defaultValue) => Metadata is T typed ? typed : defaultValue;

    /// <summary>
    /// Attempts to cast metadata to the specified type.
    /// </summary>
    public bool TryMetadataAs<T>(out T result)
    {
        if (Metadata is T typed)
        {
            result = typed;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Returns the name cast to the specified type, or default if not assignable.
    /// </summary>
    public T NameAs<T>() => Name is T typed ? typed : default;

    /// <summary>
    /// Returns the category cast to the specified type, or default if not assignable.
    /// </summary>
    public T CategoryAs<T>() => Category is T typed ? typed : default;
}

