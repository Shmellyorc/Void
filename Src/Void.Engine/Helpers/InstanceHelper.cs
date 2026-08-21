// ============================================================================
//  InstanceHelper.cs
// ============================================================================
//  Provides reflection-based object creation and type discovery across
//  all game assemblies with caching and failure tracking.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Helpers;

/// <summary>
/// Provides reflection-based object creation and type discovery across
/// all game assemblies with caching and failure tracking.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="InstanceHelper"/> class provides methods for dynamically
/// creating instances of types by name or type reference across all loaded
/// game assemblies. It caches successful lookups and tracks failed lookups
/// to improve performance.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Type discovery by name across all game assemblies</description></item>
///   <item><description>Instance creation with constructor arguments</description></item>
///   <item><description>Caching of successful type lookups</description></item>
///   <item><description>Tracking of failed lookups to avoid repeated searches</description></item>
///   <item><description>Assembly refresh for dynamic mod loading</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create an instance by name
/// var instance = InstanceHelper.CreateInstance&lt;IMod&gt;("MyMod", true, null);
/// 
/// // Create an instance by name with constructor arguments
/// var instance = InstanceHelper.CreateInstance&lt;IService&gt;("NetworkService", true, new object[] { "config.json" });
/// 
/// // Try create with out parameter
/// if (InstanceHelper.TryCreateInstance&lt;IMod&gt;("MyMod", true, null, out var mod))
/// {
///     mod.Initialize();
/// }
/// 
/// // Create from type reference
/// var instance = InstanceHelper.CreateInstanceFromType&lt;IMod&gt;(type, null);
/// 
/// // Refresh assemblies after loading new mods
/// InstanceHelper.RefreshAssemblies();
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is thread-safe and uses concurrent collections and
/// reader-writer locks for assembly access.
/// </para>
/// </remarks>
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

    /// <summary>
    /// Refreshes the list of game assemblies and clears all caches.
    /// Call this after loading or unloading assemblies at runtime.
    /// </summary>
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

    /// <summary>
    /// Attempts to create an instance of the specified type by name.
    /// </summary>
    /// <typeparam name="T">The base type or interface the instance must implement.</typeparam>
    /// <param name="name">The name of the type to create.</param>
    /// <param name="ignoreCase">Whether to ignore case when matching type names.</param>
    /// <param name="args">Constructor arguments, or null for parameterless constructor.</param>
    /// <param name="instance">When this method returns, contains the created instance, or null if creation failed.</param>
    /// <returns><see langword="true"/> if the instance was created successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreateInstance<T>(string name, bool ignoreCase, object[] args, out T instance) where T : class
    {
        instance = CreateInstance<T>(name, ignoreCase, args);
        return instance != null;
    }

    /// <summary>
    /// Creates an instance of the specified type by name.
    /// </summary>
    /// <typeparam name="T">The base type or interface the instance must implement.</typeparam>
    /// <param name="name">The name of the type to create.</param>
    /// <param name="ignoreCase">Whether to ignore case when matching type names.</param>
    /// <param name="args">Constructor arguments, or null for parameterless constructor.</param>
    /// <returns>The created instance, or null if creation failed.</returns>
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

    /// <summary>
    /// Creates an instance of the specified type from a type reference.
    /// </summary>
    /// <typeparam name="T">The base type or interface the instance must implement.</typeparam>
    /// <param name="type">The type to instantiate.</param>
    /// <param name="args">Constructor arguments, or null for parameterless constructor.</param>
    /// <returns>The created instance, or null if creation failed.</returns>
    public static T CreateInstanceFromType<T>(Type type, object[] args) where T : class
    {
        if (type == null)
            return null!;

        if (!typeof(T).IsAssignableFrom(type))
            return null!;

        try
        {
            return Activator.CreateInstance(type, args ?? Array.Empty<object>()) as T;
        }
        catch
        {
            return null!;
        }
    }

    /// <summary>
    /// Attempts to create an instance of the specified type from a type reference.
    /// </summary>
    /// <typeparam name="T">The base type or interface the instance must implement.</typeparam>
    /// <param name="type">The type to instantiate.</param>
    /// <param name="args">Constructor arguments, or null for parameterless constructor.</param>
    /// <param name="instance">When this method returns, contains the created instance, or null if creation failed.</param>
    /// <returns><see langword="true"/> if the instance was created successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreateInstanceFromType<T>(Type type, object[] args, out T instance) where T : class
    {
        instance = CreateInstanceFromType<T>(type, args);
        return instance != null;
    }

    /// <summary>
    /// Creates an instance of the same type as the provided object.
    /// </summary>
    /// <typeparam name="T">The base type or interface the instance must implement.</typeparam>
    /// <param name="obj">The object whose type to instantiate.</param>
    /// <param name="args">Constructor arguments, or null for parameterless constructor.</param>
    /// <returns>The created instance, or null if creation failed.</returns>
    public static T CreateInstanceFromObject<T>(object obj, object[] args) where T : class
    {
        if (obj == null)
            return null!;

        return CreateInstance<T>(obj.GetType().Name, true, args);
    }

    /// <summary>
    /// Attempts to create an instance of the same type as the provided object.
    /// </summary>
    /// <typeparam name="T">The base type or interface the instance must implement.</typeparam>
    /// <param name="obj">The object whose type to instantiate.</param>
    /// <param name="args">Constructor arguments, or null for parameterless constructor.</param>
    /// <param name="instance">When this method returns, contains the created instance, or null if creation failed.</param>
    /// <returns><see langword="true"/> if the instance was created successfully; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreateInstanceFromObject<T>(object obj, object[] args, out T instance) where T : class
    {
        instance = CreateInstanceFromObject<T>(obj, args);
        return instance != null;
    }
}