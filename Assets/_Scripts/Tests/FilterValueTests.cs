using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.PerformanceTesting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FilterEqualsTests
{
    public class Tests
    {
#if INCLUDE_PERFORMANCE_TESTS
        [TestFixture]
        public class CombinedPerformance
        {
            List<int> everyOtherList;
            List<int> partitionedList;
            List<int> emptyList;
            List<int> consecutiveIntegerList;

            [OneTimeSetUp]
            public void Setup()
            {
                emptyList = new List<int>();
                consecutiveIntegerList = TestUtility.CreateLargeList();
                everyOtherList = TestUtility.CreateLargeAlternating01List();
                partitionedList = TestUtility.CreateLargeAlternating01List();
                partitionedList.Sort();
            }

            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public static void RunTest(List<int> list, int target)
            {
                var timings = new Action[]
                {
                    //() => {TestUtility.Time($"IEnumerableFilter ({list.Count})", () => {FilterValue.IEnumerableFilter(list, target); });},
                    //() => {TestUtility.Time($"ForLoopFilter ({list.Count})", () => {FilterValue.ForLoopFilter(list, target); }); },
                    //() => {TestUtility.Time($"ForLoopFilterCapacity ({list.Count})", () => {FilterValue.ForLoopFilter(list, target); }); },
                    () => {TestUtility.Time($"BurstFilter ({list.Count})", () => {FilterEquals.BurstFilter(list, target); }); },
                    () => {TestUtility.Time($"V128Filter ({list.Count})", () => {FilterEquals.V128Filter(list, target); }); },
                    () => {TestUtility.Time($"V256Filter ({list.Count})", () => {FilterEquals.V256Filter(list, target); }); },
                };

#if RANDOM_SHUFFLE_TESTS
                TestUtility.RandomShuffle(timings);
#endif
                foreach (var timing in timings)
                    timing();

                Debug.Log($"{new StackFrame(1).GetMethod().Name} finished");
            }

            [Test, Performance]
            public void FilterEmptyList()
            {
                RunTest(emptyList, 0);
            }

            [Test, Performance]
            public void FilterListWithNoTarget()
            {
                RunTest(consecutiveIntegerList, -1);
            }

            [Test, Performance]
            public void FilterListWith50PercentPartitioned()
            {
                RunTest(partitionedList, 0);
            }

            [Test, Performance]
            public void FilterListWith50PercentTargetAlternating()
            {
                RunTest(everyOtherList, 0);
            }
        }
#endif

#if INCLUDE_CORRECTNESS_TESTS
        public class Correctness
        {
            public abstract class GenericTests
            {
                //protected List<int> list;
                protected Func<List<int>, int, List<int>> Func;

                [OneTimeSetUp]
                virtual public void Setup()
                {
                //    list = new List<int>();
                //    for (int i = 0; i < 1000; i++)
                //        list.Add(i);
                }

                [Test]
                public void FilterEmptyListReturnsEmptyList()
                {
                    var src = new List<int>();
                    var res = Func(src, 0);
                    Assert.IsTrue(res.Count == 0);
                }

                [Test]
                public void FilterListNotContainingTargetReturnsEmptyList()
                {
                    var src = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    var res = Func(src, -1);
                    Assert.IsTrue(res.Count == 0);
                }

                [Test]
                public void FilterListWorksCorrectWhenTargetIsAtBeginningOfList()
                {
                    var src = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    var res = Func(src, 0);
                    Assert.IsTrue(res.Count == 1);
                    Assert.IsTrue(res[0] == 0);
                }

                [Test]
                public void FilterListWorksCorrectWhenTargetIsAtEndOfList()
                {
                    var src = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    var res = Func(src, 9);
                    Assert.IsTrue(res.Count == 1);
                    Assert.IsTrue(res[0] == 9);
                }

                [Test]
                public void FilterListHandlesMultipleCases()
                {
                    var src = new List<int>() { 0, 5, 2, 3, 4, 5, 5, 5, 8, 9 };
                    var res = Func(src, 5);
                    Assert.IsTrue(res.Count == 4);
                    Assert.IsTrue(res[0] == 5);
                }
            }

            [TestFixture]
            public class IEnumerableFilter : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterEquals.IEnumerableFilter;
                }
            }

            [TestFixture]
            public class ForLoopFilter : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterEquals.ForLoopFilter;
                }
            }

            [TestFixture]
            public class ForLoopFilterCapacity : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterEquals.ForLoopFilterCapacity;
                }
            }

            [TestFixture]
            public class BurstFilter : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterEquals.BurstFilter;
                }
            }

            [TestFixture]
            public class V128Filter : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterEquals.V128Filter;
                }
            }

            [TestFixture]
            public class V256Filter : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterEquals.V256Filter;
                }
            }
        }
#endif
    }
}