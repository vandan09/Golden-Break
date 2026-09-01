using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Daily challenge panel (CLAUDE.md §4.2): today's status, best score
/// today (if already attempted), and the synthetic "ghost" target to
/// beat (see DailyChallengeManager.ComputeGhostScore). Same runtime-built
/// full-screen-overlay pattern as GalleryScreen/SettingsScreen.
///
/// Doesn't start the daily-challenge session itself — Play invokes an
/// injected callback and leaves the composition root (GameplayController)
/// to activate its own, fully separate Daily Challenge object graph
/// (own GridManager/PieceTrayController/PieceController, per
/// PieceController's own doc comment) — this screen has no business
/// owning any of that.
/// </summary>
public sealed class DailyChallengeUI : MonoBehaviour
{
    private const int StatusFontSize = 22;
    private const int ButtonLabelFontSize = 26;

    private SaveManager _saveManager;
    private InputHandler _inputHandler;
    private HomeScreen _homeScreen;
    private Action _onPlayClicked;
    private Func<DateTime> _nowProvider;

    // Set post-construction (GameplayController builds HomeScreen last).
    // Only the header's back button (and BackButtonRouter) return to Home —
    // tapping Play deliberately does not, it starts the daily-challenge
    // game instead. Real bug caught on-device: Home's own panel never
    // hid itself when opening this screen, so it opened invisibly behind
    // Home and never received a single tap.
    public void SetHomeScreen(HomeScreen homeScreen)
    {
        _homeScreen = homeScreen;
    }

    private GameObject _panel;
    private Text _statusText;
    private Text _ghostScoreText;

    public void Configure(SaveManager saveManager, InputHandler inputHandler, Action onPlayClicked, Func<DateTime> nowProvider = null)
    {
        _saveManager = saveManager;
        _inputHandler = inputHandler;
        _onPlayClicked = onPlayClicked;
        _nowProvider = nowProvider ?? (() => DateTime.UtcNow);

        BuildUi();
        _panel.SetActive(false);
    }

    private void BuildUi()
    {
        Canvas canvas = ResponsiveCanvasSetup.BuildCanvas(transform, "DailyChallengeCanvas", 20);
        RectTransform safeArea = ResponsiveCanvasSetup.BuildSafeArea(canvas.transform);

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(safeArea, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = UiPalette.Background;
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        UiKit.BuildBackHeader(_panel.transform, Strings.DailyChallengeTitle, Hide);

        _statusText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.6f), StatusFontSize, UiPalette.TextSecondary);
        _ghostScoreText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.53f), StatusFontSize, UiPalette.GoldFill);

        BuildPlayButton(_panel.transform);
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

        // Shrink to fit rather than run past the screen edge.
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMaxSize = fontSize;
        text.resizeTextMinSize = Mathf.Max(10, fontSize / 2);

        // Stretched to the screen width minus padding rather than a fixed
        // 600 units, which was wider than the 390-unit canvas itself and
        // hung off both edges.
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, anchor.y);
        rect.anchorMax = new Vector2(1f, anchor.y);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(24f, -30f);
        rect.offsetMax = new Vector2(-24f, 30f);

        return text;
    }

    private void BuildPlayButton(Transform parent)
    {
        Button button = UiKit.BuildButton(
            parent,
            "PlayButton",
            Strings.DailyChallengePlayButton,
            OnPlayClicked,
            UiKit.ButtonStyle.Primary,
            UiKit.PrimaryButtonHeight,
            TriangleSprite.Get(),
            new Vector2(14f, 18f));

        UiKit.AnchorCentred(
            button.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.35f),
            260f,
            UiKit.PrimaryButtonHeight);

        UiKit.AddPrimaryGlow(button, UiKit.PrimaryButtonHeight);
    }

    private static void AddBorder(RectTransform target, int cornerRadiusPixels, Color colour, int strokeWidth = 1)
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

        // A border is a full-rect overlay, never a layout row. Without this,
        // a parent VerticalLayoutGroup/HorizontalLayoutGroup treats it as a
        // child and gives it a row of its own — Image implements
        // ILayoutElement, so it reports the border sprite's native size —
        // squeezing the real content. On the gallery card that pushed the
        // date and score rows to zero height, making them invisible.
        borderObject.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    private void OnPlayClicked()
    {
        // Deliberately does not go through Hide() — Play starts the
        // daily-challenge game, it must not bounce back to Home the way
        // the header back button and the hardware back do. Re-enables input directly
        // (Show() disabled it, and the injected callback only builds/
        // activates the Daily Challenge view, it has no notion of
        // InputHandler to re-enable it as a side effect).
        _panel.SetActive(false);
        if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = true;
        }

        _onPlayClicked?.Invoke();
    }

    public bool IsVisible => _panel != null && _panel.activeSelf;

    public void Show()
    {
        DateTime today = _nowProvider();
        string todayIso = today.ToString("yyyy-MM-dd");
        SaveData data = _saveManager.Current;

        if (DailyChallengeManager.IsCompletedToday(data.DailyCompleted, todayIso) && data.DailyBestScores.TryGetValue(todayIso, out int bestToday))
        {
            _statusText.text = string.Format(Strings.DailyChallengeBestTodayFormat, bestToday.ToString("N0"));
        }
        else
        {
            _statusText.text = Strings.DailyChallengeNotPlayedToday;
        }

        int ghostScore = DailyChallengeManager.ComputeGhostScore(today);
        _ghostScoreText.text = string.Format(Strings.DailyChallengeGhostScoreFormat, ghostScore.ToString("N0"));

        _panel.SetActive(true);
        UiKit.PlayOverlayShow(_panel);
        if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = false;
        }
    }

    public void Hide()
    {
        _panel.SetActive(false);

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
