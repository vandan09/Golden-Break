using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared UI construction helpers so every screen is built from one set of
/// primitives instead of each re-implementing its own.
///
/// Before this existed, the overlay screens had drifted apart in three
/// visible ways: dismissal was a ✕ close button on Settings and Daily
/// Challenge but a back-chevron header on Gallery; only Home, GameplayHUD
/// and StreakPopup animated at all, so Gallery/Settings/Daily Challenge
/// popped in with a hard cut; and each screen carried a private copy of
/// AddBorder. The ✕ buttons are gone in favour of the header — Android
/// convention is back-navigation, BackButtonRouter already routes the
/// hardware/gesture back for all three, and one dismissal affordance
/// beats two.
/// </summary>
public static class UiKit
{
    public const float HeaderHeight = 44f;
    public const float SidePadding = 20f;
    public const float TopPadding = 16f;
    public const int HeaderTitleFontSize = 22;
    private const float BackIconSize = 18f;

    // Matches the OutBack entrance GameplayHUD's score popup and
    // StreakPopup already use, so overlays feel like the same app.
    public const float OverlayShowSeconds = 0.22f;
    private const float OverlayStartScale = 0.94f;

    /// <summary>
    /// Back-chevron + title header, anchored to the top of a full-screen
    /// panel. The whole header row is the tap target (not just the 18px
    /// chevron), which is what makes it comfortably thumb-reachable.
    /// </summary>
    public static void BuildBackHeader(Transform parent, string title, UnityEngine.Events.UnityAction onBack)
    {
        var headerObject = new GameObject("Header");
        var headerRect = headerObject.AddComponent<RectTransform>();
        headerObject.transform.SetParent(parent, false);
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(SidePadding, -(TopPadding + HeaderHeight));
        headerRect.offsetMax = new Vector2(-SidePadding, -TopPadding);

        var layout = headerObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 12f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var button = headerObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(onBack);

        var backIconObject = new GameObject("BackIcon");
        var backIconRect = backIconObject.AddComponent<RectTransform>();
        backIconObject.transform.SetParent(headerObject.transform, false);
        backIconRect.sizeDelta = new Vector2(BackIconSize, BackIconSize);
        var backIconImage = backIconObject.AddComponent<Image>();
        backIconImage.sprite = ChevronLeftSprite.Get();
        backIconImage.color = UiPalette.TextSecondary;
        var backIconLayoutElement = backIconObject.AddComponent<LayoutElement>();
        backIconLayoutElement.preferredWidth = BackIconSize;
        backIconLayoutElement.preferredHeight = BackIconSize;

        var titleObject = new GameObject("Title");
        titleObject.AddComponent<RectTransform>();
        titleObject.transform.SetParent(headerObject.transform, false);
        var titleText = titleObject.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = HeaderTitleFontSize;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = UiPalette.TextPrimary;
        titleText.text = title;
        var titleLayoutElement = titleObject.AddComponent<LayoutElement>();
        titleLayoutElement.preferredWidth = 200f;
        titleLayoutElement.preferredHeight = HeaderHeight;
    }

