using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

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
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PerformanceTesting;
using System;
using System.Runtime.CompilerServices;

namespace NewRGBToRGBAFloat4Tests
{
    public abstract class Tests
    {
        public abstract void ConvertMultiple(NativeArray<byte> rgb, NativeArray<float4> rgba);
        public abstract int ColorsNeededForVerifyingAllPaths { get; }

        public float4 ConvertSingle(Color color)
        {
            using var rgb = new NativeArray<byte>(3 * ColorsNeededForVerifyingAllPaths, Allocator.Temp);
            for (int i = 0; i < ColorsNeededForVerifyingAllPaths; i++)
            {
                rgb.AsRef((i * 3) + 0) = (byte)(color.r * 255.0f);
                rgb.AsRef((i * 3) + 1) = (byte)(color.g * 255.0f);
                rgb.AsRef((i * 3) + 2) = (byte)(color.b * 255.0f);

            }

            using var rgba = new NativeArray<float4>(ColorsNeededForVerifyingAllPaths, Allocator.Temp);
            ConvertMultiple(rgb, rgba);

            var res = rgba[0];
            for (int i = 0; i < ColorsNeededForVerifyingAllPaths; i++)
            {
                Assert.That(res.x, Is.EqualTo(rgba[i].x).Within(0.001f));
                Assert.That(res.y, Is.EqualTo(rgba[i].y).Within(0.001f));
                Assert.That(res.z, Is.EqualTo(rgba[i].z).Within(0.001f));
                Assert.That(res.w, Is.EqualTo(rgba[i].w).Within(0.001f));
            }

            return rgba[0];
        }

        [Test]
        public void Red_channel_is_correctly_converted()
        {
            var c = ConvertSingle(Color.red);
            Assert.That(c.x, Is.EqualTo(1.0f));
        }

        [Test]
        public void Green_channel_is_correctly_converted()
        {
            var c = ConvertSingle(Color.green);
            Assert.That(c.y, Is.EqualTo(1.0f));
        }

        [Test]
        public void Blue_channel_is_correctly_converted()
        {
            var c = ConvertSingle(Color.blue);
            Assert.That(c.z, Is.EqualTo(1.0f));
        }

        [Test]
        public void Alpha_channel_is_set_to_1()
        {
            var c = ConvertSingle(Color.black);
            Assert.That(c.w, Is.EqualTo(1.0f));
        }

        [Test]
        public void Grey_is_correctly_converted()
        {
            var c = ConvertSingle(Color.grey);
            Assert.That(c.x, Is.EqualTo(Color.grey.r).Within(0.01f));
            Assert.That(c.y, Is.EqualTo(Color.grey.g).Within(0.01f));
            Assert.That(c.z, Is.EqualTo(Color.grey.b).Within(0.01f));
            Assert.That(c.w, Is.EqualTo(Color.grey.a).Within(0.01f));
        }

        [Test]
        public void Black_is_correctly_converted()
        {
            var c = ConvertSingle(Color.black);
            Assert.That(c.x, Is.EqualTo(Color.black.r));
            Assert.That(c.y, Is.EqualTo(Color.black.g));
            Assert.That(c.z, Is.EqualTo(Color.black.b));
            Assert.That(c.w, Is.EqualTo(Color.black.a));
        }

        [Test]
        public void Helper()
        {
            var c = ConvertSingle(new Color(0.25f, 0.50f, 0.75f, 1.0f));
        }

        [Test]
        public void HelperLanes()
        {
            using var rgb = new NativeArray<byte>(33, Allocator.Persistent);
            for (int i = 0, c = 0; i < rgb.Length; i += 3, c++)
            {
                rgb.AsRef(i + 0) = (byte)((c * 10) + 0);
                rgb.AsRef(i + 1) = (byte)((c * 10) + 1);
                rgb.AsRef(i + 2) = (byte)((c * 10) + 2);
            }

            using var rgba = new NativeArray<float4>(11, Allocator.Persistent);
            ConvertMultiple(rgb, rgba);
        }

        [Test]
        public void Random_tests()
        {
            var randomizer = new Unity.Mathematics.Random(1);
            using var colors = new NativeArray<float4>(10000, Allocator.Temp);
            using var src = new NativeArray<byte>(colors.Length * 3, Allocator.Temp);
            for (int i = 0; i < colors.Length; i++)
                colors.AsRef(i) = new float4((float3)randomizer.NextInt3(0, 255) / 255.0f, 1.0f);

            for (int i = 0; i < colors.Length; i++)
            {
                src.AsRef((i * 3) + 0) = (byte)(colors[i].x * 255.0f);
                src.AsRef((i * 3) + 1) = (byte)(colors[i].y * 255.0f);
                src.AsRef((i * 3) + 2) = (byte)(colors[i].z * 255.0f);
            }

            using var result = new NativeArray<float4>(colors.Length, Allocator.Temp);
            ConvertMultiple(src, result);

            for (int i = 0; i < colors.Length; i++)
            {
                Assert.That(result[i].x, Is.EqualTo(colors[i].x).Within(0.001f), $"{i}.r");
                Assert.That(result[i].y, Is.EqualTo(colors[i].y).Within(0.001f), $"{i}.g");
                Assert.That(result[i].z, Is.EqualTo(colors[i].z).Within(0.001f), $"{i}.b");
                Assert.That(result[i].w, Is.EqualTo(colors[i].w).Within(0.001f), $"{i}.a");
            }
        }
    }

    public class _PerformanceTests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RunTest(NativeArray<byte> src, NativeArray<float4> dst)
        {
            var timings = new Action[]
            {
                () => {TestUtility.Time($"{nameof(BaseImplementation)} ({src.Length / 3})", () => {BaseImplementation.Implementation(ref src, ref dst); }); },
                () => {TestUtility.Time($"{nameof(V128Implementation5xV0)} ({src.Length / 3})", () => {V128Implementation5xV0.Implementation(ref src, ref dst); }); },
                () => {TestUtility.Time($"{nameof(V128Implementation5xV1)} ({src.Length / 3})", () => {V128Implementation5xV1.Implementation(ref src, ref dst); }); },
                () => {TestUtility.Time($"{nameof(V128Implementation5xV2)} ({src.Length / 3})", () => {V128Implementation5xV2.Implementation(ref src, ref dst); }); },
                () => {TestUtility.Time($"{nameof(V256Implementation10xV0)} ({src.Length / 3})", () => {V256Implementation10xV0.Implementation(ref src, ref dst); }); },
                () => {TestUtility.Time($"{nameof(V256Implementation10xV1)} ({src.Length / 3})", () => {V256Implementation10xV1.Implementation(ref src, ref dst); }); },
                () => {TestUtility.Time($"{nameof(V256Implementation10xV2)} ({src.Length / 3})", () => {V256Implementation10xV2.Implementation(ref src, ref dst); }); },
                () => {TestUtility.Time($"{nameof(V256Implementation8xV0)} ({src.Length / 3})", () => {V256Implementation8xV0.Implementation(ref src, ref dst); }); },
                () => {TestUtility.Time($"{nameof(V256Implementation8xV1)} ({src.Length / 3})", () => {V256Implementation8xV1.Implementation(ref src, ref dst); }); },
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
            using var dst = new NativeArray<float4>(ColorCount, Allocator.Persistent);
            RunTest(src, dst);
        }
    }

