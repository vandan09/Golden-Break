/// <summary>
/// The nine ceramic silhouettes, one per tier of CLAUDE.md §3.4's table.
/// All nine come from the Claude Design project's "Ceramic Library - 9
/// tiers" artboard as real authored artwork — see
/// <see cref="CeramicShapeData"/>, which holds that file's SVG verbatim.
///
/// Values are ordered by tier, and <c>Plate</c> keeps the numeric value 2
/// it had when this enum held only Bowl/Vase/Plate, so the tier assets
/// serialized against the old three-shape enum still deserialize to the
/// right silhouette rather than silently shifting by one.
/// </summary>
public enum CeramicShapeArchetype
{
    SimpleBowl = 0,
    TallVase = 1,
    Plate = 2,
    TeaCup = 3,
    Teapot = 4,
    LargeBowl = 5,
    OrnatePlate = 6,
    SakeSet = 7,
    TempleBowl = 8
}
