// ============================================================================
//  DiscoverableHelper.cs
// ============================================================================
//  Reflection-based discovery system for locating and managing types marked
//  with the [Discoverable] attribute across loaded assemblies.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Helpers;

/// <summary>
/// Defines how discoverable assemblies are scanned for types.
/// </summary>
public enum AssemblyScanMode
{
    /// <summary>
    /// Scan all loaded assemblies without filtering.
    /// </summary>
    All,

    /// <summary>
    /// Exclude framework and Void assemblies. This is the default mode.
    /// </summary>
    ExcludeFramework,

    /// <summary>
    /// Use a custom assembly filter provided by the developer.
    /// </summary>
    Custom,

    /// <summary>
    /// Only scan assemblies explicitly added to the whitelist.
    /// </summary>
    Whitelist,

    /// <summary>
    /// Scan all assemblies except those explicitly added to the blacklist.
    /// </summary>
    Blacklist
}

/// <summary>
/// Provides reflection-based discovery for types marked with the <see cref="DiscoverableAttribute"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DiscoverableHelper"/> class locates and caches types from loaded assemblies
/// that are decorated with the <see cref="DiscoverableAttribute"/>. It supports filtering by
/// type, name, category, and priority, and automatically handles assembly loading events.
/// </para>
/// <para>
/// This system is used by the engine to discover mods, plugins, services, and other
/// dynamically loaded components without requiring manual registration.
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Find all discoverable types implementing or inheriting from T
/// var types = DiscoverableHelper.FindAll&lt;IMod&gt;();
/// 
/// // Find a specific type by name
/// var type = DiscoverableHelper.FindSingleByName&lt;IMod&gt;("MyMod");
/// 
/// // Find types by category
/// var types = DiscoverableHelper.FindManyByCategory&lt;IService&gt;("Network");
/// 
/// // Find by name and category
/// var types = DiscoverableHelper.FindManyByNameAndCategory&lt;ICommand&gt;("Save", "Game");
/// 
/// // Invalidate caches after loading new assemblies
/// DiscoverableHelper.InvalidateCaches();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe and uses concurrent collections and locks for
/// cache management.
/// </para>
/// </remarks>
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
    /// Retrieves all discoverable types that implement or inherit from the specified type.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <returns>A read-only list of discoverable types sorted by priority.</returns>
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
    /// Finds a single discoverable type by its internal name.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The internal name of the type to find.</param>
    /// <returns>The matching type, or null if no match or multiple matches found.</returns>
    public static Type FindSingleByName<T>(string name)
    {
        var matches = FindManyByName<T>(name);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Finds a single discoverable type by its internal name using an enum.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The enum representing the internal name.</param>
    /// <returns>The matching type, or null if no match or multiple matches found.</returns>
    public static Type FindSingleByName<T>(Enum name)
        => FindSingleByName<T>(name.ToEnumString());

    /// <summary>
    /// Finds a single discoverable type by its category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="category">The category of the type to find.</param>
    /// <returns>The matching type, or null if no match or multiple matches found.</returns>
    public static Type FindSingleByCategory<T>(string category)
    {
        var matches = FindManyByCategory<T>(category);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Finds a single discoverable type by its category using an enum.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="category">The enum representing the category.</param>
    /// <returns>The matching type, or null if no match or multiple matches found.</returns>
    public static Type FindSingleByCategory<T>(Enum category)
        => FindSingleByCategory<T>(category.ToEnumString());

    /// <summary>
    /// Finds a single discoverable type by its internal name and category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The internal name of the type to find.</param>
    /// <param name="category">The category of the type to find.</param>
    /// <returns>The matching type, or null if no match or multiple matches found.</returns>
    public static Type FindSingleByNameAndCategory<T>(string name, string category)
    {
        var matches = FindManyByNameAndCategory<T>(name, category);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Finds a single discoverable type by its internal name and category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The internal name of the type to find.</param>
    /// <param name="category">The enum representing the category.</param>
    /// <returns>The matching type, or null if no match or multiple matches found.</returns>
    public static Type FindSingleByNameAndCategory<T>(string name, Enum category)
    {
        var matches = FindManyByNameAndCategory<T>(name, category);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Finds a single discoverable type by its internal name and category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The enum representing the internal name.</param>
    /// <param name="category">The category of the type to find.</param>
    /// <returns>The matching type, or null if no match or multiple matches found.</returns>
    public static Type FindSingleByNameAndCategory<T>(Enum name, string category)
    {
        var matches = FindManyByNameAndCategory<T>(name, category);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Finds a single discoverable type by its internal name and category using enums.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The enum representing the internal name.</param>
    /// <param name="category">The enum representing the category.</param>
    /// <returns>The matching type, or null if no match or multiple matches found.</returns>
    public static Type FindSingleByNameAndCategory<T>(Enum name, Enum category)
    {
        var matches = FindManyByNameAndCategory<T>(name, category);
        return matches.Count == 1 ? matches[0] : null;
    }

    /// <summary>
    /// Attempts to find a single discoverable type by its internal name.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The internal name of the type to find.</param>
    /// <param name="type">When this method returns, contains the matching type, or null if not found.</param>
    /// <returns><see langword="true"/> if exactly one matching type was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFindSingleByName<T>(string name, out Type type)
    {
        type = FindSingleByName<T>(name);
        return type != null;
    }

    /// <summary>
    /// Attempts to find a single discoverable type by its internal name using an enum.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The enum representing the internal name.</param>
    /// <param name="type">When this method returns, contains the matching type, or null if not found.</param>
    /// <returns><see langword="true"/> if exactly one matching type was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFindSingleByName<T>(Enum name, out Type type)
    {
        type = FindSingleByName<T>(name);
        return type != null;
    }

    /// <summary>
    /// Attempts to find a single discoverable type by its category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="category">The category of the type to find.</param>
    /// <param name="type">When this method returns, contains the matching type, or null if not found.</param>
    /// <returns><see langword="true"/> if exactly one matching type was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFindSingleByCategory<T>(string category, out Type type)
    {
        type = FindSingleByCategory<T>(category);
        return type != null;
    }

    /// <summary>
    /// Attempts to find a single discoverable type by its category using an enum.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="category">The enum representing the category.</param>
    /// <param name="type">When this method returns, contains the matching type, or null if not found.</param>
    /// <returns><see langword="true"/> if exactly one matching type was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFindSingleByCategory<T>(Enum category, out Type type)
    {
        type = FindSingleByCategory<T>(category);
        return type != null;
    }

    /// <summary>
    /// Attempts to find a single discoverable type by its internal name and category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The internal name of the type to find.</param>
    /// <param name="category">The category of the type to find.</param>
    /// <param name="type">When this method returns, contains the matching type, or null if not found.</param>
    /// <returns><see langword="true"/> if exactly one matching type was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFindSingleByNameAndCategory<T>(string name, string category, out Type type)
    {
        type = FindSingleByNameAndCategory<T>(name, category);
        return type != null;
    }

    /// <summary>
    /// Attempts to find a single discoverable type by its internal name and category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The internal name of the type to find.</param>
    /// <param name="category">The enum representing the category.</param>
    /// <param name="type">When this method returns, contains the matching type, or null if not found.</param>
    /// <returns><see langword="true"/> if exactly one matching type was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFindSingleByNameAndCategory<T>(string name, Enum category, out Type type)
    {
        type = FindSingleByNameAndCategory<T>(name, category);
        return type != null;
    }

    /// <summary>
    /// Attempts to find a single discoverable type by its internal name and category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The enum representing the internal name.</param>
    /// <param name="category">The category of the type to find.</param>
    /// <param name="type">When this method returns, contains the matching type, or null if not found.</param>
    /// <returns><see langword="true"/> if exactly one matching type was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFindSingleByNameAndCategory<T>(Enum name, string category, out Type type)
    {
        type = FindSingleByNameAndCategory<T>(name, category);
        return type != null;
    }

    /// <summary>
    /// Attempts to find a single discoverable type by its internal name and category using enums.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The enum representing the internal name.</param>
    /// <param name="category">The enum representing the category.</param>
    /// <param name="type">When this method returns, contains the matching type, or null if not found.</param>
    /// <returns><see langword="true"/> if exactly one matching type was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFindSingleByNameAndCategory<T>(Enum name, Enum category, out Type type)
    {
        type = FindSingleByNameAndCategory<T>(name, category);
        return type != null;
    }

    /// <summary>
    /// Retrieves all discoverable types matching the specified internal name.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The internal name to match.</param>
    /// <returns>A read-only list of matching types sorted by priority.</returns>
    public static IReadOnlyList<Type> FindManyByName<T>(string name) =>
        Filter<T>()
            .Where(x => string.Equals(x.Meta.InternalName, name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Meta.Priority)
            .Select(x => x.Type)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Retrieves all discoverable types matching the specified internal name using an enum.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The enum representing the internal name.</param>
    /// <returns>A read-only list of matching types sorted by priority.</returns>
    public static IReadOnlyList<Type> FindManyByName<T>(Enum name) =>
        FindManyByName<T>(name.ToEnumString());

    /// <summary>
    /// Retrieves all discoverable types within the specified category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="category">The category to match.</param>
    /// <returns>A read-only list of matching types sorted by priority.</returns>
    public static IReadOnlyList<Type> FindManyByCategory<T>(string category) =>
        Filter<T>()
            .Where(x => string.Equals(x.Meta.InternalCategory, category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Meta.Priority)
            .Select(x => x.Type)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Retrieves all discoverable types within the specified category using an enum.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="category">The enum representing the category.</param>
    /// <returns>A read-only list of matching types sorted by priority.</returns>
    public static IReadOnlyList<Type> FindManyByCategory<T>(Enum category) =>
        FindManyByCategory<T>(category.ToEnumString());

    /// <summary>
    /// Retrieves all discoverable types matching the specified internal name and category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The internal name to match.</param>
    /// <param name="category">The category to match.</param>
    /// <returns>A read-only list of matching types sorted by priority.</returns>
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
    /// Retrieves all discoverable types matching the specified internal name and category using enums.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The enum representing the internal name.</param>
    /// <param name="category">The enum representing the category.</param>
    /// <returns>A read-only list of matching types sorted by priority.</returns>
    public static IReadOnlyList<Type> FindManyByNameAndCategory<T>(Enum name, Enum category) =>
        FindManyByNameAndCategory<T>(name.ToEnumString(), category.ToEnumString());

    /// <summary>
    /// Retrieves all discoverable types matching the specified internal name and category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The internal name to match.</param>
    /// <param name="category">The enum representing the category.</param>
    /// <returns>A read-only list of matching types sorted by priority.</returns>
    public static IReadOnlyList<Type> FindManyByNameAndCategory<T>(string name, Enum category) =>
        FindManyByNameAndCategory<T>(name, category.ToEnumString());

    /// <summary>
    /// Retrieves all discoverable types matching the specified internal name and category.
    /// </summary>
    /// <typeparam name="T">The base type or interface to filter by.</typeparam>
    /// <param name="name">The enum representing the internal name.</param>
    /// <param name="category">The category to match.</param>
    /// <returns>A read-only list of matching types sorted by priority.</returns>
    public static IReadOnlyList<Type> FindManyByNameAndCategory<T>(Enum name, string category) =>
        FindManyByNameAndCategory<T>(name.ToEnumString(), category);

    private static DiscoverableAttribute GetMeta(Type t) =>
        t.GetCustomAttribute<DiscoverableAttribute>(inherit: false);

    /// <summary>
    /// Invalidates all internal caches. Call this when assemblies are loaded or unloaded
    /// at runtime, such as during hot-reload or dynamic mod loading.
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
/// Marks a class as discoverable by the engine's reflection-based discovery system.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DiscoverableAttribute"/> is used to identify types that should be
/// automatically found and registered by the engine for various purposes including:
/// <list type="bullet">
///   <item><description>Mod and plugin loading</description></item>
///   <item><description>Service registration</description></item>
///   <item><description>Editor tools and debug UI</description></item>
///   <item><description>Runtime registration systems</description></item>
///   <item><description>Scripting and reflection-based systems</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// [Discoverable(Name = "MyMod", Category = "Gameplay", Priority = 10)]
/// public class MyMod : IMod
/// {
///     // Implementation
/// }
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class DiscoverableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the display name or identifier for this discoverable item.
    /// </summary>
    public object Name { get; set; }

    /// <summary>
    /// Gets or sets the category under which this discoverable item should be grouped.
    /// </summary>
    public object Category { get; set; }

    /// <summary>
    /// Gets or sets the ordering priority for this discoverable item within its category.
    /// Lower numbers indicate higher priority.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets whether this discoverable item is enabled and should be included in searches.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets developer-defined metadata associated with this discoverable item.
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
    /// <typeparam name="T">The type to cast the metadata to.</typeparam>
    /// <returns>The metadata cast to type T, or default if not assignable.</returns>
    public T MetadataAs<T>() => Metadata is T typed ? typed : default;

    /// <summary>
    /// Returns the metadata cast to the specified type, or the provided default if not assignable.
    /// </summary>
    /// <typeparam name="T">The type to cast the metadata to.</typeparam>
    /// <param name="defaultValue">The default value to return if the metadata is not of type T.</param>
    /// <returns>The metadata cast to type T, or the provided default.</returns>
    public T MetadataAs<T>(T defaultValue) => Metadata is T typed ? typed : defaultValue;

    /// <summary>
    /// Attempts to cast the metadata to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to cast the metadata to.</typeparam>
    /// <param name="result">When this method returns, contains the metadata cast to type T, or default if unsuccessful.</param>
    /// <returns><see langword="true"/> if the metadata is of type T; otherwise, <see langword="false"/>.</returns>
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
    /// <typeparam name="T">The type to cast the name to.</typeparam>
    /// <returns>The name cast to type T, or default if not assignable.</returns>
    public T NameAs<T>() => Name is T typed ? typed : default;

    /// <summary>
    /// Returns the category cast to the specified type, or default if not assignable.
    /// </summary>
    /// <typeparam name="T">The type to cast the category to.</typeparam>
    /// <returns>The category cast to type T, or default if not assignable.</returns>
    public T CategoryAs<T>() => Category is T typed ? typed : default;
}