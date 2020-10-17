using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

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
using System.Runtime.InteropServices;
using System.Threading;

public static class Contains
{
    public static bool DefaultReturningContains<T>(List<T> values, T target) where T : IComparable<T>
    {
        for (int i = 0; i < values.Count; i++)
            if (target.CompareTo(values[i]) == 0)
                return true;
        return false;
    }

    public static bool DefaultNonReturningContains<T>(List<T> values, T target) where T : IComparable<T>
    {
        var found = false;
        for (int i = 0; i < values.Count; i++)
            found |= target.CompareTo(values[i]) == 0;
        return found;
    }

    public static unsafe bool BurstNonReturningContains(List<int> values, int target)
    {
        bool result = false;
        using (values.ViewAsNativeArray(out var array))
        {
            new BurstNonReturningContainsJob<int>
            {
                List = array,
                Result = &result,
                Target = target,
            }
            .Schedule()
            .Complete();
        }
        return result;
    }

    public static unsafe bool BurstReturningContains(List<int> values, int target)
    {
        bool result = false;
        using (values.ViewAsNativeArray(out var array))
        {
            new BurstReturningContainsJob<int>
            {
                List = array,
                Result = &result,
                Target = target,
            }
            .Schedule()
            .Complete();
        }
        return result;
    }

    public static unsafe bool SIMDNonReturningContains(List<int> values, int target)
    {
        bool result = false;
        using (values.ViewAsNativeArray(out var array))
        {
            new SIMDNonReturningContainsJob
            {
                List = array,
                Result = &result,
                Target = target,
            }
            .Schedule()
            .Complete();
        }
        return result;
    }

    public static unsafe bool SIMDReturningContains(List<int> values, int target)
    {
        bool result = false;
        using (values.ViewAsNativeArray(out var array))
        {
            new SIMDReturningContainsJob
            {
                List = array,
                Result = &result,
                Target = target,
            }
            .Schedule()
            .Complete();
        }
        return result;
    }

