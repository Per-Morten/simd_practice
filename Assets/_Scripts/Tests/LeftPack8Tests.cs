using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit;
using NUnit.Framework;
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
using static SIMDHelpers;
using UnityEngine;
using System.IO;

namespace LeftPackTests
{
    class LeftPack8Tests
    {
        public static void GenerateLUT()
        {
            uint maxValue = 0b11111111;
            for (uint i = 0; i <= maxValue; i++)
            {
                string s = "";
                if ((i & (1 << 0)) != 0)
                    s += "0,";
                if ((i & (1 << 1)) != 0)
                    s += "1,";
                if ((i & (1 << 2)) != 0)
                    s += "2,";
                if ((i & (1 << 3)) != 0)
                    s += "3,";
                if ((i & (1 << 4)) != 0)
                    s += "4,";
                if ((i & (1 << 5)) != 0)
                    s += "5,";
                if ((i & (1 << 6)) != 0)
                    s += "6,";
                if ((i & (1 << 7)) != 0)
                    s += "7,";
                while (s.Length < 14)
                    s += "0,";
                if (s.Length < 16)
                    s += "0";

                Debug.Log($"table[{i}] = new v256({s});");
            }
        }

        /// <summary>
        /// Method to convert an integer to a string containing the number in binary. A negative 
        /// number will be formatted as a 32-character binary number in two's compliment.
        /// </summary>
        /// <param name="theNumber">self-explanatory</param>
        /// <param name="minimumDigits">if binary number contains fewer characters leading zeros are added</param>
        /// <returns>string as described above</returns>
        public static string IntegerToBinaryString(int theNumber, int minimumDigits)
        {
            return Convert.ToString(theNumber, 2).PadLeft(minimumDigits, '0');
        }

        public static void GenerateTests()
        {
#if false
            uint maxValue = 0b11111111;
            var l = new List<int>();
            var s = new StringBuilder();
            for (uint i = 0; i <= maxValue; i++)
            {
                s.Clear();
                l.Clear();
                if ((i & (1 << 0)) != 0)
                    l.Add(1);
                else
                    l.Add(-1);
                if ((i & (1 << 1)) != 0)
                    l.Add(2);
                else
                    l.Add(-1);
                if ((i & (1 << 2)) != 0)
                    l.Add(3);
                else
                    l.Add(-1);
                if ((i & (1 << 3)) != 0)
                    l.Add(4);
                else
                    l.Add(-1);
                if ((i & (1 << 4)) != 0)
                    l.Add(5);
                else
                    l.Add(-1);
                if ((i & (1 << 5)) != 0)
                    l.Add(6);
                else
                    l.Add(-1);
                if ((i & (1 << 6)) != 0)
                    l.Add(7);
                else
                    l.Add(-1);
                if ((i & (1 << 7)) != 0)
                    l.Add(8);
                else
                    l.Add(-1);
                while (l.Count < 8)
                    l.Add(-1);

                s.AppendLine($"[Test]\npublic static void Test{IntegerToBinaryString((int)i >> 4 & 0xF, 4)}_{IntegerToBinaryString((int)i >> 0 & 0xF, 4)}()\n{{");
                s.AppendLine($"var values = new v256({l[0]},{l[1]},{l[2]},{l[3]},{l[4]},{l[5]},{l[6]},{l[7]});");
                s.AppendLine("var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);");
                s.Append("Assert.IsTrue(");
                var t = l.Where(x => x > 0).ToList();
                int j = 0;
                for (; j < t.Count; j++)
                {
                    s.Append($"res.SInt{j} == {t[j]}");
                    if (j != 7)
                        s.Append(" && ");
                    else
                        s.Append(");\n}\n");
                }
                for (; j < 8; j++)
                {
                    if ((i & 0xF) == 0)
                        s.Append($"res.SInt{j} == values.SInt0");
                    else
                        s.Append($"res.SInt{j} == res.SInt0");
                    if (j != 7)
                        s.Append(" && ");
                    else
                        s.Append(");\n}\n");
                }
                File.AppendAllText($"tests.txt", s.ToString());
            }
#endif
        }

