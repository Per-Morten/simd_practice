﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

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
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Jobs.LowLevel.Unsafe;

using u32 = System.UInt32;
using u8 = System.Byte;

[BurstCompile]
public static class RGBToRGBAU32
{
    public static List<u32> ForLoop(List<u8> src)
    {
        var dst = new List<u32>(src.Count / 3);
        for (int i = 0; i < src.Count; i += 3)
        {
            var rgba = (u32)src[i + 0] << 0 |
                       (u32)src[i + 1] << 8 |
                       (u32)src[i + 2] << 16 |
                       (u32)0xFF << 24;

            dst.Add(rgba);
        }

        return dst;
    }

    public static unsafe List<u32> BurstForLoop(List<u8> src)
    {
        var dst = new List<u32>(src.Count / 3);
        using (dst.ViewAsNativeArray(out var dstArray))
        using (src.ViewAsNativeArray(out var srcArray))
            BurstForLoop((u8*)srcArray.GetUnsafePtr(), (u32*)dstArray.GetUnsafePtr(), dst.Capacity);

        NoAllocHelpers.ResizeList(dst, src.Count / 3);
        return dst;
    }

    [BurstCompile]
    private static unsafe void BurstForLoop([NoAlias] u8* src, [NoAlias] u32* dst, int count)
    {
        for (int i = 0; i < count; i++)
            dst[i] = (u32)src[i * 3 + 0] << 0 |
                     (u32)src[i * 3 + 1] << 8 |
                     (u32)src[i * 3 + 2] << 16 |
                     (u32)0xFF << 24;
    }

    public static unsafe List<u32> V128ForLoop(List<u8> src)
    {
        var dst = new List<u32>(src.Count / 3);
        using (dst.ViewAsNativeArray(out var dstArray))
        using (src.ViewAsNativeArray(out var srcArray))
            V128ForLoop((u8*)srcArray.GetUnsafePtr(), (u32*)dstArray.GetUnsafePtr(), dst.Capacity);

        NoAllocHelpers.ResizeList(dst, src.Count / 3);
        return dst;
    }

    [BurstCompile]
    private static unsafe void V128ForLoop([NoAlias] u8* src, [NoAlias] u32* dst, int count)
    {
        // Input:
        // 
        // 0123 4567 8901 2345
        // RGBR GBRG BRGB RGBR
        // Output:
        // RGBA RGBA RGBA RGBA

        var shuffle = setr_epi8(0, 1, 2, -1, 3, 4, 5, -1, 6, 7, 8, -1, 9, 10, 11, -1);
        var alpha = set1_epi32(0xFF << 24);
        
        int i = 0;
        var alignedCount = count & ~3;
        for (; i < alignedCount; i += 4)
        {
            var v0 = loadu_ps(src + i * 3);
            var v1 = shuffle_epi8(v0, shuffle);
            var v2 = or_ps(v1, alpha);
            storeu_ps((dst + i), v2);
        }

        for (; i < count; i++)
            dst[i] = (u32)src[i * 3 + 0] << 0 |
                     (u32)src[i * 3 + 1] << 8 |
                     (u32)src[i * 3 + 2] << 16 |
                     (u32)0xFF << 24;
    }

    public static unsafe List<u32> V256ForLoop(List<u8> src)
    {
        var dst = new List<u32>(src.Count / 3);
        using (dst.ViewAsNativeArray(out var dstArray))
        using (src.ViewAsNativeArray(out var srcArray))
            V256ForLoop((u8*)srcArray.GetUnsafePtr(), (u32*)dstArray.GetUnsafePtr(), dst.Capacity);

        NoAllocHelpers.ResizeList(dst, src.Count / 3);
        return dst;
    }

    [BurstCompile]
    private static unsafe void V256ForLoop([NoAlias] u8* src, [NoAlias] u32* dst, int count)
    {
        // Input
        // u128     0                   1
        // u64      0         1         2         3
        // u32      0    1    2    3    4    5    6    7
        // u16      0 1  2 3  4 5  6 7  8 9  0 1  2 3  4 5
        // u8       0123 4567 8901 2345 6789 0123 4567 8901
        //          RGBR GBRG BRGB RGBR GBRG BRGB ---- ----
        // Output
        //          RGBA RGBA RGBA RGBA RGBA RGBA RBGA RGBA
        // Path
        // v0 =     0123 4567 89AB CDEF GHIJ KLMN ---- ----
        // v1 =     0123 4567 89AB ---- CDEF GHIJ KLMN ----
        // v2 =     012- 345- 678- 9AB- CDE- FGH- IJK- LMN-
        // v3 =     012α 345α 678α 9ABα CDEα FGHα IJKα LMNα

        var alignedCount = count & ~7;
        var permute = mm256_setr_epi32(0, 1, 2, 0xFF, 3, 4, 5, 0xFF);
        var shuffleV128 = setr_epi8(0, 1, 2, -1, 3, 4, 5, -1, 6, 7, 8, -1, 9, 10, 11, -1);
        var shuffleV256 = mm256_setr_m128(shuffleV128, shuffleV128);
        var alpha = mm256_set1_epi32(0xFF << 24);
        int i = 0;
        for (; i < alignedCount; i += 8)
        {
            var v0 = mm256_loadu_ps(src + i * 3);
            var v1 = mm256_permutevar8x32_epi32(v0, permute);
            var v2 = mm256_shuffle_epi8(v1, shuffleV256);
            var v3 = mm256_or_ps(v2, alpha);
            mm256_storeu_ps(dst + i, v3);
        }

        for (; i < count; i++)
            dst[i] = (u32)src[i * 3 + 0] << 0 |
                     (u32)src[i * 3 + 1] << 8 |
                     (u32)src[i * 3 + 2] << 16 |
                     (u32)0xFF << 24;
    }
}

