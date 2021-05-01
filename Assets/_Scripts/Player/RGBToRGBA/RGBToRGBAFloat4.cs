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
        // Input:
        // u32      0    1    2    3
        // u16      0 1  2 3  4 5  6 7
        // u8       0123 4567 8901 2345
        //          RGBR GBRG BRGB RGBR

        var alignedCount = (count / 5) * 5;
        var alpha = set1_epi32(0xFF << 24);
        var _255f = set1_ps(255.0f);

        int i = 0;
        for (; i < alignedCount; i += 5)
        {
            var v0 = loadu_si128(src + i * 3);
            
            var v0u8 = v0;
            var v1u8 = srli_si128(v0, 3);
            var v2u8 = srli_si128(v0, 6);
            var v3u8 = srli_si128(v0, 9);
            var v4u8 = srli_si128(v0, 12);

            var v0u8a = or_ps(v0u8, alpha);
            var v1u8a = or_ps(v1u8, alpha);
            var v2u8a = or_ps(v2u8, alpha);
            var v3u8a = or_ps(v3u8, alpha);
            var v4u8a = or_ps(v4u8, alpha);

            var v0u32 = cvtepu8_epi32(v0u8a);
            var v1u32 = cvtepu8_epi32(v1u8a);
            var v2u32 = cvtepu8_epi32(v2u8a);
            var v3u32 = cvtepu8_epi32(v3u8a);
            var v4u32 = cvtepu8_epi32(v4u8a);

            var v0f32 = cvtepi32_ps(v0u32);
            var v1f32 = cvtepi32_ps(v1u32);
            var v2f32 = cvtepi32_ps(v2u32);
            var v3f32 = cvtepi32_ps(v3u32);
            var v4f32 = cvtepi32_ps(v4u32);

            var v0f32div = div_ps(v0f32, _255f);
            var v1f32div = div_ps(v1f32, _255f);
            var v2f32div = div_ps(v2f32, _255f);
            var v3f32div = div_ps(v3f32, _255f);
            var v4f32div = div_ps(v4f32, _255f);

            storeu_ps(dst + i + 0, v0f32div);
            storeu_ps(dst + i + 1, v1f32div);
            storeu_ps(dst + i + 2, v2f32div);
            storeu_ps(dst + i + 3, v3f32div);
            storeu_ps(dst + i + 4, v4f32div);
        }

        for (; i < count; i++)
            dst[i] = new float4(src[i * 3 + 0], src[i * 3 + 1], src[i * 3 + 2], 255.0f) / 255.0f;

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
        // u128   0                   1
        // u64    0         1         2         3
        // u32    0    1    2    3    4    5    6    7
        // u16    0 1  2 3  4 5  6 7  8 9  0 1  2 3  4 5 
        // u8     0123 4567 8901 2345 6789 0123 4567 8901
        //        RGBR GBRG BRGB RGBR GBRG BRGB RGBR GB--
        //        0123 4567 89AB CDEF GHIJ KLMN OPQR STUV
        //
        // 0123 45-- = No operations
        // 6789 AB-- = Right shift Lo 6
        // CDEF GH-- = Permutevar, GH travels accross lanes
        // IJKL MN-- = Right shift Hi 2
        // OPQR ST-- = Right shift Hi 8

        var alphaShuffle = mm256_setr_epi8(0x00, 0x01, 0x02, 0xFF, 0x03, 0x04, 0x05, 0xFF,
                                           0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
                                           0x00, 0x01, 0x02, 0xFF, 0x03, 0x04, 0x05, 0xFF, 
                                           0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        var alpha = mm256_set1_epi32(0xFF << 24);
        var _255f = mm256_set1_ps(255.0f);

        var alignedCount = (count / 10) * 10;
        int i = 0;
        for (; i < alignedCount; i += 10)
        {
            var v0 = mm256_loadu_si256(src + i * 3);

            var v0u8 = v0;
            var v1u8 = mm256_srli_si256(v0, 6);
            var v2u8 = mm256_permutevar8x32_epi32(v0, mm256_setr_epi32(3, 4, -1, -1, -1, -1, -1, -1));
            var v3u8 = mm256_srli_si256(v0, 2);
            var v4u8 = mm256_srli_si256(v0, 8);

            var v0u8a0 = mm256_shuffle_epi8(v0u8, alphaShuffle);
            var v1u8a0 = mm256_shuffle_epi8(v1u8, alphaShuffle);
            var v2u8a0 = mm256_shuffle_epi8(v2u8, alphaShuffle);
            var v3u8a0 = mm256_shuffle_epi8(v3u8, alphaShuffle);
            var v4u8a0 = mm256_shuffle_epi8(v4u8, alphaShuffle);

            var v0u8a1 = mm256_or_ps(v0u8a0, alpha);
            var v1u8a1 = mm256_or_ps(v1u8a0, alpha);
            var v2u8a1 = mm256_or_ps(v2u8a0, alpha);
            var v3u8a1 = mm256_or_ps(v3u8a0, alpha);
            var v4u8a1 = mm256_or_ps(v4u8a0, alpha);

            var v0u32 = mm256_cvtepu8_epi32(v0u8a1.Lo128);
            var v1u32 = mm256_cvtepu8_epi32(v1u8a1.Lo128);
            var v2u32 = mm256_cvtepu8_epi32(v2u8a1.Lo128);
            var v3u32 = mm256_cvtepu8_epi32(v3u8a1.Hi128);
            var v4u32 = mm256_cvtepu8_epi32(v4u8a1.Hi128);

            var v0f32 = mm256_cvtepi32_ps(v0u32);
            var v1f32 = mm256_cvtepi32_ps(v1u32);
            var v2f32 = mm256_cvtepi32_ps(v2u32);
            var v3f32 = mm256_cvtepi32_ps(v3u32);
            var v4f32 = mm256_cvtepi32_ps(v4u32);

            var v0f32div = mm256_div_ps(v0f32, _255f);
            var v1f32div = mm256_div_ps(v1f32, _255f);
            var v2f32div = mm256_div_ps(v2f32, _255f);
            var v3f32div = mm256_div_ps(v3f32, _255f);
            var v4f32div = mm256_div_ps(v4f32, _255f);

            mm256_storeu_si256(dst + i + 0, v0f32div);
            mm256_storeu_si256(dst + i + 2, v1f32div);
            mm256_storeu_si256(dst + i + 4, v2f32div);
            mm256_storeu_si256(dst + i + 6, v3f32div);
            mm256_storeu_si256(dst + i + 8, v4f32div);
        }

        for (; i < count; i++)
            dst[i] = new float4(src[i * 3 + 0], src[i * 3 + 1], src[i * 3 + 2], 255.0f) / 255.0f;
    }
}

