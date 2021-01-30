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
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public static class FilterBetween
{
    public static List<int> IEnumerableFilter(List<int> list, int greaterThan, int lessThan)
    {
        return list.Where(x => x < lessThan && x > greaterThan).ToList();
    }

    public static List<int> ForLoopFilter(List<int> list, int greaterThan, int lessThan)
    {
        var dst = new List<int>(list.Count);
        for (int i = 0; i < list.Count; i++)
            if (list[i] < lessThan && list[i] > greaterThan)
                dst.Add(list[i]);
        return dst;
    }

    public static unsafe List<int> BurstFilter(List<int> list, int greaterThan, int lessThan)
    {
        var dst = new List<int>(list.Count);
        using var srcDispose = list.ViewAsNativeArray(out var srcArray);
        using var dstDispose = dst.ViewAsNativeArray(out var dstArray);
        BurstFilter((int*)srcArray.GetUnsafeReadOnlyPtr(), srcArray.Length, greaterThan, lessThan, (int*)dstArray.GetUnsafePtr(), out var dstCount);
        NoAllocHelpers.ResizeList(dst, dstCount);
        return dst;
    }

    [BurstCompile]
    private static unsafe void BurstFilter([NoAlias] int* src, int srcCount, int greaterThan, int lessThan, [NoAlias] int* dst, [NoAlias] out int dstCount)
    {
        var dstPtr = dst;
        for (int i = 0; i < srcCount; i++)
            if (src[i] < lessThan && src[i] > greaterThan)
                *dstPtr++ = src[i];

        dstCount = (int)(dstPtr - dst);
    }

    public static unsafe List<int> V128Filter(List<int> list, int greaterThan, int lessThan)
    {
        var dst = new List<int>(list.Count);
        using var srcDispose = list.ViewAsNativeArray(out var srcArray);
        using var dstDispose = dst.ViewAsNativeArray(out var dstArray);
        V128Filter((int*)srcArray.GetUnsafeReadOnlyPtr(), srcArray.Length, greaterThan, lessThan, (int*)dstArray.GetUnsafePtr(), out var dstCount);
        NoAllocHelpers.ResizeList(dst, dstCount);
        return dst;
    }

    [BurstCompile]
    private static unsafe void V128Filter([NoAlias] int* src, int srcCount, int greaterThan, int lessThan, [NoAlias] int* dst, [NoAlias] out int dstCount)
    {
        var dstPtr = dst;
        var alignedCount = srcCount & ~3;
        int i = 0;
        for (; i < alignedCount; i += 4)
        {
            var val = loadu_ps(&src[i]);
            var cmpLt = cmplt_epi32(val, set1_epi32(lessThan));
            var cmpGt = cmpgt_epi32(val, set1_epi32(greaterThan));
            var cmp = and_ps(cmpLt, cmpGt);
            var packed = SIMDHelpers.LeftPack4PS(cmp, val);
            storeu_ps(dstPtr, packed);
            dstPtr += popcnt_u32((uint)movemask_ps(cmp));
        }

        for (; i < srcCount; i++)
            if (src[i] < lessThan && src[i] > greaterThan)
                *dstPtr++ = src[i];

        dstCount = (int)(dstPtr - dst);
    }

    public static unsafe List<int> V256Filter(List<int> list, int greaterThan, int lessThan)
    {
        var dst = new List<int>(list.Count);
        using var srcDispose = list.ViewAsNativeArray(out var srcArray);
        using var dstDispose = dst.ViewAsNativeArray(out var dstArray);
        V256Filter((int*)srcArray.GetUnsafeReadOnlyPtr(), srcArray.Length, greaterThan, lessThan, (int*)dstArray.GetUnsafePtr(), out var dstCount);
        NoAllocHelpers.ResizeList(dst, dstCount);
        return dst;
    }

    [BurstCompile]
    private static unsafe void V256Filter([NoAlias] int* src, int srcCount, int greaterThan, int lessThan, [NoAlias] int* dst, [NoAlias] out int dstCount)
    {
        var dstPtr = dst;
        var alignedCount = srcCount & ~7;
        int i = 0;
        for (; i < alignedCount; i += 8)
        {
            var val = mm256_loadu_ps(&src[i]);
            var cmpLt = mm256_cmpgt_epi32(mm256_set1_epi32(lessThan), val);
            var cmpGt = mm256_cmpgt_epi32(val, mm256_set1_epi32(greaterThan));
            var cmp = mm256_and_ps(cmpLt, cmpGt);
            var packed = SIMDHelpers.LeftPack8PS(cmp, val);
            mm256_storeu_ps(dstPtr, packed);
            dstPtr += popcnt_u32((uint)mm256_movemask_ps(cmp));
        }

        for (; i < srcCount; i++)
            if (src[i] < lessThan && src[i] > greaterThan)
                *dstPtr++ = src[i];

        dstCount = (int)(dstPtr - dst);
    }

}

