using UnityEngine;

/// <summary>
/// One crack's cubic bezier path (CLAUDE.md §7.6). Control points are in
/// the same local space as the ceramic's silhouette.
/// </summary>
[System.Serializable]
public struct CrackPath
{
    public Vector2[] controlPoints;
}
