using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-attempt panel for a Daily Challenge session (CLAUDE.md §4.2) —
/// the Daily Challenge equivalent of <see cref="GameOverScreen"/>, its own
/// separate component rather than that one reused with some parts turned
/// off.
///
/// This deliberately had no "watch ad to continue" offer: the ghost-score
/// comparison is only strictly fair if every attempt plays the same seeded
/// sequence end to end, and a continue gives the player two extra rows the
/// ghost never got. Added anyway at the player.s request, so a daily run
/// can be saved the same way a regular one can. Worth knowing the trade:
/// daily best scores after this are not directly comparable with ones set
/// before it, and a continued run beating the ghost is not the same
/// achievement as an uninterrupted one. "Double coins" is still absent —
/// daily rewards are fixed by CLAUDE.md §4.2.
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
    private const float SidePadding = 24f;
    private const float TextRowHalfHeight = 30f;
    private const float ButtonHeight = 56f;

    private PieceController _pieceController;
    private DailyChallengeSaveTriggers _saveTriggers;
    private DailyMedallionController _medallionController;
    private Action _onPlayAgain;
    private RewardedAdController _rewardedAdController;
    private Button _continueButton;
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
        RewardedAdController rewardedAdController = null,
        Func<DateTime> nowProvider = null)
    {
        _pieceController = pieceController;
        _saveTriggers = saveTriggers;
        _medallionController = medallionController;
        _onPlayAgain = onPlayAgain;
        _onClose = onClose;
        _rewardedAdController = rewardedAdController;
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

        // Continue is the same offer regular play makes (CLAUDE.md §5.1):
        // watch an ad, keep the run. Shown only when a continue is actually
        // available - one per attempt - so it never appears as a dead
        // button on the second game-over.
        _continueButton = BuildButton(Strings.GameOverContinueButton, new Vector2(0.5f, 0.31f), OnContinueClicked, UiKit.ButtonStyle.Primary);
        BuildButton(Strings.GameOverPlayAgainButton, new Vector2(0.5f, 0.22f), OnPlayAgainClicked, UiKit.ButtonStyle.Secondary);
        BuildButton(Strings.DailyChallengeCloseButton, new Vector2(0.5f, 0.13f), OnCloseClicked, UiKit.ButtonStyle.Secondary);
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

        // Stretched to the screen width minus padding, holding only the
        // vertical position from the caller's anchor. This was a fixed
        // 600-unit-wide box on a canvas 390 units wide, so it hung 105
        // units off each edge and every long string — the game-over title
        // among them — rendered outside the screen. A fixed width cannot be
        // right on a canvas whose width is the one thing that varies.
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, anchor.y);
        rect.anchorMax = new Vector2(1f, anchor.y);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(SidePadding, -TextRowHalfHeight);
        rect.offsetMax = new Vector2(-SidePadding, TextRowHalfHeight);

        return text;
    }

    private Button BuildButton(string label, Vector2 anchor, UnityEngine.Events.UnityAction onClick, UiKit.ButtonStyle style)
    {
        Button button = UiKit.BuildButton(_panel.transform, $"{label}Button", label, onClick, style, ButtonHeight);
        UiKit.AnchorCentred(button.GetComponent<RectTransform>(), anchor, ButtonWidth, ButtonHeight);

        if (style == UiKit.ButtonStyle.Primary)
        {
            UiKit.AddPrimaryGlow(button, ButtonHeight);
        }

        return button;
    }

    // Watch an ad to resume the same daily attempt. The daily board is its
    // own PieceController, so TryContinue clears its bottom two rows and
    // deals a fresh hand exactly as it does in regular play, and the score
    // carries over. A failed or abandoned ad grants nothing and leaves the
    // panel up (CLAUDE.md §5.1: never block the player).
    private void OnContinueClicked()
    {
        if (_rewardedAdController == null)
        {
            return;
        }

        _rewardedAdController.RequestContinue(succeeded =>
        {
            if (succeeded)
            {
                _panel.SetActive(false);
            }
        });
    }

    private void Show()
    {
        _finalScoreText.text = _pieceController.Score.CurrentScore.ToString("N0");

        // One continue per attempt, and only when an ad path exists.
        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(_rewardedAdController != null && _pieceController.CanContinue);
        }

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
