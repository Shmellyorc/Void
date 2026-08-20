namespace Void.Engine.Systems;

public sealed class FastRandom
{
    private const int Y = 0x2B5B9F51;
    private const int Z = 0x4F59A821;
    private const int W = 0x6F5B9D5B;

    private uint _x, _y, _z, _w;

    private static readonly ThreadLocal<FastRandom> _threadLocal = new(() => new FastRandom());

    public static FastRandom Shared => _threadLocal.Value;

    public FastRandom(int seed)
    {
        var s = (uint)seed;

        _x = s;
        _y = Y;
        _z = Z;
        _w = W;

        for (int i = 0; i < 10; i++)
            NextUInt();
    }
    public FastRandom() : this(GenerateSeed()) { }


    public bool NextBoolean()
    {
        return (NextUInt() & 1) == 0;
    }



    public int Next()
    {
        var rtn = NextUInt() & 0x7FFFFFFF;
        // Match the case where the value is int.MaxValue to match system.Random behavior
        if (rtn == 0x7FFFFFFF)
            return Next();
        return (int)rtn;
    }

    public int Next(int maxValue)
    {
        if (maxValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue nust be greater than zero");

        return (int)((NextUInt() & 0x7FFFFFFF) % maxValue);
    }

    public int Next(int minValue, int maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue be be greater or equal to minValue");
        var range = (long)maxValue - minValue;
        if (range <= 0)
            return minValue;
        return (int)((NextUInt() & 0x7FFFFFFF) % range) + minValue;
    }



    public double NextDouble()
    {
        return (NextUInt() & 0x7FFFFFFF) / (double)0x7FFFFFFF;
    }

    public double NextDouble(double maxValue)
    {
        if (maxValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than zero");
        return NextDouble() * maxValue;
    }

    public double NextDouble(double minValue, double maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater or equal to minValue");
        var range = maxValue - minValue;
        if (range <= 0)
            return maxValue;
        return NextDouble() * range + minValue;
    }



    public float NextFloat()
    {
        return (float)((NextUInt() & 0x7FFFFFFF) / (double)0x7FFFFFFF);
    }

    public float NextFloat(float maxValue)
    {
        if (maxValue <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater than zero");

        return NextFloat() * maxValue;
    }

    public float NextFloat(float minValue, float maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue must be greater or equal to minValue");
        var range = maxValue - minValue;
        if (range <= 0)
            return minValue;
        return NextFloat() * range + minValue;
    }



    public int RangeInt(int min, int max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be greater or equal to min");
        if (min == max)
            return min;

        // To avoid overflow when max == int.MaxValue
        return (int)((NextUInt() & 0x7FFFFFFF) % ((long)max - min + 1)) + min;
    }

    public float RangeFloat(float min, float max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be greater or equal to min");
        if (min == max)
            return min;
        return NextFloat() * (max - min) + min;
    }

    public double RangeDouble(double min, double max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(max), "max must be greater or equal to min");
        if (min == max)
            return min;
        return NextDouble() * (max - min) + min;
    }



    private static int GenerateSeed()
    {
        // Combine a fast tick counter with the current thread's managed ID.
        // This ensures unique seeds across threads while being zero-allocation.
        return (int)(Environment.TickCount ^ Environment.CurrentManagedThreadId ^ (uint)DateTime.Now.Ticks);
    }

    private uint NextUInt()
    {
        uint t = (_x ^ (_x << 11));
        _x = _y;
        _y = _z;
        _z = _w;
        return _w = (_w ^ (_w >> 19)) ^ (t ^ (t >> 8));
    }
}
