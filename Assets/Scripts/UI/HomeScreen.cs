using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// App entry hub (CLAUDE.md §3.6: "Open app -> see streak status + current
/// ceramic progress -> Tap Play -> game starts immediately"). A full-
/// screen overlay shown once at startup rather than a separate scene —
/// BootController currently jumps straight Boot->Gameplay, and every
/// other Phase 2-3 screen (GameOverScreen, GalleryScreen) already proved
/// this runtime-overlay pattern works reliably, whereas a real scene
/// switch would reintroduce exactly the class of bug Phase 1 hit (scene-
/// load timing — see PROGRESS.md) for a purely cosmetic gain. Documented
/// deviation from a literal "Home screen" reading, not an oversight.
///
/// Also the natural place for once-per-session setup that has nothing to
/// do with gameplay: session counting, consent/SDK initialization,
/// notification permission timing, and the in-app review trigger — all
/// fire once, immediately, when Home appears (session start), not gated
/// behind tapping Play.
///
/// Layout below is Claude Design UI rework Screen 1 (BUILD_PLAN.md), built
/// to the mockup's exact pixel values against its own 390×844 reference
/// frame — the Canvas uses <see cref="CanvasScaler.ScaleMode.ScaleWithScreenSize"/>
/// against that same reference so those pixel values scale correctly on
/// any real device instead of only being correct at exactly 390×844.
/// </summary>
public sealed class HomeScreen : MonoBehaviour
{
    private const float SidePadding = 24f;
    private const float TopPadding = 64f;
    private const float BottomPadding = 40f;

    private const int TitleFontSize = 34;
    private const int SubtitleFontSize = 13;
    private const int CardTitleFontSize = 15;
    private const int CardDetailFontSize = 13;
    private const int PlayLabelFontSize = 18;
    private const int ButtonLabelFontSize = 14;
    private const int BadgeFontSize = 12;

    private const float CardHeight = 90f;
    private const float CardIconWidth = 72f;
    private const float CardIconHeight = 58f;
    private const float CardBottomGap = 18f;

    private const float PlayButtonHeight = 60f;
    private const float PlayButtonRadius = 30f;

    private const float DailyButtonHeight = 48f;
    private const float DailyButtonRadius = 24f;
    private const float DailyTopGap = 14f;

    private const float RowHeight = 46f;
    private const float RowRadius = 23f;
    private const float RowTopGap = 14f;
    private const float RowGap = 12f;
    private const float SettingsButtonWidth = 46f;

    private static readonly Color BadgeBackground = new Color(232f / 255f, 192f / 255f, 96f / 255f, 0.14f);

    private SaveManager _saveManager;
    private InputHandler _inputHandler;
    private GalleryScreen _galleryScreen;
    private SettingsScreen _settingsScreen;
    private DailyChallengeUI _dailyChallengeUI;
    private CeramicDefinition[] _ceramicPool;
    private Func<DateTime> _nowProvider;

    private GameObject _panel;
    private Text _subtitleText;
    private Text _cardTitleText;
    private Text _cardDetailText;
    private Text _dailyStreakBadgeText;
    private UiCeramicPreview _ceramicPreview;

    // Regular play has no start/resume decision to make any more (see
    // PieceController's own doc comment: it's always exactly one
    // always-alive session, never switched or restored) — Play simply
    // hides Home and lets whatever's already running underneath show
    // through, so there's no callback hook needed here at all.
    public void Configure(
        SaveManager saveManager,
        InputHandler inputHandler,
        GalleryScreen galleryScreen,
        SettingsScreen settingsScreen,
        DailyChallengeUI dailyChallengeUI,
        CeramicDefinition[] ceramicPool,
        Func<DateTime> nowProvider = null)
    {
        _saveManager = saveManager;
        _inputHandler = inputHandler;
        _galleryScreen = galleryScreen;
        _settingsScreen = settingsScreen;
        _dailyChallengeUI = dailyChallengeUI;
        _ceramicPool = ceramicPool ?? Array.Empty<CeramicDefinition>();
        _nowProvider = nowProvider ?? (() => DateTime.UtcNow);

        BuildUi();
        RunSessionStartSequence();
        Show();
    }