        public static void GenerateLUT3()
        {
            var l = new List<int>();
            //for (int i = 0; i < 4; i++)
            //{
            //    l.Clear();
            //    for (int j = 0; j <= i; j++)
            //        l.Add(j);
            //    while (l.Count < 8)
            //        l.Add(0);
            //    Debug.Log($"table[{i}] = new v256({l[0]},{l[1]},{l[2]},{l[3]},{l[4]},{l[5]},{l[6]},{l[7]});");
            //}

            //for (int i = 0; i < 4; i++)
            //{
            //    l.Clear();
            //    for (int j = 0; j <= i; j++)
            //        l.Add(j + 4);
            //    while (l.Count < 8)
            //        l.Add(0);
            //    Debug.Log($"table[{i}] = new v256({l[0]},{l[1]},{l[2]},{l[3]},{l[4]},{l[5]},{l[6]},{l[7]});");
            //}

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    l.Clear();
                    for (int k = 0; k <= i; k++)
                    {
                        l.Add(k);
                    }

                    for (int k = 0; k <= j; k++)
                    {
                        l.Add(k + 4);
                    }
                    while (l.Count < 8)
                        l.Add(0);
                    Debug.Log($"table[{i * 4 + j}] = new v256({l[0]},{l[1]},{l[2]},{l[3]},{l[4]},{l[5]},{l[6]},{l[7]});");
                }
            }

            //for (int i = 0; i < 4; i++)
            //{
            //    l.Clear();
            //    for (int j = 0; j <= i; j++)
            //    {
            //        l.Add(j);
            //    }
            //    for (int j = 0; j <= i; j++)
            //    {
            //        l.Add(j + 4);
            //    }
            //    while (l.Count < 8)
            //        l.Add(0);
            //    Debug.Log($"table[{i}] = new v256({l[0]},{l[1]},{l[2]},{l[3]},{l[4]},{l[5]},{l[6]},{l[7]});");
            //}

