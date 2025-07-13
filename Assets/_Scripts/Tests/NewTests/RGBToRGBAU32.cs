using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using NUnit.Framework;

using UnityEngine;

using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PerformanceTesting;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

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

namespace NewRGBToRGBAU32
{
    public interface IJobImplementation
        : IJob
    {
        public NativeArray<byte> src { get; set; }
        public NativeArray<UInt32> dst { get; set; }
    }

    public abstract class Tests<TJobImplementation>
        where TJobImplementation : struct, IJobImplementation
    {
        public const int ColorsNeededForVerifyingAllPaths = 21;

        public void ConvertMultiple(NativeArray<byte> rgb, NativeArray<UInt32> rgba)
        {
            new TJobImplementation()
            {
                src = rgb,
                dst = rgba
            }
            .Run();
        }

        public Color32 ConvertSingle(Color32 color)
        {
            using var rgb = new NativeArray<byte>(3 * ColorsNeededForVerifyingAllPaths, Allocator.TempJob);
            for (int i = 0; i < ColorsNeededForVerifyingAllPaths; i++)
            {
                rgb.AsRef((i * 3) + 0) = color.r;
                rgb.AsRef((i * 3) + 1) = color.g;
                rgb.AsRef((i * 3) + 2) = color.b;

            }

            using var rgba = new NativeArray<UInt32>(ColorsNeededForVerifyingAllPaths, Allocator.TempJob);
            ConvertMultiple(rgb, rgba);

            var res = rgba[0];
            for (int i = 0; i < ColorsNeededForVerifyingAllPaths; i++)
            {
                Assert.That(res, Is.EqualTo(rgba[i]));
            }

            return UnsafeUtility.As<UInt32, Color32>(ref rgba.AsRef(0));
        }

        [Test]
        public void Red_channel_is_correctly_converted()
        {
            var c = ConvertSingle(Color.red);
            Assert.That(c.r, Is.EqualTo(255));
        }

        [Test]
        public void Green_channel_is_correctly_converted()
        {
            var c = ConvertSingle(Color.green);
            Assert.That(c.g, Is.EqualTo(255));
        }

        [Test]
        public void Blue_channel_is_correctly_converted()
        {
            var c = ConvertSingle(Color.blue);
            Assert.That(c.b, Is.EqualTo(255));
        }

        [Test]
        public void Alpha_channel_is_set_to_1()
        {
            var c = ConvertSingle(Color.black);
            Assert.That(c.a, Is.EqualTo(255));
        }

        [Test]
        public void Grey_is_correctly_converted()
        {
            var target = (Color32)Color.grey;
            var c = ConvertSingle(target);
            Assert.That(c.r, Is.EqualTo(target.r));
            Assert.That(c.g, Is.EqualTo(target.g));
            Assert.That(c.b, Is.EqualTo(target.b));
            Assert.That(c.a, Is.EqualTo(target.a));
        }

        [Test]
        public void Black_is_correctly_converted()
        {
            var target = (Color32)Color.black;
            var c = ConvertSingle(target);
            Assert.That(c.r, Is.EqualTo(target.r));
            Assert.That(c.g, Is.EqualTo(target.g));
            Assert.That(c.b, Is.EqualTo(target.b));
            Assert.That(c.a, Is.EqualTo(target.a));
        }

        [Test]
        public void Random_tests()
        {
            var randomizer = new Unity.Mathematics.Random(1);
            for (int outer = 0; outer < 1000; outer++)
            {
                using var colors = new NativeArray<UInt32>(randomizer.NextInt(0, 1 << 16), Allocator.TempJob);
                using var src = new NativeArray<byte>(colors.Length * 3, Allocator.TempJob);
                for (int i = 0; i < colors.Length; i++)
                    colors.AsRef(i) = randomizer.NextUInt() | 0xFF000000;

                for (int i = 0; i < colors.Length; i++)
                {
                    var r = (colors[i] >> 0) & 0xFF;
                    var g = (colors[i] >> 8) & 0xFF;
                    var b = (colors[i] >> 16) & 0xFF;
                    src.AsRef((i * 3) + 0) = (byte)r;
                    src.AsRef((i * 3) + 1) = (byte)g;
                    src.AsRef((i * 3) + 2) = (byte)b;
                }

                using var result = new NativeArray<UInt32>(colors.Length, Allocator.TempJob);
                ConvertMultiple(src, result);

                var correct = true;
                for (int i = 0; i < colors.Length; i++)
                {
                    correct &= result[i] == colors[i];
                }
                Assert.That(correct);
            }
        }
    }