    [BurstCompile]
    public class BaseImplementation : Tests
    {
        public override int ColorsNeededForVerifyingAllPaths => 1;

        public override void ConvertMultiple(NativeArray<byte> rgb,
                                             NativeArray<float4> rgba)
        {
            Implementation(ref rgb, ref rgba);
        }

        [BurstCompile]
        public static void Implementation(ref NativeArray<byte> rgb,
                                          ref NativeArray<float4> rgba)
        {
            for (int i = 0; i < rgb.Length / 3; i++)
            {
                var r = rgb[i * 3 + 0];
                var g = rgb[i * 3 + 1];
                var b = rgb[i * 3 + 2];

                rgba[i] = new float4
                {
                    x = r / 255.0f,
                    y = g / 255.0f,
                    z = b / 255.0f,
                    w = 1.0f
                };
            }
        }
    }

    [BurstCompile]
    public class V128Implementation5xV0 : Tests
    {
        public override int ColorsNeededForVerifyingAllPaths => 6;

        public override void ConvertMultiple(NativeArray<byte> rgb,
                                             NativeArray<float4> rgba)
        {
            Implementation(ref rgb, ref rgba);
        }

        [BurstCompile]
        public static unsafe void Implementation(ref NativeArray<byte> rgb,
                                                 ref NativeArray<float4> rgba)
        {
            var count = rgb.Length / 3;
            var aligned_byte_count = rgb.Length / 16;
            var aligned_count = aligned_byte_count * 5;

            var _1_div_255 = set1_ps(1 / 255.0f);

            int i = 0;
            for (; i < aligned_count; i += 5)
            {
                var all = rgb.ReinterpretLoad<v128>(i * 3);

                // Can skip cvtepu8_epi32 by shuffling into correct positions here.
                // do that in next version.
                var v0 = srli_si128(all, 0);
                var v1 = srli_si128(all, 3);
                var v2 = srli_si128(all, 6);
                var v3 = srli_si128(all, 9);
                var v4 = srli_si128(all, 12);

                v0 = cvtepu8_epi32(v0);
                v1 = cvtepu8_epi32(v1);
                v2 = cvtepu8_epi32(v2);
                v3 = cvtepu8_epi32(v3);
                v4 = cvtepu8_epi32(v4);

                v0 = or_si128(v0, setr_epi32(0, 0, 0, 255));
                v1 = or_si128(v1, setr_epi32(0, 0, 0, 255));
                v2 = or_si128(v2, setr_epi32(0, 0, 0, 255));
                v3 = or_si128(v3, setr_epi32(0, 0, 0, 255));
                v4 = or_si128(v4, setr_epi32(0, 0, 0, 255));

                v0 = cvtepi32_ps(v0);
                v1 = cvtepi32_ps(v1);
                v2 = cvtepi32_ps(v2);
                v3 = cvtepi32_ps(v3);
                v4 = cvtepi32_ps(v4);

                v0 = mul_ps(v0, _1_div_255);
                v1 = mul_ps(v1, _1_div_255);
                v2 = mul_ps(v2, _1_div_255);
                v3 = mul_ps(v3, _1_div_255);
                v4 = mul_ps(v4, _1_div_255);

                rgba.ReinterpretStore(i + 0, v0);
                rgba.ReinterpretStore(i + 1, v1);
                rgba.ReinterpretStore(i + 2, v2);
                rgba.ReinterpretStore(i + 3, v3);
                rgba.ReinterpretStore(i + 4, v4);
            }

            for (; i < count; i++)
            {
                rgba[i] = new float4
                {
                    x = rgb[(i * 3) + 0] / 255.0f,
                    y = rgb[(i * 3) + 1] / 255.0f,
                    z = rgb[(i * 3) + 2] / 255.0f,
                    w = 1.0f
                };
            }
        }
    }

    [BurstCompile]
    public class V128Implementation5xV1 : Tests
    {
        public override int ColorsNeededForVerifyingAllPaths => 6;

        public override void ConvertMultiple(NativeArray<byte> rgb,
                                             NativeArray<float4> rgba)
        {
            Implementation(ref rgb, ref rgba);
        }

        [BurstCompile]
        public static unsafe void Implementation(ref NativeArray<byte> rgb,
                                                 ref NativeArray<float4> rgba)
        {
            var count = rgb.Length / 3;
            var aligned_byte_count = rgb.Length / 16;
            var aligned_count = aligned_byte_count * 5;

            var _1_div_255 = set1_ps(1 / 255.0f);

            int i = 0;
            for (; i < aligned_count; i += 5)
            {
                var all = rgb.ReinterpretLoad<v128>(i * 3);

                var v0 = shuffle_epi8(all, setr_epi8(0, -1, -1, -1,
                                                     1, -1, -1, -1,
                                                     2, -1, -1, -1,
                                                     -1, -1, -1, -1));

                var v1 = shuffle_epi8(all, setr_epi8(3, -1, -1, -1,
                                                     4, -1, -1, -1,
                                                     5, -1, -1, -1,
                                                     -1, -1, -1, -1));

                var v2 = shuffle_epi8(all, setr_epi8(6, -1, -1, -1,
                                                     7, -1, -1, -1,
                                                     8, -1, -1, -1,
                                                     -1, -1, -1, -1));

                var v3 = shuffle_epi8(all, setr_epi8(9, -1, -1, -1,
                                                     10, -1, -1, -1,
                                                     11, -1, -1, -1,
                                                     -1, -1, -1, -1));

                var v4 = shuffle_epi8(all, setr_epi8(12, -1, -1, -1,
                                                     13, -1, -1, -1,
                                                     14, -1, -1, -1,
                                                     -1, -1, -1, -1));

                v0 = or_si128(v0, setr_epi32(0, 0, 0, 255));
                v1 = or_si128(v1, setr_epi32(0, 0, 0, 255));
                v2 = or_si128(v2, setr_epi32(0, 0, 0, 255));
                v3 = or_si128(v3, setr_epi32(0, 0, 0, 255));
                v4 = or_si128(v4, setr_epi32(0, 0, 0, 255));

                v0 = cvtepi32_ps(v0);
                v1 = cvtepi32_ps(v1);
                v2 = cvtepi32_ps(v2);
                v3 = cvtepi32_ps(v3);
                v4 = cvtepi32_ps(v4);

                v0 = mul_ps(v0, _1_div_255);
                v1 = mul_ps(v1, _1_div_255);
                v2 = mul_ps(v2, _1_div_255);
                v3 = mul_ps(v3, _1_div_255);
                v4 = mul_ps(v4, _1_div_255);

                rgba.ReinterpretStore(i + 0, v0);
                rgba.ReinterpretStore(i + 1, v1);
                rgba.ReinterpretStore(i + 2, v2);
                rgba.ReinterpretStore(i + 3, v3);
                rgba.ReinterpretStore(i + 4, v4);
            }

            for (; i < count; i++)
            {
                rgba[i] = new float4
                {
                    x = rgb[(i * 3) + 0] / 255.0f,
                    y = rgb[(i * 3) + 1] / 255.0f,
                    z = rgb[(i * 3) + 2] / 255.0f,
                    w = 1.0f
                };
            }
        }
    }

