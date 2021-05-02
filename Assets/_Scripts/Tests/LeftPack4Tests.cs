using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit;
using NUnit.Framework;
using static SIMDHelpers;
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
using Unity.Collections;

namespace LeftPackTests
{
    class LeftPack4Tests
    {
        [Test]
        public static void Test0000()
        {
            var values = new v128(-1, -1, -1, -1);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == values.SInt0 && res.SInt1 == values.SInt0 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test0001()
        {
            var values = new v128(1, -1, -1, -1);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == values.SInt0 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test0010()
        {
            var values = new v128(-1, 2, -1, -1);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == values.SInt0 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test0011()
        {
            var values = new v128(1, 2, -1, -1);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test0100()
        {
            var values = new v128(-1, -1, 3, -1);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == values.SInt0 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test0101()
        {
            var values = new v128(1, -1, 3, -1);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test0110()
        {
            var values = new v128(-1, 2, 3, -1);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test0111()
        {
            var values = new v128(1, 2, 3, -1);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test1000()
        {
            var values = new v128(-1, -1, -1, 4);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == values.SInt0 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test1001()
        {
            var values = new v128(1, -1, -1, 4);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test1010()
        {
            var values = new v128(-1, 2, -1, 4);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test1011()
        {
            var values = new v128(1, 2, -1, 4);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test1100()
        {
            var values = new v128(-1, -1, 3, 4);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test1101()
        {
            var values = new v128(1, -1, 3, 4);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test1110()
        {
            var values = new v128(-1, 2, 3, 4);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == values.SInt0);
        }

        [Test]
        public static void Test1111()
        {
            var values = new v128(1, 2, 3, 4);
            var res = LeftPack4PS(cmpgt_epi32(values, set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4);
        }
    }
}
