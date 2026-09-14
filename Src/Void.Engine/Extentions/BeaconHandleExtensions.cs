// ============================================================================
//  BeaconHandleExtensions.cs
// ============================================================================
//  Typed payload and topic helpers for BeaconHandle.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Beacons;

/// <summary>
/// Provides typed convenience helpers for reading <see cref="BeaconHandle"/>
/// payloads without changing the handle's small immutable core API.
/// </summary>
/// <remarks>
/// Positional payload helpers access known indexes directly. They do not scan or
/// enumerate the payload collection.
/// </remarks>
public static class BeaconHandleExtensions
{
    /// <summary>
    /// Gets the first payload item when it matches the requested type.
    /// </summary>
    /// <typeparam name="TData">The expected payload type.</typeparam>
    /// <param name="handle">The beacon handle to read.</param>
    /// <returns>
    /// The first typed payload item when available; otherwise, the default value
    /// of <typeparamref name="TData"/>.
    /// </returns>
    public static TData Get<TData>(this BeaconHandle handle)
        => handle.Get<TData>(0);

    /// <summary>
    /// Attempts to get the first payload item as the requested type.
    /// </summary>
    /// <typeparam name="T1">The expected type of payload item zero.</typeparam>
    /// <param name="handle">The beacon handle to read.</param>
    /// <param name="first">Receives payload item zero when it matches <typeparamref name="T1"/>.</param>
    /// <returns><see langword="true"/> when payload item zero matches the requested type.</returns>
    public static bool TryGet<T1>(this BeaconHandle handle, out T1 first)
        => handle.TryGet(0, out first);

    /// <summary>
    /// Attempts to get the first two payload items as the requested types.
    /// </summary>
    /// <typeparam name="T1">The expected type of payload item zero.</typeparam>
    /// <typeparam name="T2">The expected type of payload item one.</typeparam>
    /// <param name="handle">The beacon handle to read.</param>
    /// <param name="first">Receives payload item zero.</param>
    /// <param name="second">Receives payload item one.</param>
    /// <returns><see langword="true"/> when both payload items exist and match their requested types.</returns>
    public static bool TryGet<T1, T2>(
        this BeaconHandle handle,
        out T1 first,
        out T2 second)
    {
        bool firstValid = handle.TryGet(0, out first);
        bool secondValid = handle.TryGet(1, out second);
        return firstValid && secondValid;
    }

    /// <summary>
    /// Attempts to get the first three payload items as the requested types.
    /// </summary>
    /// <typeparam name="T1">The expected type of payload item zero.</typeparam>
    /// <typeparam name="T2">The expected type of payload item one.</typeparam>
    /// <typeparam name="T3">The expected type of payload item two.</typeparam>
    /// <param name="handle">The beacon handle to read.</param>
    /// <param name="first">Receives payload item zero.</param>
    /// <param name="second">Receives payload item one.</param>
    /// <param name="third">Receives payload item two.</param>
    /// <returns><see langword="true"/> when all payload items exist and match their requested types.</returns>
    public static bool TryGet<T1, T2, T3>(
        this BeaconHandle handle,
        out T1 first,
        out T2 second,
        out T3 third)
    {
        bool firstValid = handle.TryGet(0, out first);
        bool secondValid = handle.TryGet(1, out second);
        bool thirdValid = handle.TryGet(2, out third);
        return firstValid && secondValid && thirdValid;
    }

    /// <summary>
    /// Attempts to get the first four payload items as the requested types.
    /// </summary>
    /// <typeparam name="T1">The expected type of payload item zero.</typeparam>
    /// <typeparam name="T2">The expected type of payload item one.</typeparam>
    /// <typeparam name="T3">The expected type of payload item two.</typeparam>
    /// <typeparam name="T4">The expected type of payload item three.</typeparam>
    /// <param name="handle">The beacon handle to read.</param>
    /// <param name="first">Receives payload item zero.</param>
    /// <param name="second">Receives payload item one.</param>
    /// <param name="third">Receives payload item two.</param>
    /// <param name="fourth">Receives payload item three.</param>
    /// <returns><see langword="true"/> when all payload items exist and match their requested types.</returns>
    public static bool TryGet<T1, T2, T3, T4>(
        this BeaconHandle handle,
        out T1 first,
        out T2 second,
        out T3 third,
        out T4 fourth)
    {
        bool firstValid = handle.TryGet(0, out first);
        bool secondValid = handle.TryGet(1, out second);
        bool thirdValid = handle.TryGet(2, out third);
        bool fourthValid = handle.TryGet(3, out fourth);
        return firstValid && secondValid && thirdValid && fourthValid;
    }

