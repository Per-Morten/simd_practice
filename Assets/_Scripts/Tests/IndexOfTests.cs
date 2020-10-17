using System.Collections.Generic;
using NUnit.Framework;
using Unity.PerformanceTesting;
using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Runtime.CompilerServices;

namespace IndexOfTests
{
    public class Tests
    {
#if INCLUDE_PERFORMANCE_TESTS
        public class CombinedPerformance
        {
            List<int> _list;

            [OneTimeSetUp]
            public void Setup()
            {
                _list = TestUtility.CreateLargeList();
            }

            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public static void RunTest(List<int> list, int target)
            {
                var timings = new Action[]
                {
#if INCLUDE_SLOW_PERFORMANCE_TESTS && !UNITY_EDITOR
                    () => {TestUtility.Time($"Default For Loop ({list.Count})", () => { IndexOf.DefaultIndexOf(list, target); });},
                    () => {TestUtility.Time($"List.IndexOf ({list.Count})", () => { list.IndexOf(target); });},
                    () => {TestUtility.Time($"PointerIndexOf ({list.Count})", () => {IndexOf.PointerDefaultIndexOf(list, target); }); },
                    () => {TestUtility.Time($"PointerDefaultIndexOfThrows ({list.Count})", () => {IndexOf.PointerDefaultIndexOfThrows(list, target); }); },
#endif
                    () => {TestUtility.Time($"BurstDefaultIndexOf ({list.Count})", () => {IndexOf.BurstDefaultIndexOf(list, target); }); },
                    () => {TestUtility.Time($"SIMDIndexOf ({list.Count})", () => {IndexOf.SIMDIndexOf(list, target); }); },
                    () => {TestUtility.Time($"SIMDParallelIndexOf({list.Count})", () => {IndexOf.SIMDParallelIndexOf(list, target); }); },
                };

#if RANDOM_SHUFFLE_TESTS
                TestUtility.RandomShuffle(timings);
#endif
                foreach (var timing in timings)
                    timing();

                Debug.Log($"{new StackFrame(1).GetMethod().Name} finished");
            }

            [Test, Performance]
            public void SearchLargeListFindLast()
            {
                RunTest(_list, _list.Count - 1);
            }

            [Test, Performance]
            public void SearchLargeListFindMiddle()
            {
                RunTest(_list, _list.Count / 2);
            }

            [Test, Performance]
            public void SearchLargeListFindFirst()
            {
                RunTest(_list, 0);
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
                    list = new List<int>()
                    {
                        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                    };
                }

                [Test]
                public void ReturnFirstIndexOnDuplicate()
                {
                    var l = new List<int>()
                    {
                        0, 1, 1, 1, 1, 0, 2, 2, 2, 2
                    };

                    Assert.IsTrue(Func(l, 0) == 0);
                    Assert.IsTrue(Func(l, 1) == 1);
                    Assert.IsTrue(Func(l, 2) == 6);
                }

                [Test]
                public void ReturnsNegativeOneOnFail()
                {
                    Assert.IsTrue(Func(list, list.Count) == -1);
                }

                [Test]
                public void ReturnsCorrectValueAtBeginning()
                {
                    Assert.IsTrue(Func(list, 0) == 0);
                }

                [Test]
                public void ReturnsCorrectValueAtOnePastBeginning()
                {
                    Assert.IsTrue(Func(list, 1) == 1);
                }

                [Test]
                public void ReturnsCorrectValueAtEnd()
                {
                    Assert.IsTrue(Func(list, list.Count - 1) == list.Count - 1);
                }

                [Test]
                public void ReturnsCorrectValueInMiddle()
                {
                    var target = list.Count / 2;
                    Assert.IsTrue(Func(list, target) == target);
                }

                [Test]
                public void ReturnsNegativeOneIfTarget0IsNotInTheList()
                {
                    var target = 0;
                    var list = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
                    Assert.IsTrue(Func(list, target) == -1);
                }
            }

            public class DefaultIndexOf : GenericTests
            {
                public override void Setup()
                {
                    base.Setup();
                    Func = IndexOf.DefaultIndexOf;
                }
            }

            public class PointerDefaultIndexOf : GenericTests
            {
                public override void Setup()
                {
                    base.Setup();
                    Func = IndexOf.PointerDefaultIndexOf;
                }
            }

            public class PointerDefaultIndexOfThrows : GenericTests
            {
                public override void Setup()
                {
                    base.Setup();
                    Func = IndexOf.PointerDefaultIndexOf;
                }
            }

            public class BurstDefaultIndexOf : GenericTests
            {
                public override void Setup()
                {
                    base.Setup();
                    Func = IndexOf.BurstDefaultIndexOf;
                }
            }

            public class SIMDIndexOf : GenericTests
            {
                public override void Setup()
                {
                    base.Setup();
                    Func = IndexOf.SIMDIndexOf;
                }
            }

            public class ParallelSIMDIndexOf : GenericTests
            {
                public override void Setup()
                {
                    base.Setup();
                    Func = IndexOf.SIMDIndexOf;
                }
            }
        }
#endif
    }
}