    // Fires once, immediately, when Home appears — this IS "app open" in
    // this architecture (no separate splash/loading step). Order matters:
    // consent must resolve before ad SDK init (CLAUDE.md §5.4/§9.3), and
    // TotalSessions must be incremented before other systems (in-app
    // review's own eligibility check, PushNotificationManager's day-3
    // gate) read it.
    private void RunSessionStartSequence()
    {
        SaveData data = _saveManager.Current;
        data.TotalSessions++;

        AnalyticsManager.Instance?.InitializeSdk();
        AnalyticsManager.Instance?.SetCustomDimensions(ResolveCountryCode(), ResolveDdaStateLabel(data), data.TotalGames);
        AnalyticsManager.Instance?.LogEvent("session_start", new System.Collections.Generic.Dictionary<string, object>
        {
            { "session_number", data.TotalSessions },
            { "days_since_install", ComputeDaysSinceInstall(data) }
        });

        AdManager.Instance?.RequestConsentIfRequired(() => AdManager.Instance?.InitializeSdk());

        string todayIso = _nowProvider().ToString("yyyy-MM-dd");
        if (PushNotificationManager.ShouldRequestPermission(data))
        {
            PushNotificationManager.RequestPermission(data, granted => _saveManager.Save());
        }
        PushNotificationManager.ScheduleDailyReminderIfEligible(data, todayIso);

        ReviewManager.RequestReviewIfEligible(data, this);

        _saveManager.Save();
    }

    // CLAUDE.md §6: "custom dimensions: country, DDA state, total games" —
    // set once per session alongside SDK init, not per-event, matching how
    // GameAnalytics' real custom dimensions work (set-and-forget until
    // changed, not attached to individual events).
    private static string ResolveCountryCode()
    {
        try
        {
            return System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;
        }
        catch (Exception)
        {
            return "unknown";
        }
    }

    private static string ResolveDdaStateLabel(SaveData data)
    {
        float last10Average = DDAManager.ComputeLast10Average(data.DdaLast10Scores);
        return DDAManager.Classify(last10Average, data.DdaAvgScore).ToString();
    }

    private int ComputeDaysSinceInstall(SaveData data)
    {
        if (string.IsNullOrEmpty(data.FirstLaunchDate) || !DateTime.TryParse(data.FirstLaunchDate, out DateTime installDate))
        {
            return 0;
        }

        return Math.Max(0, (_nowProvider().Date - installDate.Date).Days);
    }

    private void BuildUi()
    {
        Canvas canvas = ResponsiveCanvasSetup.BuildCanvas(transform, "HomeCanvas", sortingOrder: 25); // above every other overlay — the very first thing the player sees

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvas.transform, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = UiPalette.Background;
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Background fills the full screen edge-to-edge; real content sits
        // inside the safe area so nothing hides under a notch/punch-hole
        // camera/rounded corner.
        RectTransform safeArea = ResponsiveCanvasSetup.BuildSafeArea(_panel.transform);

        BuildTopGroup(safeArea);
        BuildBottomGroup(safeArea);
    }

    private void BuildTopGroup(Transform parent)
    {
        float cursor = TopPadding;

        string titleMarkup = $"<color=#e8c060>Golden</color> Break";
        Text title = CreateTopAnchoredText(parent, "Title", titleMarkup, cursor, 44f, TitleFontSize, UiPalette.TextPrimary);
        title.fontStyle = FontStyle.Bold;
        cursor += 44f + 10f;

        _subtitleText = CreateTopAnchoredText(parent, "Subtitle", string.Empty, cursor, 18f, SubtitleFontSize, UiPalette.TextSecondary);
    }

    private void BuildBottomGroup(Transform parent)
    {
        float cursor = BottomPadding;

        BuildIconRow(parent, cursor);
        cursor += RowHeight + RowTopGap;

        BuildDailyChallengeButton(parent, cursor);
        cursor += DailyButtonHeight + DailyTopGap;

        BuildPlayButton(parent, cursor);
        cursor += PlayButtonHeight + CardBottomGap;

        BuildCeramicCard(parent, cursor);
    }

