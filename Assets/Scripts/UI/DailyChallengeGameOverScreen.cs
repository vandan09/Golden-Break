using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-attempt panel for a Daily Challenge session (CLAUDE.md §4.2) —
/// the Daily Challenge equivalent of <see cref="GameOverScreen"/>, its own
/// separate component rather than that one reused with some parts turned
/// off. Deliberately no "watch ad to continue" or "double coins" offers:
/// Daily Challenge's ghost-score comparison only stays fair if every
/// attempt plays the exact same seeded sequence/obstacle layout end to
/// end, so there is nothing here for a rewarded ad to meaningfully unlock.
/// </summary>
public sealed class DailyChallengeGameOverScreen : MonoBehaviour
{
    private const int TitleFontSize = 28;
    private const int FinalScoreFontSize = 48;
    private const int GhostFontSize = 20;
    private const int NewBestFontSize = 20;
    private const int CoinsEarnedFontSize = 18;
    private const int PerfectRunFontSize = 18;
    private const float ButtonWidth = 280f;
    private const float ButtonHeight = 56f;

    private PieceController _pieceController;
    private DailyChallengeSaveTriggers _saveTriggers;
    private DailyMedallionController _medallionController;
    private Action _onPlayAgain;
    private Action _onClose;
    private Func<DateTime> _nowProvider;

    private GameObject _panel;
    private Text _finalScoreText;
    private Text _ghostText;
    private Text _newBestText;
    private Text _coinsEarnedText;
    private Text _perfectRunText;

    public void Configure(
        PieceController pieceController,
        DailyChallengeSaveTriggers saveTriggers,
        DailyMedallionController medallionController,
        Action onPlayAgain,
        Action onClose,
        Func<DateTime> nowProvider = null)
    {
        _pieceController = pieceController;
        _saveTriggers = saveTriggers;
        _medallionController = medallionController;
        _onPlayAgain = onPlayAgain;
        _onClose = onClose;
        _nowProvider = nowProvider ?? (() => DateTime.UtcNow);

        BuildUi();

        // Relies on DailyChallengeSaveTriggers already being constructed
        // and subscribed to the same PieceController.OnGameOver before
        // this Configure() call — its handler must populate
        // LastCompletionResult before Show() below reads it. Matches the
        // same ordering GameplayController already uses for the regular
        // GameOverScreen/GameplaySaveTriggers pair.
        _pieceController.OnGameOver += Show;
        _panel.SetActive(false);
    }

    private void BuildUi()
    {
        Canvas canvas = ResponsiveCanvasSetup.BuildCanvas(transform, "DailyChallengeGameOverCanvas", 10);
        RectTransform safeArea = ResponsiveCanvasSetup.BuildSafeArea(canvas.transform);

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(safeArea, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = new Color(UiPalette.Background.r, UiPalette.Background.g, UiPalette.Background.b, 0.92f);
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        CreateText(_panel.transform, Strings.DailyChallengeGameOverTitle, new Vector2(0.5f, 0.74f), TitleFontSize, UiPalette.TextPrimary);
        _finalScoreText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.65f), FinalScoreFontSize, UiPalette.GoldFill);
        _ghostText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.58f), GhostFontSize, UiPalette.TextSecondary);
        _newBestText = CreateText(_panel.transform, Strings.DailyChallengeNewBestTodayLabel, new Vector2(0.5f, 0.52f), NewBestFontSize, UiPalette.GoldFill);
        _coinsEarnedText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.47f), CoinsEarnedFontSize, UiPalette.GoldFill);
        _perfectRunText = CreateText(_panel.transform, Strings.DailyChallengePerfectRunLabel, new Vector2(0.5f, 0.41f), PerfectRunFontSize, UiPalette.GoldFill);

        BuildButton(Strings.GameOverPlayAgainButton, new Vector2(0.5f, 0.30f), OnPlayAgainClicked, UiKit.ButtonStyle.Primary);
        BuildButton(Strings.DailyChallengeCloseButton, new Vector2(0.5f, 0.20f), OnCloseClicked, UiKit.ButtonStyle.Secondary);
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

    private void BuildButton(string label, Vector2 anchor, UnityEngine.Events.UnityAction onClick, UiKit.ButtonStyle style)
    {
        Button button = UiKit.BuildButton(_panel.transform, $"{label}Button", label, onClick, style, ButtonHeight);
        UiKit.AnchorCentred(button.GetComponent<RectTransform>(), anchor, ButtonWidth, ButtonHeight);

        if (style == UiKit.ButtonStyle.Primary)
        {
            UiKit.AddPrimaryGlow(button, ButtonHeight);
        }
    }

    private void Show()
    {
        _finalScoreText.text = _pieceController.Score.CurrentScore.ToString("N0");

        int ghostScore = DailyChallengeManager.ComputeGhostScore(_nowProvider());
        _ghostText.text = string.Format(Strings.DailyChallengeGhostScoreFormat, ghostScore.ToString("N0"));

        DailyChallengeManager.CompletionResult? result = _saveTriggers?.LastCompletionResult;
        _newBestText.gameObject.SetActive(result.HasValue && result.Value.IsNewBestForToday);

        int coinsAwarded = result.HasValue ? result.Value.CoinsAwarded : 0;
        _coinsEarnedText.gameObject.SetActive(coinsAwarded > 0);
        if (coinsAwarded > 0)
        {
            _coinsEarnedText.text = string.Format(Strings.DailyChallengeCoinsEarnedFormat, coinsAwarded);
        }

        _perfectRunText.gameObject.SetActive(_medallionController != null && _medallionController.CompletedThisAttempt);

        _panel.SetActive(true);
        UiKit.PlayOverlayShow(_panel);
    }

    private void OnPlayAgainClicked()
    {
        _panel.SetActive(false);
        _onPlayAgain?.Invoke();
    }

    private void OnCloseClicked()
    {
        _panel.SetActive(false);
        _onClose?.Invoke();
    }
}
