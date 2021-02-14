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
        // Input:
        // u32 | 0    1    2    3    |
        // u16 | 0 1  2 3  4 5  6 7  |
        // u8  | 0123 4567 8901 2345 |
        //     | RGBR GBRG BRGB RGBR |

        // Want to output 5 values

        var _1f = set1_ps(1.0f);
        var _255f = set1_ps(255.0f);
        var alignedCount = (count / 5) * 5;
        int i = 0;
        for (; i < alignedCount; i += 5)
        {
            var all = loadu_ps(&src[i * 3]);
            var v0u8 = srli_si128(all, 0);
            var v1u8 = srli_si128(all, 3);
            var v2u8 = srli_si128(all, 6);
            var v3u8 = srli_si128(all, 9);
            var v4u8 = srli_si128(all, 12);

            var v0u32 = cvtepu8_epi32(v0u8);
            var v1u32 = cvtepu8_epi32(v1u8);
            var v2u32 = cvtepu8_epi32(v2u8);
            var v3u32 = cvtepu8_epi32(v3u8);
            var v4u32 = cvtepu8_epi32(v4u8);

            var v0ps = cvtepi32_ps(v0u32);
            var v1ps = cvtepi32_ps(v1u32);
            var v2ps = cvtepi32_ps(v2u32);
            var v3ps = cvtepi32_ps(v3u32);
            var v4ps = cvtepi32_ps(v4u32);

            var v0div = div_ps(v0ps, _255f);
            var v1div = div_ps(v1ps, _255f);
            var v2div = div_ps(v2ps, _255f);
            var v3div = div_ps(v3ps, _255f);
            var v4div = div_ps(v4ps, _255f);

            var v0RGBA = insert_ps(v0div, _1f, 3 << 4);
            var v1RGBA = insert_ps(v1div, _1f, 3 << 4);
            var v2RGBA = insert_ps(v2div, _1f, 3 << 4);
            var v3RGBA = insert_ps(v3div, _1f, 3 << 4);
            var v4RGBA = insert_ps(v4div, _1f, 3 << 4);

            // Store back to memory
            // &dst[i + n] generates extra instructions for some reason,
            // while dst + i + n goes straight to the desired vmovups
            storeu_ps(dst + i + 0, v0RGBA);
            storeu_ps(dst + i + 1, v1RGBA);
            storeu_ps(dst + i + 2, v2RGBA);
            storeu_ps(dst + i + 3, v3RGBA);
            storeu_ps(dst + i + 4, v4RGBA);
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
        // Input
        // u32  0    1    2    3    4    5    6    7
        // u16  0 1  2 3  4 5  6 7  8 9  0 1  2 3  4 5 
        // u8   0123 4567 8901 2345 6789 0123 4567 7890
        //      RGBR GBRG BRGB RGBR GBRG BRGB RGBR GBRG
        // 
        // Want to output 10 values, in 5 registers.
        //      0123 4567 89AB CDEF GHIJ KLMN OPQR STUV
        //      RGBR GBRG BRGB RGBR GBRG BRGB RGBR GBRG
        //      
        // Desired patterns:
        // v0: 0123 4567 ---- ---- ---- ---- ---- ---- (don't need to do anything)
        // v1: 4567 89AB ---- ---- ---- ---- ---- ---- Can be achieved with shifts, bring 6 (R) to front of Lo128 bytes << 6
        // v2: CDEF GHIJ ---- ---- ---- ---- ---- ---- Need mm256_permutevar8x32 because we need to travel accross lanes
        // v3: GHIJ KLMN ---- ---- ---- ---- ---- ---- Can be achieved with shifts, bring I (R) to front of Hi128 bytes << 2
        // v4: OPQR STUV ---- ---- ---- ---- ---- ---- Can be achieved with shifts, bring O (R) to front of Hi128 bytes << 8

        var alphaComponentShuffle = mm256_setr_epi8(0, 1, 2, 0xFF, 3, 4, 5, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0, 1, 2, 0xFF, 3, 4, 5, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        var _255f = mm256_set1_ps(255.0f);
        var _0001f0001f = mm256_setr_ps(0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);
        var alignedCount = (count / 10) * 10;
        int i = 0;

        for (; i < alignedCount; i += 10)
        {
            var all = mm256_loadu_si256(&src[i * 3]);

            // Shift 2x RGB values to front of register
            var v0u8 = mm256_srli_si256(all, 0);
            var v1u8 = mm256_srli_si256(all, 6); 
            var v2u8 = mm256_permutevar8x32_ps(all, mm256_setr_epi32(3, 4, -1, -1, -1, -1, -1, -1));
            var v3u8 = mm256_srli_si256(all, 2);
            var v4u8 = mm256_srli_si256(all, 8);

            // Shuffle to make room for alpha component (0 is shuffled in in alpha position)
            var v0u8a = mm256_shuffle_epi8(v0u8, alphaComponentShuffle);
            var v1u8a = mm256_shuffle_epi8(v1u8, alphaComponentShuffle);
            var v2u8a = mm256_shuffle_epi8(v2u8, alphaComponentShuffle);
            var v3u8a = mm256_shuffle_epi8(v3u8, alphaComponentShuffle);
            var v4u8a = mm256_shuffle_epi8(v4u8, alphaComponentShuffle);

            // Convert to 32-bit integers
            var v0u32 = mm256_cvtepu8_epi32(mm256_extracti128_si256(v0u8a, 0));
            var v1u32 = mm256_cvtepu8_epi32(mm256_extracti128_si256(v1u8a, 0));
            var v2u32 = mm256_cvtepu8_epi32(mm256_extracti128_si256(v2u8a, 0));
            var v3u32 = mm256_cvtepu8_epi32(mm256_extracti128_si256(v3u8a, 1));
            var v4u32 = mm256_cvtepu8_epi32(mm256_extracti128_si256(v4u8a, 1));

            // Convert to 32-bit floating point
            var v0ps = mm256_cvtepi32_ps(v0u32);
            var v1ps = mm256_cvtepi32_ps(v1u32);
            var v2ps = mm256_cvtepi32_ps(v2u32);
            var v3ps = mm256_cvtepi32_ps(v3u32);
            var v4ps = mm256_cvtepi32_ps(v4u32);

            // Divide by 255.0f
            var v0psdiv = mm256_div_ps(v0ps, _255f);
            var v1psdiv = mm256_div_ps(v1ps, _255f);
            var v2psdiv = mm256_div_ps(v2ps, _255f);
            var v3psdiv = mm256_div_ps(v3ps, _255f);
            var v4psdiv = mm256_div_ps(v4ps, _255f);

            // Or with 1.0f in alpha slot.
            var v0psa1 = mm256_or_ps(v0psdiv, _0001f0001f);
            var v1psa1 = mm256_or_ps(v1psdiv, _0001f0001f);
            var v2psa1 = mm256_or_ps(v2psdiv, _0001f0001f);
            var v3psa1 = mm256_or_ps(v3psdiv, _0001f0001f);
            var v4psa1 = mm256_or_ps(v4psdiv, _0001f0001f);

            // Store back to memory
            // &dst[i + n] generates extra instructions for some reason,
            // while dst + i + n goes straight to the desired vmovups
            mm256_storeu_ps(dst + i + 0, v0psa1);
            mm256_storeu_ps(dst + i + 2, v1psa1);
            mm256_storeu_ps(dst + i + 4, v2psa1);
            mm256_storeu_ps(dst + i + 6, v3psa1);
            mm256_storeu_ps(dst + i + 8, v4psa1);
        }

        for (; i < count; i++)
            dst[i] = new float4(src[i * 3 + 0], src[i * 3 + 1], src[i * 3 + 2], 255.0f) / 255.0f;
    }
}

