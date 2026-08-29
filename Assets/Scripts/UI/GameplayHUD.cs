using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gameplay HUD rebuilt to match the Claude Design mockup's Screen 2
/// (Gameplay) pixel-for-pixel: score/best header, ceramic preview with
/// progress bar, small circular undo/refresh icon buttons, streak display,
/// and new-best celebration. Uses <see cref="ResponsiveCanvasSetup"/> for
/// proper scaling on all devices.
/// </summary>
public sealed class GameplayHUD : MonoBehaviour
{
    private const float NewBestVisibleSeconds = 1.6f;
    private const int HudSortingOrder = 5;

    private PieceController _pieceController;
    private CoinManager _coinManager;
    private RewardedAdController _rewardedAdController;

    private Text _scoreLabelText;
    private Text _scoreValueText;
    private Text _bestLabelText;
    private Text _bestValueText;
    private Text _streakText;
    private Text _newBestText;
    private Button _undoButton;
    private Button _refreshButton;
    private Image _undoButtonBg;
    private Image _refreshButtonBg;
    private Text _scorePopupText;
    private Text _progressLabelText;
    private ToastMessage _toast;
    private HomeScreen _homeScreen;

    public void Configure(PieceController pieceController, SaveManager saveManager, CoinManager coinManager = null, RewardedAdController rewardedAdController = null)
    {
        _pieceController = pieceController;
        _coinManager = coinManager;
        _rewardedAdController = rewardedAdController;

        BuildUi();

        var toastObject = new GameObject("Toast");
        toastObject.transform.SetParent(transform, false);
        _toast = toastObject.AddComponent<ToastMessage>();
        _toast.Configure(sortingOrder: HudSortingOrder + 1);

        _pieceController.Score.OnScoreChanged += _ => RefreshScoreTexts();
        _pieceController.Score.OnNewBest += ShowNewBestCelebration;
        _pieceController.OnLinesCleared += OnLinesCleared;
        if (_coinManager != null)
        {
            _coinManager.OnBalanceChanged += _ => RefreshActionButtonInteractable();
        }

        RefreshScoreTexts();
        RefreshActionButtonInteractable();
    }

    public void SetHomeScreen(HomeScreen homeScreen)
    {
        _homeScreen = homeScreen;
    }

    private void Update()
    {
        RefreshActionButtonInteractable();
    }

    private void BuildUi()
    {
        Canvas canvas = ResponsiveCanvasSetup.BuildCanvas(transform, "HUDCanvas", HudSortingOrder);
        RectTransform safeArea = ResponsiveCanvasSetup.BuildSafeArea(canvas.transform);

        // Full-screen transparent panel for layout
        var panel = CreatePanel(safeArea, "HUDPanel");
        var panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.padding = new RectOffset(20, 20, 22, 24);
        panelLayout.spacing = 0;

        // -- Score / Best header row --
        BuildScoreHeader(panel);

        // -- Streak text (shown/hidden dynamically) --
        BuildStreakDisplay(panel);

        // -- New best celebration text --
        BuildNewBestDisplay(panel);

        // -- Progress label below ceramic area --
        BuildProgressLabel(safeArea);

        // -- Undo/Refresh buttons (right-aligned, below grid) --
        BuildActionButtons(safeArea);

        // -- Hint text anchored at bottom of safe area --
        BuildHintText(safeArea);

        // -- Score popup (centered, above layout) --
        BuildScorePopup(safeArea);
    }

