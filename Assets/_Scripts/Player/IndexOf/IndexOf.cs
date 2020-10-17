using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

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

// TODO: Create Filtering algorithm https://gdcvault.com/play/1022248/SIMD-at-Insomniac-Games-How

public static class IndexOf
{
    public static int DefaultIndexOf(List<int> list, int target)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == target)
                return i;
        return -1;
    }

    public unsafe static int PointerDefaultIndexOf(List<int> list, int target)
    {
        var len = list.Count;
        fixed (int* p = NoAllocHelpers.ExtractArrayFromListT(list))
            for (int i = 0; i < len; i++)
                if (p[i] == target)
                    return i;
        return -1;
    }

    public unsafe static int PointerDefaultIndexOfThrows(List<int> list, int target)
    {
        var len = list.Count;
        fixed (int* p = NoAllocHelpers.ExtractArrayFromListT(list))
            for (int i = 0; i < len; i++)
            {
                if (i < 0 || i >= len)
                    throw new ArgumentOutOfRangeException();
                if (p[i] == target)
                    return i;
            }
        return -1;
    }

    public static unsafe int BurstDefaultIndexOf(List<int> list, int target)
    {
        int result = -1;
        using (list.ViewAsNativeArray(out var array))
            new DefaultIndexOfJob
            {
                List = array,
                Target = target,
                Result = &result,
            }
            .Run();
        return result;
    }

    public static unsafe int SIMDIndexOf(List<int> list, int target)
    {
        int result = -1;
        using (list.ViewAsNativeArray(out var array))
            new SIMDIndexOfJob
            {
                List = array,
                Target = target,
                Result = &result,
            }
            .Run();
        return result;
    }

    // Runs slightly slower than regular index of. 
    // should probably create a custom job for this.
    public static unsafe int SIMDParallelIndexOf(List<int> list, int target)
    {
        var workerCount = JobsUtility.JobWorkerCount + 1;
        var results = new NativeArray<int>(workerCount + 1, Allocator.TempJob);
        for (int i = 0; i < results.Length; i++)
            results[i] = -1;
        using (results)
        {
            var alignedCases = list.Count / workerCount;
            var stragglers = list.Count % workerCount;
            var offsets = new List<int>();

            using (list.ViewAsNativeArray(out var array))
            {
                JobHandle handle = default;
                int i = 0;
                int count = 0;
                for (; count < workerCount;)
                {
                    var subArray = array.GetSubArray(i, alignedCases);
                    handle = new SIMDIndexOfJob
                    {
                        List = subArray,
                        Result = (int*)results.GetUnsafePtr() + count++,
                        Target = target,
                    }
                    .Schedule(handle);
                    offsets.Add(i);
                    i += alignedCases;
                }

                handle = new SIMDIndexOfJob
                {
                    List = array.GetSubArray(i, stragglers),
                    Result = (int*)results.GetUnsafePtr() + count++,
                    Target = target,
                }
                .Schedule(handle);
                offsets.Add(i);

                handle.Complete();
            }

            for (int i = 0; i < results.Length; i++)
                if (results[i] != -1)
                    return offsets[i] + results[i];
            return -1;
        }
        //return result;
    }

    [BurstCompile(CompileSynchronously = true)]
    public unsafe struct DefaultIndexOfJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> List;

        [ReadOnly]
        public int Target;

        [NativeDisableUnsafePtrRestriction, WriteOnly, NoAlias]
        public int* Result;

        public void Execute()
        {
            for (int i = 0; i < List.Length; i++)
                if (List[i] == Target)
                {
                    *Result = i;
                    break;
                }
        }
    }

    [BurstCompile(CompileSynchronously = true)]
    public unsafe struct SIMDIndexOfJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> List;

        [ReadOnly]
        public int Target;

        [NativeDisableUnsafePtrRestriction, WriteOnly, NoAlias]
        public int* Result;

        public void Execute()
        {
            var len = List.Length;
            var res = -1;
            var target = mm256_set1_epi32(Target);
            var alignedCount = List.Length & ~7;
            var ptr = (int*)List.GetUnsafeReadOnlyPtr();
            int i = 0;
            for (; ; )
            {
                if (res != -1 || i >= alignedCount)
                    break;
                res = IndexOf(*(v256*)(ptr + i), target, i);
                i += 8;
            }

            if (res == -1)
            {
                var loadmask = new v256(i + 0, i + 1, i + 2, i + 3, i + 4, i + 5, i + 6, i + 7);
                var length = new v256(List.Length);
                var m = mm256_cmp_ps(loadmask, length, (int)CMP.LT_OQ);
                var values = mm256_maskload_epi32(ptr + i, m);
                res = IndexOf(values, target, i);
            }

            if (res >= len)
                res = -1;

            *Result = res;

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(v256 values, v256 target, int i)
        {
            var cmp = mm256_cmpeq_epi32(values, target);
            var mask = mm256_movemask_ps(cmp);

            // All of this could have been avoided if C# allowed conversion from bool to int :/
            var tmp = (1 + (mask >> 31) - (-mask >> 31));
            var isZero = tmp & 1;
            var isPositive = tmp >> 1;
            return -1 * isZero + (i + Unity.Mathematics.math.tzcnt(mask)) * isPositive;
        }
    }
}
