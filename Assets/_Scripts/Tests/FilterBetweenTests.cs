using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.PerformanceTesting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace FilterBetweenTests
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
            public static void RunTest(List<int> list, int greaterThan, int lessThan)
            {
                var timings = new Action[]
                {
                    //() => {TestUtility.Time($"IEnumerableFilter ({list.Count})", () => {FilterBetween.IEnumerableFilter(list, greaterThan, lessThan); });},
                    //() => {TestUtility.Time($"ForLoopFilter ({list.Count})", () => {FilterBetween.ForLoopFilter(list, greaterThan, lessThan); }); },
                    //() => {TestUtility.Time($"BurstFilter ({list.Count})", () => {FilterBetween.BurstFilter(list, greaterThan, lessThan); }); },
                    () => {TestUtility.Time($"V128Filter ({list.Count})", () => {FilterBetween.V128Filter(list, greaterThan, lessThan); }); },
                    () => {TestUtility.Time($"V256Filter ({list.Count})", () => {FilterBetween.V256Filter(list, greaterThan, lessThan); }); },
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
                RunTest(emptyList, 0, 1);
            }

            [Test, Performance]
            public void FilterListWithNoTarget()
            {
                RunTest(consecutiveIntegerList, -2, -1);
            }

            [Test, Performance]
            public void FilterListWith50PercentPartitioned()
            {
                RunTest(partitionedList, -1, 1);
            }

            [Test, Performance]
            public void FilterListWith50PercentTargetAlternating()
            {
                RunTest(everyOtherList, -1, 1);
            }
        }
#endif

#if INCLUDE_CORRECTNESS_TESTS
        public class Correctness
        {
            public abstract class GenericTests
            {
                //protected List<int> list;
                protected Func<List<int>, int, int, List<int>> Func;

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
                    var res = Func(src, 0, 1);
                    Assert.IsTrue(res.Count == 0);
                }

                [Test]
                public void FilterListNotContainingTargetReturnsEmptyList()
                {
                    var src = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    var res = Func(src, -1, -3);
                    Assert.IsTrue(res.Count == 0);
                }

                [Test]
                public void FilterListWorksCorrectWhenTargetIsAtBeginningOfList()
                {
                    var src = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    var res = Func(src, -1, 1);
                    Assert.IsTrue(res.Count == 1);
                    Assert.IsTrue(res[0] == 0);
                }

                [Test]
                public void FilterListWorksCorrectWhenTargetIsAtEndOfList()
                {
                    var src = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    var res = Func(src, 8, 10);
                    Assert.IsTrue(res.Count == 1);
                    Assert.IsTrue(res[0] == 9);
                }

                [Test]
                public void FilterListHandlesMultipleCases()
                {
                    var src = new List<int>() { 0, 5, 2, 3, 4, 5, 5, 5, 8, 9 };
                    var res = Func(src, 4, 6);
                    Assert.IsTrue(res.Count == 4);
                    for (int i = 0; i < res.Count; i++)
                        Assert.IsTrue(res[i] == 5);
                }
            }

            [TestFixture]
            public class IEnumerableFilter : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterBetween.IEnumerableFilter;
                }
            }

            [TestFixture]
            public class ForLoopFilter : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterBetween.ForLoopFilter;
                }
            }

            [TestFixture]
            public class BurstFilter : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterBetween.BurstFilter;
                }
            }

            [TestFixture]
            public class V128Filter : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterBetween.V128Filter;
                }
            }

            [TestFixture]
            public class V256Filter : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = FilterBetween.V256Filter;
                }
            }
        }
#endif
    }
}