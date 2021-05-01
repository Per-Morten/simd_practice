using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Unity.Burst.Intrinsics.X86;
using static Unity.Burst.Intrinsics.X86.Sse;
using static Unity.Burst.Intrinsics.X86.Sse2;
using static Unity.Burst.Intrinsics.X86.Sse3;
using static Unity.Burst.Intrinsics.X86.Ssse3;
using static Unity.Burst.Intrinsics.X86.Sse4_1;
using static Unity.Burst.Intrinsics.X86.Sse4_2;
using static Unity.Burst.Intrinsics.X86.Popcnt;
using static Unity.Burst.Intrinsics.X86.Avx;
using static Unity.Burst.Intrinsics.X86.Avx2;
using static Unity.Burst.Intrinsics.X86.Fma;
using static Unity.Burst.Intrinsics.X86.F16C;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

public static class SIMDHelpers
{
    /// <summary>
    /// Shuffles 32-bit elements in <paramref name="value"/> to the left (beginning) of the returned value
    /// if the most significant bit of the corresponding 32-bit element in <paramref name="mask"/> is set.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe v128 LeftPack4PS(v128 mask, v128 value)
    {
        var m = movemask_ps(mask);
        return permutevar_ps(value, LeftPack4PSLUT[m]);
    }

    /// <inheritdoc cref="LeftPack4PS(v128, v128)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static v256 LeftPack8PS(v256 mask, v256 value)
    {
        var v256Mask = mm256_movemask_ps(mask);
        var hiMask = (v256Mask >> 4) & 0xF; // Isolate movemask for the high 128 bits
        var loMask = (v256Mask >> 0) & 0xF; // Isolate movemask for the lo 128 bits
        var hiCount = popcnt_u32((uint)hiMask);
        var loCount = popcnt_u32((uint)loMask);
        var perm = mm256_set_m128(LeftPack4PSLUT[hiMask], LeftPack4PSLUT[loMask]);
        value = mm256_permutevar_ps(value, perm); // "LeftPack4PS" internal 128-bit lanes of value.
        return mm256_permutevar8x32_ps(value, LeftPack8PSLUT[(hiCount * 5) + loCount]);
    }

    /// <summary>
    /// Compute the absolute value of packed single-precision (32-bit) floating-point elements in <paramref name="v"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static v128 AbsPS(v128 v)
    {
        // IEEE-754 floats only have a sign bit, and don't do 2's complement logic. 
        // So for Abs on floats we can just zero the sign bit.
        var ZeroSignBitMask = set1_epi32(~(1 << 31)); // All bits set except the sign bit.
        return and_ps(v, ZeroSignBitMask);
    }

    /// <inheritdoc cref="AbsPS(v128)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static v256 AbsPS(v256 v)
    {
        // IEEE-754 floats only have a sign bit, and don't do 2's complement logic. So for Abs on floats we can just zero the sign bit.
        var ZeroSignBitMask = mm256_set1_epi32(~(1 << 31)); // All bits set except the sign bit.
        return mm256_and_ps(v, ZeroSignBitMask);
    }

    /// <summary>
    /// Utility for easily extracting a float from a <paramref name="v"/>.
    /// Ideally this shouldn't be done due to performance overhead.
    /// But often useful for debugging etc.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ExtractPS(v128 v, int idx)
    {
        return idx switch
        {
            0 => v.Float0,
            1 => v.Float1,
            2 => v.Float2,
            3 => v.Float3,
            _ => float.NaN,
        };
    }

    /// <inheritdoc cref="ExtractPS(v128, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ExtractPS(v256 v, int idx)
    {
        return idx switch
        {
            0 => v.Float0,
            1 => v.Float1,
            2 => v.Float2,
            3 => v.Float3,
            4 => v.Float4,
            5 => v.Float5,
            6 => v.Float6,
            7 => v.Float7,
            _ => float.NaN,
        };
    }

    /// <summary>
    /// Lookup table for doing the LeftPack4PS operation. 
    /// <para>Do not re-order!</para>
    /// </summary>
    private static readonly v128[] LeftPack4PSLUT = new v128[]
    {
        new v128(0, 0, 0, 0),
        new v128(0, 0, 0, 0),
        new v128(1, 0, 0, 0),
        new v128(0, 1, 0, 0),
        new v128(2, 0, 0, 0),
        new v128(0, 2, 0, 0),
        new v128(1, 2, 0, 0),
        new v128(0, 1, 2, 0),
        new v128(3, 0, 0, 0),
        new v128(0, 3, 0, 0),
        new v128(1, 3, 0, 0),
        new v128(0, 1, 3, 0),
        new v128(2, 3, 0, 0),
        new v128(0, 2, 3, 0),
        new v128(1, 2, 3, 0),
        new v128(0, 1, 2, 3),
    };

    /// <summary>
    /// Lookup table for doing the LeftPack8PS operation. 
    /// <para>Do not re-order!</para>
    /// </summary>
    private static readonly v256[] LeftPack8PSLUT = new v256[]
    {
        new v256(0, 0, 0, 0, 0, 0, 0, 0),
        new v256(0, 0, 0, 0, 0, 0, 0, 0),
        new v256(0, 1, 0, 0, 0, 0, 0, 0),
        new v256(0, 1, 2, 0, 0, 0, 0, 0),
        new v256(0, 1, 2, 3, 0, 0, 0, 0),
        new v256(4, 0, 0, 0, 0, 0, 0, 0),
        new v256(0, 4, 0, 0, 0, 0, 0, 0),
        new v256(0, 1, 4, 0, 0, 0, 0, 0),
        new v256(0, 1, 2, 4, 0, 0, 0, 0),
        new v256(0, 1, 2, 3, 4, 0, 0, 0),
        new v256(4, 5, 0, 0, 0, 0, 0, 0),
        new v256(0, 4, 5, 0, 0, 0, 0, 0),
        new v256(0, 1, 4, 5, 0, 0, 0, 0),
        new v256(0, 1, 2, 4, 5, 0, 0, 0),
        new v256(0, 1, 2, 3, 4, 5, 0, 0),
        new v256(4, 5, 6, 0, 0, 0, 0, 0),
        new v256(0, 4, 5, 6, 0, 0, 0, 0),
        new v256(0, 1, 4, 5, 6, 0, 0, 0),
        new v256(0, 1, 2, 4, 5, 6, 0, 0),
        new v256(0, 1, 2, 3, 4, 5, 6, 0),
        new v256(4, 5, 6, 7, 0, 0, 0, 0),
        new v256(0, 4, 5, 6, 7, 0, 0, 0),
        new v256(0, 1, 4, 5, 6, 7, 0, 0),
        new v256(0, 1, 2, 4, 5, 6, 7, 0),
        new v256(0, 1, 2, 3, 4, 5, 6, 7),
    };
}