    /// <summary>
    /// Standard overlay entrance: a short scale-up on the panel. Called
    /// from each screen's Show() right after the panel is activated.
    /// </summary>
    public static void PlayOverlayShow(GameObject panel)
    {
        if (panel == null)
        {
            return;
        }

        Transform t = panel.transform;
        t.DOKill();
        t.localScale = Vector3.one * OverlayStartScale;
        t.DOScale(1f, OverlayShowSeconds).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// The two button weights the whole game uses. Primary is Home's Play
    /// button — surface fill, 2px gold border, bold uppercase gold label.
    /// Secondary is the same pill at a lighter weight for non-committal
    /// actions (Play Again, Remove Ads), so a screen never has two equally
    /// loud buttons competing.
    /// </summary>
    public enum ButtonStyle
    {
        Primary,
        Secondary,
    }

    public const float PrimaryButtonHeight = 60f;
    public const float SecondaryButtonHeight = 48f;
    private const int PrimaryLabelFontSize = 18;
    // Floor for best-fit shrinking: below this a label is technically
    // inside the button but no longer comfortably readable on a phone.
    private const int MinLabelFontSize = 11;
    private const int SecondaryLabelFontSize = 16;

    /// <summary>
    /// Builds the one button shape used everywhere. The caller still owns
    /// positioning — it sets anchors/offsets on the returned Button's
    /// RectTransform — because screens lay buttons out very differently
    /// (bottom-anchored stacks on Home/Game Over, centred on Daily
    /// Challenge, row-indexed in Settings), but nothing else about the
    /// button is the caller's to decide any more.
    ///
    /// Radius is always height/2, so every button is a true pill at any
    /// height. Before this, five screens each hardcoded their own radius
    /// (16, 20, 24, 30, 64) against their own height.
    /// </summary>
    public static Button BuildButton(
        Transform parent,
        string name,
        string label,
        UnityEngine.Events.UnityAction onClick,
        ButtonStyle style = ButtonStyle.Primary,
        float height = PrimaryButtonHeight,
        Sprite leadingIcon = null,
        Vector2 leadingIconSize = default)
    {
        bool primary = style == ButtonStyle.Primary;
        int radius = Mathf.RoundToInt(height * 0.5f);
        Color accent = primary ? UiPalette.GoldFill : UiPalette.TextPrimary;

        var buttonObject = new GameObject(name);
        var buttonRect = buttonObject.AddComponent<RectTransform>();
        buttonObject.transform.SetParent(parent, false);

        var background = buttonObject.AddComponent<Image>();
        background.sprite = RoundedRectSprite.Get(radius);
        background.type = Image.Type.Sliced;
        background.color = UiPalette.Surface;

        // 1px on both weights. The 2px gold stroke read as heavy next to a
        // soft glow -- a thin line with light behind it looks lit, a thick
        // one looks outlined.
        AddBorder(buttonRect, radius, primary ? UiPalette.GoldFill : UiPalette.CardBorder, 1);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = background;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        // Label and optional icon live in a centred row rather than being
        // stretched over the button, so an icon+label pair stays centred
        // as a unit instead of the icon floating independently.
        var contentObject = new GameObject("Content");
        var contentRect = contentObject.AddComponent<RectTransform>();
        contentObject.transform.SetParent(buttonObject.transform, false);
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var layout = contentObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        // Keeps the label off the rounded ends rather than running to the
        // very edge of the pill.
        layout.padding = new RectOffset(14, 14, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        if (leadingIcon != null)
        {
            Vector2 size = leadingIconSize == default ? new Vector2(14f, 18f) : leadingIconSize;
            var iconObject = new GameObject("Icon");
            var iconRect = iconObject.AddComponent<RectTransform>();
            iconObject.transform.SetParent(contentObject.transform, false);
            iconRect.sizeDelta = size;

            var iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = leadingIcon;
            iconImage.color = accent;

            var iconLayoutElement = iconObject.AddComponent<LayoutElement>();
            iconLayoutElement.preferredWidth = size.x;
            iconLayoutElement.preferredHeight = size.y;
        }

        var labelObject = new GameObject("Label");
        labelObject.AddComponent<RectTransform>();
        labelObject.transform.SetParent(contentObject.transform, false);
        var labelText = labelObject.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        int labelSize = primary ? PrimaryLabelFontSize : SecondaryLabelFontSize;
        labelText.fontSize = labelSize;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = accent;

        // Labels shrink to fit rather than spilling past the button. This
        // was Overflow, which meant any label wider than its button simply
        // ran outside it — invisible with the short labels this shipped
        // with, immediately visible the moment one got longer, and worse on
        // narrow phones where the same label has fewer pixels to live in.
        // Best-fit keeps every button correct at any width and any string
        // length, including translations, without hand-tuning each one.
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Truncate;
        labelText.resizeTextForBestFit = true;
        labelText.resizeTextMaxSize = labelSize;
        labelText.resizeTextMinSize = MinLabelFontSize;
        labelText.text = label != null ? label.ToUpperInvariant() : string.Empty;

        // Takes the space the icon does not, so the layout group gives the
        // label a real width to fit into instead of letting it size itself
        // to whatever the text happens to measure.
        var labelLayoutElement = labelObject.AddComponent<LayoutElement>();
        labelLayoutElement.flexibleWidth = 1f;
        labelLayoutElement.minWidth = 0f;

        return button;
    }

    /// <summary>
    /// Bottom-anchored full-width placement, the layout Home and Game Over
    /// stack their buttons with.
    /// </summary>
    public static void AnchorBottomStretch(RectTransform rect, float bottomY, float height, float sidePadding = SidePadding)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(sidePadding, bottomY);
        rect.offsetMax = new Vector2(-sidePadding, bottomY + height);
    }

    /// <summary>Centred placement at a fractional height, for single-button screens.</summary>
    public static void AnchorCentred(RectTransform rect, Vector2 anchor, float width, float height)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
    }

    /// <summary>
    /// The centred icon+label row inside a button built by
    /// <see cref="BuildButton"/>, so a caller can append extra content
    /// (Home's streak badge) without rebuilding the button.
    /// </summary>
    public static Transform GetContentRow(Button button) => button.transform.Find("Content");

