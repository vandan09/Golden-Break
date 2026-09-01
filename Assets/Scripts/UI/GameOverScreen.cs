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
///
/// Layout is Claude Design UI rework Screen 4 (BUILD_PLAN.md), built the
/// same way as HomeScreen: exact mockup pixel values against its own
/// 390×844 reference frame via <see cref="ResponsiveCanvasSetup"/>.
/// </summary>
public sealed class GameOverScreen : MonoBehaviour
{
    private const float SidePadding = 28f;
    private const float TopPadding = 72f;
    private const float BottomPadding = 40f;

    private const int TitleFontSize = 13;
    private const int ScoreFontSize = 54;
    private const int BadgeFontSize = 13;
    private const int CeramicDetailFontSize = 12;
    private const int ContinueLabelFontSize = 16;
    private const int PlayAgainLabelFontSize = 15;
    private const int AdTagFontSize = 10;
    private const int DoubleCoinsFontSize = 13;

    private const float ContinueHeight = 58f;
    private const float ContinueRadius = 29f;
    private const float PlayAgainHeight = 50f;
    private const float PlayAgainRadius = 25f;
    private const float DoubleCoinsRowHeight = 20f;
    private const float ButtonGap = 12f;
    private const float DoubleCoinsTopGap = 16f;

    private const float CeramicIconWidth = 140f;
    private const float CeramicIconHeight = 105f;

    private static readonly Color BadgeBackground = new Color(232f / 255f, 192f / 255f, 96f / 255f, 0.14f);
    private static readonly Color BadgeBorder = new Color(232f / 255f, 192f / 255f, 96f / 255f, 0.4f);
    private static readonly Color AdTagBackground = new Color(122f / 255f, 122f / 255f, 154f / 255f, 0.2f);

    private PieceController _pieceController;
    private GameplaySaveTriggers _saveTriggers;
    private RewardedAdController _rewardedAdController;
    private InterstitialController _interstitialController;
    private StreakPopup _streakPopup;
    private CeramicController _ceramicController;
    private CeramicDefinition[] _ceramicPool;
    private Func<DateTime> _nowProvider;

