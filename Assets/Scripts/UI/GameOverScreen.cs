using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game-over panel (CLAUDE.md §3.6, §5.1): final score, best score,
/// ceramic progress, an optional "watch ad to continue" offer, a "double
/// coins" rewarded offer, and Play Again — which restarts in-place with
/// no scene reload, so the transition stays sub-1-second per the Phase 2
/// acceptance criterion.
///
/// Owns the exact moment CLAUDE.md §5.1's interstitial cadence is
/// evaluated: only once the game-over is actually *resolved* (continue
/// declined, failed, or never offered) — not on the raw OnGameOver event
/// itself, since a successfully continued game isn't "another" game-over
/// for cadence purposes. See InterstitialController's own docstring for
/// the same reasoning from its side.
/// </summary>
public sealed class GameOverScreen : MonoBehaviour
{
    private const int TitleFontSize = 32;
    private const int FinalScoreFontSize = 48;
    private const int BestScoreFontSize = 22;
    private const int CeramicProgressFontSize = 18;
    private const int ButtonLabelFontSize = 22;

    private PieceController _pieceController;
    private GameplaySaveTriggers _saveTriggers;
    private RewardedAdController _rewardedAdController;
    private InterstitialController _interstitialController;
    private StreakPopup _streakPopup;
    private CeramicController _ceramicController;
    private Func<DateTime> _nowProvider;

    private GameObject _panel;
    private Text _finalScoreText;
    private Text _bestScoreText;
    private Text _ceramicProgressText;
    private Text _milestoneText;
    private GameObject _continueButton;
    private GameObject _doubleCoinsButton;
    private ToastMessage _toast;
    private bool _doubleCoinsUsedThisGameOver;
    private bool _resolved;

