/// <summary>
/// Which of the 3 silhouette shapes (from the Claude Design mockup's
/// "Gallery" screen — the only 3 concrete shapes it supplies) a ceramic
/// tier uses. CLAUDE.md's 9 tier names (§3.4) don't map 1:1 to exactly 3
/// shapes, so each tier is assigned the closest of these 3 by its own
/// display name (see <c>CeramicAssetGenerator</c>) — a deliberate,
/// documented simplification, not a literal reading of 9 distinct shapes
/// nothing in the design actually provides art for.
/// </summary>
public enum CeramicShapeArchetype
{
    Bowl,
    Vase,
    Plate
}