    private void BuildCeramicCard(Transform parent, float bottomY)
    {
        RectTransform cardRect = CreateBottomAnchoredStretchRect(parent, "CeramicCard", bottomY, CardHeight);
        Image cardImage = cardRect.gameObject.AddComponent<Image>();
        cardImage.sprite = RoundedRectSprite.Get(20);
        cardImage.type = Image.Type.Sliced;
        cardImage.color = UiPalette.Surface;
        AddBorder(cardRect, 20, UiPalette.CardBorder);

        var iconObject = new GameObject("CeramicIcon");
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconObject.transform.SetParent(cardRect, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(18f, 0f);
        iconRect.sizeDelta = new Vector2(CardIconWidth, CardIconHeight);

        _ceramicPreview = iconObject.AddComponent<UiCeramicPreview>();
        _ceramicPreview.Configure(iconRect);

        float textLeft = 18f + CardIconWidth + 16f;
        _cardTitleText = CreateLeftAnchoredText(cardRect, "CardTitle", string.Empty, textLeft, 14f, CardTitleFontSize, UiPalette.TextPrimary);
        _cardDetailText = CreateLeftAnchoredText(cardRect, "CardDetail", string.Empty, textLeft, -10f, CardDetailFontSize, UiPalette.TextSecondary);
    }

    private void BuildPlayButton(Transform parent, float bottomY)
    {
        RectTransform buttonRect = CreateBottomAnchoredStretchRect(parent, "PlayButton", bottomY, PlayButtonHeight);
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.sprite = RoundedRectSprite.Get(30);
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = UiPalette.Surface;
        AddBorder(buttonRect, 30, UiPalette.GoldFill);
        buttonRect.gameObject.AddComponent<Button>().onClick.AddListener(OnPlayClicked);

        var contentObject = new GameObject("Content");
        var contentRect = contentObject.AddComponent<RectTransform>();
        contentObject.transform.SetParent(buttonRect, false);
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var layout = contentObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        BuildPlayTriangle(contentObject.transform, 14f, 18f);
        Text label = CreateAutoSizeText(contentObject.transform, Strings.HomePlayButton.ToUpperInvariant(), PlayLabelFontSize, UiPalette.GoldFill);
        label.fontStyle = FontStyle.Bold;
    }

    // CSS-triangle equivalent: a UI Image using a small procedurally
    // generated right-pointing triangle sprite, matching the mockup's
    // border-trick play icon exactly in silhouette.
    private void BuildPlayTriangle(Transform parent, float width, float height)
    {
        var triangleObject = new GameObject("PlayTriangle");
        var rect = triangleObject.AddComponent<RectTransform>();
        triangleObject.transform.SetParent(parent, false);
        rect.sizeDelta = new Vector2(width, height);
        var image = triangleObject.AddComponent<Image>();
        image.sprite = TriangleSprite.Get();
        image.color = UiPalette.GoldFill;

        var layoutElement = triangleObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = width;
        layoutElement.preferredHeight = height;
    }

    private void BuildDailyChallengeButton(Transform parent, float bottomY)
    {
        RectTransform buttonRect = CreateBottomAnchoredStretchRect(parent, "DailyChallengeButton", bottomY, DailyButtonHeight);
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.sprite = RoundedRectSprite.Get((int)DailyButtonRadius);
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = UiPalette.Surface;
        AddBorder(buttonRect, (int)DailyButtonRadius, UiPalette.CardBorder);
        buttonRect.gameObject.AddComponent<Button>().onClick.AddListener(OnDailyChallengeClicked);

        var contentObject = new GameObject("Content");
        var contentRect = contentObject.AddComponent<RectTransform>();
        contentObject.transform.SetParent(buttonRect, false);
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var layout = contentObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateAutoSizeText(contentObject.transform, Strings.HomeDailyChallengeButton, ButtonLabelFontSize, UiPalette.TextPrimary).fontStyle = FontStyle.Bold;

        var badgeObject = new GameObject("StreakBadge");
        var badgeRect = badgeObject.AddComponent<RectTransform>();
        badgeObject.transform.SetParent(contentObject.transform, false);
        var badgeImage = badgeObject.AddComponent<Image>();
        badgeImage.sprite = RoundedRectSprite.Get(12);
        badgeImage.type = Image.Type.Sliced;
        badgeImage.color = BadgeBackground;
        var badgeLayoutElement = badgeObject.AddComponent<LayoutElement>();
        badgeLayoutElement.preferredHeight = 22f;
        badgeLayoutElement.preferredWidth = 52f;
        badgeRect.sizeDelta = new Vector2(52f, 22f);

        var badgeLayout = badgeObject.AddComponent<HorizontalLayoutGroup>();
        badgeLayout.childAlignment = TextAnchor.MiddleCenter;
        badgeLayout.spacing = 3f;
        badgeLayout.padding = new RectOffset(8, 8, 0, 0);
        badgeLayout.childForceExpandWidth = false;
        badgeLayout.childForceExpandHeight = false;

        // Legacy uGUI Text has no colour-emoji glyph support, so the
        // mockup's "🔥 {streak}" reads as a bare number without this —
        // see FlameIconSprite's own doc comment.
        BuildIcon(badgeObject.transform, FlameIconSprite.Get(), 12f, 12f, UiPalette.GoldFill);

        _dailyStreakBadgeText = CreateAutoSizeText(badgeObject.transform, string.Empty, BadgeFontSize, UiPalette.GoldFill);
        _dailyStreakBadgeText.fontStyle = FontStyle.Bold;
    }

    private void BuildIconRow(Transform parent, float bottomY)
    {
        var rowObject = new GameObject("IconRow");
        var rowRect = rowObject.AddComponent<RectTransform>();
        rowObject.transform.SetParent(parent, false);
        rowRect.anchorMin = new Vector2(0f, 0f);
        rowRect.anchorMax = new Vector2(1f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.offsetMin = new Vector2(SidePadding, bottomY);
        rowRect.offsetMax = new Vector2(-SidePadding, bottomY + RowHeight);

        // Gallery: flexible width via anchors (fills everything except
        // the fixed-width Settings button + gap), matching the mockup's
        // "flex:1" gallery button beside a fixed circular settings button.
        var galleryRect = CreateChildRect(rowRect);
        galleryRect.anchorMin = new Vector2(0f, 0f);
        galleryRect.anchorMax = new Vector2(1f, 1f);
        galleryRect.offsetMin = Vector2.zero;
        galleryRect.offsetMax = new Vector2(-(SettingsButtonWidth + RowGap), 0f);
        Image galleryImage = galleryRect.gameObject.AddComponent<Image>();
        galleryImage.sprite = RoundedRectSprite.Get((int)RowRadius);
        galleryImage.type = Image.Type.Sliced;
        galleryImage.color = UiPalette.Surface;
        AddBorder(galleryRect, (int)RowRadius, UiPalette.CardBorder);
        galleryRect.gameObject.AddComponent<Button>().onClick.AddListener(OnGalleryClicked);

        var galleryContent = new GameObject("Content");
        var galleryContentRect = galleryContent.AddComponent<RectTransform>();
        galleryContent.transform.SetParent(galleryRect, false);
        galleryContentRect.anchorMin = Vector2.zero;
        galleryContentRect.anchorMax = Vector2.one;
        galleryContentRect.offsetMin = Vector2.zero;
        galleryContentRect.offsetMax = Vector2.zero;
        var galleryLayout = galleryContent.AddComponent<HorizontalLayoutGroup>();
        galleryLayout.childAlignment = TextAnchor.MiddleCenter;
        galleryLayout.spacing = 8f;
        galleryLayout.childForceExpandWidth = false;
        galleryLayout.childForceExpandHeight = false;

        BuildIcon(galleryContent.transform, GalleryIconSprite.Get(), 16f, 16f, UiPalette.TextPrimary);
        CreateAutoSizeText(galleryContent.transform, Strings.HomeGalleryButton, ButtonLabelFontSize, UiPalette.TextPrimary).fontStyle = FontStyle.Bold;

        var settingsRect = CreateChildRect(rowRect);
        settingsRect.anchorMin = new Vector2(1f, 0f);
        settingsRect.anchorMax = new Vector2(1f, 1f);
        settingsRect.pivot = new Vector2(1f, 0.5f);
        settingsRect.anchoredPosition = Vector2.zero;
        settingsRect.sizeDelta = new Vector2(SettingsButtonWidth, 0f);
        Image settingsImage = settingsRect.gameObject.AddComponent<Image>();
        settingsImage.sprite = RoundedRectSprite.Get((int)RowRadius);
        settingsImage.type = Image.Type.Sliced;
        settingsImage.color = UiPalette.Surface;
        AddBorder(settingsRect, (int)RowRadius, UiPalette.CardBorder);
        settingsRect.gameObject.AddComponent<Button>().onClick.AddListener(OnSettingsClicked);

        BuildIcon(settingsRect, GearIconSprite.Get(), 20f, 20f, UiPalette.TextPrimary, centered: true);
    }

    private static void BuildIcon(Transform parent, Sprite sprite, float width, float height, Color colour, bool centered = false)
    {
        var iconObject = new GameObject("Icon");
        var rect = iconObject.AddComponent<RectTransform>();
        iconObject.transform.SetParent(parent, false);
        var image = iconObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = colour;

        if (centered)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }
        else
        {
            var layoutElement = iconObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = height;
            rect.sizeDelta = new Vector2(width, height);
        }
    }

    private static RectTransform CreateChildRect(Transform parent)
    {
        var go = new GameObject("Cell");
        var rect = go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        return rect;
    }

    private static RectTransform CreateBottomAnchoredStretchRect(Transform parent, string name, float bottomY, float height)
    {
        var go = new GameObject(name);
        var rect = go.AddComponent<RectTransform>();
        go.transform.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(SidePadding, bottomY);
        rect.offsetMax = new Vector2(-SidePadding, bottomY + height);
        return rect;
    }

    // A child Image using a hollow rounded-rect stroke sprite, not
    // UnityEngine.UI.Outline — Outline's 4-direction shadow trick reads as
    // a dashed/segmented line at these sizes (confirmed via an Editor
    // Play Mode screenshot), especially visible on the gold Play-button
    // border. cornerRadiusPixels matches the same background sprite's own
    // corner radius so the border sits exactly on that edge.
    private static void AddBorder(RectTransform target, int cornerRadiusPixels, Color colour, int strokeWidth = 6)
    {
        var borderObject = new GameObject("Border");
        var borderRect = borderObject.AddComponent<RectTransform>();
        borderObject.transform.SetParent(target, false);
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        var image = borderObject.AddComponent<Image>();
        image.sprite = RoundedRectBorderSprite.Get(cornerRadiusPixels, strokeWidth);
        image.type = Image.Type.Sliced;
        image.color = colour;
        image.raycastTarget = false;
    }

    private static Text CreateTopAnchoredText(Transform parent, string name, string initialText, float topY, float height, int fontSize, Color colour)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = colour;
        text.text = initialText;
        text.supportRichText = true;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(SidePadding, -(topY + height));
        rect.offsetMax = new Vector2(-SidePadding, -topY);

        return text;
    }