    [BurstCompile]
    public class V128Implementation5xV2 : Tests
    {
        public override int ColorsNeededForVerifyingAllPaths => 6;

        public override void ConvertMultiple(NativeArray<byte> rgb,
                                             NativeArray<float4> rgba)
        {
            Implementation(ref rgb, ref rgba);
        }

        [BurstCompile]
        public static unsafe void Implementation(ref NativeArray<byte> rgb,
                                                 ref NativeArray<float4> rgba)
        {
            var count = rgb.Length / 3;
            var aligned_byte_count = rgb.Length / 16;
            var aligned_count = aligned_byte_count * 5;

            var _1_div_255 = set1_ps(1 / 255.0f);

            var bias = 8388608.0f;
            var i32_to_f32_biased = setr_ps(bias, bias, bias, 0.0f);
            var bias_ratio = -(bias / 255.0f);
            var f32_biased_to_f32_normalized = setr_ps(bias_ratio, bias_ratio, bias_ratio, 1.0f);

            int i = 0;
            for (; i < aligned_count; i += 5)
            {
                var all = rgb.ReinterpretLoad<v128>(i * 3);

                var v0 = shuffle_epi8(all, setr_epi8(0, -1, -1, -1,
                                                     1, -1, -1, -1,
                                                     2, -1, -1, -1,
                                                     -1, -1, -1, -1));

                var v1 = shuffle_epi8(all, setr_epi8(3, -1, -1, -1,
                                                     4, -1, -1, -1,
                                                     5, -1, -1, -1,
                                                     -1, -1, -1, -1));

                var v2 = shuffle_epi8(all, setr_epi8(6, -1, -1, -1,
                                                     7, -1, -1, -1,
                                                     8, -1, -1, -1,
                                                     -1, -1, -1, -1));

                var v3 = shuffle_epi8(all, setr_epi8(9, -1, -1, -1,
                                                     10, -1, -1, -1,
                                                     11, -1, -1, -1,
                                                     -1, -1, -1, -1));

                var v4 = shuffle_epi8(all, setr_epi8(12, -1, -1, -1,
                                                     13, -1, -1, -1,
                                                     14, -1, -1, -1,
                                                     -1, -1, -1, -1));

                v0 = or_si128(v0, i32_to_f32_biased);
                v1 = or_si128(v1, i32_to_f32_biased);
                v2 = or_si128(v2, i32_to_f32_biased);
                v3 = or_si128(v3, i32_to_f32_biased);
                v4 = or_si128(v4, i32_to_f32_biased);

                v0 = fmadd_ps(v0, _1_div_255, f32_biased_to_f32_normalized);
                v1 = fmadd_ps(v1, _1_div_255, f32_biased_to_f32_normalized);
                v2 = fmadd_ps(v2, _1_div_255, f32_biased_to_f32_normalized);
                v3 = fmadd_ps(v3, _1_div_255, f32_biased_to_f32_normalized);
                v4 = fmadd_ps(v4, _1_div_255, f32_biased_to_f32_normalized);

                rgba.ReinterpretStore(i + 0, v0);
                rgba.ReinterpretStore(i + 1, v1);
                rgba.ReinterpretStore(i + 2, v2);
                rgba.ReinterpretStore(i + 3, v3);
                rgba.ReinterpretStore(i + 4, v4);
            }

            for (; i < count; i++)
            {
                rgba[i] = new float4
                {
                    x = rgb[(i * 3) + 0] / 255.0f,
                    y = rgb[(i * 3) + 1] / 255.0f,
                    z = rgb[(i * 3) + 2] / 255.0f,
                    w = 1.0f
                };
            }
        }
    }

    [BurstCompile]
    public class V256Implementation10xV0 : Tests
    {
        public override int ColorsNeededForVerifyingAllPaths => 11;

        public override void ConvertMultiple(NativeArray<byte> rgb,
                                             NativeArray<float4> rgba)
        {
            Implementation(ref rgb, ref rgba);
        }

        [BurstCompile]
        public static unsafe void Implementation(ref NativeArray<byte> rgb,
                                                 ref NativeArray<float4> rgba)
        {
            // Input:
            //  u128    0                   1
            //  u64     0         1         2         3
            //  u32     0    1    2    3    4    5    6    7
            //  u16     0 1  2 3  4 5  6 7  8 9  0 1  2 3  4 5
            //  u8      0123 4567 8901 2345 6789 0123 4567 8901
            //          RGBR GBRG BRGB RGBR GBRG BRGB RGBR GBRG 
            //          0  1   2   3   4  5   6   7   8  9
            // Registers: 
            //          0123 4567 89AB CDEF GHIJ KLMN OPQR ST--
            // v0       0123 45-- ---- ---- ---- ---- ---- ---- << 0, lo
            // v1       6789 AB-- ---- ---- ---- ---- ---- ---- << 6, lo
            // v2       CDEF GH-- ---- ---- ---- ---- ---- ---- permute_epi32(3, 4, -1, -1, -1)
            // v3       IJKL MN-- ---- ---- ---- ---- ---- ---- << 2, hi
            // v4       OPQR ST-- ---- ---- ---- ---- ---- ---- << 8, hi
            //
            // Path each register takes after isolating 8 values we're working on.
            // 
            // α = 255
            //          0123  45--  ----  ----  ----  ----  ----  ----
            //          0120  3450  ----  ----  ----  ----  ----  ----
            //          012α  345α  ----  ----  ----  ----  ----  ----
            //          0     1     2     α     3     4     5     α
            //          0f    1f    2f    αf    3f    4f    5f    αf
            //          0f/αf 1f/αf 2f/αf αf/αf 3f/αf 4f/αf 5f/αf αf/αf
            //          

            var src = (byte*)rgb.GetUnsafePtr();
            var count = rgb.Length / 3;
            var dst = (float4*)rgba.GetUnsafePtr();

            var shuffleV128 = setr_epi8(0, 1, 2, -1, 3, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1);
            var shuffleV256 = mm256_setr_m128(shuffleV128, shuffleV128);

            var aligned_byte_count = rgb.Length / 32;
            var aligned_count = aligned_byte_count * 10;

            var alpha = mm256_set1_epi32(0xFF << 24);
            var _1div255 = mm256_set1_ps(1 / 255.0f);

            int i = 0;
            for (; i < aligned_count; i += 10)
            {
                var all = mm256_loadu_ps(src + i * 3);
                var v0 = mm256_srli_si256(all, 0);
                var v1 = mm256_srli_si256(all, 6);
                var v2 = mm256_permutevar8x32_epi32(all, mm256_setr_epi32(3, 4, -1, -1, -1, -1, -1, -1));
                var v3 = mm256_srli_si256(all, 2);
                var v4 = mm256_srli_si256(all, 8);

                v0 = mm256_shuffle_epi8(v0, shuffleV256);
                v1 = mm256_shuffle_epi8(v1, shuffleV256);
                v2 = mm256_shuffle_epi8(v2, shuffleV256);
                v3 = mm256_shuffle_epi8(v3, shuffleV256);
                v4 = mm256_shuffle_epi8(v4, shuffleV256);

                v0 = mm256_or_ps(v0, alpha);
                v1 = mm256_or_ps(v1, alpha);
                v2 = mm256_or_ps(v2, alpha);
                v3 = mm256_or_ps(v3, alpha);
                v4 = mm256_or_ps(v4, alpha);

                v0 = mm256_cvtepu8_epi32(v0.Lo128);
                v1 = mm256_cvtepu8_epi32(v1.Lo128);
                v2 = mm256_cvtepu8_epi32(v2.Lo128);
                v3 = mm256_cvtepu8_epi32(v3.Hi128);
                v4 = mm256_cvtepu8_epi32(v4.Hi128);

                v0 = mm256_cvtepi32_ps(v0);
                v1 = mm256_cvtepi32_ps(v1);
                v2 = mm256_cvtepi32_ps(v2);
                v3 = mm256_cvtepi32_ps(v3);
                v4 = mm256_cvtepi32_ps(v4);

                v0 = mm256_mul_ps(v0, _1div255);
                v1 = mm256_mul_ps(v1, _1div255);
                v2 = mm256_mul_ps(v2, _1div255);
                v3 = mm256_mul_ps(v3, _1div255);
                v4 = mm256_mul_ps(v4, _1div255);

                mm256_storeu_ps(dst + i + 0, v0);
                mm256_storeu_ps(dst + i + 2, v1);
                mm256_storeu_ps(dst + i + 4, v2);
                mm256_storeu_ps(dst + i + 6, v3);
                mm256_storeu_ps(dst + i + 8, v4);
            }

            for (; i < count; i++)
                dst[i] = new float4(src[i * 3 + 0] / 255.0f, src[i * 3 + 1] / 255.0f, src[i * 3 + 2] / 255.0f, 1.0f);
        }
    }

