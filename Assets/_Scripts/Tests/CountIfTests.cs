using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.PerformanceTesting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CountIfTests
{
    public class Tests
    {
#if INCLUDE_PERFORMANCE_TESTS
        [TestFixture]
        public class CombinedPerformance
        {
            List<int> everyOtherList;
            List<int> partitionedList;

            [OneTimeSetUp]
            public void Setup()
            {
                everyOtherList = TestUtility.CreateLargeAlternating01List();
                partitionedList = TestUtility.CreateLargeAlternating01List();
                partitionedList.Sort();
            }

            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public static void RunTest(List<int> list, int target)
            {
                var timings = new Action[]
                {
                    //() => {TestUtility.Time($"IEnumerableCountIf ({list.Count})", () => {CountIf.IEnumerableCountIf(list, target); });},
                    //() => {TestUtility.Time($"ForLoopCountIf ({list.Count})", () => {CountIf.ForLoopCountIf(list, target); }); },
                    () => {TestUtility.Time($"BurstForLoopCountIf ({list.Count})", () => {CountIf.BurstForLoopCountIf(list, target); }); },
                    () => {TestUtility.Time($"JobifiedCountIf ({list.Count})", () => {CountIf.JobifiedCountIf(list, target); }); },
                    //() => {TestUtility.Time($"RecursiveCountIf ({list.Count})", () => {CountIf.RecursiveCountIf(list, target); }); }, // Not supported, as C# doesn't allow for tail call optimization needed to avoid stack overflow
                };

#if RANDOM_SHUFFLE_TESTS
                TestUtility.RandomShuffle(timings);
#endif
                foreach (var timing in timings)
                    timing();

                Debug.Log($"{new StackFrame(1).GetMethod().Name} finished");
            }

            [Test, Performance]
            public void SumEveryOther()
            {
                RunTest(everyOtherList, 0);
            }

            [Test, Performance]
            public void SumPartitioned()
            {
                RunTest(partitionedList, 0);
            }
        }
#endif

#if INCLUDE_CORRECTNESS_TESTS
        public class Correctness
        {
            public abstract class GenericTests
            {
                protected List<int> list;
                protected Func<List<int>, int, int> Func;

                [OneTimeSetUp]
                virtual public void Setup()
                {
                    list = new List<int>();
                    for (int i = 0; i < 1000; i++)
                        list.Add(i);
                }

                [Test]
                public void SumIs0InEmptyList()
                {
                    Assert.IsTrue(Func(new List<int>(), 0) == 0);
                }

                [Test]
                public void SumLookingFor0IsCorrect()
                {
                    Assert.IsTrue(Func(list, 0) == 1);
                }

                [Test]
                public void SumIsCorrect()
                {
                    var l = new List<int>() { 0, 1, 1, 2, 5, 1 };
                    Assert.IsTrue(Func(l, 1) == 3);
                }
            }

            [TestFixture]
            public class IEnumerableCountIf : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = CountIf.IEnumerableCountIf;
                }
            }

            [TestFixture]
            public class ForLoopCountIf : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = CountIf.ForLoopCountIf;
                }
            }

            [TestFixture]
            public class RecursiveCountIf : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = CountIf.RecursiveCountIf;
                }
            }

            [TestFixture]
            public class JobifiedCountIf : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = CountIf.JobifiedCountIf;
                }
            }

            [TestFixture]
            public class BurstCompiledCountIf : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = CountIf.BurstForLoopCountIf;
                }
            }
        }
#endif
    }
}