    private void BuildScoreHeader(RectTransform parent)
    {
        var row = CreateLayoutChild(parent, "ScoreRow", 48);

        // Score (left)
        var scoreGroup = new GameObject("ScoreGroup");
        scoreGroup.transform.SetParent(row, false);
        var scoreGroupRect = scoreGroup.AddComponent<RectTransform>();
        scoreGroupRect.anchorMin = new Vector2(0, 0);
        scoreGroupRect.anchorMax = new Vector2(0.5f, 1);
        scoreGroupRect.offsetMin = new Vector2(2, 0);
        scoreGroupRect.offsetMax = Vector2.zero;

        _scoreLabelText = CreateLabel(scoreGroup.transform, "ScoreLabel", "SCORE", 11,
            FontStyle.Bold, UiPalette.TextSecondary, TextAnchor.LowerLeft);
        var slRect = _scoreLabelText.rectTransform;
        slRect.anchorMin = new Vector2(0, 0.55f);
        slRect.anchorMax = new Vector2(1, 1);
        slRect.offsetMin = slRect.offsetMax = Vector2.zero;

        _scoreValueText = CreateLabel(scoreGroup.transform, "ScoreValue", "0", 22,
            FontStyle.Bold, UiPalette.TextPrimary, TextAnchor.UpperLeft);
        var svRect = _scoreValueText.rectTransform;
        svRect.anchorMin = Vector2.zero;
        svRect.anchorMax = new Vector2(1, 0.55f);
        svRect.offsetMin = svRect.offsetMax = Vector2.zero;

        // Best (right)
        var bestGroup = new GameObject("BestGroup");
        bestGroup.transform.SetParent(row, false);
        var bestGroupRect = bestGroup.AddComponent<RectTransform>();
        bestGroupRect.anchorMin = new Vector2(0.5f, 0);
        bestGroupRect.anchorMax = Vector2.one;
        bestGroupRect.offsetMin = Vector2.zero;
        bestGroupRect.offsetMax = new Vector2(-2, 0);

        _bestLabelText = CreateLabel(bestGroup.transform, "BestLabel", "BEST", 11,
            FontStyle.Bold, UiPalette.TextSecondary, TextAnchor.LowerRight);
        var blRect = _bestLabelText.rectTransform;
        blRect.anchorMin = new Vector2(0, 0.55f);
        blRect.anchorMax = Vector2.one;
        blRect.offsetMin = blRect.offsetMax = Vector2.zero;

        _bestValueText = CreateLabel(bestGroup.transform, "BestValue", "0", 16,
            FontStyle.Bold, UiPalette.TextSecondary, TextAnchor.UpperRight);
        var bvRect = _bestValueText.rectTransform;
        bvRect.anchorMin = Vector2.zero;
        bvRect.anchorMax = new Vector2(1, 0.55f);
        bvRect.offsetMin = bvRect.offsetMax = Vector2.zero;
    }

    private void BuildStreakDisplay(RectTransform parent)
    {
        var streakObj = new GameObject("StreakText");
        streakObj.transform.SetParent(parent, false);
        _streakText = streakObj.AddComponent<Text>();
        _streakText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _streakText.fontSize = 13;
        _streakText.fontStyle = FontStyle.Bold;
        _streakText.alignment = TextAnchor.MiddleLeft;
        _streakText.color = UiPalette.GoldFill;
        _streakText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var le = streakObj.AddComponent<LayoutElement>();
        le.preferredHeight = 20;
        _streakText.gameObject.SetActive(false);
    }

    private void BuildNewBestDisplay(RectTransform parent)
    {
        var nbObj = new GameObject("NewBestText");
        nbObj.transform.SetParent(parent, false);
        _newBestText = nbObj.AddComponent<Text>();
        _newBestText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _newBestText.fontSize = 26;
        _newBestText.fontStyle = FontStyle.Bold;
        _newBestText.alignment = TextAnchor.MiddleCenter;
        _newBestText.color = UiPalette.GoldFill;
        _newBestText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var le = nbObj.AddComponent<LayoutElement>();
        le.preferredHeight = 36;
        _newBestText.gameObject.SetActive(false);
    }

    private void BuildActionButtons(RectTransform parent)
    {
        var rowObj = new GameObject("ActionRow");
        rowObj.transform.SetParent(parent, false);
        var row = rowObj.AddComponent<RectTransform>();
        row.anchorMin = new Vector2(0.14f, 0.11f);
        row.anchorMax = new Vector2(0.86f, 0.15f);
        row.offsetMin = Vector2.zero;
        row.offsetMax = Vector2.zero;

        var layout = rowObj.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 8;

        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(row, false);
        var spacerLE = spacer.AddComponent<LayoutElement>();
        spacerLE.flexibleWidth = 1;

        (_undoButton, _undoButtonBg) = BuildSmallIconButton(row, "UndoBtn", UndoIconSprite.Get(), OnUndoClicked, UiPalette.TextSecondary);
        (_refreshButton, _refreshButtonBg) = BuildSmallIconButton(row, "RefreshBtn", RefreshIconSprite.Get(), OnRefreshClicked, UiPalette.GoldFill);
    }

