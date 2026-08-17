using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Void.Engine.Systems;

public readonly struct Vect3
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; }

    public Vect3(float x, float y, float z) { X = x; Y = y; Z = z; }
}