    public class _PerformanceTests
    {
        [MethodImpl]
        public static void Time<TJobImplementation>(NativeArray<byte> src, NativeArray<UInt32> dst) where TJobImplementation : struct, IJobImplementation
        {
            TestUtility.Time($"{typeof(TJobImplementation).Name} ({src.Length / 3})", () => { new TJobImplementation { src = src, dst = dst }.Run(); });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RunTest(NativeArray<byte> src, NativeArray<UInt32> dst)
        {
            var timings = new Action[]
            {
                () => { Time<BaseImplementation>(src, dst); },
                () => { Time<V128ImplementationV0>(src, dst); },
            };

#if RANDOM_SHUFFLE_TESTS
            TestUtility.RandomShuffle(timings);
#endif
            foreach (var timing in timings)
                timing();
        }

        [Test, Performance]
        public void Comparison()
        {
            const int ColorCount = 100000;
            using var src = new NativeArray<byte>(ColorCount * 3, Allocator.Persistent);
            using var dst = new NativeArray<UInt32>(ColorCount, Allocator.Persistent);
            RunTest(src, dst);
        }
    }

    [BurstCompile]
    public struct BaseImplementation
        : IJobImplementation
    {
        public class Tests : Tests<BaseImplementation> { };

        public NativeArray<byte> src { get; set; }
        public NativeArray<uint> dst { get; set; }

        public void Execute()
        {
            var count = src.Length / 3;
            for (int i = 0; i < count; i++)
            {
                dst.AsRef(i)
                     = (uint)(src[(i * 3) + 0] << 0)
                     | (uint)(src[(i * 3) + 1] << 8)
                     | (uint)(src[(i * 3) + 2] << 16)
                     | 0xFF000000;
            }
        }
    }

    [BurstCompile]
    public struct V128ImplementationV0
        : IJobImplementation
    {
        public class Tests : Tests<V128ImplementationV0> { };

        public NativeArray<byte> src { get; set; }
        public NativeArray<uint> dst { get; set; }

        public void Execute()
        {
            // Registers:
            // 0  1   2   3   4  5
            // RGBR GBRG BRGB RGBR
            // 0123 4567 8901 2345
            // ---- ---- ---- ----
            // RGB- RGB- RGB- RGB- shuffle
            // RGBα RGBα RGBα RGBα or

            var count = src.Length / 3;

            // Correct formula:
            // aligned_count = number of wasted elements per iteration, rounded down to nearest number of elements
            // alternative:
            // aligned_count = (count - 2) & ~3;
            var aligned_byte_count = ((src.Length - 4) / 12) * 12;
            var aligned_count = aligned_byte_count / 3;

            var shuffle = setr_epi8(0, 1, 2, -1,
                                    3, 4, 5, -1,
                                    6, 7, 8, -1,
                                    9, 10, 11, -1);

            var alpha = set1_epi32(0xFF << 24);

            int i = 0;
            for (; i < aligned_count; i += 4)
            {
                var all = src.ReinterpretLoad<v128>(i * 3);
                var shuffled = shuffle_epi8(all, shuffle);
                var alphaed = or_si128(shuffled, alpha);
                dst.ReinterpretStore(i, alphaed);
            }

            for (; i < count; i++)
            {
                dst.AsRef(i)
                     = (uint)src[(i * 3) + 0] << 0
                     | (uint)src[(i * 3) + 1] << 8
                     | (uint)src[(i * 3) + 2] << 16
                     | (uint)0xFF << 24;
            }
        }
    }
}