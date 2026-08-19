using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scrollable gallery of completed ceramics (CLAUDE.md §3.5): thumbnail,
/// completion date, and cumulative score per entry. Opens as a full-screen
/// overlay above the HUD. Its own floating "Gallery" button (Phase 3) has
/// been removed now that HomeScreen exists to host that entry point, per
/// this class's own prior documentation that the button's placement was
/// temporary — Show()/Hide() remain the public API, called from
/// HomeScreen now instead.
///
/// Rebuilds its card list every time it's opened rather than reacting to
/// a live event — GalleryManager has no OnEntryAdded event because
/// nothing needs one yet: the screen is closed during play, so "always
/// current when shown" is sufficient and simpler than keeping a hidden
/// view in sync in real time.
///
/// Layout is Claude Design UI rework Screen 5 (BUILD_PLAN.md): 2-column
/// cards showing each ceramic's *real* silhouette (<see cref="UiCeramicPreview"/>,
/// fully gold since a gallery entry is always a completed ceramic) instead
/// of the flat colour-square thumbnail this screen used before.
/// </summary>
public sealed class GalleryScreen : MonoBehaviour
{
    private const float SidePadding = 20f;
    private const float TopPadding = 22f;
    private const float BottomPadding = 20f;
    private const float HeaderHeight = 28f;
    private const float GridTopGap = 16f;
    private const float ColumnGap = 14f;
    private const float RowGap = 14f;
    private const float ThumbnailHeight = 100f;
    private const float CardPadding = 12f;

    private const int TitleFontSize = 20;
    private const int CardTierFontSize = 14;
    private const int CardDetailFontSize = 12;

    private GalleryManager _galleryManager;
    private CeramicDefinition[] _ceramicPool;
    private InputHandler _inputHandler;
    private HomeScreen _homeScreen;
    private GameObject _panel;
    private RectTransform _gridContentRect;
    private Text _emptyStateText;

    // Set post-construction (GameplayController builds HomeScreen last).
    // Gallery is only ever reachable from Home (its own button was
    // removed in Phase 4), so closing it must return to Home — not
    // directly resume gameplay, which was the real bug caught on-device:
    // Home's own panel never hid itself when opening Gallery, so Gallery
    // opened invisibly behind it and never received a single tap.
    public void SetHomeScreen(HomeScreen homeScreen)
    {
        _homeScreen = homeScreen;
    }

    // inputHandler is optional so any existing caller that doesn't pass
    // one keeps compiling — but GameplayController always supplies it now,
    // closing the "overlay doesn't block the world-space drag input
    // underneath" gap (see InputHandler.InputEnabled's own doc comment).
    public void Configure(GalleryManager galleryManager, CeramicDefinition[] ceramicPool, InputHandler inputHandler = null)
    {
        _galleryManager = galleryManager;
        _ceramicPool = ceramicPool;
        _inputHandler = inputHandler;

        BuildUi();
        _panel.SetActive(false);
    }

    private void BuildUi()
    {
        Canvas canvas = ResponsiveCanvasSetup.BuildCanvas(transform, "GalleryCanvas", sortingOrder: 15); // above GameOverScreen (10) — gallery can be opened over a fresh game too

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvas.transform, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = UiPalette.Background;
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        RectTransform safeArea = ResponsiveCanvasSetup.BuildSafeArea(_panel.transform);

        BuildHeader(safeArea);
        BuildEmptyStateText(safeArea);
        BuildScrollView(safeArea);
    }

    private void BuildHeader(Transform parent)
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
        button.onClick.AddListener(Hide);

        var backIconObject = new GameObject("BackIcon");
        var backIconRect = backIconObject.AddComponent<RectTransform>();
        backIconObject.transform.SetParent(headerObject.transform, false);
        backIconRect.sizeDelta = new Vector2(18f, 18f);
        var backIconImage = backIconObject.AddComponent<Image>();
        backIconImage.sprite = ChevronLeftSprite.Get();
        backIconImage.color = UiPalette.TextSecondary;
        var backIconLayoutElement = backIconObject.AddComponent<LayoutElement>();
        backIconLayoutElement.preferredWidth = 18f;
        backIconLayoutElement.preferredHeight = 18f;

