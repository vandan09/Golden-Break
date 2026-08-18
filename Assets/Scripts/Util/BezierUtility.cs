using UnityEngine;

/// <summary>
/// Cubic bezier evaluation for crack-path rendering (CLAUDE.md §7.6).
/// Pure math, no Unity scene dependency beyond the Vector2 type.
/// </summary>
public static class BezierUtility
{
    public static Vector2 Evaluate(Vector2[] controlPoints, float t)
    {
        if (controlPoints == null || controlPoints.Length != 4)
        {
            throw new System.ArgumentException("BezierUtility.Evaluate requires exactly 4 cubic bezier control points.", nameof(controlPoints));
        }

        float u = 1f - t;
        float uu = u * u;
        float uuu = uu * u;
        float tt = t * t;
        float ttt = tt * t;

        return (uuu * controlPoints[0])
            + (3f * uu * t * controlPoints[1])
            + (3f * u * tt * controlPoints[2])
            + (ttt * controlPoints[3]);
    }
}
