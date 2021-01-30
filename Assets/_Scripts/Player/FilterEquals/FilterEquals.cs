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

[BurstCompile]
public static class FilterEquals
{
    public static List<int> IEnumerableFilter(List<int> src, int target)
    {
        return src.Where(x => x == target).ToList();
    }

    public static List<int> ForLoopFilter(List<int> src, int target)
    {
        var dst = new List<int>();
        for (int i = 0; i < src.Count; i++)
            if (src[i] == target)
                dst.Add(src[i]);
        return dst;
    }

    public static List<int> ForLoopFilterCapacity(List<int> src, int target)
    {
        var dst = new List<int>(src.Count);
        for (int i = 0; i < src.Count; i++)
            if (src[i] == target)
                dst.Add(src[i]);
        return dst;
    }

    public static unsafe List<int> BurstFilter(List<int> src, int target)
    {
        var dst = new List<int>(src.Count);
        using var srcDispose = src.ViewAsNativeArray(out var srcArray);
        using var dstDispose = dst.ViewAsNativeArray(out var dstArray);
        BurstFilter((int*)srcArray.GetUnsafeReadOnlyPtr(), srcArray.Length, target, (int*)dstArray.GetUnsafePtr(), out var dstCount);
        NoAllocHelpers.ResizeList(dst, dstCount);
        return dst;
    }

    [BurstCompile]
    private static unsafe void BurstFilter(int* src, int srcCount, int target, int* dst, out int dstCount)
    {
        dstCount = 0;
        for (int i = 0; i < srcCount; i++)
            if (src[i] == target)
                dst[dstCount++] = src[i];
    }

    public static unsafe List<int> V128Filter(List<int> src, int target)
    {
        var dst = new List<int>(src.Count);
        using var srcDispose = src.ViewAsNativeArray(out var srcArray);
        using var dstDispose = dst.ViewAsNativeArray(out var dstArray);
        V128Filter((int*)srcArray.GetUnsafeReadOnlyPtr(), srcArray.Length, target, (int*)dstArray.GetUnsafePtr(), out var dstCount);
        NoAllocHelpers.ResizeList(dst, dstCount);
        return dst;
    }

    [BurstCompile]
    public static unsafe void V128Filter(int* src, int srcCount, int target, int* dst, out int dstCount)
    {
        var dstPtr = dst;
        var alignedCount = srcCount & ~3;
        int i = 0;
        for (; i < alignedCount; i += 4)
        {
            var val = loadu_ps(&src[i]);
            var cmp = cmpeq_epi32(val, set1_epi32(target));
            var packed = SIMDHelpers.LeftPack4PS(cmp, val);
            storeu_ps(dstPtr, packed);
            dstPtr += popcnt_u32((uint)movemask_ps(cmp));
        }

        for (; i < srcCount; i++)
            if (src[i] == target)
                *dstPtr++ = src[i];

        dstCount = (int)(dstPtr - dst);
    }

    public static unsafe List<int> V256Filter(List<int> src, int target)
    {
        var dst = new List<int>(src.Count);
        using var srcDispose = src.ViewAsNativeArray(out var srcArray);
        using var dstDispose = dst.ViewAsNativeArray(out var dstArray);
        V256Filter((int*)srcArray.GetUnsafeReadOnlyPtr(), srcArray.Length, target, (int*)dstArray.GetUnsafePtr(), out var dstCount);
        NoAllocHelpers.ResizeList(dst, dstCount);
        return dst;
    }

    [BurstCompile]
    public static unsafe void V256Filter(int* src, int srcCount, int target, int* dst, out int dstCount)
    {
        var dstPtr = dst;
        var alignedCount = srcCount & ~7;
        int i = 0;
        for (; i < alignedCount; i += 8)
        {
            var val = mm256_loadu_ps(&src[i]);
            var cmp = mm256_cmpeq_epi32(val, mm256_set1_epi32(target));
            var packed = SIMDHelpers.LeftPack8PS(cmp, val);
            mm256_storeu_ps(dstPtr, packed);
            dstPtr += popcnt_u32((uint)mm256_movemask_ps(cmp));
        }

        for (; i < srcCount; i++)
            if (src[i] == target)
                *dstPtr++ = src[i];

        dstCount = (int)(dstPtr - dst);
    }
}

