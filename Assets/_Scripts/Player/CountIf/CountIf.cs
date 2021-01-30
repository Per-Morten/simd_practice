using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;

[BurstCompile]
public static class CountIf
{
    public static int IEnumerableCountIf(List<int> list, int target)
    {
        return list.Count(x => x == target);
    }

    public static int ForLoopCountIf(List<int> list, int target)
    {
        int count = 0;
        for (int i = 0; i < list.Count; i++)
            if (list[i] == target)
                count++;
        return count;
    }

    public unsafe static int BurstForLoopCountIf(List<int> list, int target)
    {
        using (list.ViewAsNativeArray(out var array))
        {
            return InternalBurstForLoopCountIf((int*)array.GetUnsafeReadOnlyPtr(), array.Length, target);
        }
    }

    [BurstCompile]
    public unsafe static int InternalBurstForLoopCountIf(int* list, int count, int target)
    {
        int c = 0;
        for (int i = 0; i < count; i++)
            if (list[i] == target)
                c++;
        return c;
    }

    private static int RecursiveCountIf(List<int> list, int target, int idx)
    {
        return idx >= list.Count
            ? 0
            : ((list[idx] == target) ? 1 : 0) + RecursiveCountIf(list, target, idx + 1);
    }

    public static int RecursiveCountIf(List<int> list, int target)
    {
        return RecursiveCountIf(list, target, 0);
    }

    [BurstCompile]
    struct CountIfJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> Values;

        [ReadOnly]
        public int Target;

        [WriteOnly]
        public NativeReference<int> Result;

        public void Execute()
        {
            int count = 0;
            for (int i = 0; i < Values.Length; i++)
                if (Values[i] == Target)
                    count++;
            Result.Value = count;
        }
    }

    public static int JobifiedCountIf(List<int> list, int target)
    {
        using var _ = list.ViewAsNativeArray(out var array);
        using var count = new NativeReference<int>(Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        new CountIfJob
        {
            Target = target,
            Result = count,
            Values = array,
        }
        .Run();

        return count.Value;
    }

}