    private void BuildMenuButton(RectTransform parent)
    {
        var btnObj = new GameObject("MenuBtn");
        btnObj.transform.SetParent(parent, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 1f);
        btnRect.anchorMax = new Vector2(1f, 1f);
        btnRect.pivot = new Vector2(1f, 1f);
        btnRect.anchoredPosition = new Vector2(-8f, -6f);
        btnRect.sizeDelta = new Vector2(24, 24);

        var bg = btnObj.AddComponent<Image>();
        bg.sprite = RoundedRectSprite.Get(64);
        bg.type = Image.Type.Sliced;
        bg.color = UiPalette.Surface;

        var button = btnObj.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(OnMenuClicked);

        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        var iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = ThreeDotsIconSprite.Get();
        iconImg.color = UiPalette.TextSecondary;
        var iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(12, 12);
    }

    private void BuildProgressLabel(RectTransform parent)
    {
        _progressLabelText = CreateLabel(parent, "ProgressLabel", "", 12,
            FontStyle.Normal, UiPalette.TextSecondary, TextAnchor.MiddleCenter);
        var rect = _progressLabelText.rectTransform;
        rect.anchorMin = new Vector2(0.15f, 0.67f);
        rect.anchorMax = new Vector2(0.85f, 0.695f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private (Button, Image) BuildSmallIconButton(RectTransform parent, string name, Sprite icon, UnityEngine.Events.UnityAction onClick, Color iconColor, string textFallback = null, bool isTextButton = false)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        var bg = btnObj.AddComponent<Image>();
        bg.sprite = RoundedRectSprite.Get(64);
        bg.type = Image.Type.Sliced;
        bg.color = UiPalette.Surface;

        var border = new GameObject("Border");
        border.transform.SetParent(btnObj.transform, false);
        var borderImg = border.AddComponent<Image>();
        borderImg.sprite = RoundedRectBorderSprite.Get(64, 1);
        borderImg.type = Image.Type.Sliced;
        borderImg.color = UiPalette.CardBorder;
        var borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = borderRect.offsetMax = Vector2.zero;

        var button = btnObj.AddComponent<Button>();
        button.targetGraphic = bg;
        button.onClick.AddListener(onClick);

        var le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = 34;
        le.preferredHeight = 34;

        var btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(34, 34);

        if (isTextButton && textFallback != null)
        {
            var label = CreateLabel(btnObj.transform, "Label", textFallback, 16,
                FontStyle.Bold, iconColor, TextAnchor.MiddleCenter);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        }
        else if (icon != null)
        {
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = icon;
            iconImg.color = iconColor;
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(20, 20);
        }

        return (button, bg);
    }

    private void BuildHintText(RectTransform parent)
    {
        var hint = CreateLabel(parent, "HintText", "drag a piece onto the grid", 12,
            FontStyle.Italic, UiPalette.TextSecondary, TextAnchor.MiddleCenter);
        var rect = hint.rectTransform;
        rect.anchorMin = new Vector2(0, 0.04f);
        rect.anchorMax = new Vector2(1, 0.06f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void RefreshScoreTexts()
    {
        _scoreValueText.text = _pieceController.Score.CurrentScore.ToString("N0");
        _bestValueText.text = _pieceController.Score.BestScore.ToString("N0");
    }

    public void SetCeramicInfo(CeramicManager ceramicManager, CeramicDefinition[] ceramicPool)
    {
        if (ceramicManager == null || _progressLabelText == null) return;
        UpdateProgressLabelFromCeramic(ceramicManager, ceramicPool);
        _pieceController.OnLinesCleared += (_, __) => UpdateProgressLabelFromCeramic(ceramicManager, ceramicPool);
    }

    private void UpdateProgressLabelFromCeramic(CeramicManager ceramicManager, CeramicDefinition[] ceramicPool)
    {
        var progress = ceramicManager.Progress;
        string name = "Ceramic";
        int defTier = CeramicTierResolver.ResolveDefinitionTier(progress.Tier);
        foreach (var def in ceramicPool)
        {
            if (def.tier == defTier)
            {
                name = def.displayName;
                break;
            }
        }
        _progressLabelText.text = $"{progress.CracksRepaired} of {progress.TotalCracks} repaired";
    }

    public void RefreshActionButtonInteractable()
    {
        if (_undoButton != null)
        {
            _undoButton.interactable = _pieceController.CanUndo;
            _undoButtonBg.color = _pieceController.CanUndo ? UiPalette.Surface : new Color(0.12f, 0.12f, 0.2f, 1f);
        }

        if (_refreshButton != null)
        {
            _refreshButton.interactable = _pieceController.CanRefresh;
            _refreshButtonBg.color = _pieceController.CanRefresh ? UiPalette.Surface : new Color(0.12f, 0.12f, 0.2f, 1f);
        }
    }

    private void OnMenuClicked()
    {
        _homeScreen?.Show();
    }

    // CLAUDE.md §5.1 prices undo at 50 coins and refresh at 75, with a
    // rewarded ad as the alternative to paying. The spend must gate the
    // action: these once called TrySpend and ran regardless of the result,
    // which made both free. Affording it spends coins; not affording it
    // offers the ad.
    private void OnUndoClicked()
    {
        if (!_pieceController.CanUndo) return;

        if (CanAfford(Constants.UndoCostCoins))
        {
            _coinManager?.TrySpend(Constants.UndoCostCoins);
            LogUndoOrRefreshUsed("undo_used", "coins");
            _pieceController.TryUndo();
            return;
        }

        OfferRewardedFallback(
            Constants.UndoCostCoins,
            onWatch => _rewardedAdController.RequestFreeUndo(onWatch),
            "undo_used");
    }

    private void OnRefreshClicked()
    {
        if (!_pieceController.CanRefresh) return;

        if (CanAfford(Constants.RefreshCostCoins))
        {
            _coinManager?.TrySpend(Constants.RefreshCostCoins);
            LogUndoOrRefreshUsed("refresh_used", "coins");
            _pieceController.TryRefresh();
            return;
        }

        OfferRewardedFallback(
            Constants.RefreshCostCoins,
            onWatch => _rewardedAdController.RequestFreeRefresh(onWatch),
            "refresh_used");
    }

    private bool CanAfford(int cost)
    {
        return _coinManager == null || _coinManager.CanAfford(cost);
    }

    // CLAUDE.md §5.1 offers a rewarded ad as the alternative to paying
    // coins for undo and refresh. That path existed in RewardedAdController
    // from the start but was never called from here — the button simply
    // reported the shortfall — because AdManager was a stub at the time.
    // With the real SDK wired, a player out of coins now gets the ad offer
    // the design promises instead of a dead button.
    //
    // §5.1's failure rule still holds: if the ad does not load or the
    // player abandons it, nothing is granted and they are told why rather
    // than being blocked.
    private void OfferRewardedFallback(int cost, System.Action<System.Action<bool>> request, string analyticsEvent)
    {
        if (_rewardedAdController == null)
        {
            _toast?.Show(string.Format(Strings.NotEnoughCoinsFormat, cost));
            return;
        }

        request(granted =>
        {
            if (granted)
            {
                LogUndoOrRefreshUsed(analyticsEvent, "ad");
                RefreshActionButtonInteractable();
                return;
            }

            _toast?.Show(Strings.AdUnavailableToast);
        });
    }

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

        if (result.AnyCleared && pointsAwarded > 0)
        {
            ShowScorePopup(pointsAwarded);
        }

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

    private void BuildScorePopup(RectTransform parent)
    {
        var obj = new GameObject("ScorePopup");
        obj.transform.SetParent(parent, false);
        _scorePopupText = obj.AddComponent<Text>();
        _scorePopupText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _scorePopupText.fontSize = 26;
        _scorePopupText.fontStyle = FontStyle.Bold;
        _scorePopupText.alignment = TextAnchor.MiddleCenter;
        _scorePopupText.color = UiPalette.GoldFill;
        _scorePopupText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rect = _scorePopupText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(200, 40);
        obj.SetActive(false);
    }

    private void ShowScorePopup(int points)
    {
        if (points <= 0) return;
        _scorePopupText.text = $"+{points}";
        _scorePopupText.gameObject.SetActive(true);
        _scorePopupText.color = UiPalette.GoldFill;
        _scorePopupText.transform.localScale = Vector3.one * 0.7f;

        DOTween.Sequence()
            .Append(_scorePopupText.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
            .Join(DOTween.ToAlpha(
                () => _scorePopupText.color, c => _scorePopupText.color = c,
                1f, 0.1f))
            .AppendInterval(0.6f)
            .Append(DOTween.ToAlpha(
                () => _scorePopupText.color, c => _scorePopupText.color = c,
                0f, 0.3f))
            .OnComplete(() => _scorePopupText.gameObject.SetActive(false));
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

    // -- Helpers --

    private static RectTransform CreatePanel(Transform parent, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static RectTransform CreateLayoutChild(Transform parent, string name, float height)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        return rect;
    }

    private static Text CreateLabel(Transform parent, string name, string text, int fontSize, FontStyle style, Color color, TextAnchor alignment)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var t = obj.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = alignment;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
