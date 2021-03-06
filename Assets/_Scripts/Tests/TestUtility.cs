using System;
using System.Collections;
using System.Collections.Generic;
using Unity.PerformanceTesting;
using UnityEngine;

public static class TestUtility
{
    public static void Time(string name, Action action)
    {
#if PRECISE_TIMINGS
        Measure.Method(() =>
        {
            action();
        })
        .WarmupCount(100) // Old 100
        .MeasurementCount(100) // Old 100
        .SampleGroup(name)
        .IterationsPerMeasurement(25) // Old 25
        .Run();
#else
        Measure.Method(() =>
        {
            action();
        })
        .WarmupCount(2)
        .MeasurementCount(5)
        .SampleGroup(name)
        .IterationsPerMeasurement(2)
        .Run();
#endif
    }

    public static void TimeBasic(string name, Action action)
    {
        Measure.Method(() =>
        {
            action();
        })
        .WarmupCount(100) // Old 100
        .MeasurementCount(100) // Old 100
        .SampleGroup(name)
        .IterationsPerMeasurement(25) // Old 25
        .Run();
    }

    public static List<int> CreateLargeList()
    {
#if TEN_MILLION_LIST
        var list = new List<int>(10000000);
#elif ONE_MILLION_LIST
        var list = new List<int>(1000000);
#elif HUNDRED_THOUSAND_LIST
        var list = new List<int>(100000);
#else
        var list = new List<int>(10000);
#endif
        for (int i = 0; i < list.Capacity; i++)
            list.Add(i);
        return list;
    }

    public static List<int> CreateLargeAlternating01List()
    {
#if TEN_MILLION_LIST
        var list = new List<int>(10000000);
#elif ONE_MILLION_LIST
        var list = new List<int>(1000000);
#elif HUNDRED_THOUSAND_LIST
        var list = new List<int>(100000);
#else
        var list = new List<int>(10000);
#endif
        for (int i = 0; i < list.Capacity; i++)
            list.Add(i & 1);
        return list;
    }

    public static void RandomShuffle(Action[] actions)
    {
        var random = new System.Random(DateTime.Now.Millisecond);
        for (int i = 0; i < actions.Length; i++)
        {
            var idx = random.Next(0, actions.Length - 1);
            var tmp = actions[idx];
            actions[idx] = actions[i];
            actions[i] = tmp;
        }
    }
}
