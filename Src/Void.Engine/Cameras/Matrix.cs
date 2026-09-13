// ============================================================================
//  Matrix.cs
// ============================================================================
//  VOID-owned 2D transformation matrix with row-major storage for direct GPU use.
//
//  Copyright (c) 2026 Void Engine
//  Licensed under the MIT License.
// ============================================================================

namespace Void.Engine.Cameras;

/// <summary>
/// Represents a 4x4 matrix specialized for VOID's 2D transformation pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The structure stores sixteen single-precision values in row-major field order so it can
/// be passed to renderer backends without converting through an external matrix type.
/// Public factory methods intentionally focus on 2D translation, scaling, rotation, skew,
/// orthographic projection, and composition.
/// </para>
/// <para>
/// VOID uses row-vector composition. A point transformed by <c>A * B</c> is transformed by
/// <c>A</c> first and then <c>B</c>.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct Matrix : IEquatable<Matrix>
{
    private const float InversionEpsilon = 1e-8f;
    private const float DecomposeEpsilon = 1e-6f;

    /// <summary>Gets or sets the value in row 1, column 1.</summary>
    public float M11;

    /// <summary>Gets or sets the value in row 1, column 2.</summary>
    public float M12;

    /// <summary>Gets or sets the value in row 1, column 3.</summary>
    public float M13;

    /// <summary>Gets or sets the value in row 1, column 4.</summary>
    public float M14;

    /// <summary>Gets or sets the value in row 2, column 1.</summary>
    public float M21;

    /// <summary>Gets or sets the value in row 2, column 2.</summary>
    public float M22;

    /// <summary>Gets or sets the value in row 2, column 3.</summary>
    public float M23;

    /// <summary>Gets or sets the value in row 2, column 4.</summary>
    public float M24;

    /// <summary>Gets or sets the value in row 3, column 1.</summary>
    public float M31;

    /// <summary>Gets or sets the value in row 3, column 2.</summary>
    public float M32;

    /// <summary>Gets or sets the value in row 3, column 3.</summary>
    public float M33;

    /// <summary>Gets or sets the value in row 3, column 4.</summary>
    public float M34;

    /// <summary>Gets or sets the value in row 4, column 1.</summary>
    public float M41;

    /// <summary>Gets or sets the value in row 4, column 2.</summary>
    public float M42;

    /// <summary>Gets or sets the value in row 4, column 3.</summary>
    public float M43;

    /// <summary>Gets or sets the value in row 4, column 4.</summary>
    public float M44;

    /// <summary>Gets the identity matrix.</summary>
    public static Matrix Identity => new(
        1f, 0f, 0f, 0f,
        0f, 1f, 0f, 0f,
        0f, 0f, 1f, 0f,
        0f, 0f, 0f, 1f);

    /// <summary>Gets whether this matrix is exactly the identity matrix.</summary>
    public readonly bool IsIdentity => Equals(Identity);

    /// <summary>
    /// Initializes a matrix from its sixteen row-major components.
    /// </summary>
    /// <param name="m11">Value for row 1, column 1.</param>
    /// <param name="m12">Value for row 1, column 2.</param>
    /// <param name="m13">Value for row 1, column 3.</param>
    /// <param name="m14">Value for row 1, column 4.</param>
    /// <param name="m21">Value for row 2, column 1.</param>
    /// <param name="m22">Value for row 2, column 2.</param>
    /// <param name="m23">Value for row 2, column 3.</param>
    /// <param name="m24">Value for row 2, column 4.</param>
    /// <param name="m31">Value for row 3, column 1.</param>
    /// <param name="m32">Value for row 3, column 2.</param>
    /// <param name="m33">Value for row 3, column 3.</param>
    /// <param name="m34">Value for row 3, column 4.</param>
    /// <param name="m41">Value for row 4, column 1.</param>
    /// <param name="m42">Value for row 4, column 2.</param>
    /// <param name="m43">Value for row 4, column 3.</param>
    /// <param name="m44">Value for row 4, column 4.</param>
    public Matrix(
        float m11, float m12, float m13, float m14,
        float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34,
        float m41, float m42, float m43, float m44)
    {
        M11 = m11; M12 = m12; M13 = m13; M14 = m14;
        M21 = m21; M22 = m22; M23 = m23; M24 = m24;
        M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        M41 = m41; M42 = m42; M43 = m43; M44 = m44;
    }

    /// <summary>Creates a 2D translation matrix.</summary>
    public static Matrix CreateTranslation(float x, float y)
    {
        Matrix result = Identity;
        result.M41 = x;
        result.M42 = y;
        return result;
    }

    /// <summary>Creates a 2D translation matrix.</summary>
    public static Matrix CreateTranslation(Vect2 position)
        => CreateTranslation(position.X, position.Y);

    /// <summary>Creates a uniform 2D scale matrix.</summary>
    public static Matrix CreateScale(float scale)
        => CreateScale(scale, scale);

    /// <summary>Creates a non-uniform 2D scale matrix.</summary>
    public static Matrix CreateScale(float x, float y)
    {
        Matrix result = Identity;
        result.M11 = x;
        result.M22 = y;
        return result;
    }

    /// <summary>Creates a non-uniform 2D scale matrix.</summary>
    public static Matrix CreateScale(Vect2 scale)
        => CreateScale(scale.X, scale.Y);

    /// <summary>Creates a 2D scale matrix around an origin point.</summary>
    public static Matrix CreateScale(Vect2 scale, Vect2 origin)
    {
        Matrix result = CreateScale(scale);
        result.M41 = origin.X - origin.X * scale.X;
        result.M42 = origin.Y - origin.Y * scale.Y;
        return result;
    }

    /// <summary>Creates a 2D rotation matrix using radians.</summary>
    public static Matrix CreateRotation(float radians)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);

        return new Matrix(
             c, s, 0f, 0f,
            -s, c, 0f, 0f,
             0f, 0f, 1f, 0f,
             0f, 0f, 0f, 1f);
    }

    /// <summary>Creates a 2D rotation matrix around an origin point using radians.</summary>
    public static Matrix CreateRotation(float radians, Vect2 origin)
    {
        float c = MathF.Cos(radians);
        float s = MathF.Sin(radians);

        return new Matrix(
             c, s, 0f, 0f,
            -s, c, 0f, 0f,
             0f, 0f, 1f, 0f,
             origin.X - origin.X * c + origin.Y * s,
             origin.Y - origin.X * s - origin.Y * c,
             0f,
             1f);
    }

    /// <summary>Creates a 2D skew matrix using radians for the X and Y shear angles.</summary>
    public static Matrix CreateSkew(float xRadians, float yRadians)
    {
        Matrix result = Identity;
        result.M21 = MathF.Tan(xRadians);
        result.M12 = MathF.Tan(yRadians);
        return result;
    }

    /// <summary>Creates a combined scale, rotation, then translation transform.</summary>
    public static Matrix CreateTransform(Vect2 position, float rotation, Vect2 scale)
        => CreateTransform(position, rotation, scale, Vect2.Zero);

    /// <summary>Creates a combined transform around an origin point.</summary>
    public static Matrix CreateTransform(Vect2 position, float rotation, Vect2 scale, Vect2 origin)
    {
        float c = MathF.Cos(rotation);
        float s = MathF.Sin(rotation);

        float m11 = scale.X * c;
        float m12 = scale.X * s;
        float m21 = -scale.Y * s;
        float m22 = scale.Y * c;

        float tx = position.X - origin.X * m11 - origin.Y * m21;
        float ty = position.Y - origin.X * m12 - origin.Y * m22;

        return new Matrix(
            m11, m12, 0f, 0f,
            m21, m22, 0f, 0f,
            0f, 0f, 1f, 0f,
            tx, ty, 0f, 1f);
    }

    /// <summary>Creates a centered positive-Y-down 2D orthographic projection.</summary>
    public static Matrix CreateOrthographic(float width, float height)
    {
        if (width <= 0f)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        if (height <= 0f)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");

        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;
        return CreateOrthographicOffCenter(-halfWidth, halfWidth, -halfHeight, halfHeight);
    }

    /// <summary>Creates an off-center positive-Y-down 2D orthographic projection.</summary>
    public static Matrix CreateOrthographicOffCenter(float left, float right, float top, float bottom)
    {
        float width = right - left;
        float height = bottom - top;

        if (MathF.Abs(width) <= InversionEpsilon)
            throw new ArgumentException("Left and right must define a non-zero width.");
        if (MathF.Abs(height) <= InversionEpsilon)
            throw new ArgumentException("Top and bottom must define a non-zero height.");

        return new Matrix(
            2f / width, 0f, 0f, 0f,
            0f, -2f / height, 0f, 0f,
            0f, 0f, 1f, 0f,
            -(right + left) / width,
            (bottom + top) / height,
            0f,
            1f);
    }

    /// <summary>Multiplies two matrices using VOID's row-vector composition order.</summary>
    public static Matrix Multiply(in Matrix a, in Matrix b)
    {
        return new Matrix(
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
            a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,
            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
            a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,
            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
            a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,
            a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
            a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
            a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
            a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44);
    }

    /// <summary>Returns the transpose of a matrix.</summary>
    public static Matrix Transpose(in Matrix matrix)
        => new(
            matrix.M11, matrix.M21, matrix.M31, matrix.M41,
            matrix.M12, matrix.M22, matrix.M32, matrix.M42,
            matrix.M13, matrix.M23, matrix.M33, matrix.M43,
            matrix.M14, matrix.M24, matrix.M34, matrix.M44);

    /// <summary>Calculates the determinant of the matrix.</summary>
    public readonly float Determinant()
    {
        float a0 = M11 * M22 - M12 * M21;
        float a1 = M11 * M23 - M13 * M21;
        float a2 = M11 * M24 - M14 * M21;
        float a3 = M12 * M23 - M13 * M22;
        float a4 = M12 * M24 - M14 * M22;
        float a5 = M13 * M24 - M14 * M23;
        float b0 = M31 * M42 - M32 * M41;
        float b1 = M31 * M43 - M33 * M41;
        float b2 = M31 * M44 - M34 * M41;
        float b3 = M32 * M43 - M33 * M42;
        float b4 = M32 * M44 - M34 * M42;
        float b5 = M33 * M44 - M34 * M43;
        return a0 * b5 - a1 * b4 + a2 * b3 + a3 * b2 - a4 * b1 + a5 * b0;
    }

    /// <summary>Attempts to invert a matrix without heap allocations.</summary>
    public static bool TryInvert(in Matrix matrix, out Matrix result)
    {
        if (IsAffine2D(matrix))
            return TryInvertAffine2D(matrix, out result);

        Span<float> a = stackalloc float[32];
        a.Clear();
        a[0] = matrix.M11; a[1] = matrix.M12; a[2] = matrix.M13; a[3] = matrix.M14; a[4] = 1f;
        a[8] = matrix.M21; a[9] = matrix.M22; a[10] = matrix.M23; a[11] = matrix.M24; a[13] = 1f;
        a[16] = matrix.M31; a[17] = matrix.M32; a[18] = matrix.M33; a[19] = matrix.M34; a[22] = 1f;
        a[24] = matrix.M41; a[25] = matrix.M42; a[26] = matrix.M43; a[27] = matrix.M44; a[31] = 1f;

        for (int column = 0; column < 4; column++)
        {
            int pivotRow = column;
            float pivotMagnitude = MathF.Abs(a[pivotRow * 8 + column]);
            for (int row = column + 1; row < 4; row++)
            {
                float magnitude = MathF.Abs(a[row * 8 + column]);
                if (magnitude > pivotMagnitude)
                {
                    pivotMagnitude = magnitude;
                    pivotRow = row;
                }
            }

            if (pivotMagnitude <= InversionEpsilon)
            {
                result = default;
                return false;
            }

            if (pivotRow != column)
            {
                int aRow = column * 8;
                int bRow = pivotRow * 8;
                for (int i = 0; i < 8; i++)
                    (a[aRow + i], a[bRow + i]) = (a[bRow + i], a[aRow + i]);
            }

            int pivotIndex = column * 8;
            float inversePivot = 1f / a[pivotIndex + column];
            for (int i = 0; i < 8; i++)
                a[pivotIndex + i] *= inversePivot;

            for (int row = 0; row < 4; row++)
            {
                if (row == column)
                    continue;

                int rowIndex = row * 8;
                float factor = a[rowIndex + column];
                if (factor == 0f)
                    continue;

                for (int i = 0; i < 8; i++)
                    a[rowIndex + i] -= factor * a[pivotIndex + i];
            }
        }

        result = new Matrix(
            a[4], a[5], a[6], a[7],
            a[12], a[13], a[14], a[15],
            a[20], a[21], a[22], a[23],
            a[28], a[29], a[30], a[31]);
        return true;
    }

    /// <summary>Returns the inverse of a matrix.</summary>
    public static Matrix Invert(in Matrix matrix)
    {
        if (!TryInvert(matrix, out Matrix result))
            throw new InvalidOperationException("Matrix is not invertible.");
        return result;
    }

    /// <summary>Transforms a 2D point, including translation.</summary>
    public readonly Vect2 TransformPoint(Vect2 point)
        => new(point.X * M11 + point.Y * M21 + M41,
               point.X * M12 + point.Y * M22 + M42);

    /// <summary>Transforms a 2D direction, excluding translation.</summary>
    public readonly Vect2 TransformDirection(Vect2 direction)
        => new(direction.X * M11 + direction.Y * M21,
               direction.X * M12 + direction.Y * M22);

    /// <summary>Attempts to decompose a non-skewed 2D affine matrix.</summary>
    public readonly bool Decompose(out Vect2 translation, out float rotation, out Vect2 scale)
    {
        translation = new Vect2(M41, M42);
        rotation = 0f;
        scale = Vect2.Zero;

        if (!IsAffine2D(this))
            return false;

        float scaleX = MathF.Sqrt(M11 * M11 + M12 * M12);
        float scaleY = MathF.Sqrt(M21 * M21 + M22 * M22);
        if (scaleX <= DecomposeEpsilon || scaleY <= DecomposeEpsilon)
            return false;

        float dot = M11 * M21 + M12 * M22;
        if (MathF.Abs(dot) > DecomposeEpsilon * scaleX * scaleY)
            return false;

        float determinant = M11 * M22 - M12 * M21;
        if (determinant < 0f)
            scaleY = -scaleY;

        rotation = MathF.Atan2(M12, M11);
        scale = new Vect2(scaleX, scaleY);
        return true;
    }

    /// <summary>Determines whether every component is within a supplied tolerance.</summary>
    public readonly bool NearlyEquals(in Matrix other, float tolerance = MathHelper.Epsilon)
    {
        if (tolerance < 0f)
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance cannot be negative.");

        return
            MathF.Abs(M11 - other.M11) <= tolerance && MathF.Abs(M12 - other.M12) <= tolerance &&
            MathF.Abs(M13 - other.M13) <= tolerance && MathF.Abs(M14 - other.M14) <= tolerance &&
            MathF.Abs(M21 - other.M21) <= tolerance && MathF.Abs(M22 - other.M22) <= tolerance &&
            MathF.Abs(M23 - other.M23) <= tolerance && MathF.Abs(M24 - other.M24) <= tolerance &&
            MathF.Abs(M31 - other.M31) <= tolerance && MathF.Abs(M32 - other.M32) <= tolerance &&
            MathF.Abs(M33 - other.M33) <= tolerance && MathF.Abs(M34 - other.M34) <= tolerance &&
            MathF.Abs(M41 - other.M41) <= tolerance && MathF.Abs(M42 - other.M42) <= tolerance &&
            MathF.Abs(M43 - other.M43) <= tolerance && MathF.Abs(M44 - other.M44) <= tolerance;
    }

    /// <summary>
    /// Determines whether this matrix has exactly the same component values as another matrix.
    /// </summary>
    /// <param name="other">The matrix to compare with this matrix.</param>
    /// <returns><see langword="true"/> when all sixteen components are equal; otherwise, <see langword="false"/>.</returns>
    public readonly bool Equals(Matrix other)
        => M11 == other.M11 && M12 == other.M12 && M13 == other.M13 && M14 == other.M14 &&
           M21 == other.M21 && M22 == other.M22 && M23 == other.M23 && M24 == other.M24 &&
           M31 == other.M31 && M32 == other.M32 && M33 == other.M33 && M34 == other.M34 &&
           M41 == other.M41 && M42 == other.M42 && M43 == other.M43 && M44 == other.M44;

    /// <summary>
    /// Determines whether the specified object is a matrix with exactly equal component values.
    /// </summary>
    /// <param name="obj">The object to compare with this matrix.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an equal <see cref="Matrix"/>; otherwise, <see langword="false"/>.</returns>
    public override readonly bool Equals(object obj)
        => obj is Matrix other && Equals(other);

    /// <summary>
    /// Returns a hash code based on all sixteen matrix components.
    /// </summary>
    /// <returns>The hash code for this matrix.</returns>
    public override readonly int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(M11); hash.Add(M12); hash.Add(M13); hash.Add(M14);
        hash.Add(M21); hash.Add(M22); hash.Add(M23); hash.Add(M24);
        hash.Add(M31); hash.Add(M32); hash.Add(M33); hash.Add(M34);
        hash.Add(M41); hash.Add(M42); hash.Add(M43); hash.Add(M44);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns a string containing all sixteen matrix components.
    /// </summary>
    /// <returns>A string representation of this matrix.</returns>
    public override readonly string ToString()
        => $"{{ {{M11:{M11} M12:{M12} M13:{M13} M14:{M14}}} " +
           $"{{M21:{M21} M22:{M22} M23:{M23} M24:{M24}}} " +
           $"{{M31:{M31} M32:{M32} M33:{M33} M34:{M34}}} " +
           $"{{M41:{M41} M42:{M42} M43:{M43} M44:{M44}}} }}";

    /// <summary>
    /// Multiplies two matrices using VOID's row-vector composition order.
    /// </summary>
    /// <param name="a">The transform applied first.</param>
    /// <param name="b">The transform applied second.</param>
    /// <returns>The composed matrix.</returns>
    public static Matrix operator *(in Matrix a, in Matrix b) => Multiply(a, b);

    /// <summary>
    /// Determines whether two matrices have exactly equal component values.
    /// </summary>
    /// <param name="a">The first matrix.</param>
    /// <param name="b">The second matrix.</param>
    /// <returns><see langword="true"/> when the matrices are exactly equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(in Matrix a, in Matrix b) => a.Equals(b);

    /// <summary>
    /// Determines whether two matrices differ in at least one component.
    /// </summary>
    /// <param name="a">The first matrix.</param>
    /// <param name="b">The second matrix.</param>
    /// <returns><see langword="true"/> when the matrices are not exactly equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(in Matrix a, in Matrix b) => !a.Equals(b);

    private static bool IsAffine2D(in Matrix matrix)
        => matrix.M13 == 0f && matrix.M14 == 0f &&
           matrix.M23 == 0f && matrix.M24 == 0f &&
           matrix.M31 == 0f && matrix.M32 == 0f && matrix.M34 == 0f &&
           matrix.M43 == 0f && matrix.M44 == 1f;

    private static bool TryInvertAffine2D(in Matrix matrix, out Matrix result)
    {
        float determinant = matrix.M11 * matrix.M22 - matrix.M12 * matrix.M21;
        if (MathF.Abs(determinant) <= InversionEpsilon || MathF.Abs(matrix.M33) <= InversionEpsilon)
        {
            result = default;
            return false;
        }

        float inverseDeterminant = 1f / determinant;
        float m11 = matrix.M22 * inverseDeterminant;
        float m12 = -matrix.M12 * inverseDeterminant;
        float m21 = -matrix.M21 * inverseDeterminant;
        float m22 = matrix.M11 * inverseDeterminant;
        float tx = -(matrix.M41 * m11 + matrix.M42 * m21);
        float ty = -(matrix.M41 * m12 + matrix.M42 * m22);

        result = new Matrix(
            m11, m12, 0f, 0f,
            m21, m22, 0f, 0f,
            0f, 0f, 1f / matrix.M33, 0f,
            tx, ty, 0f, 1f);
        return true;
    }
}