    /// <summary>
    /// Attempts to get the first five payload items as the requested types.
    /// </summary>
    /// <typeparam name="T1">The expected type of payload item zero.</typeparam>
    /// <typeparam name="T2">The expected type of payload item one.</typeparam>
    /// <typeparam name="T3">The expected type of payload item two.</typeparam>
    /// <typeparam name="T4">The expected type of payload item three.</typeparam>
    /// <typeparam name="T5">The expected type of payload item four.</typeparam>
    /// <param name="handle">The beacon handle to read.</param>
    /// <param name="first">Receives payload item zero.</param>
    /// <param name="second">Receives payload item one.</param>
    /// <param name="third">Receives payload item two.</param>
    /// <param name="fourth">Receives payload item three.</param>
    /// <param name="fifth">Receives payload item four.</param>
    /// <returns><see langword="true"/> when all payload items exist and match their requested types.</returns>
    public static bool TryGet<T1, T2, T3, T4, T5>(
        this BeaconHandle handle,
        out T1 first,
        out T2 second,
        out T3 third,
        out T4 fourth,
        out T5 fifth)
    {
        bool firstValid = handle.TryGet(0, out first);
        bool secondValid = handle.TryGet(1, out second);
        bool thirdValid = handle.TryGet(2, out third);
        bool fourthValid = handle.TryGet(3, out fourth);
        bool fifthValid = handle.TryGet(4, out fifth);
        
        return firstValid && secondValid && thirdValid && fourthValid && fifthValid;
    }

    /// <summary>
    /// Gets the first payload item when it matches the requested type, or returns a fallback value.
    /// </summary>
    /// <typeparam name="TData">The expected payload type.</typeparam>
    /// <param name="handle">The beacon handle to read.</param>
    /// <param name="fallback">Value returned when the first payload item is missing or has another type.</param>
    /// <returns>The typed payload value when available; otherwise, <paramref name="fallback"/>.</returns>
    public static TData GetOr<TData>(this BeaconHandle handle, TData fallback)
        => handle.TryGet(0, out TData value) ? value : fallback;

    /// <summary>
    /// Gets a payload item when it matches the requested type, or returns a fallback value.
    /// </summary>
    /// <typeparam name="TData">The expected payload type.</typeparam>
    /// <param name="handle">The beacon handle to read.</param>
    /// <param name="index">The zero-based payload index.</param>
    /// <param name="fallback">Value returned when the item is missing or has another type.</param>
    /// <returns>The typed payload value when available; otherwise, <paramref name="fallback"/>.</returns>
    public static TData GetOr<TData>(this BeaconHandle handle, int index, TData fallback)
        => handle.TryGet(index, out TData value) ? value : fallback;

    /// <summary>
    /// Gets whether the first payload item exists and matches the requested type.
    /// </summary>
    /// <typeparam name="TData">The payload type to test.</typeparam>
    /// <param name="handle">The beacon handle to inspect.</param>
    /// <returns><see langword="true"/> when payload item zero matches <typeparamref name="TData"/>.</returns>
    public static bool Has<TData>(this BeaconHandle handle)
        => handle.TryGet<TData>(0, out _);

    /// <summary>
    /// Gets whether a payload item exists at the requested index and matches the requested type.
    /// </summary>
    /// <typeparam name="TData">The payload type to test.</typeparam>
    /// <param name="handle">The beacon handle to inspect.</param>
    /// <param name="index">The zero-based payload index.</param>
    /// <returns><see langword="true"/> when the indexed payload item matches <typeparamref name="TData"/>.</returns>
    public static bool Has<TData>(this BeaconHandle handle, int index)
        => handle.TryGet<TData>(index, out _);

    /// <summary>
    /// Gets whether the handle was published for the specified string topic.
    /// </summary>
    /// <param name="handle">The beacon handle to inspect.</param>
    /// <param name="topic">The topic to compare.</param>
    /// <returns><see langword="true"/> when the topics match exactly; otherwise, <see langword="false"/>.</returns>
    public static bool IsTopic(this BeaconHandle handle, string topic)
        => topic != null && string.Equals(handle.Topic, topic, StringComparison.Ordinal);

    /// <summary>
    /// Gets whether the handle was published for the specified enum topic.
    /// </summary>
    /// <param name="handle">The beacon handle to inspect.</param>
    /// <param name="topic">The enum value used as the topic.</param>
    /// <returns><see langword="true"/> when the topics match exactly; otherwise, <see langword="false"/>.</returns>
    public static bool IsTopic(this BeaconHandle handle, Enum topic)
        => topic != null && handle.IsTopic(topic.ToEnumString());

    /// <summary>
    /// Gets whether the handle contains at least one payload item.
    /// </summary>
    /// <param name="handle">The beacon handle to inspect.</param>
    /// <returns><see langword="true"/> when one or more payload items are present.</returns>
    public static bool HasData(this BeaconHandle handle)
        => handle.Count > 0;

    /// <summary>
    /// Gets whether the handle contains no payload items.
    /// </summary>
    /// <param name="handle">The beacon handle to inspect.</param>
    /// <returns><see langword="true"/> when the payload is empty.</returns>
    public static bool IsEmpty(this BeaconHandle handle)
        => handle.Count == 0;
}
