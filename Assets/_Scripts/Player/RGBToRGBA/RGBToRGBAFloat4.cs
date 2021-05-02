using System;
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
using System.Collections;

[BurstCompile]
public static class RGBToRGBAFloat4
{
    public static List<float4> ForLoop(List<u8> src)
    {
        var dst = new List<float4>(src.Count / 3);
        for (int i = 0; i < src.Count; i += 3)
            dst.Add(new float4(src[i + 0] / 255.0f, src[i + 1] / 255.0f, src[i + 2] / 255.0f, 1.0f));

        return dst;
    }

    public static unsafe List<float4> BurstForLoop(List<u8> src)
    {
        var dst = new List<float4>(src.Count / 3);
        using (dst.ViewAsNativeArray(out var dstArray))
        using (src.ViewAsNativeArray(out var srcArray))
            BurstForLoop((u8*)srcArray.GetUnsafePtr(), (float4*)dstArray.GetUnsafePtr(), dst.Capacity);

        NoAllocHelpers.ResizeList(dst, src.Count / 3);
        return dst;
    }

    [BurstCompile]
    private static unsafe void BurstForLoop([NoAlias] u8* src, [NoAlias] float4* dst, int count)
    {
        for (int i = 0; i < count; i++)
            dst[i] = new float4(src[i * 3 + 0] / 255.0f, src[i * 3 + 1] / 255.0f, src[i * 3 + 2] / 255.0f, 1.0f);
    }

    public static unsafe List<float4> V128ForLoop(List<u8> src)
    {
        var dst = new List<float4>(src.Count / 3);
        using (dst.ViewAsNativeArray(out var dstArray))
        using (src.ViewAsNativeArray(out var srcArray))
            V128ForLoop((u8*)srcArray.GetUnsafePtr(), (float4*)dstArray.GetUnsafePtr(), dst.Capacity);

        NoAllocHelpers.ResizeList(dst, src.Count / 3);
        return dst;
    }

    [BurstCompile]
    private static unsafe void V128ForLoop([NoAlias] u8* src, [NoAlias] float4* dst, int count)
    {
        var alignedCount = (count / 5) * 5;
        var alpha = set1_epi32(0xFF << 24);
        var _255f = set1_ps(255.0f);
        int i = 0;
        for (;  i < alignedCount; i += 5)
        {
            var all = loadu_ps(src + i * 3);

            var v0 = srli_si128(all, 0);
            var v1 = srli_si128(all, 3);
            var v2 = srli_si128(all, 6);
            var v3 = srli_si128(all, 9);
            var v4 = srli_si128(all, 12);

            v0 = or_ps(v0, alpha);
            v1 = or_ps(v1, alpha);
            v2 = or_ps(v2, alpha);
            v3 = or_ps(v3, alpha);
            v4 = or_ps(v4, alpha);

            v0 = cvtepu8_epi32(v0);
            v1 = cvtepu8_epi32(v1);
            v2 = cvtepu8_epi32(v2);
            v3 = cvtepu8_epi32(v3);
            v4 = cvtepu8_epi32(v4);

            v0 = cvtepi32_ps(v0);
            v1 = cvtepi32_ps(v1);
            v2 = cvtepi32_ps(v2);
            v3 = cvtepi32_ps(v3);
            v4 = cvtepi32_ps(v4);

            v0 = div_ps(v0, _255f);
            v1 = div_ps(v1, _255f);
            v2 = div_ps(v2, _255f);
            v3 = div_ps(v3, _255f);
            v4 = div_ps(v4, _255f);

            storeu_ps(dst + i + 0, v0);
            storeu_ps(dst + i + 1, v1);
            storeu_ps(dst + i + 2, v2);
            storeu_ps(dst + i + 3, v3);
            storeu_ps(dst + i + 4, v4);
        }

        for (; i < count; i++)
            dst[i] = new float4(src[i * 3 + 0] / 255.0f, src[i * 3 + 1] / 255.0f, src[i * 3 + 2] / 255.0f, 1.0f);
    }

    public static unsafe List<float4> V256ForLoop(List<u8> src)
    {
        var dst = new List<float4>(src.Count / 3);
        using (dst.ViewAsNativeArray(out var dstArray))
        using (src.ViewAsNativeArray(out var srcArray))
            V256ForLoop((u8*)srcArray.GetUnsafePtr(), (float4*)dstArray.GetUnsafePtr(), dst.Capacity);

        NoAllocHelpers.ResizeList(dst, src.Count / 3);
        return dst;
    }

