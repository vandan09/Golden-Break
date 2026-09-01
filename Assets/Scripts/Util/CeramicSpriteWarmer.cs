using System.Collections;
using UnityEngine;

/// <summary>
/// Rasterizes the ceramic sprites the gallery will need, one per frame,
/// while the player is somewhere else.
///
/// Every composite is built on first request and cached forever after, so
/// the very first gallery open paid for all of them at once and visibly
/// stalled — the player's own report. Each one fills a ~500,000 pixel
/// texture and the polygon fill tests every pixel against every vertex, so
/// this is genuinely expensive work rather than something to micro-tune
/// away.
///
/// Spreading it one per frame from startup means the cost lands while Home
/// is on screen and nothing is animating, instead of at the moment the
/// player asks to see something. Nothing here changes what is drawn; it
/// only decides when the drawing happens.
/// </summary>
public sealed class CeramicSpriteWarmer : MonoBehaviour
{
    private CeramicDefinition[] _pool;

    public static void Warm(CeramicDefinition[] pool)
    {
        if (pool == null || pool.Length == 0)
        {
            return;
        }

        var warmerObject = new GameObject("CeramicSpriteWarmer");
        var warmer = warmerObject.AddComponent<CeramicSpriteWarmer>();
        warmer._pool = pool;
    }

    private IEnumerator Start()
    {
        // A frame's grace so the first gameplay frame is never the one that
        // also rasterizes a ceramic.
        yield return null;

        foreach (CeramicDefinition definition in _pool)
        {
            if (definition == null)
            {
                continue;
            }

            // Fully repaired is what the gallery shows for a completed
            // ceramic, and it is the most expensive variant (every crack
            // stroked plus its glow), so warming it covers the cheaper
            // in-progress states of the same shape too.
            CeramicSilhouetteSprite.GetComposite(definition.shape, definition.cracks, definition.totalCracks);
            yield return null;
        }

        Destroy(gameObject);
    }
}
