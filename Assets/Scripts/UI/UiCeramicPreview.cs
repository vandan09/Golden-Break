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
/// <see cref="BezierUtility"/> and rendered as thin rotated Image
/// segments between consecutive sample points, since uGUI has no native
/// line/path renderer.
/// </summary>
public sealed class UiCeramicPreview : MonoBehaviour
{
    private const int SamplesPerCrack = 10;
    private const float LineThickness = 2.2f;

    private static readonly Color RepairedColour = FromHex("#e8c060");
    private static readonly Color UnrepairedColour = FromHex("#4a4768");

    private RectTransform _container;
    private Image _silhouetteImage;
    private RectTransform _crackRoot;

    public void Configure(RectTransform container)
    {
        _container = container;

        var silhouetteObject = new GameObject("Silhouette");
        var silhouetteRect = silhouetteObject.AddComponent<RectTransform>();
        silhouetteObject.transform.SetParent(container, false);
        silhouetteRect.anchorMin = Vector2.zero;
        silhouetteRect.anchorMax = Vector2.one;
        silhouetteRect.offsetMin = Vector2.zero;
        silhouetteRect.offsetMax = Vector2.zero;
        _silhouetteImage = silhouetteObject.AddComponent<Image>();

        var crackRootObject = new GameObject("Cracks");
        _crackRoot = crackRootObject.AddComponent<RectTransform>();
        crackRootObject.transform.SetParent(container, false);
        _crackRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _crackRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _crackRoot.anchoredPosition = Vector2.zero;
        _crackRoot.sizeDelta = Vector2.zero;
    }

    public void SetCeramic(CeramicDefinition definition, int cracksRepaired)
    {
        if (definition == null)
        {
            _silhouetteImage.enabled = false;
            ClearCracks();
            return;
        }

        _silhouetteImage.enabled = true;
        _silhouetteImage.sprite = CeramicSilhouetteSprite.Get(definition.shape);

        ClearCracks();

        Vector2 viewBox = CeramicSilhouetteSprite.GetViewBoxSize(definition.shape);
        Rect containerRect = _container.rect;
        float scaleX = containerRect.width / viewBox.x;
        float scaleY = containerRect.height / viewBox.y;

        if (definition.cracks == null)
        {
            return;
        }

        for (int i = 0; i < definition.cracks.Length; i++)
        {
            bool repaired = i < cracksRepaired;
            DrawCrack(definition.cracks[i], repaired, scaleX, scaleY);
        }
    }

    private void DrawCrack(CrackPath path, bool repaired, float scaleX, float scaleY)
    {
        if (path.controlPoints == null || path.controlPoints.Length != 4)
        {
            return;
        }

        Color colour = repaired ? RepairedColour : UnrepairedColour;
        Vector2 previous = ToUiSpace(BezierUtility.Evaluate(path.controlPoints, 0f), scaleX, scaleY);

        for (int s = 1; s <= SamplesPerCrack; s++)
        {
            float t = s / (float)SamplesPerCrack;
            Vector2 current = ToUiSpace(BezierUtility.Evaluate(path.controlPoints, t), scaleX, scaleY);
            CreateSegment(previous, current, colour);
            previous = current;
        }
    }

    private static Vector2 ToUiSpace(Vector2 localPoint, float scaleX, float scaleY)
    {
        return new Vector2(localPoint.x * scaleX, localPoint.y * scaleY);
    }

    private void CreateSegment(Vector2 from, Vector2 to, Color colour)
    {
        var segmentObject = new GameObject("CrackSegment");
        var rect = segmentObject.AddComponent<RectTransform>();
        segmentObject.transform.SetParent(_crackRoot, false);

        var image = segmentObject.AddComponent<Image>();
        image.color = colour;

        Vector2 delta = to - from;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = (from + to) * 0.5f;
        rect.sizeDelta = new Vector2(length, LineThickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ClearCracks()
    {
        for (int i = _crackRoot.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(_crackRoot.GetChild(i).gameObject);
        }
    }

    private static Color FromHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color colour);
        return colour;
    }
}