    private static Text CreateLeftAnchoredText(Transform parent, string name, string initialText, float leftX, float centerY, int fontSize, Color colour)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = colour;
        text.text = initialText;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(leftX, centerY);
        rect.sizeDelta = new Vector2(220f, 24f);

        return text;
    }

    private static Text CreateAutoSizeText(Transform parent, string initialText, int fontSize, Color colour)
    {
        var textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);

        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = colour;
        text.text = initialText;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        var layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = Mathf.Max(20f, initialText.Length * fontSize * 0.62f);
        layoutElement.preferredHeight = fontSize * 1.3f;

        var rect = textObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(layoutElement.preferredWidth, layoutElement.preferredHeight);

        return text;
    }

    // Whether Home is currently the visible screen — used by
    // BackButtonRouter to decide what a hardware back-press should do
    // (quit the app when Home itself is showing, since it's the top of
    // the navigation stack).
    public bool IsVisible => _panel != null && _panel.activeSelf;

    private void OnPlayClicked()
    {
        Hide();
    }

    // Each of these hides Home *first* — real bug caught on-device:
    // Home's own panel (sorting order 25, opaque, full-screen) never
    // hid itself when opening a sub-screen, so the sub-screen opened
    // invisibly behind it and never received a single tap ("doesn't work
    // on first click"). The sub-screen's own Hide() re-shows Home when
    // the player backs out (see GalleryScreen/SettingsScreen/
    // DailyChallengeUI.SetHomeScreen).
    private void OnGalleryClicked()
    {
        Hide();
        _galleryScreen?.Show();
    }

    private void OnSettingsClicked()
    {
        Hide();
        _settingsScreen?.Show();
    }

    private void OnDailyChallengeClicked()
    {
        Hide();
        _dailyChallengeUI?.Show();
    }

    // Public: also reused as a pause menu, reopened mid-game via
    // GameplayHUD's Menu button or the Android back button
    // (BackButtonRouter) — CLAUDE.md §3.6 never describes a way back to
    // Home once Play is tapped, a real gap caught on-device (see
    // PROGRESS.md). Tapping Play again from this reopened state just
    // resumes — OnPlayClicked only ever calls Hide(), it never touches
    // PieceController, so the game underneath is untouched.
    public void Show()
    {
        SaveData data = _saveManager.Current;
        _subtitleText.text = string.Format(Strings.HomeBestScoreFormat, data.BestScore.ToString("N0"));
        _dailyStreakBadgeText.text = string.Format(Strings.HomeDailyStreakBadgeFormat, data.StreakCount);

        CeramicDefinition definition = FindCeramicDefinition(data.CurrentCeramic.Tier);
        int repaired = data.CurrentCeramic.CracksRepaired;
        int total = data.CurrentCeramic.TotalCracks;

        _cardTitleText.text = definition != null ? definition.displayName : "Ceramic";
        _cardDetailText.text = string.Format(Strings.HomeCrackFractionFormat, repaired, total);
        _ceramicPreview.SetCeramic(definition, repaired);

        _panel.SetActive(true);
        if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = false;
        }

        // CLAUDE.md §5.1: "Banner: Home screen and gallery screen only.
        // Never during gameplay."
        AdManager.Instance?.ShowBanner();
    }

    public void Hide()
    {
        _panel.SetActive(false);
        if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = true;
        }

        AdManager.Instance?.HideBanner();
    }

    private CeramicDefinition FindCeramicDefinition(int tier)
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
}
