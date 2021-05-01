using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
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
using System.Runtime.CompilerServices;
using System.Globalization;

namespace CurvesOptimized
{
    [BurstCompile]
    public static class OptimizedHelpers
    {
        [BurstCompile]
        public static unsafe void GetLeftTangent(int last, Vector2* pts, int ptsCount, float* arclen, int arclenCount, out Vector2 retval)
        {
            float totalLen = arclen[arclenCount - 1];
            Vector2 p0 = pts[0];
            Vector2 tanL = VectorHelper.Normalize(pts[1] - p0);
            Vector2 total = tanL;
            float weightTotal = 1;
            last = Math.Min(CurveFitBase.END_TANGENT_N_PTS, last - 1);
            for (int i = 2; i <= last; i++)
            {
                float ti = 1 - (arclen[i] / totalLen);
                float weight = ti * ti * ti;
                Vector2 v = VectorHelper.Normalize(pts[i] - p0);
                total += v * weight;
                weightTotal += weight;
            }
            // if the vectors add up to zero (ie going opposite directions), there's no way to normalize them
            if (VectorHelper.Length(total) > CurveFitBase.EPSILON)
                tanL = VectorHelper.Normalize(total / weightTotal);
            retval = tanL;
        }

        [BurstCompile]
        public static unsafe void GetRightTangent(int first, Vector2* pts, int ptsCount, float* arclen, int arclenCount, out Vector2 retval)
        {
            var totalLen = arclen[arclenCount - 1];
            var p3 = pts[ptsCount - 1];
            var tanR = VectorHelper.Normalize(pts[ptsCount - 2] - p3);
            var total = tanR;
            var weightTotal = 1.0f;
            first = Math.Max(ptsCount - (CurveFitBase.END_TANGENT_N_PTS + 1), first + 1);
            for (int i = ptsCount - 3; i >= first; i--)
            {
                var t = arclen[i] / totalLen;
                var weight = t * t * t;
                var v = VectorHelper.Normalize(pts[i] - p3);
                total += v * weight;
                weightTotal += weight;
            }
            if (VectorHelper.Length(total) > CurveFitBase.EPSILON)
                tanR = VectorHelper.Normalize(total / weightTotal);
            retval = tanR;
        }

        public static unsafe void GenerateBezier(int first, int last, in Vector2 tanL, in Vector2 tanR, Vector2* pts, float* u, out CubicBezier retVal)
        {
            GenerateBezierV128V1(first, last, tanL, tanR, pts, u, out retVal);
            //GenerateBezierStandard(first, last, tanL, tanR, pts, u, out retVal);
        }

