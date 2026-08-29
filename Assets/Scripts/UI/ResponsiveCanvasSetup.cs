using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared canvas setup for every Claude Design UI rework screen (BUILD_PLAN
/// "Claude Design UI rework"): the mockup's own 390×844 reference
/// resolution via <see cref="CanvasScaler.ScaleMode.ScaleWithScreenSize"/>
/// (so pixel values copied directly from the mockup scale correctly on any
/// real device instead of only being correct at exactly 390×844 — see
/// HomeScreen's own doc comment, the first screen this was worked out for),
/// plus a "SafeArea" wrapper every screen's real content should parent
/// under instead of the raw full-screen panel — production-quality
/// requirement from the player: "no text should hide under phone screen,"
/// i.e. respect notches/punch-hole cameras/rounded corners via
/// <see cref="Screen.safeArea"/>, not just the nominal screen rect.
/// </summary>
public static class ResponsiveCanvasSetup
{
    public const float ReferenceWidth = 390f;
    public const float ReferenceHeight = 844f;

    public static Canvas BuildCanvas(Transform parent, string name, int sortingOrder)
    {
        var canvasObject = new GameObject(name);
        canvasObject.transform.SetParent(parent, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // Match WIDTH, not height. The mockup is a 390-wide phone frame and
        // every pixel value in this UI is copied from it, so the canvas must
        // be exactly 390 units wide on every device for those numbers to be
        // literally correct. Matching height instead made the canvas
        // 844 x screenAspect units wide — 379.8 on a 1080x2400 phone — so
        // anything sized against the mockup's 390 overflowed the right edge
        // by ~2.6% (visible as the gallery's second column being cut off).
        // Matching width instead varies the vertical unit count, which the
        // screens already absorb via flex spacers and anchored content.
        scaler.matchWidthOrHeight = 0f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // A RectTransform stretched to exactly Screen.safeArea (normalized to
    // 0-1 against Screen.width/height), updated once at build time — every
    // screen's real content should parent under this, not directly under
    // the full-bleed background panel, so nothing sits under a notch,
    // punch-hole camera, or rounded corner.
    public static RectTransform BuildSafeArea(Transform parent)
    {
        var safeAreaObject = new GameObject("SafeArea");
        var rect = safeAreaObject.AddComponent<RectTransform>();
        safeAreaObject.transform.SetParent(parent, false);

        ApplySafeArea(rect);
        return rect;
    }

    private static void ApplySafeArea(RectTransform rect)
    {
        Rect safeArea = Screen.safeArea;
        float width = Mathf.Max(Screen.width, 1);
        float height = Mathf.Max(Screen.height, 1);

        Vector2 anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
        Vector2 anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