    public static unsafe bool SIMDParallelReturningContains(List<int> values, int target)
    {
        var participatingThreads = JobsUtility.JobWorkerCount + 1;
        long result = 0;

        var itemsPerThread = values.Count / participatingThreads;
        var begin = new NativeArray<int>(participatingThreads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        begin[0] = 0;
        for (int i = 1; i < participatingThreads; i++)
            begin[i] = begin[i - 1] + itemsPerThread;

        var end = new NativeArray<int>(participatingThreads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        end[participatingThreads - 1] = values.Count;
        for (int i = 0; i < participatingThreads - 1; i++)
            end[i] = begin[i + 1];

        using (values.ViewAsNativeArray(out var array))
        {
            new SIMDParallelReturningContainsJob
            {
                Begin = begin,
                End = end,
                List = array,
                Result = &result,
                Target = target,
            }
            .Schedule(participatingThreads, 1)
            .Complete();
        }

        end.Dispose();
        begin.Dispose();
        return result != 0;
    }

    public static unsafe bool SIMDParallelUnrolledReturningContains(List<int> values, int target)
    {
        var participatingThreads = JobsUtility.JobWorkerCount + 1;
        long result = 0;

        var itemsPerThread = values.Count / participatingThreads;
        var begin = new NativeArray<int>(participatingThreads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        begin[0] = 0;
        for (int i = 1; i < participatingThreads; i++)
            begin[i] = begin[i - 1] + itemsPerThread;

        var end = new NativeArray<int>(participatingThreads, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        end[participatingThreads - 1] = values.Count;
        for (int i = 0; i < participatingThreads - 1; i++)
            end[i] = begin[i + 1];

        using (values.ViewAsNativeArray(out var array))
        {
            new SIMDParallelUnrolledReturningContainsJob
            {
                Begin = begin,
                End = end,
                List = array,
                Result = &result,
                Target = target,
            }
            .Schedule(participatingThreads, 1)
            .Complete();
        }

        end.Dispose();
        begin.Dispose();
        return result != 0;
    }

    [BurstCompile]
    public unsafe struct BurstNonReturningContainsJob<T> : IJob where T : unmanaged, IEquatable<T>
    {
        [ReadOnly]
        public NativeArray<T> List;

        [ReadOnly]
        public T Target;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public bool* Result;

        public void Execute()
        {
            var found = false;
            for (int i = 0; i < List.Length; i++)
                found |= Target.Equals(List[i]);
            *Result = found;
        }
    }

    [BurstCompile]
    public unsafe struct BurstReturningContainsJob<T> : IJob where T : unmanaged, IEquatable<T>
    {
        [ReadOnly]
        public NativeArray<T> List;

        [ReadOnly]
        public T Target;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public bool* Result;

        public void Execute()
        {
            for (int i = 0; i < List.Length; i++)
                if (Target.Equals(List[i]))
                {
                    *Result = true;
                    break;
                }
        }
    }

    [BurstCompile]
    public unsafe struct SIMDNonReturningContainsJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> List;

        [ReadOnly]
        public int Target;
         
        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public bool* Result;

        public void Execute()
        {
            var target = mm256_set1_epi32(Target);
            var src = (int*)List.GetUnsafeReadOnlyPtr();
            var alignedCount = List.Length & ~7;
            v256 vMask = mm256_setzero_si256();
            int i = 0;
            for (; i < alignedCount; i += 8)
            {
                var res = mm256_cmpeq_epi32(*(v256*)(src + i), target);
                vMask = mm256_or_si256(vMask, res);
            }

            var mask = mm256_movemask_ps(vMask);
            for (; i < List.Length; i++)
            {
                mask |= (src[i] == Target) ? 1 : 0;
            }

            *Result = mask != 0;
        }
    }

    [BurstCompile]
    public unsafe struct SIMDReturningContainsJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> List;

        [ReadOnly]
        public int Target;

        [WriteOnly, NativeDisableUnsafePtrRestriction]
        public bool* Result;

        public void Execute()
        {
            var target = mm256_set1_epi32(Target);
            var src = (int*)List.GetUnsafeReadOnlyPtr();
            var alignedCount = List.Length & ~7;
            int i = 0;
            for (; i < alignedCount; i += 8)
            {
                var res = mm256_cmpeq_epi32(*(v256*)(src + i), target);
                if (mm256_movemask_ps(res) != 0)
                {
                    *Result = true;
                    return;
                }
            }

            for (; i < List.Length; i++)
            {
                if (src[i] == Target)
                {
                    *Result = true;
                    return;
                }
            }
        }
    }

    [BurstCompile]
    public unsafe struct SIMDParallelReturningContainsJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int> List;

        [ReadOnly]
        public NativeArray<int> Begin;

        [ReadOnly]
        public NativeArray<int> End;

        [ReadOnly]
        public int Target;

        [NativeDisableUnsafePtrRestriction]
        public long* Result;

        public void Execute(int index)
        {
            int begin = Begin[index];
            int end = End[index];
            int count = end - begin;
            int alignedCount = count & ~7;
            v256 target = mm256_set1_epi32(Target);
            
            int* ptr = (int*)List.GetUnsafeReadOnlyPtr() + begin;
            int i = 0;
            while (Interlocked.Read(ref UnsafeUtility.AsRef<long>(Result)) == 0 && i < alignedCount)
            {
                var res = mm256_cmpeq_epi32(*(v256*)(ptr + i), target);
                if (mm256_movemask_ps(res) != 0)
                {
                    Interlocked.Exchange(ref UnsafeUtility.AsRef<long>(Result), 1);
                    return;
                }
                i += 8;
            }

            while (Interlocked.Read(ref UnsafeUtility.AsRef<long>(Result)) == 0 && i < count)
                if (ptr[i++] == Target)
                {
                    Interlocked.Exchange(ref UnsafeUtility.AsRef<long>(Result), 1);
                    return;
                }
        }
    }

    [BurstCompile]
    public unsafe struct SIMDParallelUnrolledReturningContainsJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int> List;

        [ReadOnly]
        public NativeArray<int> Begin;

        [ReadOnly]
        public NativeArray<int> End;

        [ReadOnly]
        public int Target;

        [NativeDisableUnsafePtrRestriction]
        public long* Result;

        public void Execute(int index)
        {
            int begin = Begin[index];
            int end = End[index];
            int count = end - begin;
            int alignedCount = count & ~31;
            v256 target = mm256_set1_epi32(Target);

            int* ptr = (int*)List.GetUnsafeReadOnlyPtr() + begin;
            int i = 0;
            while (Interlocked.Read(ref UnsafeUtility.AsRef<long>(Result)) == 0 && i < alignedCount)
            {
                var r32_0 = mm256_cmpeq_epi32(*(v256*)(ptr + i + 0), target);
                var r32_1 = mm256_cmpeq_epi32(*(v256*)(ptr + i + 8), target);
                var r16_0 = mm256_packs_epi32(r32_0, r32_1);
                var r32_2 = mm256_cmpeq_epi32(*(v256*)(ptr + i + 16), target);
                var r32_3 = mm256_cmpeq_epi32(*(v256*)(ptr + i + 24), target);
                var r16_1 = mm256_packs_epi32(r32_2, r32_3);
                var r8_0 = mm256_packs_epi16(r16_0, r16_1);
                if (mm256_movemask_epi8(r8_0) != 0)
                {
                    Interlocked.Exchange(ref UnsafeUtility.AsRef<long>(Result), 1);
                    return;
                }
                i += 32;
            }

            while (Interlocked.Read(ref UnsafeUtility.AsRef<long>(Result)) == 0 && i < count)
                if (ptr[i++] == Target)
                {
                    Interlocked.Exchange(ref UnsafeUtility.AsRef<long>(Result), 1);
                    return;
                }
        }
    }
}