    [BurstCompile]
    public class V256Implementation10xV1 : Tests
    {
        public override int ColorsNeededForVerifyingAllPaths => 11;

        public override void ConvertMultiple(NativeArray<byte> rgb,
                                             NativeArray<float4> rgba)
        {
            Implementation(ref rgb, ref rgba);
        }

        [BurstCompile]
        public static unsafe void Implementation(ref NativeArray<byte> rgb,
                                                 ref NativeArray<float4> rgba)
        {
            // Input:
            //  u128    0                   1
            //  u64     0         1         2         3
            //  u32     0    1    2    3    4    5    6    7
            //  u16     0 1  2 3  4 5  6 7  8 9  0 1  2 3  4 5
            //  u8      0123 4567 8901 2345 6789 0123 4567 8901
            //          RGBR GBRG BRGB RGBR GBRG BRGB RGBR GBRG 
            //          0  1   2   3   4  5   6   7   8  9
            // Registers: 
            //          0123 4567 89AB CDEF GHIJ KLMN OPQR ST--
            // v0       0123 45-- ---- ---- ---- ---- ---- ---- << 0, lo
            // v1       6789 AB-- ---- ---- ---- ---- ---- ---- << 6, lo
            // v2       CDEF GH-- ---- ---- ---- ---- ---- ---- permute_epi32(3, 4, -1, -1, -1)
            // v3       IJKL MN-- ---- ---- ---- ---- ---- ---- << 2, hi
            // v4       OPQR ST-- ---- ---- ---- ---- ---- ---- << 8, hi
            //
            // Path each register takes after isolating 8 values we're working on.
            // 
            // α = 255
            //          0123  45--  ----  ----  ----  ----  ----  ----
            //          0120  3450  ----  ----  ----  ----  ----  ----
            //          012α  345α  ----  ----  ----  ----  ----  ----
            //          0     1     2     α     3     4     5     α
            //          0f    1f    2f    αf    3f    4f    5f    αf
            //          0f/αf 1f/αf 2f/αf αf/αf 3f/αf 4f/αf 5f/αf αf/αf
            //          

            var src = (byte*)rgb.GetUnsafePtr();
            var count = rgb.Length / 3;
            var dst = (float4*)rgba.GetUnsafePtr();

            var shuffle_v128 = setr_epi8(0, 1, 2, -1, 3, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1);
            var shuffle_v256 = mm256_setr_m128(shuffle_v128, shuffle_v128);

            var aligned_byte_count = rgb.Length / 32;
            var aligned_count = aligned_byte_count * 10;

            var _1_div_255 = mm256_set1_ps(1 / 255.0f);

            // 8388608.0f == 0x4b000000. At this value f32 doesn't have any
            // decimal precision anymore. Going to or with it so I can bake the
            // i32->f32 conversion into the fmadd.
            //
            // Note: Not OR'ing with this value on alpha channel because it will
            // be set to 1 in the fmadd.
            var i32_to_f32_biased = mm256_setr_ps(8388608.0f,
                                                  8388608.0f,
                                                  8388608.0f,
                                                  0.0f,
                                                  8388608.0f,
                                                  8388608.0f,
                                                  8388608.0f,
                                                  0.0f);

            // We need to remove the bias we introduced as part of int to float
            // conversion
            var bias_ratio = -(8388608.0f / 255.0f);
            var f32_biased_to_f32_normalized = mm256_setr_ps(bias_ratio,
                                                             bias_ratio,
                                                             bias_ratio,
                                                             1.0f,
                                                             bias_ratio,
                                                             bias_ratio,
                                                             bias_ratio,
                                                             1.0f);

            int i = 0;
            for (; i < aligned_count; i += 10)
            {
                var all = rgb.ReinterpretLoad<v256>(i * 3);

                var v0 = mm256_srli_si256(all, 0);
                var v1 = mm256_srli_si256(all, 6);

                // Need to permute because these RGB values are straddling the
                // 128-bit boundary.
                var v2 = mm256_permutevar8x32_epi32(all, mm256_setr_epi32(3, 4, -1, -1, -1, -1, -1, -1));

                var v3 = mm256_srli_si256(all, 2);
                var v4 = mm256_srli_si256(all, 8);

                // Shuffle from RGBRGB-- to RGB-RGB- (space for alpha channel).
                // Note: alpha channel is 0 due to shuffle with -1
                v0 = mm256_shuffle_epi8(v0, shuffle_v256);
                v1 = mm256_shuffle_epi8(v1, shuffle_v256);
                v2 = mm256_shuffle_epi8(v2, shuffle_v256);
                v3 = mm256_shuffle_epi8(v3, shuffle_v256);
                v4 = mm256_shuffle_epi8(v4, shuffle_v256);

                // Convert to 32-bit representation
                v0 = mm256_cvtepu8_epi32(v0.Lo128);
                v1 = mm256_cvtepu8_epi32(v1.Lo128);
                v2 = mm256_cvtepu8_epi32(v2.Lo128);
                v3 = mm256_cvtepu8_epi32(v3.Hi128);
                v4 = mm256_cvtepu8_epi32(v4.Hi128);

                // OR'ing takes us to the biased f32 range without decimal precision 
                v0 = mm256_or_ps(v0, i32_to_f32_biased);
                v1 = mm256_or_ps(v1, i32_to_f32_biased);
                v2 = mm256_or_ps(v2, i32_to_f32_biased);
                v3 = mm256_or_ps(v3, i32_to_f32_biased);
                v4 = mm256_or_ps(v4, i32_to_f32_biased);

                // Divide by 255.0f to get us in normalized RGB range, then
                // subtract (8388608.0f / 255.0f) to remove the bias we added to
                // do int to float conversion. Alpha lane just gets the value 1
                // added because it's 0 after shuffling. 
                v0 = mm256_fmadd_ps(v0, _1_div_255, f32_biased_to_f32_normalized);
                v1 = mm256_fmadd_ps(v1, _1_div_255, f32_biased_to_f32_normalized);
                v2 = mm256_fmadd_ps(v2, _1_div_255, f32_biased_to_f32_normalized);
                v3 = mm256_fmadd_ps(v3, _1_div_255, f32_biased_to_f32_normalized);
                v4 = mm256_fmadd_ps(v4, _1_div_255, f32_biased_to_f32_normalized);

                rgba.ReinterpretStore(i + 0, v0);
                rgba.ReinterpretStore(i + 2, v1);
                rgba.ReinterpretStore(i + 4, v2);
                rgba.ReinterpretStore(i + 6, v3);
                rgba.ReinterpretStore(i + 8, v4);
            }

            for (; i < count; i++)
                dst[i] = new float4(src[i * 3 + 0] / 255.0f, src[i * 3 + 1] / 255.0f, src[i * 3 + 2] / 255.0f, 1.0f);
        }
    }