        [BurstCompile]
        public static unsafe void GenerateBezierStandard(int first, int last, in Vector2 tanL, in Vector2 tanR, Vector2* pts, float* u, out CubicBezier retVal)
        {
            int nPts = last - first + 1;
            Vector2 p0 = pts[first], p3 = pts[last]; // first and last points of curve are actual points on data
            float c00 = 0, c01 = 0, c11 = 0, x0 = 0, x1 = 0; // matrix members -- both C[0,1] and C[1,0] are the same, stored in c01
            for (int i = 1; i < nPts; i++)
            {
                // Calculate cubic bezier multipliers
                float t = u[i];
                float ti = 1 - t;
                float t0 = ti * ti * ti;
                float t1 = 3 * ti * ti * t;
                float t2 = 3 * ti * t * t;
                float t3 = t * t * t;

                // For X matrix; moving this up here since profiling shows it's better up here (maybe a0/a1 not in registers vs only v not in regs)
                Vector2 s = (p0 * t0) + (p0 * t1) + (p3 * t2) + (p3 * t3); // NOTE: this would be Q(t) if p1=p0 and p2=p3
                Vector2 v = pts[first + i] - s;

                // C matrix
                Vector2 a0 = tanL * t1;
                Vector2 a1 = tanR * t2;
                c00 += VectorHelper.Dot(a0, a0);
                c01 += VectorHelper.Dot(a0, a1);
                c11 += VectorHelper.Dot(a1, a1);

                // X matrix
                x0 += VectorHelper.Dot(a0, v);
                x1 += VectorHelper.Dot(a1, v);
            }

            // determinents of X and C matrices
            float det_C0_C1 = c00 * c11 - c01 * c01;
            float det_C0_X = c00 * x1 - c01 * x0;
            float det_X_C1 = x0 * c11 - x1 * c01;
            float alphaL = det_X_C1 / det_C0_C1;
            float alphaR = det_C0_X / det_C0_C1;

            // if alpha is negative, zero, or very small (or we can't trust it since C matrix is small), fall back to Wu/Barsky heuristic
            float linDist = VectorHelper.Distance(p0, p3);
            float epsilon2 = CurveFitBase.EPSILON * linDist;
            if (Math.Abs(det_C0_C1) < CurveFitBase.EPSILON || alphaL < epsilon2 || alphaR < epsilon2)
            {
                float alpha = linDist / 3;
                Vector2 p1 = (tanL * alpha) + p0;
                Vector2 p2 = (tanR * alpha) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
            else
            {
                Vector2 p1 = (tanL * alphaL) + p0;
                Vector2 p2 = (tanR * alphaR) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
        }

        //[BurstCompile]
        public static unsafe void GenerateBezierStandardChanged(int first, int last, in Vector2 tanL, in Vector2 tanR, Vector2* pts, float* u, out CubicBezier retVal)
        {
            int nPts = last - first + 1;
            Vector2 p0 = pts[first], p3 = pts[last]; // first and last points of curve are actual points on data
            float c00 = 0, c01 = 0, c11 = 0, x0 = 0, x1 = 0; // matrix members -- both C[0,1] and C[1,0] are the same, stored in c01

            var jArray = new Newtonsoft.Json.Linq.JArray();
            for (int i = 1; i < nPts; i++)
            {
                // Calculate cubic bezier multipliers
                float T = u[i];
                float TI = 1.0f - T;
                float TIsq = TI * TI;
                float TIMulT = TI * T;
                float Tsq = T * T;

                float T0 = TIsq * TI;
                float T1 = 3.0f * (TIsq * T);
                float T2 = 3.0f * (TI * Tsq);
                float T3 = Tsq * T;

                // For X matrix; moving this up here since profiling shows it's better up here (maybe a0/a1 not in registers vs only v not in regs)
                Vector2 S0 = (p0 * T0);
                Vector2 S1 = (p0 * T1);
                Vector2 S2 = (p3 * T2);
                Vector2 S3 = (p3 * T3);
                Vector2 S4 = S0 + S1;
                Vector2 S5 = S2 + S3;
                Vector2 s = S4 + S5;
                //Vector2 s = (p0 * T0) + (p0 * T1) + (p3 * T2) + (p3 * T3); // NOTE: this would be Q(t) if p1=p0 and p2=p3
                Vector2 v = pts[first + i] - s;

                // C matrix
                Vector2 a0 = tanL * T1;
                Vector2 a1 = tanR * T2;
                c00 = c00 + VectorHelper.Dot(a0, a0);
                c01 = c01 + VectorHelper.Dot(a0, a1);
                c11 = c11 + VectorHelper.Dot(a1, a1);

                // X matrix
                x0 = x0 + VectorHelper.Dot(a0, v);
                x1 = x1 + VectorHelper.Dot(a1, v);

                var jObject = new Newtonsoft.Json.Linq.JObject();
                jObject.Add("i", i);
                jObject.Add("u", u[i].ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("T", T.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("TI", TI.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("TIsq", TIsq.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("TIMulT", TIMulT.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("Tsq", Tsq.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("T0", T0.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("T1", T1.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("T2", T2.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("T3", T3.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("S0", S0.ToString("F8"));
                jObject.Add("S1", S1.ToString("F8"));
                jObject.Add("S2", S2.ToString("F8"));
                jObject.Add("S3", S3.ToString("F8"));
                jObject.Add("S4", S4.ToString("F8"));
                jObject.Add("S5", S5.ToString("F8"));
                jObject.Add("s", s.ToString("F8"));
                jObject.Add("v0", pts[first + i].ToString("F8"));
                jObject.Add("v", v.ToString("F8"));
                jObject.Add("a0", a0.ToString("F8"));
                jObject.Add("a1", a1.ToString("F8"));
                jObject.Add("c00", c00.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("c01", c01.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("c11", c11.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("x0", x0.ToString("F8", CultureInfo.InvariantCulture));
                jObject.Add("x1", x1.ToString("F8", CultureInfo.InvariantCulture));
                jArray.Add(jObject);
            }

            // determinents of X and C matrices
            float det_C0_C1 = c00 * c11 - c01 * c01;
            float det_C0_X = c00 * x1 - c01 * x0;
            float det_X_C1 = x0 * c11 - x1 * c01;
            float alphaL = det_X_C1 / det_C0_C1;
            float alphaR = det_C0_X / det_C0_C1;

            // if alpha is negative, zero, or very small (or we can't trust it since C matrix is small), fall back to Wu/Barsky heuristic
            float linDist = VectorHelper.Distance(p0, p3);
            float epsilon2 = CurveFitBase.EPSILON * linDist;
            if (Math.Abs(det_C0_C1) < CurveFitBase.EPSILON || alphaL < epsilon2 || alphaR < epsilon2)
            {
                float alpha = linDist / 3;
                Vector2 p1 = (tanL * alpha) + p0;
                Vector2 p2 = (tanR * alpha) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
            else
            {
                Vector2 p1 = (tanL * alphaL) + p0;
                Vector2 p2 = (tanR * alphaR) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }

            //System.IO.File.AppendAllText("GenerateBezierStandardChanged.json", jArray.ToString());
        }

        [BurstCompile]
        public static unsafe void GenerateBezierUnityMathematics(int first, int last, in Vector2 tanL, in Vector2 tanR, Vector2* pts, float* u, out CubicBezier retVal)
        {
            // TODO: Good potential for SIMD'ing the for loop, as it mainly works on single floats.
            int nPts = last - first + 1;
            float2 p0 = pts[first], p3 = pts[last]; // first and last points of curve are actual points on data
            float c00 = 0, c01 = 0, c11 = 0, x0 = 0, x1 = 0; // matrix members -- both C[0,1] and C[1,0] are the same, stored in c01
            for (int i = 1; i < nPts; i++)
            {
                // Calculate cubic bezier multipliers
                float t = u[i];
                float ti = 1 - t;
                float t0 = ti * ti * ti;
                float t1 = 3 * ti * ti * t;
                float t2 = 3 * ti * t * t;
                float t3 = t * t * t;

                // For X matrix; moving this up here since profiling shows it's better up here (maybe a0/a1 not in registers vs only v not in regs)
                float2 s = (p0 * t0) + (p0 * t1) + (p3 * t2) + (p3 * t3); // NOTE: this would be Q(t) if p1=p0 and p2=p3
                float2 v = (float2)pts[first + i] - s;

                // C matrix
                float2 a0 = tanL * t1;
                float2 a1 = tanR * t2;
                c00 += VectorHelper.Dot(a0, a0);
                c01 += VectorHelper.Dot(a0, a1);
                c11 += VectorHelper.Dot(a1, a1);

                // X matrix
                x0 += VectorHelper.Dot(a0, v);
                x1 += VectorHelper.Dot(a1, v);
            }

            // determinents of X and C matrices
            float det_C0_C1 = c00 * c11 - c01 * c01;
            float det_C0_X = c00 * x1 - c01 * x0;
            float det_X_C1 = x0 * c11 - x1 * c01;
            float alphaL = det_X_C1 / det_C0_C1;
            float alphaR = det_C0_X / det_C0_C1;

            // if alpha is negative, zero, or very small (or we can't trust it since C matrix is small), fall back to Wu/Barsky heuristic
            float linDist = VectorHelper.Distance(p0, p3);
            float epsilon2 = CurveFitBase.EPSILON * linDist;
            if (Math.Abs(det_C0_C1) < CurveFitBase.EPSILON || alphaL < epsilon2 || alphaR < epsilon2)
            {
                float alpha = linDist / 3;
                float2 p1 = ((float2)tanL * alpha) + p0;
                float2 p2 = ((float2)tanR * alpha) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
            else
            {
                float2 p1 = ((float2)tanL * alphaL) + p0;
                float2 p2 = ((float2)tanR * alphaR) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
        }

        //[MethodImpl(MethodImplOptions.NoInlining)]
        private static v128 DotPSV0(v128 ax, v128 ay, v128 bx, v128 by)
        {
            //return fmadd_ps(ax, bx, mul_ps(ay, by));
            var t0 = mul_ps(ax, bx);
            var t1 = mul_ps(ay, by);
            return add_ps(t0, t1);
        }




        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ForLoopBarrierBegin(v128 v = default)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ForLoopBarrierRef(ref v128 v)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ForLoopBarrierEnd()
        {
        }

        [BurstCompile]
        public static unsafe void GenerateBezierV128V0(int first, int last, in Vector2 tanL, in Vector2 tanR, Vector2* pts, float* u, out CubicBezier retVal)
        {
            // TODO: Good potential for SIMD'ing the for loop, as it mainly works on single floats.
            int nPts = last - first + 1;
            float2 p0 = pts[first], p3 = pts[last]; // first and last points of curve are actual points on data
            float c00 = 0, c01 = 0, c11 = 0, x0 = 0, x1 = 0; // matrix members -- both C[0,1] and C[1,0] are the same, stored in c01

            var P0X = set1_ps(p0.x);
            var P0Y = set1_ps(p0.y);
            var P3X = set1_ps(p3.x);
            var P3Y = set1_ps(p3.y);
            var C00 = setzero_ps();
            var C01 = setzero_ps();
            var C11 = setzero_ps();
            var X0 = setzero_ps();
            var X1 = setzero_ps();

            var alignedCount = nPts & ~3;
            // We can do four values at the time, but it's going to be a bit tricky, Due to p0 and p3 begin vectors
            int i = 1;
            for (; i < alignedCount; i += 4)
            {
                // Calculate cubic bezier multipliers
                var T = loadu_ps(u + i);
                var _1f = set1_ps(1.0f);
                var TI = sub_ps(_1f, T);
                var TIsq = mul_ps(TI, TI);
                var TIMulT = mul_ps(TI, T);
                var Tsq = mul_ps(T, T);
                var _3f = set1_ps(3.0f);

                var T0 = mul_ps(TIsq, TI);
                var T1 = mul_ps(_3f, mul_ps(TIsq, T));
                var T2 = mul_ps(_3f, mul_ps(TI, Tsq));
                var T3 = mul_ps(Tsq, T);

                var A0X = mul_ps(set1_ps(tanL.x), T1);
                var A0Y = mul_ps(set1_ps(tanL.y), T1);
                var A1X = mul_ps(set1_ps(tanR.x), T2);
                var A1Y = mul_ps(set1_ps(tanR.y), T2);

                C00 = add_ps(C00, DotPSV0(A0X, A0Y, A0X, A0Y));
                C01 = add_ps(C01, DotPSV0(A0X, A0Y, A1X, A1Y));
                C11 = add_ps(C11, DotPSV0(A1X, A1Y, A1X, A1Y));

                //ForLoopBarrierBegin();
                var SX0 = mul_ps(P0X, T0);
                var SX1 = mul_ps(P0X, T1);
                var SX2 = mul_ps(P3X, T2);
                var SX3 = mul_ps(P3X, T3);
                var SX4 = add_ps(SX0, SX1);
                var SX5 = add_ps(SX2, SX3);
                var SX  = add_ps(SX4, SX5);
                //ForLoopBarrierRef(ref SX);
                //ForLoopBarrierEnd();

                var SY0 = mul_ps(P0Y, T0);
                var SY1 = mul_ps(P0Y, T1);
                var SY2 = mul_ps(P3Y, T2);
                var SY3 = mul_ps(P3Y, T3);
                var SY4 = add_ps(SY0, SY1);
                var SY5 = add_ps(SY2, SY3);
                var SY  = add_ps(SY4, SY5);

                var V0XYXY = load_ps(pts + first + i);
                var V1XYXY = load_ps(pts + first + i + 2);
                var VXXXX = shuffle_ps(V0XYXY, V1XYXY, SHUFFLE(2, 0, 2, 0));
                var VYYYY = shuffle_ps(V0XYXY, V1XYXY, SHUFFLE(3, 1, 3, 1));
                var VXXXXSubS = sub_ps(VXXXX, SX);
                var VYYYYSubS = sub_ps(VYYYY, SY);

                X0 = add_ps(X0, DotPSV0(A0X, A0Y, VXXXXSubS, VYYYYSubS));
                X1 = add_ps(X1, DotPSV0(A1X, A1Y, VXXXXSubS, VYYYYSubS));
            }

            c00 = extractf_ps(C00, 0) + extractf_ps(C00, 1) + extractf_ps(C00, 2) + extractf_ps(C00, 3);
            c01 = extractf_ps(C01, 0) + extractf_ps(C01, 1) + extractf_ps(C01, 2) + extractf_ps(C01, 3);
            c11 = extractf_ps(C11, 0) + extractf_ps(C11, 1) + extractf_ps(C11, 2) + extractf_ps(C11, 3);
            x0 = extractf_ps(X0, 0) + extractf_ps(X0, 1) + extractf_ps(X0, 2) + extractf_ps(X0, 3);
            x1 = extractf_ps(X1, 0) + extractf_ps(X1, 1) + extractf_ps(X1, 2) + extractf_ps(X1, 3);

            // Scalar loop
            for (; i < nPts; i++)
            {
                // Calculate cubic bezier multipliers
                float t = u[i];
                float ti = 1 - t;
                float t0 = ti * ti * ti;
                float t1 = 3 * ti * ti * t;
                float t2 = 3 * ti * t * t;
                float t3 = t * t * t;

                // For X matrix; moving this up here since profiling shows it's better up here (maybe a0/a1 not in registers vs only v not in regs)

                // C matrix
                float2 a0 = tanL * t1;
                float2 a1 = tanR * t2;
                c00 += VectorHelper.Dot(a0, a0);
                c01 += VectorHelper.Dot(a0, a1);
                c11 += VectorHelper.Dot(a1, a1);

                // X matrix
                float2 s = (p0 * t0) + (p0 * t1) + (p3 * t2) + (p3 * t3); // NOTE: this would be Q(t) if p1=p0 and p2=p3
                float2 v = (float2)pts[first + i] - s;
                x0 += VectorHelper.Dot(a0, v);
                x1 += VectorHelper.Dot(a1, v);
            }

            // determinents of X and C matrices
            float det_C0_C1 = c00 * c11 - c01 * c01;
            float det_C0_X = c00 * x1 - c01 * x0;
            float det_X_C1 = x0 * c11 - x1 * c01;
            float alphaL = det_X_C1 / det_C0_C1;
            float alphaR = det_C0_X / det_C0_C1;

            // if alpha is negative, zero, or very small (or we can't trust it since C matrix is small), fall back to Wu/Barsky heuristic
            float linDist = VectorHelper.Distance(p0, p3);
            float epsilon2 = CurveFitBase.EPSILON * linDist;
            if (Math.Abs(det_C0_C1) < CurveFitBase.EPSILON || alphaL < epsilon2 || alphaR < epsilon2)
            {
                float alpha = linDist / 3;
                float2 p1 = ((float2)tanL * alpha) + p0;
                float2 p2 = ((float2)tanR * alpha) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
            else
            {
                float2 p1 = ((float2)tanL * alphaL) + p0;
                float2 p2 = ((float2)tanR * alphaR) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
        }

        [BurstCompile]
        public static unsafe void GenerateBezierV128V1(int first, int last, in Vector2 tanL, in Vector2 tanR, Vector2* pts, float* u, out CubicBezier retVal)
        {
            int nPts = last - first + 1;
            float2 p0 = pts[first], p3 = pts[last]; // first and last points of curve are actual points on data
            float c00 = 0, c01 = 0, c11 = 0, x0 = 0, x1 = 0; // matrix members -- both C[0,1] and C[1,0] are the same, stored in c01

            var P0X = set1_ps(p0.x);
            var P0Y = set1_ps(p0.y);
            var P3X = set1_ps(p3.x);
            var P3Y = set1_ps(p3.y);
            var C00 = setzero_ps();
            var C01 = setzero_ps();
            var C11 = setzero_ps();
            var X0 = setzero_ps();
            var X1 = setzero_ps();
            var _1f = set1_ps(1.0f);
            var _3f = set1_ps(3.0f);
            var tanLX = set1_ps(tanL.x);
            var tanLY = set1_ps(tanL.y);
            var tanRX = set1_ps(tanR.x);
            var tanRY = set1_ps(tanR.y);

            var alignedCount = nPts & ~3;
            // We can do four values at the time, but it's going to be a bit tricky, Due to p0 and p3 begin vectors
            int i = 1;
            for (; i < alignedCount; i += 4)
            {
                var T = loadu_ps(&u[i]);
                var TI = sub_ps(_1f, T);
                var TIsq = mul_ps(TI, TI);
                var TIMulT = mul_ps(TI, T);
                var Tsq = mul_ps(T, T);

                var T0 = mul_ps(TIsq, TI);
                var T1 = mul_ps(_3f, mul_ps(TIsq, T));
                var T2 = mul_ps(_3f, mul_ps(TI, Tsq));
                var T3 = mul_ps(Tsq, T);

                var A0X = mul_ps(tanLX, T1);
                var A0Y = mul_ps(tanLY, T1);
                var A1X = mul_ps(tanRX, T2);
                var A1Y = mul_ps(tanRY, T2);

                C00 = fmadd_ps(A0X, A0X, fmadd_ps(A0Y, A0Y, C00));
                C01 = fmadd_ps(A0X, A1X, fmadd_ps(A0Y, A1Y, C01));
                C11 = fmadd_ps(A1X, A1X, fmadd_ps(A1Y, A1Y, C11));

                var SX = fmadd_ps(P0X, T0, fmadd_ps(P0X, T1, fmadd_ps(P3X, T2, mul_ps(P3X, T3))));
                var SY = fmadd_ps(P0Y, T0, fmadd_ps(P0Y, T1, fmadd_ps(P3Y, T2, mul_ps(P3Y, T3))));

                var V0XYXY = load_ps(pts + first + i);
                var V1XYXY = load_ps(pts + first + i + 2);
                var VXXXX = shuffle_ps(V0XYXY, V1XYXY, SHUFFLE(2, 0, 2, 0));
                var VYYYY = shuffle_ps(V0XYXY, V1XYXY, SHUFFLE(3, 1, 3, 1));
                var VXXXXSubS = sub_ps(VXXXX, SX);
                var VYYYYSubS = sub_ps(VYYYY, SY);

                X0 = fmadd_ps(A0X, VXXXXSubS, fmadd_ps(A0Y, VYYYYSubS, X0));
                X1 = fmadd_ps(A1X, VXXXXSubS, fmadd_ps(A1Y, VYYYYSubS, X1));
            }

            c00 = extractf_ps(C00, 0) + extractf_ps(C00, 1) + extractf_ps(C00, 2) + extractf_ps(C00, 3);
            c01 = extractf_ps(C01, 0) + extractf_ps(C01, 1) + extractf_ps(C01, 2) + extractf_ps(C01, 3);
            c11 = extractf_ps(C11, 0) + extractf_ps(C11, 1) + extractf_ps(C11, 2) + extractf_ps(C11, 3);
            x0 = extractf_ps(X0, 0) + extractf_ps(X0, 1) + extractf_ps(X0, 2) + extractf_ps(X0, 3);
            x1 = extractf_ps(X1, 0) + extractf_ps(X1, 1) + extractf_ps(X1, 2) + extractf_ps(X1, 3);

            // Scalar loop
            for (; i < nPts; i++)
            {
                // Calculate cubic bezier multipliers
                float t = u[i];
                float ti = 1 - t;
                float t0 = ti * ti * ti;
                float t1 = 3 * ti * ti * t;
                float t2 = 3 * ti * t * t;
                float t3 = t * t * t;

                // For X matrix; moving this up here since profiling shows it's better up here (maybe a0/a1 not in registers vs only v not in regs)
                float2 s = (p0 * t0) + (p0 * t1) + (p3 * t2) + (p3 * t3); // NOTE: this would be Q(t) if p1=p0 and p2=p3
                float2 v = (float2)pts[first + i] - s;

                // C matrix
                float2 a0 = tanL * t1;
                float2 a1 = tanR * t2;
                c00 += VectorHelper.Dot(a0, a0);
                c01 += VectorHelper.Dot(a0, a1);
                c11 += VectorHelper.Dot(a1, a1);

                // X matrix
                x0 += VectorHelper.Dot(a0, v);
                x1 += VectorHelper.Dot(a1, v);
            }

            // determinents of X and C matrices
            float det_C0_C1 = c00 * c11 - c01 * c01;
            float det_C0_X = c00 * x1 - c01 * x0;
            float det_X_C1 = x0 * c11 - x1 * c01;
            float alphaL = det_X_C1 / det_C0_C1;
            float alphaR = det_C0_X / det_C0_C1;

            // if alpha is negative, zero, or very small (or we can't trust it since C matrix is small), fall back to Wu/Barsky heuristic
            float linDist = VectorHelper.Distance(p0, p3);
            float epsilon2 = CurveFitBase.EPSILON * linDist;
            if (Math.Abs(det_C0_C1) < CurveFitBase.EPSILON || alphaL < epsilon2 || alphaR < epsilon2)
            {
                float alpha = linDist / 3;
                float2 p1 = ((float2)tanL * alpha) + p0;
                float2 p2 = ((float2)tanR * alpha) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
            else
            {
                float2 p1 = ((float2)tanL * alphaL) + p0;
                float2 p2 = ((float2)tanR * alphaR) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
        }

        // Broken, debug later
        [BurstCompile]
        public static unsafe void GenerateBezierV256V1(int first, int last, in Vector2 tanL, in Vector2 tanR, Vector2* pts, float* u, out CubicBezier retVal)
        {
            int nPts = last - first + 1;
            float2 p0 = pts[first], p3 = pts[last]; // first and last points of curve are actual points on data
            float c00 = 0, c01 = 0, c11 = 0, x0 = 0, x1 = 0; // matrix members -- both C[0,1] and C[1,0] are the same, stored in c01

            var P0X = mm256_set1_ps(p0.x);
            var P0Y = mm256_set1_ps(p0.y);
            var P3X = mm256_set1_ps(p3.x);
            var P3Y = mm256_set1_ps(p3.y);
            var C00 = mm256_setzero_ps();
            var C01 = mm256_setzero_ps();
            var C11 = mm256_setzero_ps();
            var X0 = mm256_setzero_ps();
            var X1 = mm256_setzero_ps();
            var _1f = mm256_set1_ps(1.0f);
            var _3f = mm256_set1_ps(3.0f);
            var tanLX = mm256_set1_ps(tanL.x);
            var tanLY = mm256_set1_ps(tanL.y);
            var tanRX = mm256_set1_ps(tanR.x);
            var tanRY = mm256_set1_ps(tanR.y);

            var alignedCount = nPts & ~7;
            // We can do four values at the time, but it's going to be a bit tricky, Due to p0 and p3 begin vectors
            int i = 1;
            for (; i < alignedCount; i += 8)
            {
                var T = mm256_loadu_ps(&u[i]);
                var TI = mm256_sub_ps(_1f, T);
                var TIsq = mm256_mul_ps(TI, TI);
                var TIMulT = mm256_mul_ps(TI, T);
                var Tsq = mm256_mul_ps(T, T);

                var T0 = mm256_mul_ps(TIsq, TI);
                var T1 = mm256_mul_ps(_3f, mm256_mul_ps(TIsq, T));
                var T2 = mm256_mul_ps(_3f, mm256_mul_ps(TI, Tsq));
                var T3 = mm256_mul_ps(Tsq, T);

                var A0X = mm256_mul_ps(tanLX, T1);
                var A0Y = mm256_mul_ps(tanLY, T1);
                var A1X = mm256_mul_ps(tanRX, T2);
                var A1Y = mm256_mul_ps(tanRY, T2);

                C00 = mm256_fmadd_ps(A0X, A0X, mm256_fmadd_ps(A0Y, A0Y, C00));
                C01 = mm256_fmadd_ps(A0X, A1X, mm256_fmadd_ps(A0Y, A1Y, C01));
                C11 = mm256_fmadd_ps(A1X, A1X, mm256_fmadd_ps(A1Y, A1Y, C11));

                var SX = mm256_fmadd_ps(P0X, T0, mm256_fmadd_ps(P0X, T1, mm256_fmadd_ps(P3X, T2, mm256_mul_ps(P3X, T3))));
                var SY = mm256_fmadd_ps(P0Y, T0, mm256_fmadd_ps(P0Y, T1, mm256_fmadd_ps(P3Y, T2, mm256_mul_ps(P3Y, T3))));

                var V0XYXY = mm256_load_ps(pts + first + i);
                var V1XYXY = mm256_load_ps(pts + first + i + 4);
                var VXXXX = mm256_shuffle_ps(V0XYXY, V1XYXY, SHUFFLE(2, 0, 2, 0));
                var VYYYY = mm256_shuffle_ps(V0XYXY, V1XYXY, SHUFFLE(3, 1, 3, 1));
                var VXXXXSubS = mm256_sub_ps(VXXXX, SX);
                var VYYYYSubS = mm256_sub_ps(VYYYY, SY);

                X0 = mm256_fmadd_ps(A0X, VXXXXSubS, mm256_fmadd_ps(A0Y, VYYYYSubS, X0));
                X1 = mm256_fmadd_ps(A1X, VXXXXSubS, mm256_fmadd_ps(A1Y, VYYYYSubS, X1));
            }

            var C00Lo = mm256_extractf128_ps(C00, 0);
            var C00Hi = mm256_extractf128_ps(C00, 1);
            c00 = extractf_ps(C00Lo, 0) + extractf_ps(C00Lo, 1) + extractf_ps(C00Lo, 2) + extractf_ps(C00Lo, 3)
                + extractf_ps(C00Hi, 0) + extractf_ps(C00Hi, 1) + extractf_ps(C00Hi, 2) + extractf_ps(C00Hi, 3);

            var C01Lo = mm256_extractf128_ps(C01, 0);
            var C01Hi = mm256_extractf128_ps(C01, 1);
            c01 = extractf_ps(C01Lo, 0) + extractf_ps(C01Lo, 1) + extractf_ps(C01Lo, 2) + extractf_ps(C01Lo, 3)
                + extractf_ps(C01Hi, 0) + extractf_ps(C01Hi, 1) + extractf_ps(C01Hi, 2) + extractf_ps(C01Hi, 3);

            var C11Lo = mm256_extractf128_ps(C11, 0);
            var C11Hi = mm256_extractf128_ps(C11, 1);
            c11 = extractf_ps(C11Lo, 0) + extractf_ps(C11Lo, 1) + extractf_ps(C11Lo, 2) + extractf_ps(C11Lo, 3)
                + extractf_ps(C11Hi, 0) + extractf_ps(C11Hi, 1) + extractf_ps(C11Hi, 2) + extractf_ps(C11Hi, 3);

            var X0Lo = mm256_extractf128_ps(X0, 0);
            var X0Hi = mm256_extractf128_ps(X0, 1);
            x0 = extractf_ps(X0Lo, 0) + extractf_ps(X0Lo, 1) + extractf_ps(X0Lo, 2) + extractf_ps(X0Lo, 3)
               + extractf_ps(X0Hi, 0) + extractf_ps(X0Hi, 1) + extractf_ps(X0Hi, 2) + extractf_ps(X0Hi, 3);

            var X1Lo = mm256_extractf128_ps(X1, 0);
            var X1Hi = mm256_extractf128_ps(X1, 1);
            x1 = extractf_ps(X1Lo, 0) + extractf_ps(X1Lo, 1) + extractf_ps(X1Lo, 2) + extractf_ps(X1Lo, 3)
               + extractf_ps(X1Hi, 0) + extractf_ps(X1Hi, 1) + extractf_ps(X1Hi, 2) + extractf_ps(X1Hi, 3);

            // Scalar loop
            for (; i < nPts; i++)
            {
                // Calculate cubic bezier multipliers
                float t = u[i];
                float ti = 1 - t;
                float t0 = ti * ti * ti;
                float t1 = 3 * ti * ti * t;
                float t2 = 3 * ti * t * t;
                float t3 = t * t * t;

                // For X matrix; moving this up here since profiling shows it's better up here (maybe a0/a1 not in registers vs only v not in regs)
                float2 s = (p0 * t0) + (p0 * t1) + (p3 * t2) + (p3 * t3); // NOTE: this would be Q(t) if p1=p0 and p2=p3
                float2 v = (float2)pts[first + i] - s;

                // C matrix
                float2 a0 = tanL * t1;
                float2 a1 = tanR * t2;
                c00 += VectorHelper.Dot(a0, a0);
                c01 += VectorHelper.Dot(a0, a1);
                c11 += VectorHelper.Dot(a1, a1);

                // X matrix
                x0 += VectorHelper.Dot(a0, v);
                x1 += VectorHelper.Dot(a1, v);
            }

            // determinents of X and C matrices
            float det_C0_C1 = c00 * c11 - c01 * c01;
            float det_C0_X = c00 * x1 - c01 * x0;
            float det_X_C1 = x0 * c11 - x1 * c01;
            float alphaL = det_X_C1 / det_C0_C1;
            float alphaR = det_C0_X / det_C0_C1;

            // if alpha is negative, zero, or very small (or we can't trust it since C matrix is small), fall back to Wu/Barsky heuristic
            float linDist = VectorHelper.Distance(p0, p3);
            float epsilon2 = CurveFitBase.EPSILON * linDist;
            if (Math.Abs(det_C0_C1) < CurveFitBase.EPSILON || alphaL < epsilon2 || alphaR < epsilon2)
            {
                float alpha = linDist / 3;
                float2 p1 = ((float2)tanL * alpha) + p0;
                float2 p2 = ((float2)tanR * alpha) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
            else
            {
                float2 p1 = ((float2)tanL * alphaL) + p0;
                float2 p2 = ((float2)tanR * alphaR) + p3;
                retVal = new CubicBezier(p0, p1, p2, p3);
            }
        }

        [BurstCompile]
        public static unsafe void Reparameterize(int first, int last, in CubicBezier curve, Vector2* pts, float* u)
        {
            int nPts = last - first;
            for (int i = 1; i < nPts; i++)
            {
                Vector2 p = pts[first + i];
                float t = u[i];
                float ti = 1 - t;

                // Control vertices for Q'
                Vector2 qp0 = (curve.p1 - curve.p0) * 3;
                Vector2 qp1 = (curve.p2 - curve.p1) * 3;
                Vector2 qp2 = (curve.p3 - curve.p2) * 3;

                // Control vertices for Q''
                Vector2 qpp0 = (qp1 - qp0) * 2;
                Vector2 qpp1 = (qp2 - qp1) * 2;

                // Evaluate Q(t), Q'(t), and Q''(t)
                Vector2 p0 = curve.Sample(t);
                Vector2 p1 = ((ti * ti) * qp0) + ((2 * ti * t) * qp1) + ((t * t) * qp2);
                Vector2 p2 = (ti * qpp0) + (t * qpp1);

                // these are the actual fitting calculations using http://en.wikipedia.org/wiki/Newton%27s_method
                // We can't just use .X and .Y because Unity uses lower-case "x" and "y".
                float num = ((VectorHelper.GetX(p0) - VectorHelper.GetX(p)) * VectorHelper.GetX(p1)) + ((VectorHelper.GetY(p0) - VectorHelper.GetY(p)) * VectorHelper.GetY(p1));
                float den = (VectorHelper.GetX(p1) * VectorHelper.GetX(p1)) + (VectorHelper.GetY(p1) * VectorHelper.GetY(p1)) + ((VectorHelper.GetX(p0) - VectorHelper.GetX(p)) * VectorHelper.GetX(p2)) + ((VectorHelper.GetY(p0) - VectorHelper.GetY(p)) * VectorHelper.GetY(p2));
                float newU = t - num / den;
                if (Math.Abs(den) > CurveFitBase.EPSILON && newU >= 0 && newU <= 1)
                    u[i] = newU;
            }
        }

        [BurstCompile]
        public static unsafe void FindMaxSquaredError(int first, int last, in CubicBezier curve, out int split, Vector2* pts, float* u, out float max)
        {
            int s = (last - first + 1) / 2;
            int nPts = last - first + 1;
            float m = 0;
            for (int i = 1; i < nPts; i++)
            {
                Vector2 v0 = pts[first + i];
                Vector2 v1 = curve.Sample(u[i]);
                float d = VectorHelper.DistanceSquared(v0, v1);
                if (d > m)
                {
                    m = d;
                    s = i;
                }
            }
            max = m;

            // split at point of maximum error
            split = s + first;
            if (split <= first)
                split = first + 1;
            if (split >= last)
                split = last - 1;
        }
    }
}
