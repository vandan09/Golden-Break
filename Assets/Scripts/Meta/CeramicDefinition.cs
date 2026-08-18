using UnityEngine;

/// <summary>
/// One ceramic tier's art data (CLAUDE.md §7.6, §3.4). Cracks are cubic
/// bezier paths; gold-fill animation draws a growing LineRenderer segment
/// along the same path.
/// </summary>
[CreateAssetMenu(fileName = "Ceramic", menuName = "Golden Break/CeramicDefinition")]
public class CeramicDefinition : ScriptableObject
{
    public int tier;
    public string displayName;
    public Sprite silhouette;
    public CrackPath[] cracks;
    public int totalCracks;
}