    [BurstCompile]
    public class V256Implementation10xV2 : Tests
    {
        public override int ColorsNeededForVerifyingAllPaths => 11;

        public override void ConvertMultiple(NativeArray<byte> rgb,
                                             NativeArray<float4> rgba)
        {
            Implementation(ref rgb, ref rgba);
        }

        [BurstCompile]
        public static void Implementation(ref NativeArray<byte> rgb,
                                          ref NativeArray<float4> rgba)
        {
            // Input:
            //  u128    0                   1
            //  u64     0         1         2         3
            //  u32     0    1    2    3    4    5    6    7
            //  u16     0 1  2 3  4 5  6 7  8 9  0 1  2 3  4 5
            //  u8      0123 4567 8901 2345 6789 0123 4567 8901
            //          RGBR GBRG BRGB RGBR GBRG BRGB RGBR GBRG 
            //          0  1   2   3   4  5   6   7   8  9
            //
            // Registers: 
            //          0123 4567 89AB CDEF GHIJ KLMN OPQR ST--
            // s0       012- 345- ---- ---- IJK- LMN- ---- ---- shuffle
            // s1       678- 9AB- ---- ---- OPQ- RST- ---- ---- shuffle
            // s2       CDEF GH-- ---- ---- ---- ---- ---- ---- alignr
            // s2       CDE- FGH- ---- ---- ---- ---- ---- ---- shuffle
            //
            //          0123 4567 89AB CDEF GHIJ KLMN OPQR ST--
            // v0       012- 345- ---- ---- ---- ---- ---- ---- 
            // v1       678- 9AB- ---- ---- ---- ---- ---- ---- 
            // v2       CDE- FGH- ---- ---- ---- ---- ---- ---- 
            // v3       IJK- LMN- ---- ---- ---- ---- ---- ---- 
            // v4       OPQ- RST- ---- ---- ---- ---- ---- ---- 
            //
            // Path each register takes after isolating 8 values we're working on.
            // 
            // α = 255
            //          012-  345-  ----  ----  ----  ----  ----  ----
            //          0120  3450  ----  ----  ----  ----  ----  ----
            //          012α  345α  ----  ----  ----  ----  ----  ----
            //          0     1     2     α     3     4     5     α
            //          0f    1f    2f    αf    3f    4f    5f    αf
            //          0f/αf 1f/αf 2f/αf αf/αf 3f/αf 4f/αf 5f/αf αf/αf
            //          

            var count = rgb.Length / 3;
            var aligned_byte_count = rgb.Length / 32;
            var aligned_count = aligned_byte_count * 10;

            var _1_div_255 = mm256_set1_ps(1 / 255.0f);

            // 8388608.0f == 0x4b000000. At this value f32 doesn't have any
            // decimal precision anymore. Going to or with it so I can bake the
            // i32->f32 conversion into the fmadd.
            //
            // Note: Not OR'ing with this value on alpha channel. It will remain
            // zero and have "1" added to it in the fmadd.
            var bias = 8388608.0f;
            var i32_to_f32_biased = mm256_setr_ps(bias,
                                                  bias,
                                                  bias,
                                                  0.0f,
                                                  bias,
                                                  bias,
                                                  bias,
                                                  0.0f);

            // Need to remove the bias we introduced as part of int to float conversion
            var bias_ratio = -(bias / 255.0f);
            var f32_biased_to_f32_normalized = mm256_setr_ps(bias_ratio,
                                                             bias_ratio,
                                                             bias_ratio,
                                                             1.0f,
                                                             bias_ratio,
                                                             bias_ratio,
                                                             bias_ratio,
                                                             1.0f);

            int i = 0;
            for (; i < aligned_count; i += 10)
            {
                var all = rgb.ReinterpretLoad<v256>(i * 3);

                // Shuffles RGBRGB to RGB0RGB0 for RGB values 0, 1, and 6, 7.
                var s0 = mm256_shuffle_epi8(all, mm256_setr_epi8(0, 1, 2, 0xFF,
                                                                 3, 4, 5, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 2, 3, 4, 0xFF,
                                                                 5, 6, 7, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF));

                // Shuffles RGBRGB to RGB0RGB0 for RGB values 2, 3, and 8, 9.
                var s1 = mm256_shuffle_epi8(all, mm256_setr_epi8(6, 7, 8, 0xFF,
                                                                 9, 10, 11, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 8, 9, 10, 0xFF,
                                                                 11, 12, 13, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF));

                // Creates RGBRGB for values 4, 5. Need to do this differently because 5 straddles the 128-bit boundary.
                var s2 = alignr_epi8(all.Lo128, all.Hi128, 12);

                // Shuffles RGBRGB to RGB0RGB0 for RGB values 4, 5.
                s2 = shuffle_epi8(s2, setr_epi8(0, 1, 2, -1, 3, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1));

                // Convert to 32-bit representation
                var v0 = mm256_cvtepu8_epi32(s0.Lo128);
                var v1 = mm256_cvtepu8_epi32(s1.Lo128);
                var v2 = mm256_cvtepu8_epi32(s2);
                var v3 = mm256_cvtepu8_epi32(s0.Hi128);
                var v4 = mm256_cvtepu8_epi32(s1.Hi128);

                // OR'ing takes us to the biased f32 range without decimal precision 
                v0 = mm256_or_ps(v0, i32_to_f32_biased);
                v1 = mm256_or_ps(v1, i32_to_f32_biased);
                v2 = mm256_or_ps(v2, i32_to_f32_biased);
                v3 = mm256_or_ps(v3, i32_to_f32_biased);
                v4 = mm256_or_ps(v4, i32_to_f32_biased);

                // Divide by 255.0f to get us in normalized RGB range, then
                // subtract (8388608.0f / 255.0f) to remove the bias we added to
                // do int to float conversion. Alpha lane just gets the value 1
                // added because it's 0 after the initial shuffling. 
                v0 = mm256_fmadd_ps(v0, _1_div_255, f32_biased_to_f32_normalized);
                v1 = mm256_fmadd_ps(v1, _1_div_255, f32_biased_to_f32_normalized);
                v2 = mm256_fmadd_ps(v2, _1_div_255, f32_biased_to_f32_normalized);
                v3 = mm256_fmadd_ps(v3, _1_div_255, f32_biased_to_f32_normalized);
                v4 = mm256_fmadd_ps(v4, _1_div_255, f32_biased_to_f32_normalized);

                rgba.ReinterpretStore(i + 0, v0);
                rgba.ReinterpretStore(i + 2, v1);
                rgba.ReinterpretStore(i + 4, v2);
                rgba.ReinterpretStore(i + 6, v3);
                rgba.ReinterpretStore(i + 8, v4);
            }

            for (; i < count; i++)
                rgba[i] = new float4(rgb[i * 3 + 0] / 255.0f, rgb[i * 3 + 1] / 255.0f, rgb[i * 3 + 2] / 255.0f, 1.0f);
        }
    }

