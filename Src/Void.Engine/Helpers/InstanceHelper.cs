// ============================================================================
//  InstanceHelper.cs
// ============================================================================
//  Reflection-based object creation and type discovery across game assemblies.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Void.Engine.Helpers;

/// <summary>
/// Provides reflection-based type lookup and instance creation across loaded
/// non-framework assemblies.
/// </summary>
/// <remarks>
/// <para>
/// Name lookups are cached per requested base type, requested name, and case
/// comparison policy. Failed lookups are cached separately until
/// <see cref="RefreshAssemblies"/> is called.
/// </para>
/// <para>
/// Call <see cref="RefreshAssemblies"/> after dynamically loading or unloading
/// game or mod assemblies so subsequent name lookups see the new assembly set.
/// </para>
/// <code>
/// IMod mod = InstanceHelper.CreateInstance&lt;IMod&gt;("MyMod", true, null);
/// if (mod is not null)
/// {
///     // Use the dynamically created mod.
/// }
/// </code>
/// </remarks>
public static class InstanceHelper
{
    private static readonly List<Assembly> GameAssemblies = [];
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> FailedLookups = new(StringComparer.Ordinal);
    private static readonly ReaderWriterLockSlim AssemblyLock = new();

    static InstanceHelper()
    {
        LoadAssemblies();
    }

    /// <summary>
    /// Refreshes the loaded game-assembly snapshot and clears successful and failed lookup caches.
    /// </summary>
    /// <remarks>
    /// Call this after dynamically loading or unloading assemblies that should participate in
    /// reflection-based instance creation.
    /// </remarks>
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

    /// <summary>Attempts to create an instance of a matching type by name.</summary>
    /// <typeparam name="T">Required base class or interface.</typeparam>
    /// <param name="name">Full type name or simple type name to locate.</param>
    /// <param name="ignoreCase">Whether name matching ignores character case.</param>
    /// <param name="args">Constructor arguments, or <see langword="null"/> for a parameterless constructor.</param>
    /// <param name="instance">Receives the created instance when successful.</param>
    /// <returns><see langword="true"/> when an instance was created; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreateInstance<T>(string name, bool ignoreCase, object[] args, out T instance) where T : class
    {
        instance = CreateInstance<T>(name, ignoreCase, args);
        return instance != null;
    }

    /// <summary>Creates an instance of a matching type by name.</summary>
    /// <typeparam name="T">Required base class or interface.</typeparam>
    /// <param name="name">Full type name or simple type name to locate.</param>
    /// <param name="ignoreCase">Whether name matching ignores character case.</param>
    /// <param name="args">Constructor arguments, or <see langword="null"/> for a parameterless constructor.</param>
    /// <returns>The created instance, or <see langword="null"/> when lookup or construction fails.</returns>
    public static T CreateInstance<T>(string name, bool ignoreCase, object[] args) where T : class
    {
        if (string.IsNullOrEmpty(name))
            return null!;

        string cacheKey = CreateCacheKey<T>(name, ignoreCase);

        if (FailedLookups.ContainsKey(cacheKey))
            return null!;

        if (!TypeCache.TryGetValue(cacheKey, out Type type))
        {
            StringComparison comparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            type = FindType<T>(name, comparison);
            if (type == null)
            {
                FailedLookups.TryAdd(cacheKey, 0);
                return null!;
            }

            TypeCache.TryAdd(cacheKey, type);
        }

        return CreateInstanceFromType<T>(type, args);
    }

    /// <summary>Creates an instance from an exact runtime type.</summary>
    /// <typeparam name="T">Required base class or interface.</typeparam>
    /// <param name="type">Runtime type to instantiate.</param>
    /// <param name="args">Constructor arguments, or <see langword="null"/> for a parameterless constructor.</param>
    /// <returns>The created instance, or <see langword="null"/> when the type is incompatible or construction fails.</returns>
    public static T CreateInstanceFromType<T>(Type type, object[] args) where T : class
    {
        if (type == null || !typeof(T).IsAssignableFrom(type))
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

    /// <summary>Attempts to create an instance from an exact runtime type.</summary>
    /// <typeparam name="T">Required base class or interface.</typeparam>
    /// <param name="type">Runtime type to instantiate.</param>
    /// <param name="args">Constructor arguments, or <see langword="null"/> for a parameterless constructor.</param>
    /// <param name="instance">Receives the created instance when successful.</param>
    /// <returns><see langword="true"/> when an instance was created; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreateInstanceFromType<T>(Type type, object[] args, out T instance) where T : class
    {
        instance = CreateInstanceFromType<T>(type, args);
        return instance != null;
    }

    /// <summary>Creates a new instance of the exact runtime type of an existing object.</summary>
    /// <typeparam name="T">Required base class or interface.</typeparam>
    /// <param name="obj">Object whose runtime type should be instantiated.</param>
    /// <param name="args">Constructor arguments, or <see langword="null"/> for a parameterless constructor.</param>
    /// <returns>The created instance, or <see langword="null"/> when the object is null, incompatible, or construction fails.</returns>
    public static T CreateInstanceFromObject<T>(object obj, object[] args) where T : class
    {
        if (obj == null)
            return null!;

        return CreateInstanceFromType<T>(obj.GetType(), args);
    }

    /// <summary>Attempts to create a new instance of the exact runtime type of an existing object.</summary>
    /// <typeparam name="T">Required base class or interface.</typeparam>
    /// <param name="obj">Object whose runtime type should be instantiated.</param>
    /// <param name="args">Constructor arguments, or <see langword="null"/> for a parameterless constructor.</param>
    /// <param name="instance">Receives the created instance when successful.</param>
    /// <returns><see langword="true"/> when an instance was created; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreateInstanceFromObject<T>(object obj, object[] args, out T instance) where T : class
    {
        instance = CreateInstanceFromObject<T>(obj, args);
        return instance != null;
    }

    private static string CreateCacheKey<T>(string name, bool ignoreCase)
    {
        string lookupName = ignoreCase ? name.ToUpperInvariant() : name;
        return $"{typeof(T).AssemblyQualifiedName}|{(ignoreCase ? 'I' : 'C')}|{lookupName}";
    }

    private static void LoadAssemblies()
    {
        GameAssemblies.Clear();

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string name = assembly.GetName().Name;
            if (name == null || assembly.IsDynamic)
                continue;

            if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("netstandard", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            GameAssemblies.Add(assembly);
        }
    }

    private static Type FindType<T>(string name, StringComparison comparison)
    {
        bool ignoreCase = comparison == StringComparison.OrdinalIgnoreCase;

        AssemblyLock.EnterReadLock();
        try
        {
            foreach (Assembly assembly in GameAssemblies)
            {
                try
                {
                    Type type = assembly.GetType(name, throwOnError: false, ignoreCase);
                    if (type != null && typeof(T).IsAssignableFrom(type))
                        return type;
                }
                catch
                {
                    // One problematic assembly should not prevent lookup in the remaining assemblies.
                }
            }

            foreach (Assembly assembly in GameAssemblies)
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (type.Name.Equals(name, comparison) && typeof(T).IsAssignableFrom(type))
                        return type;
                }
            }

            return null;
        }
        finally
        {
            AssemblyLock.ExitReadLock();
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types?.Where(static type => type != null).Cast<Type>() ?? Array.Empty<Type>();
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }
}
