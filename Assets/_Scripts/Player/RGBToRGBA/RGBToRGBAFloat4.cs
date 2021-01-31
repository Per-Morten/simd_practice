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

        var _1f = set1_ps(1.0f); // 0x3f800000 == 1.0f
        var _255f = set1_ps(255.0f);
        var alignedCount = count / 5;
        int i = 0;
        for (; i < alignedCount; i += 5)
        {
            var all = loadu_ps(&src[i * 3]);
            var v0u8 = srli_si128(all, 0);
            var v1u8 = srli_si128(all, 3);
            var v2u8 = srli_si128(all, 6);
            var v3u8 = srli_si128(all, 9);
            var v4u8 = srli_si128(all, 12);

            var v0u16 = cvtepu8_epi32(v0u8);
            var v1u16 = cvtepu8_epi32(v1u8);
            var v2u16 = cvtepu8_epi32(v2u8);
            var v3u16 = cvtepu8_epi32(v3u8);
            var v4u16 = cvtepu8_epi32(v4u8);

            var v0ps = cvtepi32_ps(v0u16);
            var v1ps = cvtepi32_ps(v1u16);
            var v2ps = cvtepi32_ps(v2u16);
            var v3ps = cvtepi32_ps(v3u16);
            var v4ps = cvtepi32_ps(v4u16);

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

            storeu_ps(&dst[i + 0], v0RGBA);
            storeu_ps(&dst[i + 1], v1RGBA);
            storeu_ps(&dst[i + 2], v2RGBA);
            storeu_ps(&dst[i + 3], v3RGBA);
            storeu_ps(&dst[i + 4], v4RGBA);
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

    }
}