    [BurstCompile]
    public class V256Implementation10xV3 : Tests
    {
        public override int ColorsNeededForVerifyingAllPaths => 11;

        public override void ConvertMultiple(NativeArray<byte> rgb,
                                             NativeArray<float4> rgba)
        {
            Implementation(ref rgb, ref rgba);
        }

        [BurstCompile]
        public static void Implementation(ref NativeArray<byte> rgb,
                                          ref NativeArray<float4> rgba)
        {
            // Input:
            //  u128    0                   1
            //  u64     0         1         2         3
            //  u32     0    1    2    3    4    5    6    7
            //  u16     0 1  2 3  4 5  6 7  8 9  0 1  2 3  4 5
            //  u8      0123 4567 8901 2345 6789 0123 4567 8901
            //          RGBR GBRG BRGB RGBR GBRG BRGB RGBR GBRG 
            //          0  1   2   3   4  5   6   7   8  9
            //
            // Registers: 
            //          0123 4567 89AB CDEF GHIJ KLMN OPQR ST--
            // s0       012- 345- ---- ---- IJK- LMN- ---- ---- shuffle
            // s1       678- 9AB- ---- ---- OPQ- RST- ---- ---- shuffle
            // s2       CDEF GH-- ---- ---- ---- ---- ---- ---- alignr
            // s2       CDE- FGH- ---- ---- ---- ---- ---- ---- shuffle
            //
            //          0123 4567 89AB CDEF GHIJ KLMN OPQR ST--
            // v0       012- 345- ---- ---- ---- ---- ---- ---- 
            // v1       678- 9AB- ---- ---- ---- ---- ---- ---- 
            // v2       CDE- FGH- ---- ---- ---- ---- ---- ---- 
            // v3       IJK- LMN- ---- ---- ---- ---- ---- ---- 
            // v4       OPQ- RST- ---- ---- ---- ---- ---- ---- 
            //
            // Path each register takes after isolating 8 values we're working on.
            // 
            // α = 255
            //          012-  345-  ----  ----  ----  ----  ----  ----
            //          0120  3450  ----  ----  ----  ----  ----  ----
            //          012α  345α  ----  ----  ----  ----  ----  ----
            //          0     1     2     α     3     4     5     α
            //          0f    1f    2f    αf    3f    4f    5f    αf
            //          0f/αf 1f/αf 2f/αf αf/αf 3f/αf 4f/αf 5f/αf αf/αf
            //          

            var count = rgb.Length / 3;
            var aligned_byte_count = rgb.Length / 32;
            var aligned_count = aligned_byte_count * 10;

            var _1_div_255 = mm256_set1_ps(1 / 255.0f);

            // 8388608.0f == 0x4b000000. At this value f32 doesn't have any
            // decimal precision anymore. Going to or with it so I can bake the
            // i32->f32 conversion into the fmadd.
            //
            // Note: Not OR'ing with this value on alpha channel. It will remain
            // zero and have "1" added to it in the fmadd.
            var bias = 8388608.0f;
            var i32_to_f32_biased = mm256_setr_ps(bias,
                                                  bias,
                                                  bias,
                                                  0.0f,
                                                  bias,
                                                  bias,
                                                  bias,
                                                  0.0f);

            // Need to remove the bias we introduced as part of int to float conversion
            var bias_ratio = -(bias / 255.0f);
            var f32_biased_to_f32_normalized = mm256_setr_ps(bias_ratio,
                                                             bias_ratio,
                                                             bias_ratio,
                                                             1.0f,
                                                             bias_ratio,
                                                             bias_ratio,
                                                             bias_ratio,
                                                             1.0f);

            int i = 0;
            for (; i < aligned_count; i += 10)
            {
                var all = rgb.ReinterpretLoad<v256>(i * 3);

                // Shuffles RGBRGB to RGB0RGB0 for RGB values 0, 1, and 6, 7.
                var s0 = mm256_shuffle_epi8(all, mm256_setr_epi8(0, 1, 2, 0xFF,
                                                                 3, 4, 5, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 2, 3, 4, 0xFF,
                                                                 5, 6, 7, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF));

                // Shuffles RGBRGB to RGB0RGB0 for RGB values 2, 3, and 8, 9.
                var s1 = mm256_shuffle_epi8(all, mm256_setr_epi8(6, 7, 8, 0xFF,
                                                                 9, 10, 11, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 8, 9, 10, 0xFF,
                                                                 11, 12, 13, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF,
                                                                 0xFF, 0xFF, 0xFF, 0xFF));

                // Creates RGBRGB for values 4, 5. Need to do this differently because 5 straddles the 128-bit boundary.
                var s2 = alignr_epi8(all.Lo128, all.Hi128, 12);

                // Shuffles RGBRGB to RGB0RGB0 for RGB values 4, 5.
                s2 = shuffle_epi8(s2, setr_epi8(0, 1, 2, -1, 3, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1));

                // Convert to 32-bit representation
                // can we skip this by being more intelligent with out shuffles?
                // I.e. shuffle the bytes into the correct locations instead?
                // One thing I'm unsure of in relation to that is that we would then be back to the problem of
                // no registers holding contiguous values.
                var v0 = mm256_cvtepu8_epi32(s0.Lo128);
                var v1 = mm256_cvtepu8_epi32(s1.Lo128);
                var v2 = mm256_cvtepu8_epi32(s2);
                var v3 = mm256_cvtepu8_epi32(s0.Hi128);
                var v4 = mm256_cvtepu8_epi32(s1.Hi128);

                // OR'ing takes us to the biased f32 range without decimal precision 
                v0 = mm256_or_ps(v0, i32_to_f32_biased);
                v1 = mm256_or_ps(v1, i32_to_f32_biased);
                v2 = mm256_or_ps(v2, i32_to_f32_biased);
                v3 = mm256_or_ps(v3, i32_to_f32_biased);
                v4 = mm256_or_ps(v4, i32_to_f32_biased);

                // Divide by 255.0f to get us in normalized RGB range, then
                // subtract (8388608.0f / 255.0f) to remove the bias we added to
                // do int to float conversion. Alpha lane just gets the value 1
                // added because it's 0 after the initial shuffling. 
                v0 = mm256_fmadd_ps(v0, _1_div_255, f32_biased_to_f32_normalized);
                v1 = mm256_fmadd_ps(v1, _1_div_255, f32_biased_to_f32_normalized);
                v2 = mm256_fmadd_ps(v2, _1_div_255, f32_biased_to_f32_normalized);
                v3 = mm256_fmadd_ps(v3, _1_div_255, f32_biased_to_f32_normalized);
                v4 = mm256_fmadd_ps(v4, _1_div_255, f32_biased_to_f32_normalized);

                rgba.ReinterpretStore(i + 0, v0);
                rgba.ReinterpretStore(i + 2, v1);
                rgba.ReinterpretStore(i + 4, v2);
                rgba.ReinterpretStore(i + 6, v3);
                rgba.ReinterpretStore(i + 8, v4);
            }

            for (; i < count; i++)
                rgba[i] = new float4(rgb[i * 3 + 0] / 255.0f, rgb[i * 3 + 1] / 255.0f, rgb[i * 3 + 2] / 255.0f, 1.0f);
        }
    }

