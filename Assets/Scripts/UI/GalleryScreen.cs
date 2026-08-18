using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scrollable gallery of completed ceramics (CLAUDE.md §3.5): thumbnail,
/// completion date, and cumulative score per entry. Opens as a full-screen
/// overlay above the HUD via its own "Gallery" button — the Home screen
/// that would normally host this entry point is Phase 4 scope, so this
/// button lives on gameplay's own canvas for now (documented deviation,
/// see PROGRESS.md).
///
/// Rebuilds its card list every time it's opened rather than reacting to
/// a live event — GalleryManager has no OnEntryAdded event because
/// nothing needs one yet: the screen is closed during play, so "always
/// current when shown" is sufficient and simpler than keeping a hidden
/// view in sync in real time.
/// </summary>
public sealed class GalleryScreen : MonoBehaviour
{
    private const int TitleFontSize = 32;
    private const int CardTierFontSize = 24;
    private const int CardDetailFontSize = 18;
    private const float CardHeight = 110f;
    private const float CardSpacing = 16f;

    private GalleryManager _galleryManager;
    private CeramicDefinition[] _ceramicPool;
    private GameObject _panel;
    private RectTransform _contentRect;
    private Text _emptyStateText;

    public void Configure(GalleryManager galleryManager, CeramicDefinition[] ceramicPool)
    {
        _galleryManager = galleryManager;
        _ceramicPool = ceramicPool;

        BuildUi();
        _panel.SetActive(false);
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("GalleryCanvas");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15; // above GameOverScreen (10) — gallery can be opened over a fresh game too
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        BuildOpenButton(canvasObject.transform);

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvasObject.transform, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = UiPalette.Background;
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        BuildTitleText(_panel.transform);
        BuildCloseButton(_panel.transform);
        BuildEmptyStateText(_panel.transform);
        BuildScrollView(_panel.transform);
    }

    private void BuildOpenButton(Transform parent)
    {
        var buttonObject = new GameObject("OpenGalleryButton");
        buttonObject.transform.SetParent(parent, false);
        buttonObject.AddComponent<Image>().color = UiPalette.Surface;
        buttonObject.AddComponent<Button>().onClick.AddListener(Show);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(150f, 50f);

        BuildFillCenteredText(buttonObject.transform, "Gallery", 20, UiPalette.TextPrimary);
    }

    private void BuildCloseButton(Transform parent)
    {
        var buttonObject = new GameObject("CloseButton");
        buttonObject.transform.SetParent(parent, false);
        buttonObject.AddComponent<Image>().color = UiPalette.Surface;
        buttonObject.AddComponent<Button>().onClick.AddListener(Hide);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -20f);
        rect.sizeDelta = new Vector2(60f, 50f);

        BuildFillCenteredText(buttonObject.transform, "X", 24, UiPalette.TextPrimary);
    }

    private void BuildTitleText(Transform parent)
    {
        var textObject = new GameObject("Title");
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = TitleFontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = UiPalette.TextPrimary;
        text.text = "Gallery";

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -20f);
        rect.sizeDelta = new Vector2(400f, 60f);
    }

    private void BuildEmptyStateText(Transform parent)
    {
        var textObject = new GameObject("EmptyStateText");
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = UiPalette.TextSecondary;
        text.text = "No completed ceramics yet - repair your first one!";

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
        scrollRectTransform.anchorMin = new Vector2(0.05f, 0.08f);
        scrollRectTransform.anchorMax = new Vector2(0.95f, 0.82f);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;

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
        _contentRect = contentObject.AddComponent<RectTransform>();
        _contentRect.anchorMin = new Vector2(0f, 1f);
        _contentRect.anchorMax = new Vector2(1f, 1f);
        _contentRect.pivot = new Vector2(0.5f, 1f);
        _contentRect.anchoredPosition = Vector2.zero;

        var layoutGroup = contentObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = CardSpacing;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;

        var sizeFitter = contentObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = _contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
    }

    private void RefreshCards()
    {
        for (int i = _contentRect.childCount - 1; i >= 0; i--)
        {
            Destroy(_contentRect.GetChild(i).gameObject);
        }

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
        cardObject.transform.SetParent(_contentRect, false);
        cardObject.AddComponent<RectTransform>();
        var layoutElement = cardObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = CardHeight;
        layoutElement.flexibleWidth = 1f;
        cardObject.AddComponent<Image>().color = UiPalette.Surface;

        var thumbnail = new GameObject("Thumbnail");
        thumbnail.transform.SetParent(cardObject.transform, false);
        thumbnail.AddComponent<Image>().color = UiPalette.GetBlockColour(entry.Tier);
        var thumbnailRect = thumbnail.GetComponent<RectTransform>();
        thumbnailRect.anchorMin = new Vector2(0f, 0.5f);
        thumbnailRect.anchorMax = new Vector2(0f, 0.5f);
        thumbnailRect.pivot = new Vector2(0f, 0.5f);
        thumbnailRect.anchoredPosition = new Vector2(16f, 0f);
        thumbnailRect.sizeDelta = new Vector2(78f, 78f);

        string displayName = ResolveDisplayName(entry.Tier);
        BuildLeftAlignedText(cardObject.transform, $"Tier {entry.Tier} - {displayName}", new Vector2(112f, -18f), CardTierFontSize, UiPalette.TextPrimary);
        BuildLeftAlignedText(cardObject.transform, $"Completed {entry.Date}", new Vector2(112f, -50f), CardDetailFontSize, UiPalette.TextSecondary);
        BuildLeftAlignedText(cardObject.transform, $"Score {entry.Score:N0}", new Vector2(112f, -78f), CardDetailFontSize, UiPalette.GoldFill);
    }

    private string ResolveDisplayName(int tier)
    {
        int definitionTier = CeramicTierResolver.ResolveDefinitionTier(tier);
        foreach (CeramicDefinition definition in _ceramicPool)
        {
            if (definition.tier == definitionTier)
            {
                return definition.displayName;
            }
        }

        return "Ceramic";
    }

    private static void BuildLeftAlignedText(Transform parent, string content, Vector2 anchoredPosition, int fontSize, Color colour)
    {
        var textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.UpperLeft;
        text.color = colour;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(420f, 30f);
    }

    private static void BuildFillCenteredText(Transform parent, string content, int fontSize, Color colour)
    {
        var textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = colour;
        text.text = content;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void Show()
    {
        RefreshCards();
        _panel.SetActive(true);
    }

    private void Hide()
    {
        _panel.SetActive(false);
    }
}
