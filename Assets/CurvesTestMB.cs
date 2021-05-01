using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BMCurves = burningmime.curves;
using OPCurves = CurvesOptimized;

public class CurvesTestMB : MonoBehaviour
{
    public LineRenderer BurningMimeCurves;
    public LineRenderer OptimizedCurves;

    public const float DistanceBetweenPoints = 0.01f;
    public const float CurveSplitError = 0.01f;

    private int PointCount = 100;
    public List<Vector2> Points;
    public uint Seed;

    // Start is called before the first frame update
    void Start()
    {
        Points = new List<Vector2>(PointCount);
        //Seed = (uint)Mathf.Max(1, System.DateTime.Now.Millisecond);
        var random = new Unity.Mathematics.Random(Seed);
        for (int i = 0; i < PointCount; i++)
            Points.Add(random.NextFloat2());
        //for (int i = 0; i < PointCount; i++)
        //{
        //    Points.Add(new Vector2(i / 100.0f, Mathf.Sin(i / 10.0f)));
        //}

        {
            var s = new BMCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
            for (int i = 0; i < Points.Count; i++)
                s.AddPoint(Points[i]);

            var curves = s.Curves;
            BurningMimeCurves.positionCount = curves.Count * 4;
            for (int i = 0; i < curves.Count; i++)
            {
                BurningMimeCurves.SetPosition(i * 4 + 0, curves[i].p0 * 25.0f);
                BurningMimeCurves.SetPosition(i * 4 + 1, curves[i].p1 * 25.0f);
                BurningMimeCurves.SetPosition(i * 4 + 2, curves[i].p2 * 25.0f);
                BurningMimeCurves.SetPosition(i * 4 + 3, curves[i].p3 * 25.0f);
            }
        }

        {
            var s = new OPCurves.CurveBuilder(DistanceBetweenPoints, CurveSplitError);
            for (int i = 0; i < Points.Count; i++)
                s.AddPoint(Points[i]);

            var curves = s.Curves;
            OptimizedCurves.positionCount = curves.Count * 4;
            for (int i = 0; i < curves.Count; i++)
            {
                OptimizedCurves.SetPosition(i * 4 + 0, curves[i].p0 * 25.0f);
                OptimizedCurves.SetPosition(i * 4 + 1, curves[i].p1 * 25.0f);
                OptimizedCurves.SetPosition(i * 4 + 2, curves[i].p2 * 25.0f);
                OptimizedCurves.SetPosition(i * 4 + 3, curves[i].p3 * 25.0f);
            }
        }
    }
}