    [BurstCompile]
    public class V256Implementation8xV0 : Tests
    {
        public override int ColorsNeededForVerifyingAllPaths => 11;

        public override void ConvertMultiple(NativeArray<byte> rgb,
                                             NativeArray<float4> rgba)
        {
            Implementation(ref rgb, ref rgba);
        }

        [BurstCompile]
        public static unsafe void Implementation(ref NativeArray<byte> rgb,
                                                 ref NativeArray<float4> rgba)
        {
            // Input:
            //  u128    0                   1
            //  u64     0         1         2         3
            //  u32     0    1    2    3    4    5    6    7
            //  u16     0 1  2 3  4 5  6 7  8 9  0 1  2 3  4 5
            //  u8      0123 4567 8901 2345 6789 0123 4567 8901
            //          RGBR GBRG BRGB RGBR GBRG BRGB RGBR GBRG 
            //          0  1   2   3   4  5   6   7   8  9
            //
            // Registers: 
            // 
            // Shuffle into place:
            //          RGBR GBRG BRGB RGBR GBRG BRGB
            //          0123 4567 89AB CDEF GHIJ KLMN ---- ----
            // all      0123 4567 89AB ---- CDEF GHIJ KLMN ---- permute
            // r        0369 ---- ---- ---- CFIL ---- ---- ---- shuffle
            // g        147A ---- ---- ---- DGJM ---- ---- ---- shuffle
            // b        258B ---- ---- ---- EHKN ---- ---- ---- shuffle
            //
            //          RGBR GBRG BRGB RGBR GBRG BRGB
            //          0123 4567 89AB CDEF GHIJ KLMN ---- ----
            // r        0369 CFIL ---- ---- ---- ---- ---- ---- alignr?
            // g        147A DGJM ---- ---- ---- ---- ---- ---- alignr?
            // b        258B EHKN ---- ---- ---- ---- ---- ---- alignr?

            // Need at minimum one permute.
            // But might be that there are shortcuts that could be taken to avoid more?

            var count = rgb.Length / 3;
            var aligned_byte_count = rgb.Length / 32;
            var aligned_count = aligned_byte_count * 8;

            var alpha = mm256_set1_ps(1.0f);

            int i = 0;
            for (; i < aligned_count; i += 8)
            {

                var all = rgb.ReinterpretLoad<v256>(i * 3);

                // For next version, see if it's possible to organize these
                // differently such that when we unpack them, we end up with
                // color 0 and 1, as opposed to color 0 and 4. Not exactly sure
                // what the order will be for that, but inspect it.
                var organized = mm256_permutevar8x32_epi32(all, mm256_setr_epi32(0, 1, 2, -1, 3, 4, 5, -1));

                var r0 = mm256_shuffle_epi8(organized, mm256_setr_epi8(0, 3, 6, 9,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0, 3, 6, 9,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF));

                var r1 = unpacklo_epi32(r0.Lo128, r0.Hi128);
                var r2 = mm256_cvtepu8_epi32(r1);
                var r3 = mm256_cvtepi32_ps(r2);
                var r4 = mm256_div_ps(r3, mm256_set1_ps(255.0f));

                var g0 = mm256_shuffle_epi8(organized, mm256_setr_epi8(1, 4, 7, 10,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       1, 4, 7, 10,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF));
                var g1 = unpacklo_epi32(g0.Lo128, g0.Hi128);
                var g2 = mm256_cvtepu8_epi32(g1);
                var g3 = mm256_cvtepi32_ps(g2);
                var g4 = mm256_div_ps(g3, mm256_set1_ps(255.0f));

                var b0 = mm256_shuffle_epi8(organized, mm256_setr_epi8(2, 5, 8, 11,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       2, 5, 8, 11,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF));
                var b1 = unpacklo_epi32(b0.Lo128, b0.Hi128);
                var b2 = mm256_cvtepu8_epi32(b1);
                var b3 = mm256_cvtepi32_ps(b2);
                var b4 = mm256_div_ps(b3, mm256_set1_ps(255.0f));

                var c0c1c4c5_rg = mm256_unpacklo_epi32(r4, g4);
                var c2c3c6c7_rg = mm256_unpackhi_epi32(r4, g4);
                var c0c1c4c5_ba = mm256_unpacklo_epi32(b4, alpha);
                var c2c3c6c7_ba = mm256_unpackhi_epi32(b4, alpha);

                var c0c4_rgba = mm256_unpacklo_epi64(c0c1c4c5_rg, c0c1c4c5_ba);
                var c1c5_rgba = mm256_unpackhi_epi64(c0c1c4c5_rg, c0c1c4c5_ba);
                var c2c6_rgba = mm256_unpacklo_epi64(c2c3c6c7_rg, c2c3c6c7_ba);
                var c3c7_rgba = mm256_unpackhi_epi64(c2c3c6c7_rg, c2c3c6c7_ba);

                rgba.ReinterpretStore(i + 0, c0c4_rgba.Lo128);
                rgba.ReinterpretStore(i + 1, c1c5_rgba.Lo128);
                rgba.ReinterpretStore(i + 2, c2c6_rgba.Lo128);
                rgba.ReinterpretStore(i + 3, c3c7_rgba.Lo128);

                rgba.ReinterpretStore(i + 4, c0c4_rgba.Hi128);
                rgba.ReinterpretStore(i + 5, c1c5_rgba.Hi128);
                rgba.ReinterpretStore(i + 6, c2c6_rgba.Hi128);
                rgba.ReinterpretStore(i + 7, c3c7_rgba.Hi128);
            }

            for (; i < count; i++)
            {
                rgba[i] = new float4(rgb[i * 3 + 0] / 255.0f,
                                     rgb[i * 3 + 1] / 255.0f,
                                     rgb[i * 3 + 2] / 255.0f,
                                     1.0f);
            }
        }
    }

