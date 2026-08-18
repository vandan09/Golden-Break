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
/// </summary>
public sealed class HomeScreen : MonoBehaviour
{
    private const int TitleFontSize = 40;
    private const int StreakFontSize = 24;
    private const int CeramicFontSize = 20;
    private const int ButtonLabelFontSize = 24;

    private SaveManager _saveManager;
    private InputHandler _inputHandler;
    private GalleryScreen _galleryScreen;
    private SettingsScreen _settingsScreen;
    private DailyChallengeUI _dailyChallengeUI;
    private Func<DateTime> _nowProvider;

    private GameObject _panel;
    private Text _streakText;
    private Text _ceramicText;

    public void Configure(
        SaveManager saveManager,
        InputHandler inputHandler,
        GalleryScreen galleryScreen,
        SettingsScreen settingsScreen,
        DailyChallengeUI dailyChallengeUI,
        Func<DateTime> nowProvider = null)
    {
        _saveManager = saveManager;
        _inputHandler = inputHandler;
        _galleryScreen = galleryScreen;
        _settingsScreen = settingsScreen;
        _dailyChallengeUI = dailyChallengeUI;
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

        ReviewManager.RequestReviewIfEligible(data);

        _saveManager.Save();
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
        var canvasObject = new GameObject("HomeCanvas");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25; // above every other overlay — the very first thing the player sees
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvasObject.transform, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = UiPalette.Background;
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        CreateText(_panel.transform, Strings.AppTitle, new Vector2(0.5f, 0.8f), TitleFontSize, UiPalette.GoldFill);
        _streakText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.71f), StreakFontSize, UiPalette.TextPrimary);
        _ceramicText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.65f), CeramicFontSize, UiPalette.TextSecondary);

        BuildButton(_panel.transform, Strings.HomePlayButton, new Vector2(0.5f, 0.45f), OnPlayClicked);
        BuildButton(_panel.transform, Strings.HomeDailyChallengeButton, new Vector2(0.5f, 0.35f), OnDailyChallengeClicked);
        BuildButton(_panel.transform, Strings.HomeGalleryButton, new Vector2(0.5f, 0.25f), OnGalleryClicked);
        BuildButton(_panel.transform, Strings.HomeSettingsButton, new Vector2(0.5f, 0.15f), OnSettingsClicked);
    }

    private static Text CreateText(Transform parent, string initialText, Vector2 anchor, int fontSize, Color colour)
    {
        var textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);

        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = colour;
        text.text = initialText;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600f, 60f);

        return text;
    }

    private void BuildButton(Transform parent, string label, Vector2 anchor, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject($"{label}Button");
        buttonObject.transform.SetParent(parent, false);
        buttonObject.AddComponent<Image>().color = UiPalette.Surface;
        buttonObject.AddComponent<Button>().onClick.AddListener(onClick);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(320f, 70f);

        var text = CreateText(buttonObject.transform, label, Vector2.zero, ButtonLabelFontSize, UiPalette.TextPrimary);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void OnPlayClicked()
    {
        Hide();
    }

    private void OnGalleryClicked()
    {
        _galleryScreen?.Show();
    }

    private void OnSettingsClicked()
    {
        _settingsScreen?.Show();
    }

    private void OnDailyChallengeClicked()
    {
        _dailyChallengeUI?.Show();
    }

    private void Show()
    {
        SaveData data = _saveManager.Current;
        _streakText.text = data.StreakCount > 0 ? string.Format(Strings.HomeStreakActiveFormat, data.StreakCount) : Strings.HomeStreakStartPrompt;

        int repaired = data.CurrentCeramic.CracksRepaired;
        int total = data.CurrentCeramic.TotalCracks;
        _ceramicText.text = string.Format(Strings.CeramicProgressFormat, repaired, total);

        _panel.SetActive(true);
        if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = false;
        }
    }

    private void Hide()
    {
        _panel.SetActive(false);
        if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = true;
        }
    }
}