    // Mockup gold-glow values, kept here so every gold element is lit from
    // one place: Play button box-shadow 0 0 24px rgba(232,192,96,0.22),
    // "Golden" title text-shadow 0 0 20px rgba(232,192,96,0.35).
    // Wider and much fainter than the mockup's literal 24px/0.22: a broad,
    // low-alpha wash dissolves into the background, whereas a tight bright
    // one reads as a ring drawn around the button.
    public const int ButtonGlowBlur = 30;
    public const float ButtonGlowAlpha = 0.10f;
    public const int TextGlowBlur = 26;
    public const float TextGlowAlpha = 0.15f;

    /// <summary>
    /// Adds a soft gold wash behind an already-positioned element.
    ///
    /// The glow has to be a SIBLING placed before the target, not a child:
    /// uGUI draws a parent Graphic first and then its children on top, so a
    /// child glow would cover the very button it is meant to sit behind.
    /// Call this AFTER the target has its anchors set -- the glow copies them.
    ///
    /// IMPORTANT: because it is a sibling, SetActive on the target does NOT
    /// hide the glow. Any caller that shows/hides its target must toggle the
    /// returned Image too. Missing this left StreakPopup's halo burning a
    /// permanent gold rectangle over the gameplay screen while the popup
    /// itself was hidden.
    /// </summary>
    public static Image AddGlowBehind(RectTransform target, Color colour, int blur, float alpha, int cornerRadius)
    {
        var glowObject = new GameObject("Glow");
        var glowRect = glowObject.AddComponent<RectTransform>();
        glowObject.transform.SetParent(target.parent, false);
        glowObject.transform.SetSiblingIndex(target.GetSiblingIndex());

        glowRect.anchorMin = target.anchorMin;
        glowRect.anchorMax = target.anchorMax;
        glowRect.pivot = target.pivot;

        // sizeDelta alone expands by blur on every side for BOTH stretch and
        // point anchors. Also writing offsetMin/offsetMax overrides that and
        // mis-places anything point-anchored, such as a single centred word.
        glowRect.sizeDelta = target.sizeDelta + (Vector2.one * (blur * 2f));

        // A rect grows AWAY from its pivot, so copying the pivot and enlarging
        // put the whole expansion on one side: the Play button pivots at the
        // bottom (0.5,0) and threw all its glow upward, while a Settings row
        // pivots at the top (0.5,1) and threw it all downward.
        //
        // Shifting by blur*(2*pivot-1) re-centres the growth: it cancels to 0
        // for a centred pivot, -blur for a bottom/left pivot, +blur for a
        // top/right one, so every edge ends up with exactly `blur` of glow.
        Vector2 pivot = target.pivot;
        var recentre = new Vector2(
            blur * ((2f * pivot.x) - 1f),
            blur * ((2f * pivot.y) - 1f));

        glowRect.anchoredPosition = target.anchoredPosition + recentre;

        var image = glowObject.AddComponent<Image>();
        image.sprite = SoftGlowSprite.Get(cornerRadius, blur);
        image.type = Image.Type.Sliced;
        image.color = new Color(colour.r, colour.g, colour.b, alpha);
        image.raycastTarget = false;

        return image;
    }

    /// <summary>
    /// Standard gold wash for a Primary button. Radius tracks the pill so the
    /// glow hugs the border rather than boxing it. Call after positioning.
    /// </summary>
    // Height is passed rather than read from rect.rect, which is not
    // resolved until uGUI rebuilds layout -- reading it here would give 0
    // and collapse the glow radius.
    public static void AddPrimaryGlow(Button button, float height)
    {
        AddGlowBehind(
            (RectTransform)button.transform,
            UiPalette.GoldFill,
            ButtonGlowBlur,
            ButtonGlowAlpha,
            Mathf.RoundToInt(height * 0.5f));
    }

    /// <summary>The label Text inside a button built by <see cref="BuildButton"/>.</summary>
    public static Text GetLabel(Button button) => button.transform.Find("Content/Label").GetComponent<Text>();

    /// <summary>1px card border, previously duplicated per screen.</summary>
    public static void AddBorder(RectTransform parent, int cornerRadiusPixels, Color colour, int strokeWidth = 1)
    {
        var borderObject = new GameObject("Border");
        var rect = borderObject.AddComponent<RectTransform>();
        borderObject.transform.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = borderObject.AddComponent<Image>();
        image.sprite = RoundedRectBorderSprite.Get(cornerRadiusPixels, strokeWidth);
        image.type = Image.Type.Sliced;
        image.color = colour;
        image.raycastTarget = false;

        // A border is a full-rect overlay, never a layout row. Without this,
        // a parent VerticalLayoutGroup/HorizontalLayoutGroup treats it as a
        // child and gives it a row of its own — Image implements
        // ILayoutElement, so it reports the border sprite's native size —
        // squeezing the real content. On the gallery card that pushed the
        // date and score rows to zero height, making them invisible.
        borderObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }
}
