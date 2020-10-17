using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.PerformanceTesting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ContainsTests
{
    public class Tests
    {
#if INCLUDE_PERFORMANCE_TESTS
        [TestFixture]
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
                    () => {TestUtility.Time($"DefaultReturningContains ({list.Count})", () => { Contains.DefaultReturningContains(list, target); });},
                    () => {TestUtility.Time($"DefaultNonReturningContains ({list.Count})", () => {Contains.DefaultNonReturningContains(list, target); }); },
                    () => {TestUtility.Time($"BurstNonReturningContains ({list.Count})", () => {Contains.BurstNonReturningContains(list, target); }); },
                    () => {TestUtility.Time($"SIMDNonReturningContains ({list.Count})", () => {Contains.SIMDNonReturningContains(list, target); }); },
#endif
                    () => {TestUtility.Time($"BurstReturningContains ({list.Count})", () => {Contains.BurstReturningContains(list, target); }); },
                    () => {TestUtility.Time($"SIMDReturningContains ({list.Count})", () => {Contains.SIMDReturningContains(list, target); }); },
                    () => {TestUtility.Time($"SIMDParallelReturningContains ({list.Count})", () => {Contains.SIMDParallelReturningContains(list, target); }); },
                    () => {TestUtility.Time($"SIMDParallelUnrolledReturningContains ({list.Count})", () => {Contains.SIMDParallelUnrolledReturningContains(list, target); }); },
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
            public void SearchLargeListFindLastThird()
            {
                RunTest(_list, (int)(2.0f * (_list.Count / 3)));
            }

            [Test, Performance]
            public void SearchLargeListFindMiddleOfLastEight()
            {
                RunTest(_list, (int)(7.5f * (_list.Count / 8)));
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
                protected Func<List<int>, int, bool> Func;

                [OneTimeSetUp]
                virtual public void Setup()
                {
                    list = new List<int>();
                    for (int i = 0; i < 1000; i++)
                        list.Add(i);
                }

                [Test]
                public void ReturnsTrueOnFoundLast()
                {
                    Assert.IsTrue(Func(list, list.Count - 1));
                }

                [Test]
                public void ReturnFalseOnNotFound()
                {
                    Assert.IsFalse(Func(list, list.Count + 1));
                }

                [Test]
                public void ReturnTrueOnFoundFirst()
                {
                    Assert.IsTrue(Func(list, 0));
                }

                [Test]
                public void ReturnTrueOnFoundMiddle()
                {
                    Assert.IsTrue(Func(list, list.Count / 2));
                }

                [Test]
                public void ReturnsTrueOnFoundNextToLast()
                {
                    Assert.IsTrue(Func(list, list.Count - 2));
                }

                [Test]
                public void ReturnsTrueOnFoundFirstStraggler()
                {
                    Assert.IsTrue(Func(list, list.Count - 31));
                }

                [Test]
                public void ReturnsOnAllFrom0ToListCount()
                {
                    for (int i = 0; i < list.Count; i++)
                        Assert.IsTrue(Func(list, list[i]), $"Failed On: {i}");
                }
            }

            [TestFixture]
            public class BurstNonReturningContains : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = Contains.BurstNonReturningContains;
                }
            }

            [TestFixture]
            public class BurstReturningContains : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = Contains.BurstReturningContains;
                }
            }

            [TestFixture]
            public class DefaultNonReturningContains : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = Contains.DefaultNonReturningContains;
                }
            }

            [TestFixture]
            public class DefaultReturningContains : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = Contains.DefaultNonReturningContains;
                }
            }

            [TestFixture]
            public class SIMDNonReturningContains : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = Contains.SIMDNonReturningContains;
                }
            }

            [TestFixture]
            public class SIMDReturningContains : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = Contains.SIMDReturningContains;
                }
            }

            [TestFixture]
            public class SIMDParallelReturningContains : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = Contains.SIMDParallelReturningContains;
                }
            }

            [TestFixture]
            public class SIMDParallelUnrolledReturningContains : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = Contains.SIMDParallelUnrolledReturningContains;
                }
            }
        }
#endif
    }
}