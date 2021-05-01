using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.PerformanceTesting;
using UnityEngine;
using Debug = UnityEngine.Debug;
using BMCurves = burningmime.curves;
using OPCurves = CurvesOptimized;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using CurvesOptimized;
using System.Threading;

namespace CurvesTests
{
    public class Tests
    {
        public const float DistanceBetweenPoints = 0.01f;
        public const float CurveSplitError = 0.01f;

#if INCLUDE_PERFORMANCE_TESTS
        [TestFixture]
        public class CombinedPerformance
        {
            public int PointCount = 2500;
            public List<Vector2> Points;
            public uint Seed;

            [OneTimeSetUp]
            public void Setup()
            {
                Points = new List<Vector2>(PointCount);
                Seed = (uint)1;
                var random = new Unity.Mathematics.Random(Seed);
                //for (int i = 0; i < PointCount; i++)
                //    Points.Add(random.NextFloat2());
                for (int i = 0; i < PointCount; i++)
                {
                    Points.Add(new Vector2(i / 100.0f, Mathf.Sin(i / 10.0f)));
                }
            }

            static Unity.Profiling.ProfilerMarker OPTotal = new Unity.Profiling.ProfilerMarker("OP Total");
            static Unity.Profiling.ProfilerMarker OPAddPoints = new Unity.Profiling.ProfilerMarker("OP AddPoints");

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void RunWholeTest(List<Vector2> list, uint seed)
            {
                var timings = new Action[]
                {
                    //() =>
                    //{
                    //    TestUtility.Time($"BMCurves {seed}: {list.Count}",
                    //        () =>
                    //        {
                    //            var s = new BMCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
                    //            for (int i = 0; i < list.Count; i++)
                    //                s.AddPoint(list[i]);
                    //        });
                    //},

                    () =>
                    {
                        TestUtility.Time($"OPCurves {seed}: {list.Count}",
                            () =>
                            {
                                using var total = OPTotal.Auto();
                                var s = new OPCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
                                using var addPoints = OPAddPoints.Auto();
                                for (int i = 0; i < list.Count; i++)
                                    s.AddPoint(list[i]);
                            });
                    },

                    //() => {TestUtility.Time($"IEnumerableFilter ({list.Count})", () => {FilterBetween.IEnumerableFilter(list, greaterThan, lessThan); });},
                };

#if RANDOM_SHUFFLE_TESTS
                TestUtility.RandomShuffle(timings);
#endif
                foreach (var timing in timings)
                    timing();

                Debug.Log($"{new StackFrame(1).GetMethod().Name} finished");
            }

            public static void CopyTo<T>(List<T> dst, List<T> src) where T : unmanaged
            {
                dst.Capacity = src.Count;
                NoAllocHelpers.ResizeList(dst, src.Count);
                using (dst.ViewAsNativeArray(out var dstArray))
                using (src.ViewAsNativeArray(out var srcArray))
                    dstArray.CopyFrom(srcArray);
            }

            public static List<T> CreateCopyOf<T>(List<T> src) where T : unmanaged
            {
                var dst = new List<T>(src.Capacity);
                CopyTo(dst, src);
                return dst;
            }