            //uint maxValue = 0b11111111;
            //for (uint i = 0; i <= maxValue; i++)
            //{
            //    if ((i & (1 << 0)) != 0)
            //        l.Add(0);
            //    if ((i & (1 << 1)) != 0)
            //        l.Add(1);
            //    if ((i & (1 << 2)) != 0)
            //        l.Add(2);
            //    if ((i & (1 << 3)) != 0)
            //        l.Add(3);
            //    if ((i & (1 << 4)) != 0)
            //        l.Add(4);
            //    if ((i & (1 << 5)) != 0)
            //        l.Add(5);
            //    if ((i & (1 << 6)) != 0)
            //        l.Add(6);
            //    if ((i & (1 << 7)) != 0)
            //        l.Add(7);
            //    l.Sort();
            //}
        }

        #region Generated
        [Test]
        public static void Test0000_0000()
        {
            var values = new v256(-1, -1, -1, -1, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == values.SInt0 && res.SInt1 == values.SInt0 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test0000_0001()
        {
            var values = new v256(1, -1, -1, -1, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == res.SInt0 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_0010()
        {
            var values = new v256(-1, 2, -1, -1, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == res.SInt0 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_0011()
        {
            var values = new v256(1, 2, -1, -1, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_0100()
        {
            var values = new v256(-1, -1, 3, -1, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == res.SInt0 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_0101()
        {
            var values = new v256(1, -1, 3, -1, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_0110()
        {
            var values = new v256(-1, 2, 3, -1, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_0111()
        {
            var values = new v256(1, 2, 3, -1, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_1000()
        {
            var values = new v256(-1, -1, -1, 4, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == res.SInt0 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_1001()
        {
            var values = new v256(1, -1, -1, 4, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_1010()
        {
            var values = new v256(-1, 2, -1, 4, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_1011()
        {
            var values = new v256(1, 2, -1, 4, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_1100()
        {
            var values = new v256(-1, -1, 3, 4, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_1101()
        {
            var values = new v256(1, -1, 3, 4, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_1110()
        {
            var values = new v256(-1, 2, 3, 4, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0000_1111()
        {
            var values = new v256(1, 2, 3, 4, -1, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_0000()
        {
            var values = new v256(-1, -1, -1, -1, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 5 && res.SInt1 == values.SInt0 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test0001_0001()
        {
            var values = new v256(1, -1, -1, -1, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 5 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_0010()
        {
            var values = new v256(-1, 2, -1, -1, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 5 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_0011()
        {
            var values = new v256(1, 2, -1, -1, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 5 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_0100()
        {
            var values = new v256(-1, -1, 3, -1, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 5 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_0101()
        {
            var values = new v256(1, -1, 3, -1, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_0110()
        {
            var values = new v256(-1, 2, 3, -1, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_0111()
        {
            var values = new v256(1, 2, 3, -1, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 5 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_1000()
        {
            var values = new v256(-1, -1, -1, 4, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 5 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_1001()
        {
            var values = new v256(1, -1, -1, 4, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_1010()
        {
            var values = new v256(-1, 2, -1, 4, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_1011()
        {
            var values = new v256(1, 2, -1, 4, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_1100()
        {
            var values = new v256(-1, -1, 3, 4, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_1101()
        {
            var values = new v256(1, -1, 3, 4, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_1110()
        {
            var values = new v256(-1, 2, 3, 4, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0001_1111()
        {
            var values = new v256(1, 2, 3, 4, 5, -1, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 5 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_0000()
        {
            var values = new v256(-1, -1, -1, -1, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 6 && res.SInt1 == values.SInt0 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test0010_0001()
        {
            var values = new v256(1, -1, -1, -1, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 6 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_0010()
        {
            var values = new v256(-1, 2, -1, -1, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 6 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_0011()
        {
            var values = new v256(1, 2, -1, -1, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 6 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_0100()
        {
            var values = new v256(-1, -1, 3, -1, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 6 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_0101()
        {
            var values = new v256(1, -1, 3, -1, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 6 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_0110()
        {
            var values = new v256(-1, 2, 3, -1, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 6 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_0111()
        {
            var values = new v256(1, 2, 3, -1, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 6 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_1000()
        {
            var values = new v256(-1, -1, -1, 4, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 6 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_1001()
        {
            var values = new v256(1, -1, -1, 4, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_1010()
        {
            var values = new v256(-1, 2, -1, 4, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_1011()
        {
            var values = new v256(1, 2, -1, 4, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_1100()
        {
            var values = new v256(-1, -1, 3, 4, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_1101()
        {
            var values = new v256(1, -1, 3, 4, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_1110()
        {
            var values = new v256(-1, 2, 3, 4, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0010_1111()
        {
            var values = new v256(1, 2, 3, 4, -1, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 6 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_0000()
        {
            var values = new v256(-1, -1, -1, -1, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 5 && res.SInt1 == 6 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test0011_0001()
        {
            var values = new v256(1, -1, -1, -1, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_0010()
        {
            var values = new v256(-1, 2, -1, -1, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_0011()
        {
            var values = new v256(1, 2, -1, -1, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_0100()
        {
            var values = new v256(-1, -1, 3, -1, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_0101()
        {
            var values = new v256(1, -1, 3, -1, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_0110()
        {
            var values = new v256(-1, 2, 3, -1, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_0111()
        {
            var values = new v256(1, 2, 3, -1, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_1000()
        {
            var values = new v256(-1, -1, -1, 4, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_1001()
        {
            var values = new v256(1, -1, -1, 4, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_1010()
        {
            var values = new v256(-1, 2, -1, 4, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_1011()
        {
            var values = new v256(1, 2, -1, 4, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_1100()
        {
            var values = new v256(-1, -1, 3, 4, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_1101()
        {
            var values = new v256(1, -1, 3, 4, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_1110()
        {
            var values = new v256(-1, 2, 3, 4, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0011_1111()
        {
            var values = new v256(1, 2, 3, 4, 5, 6, -1, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 5 && res.SInt5 == 6 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_0000()
        {
            var values = new v256(-1, -1, -1, -1, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 7 && res.SInt1 == values.SInt0 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test0100_0001()
        {
            var values = new v256(1, -1, -1, -1, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 7 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_0010()
        {
            var values = new v256(-1, 2, -1, -1, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 7 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_0011()
        {
            var values = new v256(1, 2, -1, -1, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_0100()
        {
            var values = new v256(-1, -1, 3, -1, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 7 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_0101()
        {
            var values = new v256(1, -1, 3, -1, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_0110()
        {
            var values = new v256(-1, 2, 3, -1, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_0111()
        {
            var values = new v256(1, 2, 3, -1, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_1000()
        {
            var values = new v256(-1, -1, -1, 4, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 7 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_1001()
        {
            var values = new v256(1, -1, -1, 4, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_1010()
        {
            var values = new v256(-1, 2, -1, 4, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_1011()
        {
            var values = new v256(1, 2, -1, 4, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_1100()
        {
            var values = new v256(-1, -1, 3, 4, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_1101()
        {
            var values = new v256(1, -1, 3, 4, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_1110()
        {
            var values = new v256(-1, 2, 3, 4, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0100_1111()
        {
            var values = new v256(1, 2, 3, 4, -1, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_0000()
        {
            var values = new v256(-1, -1, -1, -1, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 5 && res.SInt1 == 7 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test0101_0001()
        {
            var values = new v256(1, -1, -1, -1, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 5 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_0010()
        {
            var values = new v256(-1, 2, -1, -1, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 5 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_0011()
        {
            var values = new v256(1, 2, -1, -1, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_0100()
        {
            var values = new v256(-1, -1, 3, -1, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 5 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_0101()
        {
            var values = new v256(1, -1, 3, -1, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_0110()
        {
            var values = new v256(-1, 2, 3, -1, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_0111()
        {
            var values = new v256(1, 2, 3, -1, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 5 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_1000()
        {
            var values = new v256(-1, -1, -1, 4, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 5 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_1001()
        {
            var values = new v256(1, -1, -1, 4, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_1010()
        {
            var values = new v256(-1, 2, -1, 4, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_1011()
        {
            var values = new v256(1, 2, -1, 4, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_1100()
        {
            var values = new v256(-1, -1, 3, 4, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_1101()
        {
            var values = new v256(1, -1, 3, 4, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_1110()
        {
            var values = new v256(-1, 2, 3, 4, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0101_1111()
        {
            var values = new v256(1, 2, 3, 4, 5, -1, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 5 && res.SInt5 == 7 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_0000()
        {
            var values = new v256(-1, -1, -1, -1, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 6 && res.SInt1 == 7 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test0110_0001()
        {
            var values = new v256(1, -1, -1, -1, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 6 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_0010()
        {
            var values = new v256(-1, 2, -1, -1, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 6 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_0011()
        {
            var values = new v256(1, 2, -1, -1, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_0100()
        {
            var values = new v256(-1, -1, 3, -1, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 6 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_0101()
        {
            var values = new v256(1, -1, 3, -1, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_0110()
        {
            var values = new v256(-1, 2, 3, -1, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_0111()
        {
            var values = new v256(1, 2, 3, -1, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_1000()
        {
            var values = new v256(-1, -1, -1, 4, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 6 && res.SInt2 == 7 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_1001()
        {
            var values = new v256(1, -1, -1, 4, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_1010()
        {
            var values = new v256(-1, 2, -1, 4, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_1011()
        {
            var values = new v256(1, 2, -1, 4, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_1100()
        {
            var values = new v256(-1, -1, 3, 4, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_1101()
        {
            var values = new v256(1, -1, 3, 4, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_1110()
        {
            var values = new v256(-1, 2, 3, 4, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0110_1111()
        {
            var values = new v256(1, 2, 3, 4, -1, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 6 && res.SInt5 == 7 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_0000()
        {
            var values = new v256(-1, -1, -1, -1, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 5 && res.SInt1 == 6 && res.SInt2 == 7 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test0111_0001()
        {
            var values = new v256(1, -1, -1, -1, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_0010()
        {
            var values = new v256(-1, 2, -1, -1, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_0011()
        {
            var values = new v256(1, 2, -1, -1, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_0100()
        {
            var values = new v256(-1, -1, 3, -1, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_0101()
        {
            var values = new v256(1, -1, 3, -1, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_0110()
        {
            var values = new v256(-1, 2, 3, -1, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_0111()
        {
            var values = new v256(1, 2, 3, -1, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 7 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_1000()
        {
            var values = new v256(-1, -1, -1, 4, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_1001()
        {
            var values = new v256(1, -1, -1, 4, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_1010()
        {
            var values = new v256(-1, 2, -1, 4, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_1011()
        {
            var values = new v256(1, 2, -1, 4, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 7 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_1100()
        {
            var values = new v256(-1, -1, 3, 4, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_1101()
        {
            var values = new v256(1, -1, 3, 4, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 7 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_1110()
        {
            var values = new v256(-1, 2, 3, 4, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 7 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test0111_1111()
        {
            var values = new v256(1, 2, 3, 4, 5, 6, 7, -1);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 5 && res.SInt5 == 6 && res.SInt6 == 7 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_0000()
        {
            var values = new v256(-1, -1, -1, -1, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 8 && res.SInt1 == values.SInt0 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test1000_0001()
        {
            var values = new v256(1, -1, -1, -1, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 8 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_0010()
        {
            var values = new v256(-1, 2, -1, -1, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 8 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_0011()
        {
            var values = new v256(1, 2, -1, -1, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_0100()
        {
            var values = new v256(-1, -1, 3, -1, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 8 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_0101()
        {
            var values = new v256(1, -1, 3, -1, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_0110()
        {
            var values = new v256(-1, 2, 3, -1, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_0111()
        {
            var values = new v256(1, 2, 3, -1, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_1000()
        {
            var values = new v256(-1, -1, -1, 4, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 8 && res.SInt2 == res.SInt0 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_1001()
        {
            var values = new v256(1, -1, -1, 4, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_1010()
        {
            var values = new v256(-1, 2, -1, 4, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_1011()
        {
            var values = new v256(1, 2, -1, 4, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_1100()
        {
            var values = new v256(-1, -1, 3, 4, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_1101()
        {
            var values = new v256(1, -1, 3, 4, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_1110()
        {
            var values = new v256(-1, 2, 3, 4, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1000_1111()
        {
            var values = new v256(1, 2, 3, 4, -1, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_0000()
        {
            var values = new v256(-1, -1, -1, -1, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 5 && res.SInt1 == 8 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test1001_0001()
        {
            var values = new v256(1, -1, -1, -1, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 5 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_0010()
        {
            var values = new v256(-1, 2, -1, -1, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 5 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_0011()
        {
            var values = new v256(1, 2, -1, -1, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 5 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_0100()
        {
            var values = new v256(-1, -1, 3, -1, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 5 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_0101()
        {
            var values = new v256(1, -1, 3, -1, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_0110()
        {
            var values = new v256(-1, 2, 3, -1, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_0111()
        {
            var values = new v256(1, 2, 3, -1, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 5 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_1000()
        {
            var values = new v256(-1, -1, -1, 4, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 5 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_1001()
        {
            var values = new v256(1, -1, -1, 4, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_1010()
        {
            var values = new v256(-1, 2, -1, 4, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_1011()
        {
            var values = new v256(1, 2, -1, 4, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_1100()
        {
            var values = new v256(-1, -1, 3, 4, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_1101()
        {
            var values = new v256(1, -1, 3, 4, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_1110()
        {
            var values = new v256(-1, 2, 3, 4, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1001_1111()
        {
            var values = new v256(1, 2, 3, 4, 5, -1, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 5 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_0000()
        {
            var values = new v256(-1, -1, -1, -1, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 6 && res.SInt1 == 8 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test1010_0001()
        {
            var values = new v256(1, -1, -1, -1, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 6 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_0010()
        {
            var values = new v256(-1, 2, -1, -1, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 6 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_0011()
        {
            var values = new v256(1, 2, -1, -1, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 6 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_0100()
        {
            var values = new v256(-1, -1, 3, -1, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 6 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_0101()
        {
            var values = new v256(1, -1, 3, -1, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 6 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_0110()
        {
            var values = new v256(-1, 2, 3, -1, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 6 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_0111()
        {
            var values = new v256(1, 2, 3, -1, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 6 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_1000()
        {
            var values = new v256(-1, -1, -1, 4, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 6 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_1001()
        {
            var values = new v256(1, -1, -1, 4, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_1010()
        {
            var values = new v256(-1, 2, -1, 4, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_1011()
        {
            var values = new v256(1, 2, -1, 4, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_1100()
        {
            var values = new v256(-1, -1, 3, 4, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_1101()
        {
            var values = new v256(1, -1, 3, 4, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_1110()
        {
            var values = new v256(-1, 2, 3, 4, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1010_1111()
        {
            var values = new v256(1, 2, 3, 4, -1, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 6 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_0000()
        {
            var values = new v256(-1, -1, -1, -1, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 5 && res.SInt1 == 6 && res.SInt2 == 8 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test1011_0001()
        {
            var values = new v256(1, -1, -1, -1, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_0010()
        {
            var values = new v256(-1, 2, -1, -1, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_0011()
        {
            var values = new v256(1, 2, -1, -1, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_0100()
        {
            var values = new v256(-1, -1, 3, -1, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_0101()
        {
            var values = new v256(1, -1, 3, -1, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_0110()
        {
            var values = new v256(-1, 2, 3, -1, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_0111()
        {
            var values = new v256(1, 2, 3, -1, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_1000()
        {
            var values = new v256(-1, -1, -1, 4, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_1001()
        {
            var values = new v256(1, -1, -1, 4, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_1010()
        {
            var values = new v256(-1, 2, -1, 4, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_1011()
        {
            var values = new v256(1, 2, -1, 4, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_1100()
        {
            var values = new v256(-1, -1, 3, 4, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_1101()
        {
            var values = new v256(1, -1, 3, 4, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_1110()
        {
            var values = new v256(-1, 2, 3, 4, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1011_1111()
        {
            var values = new v256(1, 2, 3, 4, 5, 6, -1, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 5 && res.SInt5 == 6 && res.SInt6 == 8 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_0000()
        {
            var values = new v256(-1, -1, -1, -1, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 7 && res.SInt1 == 8 && res.SInt2 == values.SInt0 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test1100_0001()
        {
            var values = new v256(1, -1, -1, -1, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 7 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_0010()
        {
            var values = new v256(-1, 2, -1, -1, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 7 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_0011()
        {
            var values = new v256(1, 2, -1, -1, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_0100()
        {
            var values = new v256(-1, -1, 3, -1, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 7 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_0101()
        {
            var values = new v256(1, -1, 3, -1, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_0110()
        {
            var values = new v256(-1, 2, 3, -1, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_0111()
        {
            var values = new v256(1, 2, 3, -1, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_1000()
        {
            var values = new v256(-1, -1, -1, 4, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 7 && res.SInt2 == 8 && res.SInt3 == res.SInt0 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_1001()
        {
            var values = new v256(1, -1, -1, 4, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_1010()
        {
            var values = new v256(-1, 2, -1, 4, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_1011()
        {
            var values = new v256(1, 2, -1, 4, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_1100()
        {
            var values = new v256(-1, -1, 3, 4, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_1101()
        {
            var values = new v256(1, -1, 3, 4, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_1110()
        {
            var values = new v256(-1, 2, 3, 4, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1100_1111()
        {
            var values = new v256(1, 2, 3, 4, -1, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_0000()
        {
            var values = new v256(-1, -1, -1, -1, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 5 && res.SInt1 == 7 && res.SInt2 == 8 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test1101_0001()
        {
            var values = new v256(1, -1, -1, -1, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 5 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_0010()
        {
            var values = new v256(-1, 2, -1, -1, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 5 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_0011()
        {
            var values = new v256(1, 2, -1, -1, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_0100()
        {
            var values = new v256(-1, -1, 3, -1, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 5 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_0101()
        {
            var values = new v256(1, -1, 3, -1, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_0110()
        {
            var values = new v256(-1, 2, 3, -1, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_0111()
        {
            var values = new v256(1, 2, 3, -1, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 5 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_1000()
        {
            var values = new v256(-1, -1, -1, 4, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 5 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_1001()
        {
            var values = new v256(1, -1, -1, 4, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_1010()
        {
            var values = new v256(-1, 2, -1, 4, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_1011()
        {
            var values = new v256(1, 2, -1, 4, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_1100()
        {
            var values = new v256(-1, -1, 3, 4, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_1101()
        {
            var values = new v256(1, -1, 3, 4, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_1110()
        {
            var values = new v256(-1, 2, 3, 4, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1101_1111()
        {
            var values = new v256(1, 2, 3, 4, 5, -1, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 5 && res.SInt5 == 7 && res.SInt6 == 8 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_0000()
        {
            var values = new v256(-1, -1, -1, -1, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 6 && res.SInt1 == 7 && res.SInt2 == 8 && res.SInt3 == values.SInt0 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test1110_0001()
        {
            var values = new v256(1, -1, -1, -1, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 6 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_0010()
        {
            var values = new v256(-1, 2, -1, -1, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 6 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_0011()
        {
            var values = new v256(1, 2, -1, -1, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_0100()
        {
            var values = new v256(-1, -1, 3, -1, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 6 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_0101()
        {
            var values = new v256(1, -1, 3, -1, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_0110()
        {
            var values = new v256(-1, 2, 3, -1, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_0111()
        {
            var values = new v256(1, 2, 3, -1, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_1000()
        {
            var values = new v256(-1, -1, -1, 4, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 6 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == res.SInt0 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_1001()
        {
            var values = new v256(1, -1, -1, 4, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_1010()
        {
            var values = new v256(-1, 2, -1, 4, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_1011()
        {
            var values = new v256(1, 2, -1, 4, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_1100()
        {
            var values = new v256(-1, -1, 3, 4, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_1101()
        {
            var values = new v256(1, -1, 3, 4, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_1110()
        {
            var values = new v256(-1, 2, 3, 4, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1110_1111()
        {
            var values = new v256(1, 2, 3, 4, -1, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 6 && res.SInt5 == 7 && res.SInt6 == 8 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_0000()
        {
            var values = new v256(-1, -1, -1, -1, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 5 && res.SInt1 == 6 && res.SInt2 == 7 && res.SInt3 == 8 && res.SInt4 == values.SInt0 && res.SInt5 == values.SInt0 && res.SInt6 == values.SInt0 && res.SInt7 == values.SInt0);
        }
        [Test]
        public static void Test1111_0001()
        {
            var values = new v256(1, -1, -1, -1, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_0010()
        {
            var values = new v256(-1, 2, -1, -1, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_0011()
        {
            var values = new v256(1, 2, -1, -1, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_0100()
        {
            var values = new v256(-1, -1, 3, -1, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_0101()
        {
            var values = new v256(1, -1, 3, -1, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_0110()
        {
            var values = new v256(-1, 2, 3, -1, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_0111()
        {
            var values = new v256(1, 2, 3, -1, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 7 && res.SInt6 == 8 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_1000()
        {
            var values = new v256(-1, -1, -1, 4, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 4 && res.SInt1 == 5 && res.SInt2 == 6 && res.SInt3 == 7 && res.SInt4 == 8 && res.SInt5 == res.SInt0 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_1001()
        {
            var values = new v256(1, -1, -1, 4, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_1010()
        {
            var values = new v256(-1, 2, -1, 4, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_1011()
        {
            var values = new v256(1, 2, -1, 4, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 7 && res.SInt6 == 8 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_1100()
        {
            var values = new v256(-1, -1, 3, 4, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 3 && res.SInt1 == 4 && res.SInt2 == 5 && res.SInt3 == 6 && res.SInt4 == 7 && res.SInt5 == 8 && res.SInt6 == res.SInt0 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_1101()
        {
            var values = new v256(1, -1, 3, 4, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 7 && res.SInt6 == 8 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_1110()
        {
            var values = new v256(-1, 2, 3, 4, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 2 && res.SInt1 == 3 && res.SInt2 == 4 && res.SInt3 == 5 && res.SInt4 == 6 && res.SInt5 == 7 && res.SInt6 == 8 && res.SInt7 == res.SInt0);
        }
        [Test]
        public static void Test1111_1111()
        {
            var values = new v256(1, 2, 3, 4, 5, 6, 7, 8);
            var res = LeftPack8PS(mm256_cmpgt_epi32(values, mm256_set1_ps(0)), values);
            Assert.IsTrue(res.SInt0 == 1 && res.SInt1 == 2 && res.SInt2 == 3 && res.SInt3 == 4 && res.SInt4 == 5 && res.SInt5 == 6 && res.SInt6 == 7 && res.SInt7 == 8);
        }

        #endregion
    }




}
