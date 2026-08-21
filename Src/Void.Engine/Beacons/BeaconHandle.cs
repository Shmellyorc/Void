// ============================================================================
//  BeaconHandle.cs
// ============================================================================
//  A handle containing data for a published beacon event. Provides type-safe
//  access to the event data payload.
//
//  Copyright (c) 2025 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Beacons;

/// <summary>
/// A handle containing data for a published beacon event.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="BeaconHandle"/> struct provides type-safe access to the
/// data payload of a beacon event. It is passed to subscribers when a
/// beacon is published.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Contains the event topic/identifier</description></item>
///   <item><description>Holds the event data payload as an object array</description></item>
///   <item><description>Provides type-safe access via <see cref="Get{TData}"/> and <see cref="TryGet{TData}"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Publishing a beacon with data
/// BeaconManager.Instance.Publish(GameBeacons.PlayerMoved, player, position);
/// 
/// // Subscribing and handling
/// BeaconManager.Instance.Subscribe(GameBeacons.PlayerMoved, OnPlayerMoved);
/// 
/// private void OnPlayerMoved(BeaconHandle handle)
/// {
///     var player = handle.Get&lt;Player&gt;(0);
///     var position = handle.Get&lt;Vect2&gt;(1);
///     // ... handle event ...
/// }
/// 
/// // Safe access with TryGet
/// if (handle.TryGet&lt;Player&gt;(0, out var player))
/// {
///     // player is valid
/// }
/// </code>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This struct is immutable and thread-safe for reading.
/// </para>
/// </remarks>
public readonly struct BeaconHandle
{
    /// <summary>
    /// Gets the topic/identifier of the beacon event.
    /// </summary>
    /// <value>The event topic string (e.g., "PlayerMoved", "UpdateFood").</value>
    public string Topic { get; }

    /// <summary>
    /// Gets the data payload of the beacon event.
    /// </summary>
    /// <value>An array of objects containing the event data.</value>
    /// <remarks>
    /// The data array contains the parameters passed when the beacon was
    /// published. Index 0 is the first parameter, index 1 is the second, etc.
    /// </remarks>
    public object[] Data { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BeaconHandle"/> struct.
    /// </summary>
    /// <param name="topic">The topic/identifier of the beacon event.</param>
    /// <param name="data">The data payload of the beacon event.</param>
    internal BeaconHandle(string topic, object[] data)
    {
        Topic = topic;
        Data = data;
    }

    /// <summary>
    /// Gets data of the specified type from the beacon handle.
    /// </summary>
    /// <typeparam name="TData">The type of data to retrieve.</typeparam>
    /// <param name="index">The index of the data item to retrieve.</param>
    /// <returns>
    /// The data at the specified index cast to <typeparamref name="TData"/>,
    /// or <see langword="default"/> if the index is out of range or the
    /// data is not of the specified type.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method performs a type check before returning the data.
    /// If the data is not of the specified type, <see langword="default"/> is returned.
    /// </para>
    /// <para>
    /// For safe access that checks validity, use <see cref="TryGet{TData}"/> instead.
    /// </para>
    /// </remarks>
    public TData Get<TData>(int index)
    {
        if (index < 0 || index >= Data.Length)
            return default;
        if (Data[index] is not TData)
            return default;

        return (TData)Data[index];
    }

    /// <summary>
    /// Attempts to get data of the specified type from the beacon handle.
    /// </summary>
    /// <typeparam name="TData">The type of data to retrieve.</typeparam>
    /// <param name="index">The index of the data item to retrieve.</param>
    /// <param name="data">
    /// When this method returns, contains the data at the specified index
    /// cast to <typeparamref name="TData"/> if successful; otherwise, <see langword="default"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the data at the specified index is of the
    /// specified type; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method provides safe access to the beacon data without throwing
    /// exceptions. It returns <see langword="true"/> only if the index is valid
    /// and the data is of the specified type.
    /// </para>
    /// <para>
    /// <b>Usage Example:</b>
    /// <code>
    /// if (handle.TryGet&lt;Player&gt;(0, out var player))
    /// {
    ///     // player is valid and can be used safely
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public bool TryGet<TData>(int index, out TData data)
    {
        data = Get<TData>(index);

        return data is TData;
    }
}