    [BurstCompile]
    private static unsafe void V256ForLoop([NoAlias] u8* src, [NoAlias] float4* dst, int count)
    {
        // Input:
        //  u128    0                   1
        //  u64     0         1         2         3
        //  u32     0    1    2    3    4    5    6    7
        //  u16     0 1  2 3  4 5  6 7  8 9  0 1  2 3  4 5
        //  u8      0123 4567 8901 2345 6789 0123 4567 8901
        //          RGBR GBRG BRGB RGBR GBRG BRGB RGBR GBRG 
        //          0  1   2   3   4  5   6   7   8  9
        // Registers: 
        //          0123 4567 89AB CDEF GHIJ KLMN OPQR ST--
        // v0       0123 45-- ---- ---- ---- ---- ---- ---- << 0, lo
        // v1       6789 AB-- ---- ---- ---- ---- ---- ---- << 6, lo
        // v2       CDEF GH-- ---- ---- ---- ---- ---- ---- permute_epi32(3, 4, -1, -1, -1)
        // v3       IJKL MN-- ---- ---- ---- ---- ---- ---- << 2, hi
        // v4       OPQR ST-- ---- ---- ---- ---- ---- ---- << 8, hi
        //
        // Path each register takes after isolating 8 values we're working on.
        // 
        // α = 255
        //          0123  45--  ----  ----  ----  ----  ----  ----
        //          0120  3450  ----  ----  ----  ----  ----  ----
        //          012α  345α  ----  ----  ----  ----  ----  ----
        //          0     1     2     α     3     4     5     α
        //          0f    1f    2f    αf    3f    4f    5f    αf
        //          0f/αf 1f/αf 2f/αf αf/αf 3f/αf 4f/αf 5f/αf αf/αf
        //          

        var shuffleV128 = setr_epi8(0, 1, 2, -1, 3, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1);
        var shuffleV256 = mm256_setr_m128(shuffleV128, shuffleV128);

        var alpha = mm256_set1_epi32(0xFF << 24);
        var alignedCount = (count / 10) * 10;
        var _1div255 = mm256_rcp_ps(mm256_set1_ps(255.0f));
        int i = 0;
        for (; i < alignedCount; i += 10)
        {
            var all = mm256_loadu_ps(src + i * 3);
            var v0 = mm256_srli_si256(all, 0);
            var v1 = mm256_srli_si256(all, 6);
            var v2 = mm256_permutevar8x32_epi32(all, mm256_setr_epi32(3, 4, -1, -1, -1, -1, -1, -1));
            var v3 = mm256_srli_si256(all, 2);
            var v4 = mm256_srli_si256(all, 8);

            v0 = mm256_shuffle_epi8(v0, shuffleV256);
            v1 = mm256_shuffle_epi8(v1, shuffleV256);
            v2 = mm256_shuffle_epi8(v2, shuffleV256);
            v3 = mm256_shuffle_epi8(v3, shuffleV256);
            v4 = mm256_shuffle_epi8(v4, shuffleV256);

            v0 = mm256_or_ps(v0, alpha);
            v1 = mm256_or_ps(v1, alpha);
            v2 = mm256_or_ps(v2, alpha);
            v3 = mm256_or_ps(v3, alpha);
            v4 = mm256_or_ps(v4, alpha);

            v0 = mm256_cvtepu8_epi32(v0.Lo128);
            v1 = mm256_cvtepu8_epi32(v1.Lo128);
            v2 = mm256_cvtepu8_epi32(v2.Lo128);
            v3 = mm256_cvtepu8_epi32(v3.Hi128);
            v4 = mm256_cvtepu8_epi32(v4.Hi128);

            v0 = mm256_cvtepi32_ps(v0);
            v1 = mm256_cvtepi32_ps(v1);
            v2 = mm256_cvtepi32_ps(v2);
            v3 = mm256_cvtepi32_ps(v3);
            v4 = mm256_cvtepi32_ps(v4);

            v0 = mm256_mul_ps(v0, _1div255);
            v1 = mm256_mul_ps(v1, _1div255);
            v2 = mm256_mul_ps(v2, _1div255);
            v3 = mm256_mul_ps(v3, _1div255);
            v4 = mm256_mul_ps(v4, _1div255);

            mm256_storeu_ps(dst + i + 0, v0);
            mm256_storeu_ps(dst + i + 2, v1);
            mm256_storeu_ps(dst + i + 4, v2);
            mm256_storeu_ps(dst + i + 6, v3);
            mm256_storeu_ps(dst + i + 8, v4);
        }

        for (; i < count; i++)
            dst[i] = new float4(src[i * 3 + 0] / 255.0f, src[i * 3 + 1] / 255.0f, src[i * 3 + 2] / 255.0f, 1.0f);
    }
}