    private GameObject _panel;
    private Text _finalScoreText;
    private GameObject _newBestBadge;
    private GameObject _newBestGlow;
    private Text _ceramicDetailText;
    private UiCeramicPreview _ceramicPreview;
    private RectTransform _progressFillRect;
    private GameObject _continueButton;
    private GameObject _doubleCoinsRow;
    private Text _doubleCoinsText;
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
        CeramicDefinition[] ceramicPool,
        Func<DateTime> nowProvider = null)
    {
        _pieceController = pieceController;
        _saveTriggers = saveTriggers;
        _rewardedAdController = rewardedAdController;
        _interstitialController = interstitialController;
        _streakPopup = streakPopup;
        _ceramicController = ceramicController;
        _ceramicPool = ceramicPool ?? Array.Empty<CeramicDefinition>();
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
        Canvas canvas = ResponsiveCanvasSetup.BuildCanvas(transform, "GameOverCanvas", sortingOrder: 10); // above GameplayHUD's default-order canvas

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvas.transform, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = new Color(UiPalette.Background.r, UiPalette.Background.g, UiPalette.Background.b, 0.96f);
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        RectTransform safeArea = ResponsiveCanvasSetup.BuildSafeArea(_panel.transform);

        BuildTopGroup(safeArea);
        BuildCeramicPreview(safeArea);
        BuildBottomGroup(safeArea);
    }

    private void BuildTopGroup(Transform parent)
    {
        float cursor = TopPadding;

        CreateTopAnchoredText(parent, "Title", Strings.GameOverTitle, cursor, 18f, TitleFontSize, UiPalette.TextSecondary, letterSpacing: true);
        cursor += 18f;

        _finalScoreText = CreateTopAnchoredText(parent, "Score", string.Empty, cursor, 60f, ScoreFontSize, UiPalette.TextPrimary);
        _finalScoreText.fontStyle = FontStyle.Bold;
        cursor += 60f + 12f;

        BuildNewBestBadge(parent, cursor);
    }

    private void BuildNewBestBadge(Transform parent, float topY)
    {
        _newBestBadge = new GameObject("NewBestBadge");
        var rect = _newBestBadge.AddComponent<RectTransform>();
        _newBestBadge.transform.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topY);
        rect.sizeDelta = new Vector2(120f, 30f);

        var image = _newBestBadge.AddComponent<Image>();
        image.sprite = RoundedRectSprite.Get(16);
        image.type = Image.Type.Sliced;
        image.color = BadgeBackground;

        AddBorderSprite(_newBestBadge.GetComponent<RectTransform>(), 16, BadgeBorder, 1);

        // Subtler than a button: the badge is small, and at full button alpha
        // the wash swamps the 120x30 pill it is meant to rim.
        //
        // Held as a field because the glow is a SIBLING of the badge, so the
        // badge being switched off for a non-record game would otherwise
        // leave its halo glowing over nothing.
        _newBestGlow = UiKit.AddGlowBehind(_newBestBadge.GetComponent<RectTransform>(), UiPalette.GoldFill, 16, 0.18f, 15).gameObject;

        var layout = _newBestBadge.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 6f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var diamondObject = new GameObject("Diamond");
        var diamondRect = diamondObject.AddComponent<RectTransform>();
        diamondObject.transform.SetParent(_newBestBadge.transform, false);
        diamondRect.sizeDelta = new Vector2(9f, 9f);
        diamondRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var diamondImage = diamondObject.AddComponent<Image>();
        diamondImage.color = UiPalette.GoldFill;
        var diamondLayoutElement = diamondObject.AddComponent<LayoutElement>();
        diamondLayoutElement.preferredWidth = 9f;
        diamondLayoutElement.preferredHeight = 9f;

        Text label = CreateAutoSizeText(_newBestBadge.transform, "New best!", BadgeFontSize, UiPalette.GoldFill);
        label.fontStyle = FontStyle.Bold;
    }

    private void BuildCeramicPreview(Transform parent)
    {
        var containerObject = new GameObject("CeramicPreview");
        var containerRect = containerObject.AddComponent<RectTransform>();
        containerObject.transform.SetParent(parent, false);
        containerRect.anchorMin = new Vector2(0.5f, 0.46f);
        containerRect.anchorMax = new Vector2(0.5f, 0.46f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(220f, 150f);

        var iconObject = new GameObject("Icon");
        var iconRect = iconObject.AddComponent<RectTransform>();
        iconObject.transform.SetParent(containerRect, false);
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(CeramicIconWidth, CeramicIconHeight);
        _ceramicPreview = iconObject.AddComponent<UiCeramicPreview>();
        _ceramicPreview.Configure(iconRect);

        var trackObject = new GameObject("ProgressTrack");
        var trackRect = trackObject.AddComponent<RectTransform>();
        trackObject.transform.SetParent(containerRect, false);
        trackRect.anchorMin = new Vector2(0.5f, 1f);
        trackRect.anchorMax = new Vector2(0.5f, 1f);
        trackRect.pivot = new Vector2(0.5f, 1f);
        trackRect.anchoredPosition = new Vector2(0f, -(CeramicIconHeight + 8f));
        trackRect.sizeDelta = new Vector2(170f, 6f);
        var trackImage = trackObject.AddComponent<Image>();
        trackImage.sprite = RoundedRectSprite.Get(3);
        trackImage.type = Image.Type.Sliced;
        trackImage.color = UiPalette.EmptyCellFill;

        var fillObject = new GameObject("ProgressFill");
        _progressFillRect = fillObject.AddComponent<RectTransform>();
        fillObject.transform.SetParent(trackRect, false);
        _progressFillRect.anchorMin = new Vector2(0f, 0f);
        _progressFillRect.anchorMax = new Vector2(0f, 1f);
        _progressFillRect.pivot = new Vector2(0f, 0.5f);
        _progressFillRect.anchoredPosition = Vector2.zero;
        _progressFillRect.sizeDelta = new Vector2(0f, 0f);
        var fillImage = fillObject.AddComponent<Image>();
        fillImage.sprite = RoundedRectSprite.Get(3);
        fillImage.type = Image.Type.Sliced;
        fillImage.color = UiPalette.GoldFill;

        _ceramicDetailText = CreateTopAnchoredText(containerRect, "CeramicDetail", string.Empty, CeramicIconHeight + 8f + 6f + 18f, 18f, CeramicDetailFontSize, UiPalette.TextSecondary);
    }

    private void BuildBottomGroup(Transform parent)
    {
        float cursor = BottomPadding;

        BuildDoubleCoinsRow(parent, cursor);
        cursor += DoubleCoinsRowHeight + DoubleCoinsTopGap;

        BuildPlayAgainButton(parent, cursor);
        cursor += PlayAgainHeight + ButtonGap;

        BuildContinueButton(parent, cursor);
    }

    private void BuildContinueButton(Transform parent, float bottomY)
    {
        Button button = UiKit.BuildButton(
            parent,
            "ContinueButton",
            Strings.GameOverContinueButton,
            OnContinueClicked,
            UiKit.ButtonStyle.Primary,
            ContinueHeight,
            TriangleSprite.Get(),
            new Vector2(12f, 16f));

        _continueButton = button.gameObject;
        UiKit.AnchorBottomStretch(_continueButton.GetComponent<RectTransform>(), bottomY, ContinueHeight, SidePadding);
        UiKit.AddPrimaryGlow(button, ContinueHeight);

        var adTagObject = new GameObject("AdTag");
        var adTagRect = adTagObject.AddComponent<RectTransform>();
        adTagObject.transform.SetParent(_continueButton.transform, false);
        adTagRect.anchorMin = new Vector2(1f, 0.5f);
        adTagRect.anchorMax = new Vector2(1f, 0.5f);
        adTagRect.pivot = new Vector2(1f, 0.5f);
        adTagRect.anchoredPosition = new Vector2(-14f, 0f);
        adTagRect.sizeDelta = new Vector2(32f, 20f);
        var adTagImage = adTagObject.AddComponent<Image>();
        adTagImage.sprite = RoundedRectSprite.Get(8);
        adTagImage.type = Image.Type.Sliced;
        adTagImage.color = AdTagBackground;

        Text adTagLabel = CreateAutoSizeText(adTagObject.transform, "AD", AdTagFontSize, UiPalette.TextSecondary);
        adTagLabel.fontStyle = FontStyle.Bold;
        var adTagLabelRect = adTagLabel.GetComponent<RectTransform>();
        adTagLabelRect.anchorMin = Vector2.zero;
        adTagLabelRect.anchorMax = Vector2.one;
        adTagLabelRect.offsetMin = Vector2.zero;
        adTagLabelRect.offsetMax = Vector2.zero;
    }

    private void BuildPlayAgainButton(Transform parent, float bottomY)
    {
        Button button = UiKit.BuildButton(
            parent,
            "PlayAgainButton",
            Strings.GameOverPlayAgainButton,
            OnPlayAgainClicked,
            UiKit.ButtonStyle.Secondary,
            PlayAgainHeight);

        UiKit.AnchorBottomStretch(button.GetComponent<RectTransform>(), bottomY, PlayAgainHeight, SidePadding);
    }

    private void BuildDoubleCoinsRow(Transform parent, float bottomY)
    {
        _doubleCoinsRow = new GameObject("DoubleCoinsRow");
        var rect = _doubleCoinsRow.AddComponent<RectTransform>();
        _doubleCoinsRow.transform.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottomY);
        rect.sizeDelta = new Vector2(180f, DoubleCoinsRowHeight);

        var button = _doubleCoinsRow.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OnDoubleCoinsClicked);

        var layout = _doubleCoinsRow.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 6f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var dotObject = new GameObject("Dot");
        var dotRect = dotObject.AddComponent<RectTransform>();
        dotObject.transform.SetParent(_doubleCoinsRow.transform, false);
        dotRect.sizeDelta = new Vector2(14f, 14f);
        var dotImage = dotObject.AddComponent<Image>();
        dotImage.sprite = RoundedRectSprite.Get(7);
        dotImage.color = UiPalette.GoldFill;
        var dotLayoutElement = dotObject.AddComponent<LayoutElement>();
        dotLayoutElement.preferredWidth = 14f;
        dotLayoutElement.preferredHeight = 14f;

        _doubleCoinsText = CreateAutoSizeText(_doubleCoinsRow.transform, Strings.GameOverDoubleCoinsButton, DoubleCoinsFontSize, UiPalette.GoldFill);
        _doubleCoinsText.fontStyle = FontStyle.Bold;
    }

    private static void AddBorder(RectTransform target, Color colour, int strokeWidth = 1)
    {
        AddBorderSprite(target, (int)ContinueRadius, colour, strokeWidth);
    }

    private static void AddBorderSprite(RectTransform target, int cornerRadiusPixels, Color colour, int strokeWidth = 1)
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

    private static Text CreateTopAnchoredText(Transform parent, string name, string initialText, float topY, float height, int fontSize, Color colour, bool letterSpacing = false)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = colour;
        // Legacy uGUI Text has no real letter-spacing property — a thin
        // space (U+2009) between characters approximates the mockup's
        // subtle 2px tracking without the huge gaps a plain space (U+0020)
        // produces (confirmed too wide via a rendered screenshot).
        text.text = letterSpacing ? string.Join(" ", initialText.ToCharArray()) : initialText;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(SidePadding, -(topY + height));
        rect.offsetMax = new Vector2(-SidePadding, -topY);

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

    private void Show()
    {
        _resolved = false;
        _doubleCoinsUsedThisGameOver = false;

        int currentScore = _pieceController.Score.CurrentScore;
        int bestScore = _pieceController.Score.BestScore;
        _finalScoreText.text = currentScore.ToString("N0");
        bool isNewBest = currentScore >= bestScore && currentScore > 0;
        _newBestBadge.SetActive(isNewBest);
        _newBestGlow.SetActive(isNewBest);

        if (_ceramicController != null)
        {
            CeramicProgressData progress = _ceramicController.Ceramic.Progress;
            CeramicDefinition definition = FindCeramicDefinition(progress.Tier);
            string displayName = definition != null ? definition.displayName : "Ceramic";
            _ceramicDetailText.text = $"{displayName} · {progress.CracksRepaired}/{progress.TotalCracks}";
            _ceramicPreview.SetCeramic(definition, progress.CracksRepaired);

            float fraction = progress.TotalCracks > 0 ? (float)progress.CracksRepaired / progress.TotalCracks : 0f;
            _progressFillRect.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        _continueButton.SetActive(_pieceController.CanContinue);
        _doubleCoinsRow.SetActive(true);

        _panel.SetActive(true);
        UiKit.PlayOverlayShow(_panel);

        if (_saveTriggers != null && _saveTriggers.LastStreakResult.HasValue)
        {
            _streakPopup?.Show(_saveTriggers.LastStreakResult.Value);
        }

        // The mockup has no dedicated milestone element (its Game Over
        // screen only shows score/badge/ceramic/buttons) — routed through
        // the existing toast system instead of a static banner, rather
        // than silently dropping this functionality to match the mockup
        // literally.
        if (_saveTriggers != null && _saveTriggers.LastMilestoneResults.Count > 0)
        {
            MilestoneManager.MilestoneResult milestone = _saveTriggers.LastMilestoneResults[_saveTriggers.LastMilestoneResults.Count - 1];
            _toast.Show(string.Format(Strings.GameOverMilestoneReachedFormat, milestone.MilestoneScore.ToString("N0"), milestone.CoinsAwarded));
        }
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

    private void OnContinueClicked()
    {
        // Captured before the ad request so the analytics event reports the
        // score the player actually chose to save, independent of anything
        // that happens while the ad is on screen.
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
                _doubleCoinsRow.SetActive(false);
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
