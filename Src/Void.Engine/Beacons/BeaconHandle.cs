namespace Void.Engine.Beacons;

public readonly struct BeaconHandle
{
    public string Topic { get; }
    public object[] Data { get; }

    internal BeaconHandle(string topic, object[] data)
    {
        Topic = topic;
        Data = data;
    }

    public TData Get<TData>(int index)
    {
        if (index < 0 || index >= Data.Length)
            return default;
        if (Data[index] is not TData)
            return default;

        return (TData)Data[index];
    }

    public bool TryGet<TData>(int index, out TData data)
    {
        data = Get<TData>(index);

        return data is TData;
    }
}
