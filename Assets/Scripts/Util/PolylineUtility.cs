using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arc-length walking for <see cref="CrackPath"/> polylines — the gold-flow
/// animation fills a crack progressively, so it needs "the path up to
/// fraction t", not just its endpoints.
/// </summary>
public static class PolylineUtility
{
    public static float TotalLength(Vector2[] points)
    {
        if (points == null || points.Length < 2)
        {
            return 0f;
        }

        float total = 0f;
        for (int i = 1; i < points.Length; i++)
        {
            total += Vector2.Distance(points[i - 1], points[i]);
        }

        return total;
    }

    /// <summary>Point at fraction <paramref name="t"/> along the polyline by arc length.</summary>
    public static Vector2 Evaluate(Vector2[] points, float t)
    {
        if (points == null || points.Length == 0)
        {
            return Vector2.zero;
        }

        if (points.Length == 1)
        {
            return points[0];
        }

        float target = TotalLength(points) * Mathf.Clamp01(t);
        float travelled = 0f;

        for (int i = 1; i < points.Length; i++)
        {
            float segment = Vector2.Distance(points[i - 1], points[i]);
            if (travelled + segment >= target)
            {
                float f = segment > 0f ? (target - travelled) / segment : 0f;
                return Vector2.Lerp(points[i - 1], points[i], f);
            }

            travelled += segment;
        }

        return points[points.Length - 1];
    }

    /// <summary>
    /// Fills <paramref name="into"/> with the polyline truncated at fraction
    /// <paramref name="t"/>. Emits the real vertices rather than evenly spaced
    /// samples, so a crack's bend stays exactly where it was authored instead
    /// of being rounded off between samples.
    /// </summary>
    public static void BuildPartial(Vector2[] points, float t, List<Vector2> into)
    {
        into.Clear();

        if (points == null || points.Length == 0)
        {
            return;
        }

        if (points.Length == 1)
        {
            into.Add(points[0]);
            return;
        }

        float target = TotalLength(points) * Mathf.Clamp01(t);
        float travelled = 0f;

        into.Add(points[0]);

        for (int i = 1; i < points.Length; i++)
        {
            float segment = Vector2.Distance(points[i - 1], points[i]);
            if (travelled + segment >= target)
            {
                float f = segment > 0f ? (target - travelled) / segment : 0f;
                into.Add(Vector2.Lerp(points[i - 1], points[i], f));
                return;
            }

            travelled += segment;
            into.Add(points[i]);
        }
    }
}