    [BurstCompile]
    public class V256Implementation8xV1 : Tests
    {
        public override int ColorsNeededForVerifyingAllPaths => 11;

        public override void ConvertMultiple(NativeArray<byte> rgb,
                                             NativeArray<float4> rgba)
        {
            Implementation(ref rgb, ref rgba);
        }

        [BurstCompile]
        public static unsafe void Implementation(ref NativeArray<byte> rgb,
                                                 ref NativeArray<float4> rgba)
        {
            // Input:
            //  u128    0                   1
            //  u64     0         1         2         3
            //  u32     0    1    2    3    4    5    6    7
            //  u16     0 1  2 3  4 5  6 7  8 9  0 1  2 3  4 5
            //  u8      0123 4567 8901 2345 6789 0123 4567 8901
            //          RGBR GBRG BRGB RGBR GBRG BRGB RGBR GBRG 
            //          0  1   2   3   4  5   6   7   8  9
            //
            // Registers: 
            // 
            // Shuffle into place:
            //          RGBR GBRG BRGB RGBR GBRG BRGB
            //          0123 4567 89AB CDEF GHIJ KLMN ---- ----
            // all      0123 4567 89AB ---- CDEF GHIJ KLMN ---- permute


            var count = rgb.Length / 3;
            var aligned_byte_count = rgb.Length / 32;
            var aligned_count = aligned_byte_count * 8;

            var bias = 8388608.0f;
            var i32_to_f32_biased = mm256_set1_ps(bias);
            var bias_ratio = -(bias / 255.0f);
            var f32_biased_to_f32_normalized = mm256_set1_ps(bias_ratio);
            var _1_div_255 = mm256_set1_ps(1.0f / 255.0f);

            var alpha = mm256_set1_ps(1.0f);

            int i = 0;
            for (; i < aligned_count; i += 8)
            {
                var all = rgb.ReinterpretLoad<v256>(i * 3);

                // Shuffle across boundaries so that RGB values 4, 5, 6, 7 is located in hi 128-bits.
                var organized = mm256_permutevar8x32_epi32(all, mm256_setr_epi32(0, 1, 2, -1, 3, 4, 5, -1));

                // Order component values to the beginning of each 128-bit half.
                var r0 = mm256_shuffle_epi8(organized, mm256_setr_epi8(0, 3, 6, 9,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0, 3, 6, 9,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF));

                var g0 = mm256_shuffle_epi8(organized, mm256_setr_epi8(1, 4, 7, 10,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       1, 4, 7, 10,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF));

                var b0 = mm256_shuffle_epi8(organized, mm256_setr_epi8(2, 5, 8, 11,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       2, 5, 8, 11,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF,
                                                                       0xFF, 0xFF, 0xFF, 0xFF));

                // Order component values to the beginning of their own 128-bit register.
                var r1 = unpacklo_epi32(r0.Lo128, r0.Hi128);
                var g1 = unpacklo_epi32(g0.Lo128, g0.Hi128);
                var b1 = unpacklo_epi32(b0.Lo128, b0.Hi128);

                // Shuffle component values such that when we later unpack to go
                // back to AoS format we'll end up with each 256-bit register
                // containing the correctly ordered RGBA value in the 128-bit
                // register, such that we only need one store pr 2 RGBA values.
                var r2 = shuffle_epi8(r1, setr_epi8(0, 2, 4, 6, 1, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1));
                var g2 = shuffle_epi8(g1, setr_epi8(0, 2, 4, 6, 1, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1));
                var b2 = shuffle_epi8(b1, setr_epi8(0, 2, 4, 6, 1, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1));

                // Convert components to 32-bit
                var r3 = mm256_cvtepu8_epi32(r2);
                var g3 = mm256_cvtepu8_epi32(g2);
                var b3 = mm256_cvtepu8_epi32(b2);

                // Convert components to normalized 32-bit floats using 'short
                // to float' trick. I.e. Divide by 255.0f to get us in normalized RGB
                // range, then subtract (8388608.0f / 255.0f) to remove the bias
                // we added to do int to float conversion.  
                var r4 = mm256_or_si256(r3, i32_to_f32_biased);
                var r5 = mm256_fmadd_ps(r4, _1_div_255, f32_biased_to_f32_normalized);

                var g4 = mm256_or_si256(g3, i32_to_f32_biased);
                var g5 = mm256_fmadd_ps(g4, _1_div_255, f32_biased_to_f32_normalized);

                var b4 = mm256_or_si256(b3, i32_to_f32_biased);
                var b5 = mm256_fmadd_ps(b4, _1_div_255, f32_biased_to_f32_normalized);

                // Unpack and interleave red and green component values
                var c0c1c2c3_rg = mm256_unpacklo_epi32(r5, g5);
                var c4c5c6c7_rg = mm256_unpackhi_epi32(r5, g5);

                // Unpack and interleave blue and alpha component values
                var c0c1c2c3_ba = mm256_unpacklo_epi32(b5, alpha);
                var c4c5c6c7_ba = mm256_unpackhi_epi32(b5, alpha);

                // Unpack and interleave all component values, 
                // creating a complete RGBA color value.
                var c0c1_rgba = mm256_unpacklo_epi64(c0c1c2c3_rg, c0c1c2c3_ba);
                var c2c3_rgba = mm256_unpackhi_epi64(c0c1c2c3_rg, c0c1c2c3_ba);
                var c4c5_rgba = mm256_unpacklo_epi64(c4c5c6c7_rg, c4c5c6c7_ba);
                var c6c7_rgba = mm256_unpackhi_epi64(c4c5c6c7_rg, c4c5c6c7_ba);

                rgba.ReinterpretStore(i + 0, c0c1_rgba);
                rgba.ReinterpretStore(i + 2, c2c3_rgba);
                rgba.ReinterpretStore(i + 4, c4c5_rgba);
                rgba.ReinterpretStore(i + 6, c6c7_rgba);
            }

            for (; i < count; i++)
            {
                rgba[i] = new float4(rgb[i * 3 + 0] / 255.0f,
                                     rgb[i * 3 + 1] / 255.0f,
                                     rgb[i * 3 + 2] / 255.0f,
                                     1.0f);
            }
        }
    }
}