            static Unity.Profiling.ProfilerMarker RunGenerateBezierTestMarker = new Unity.Profiling.ProfilerMarker("RunGenerateBezierTest");
            public static unsafe void RunGenerateBezierTest(List<Vector2> list, uint seed)
            {
                var watch = Stopwatch.StartNew();
                var original = new OPCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
                for (int i = 0; i < list.Count; i++)
                    original.AddPoint(list[i]);

                Debug.Log($"Generated curve ({list.Count} points) in: {watch.Elapsed.TotalSeconds} seconds");

                var tanL = original.GetLeftTangent(original._pts.Count);
                var tanR = original.GetRightTangent(0);

                var optimizedPts = CreateCopyOf(original._pts);
                var optimizedU = CreateCopyOf(original._u);

                //Debug.Assert(original._pts.Count <= original._u.Count);

                const int WarmupCount = 25; // 100
                const int MeasurementCount = 100; // 100
                const int IterationsPerMeasurement = 25; // 25

#if false
                Measure.Method(() =>
                {
                    using (optimizedPts.ViewAsNativeArray(out var pts))
                    using (optimizedU.ViewAsNativeArray(out var u))
                        OptimizedHelpers.GenerateBezierStandard(0, original._pts.Count, tanL, tanR, (Vector2*)pts.GetUnsafePtr(), (float*)u.GetUnsafePtr(), out var b);
                })
                .SetUp(() =>
                {
                    CopyTo(optimizedPts, original._pts);
                    CopyTo(optimizedU, original._u);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(IterationsPerMeasurement)
                .SampleGroup("GenerateBezierStandard")
                .Run();

#else
                Measure.Method(() =>
                {
                    using (optimizedPts.ViewAsNativeArray(out var pts))
                    using (optimizedU.ViewAsNativeArray(out var u))
                        OptimizedHelpers.GenerateBezierUnityMathematics(0, original._pts.Count, tanL, tanR, (Vector2*)pts.GetUnsafePtr(), (float*)u.GetUnsafePtr(), out var b);
                })
                .SetUp(() =>
                {
                    CopyTo(optimizedPts, original._pts);
                    CopyTo(optimizedU, original._u);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(IterationsPerMeasurement)
                .SampleGroup("GenerateBezierUnityMathematics")
                .Run();

                Measure.Method(() =>
                {
                    using (optimizedPts.ViewAsNativeArray(out var pts))
                    using (optimizedU.ViewAsNativeArray(out var u))
                        OptimizedHelpers.GenerateBezierV128V0(0, original._pts.Count, tanL, tanR, (Vector2*)pts.GetUnsafePtr(), (float*)u.GetUnsafePtr(), out var b);
                })
                .SetUp(() =>
                {
                    CopyTo(optimizedPts, original._pts);
                    CopyTo(optimizedU, original._u);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(IterationsPerMeasurement)
                .SampleGroup("GenerateBezierV128V0")
                .Run();

                Measure.Method(() =>
                {
                    using (optimizedPts.ViewAsNativeArray(out var pts))
                    using (optimizedU.ViewAsNativeArray(out var u))
                        OptimizedHelpers.GenerateBezierV128V1(0, original._pts.Count, tanL, tanR, (Vector2*)pts.GetUnsafePtr(), (float*)u.GetUnsafePtr(), out var b);
                })
                .SetUp(() =>
                {
                    CopyTo(optimizedPts, original._pts);
                    CopyTo(optimizedU, original._u);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(IterationsPerMeasurement)
                .SampleGroup("GenerateBezierV128V1")
                .Run();

#if false
                Measure.Method(() =>
                {
                    using (optimizedPts.ViewAsNativeArray(out var pts))
                    using (optimizedU.ViewAsNativeArray(out var u))
                        OptimizedHelpers.GenerateBezierV256V1(0, original._pts.Count, tanL, tanR, (Vector2*)pts.GetUnsafePtr(), (float*)u.GetUnsafePtr(), out var b);
                })
                .SetUp(() =>
                {
                    CopyTo(optimizedPts, original._pts);
                    CopyTo(optimizedU, original._u);
                })
                .WarmupCount(WarmupCount)
                .MeasurementCount(MeasurementCount)
                .IterationsPerMeasurement(IterationsPerMeasurement)
                .SampleGroup("GenerateBezierV256V1")
                .Run();
#endif
#endif

                Debug.Log($"{new StackFrame(1).GetMethod().Name} finished");
            }

            [Test, Performance]
            public void AddPointsTest()
            {
                RunWholeTest(Points, Seed);
            }

            [Test, Performance]
            public void GenerateBezierBenchmark()
            {
                RunGenerateBezierTest(Points, Seed);
            }

            [Test]
            public void RepeatableSingleLargeTest()
            {
                var points = new List<Vector2>(PointCount);
                var seed = (uint)1;
                var random = new Unity.Mathematics.Random(seed);
                for (int i = 0; i < PointCount; i++)
                    points.Add(random.NextFloat2());

                using var total = OPTotal.Auto();
                var s = new OPCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
                using var addPoints = OPAddPoints.Auto();
                for (int i = 0; i < points.Count; i++)
                    s.AddPoint(points[i]);
            }
        }
#endif

#if INCLUDE_CORRECTNESS_TESTS
            public class Correctness
        {
            [TestFixture]
            public class CurvesTests
            {
                [OneTimeSetUp]
                virtual public void Setup()
                {
                }

                [Test]
                [Explicit("Test for specific fail case which I don't want to deal with atm, so keeping it around here, but will have fix for it at some point?")] // TODO: Make this pass
                public void FailingAddTrianglePoints()
                {
                    //var randomSeed = (uint)Mathf.Max(1, System.DateTime.Now.Millisecond);
                    var randomSeed = (uint)886;
                    try
                    {
                        using var d = new Unity.Profiling.ProfilerMarker("AddTrianglePoints").Auto();
                        var points = new List<Vector2>();
                        var randomPoints = new Unity.Mathematics.Random(randomSeed);
                        for (int i = 0; i < 500; i++)
                            points.Add(randomPoints.NextFloat2());

                        var desiredSpline = new BMCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
                        var optimizedSpline = new OPCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
                        for (int i = 0; i < points.Count; i++)
                        {
                            var desiredRes = desiredSpline.AddPoint(points[i]);
                            var optimizedRes = optimizedSpline.AddPoint(points[i]);
                            Assert.IsTrue(desiredRes.FirstChangedIndex == optimizedRes.FirstChangedIndex);
                        }

                        for (int i = 0; i < desiredSpline.Curves.Count; i++)
                        {
                            try
                            {
                                Assert.IsTrue(Equals(desiredSpline.Curves[i], optimizedSpline.Curves[i]));

                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Seed: {randomSeed}");
                        throw e;
                    }
                }

                [Test]
                public void AddTrianglePoints()
                {
                    var randomSeed = (uint)Mathf.Max(1, System.DateTime.Now.Millisecond);
                    try
                    {
                        using var d = new Unity.Profiling.ProfilerMarker("AddTrianglePoints").Auto();
                        var points = new List<Vector2>();
                        var randomPoints = new Unity.Mathematics.Random(randomSeed);
                        for (int i = 0; i < 500; i++)
                            points.Add(randomPoints.NextFloat2());

                        var desiredSpline = new BMCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
                        var optimizedSpline = new OPCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
                        for (int i = 0; i < points.Count; i++)
                        {
                            var desiredRes = desiredSpline.AddPoint(points[i]);
                            var optimizedRes = optimizedSpline.AddPoint(points[i]);
                            Assert.IsTrue(desiredRes.FirstChangedIndex == optimizedRes.FirstChangedIndex);
                        }

                        for (int i = 0; i < desiredSpline.Curves.Count; i++)
                        {
                            try
                            {
                                Assert.IsTrue(Equals(desiredSpline.Curves[i], optimizedSpline.Curves[i]));

                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Seed: {randomSeed}");
                        throw e;
                    }
                }

                [Test]
                public unsafe void GenerateBezier()
                {
                    var points = new List<Vector2>(100);
                    var seed = (uint)1;
                    var random = new Unity.Mathematics.Random(seed);
                    for (int i = 0; i < 100; i++)
                        points.Add(random.NextFloat2());

                    var original = new OPCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
                    for (int i = 0; i < points.Count; i++)
                        original.AddPoint(points[i]);

                    var tanL = original.GetLeftTangent(original._pts.Count);
                    var tanR = original.GetRightTangent(original._first);

                    var optimizedPts = new List<Vector2>();
                    for (int i = 0; i < original._pts.Count; i++)
                        optimizedPts.Add(original._pts[i]);

                    var optimizedU = new List<float>();
                    for (int i = 0; i < original._u.Count; i++)
                        optimizedU.Add(original._u[i]);

                    CubicBezier originalBezier;
                    using (original._pts.ViewAsNativeArray(out var pts))
                    using (original._u.ViewAsNativeArray(out var u))
                        OptimizedHelpers.GenerateBezierStandard(0, original._pts.Count, tanL, tanR, (Vector2*)pts.GetUnsafePtr(), (float*)u.GetUnsafePtr(), out originalBezier);

                    CubicBezier optimizedBezier;
                    using (optimizedPts.ViewAsNativeArray(out var pts))
                    using (optimizedU.ViewAsNativeArray(out var u))
                        OptimizedHelpers.GenerateBezierV256V1(0, original._pts.Count, tanL, tanR, (Vector2*)pts.GetUnsafePtr(), (float*)u.GetUnsafePtr(), out optimizedBezier);

                    for (int i = 0; i < original._pts.Count; i++)
                        Assert.IsTrue(math.all(aprox(original._pts[i], optimizedPts[i])));

                    for (int i = 0; i < original._u.Count; i++)
                        Assert.IsTrue(aprox(original._u[i], optimizedU[i]));

                    Assert.IsTrue(Equals(originalBezier, optimizedBezier));
                }

                static bool Equals(OPCurves.CubicBezier a, OPCurves.CubicBezier b)
                {

                    var r0 = math.all(aprox(a.p0, b.p0)) || (math.all(math.isnan(a.p0)) && math.all(math.isnan(b.p0)));
                    var r1 = math.all(aprox(a.p1, b.p1)) || (math.all(math.isnan(a.p1)) && math.all(math.isnan(b.p1)));
                    var r2 = math.all(aprox(a.p2, b.p2)) || (math.all(math.isnan(a.p2)) && math.all(math.isnan(b.p2)));
                    var r3 = math.all(aprox(a.p3, b.p3)) || (math.all(math.isnan(a.p3)) && math.all(math.isnan(b.p3)));

                    return r0 &&
                           r1 &&
                           r2 &&
                           r3;
                }

                static bool Equals(BMCurves.CubicBezier a, OPCurves.CubicBezier b)
                {
                    var r0 = math.all(aprox(a.p0, b.p0));
                    var r1 = math.all(aprox(a.p1, b.p1));
                    var r2 = math.all(aprox(a.p2, b.p2));
                    var r3 = math.all(aprox(a.p3, b.p3));

                    return math.all(aprox(a.p0, b.p0)) &&
                           math.all(aprox(a.p1, b.p1)) &&
                           math.all(aprox(a.p2, b.p2)) &&
                           math.all(aprox(a.p3, b.p3));
                }

                static bool aprox(float a, float b)
                {
                    return math.abs(a - b) < 0.0025f;
                }

                static bool2 aprox(float2 a, float2 b)
                {
                    return math.abs(a - b) < 0.0025f;
                }
            }
        }
#endif
    }
}