using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Live score/best/streak/coins HUD (CLAUDE.md §3.2 "always visible at
/// the top", §3.8 streak counter) plus the undo/refresh buttons (§4.5,
/// §5.1: "During gameplay (button always visible)"). Minimal uGUI on a
/// Screen Space Overlay canvas — layout/typography match nothing final
/// yet (Phase 4/5), this only needs to show correct, always-current
/// numbers and working buttons.
///
/// Each button tries the coin-cost path first (CoinManager.TrySpend) and
/// falls back to the rewarded-ad path only when coins are insufficient —
/// §4.5 offers both, and auto-falling-back avoids needing a second
/// "or watch an ad instead" dialog for what's structurally one action.
/// </summary>
public sealed class GameplayHUD : MonoBehaviour
{
    private const int ScoreFontSize = 42;
    private const int BestFontSize = 24;
    private const int CoinsFontSize = 24;
    private const int StreakFontSize = 22;
    private const int NewBestFontSize = 30;
    private const int ActionButtonLabelFontSize = 16;
    private const float NewBestVisibleSeconds = 1.6f;

    private PieceController _pieceController;
    private CoinManager _coinManager;
    private RewardedAdController _rewardedAdController;

    private Text _scoreText;
    private Text _bestText;
    private Text _coinsText;
    private Text _streakText;
    private Text _newBestText;
    private Button _undoButton;
    private Text _undoButtonLabel;
    private Button _refreshButton;
    private Text _refreshButtonLabel;

    public void Configure(PieceController pieceController, SaveManager saveManager, CoinManager coinManager = null, RewardedAdController rewardedAdController = null)
    {
        _pieceController = pieceController;
        _coinManager = coinManager;
        _rewardedAdController = rewardedAdController;

        BuildUi();

        _pieceController.Score.OnScoreChanged += _ => RefreshScoreTexts();
        _pieceController.Score.OnNewBest += ShowNewBestCelebration;
        _pieceController.OnLinesCleared += OnLinesCleared;
        if (_coinManager != null)
        {
            _coinManager.OnBalanceChanged += _ => RefreshCoinsTextAndButtons();
        }

        RefreshScoreTexts();
        RefreshCoinsTextAndButtons();
    }

    private void Update()
    {
        // CanUndo/CanRefresh depend on drag state, hand-placement state,
        // and game-over state that can change from several different
        // call sites (EndDrag, DealNewHand, TryContinue, RestartGame) —
        // polling two cheap booleans here is simpler and more robust than
        // trying to hook every single one, the same narrow exception
        // InputHandler's own doc comment already carves out of "no logic
        // in Update() that could be event-driven."
        RefreshActionButtonInteractable();
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("HUDCanvas");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _scoreText = CreateText(canvasObject.transform, "ScoreText", new Vector2(0f, 1f), new Vector2(24f, -24f), ScoreFontSize, TextAnchor.UpperLeft, UiPalette.TextPrimary);
        _bestText = CreateText(canvasObject.transform, "BestText", new Vector2(1f, 1f), new Vector2(-24f, -24f), BestFontSize, TextAnchor.UpperRight, UiPalette.TextSecondary);
        _coinsText = CreateText(canvasObject.transform, "CoinsText", new Vector2(1f, 1f), new Vector2(-24f, -52f), CoinsFontSize, TextAnchor.UpperRight, UiPalette.GoldFill);
        _streakText = CreateText(canvasObject.transform, "StreakText", new Vector2(0f, 1f), new Vector2(24f, -78f), StreakFontSize, TextAnchor.UpperLeft, UiPalette.GoldFill);
        _newBestText = CreateText(canvasObject.transform, "NewBestText", new Vector2(0.5f, 1f), new Vector2(0f, -130f), NewBestFontSize, TextAnchor.UpperCenter, UiPalette.GoldFill);

        _streakText.gameObject.SetActive(false);
        _newBestText.gameObject.SetActive(false);

        (_undoButton, _undoButtonLabel) = BuildActionButton(canvasObject.transform, "UndoButton", new Vector2(0f, 0f), new Vector2(24f, 24f), OnUndoClicked);
        (_refreshButton, _refreshButtonLabel) = BuildActionButton(canvasObject.transform, "RefreshButton", new Vector2(0f, 0f), new Vector2(24f + 160f + 12f, 24f), OnRefreshClicked);
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

    private (Button, Text) BuildActionButton(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        buttonObject.AddComponent<Image>().color = UiPalette.Surface;
        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(160f, 56f);

        var label = CreateText(buttonObject.transform, "Label", Vector2.zero, Vector2.zero, ActionButtonLabelFontSize, TextAnchor.MiddleCenter, UiPalette.TextPrimary);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return (button, label);
    }

    private void RefreshScoreTexts()
    {
        _scoreText.text = _pieceController.Score.CurrentScore.ToString("N0");
        _bestText.text = string.Format(Strings.HudBestFormat, _pieceController.Score.BestScore.ToString("N0"));
    }

    private void RefreshCoinsTextAndButtons()
    {
        if (_coinManager != null)
        {
            _coinsText.text = string.Format(Strings.HudCoinsFormat, _coinManager.Balance.ToString("N0"));
        }

        RefreshActionButtonInteractable();
    }

    private void RefreshActionButtonInteractable()
    {
        _undoButton.interactable = _pieceController.CanUndo;
        _undoButtonLabel.text = string.Format(Strings.HudUndoButtonFormat, Constants.UndoCostCoins);

        _refreshButton.interactable = _pieceController.CanRefresh;
        _refreshButtonLabel.text = string.Format(Strings.HudRefreshButtonFormat, Constants.RefreshCostCoins);
    }

    private void OnUndoClicked()
    {
        if (!_pieceController.CanUndo)
        {
            return;
        }

        if (_coinManager != null && _coinManager.TrySpend(Constants.UndoCostCoins))
        {
            _pieceController.TryUndo();
            LogUndoOrRefreshUsed("undo_used", "coins");
        }
        else
        {
            _rewardedAdController?.RequestFreeUndo(succeeded =>
            {
                if (succeeded)
                {
                    LogUndoOrRefreshUsed("undo_used", "rewarded");
                }
            });
        }
    }

    private void OnRefreshClicked()
    {
        if (!_pieceController.CanRefresh)
        {
            return;
        }

        if (_coinManager != null && _coinManager.TrySpend(Constants.RefreshCostCoins))
        {
            _pieceController.TryRefresh();
            LogUndoOrRefreshUsed("refresh_used", "coins");
        }
        else
        {
            _rewardedAdController?.RequestFreeRefresh(succeeded =>
            {
                if (succeeded)
                {
                    LogUndoOrRefreshUsed("refresh_used", "rewarded");
                }
            });
        }
    }

    // Matches CLAUDE.md §6.2's exact param lists: undo_used gets
    // source + score_at_undo; refresh_used gets source only.
    private void LogUndoOrRefreshUsed(string eventName, string source)
    {
        var parameters = new System.Collections.Generic.Dictionary<string, object> { { "source", source } };
        if (eventName == "undo_used")
        {
            parameters["score_at_undo"] = _pieceController.Score.CurrentScore;
        }

        AnalyticsManager.Instance?.LogEvent(eventName, parameters);
    }

    private void OnLinesCleared(LineClearDetector.ClearResult result, int pointsAwarded)
    {
        RefreshScoreTexts();

        float multiplier = _pieceController.Score.StreakMultiplier;
        if (result.AnyCleared && multiplier > 1f)
        {
            _streakText.gameObject.SetActive(true);
            _streakText.text = string.Format(Strings.HudStreakMultiplierFormat, multiplier.ToString("0.#"));
        }
        else
        {
            _streakText.gameObject.SetActive(false);
        }
    }

    private void ShowNewBestCelebration()
    {
        _newBestText.text = Strings.HudNewBest;
        _newBestText.gameObject.SetActive(true);
        _newBestText.transform.localScale = Vector3.one * 0.7f;

        DOTween.Sequence()
            .Append(_newBestText.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack))
            .AppendInterval(NewBestVisibleSeconds)
            .OnComplete(() => _newBestText.gameObject.SetActive(false));
    }
}
