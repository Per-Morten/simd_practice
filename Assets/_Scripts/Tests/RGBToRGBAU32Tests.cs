using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.PerformanceTesting;
using UnityEngine;
using Debug = UnityEngine.Debug;

using u32 = System.UInt32;
using u8 = System.Byte;

namespace RGBToRGBAU32Tests
{
    public class Tests
    {
        private static List<u8> ConvertColor(Color color, int count)
        {
            var colors = new List<u8>(count * 3);
            var c = (Color32)color;
            for (int i = 0; i < count; i++)
            {
                colors.Add(c.r);
                colors.Add(c.g);
                colors.Add(c.b);
            }
            return colors;
        }

        private static List<u8> ConvertColor(List<Color> colors)
        {
            var compressed = new List<u8>(colors.Count * 3);
            for (int i = 0; i < colors.Count; i++)
            {
                var c = (Color32)colors[i];
                compressed.Add(c.r);
                compressed.Add(c.g);
                compressed.Add(c.b);
            }
            return compressed;
        }

#if INCLUDE_PERFORMANCE_TESTS
        [TestFixture]
        public class CombinedPerformance
        {
#if TEN_MILLION_LIST
        const int ColorCount = 10000000;
#elif ONE_MILLION_LIST
            const int ColorCount = 1000000;
#elif HUNDRED_THOUSAND_LIST
        const int ColorCount = 100000;
#else
        const int ColorCount = 10000;
#endif
            List<u8> Colors;

            [OneTimeSetUp]
            public void Setup()
            {
                Colors = ConvertColor(Color.cyan, ColorCount);
            }

            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public static void RunTest(List<u8> list)
            {
                var timings = new Action[]
                {
                    //() => {TestUtility.Time($"ForLoopFilter ({list.Count / 3})", () => {RGBToRGBAU32.ForLoop(list); }); },
                    () => {TestUtility.Time($"Burst ({list.Count / 3})", () => {RGBToRGBAU32.BurstForLoop(list); }); },
                    () => {TestUtility.Time($"V128 ({list.Count / 3})", () => {RGBToRGBAU32.V128ForLoop(list); }); },
                    () => {TestUtility.Time($"V256 ({list.Count / 3})", () => {RGBToRGBAU32.V256ForLoop(list); }); },
                };

#if RANDOM_SHUFFLE_TESTS
                TestUtility.RandomShuffle(timings);
#endif
                foreach (var timing in timings)
                    timing();

                Debug.Log($"{new StackFrame(1).GetMethod().Name} finished");
            }

            [Test, Performance]
            public void ConvertColors()
            {
                RunTest(Colors);   
            }
        }
#endif

#if INCLUDE_CORRECTNESS_TESTS
        public class Correctness
        {
            public abstract class GenericTests
            {
                protected Func<List<u8>, List<u32>> Func;

                [OneTimeSetUp]
                virtual public void Setup()
                {
                }

                private unsafe void RunTest(Color color)
                {
                    var rgb = ConvertColor(color, 9);
                    var colors = Func(rgb);
                    var targetColor = ((Color32)color);
                    var target = *(u32*)&targetColor;
                    for (int i = 0; i < colors.Count; i++)
                        Assert.IsTrue(colors[i] == target);
                }

                [Test]
                public unsafe void CanConvertRed()
                {
                    RunTest(Color.red);
                }

                [Test]
                public void CanConvertBlue()
                {
                    RunTest(Color.blue);
                }

                [Test]
                public void CanConvertGreen()
                {
                    RunTest(Color.green);
                }

                [Test]
                public void CanConvertBlack()
                {
                    RunTest(Color.black);
                }

                [Test]
                public void CanConvertCyan()
                {
                    RunTest(Color.cyan);
                }

                [Test]
                public void CanConvertWhite()
                {
                    RunTest(Color.white);
                }

                [Test]
                public void CanConvertMagenta()
                {
                    RunTest(Color.magenta);
                }

                [Test]
                public void CanConvertYellow()
                {
                    RunTest(Color.yellow);
                }

                [Test]
                public unsafe void CanConvertMultiple()
                {
                    var colors = new List<Color> { Color.blue, Color.red, Color.green, Color.yellow, Color.cyan, Color.magenta, Color.gray, Color.white, Color.blue, Color.red, Color.green, Color.yellow, Color.cyan, Color.magenta, Color.gray, Color.white };
                    var result = Func(ConvertColor(colors));
                    for (int i = 0; i < colors.Count; i++)
                    {
                        var targetColor = ((Color32)colors[i]);
                        var target = *(u32*)&targetColor;
                        Assert.IsTrue(result[i] == target);
                    }
                }

                [Test]
                public unsafe void ByteRecognizablePattern()
                {
                    var pattern = new List<u8>();
                    for (int i = 0; i < 17 * 3; i++)
                        pattern.Add((u8)(i));

                    var result = Func(pattern);
                    for (int i = 0; i < result.Count; i++)
                    {
                        var targetColor = new Color32(pattern[i * 3 + 0], pattern[i * 3 + 1], pattern[i * 3 + 2], 0xFF);
                        var target = *(u32*)&targetColor;
                        Assert.IsTrue(result[i] == target);
                    }
                }
            }

            [TestFixture]
            public class ForLoop : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = RGBToRGBAU32.ForLoop;
                }
            }

            [TestFixture]
            public class Burst : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = RGBToRGBAU32.BurstForLoop;
                }
            }

            [TestFixture]
            public class V128ForLoop : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = RGBToRGBAU32.V128ForLoop;
                }
            }

            [TestFixture]
            public class V256ForLoop : GenericTests
            {
                [OneTimeSetUp]
                public override void Setup()
                {
                    base.Setup();
                    Func = RGBToRGBAU32.V256ForLoop;
                }
            }
        }
#endif
    }
}