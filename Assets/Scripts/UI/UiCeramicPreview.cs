using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A small ceramic silhouette + crack lines rendered as plain uGUI
/// elements (Image only — no SpriteRenderer/world-space camera), for
/// embedding inside a Canvas the way the Claude Design mockup's Home and
/// Game Over screens do (a compact "here's your bowl" preview next to
/// other UI, not the full gameplay ceramic above the grid, which stays
/// world-space via <see cref="CeramicView"/>).
///
/// Crack coordinates come from the same <see cref="CrackPath"/> data
/// CeramicView draws in world space — sampled here with the same
/// <see cref="PolylineUtility"/> and rendered as thin rotated Image
/// segments between consecutive sample points, since uGUI has no native
/// line/path renderer.
/// </summary>
public sealed class UiCeramicPreview : MonoBehaviour
{
    private RectTransform _container;
    private Image _silhouetteImage;
    private RectTransform _crackRoot;

    public void Configure(RectTransform container)
    {
        _container = container;

        var silhouetteObject = new GameObject("Silhouette");
        var silhouetteRect = silhouetteObject.AddComponent<RectTransform>();
        silhouetteObject.transform.SetParent(container, false);
        // Centred with an explicit size rather than stretched to fill: the
        // size is set per shape in SetCeramic so each keeps its own aspect.
        silhouetteRect.anchorMin = new Vector2(0.5f, 0.5f);
        silhouetteRect.anchorMax = new Vector2(0.5f, 0.5f);
        silhouetteRect.pivot = new Vector2(0.5f, 0.5f);
        silhouetteRect.anchoredPosition = Vector2.zero;
        _silhouetteImage = silhouetteObject.AddComponent<Image>();

        var crackRootObject = new GameObject("Cracks");
        _crackRoot = crackRootObject.AddComponent<RectTransform>();
        crackRootObject.transform.SetParent(container, false);
        _crackRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _crackRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _crackRoot.anchoredPosition = Vector2.zero;
        _crackRoot.sizeDelta = Vector2.zero;
    }

    public void SetCeramic(CeramicDefinition definition, int cracksRepaired, int colourVariant = 0)
    {
        if (definition == null)
        {
            _silhouetteImage.enabled = false;
            ClearCracks();
            return;
        }

        ClearCracks();
        _silhouetteImage.enabled = true;

        // Silhouette AND cracks come from one rasterized sprite that paints
        // the design SVG exactly — round caps, round joins and the gold
        // drop-shadow included. The previous approach drew each crack
        // segment as a rotated Image quad, which uGUI cannot give round ends
        // or joins, so every bend showed a notch and the gold read flat.
        _silhouetteImage.sprite = CeramicSilhouetteSprite.GetComposite(
            definition.shape, definition.cracks, cracksRepaired, colourVariant);

        Vector2 viewBox = CeramicSilhouetteSprite.GetViewBoxSize(definition.shape);
        Rect containerRect = _container.rect;

        // ONE scale for both axes, fitted to the shorter side. Scaling X and
        // Y independently to fill a square box stretched a plate (200x100) to
        // double height and squashed a vase (140x200) by ~43%%, which is why
        // every shape except the near-square bowl looked wrong.
        float scale = Mathf.Min(containerRect.width / viewBox.x, containerRect.height / viewBox.y);
        _silhouetteImage.rectTransform.sizeDelta = viewBox * scale;
    }

    private void ClearCracks()
    {
        for (int i = _crackRoot.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(_crackRoot.GetChild(i).gameObject);
        }
    }
}
