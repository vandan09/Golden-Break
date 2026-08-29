using UnityEngine;

/// <summary>
/// One crack, as a polyline in the same local space as the ceramic's
/// silhouette (CLAUDE.md §7.6).
///
/// Was a single 4-point cubic bezier, which could not represent what the
/// Claude Design mockup actually draws: every crack there is a multi-segment
/// path with a hard bend (e.g. "M100,48 L84,72 L68,102" turning at 84,72)
/// rendered with strokeLinejoin="round". One cubic can make a smooth arc or
/// a straight line, never a kink, so the authored look was unreachable by
/// fitting bezier control points.
/// </summary>
[System.Serializable]
public struct CrackPath
{
    public Vector2[] points;
}
