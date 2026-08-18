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
/// injected callback and leaves actually calling
/// PieceController.StartDailyChallenge to whoever configured this (the
/// composition root has the PieceController/PieceDefinition pool this
/// screen has no business owning).
/// </summary>
public sealed class DailyChallengeUI : MonoBehaviour
{
    private const int TitleFontSize = 32;
    private const int StatusFontSize = 22;
    private const int ButtonLabelFontSize = 26;

    private SaveManager _saveManager;
    private InputHandler _inputHandler;
    private Action _onPlayClicked;
    private Func<DateTime> _nowProvider;

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
        var canvasObject = new GameObject("DailyChallengeCanvas");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
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

        CreateText(_panel.transform, "Daily Challenge", new Vector2(0.5f, 0.72f), TitleFontSize, UiPalette.TextPrimary);
        _statusText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.6f), StatusFontSize, UiPalette.TextSecondary);
        _ghostScoreText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.53f), StatusFontSize, UiPalette.GoldFill);

        BuildCloseButton(_panel.transform);
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

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600f, 60f);

        return text;
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

        var label = CreateText(buttonObject.transform, "X", Vector2.zero, 24, UiPalette.TextPrimary);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void BuildPlayButton(Transform parent)
    {
        var buttonObject = new GameObject("PlayButton");
        buttonObject.transform.SetParent(parent, false);
        buttonObject.AddComponent<Image>().color = UiPalette.Surface;
        buttonObject.AddComponent<Button>().onClick.AddListener(OnPlayClicked);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.35f);
        rect.anchorMax = new Vector2(0.5f, 0.35f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(280f, 70f);

        var label = CreateText(buttonObject.transform, "Play", Vector2.zero, ButtonLabelFontSize, UiPalette.TextPrimary);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void OnPlayClicked()
    {
        Hide();
        _onPlayClicked?.Invoke();
    }

    public void Show()
    {
        DateTime today = _nowProvider();
        string todayIso = today.ToString("yyyy-MM-dd");
        SaveData data = _saveManager.Current;

        if (DailyChallengeManager.IsCompletedToday(data.DailyCompleted, todayIso) && data.DailyBestScores.TryGetValue(todayIso, out int bestToday))
        {
            _statusText.text = $"Best today: {bestToday:N0}";
        }
        else
        {
            _statusText.text = "Not played today";
        }

        int ghostScore = DailyChallengeManager.ComputeGhostScore(today);
        _ghostScoreText.text = $"Beat: {ghostScore:N0}";

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
