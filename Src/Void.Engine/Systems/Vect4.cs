using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Void.Engine.Systems;

public readonly struct Vect4
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float W { get; }

    public Vect4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
}
