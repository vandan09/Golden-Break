using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Live HUD for an in-progress Daily Challenge attempt (CLAUDE.md §4.2) —
/// the Daily Challenge equivalent of <see cref="GameplayHUD"/>, built as
/// its own separate, much smaller component rather than a mode-branch
/// inside that one. Deliberately has no undo/refresh buttons: Daily
/// Challenge is meant to be a genuinely harder, distinct challenge
/// (confirmed with the player), and dropping the regular game's
/// coin/ad-assisted safety nets is part of what makes it harder, not an
/// oversight.
/// </summary>
public sealed class DailyChallengeHUD : MonoBehaviour
{
    private const int ScoreFontSize = 42;
    private const int GhostFontSize = 20;
    private const int BestTodayFontSize = 20;
    private const int MenuButtonLabelFontSize = 16;

    private PieceController _pieceController;
    private SaveManager _saveManager;
    private Action _onMenuClicked;
    private Func<DateTime> _nowProvider;

    private Text _scoreText;
    private Text _ghostText;
    private Text _bestTodayText;

    public void Configure(PieceController pieceController, SaveManager saveManager, Action onMenuClicked, Func<DateTime> nowProvider = null)
    {
        _pieceController = pieceController;
        _saveManager = saveManager;
        _onMenuClicked = onMenuClicked;
        _nowProvider = nowProvider ?? (() => DateTime.UtcNow);

        BuildUi();

        _pieceController.Score.OnScoreChanged += _ => RefreshScoreText();

        RefreshScoreText();
        RefreshGhostAndBestTexts();
    }

    // Called by GameplayController each time a Daily Challenge attempt
    // (re)starts — best-today can change between attempts within the same
    // day, and there's no PieceController event for "a completed attempt
    // just updated daily_best_scores" for this to hook instead.
    public void RefreshGhostAndBestTexts()
    {
        DateTime today = _nowProvider();
        string todayIso = today.ToString("yyyy-MM-dd");

        int ghostScore = DailyChallengeManager.ComputeGhostScore(today);
        _ghostText.text = string.Format(Strings.DailyChallengeGhostScoreFormat, ghostScore.ToString("N0"));

        if (_saveManager.Current.DailyBestScores.TryGetValue(todayIso, out int bestToday))
        {
            _bestTodayText.text = string.Format(Strings.DailyChallengeBestTodayFormat, bestToday.ToString("N0"));
        }
        else
        {
            _bestTodayText.text = Strings.DailyChallengeNotPlayedToday;
        }
    }

    private void RefreshScoreText()
    {
        _scoreText.text = _pieceController.Score.CurrentScore.ToString("N0");
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("DailyChallengeHUDCanvas");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _scoreText = CreateText(canvasObject.transform, "ScoreText", new Vector2(0f, 1f), new Vector2(24f, -24f), ScoreFontSize, TextAnchor.UpperLeft, UiPalette.TextPrimary);
        _ghostText = CreateText(canvasObject.transform, "GhostText", new Vector2(1f, 1f), new Vector2(-24f, -24f), GhostFontSize, TextAnchor.UpperRight, UiPalette.GoldFill);
        _bestTodayText = CreateText(canvasObject.transform, "BestTodayText", new Vector2(1f, 1f), new Vector2(-24f, -52f), BestTodayFontSize, TextAnchor.UpperRight, UiPalette.TextSecondary);

        BuildMenuButton(canvasObject.transform);
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, int fontSize, TextAnchor alignment, Color colour)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = colour;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(400f, 60f);

        return text;
    }

    private void BuildMenuButton(Transform parent)
    {
        var buttonObject = new GameObject("MenuButton");
        buttonObject.transform.SetParent(parent, false);
        buttonObject.AddComponent<Image>().color = UiPalette.Surface;
        buttonObject.AddComponent<Button>().onClick.AddListener(() => _onMenuClicked?.Invoke());

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-24f, 24f);
        rect.sizeDelta = new Vector2(160f, 56f);

        var label = CreateText(buttonObject.transform, "Label", Vector2.zero, Vector2.zero, MenuButtonLabelFontSize, TextAnchor.MiddleCenter, UiPalette.TextPrimary);
        label.text = Strings.MenuButtonLabel;
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }
}
