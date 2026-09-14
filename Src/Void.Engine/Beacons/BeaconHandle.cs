// ============================================================================
//  BeaconHandle.cs
// ============================================================================
//  Payload delivered to subscribers when a beacon is published.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Beacons;

/// <summary>
/// Represents a beacon topic and the data published with it.
/// </summary>
/// <remarks>
/// A handle is created by <see cref="BeaconManager"/> for each published topic
/// that has subscribers. Payload items are stored in the same order they were
/// supplied to <see cref="BeaconManager.Publish(string, object[])"/>.
/// </remarks>
public readonly struct BeaconHandle
{
    private readonly object[] _data;

    /// <summary>
    /// Gets the topic that was published.
    /// </summary>
    public string Topic { get; }

    /// <summary>
    /// Gets a read-only view of the published payload items.
    /// </summary>
    /// <remarks>
    /// Index zero contains the first item supplied when the beacon was published.
    /// The payload cannot be replaced or reordered through this handle.
    /// </remarks>
    public ReadOnlySpan<object> Data => _data ?? Array.Empty<object>();

    /// <summary>
    /// Gets the number of payload items carried by this beacon.
    /// </summary>
    public int Count => _data?.Length ?? 0;

    internal BeaconHandle(string topic, object[] data)
    {
        Topic = topic;
        _data = data ?? Array.Empty<object>();
    }

    /// <summary>
    /// Gets a payload item when the index is valid and the item matches the requested type.
    /// </summary>
    /// <typeparam name="TData">The expected payload type.</typeparam>
    /// <param name="index">The zero-based payload index.</param>
    /// <returns>
    /// The typed payload item when available; otherwise, the default value of
    /// <typeparamref name="TData"/>.
    /// </returns>
    public TData Get<TData>(int index)
    {
        var data = _data;

        if (data == null || (uint)index >= (uint)data.Length)
            return default;

        return data[index] is TData typed
            ? typed
            : default;
    }

    /// <summary>
    /// Attempts to get a payload item of the requested type.
    /// </summary>
    /// <typeparam name="TData">The expected payload type.</typeparam>
    /// <param name="index">The zero-based payload index.</param>
    /// <param name="data">
    /// When this method returns, contains the typed payload item when found;
    /// otherwise, the default value of <typeparamref name="TData"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the index is valid and the item matches
    /// <typeparamref name="TData"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGet<TData>(int index, out TData data)
    {
        var payload = _data;

        if (payload == null || (uint)index >= (uint)payload.Length)
        {
            data = default;
            return false;
        }

        if (payload[index] is not TData typed)
        {
            data = default;
            return false;
        }

        data = typed;
        return true;
    }
}