        var titleObject = new GameObject("Title");
        var titleRect = titleObject.AddComponent<RectTransform>();
        titleObject.transform.SetParent(headerObject.transform, false);
        var titleText = titleObject.AddComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = TitleFontSize;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleLeft;
        titleText.color = UiPalette.TextPrimary;
        titleText.text = Strings.GalleryTitle;
        var titleLayoutElement = titleObject.AddComponent<LayoutElement>();
        titleLayoutElement.preferredWidth = 200f;
        titleLayoutElement.preferredHeight = HeaderHeight;
    }

    private void BuildEmptyStateText(Transform parent)
    {
        var textObject = new GameObject("EmptyStateText");
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = UiPalette.TextSecondary;
        text.text = Strings.GalleryEmptyState;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.42f);
        rect.anchorMax = new Vector2(0.9f, 0.52f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _emptyStateText = text;
    }

    private void BuildScrollView(Transform parent)
    {
        var scrollObject = new GameObject("ScrollView");
        scrollObject.transform.SetParent(parent, false);
        var scrollRect = scrollObject.AddComponent<ScrollRect>();
        var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(SidePadding, BottomPadding);
        scrollRectTransform.offsetMax = new Vector2(-SidePadding, -(TopPadding + HeaderHeight + GridTopGap));

        var viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0f);
        viewportObject.AddComponent<RectMask2D>();
        var viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        var contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        _gridContentRect = contentObject.AddComponent<RectTransform>();
        _gridContentRect.anchorMin = new Vector2(0f, 1f);
        _gridContentRect.anchorMax = new Vector2(1f, 1f);
        _gridContentRect.pivot = new Vector2(0.5f, 1f);
        _gridContentRect.anchoredPosition = Vector2.zero;
        // For a horizontally-stretched anchor (anchorMin.x=0, anchorMax.x=1),
        // sizeDelta.x is an ADDITIVE offset on top of the anchor-derived
        // width, not an absolute size — an unset (non-zero default) value
        // here was silently widening the content past the viewport,
        // producing the "left column flush against the screen edge, huge
        // gap on the right" bug a rendered screenshot caught.
        _gridContentRect.sizeDelta = Vector2.zero;

        var gridLayout = contentObject.AddComponent<GridLayoutGroup>();
        gridLayout.spacing = new Vector2(ColumnGap, RowGap);
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2;
        gridLayout.cellSize = new Vector2(160f, 170f); // recomputed against real width in RefreshCards

        var sizeFitter = contentObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = _gridContentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
    }

    private void RefreshCards()
    {
        for (int i = _gridContentRect.childCount - 1; i >= 0; i--)
        {
            Destroy(_gridContentRect.GetChild(i).gameObject);
        }

        // Two columns spanning the content width exactly, matching the
        // mockup's edge-to-edge 2-column grid. Computed against the
        // reference resolution directly, the same way every other Claude
        // Design screen sizes its fixed elements — not measured from
        // _gridContentRect.rect.width at runtime, which is unreliable
        // here: RefreshCards() runs before the panel (and therefore this
        // whole layout hierarchy) is even active, so uGUI hasn't resolved
        // a real size for it yet (confirmed via a rendered screenshot
        // showing one wildly oversized card before this fix).
        float contentWidth = ResponsiveCanvasSetup.ReferenceWidth - (SidePadding * 2f);
        float cellWidth = (contentWidth - ColumnGap) / 2f;
        var gridLayout = _gridContentRect.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(cellWidth, cellWidth * 1.35f);

        IReadOnlyList<GalleryEntryData> entries = _galleryManager.Entries;
        _emptyStateText.gameObject.SetActive(entries.Count == 0);

        // Newest completion first, matching the "most recent achievement
        // first" convention used everywhere else in the game (best score,
        // streak, etc.)
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            BuildCard(entries[i]);
        }
    }

    private void BuildCard(GalleryEntryData entry)
    {
        var cardObject = new GameObject($"Card_Tier{entry.Tier}");
        cardObject.transform.SetParent(_gridContentRect, false);
        cardObject.AddComponent<RectTransform>();

        var cardImage = cardObject.AddComponent<Image>();
        cardImage.sprite = RoundedRectSprite.Get(18);
        cardImage.type = Image.Type.Sliced;
        cardImage.color = UiPalette.Surface;
        var outline = cardObject.AddComponent<Outline>();
        outline.effectColor = UiPalette.CardBorder;
        outline.effectDistance = new Vector2(1f, -1f);

        var layout = cardObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset((int)CardPadding, (int)CardPadding, (int)CardPadding, (int)CardPadding);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var thumbnailObject = new GameObject("Thumbnail");
        var thumbnailRect = thumbnailObject.AddComponent<RectTransform>();
        thumbnailObject.transform.SetParent(cardObject.transform, false);
        var thumbnailImage = thumbnailObject.AddComponent<Image>();
        thumbnailImage.sprite = RoundedRectSprite.Get(14);
        thumbnailImage.type = Image.Type.Sliced;
        thumbnailImage.color = UiPalette.EmptyCellFill;
        var thumbnailLayoutElement = thumbnailObject.AddComponent<LayoutElement>();
        thumbnailLayoutElement.preferredHeight = ThumbnailHeight;

        var iconObject = new GameObject("Icon");
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconObject.transform.SetParent(thumbnailObject.transform, false);
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(ThumbnailHeight - 20f, ThumbnailHeight - 20f);

        CeramicDefinition definition = ResolveDefinition(entry.Tier);
        var preview = iconObject.AddComponent<UiCeramicPreview>();
        preview.Configure(iconRect);
        // A gallery entry is always a completed ceramic — every crack
        // repaired, unconditionally, regardless of the definition's own
        // totalCracks.
        preview.SetCeramic(definition, definition != null ? definition.totalCracks : 0);

        BuildCardText(cardObject.transform, entry.Date, CardDetailFontSize, UiPalette.TextSecondary);
        BuildCardText(cardObject.transform, string.Format(Strings.GalleryCardScoreFormat, entry.Score.ToString("N0")), CardTierFontSize, UiPalette.TextPrimary, bold: true);
    }

    private static void BuildCardText(Transform parent, string content, int fontSize, Color colour, bool bold = false)
    {
        var textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = colour;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        var layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = fontSize * 1.3f;
        layoutElement.flexibleWidth = 1f;
    }

    private CeramicDefinition ResolveDefinition(int tier)
    {
        int definitionTier = CeramicTierResolver.ResolveDefinitionTier(tier);
        foreach (CeramicDefinition definition in _ceramicPool)
        {
            if (definition.tier == definitionTier)
            {
                return definition;
            }
        }

        return null;
    }

    public bool IsVisible => _panel != null && _panel.activeSelf;

    public void Show()
    {
        // Activate first, refresh second — RefreshCards() builds uGUI
        // layout elements (GridLayoutGroup/ContentSizeFitter) that only
        // resolve correctly against an active hierarchy; doing this in
        // the opposite order was the real cause of a card-layout bug
        // caught via a rendered screenshot (see GalleryScreen's own
        // RefreshCards doc comment for the specific fix that was needed
        // alongside this reordering).
        _panel.SetActive(true);
        RefreshCards();
        if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = false;
        }

        // CLAUDE.md §5.1: "Banner: Home screen and gallery screen only."
        AdManager.Instance?.ShowBanner();
    }

    public void Hide()
    {
        _panel.SetActive(false);
        AdManager.Instance?.HideBanner();

        // Gallery is only ever opened from Home, so closing it returns
        // there (Home's own Show() keeps InputEnabled false, since
        // gameplay isn't the destination) — only falls back to directly
        // re-enabling input if somehow no HomeScreen was wired. Home's
        // own Show() re-shows the banner immediately after the HideBanner
        // call above, which is fine — ShowBanner/HideBanner are both
        // idempotent no-ops when already in the requested state.
        if (_homeScreen != null)
        {
            _homeScreen.Show();
        }
        else if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = true;
        }
    }
}