    public void Configure(
        PieceController pieceController,
        GameplaySaveTriggers saveTriggers,
        RewardedAdController rewardedAdController,
        InterstitialController interstitialController,
        StreakPopup streakPopup,
        CeramicController ceramicController,
        Func<DateTime> nowProvider = null)
    {
        _pieceController = pieceController;
        _saveTriggers = saveTriggers;
        _rewardedAdController = rewardedAdController;
        _interstitialController = interstitialController;
        _streakPopup = streakPopup;
        _ceramicController = ceramicController;
        _nowProvider = nowProvider ?? (() => DateTime.UtcNow);

        BuildUi();

        var toastObject = new GameObject("Toast");
        toastObject.transform.SetParent(transform, false);
        _toast = toastObject.AddComponent<ToastMessage>();
        _toast.Configure(sortingOrder: 11); // above this screen's own canvas (10)

        _pieceController.OnGameOver += Show;
        _panel.SetActive(false);
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("GameOverCanvas");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // above GameplayHUD's default-order canvas
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvasObject.transform, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = new Color(UiPalette.Background.r, UiPalette.Background.g, UiPalette.Background.b, 0.92f);
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        CreateText(_panel.transform, Strings.GameOverTitle, new Vector2(0.5f, 0.74f), TitleFontSize, UiPalette.TextPrimary);
        _finalScoreText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.66f), FinalScoreFontSize, UiPalette.GoldFill);
        _bestScoreText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.59f), BestScoreFontSize, UiPalette.TextSecondary);
        _ceramicProgressText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.54f), CeramicProgressFontSize, UiPalette.TextSecondary);
        _milestoneText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.49f), CeramicProgressFontSize, UiPalette.GoldFill);

        _continueButton = BuildButton(_panel.transform, Strings.GameOverContinueButton, new Vector2(0.5f, 0.40f), OnContinueClicked);
        _doubleCoinsButton = BuildButton(_panel.transform, Strings.GameOverDoubleCoinsButton, new Vector2(0.5f, 0.32f), OnDoubleCoinsClicked);
        BuildButton(_panel.transform, Strings.GameOverPlayAgainButton, new Vector2(0.5f, 0.22f), OnPlayAgainClicked);
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

    private GameObject BuildButton(Transform parent, string label, Vector2 anchor, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject($"{label}Button");
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.AddComponent<Image>();
        image.color = UiPalette.Surface;
        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(320f, 64f);

        Text buttonLabel = CreateText(buttonObject.transform, label, Vector2.zero, ButtonLabelFontSize, UiPalette.TextPrimary);
        var labelRect = buttonLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return buttonObject;
    }

    private void Show()
    {
        _resolved = false;
        _doubleCoinsUsedThisGameOver = false;

        _finalScoreText.text = _pieceController.Score.CurrentScore.ToString("N0");
        _bestScoreText.text = string.Format(Strings.GameOverBestScoreFormat, _pieceController.Score.BestScore.ToString("N0"));

        if (_ceramicController != null)
        {
            CeramicProgressData progress = _ceramicController.Ceramic.Progress;
            _ceramicProgressText.text = string.Format(Strings.GameOverCeramicProgressFormat, progress.CracksRepaired, progress.TotalCracks);
        }

        _milestoneText.gameObject.SetActive(false);
        if (_saveTriggers != null && _saveTriggers.LastMilestoneResults.Count > 0)
        {
            MilestoneManager.MilestoneResult milestone = _saveTriggers.LastMilestoneResults[_saveTriggers.LastMilestoneResults.Count - 1];
            _milestoneText.text = string.Format(Strings.GameOverMilestoneReachedFormat, milestone.MilestoneScore.ToString("N0"), milestone.CoinsAwarded);
            _milestoneText.gameObject.SetActive(true);
        }

        _continueButton.SetActive(_pieceController.CanContinue);
        _doubleCoinsButton.SetActive(true);

        _panel.SetActive(true);

        if (_saveTriggers != null && _saveTriggers.LastStreakResult.HasValue)
        {
            _streakPopup?.Show(_saveTriggers.LastStreakResult.Value);
        }
    }

    private void OnContinueClicked()
    {
        // Captured before the ad request — TryContinue (on success) resets
        // CurrentScore to 0 as part of starting the next portion of the
        // game, so "score at the moment continue was used" has to be read
        // before that happens, not from the callback.
        int scoreAtContinue = _pieceController.Score.CurrentScore;

        _rewardedAdController?.RequestContinue(succeeded =>
        {
            if (succeeded)
            {
                _panel.SetActive(false);
                AnalyticsManager.Instance?.LogEvent("continue_used", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "score_at_continue", scoreAtContinue }
                });
            }
            else
            {
                // Ad failed/unavailable — CLAUDE.md §5.1: "no reward
                // granted, player never blocked." The panel stays exactly
                // as it was; Play Again remains available.
                _continueButton.SetActive(_pieceController.CanContinue);
                _toast.Show(Strings.AdUnavailableToast);
            }
        });
    }

    private void OnDoubleCoinsClicked()
    {
        if (_doubleCoinsUsedThisGameOver || _saveTriggers == null)
        {
            return;
        }

        _rewardedAdController?.RequestDoubleCoins(_saveTriggers.LastGameOverCoinsAwarded, succeeded =>
        {
            if (succeeded)
            {
                _doubleCoinsUsedThisGameOver = true;
                _doubleCoinsButton.SetActive(false);
            }
            else
            {
                _toast.Show(Strings.AdUnavailableToast);
            }
        });
    }

    private void OnPlayAgainClicked()
    {
        ResolveIfNeeded();
        _panel.SetActive(false);
        _pieceController.RestartGame();
    }

    // Fires the interstitial cadence check exactly once per game-over,
    // the moment it's actually resolved (Play Again tapped without ever
    // continuing, or continue was offered and declined/failed) — not
    // when OnGameOver first fired, and not again if the player already
    // continued successfully (continuing hides the panel without ever
    // reaching Play Again, so this method simply never runs for that
    // outcome).
    private void ResolveIfNeeded()
    {
        if (_resolved || _interstitialController == null)
        {
            return;
        }

        _resolved = true;
        string todayIso = _nowProvider().ToString("yyyy-MM-dd");
        _interstitialController.RecordResolvedGameOver(todayIso);
    }
}
