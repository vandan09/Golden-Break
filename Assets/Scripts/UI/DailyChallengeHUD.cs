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
    private PieceController _pieceController;
    private SaveManager _saveManager;
    private Action _onMenuClicked;
    private Func<DateTime> _nowProvider;

    private Text _scoreValueText;
    private Text _ghostValueText;
    private Text _bestTodayValueText;

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

    public void RefreshGhostAndBestTexts()
    {
        DateTime today = _nowProvider();
        string todayIso = today.ToString("yyyy-MM-dd");

        int ghostScore = DailyChallengeManager.ComputeGhostScore(today);
        _ghostValueText.text = ghostScore.ToString("N0");

        if (_saveManager.Current.DailyBestScores.TryGetValue(todayIso, out int bestToday))
        {
            _bestTodayValueText.text = bestToday.ToString("N0");
        }
        else
        {
            _bestTodayValueText.text = "—";
        }
    }

    private void RefreshScoreText()
    {
        _scoreValueText.text = _pieceController.Score.CurrentScore.ToString("N0");
    }

    private void BuildUi()
    {
        Canvas canvas = ResponsiveCanvasSetup.BuildCanvas(transform, "DailyChallengeHUDCanvas", 5);
        RectTransform safeArea = ResponsiveCanvasSetup.BuildSafeArea(canvas.transform);

        var headerPanel = new GameObject("HeaderPanel");
        headerPanel.transform.SetParent(safeArea, false);
        var headerRect = headerPanel.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.offsetMin = new Vector2(20, -70);
        headerRect.offsetMax = new Vector2(-20, -22);

        // Score (left)
        var scoreGroup = new GameObject("ScoreGroup");
        scoreGroup.transform.SetParent(headerRect, false);
        var scoreGroupRect = scoreGroup.AddComponent<RectTransform>();
        scoreGroupRect.anchorMin = Vector2.zero;
        scoreGroupRect.anchorMax = new Vector2(0.33f, 1);
        scoreGroupRect.offsetMin = new Vector2(2, 0);
        scoreGroupRect.offsetMax = Vector2.zero;

        CreateLabel(scoreGroup.transform, "ScoreLabel", "SCORE", 11,
            FontStyle.Bold, UiPalette.TextSecondary, TextAnchor.UpperLeft,
            new Vector2(0, 0.55f), Vector2.one);
        _scoreValueText = CreateLabel(scoreGroup.transform, "ScoreValue", "0", 22,
            FontStyle.Bold, UiPalette.TextPrimary, TextAnchor.LowerLeft,
            Vector2.zero, new Vector2(1, 0.55f));

        // Ghost target (center)
        var ghostGroup = new GameObject("GhostGroup");
        ghostGroup.transform.SetParent(headerRect, false);
        var ghostGroupRect = ghostGroup.AddComponent<RectTransform>();
        ghostGroupRect.anchorMin = new Vector2(0.33f, 0);
        ghostGroupRect.anchorMax = new Vector2(0.66f, 1);
        ghostGroupRect.offsetMin = ghostGroupRect.offsetMax = Vector2.zero;

        CreateLabel(ghostGroup.transform, "GhostLabel", "TARGET", 11,
            FontStyle.Bold, UiPalette.TextSecondary, TextAnchor.UpperCenter,
            new Vector2(0, 0.55f), Vector2.one);
        _ghostValueText = CreateLabel(ghostGroup.transform, "GhostValue", "0", 16,
            FontStyle.Bold, UiPalette.GoldFill, TextAnchor.LowerCenter,
            Vector2.zero, new Vector2(1, 0.55f));

        // Best today (right)
        var bestGroup = new GameObject("BestGroup");
        bestGroup.transform.SetParent(headerRect, false);
        var bestGroupRect = bestGroup.AddComponent<RectTransform>();
        bestGroupRect.anchorMin = new Vector2(0.66f, 0);
        bestGroupRect.anchorMax = Vector2.one;
        bestGroupRect.offsetMin = Vector2.zero;
        bestGroupRect.offsetMax = new Vector2(-2, 0);

        CreateLabel(bestGroup.transform, "BestLabel", "BEST", 11,
            FontStyle.Bold, UiPalette.TextSecondary, TextAnchor.UpperRight,
            new Vector2(0, 0.55f), Vector2.one);
        _bestTodayValueText = CreateLabel(bestGroup.transform, "BestValue", "—", 16,
            FontStyle.Bold, UiPalette.TextSecondary, TextAnchor.LowerRight,
            Vector2.zero, new Vector2(1, 0.55f));

        // Menu button (circular, bottom-right)
        BuildMenuButton(safeArea);

        // Daily challenge label (italic hint at bottom)
        var hintLabel = CreateLabel(safeArea, "DailyLabel", "daily challenge", 12,
            FontStyle.Italic, UiPalette.TextSecondary, TextAnchor.MiddleCenter,
            new Vector2(0, 0.04f), new Vector2(1, 0.06f));
        hintLabel.rectTransform.offsetMin = hintLabel.rectTransform.offsetMax = Vector2.zero;
    }

    private void BuildMenuButton(Transform parent)
    {
        var btnObj = new GameObject("MenuButton");
        btnObj.transform.SetParent(parent, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1f, 0f);
        btnRect.anchorMax = new Vector2(1f, 0f);
        btnRect.pivot = new Vector2(1f, 0f);
        btnRect.anchoredPosition = new Vector2(-20f, 20f);
        btnRect.sizeDelta = new Vector2(34, 34);

        var bg = btnObj.AddComponent<Image>();
        bg.sprite = RoundedRectSprite.Get(64);
        bg.type = Image.Type.Sliced;
        bg.color = UiPalette.Surface;

        AddBorder(btnRect, 64, UiPalette.CardBorder, 4);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() => _onMenuClicked?.Invoke());

        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(btnObj.transform, false);
        var iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = ThreeDotsIconSprite.Get();
        iconImg.color = UiPalette.TextSecondary;
        var iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(14, 14);
    }

    private static Text CreateLabel(Transform parent, string name, string text, int fontSize,
        FontStyle style, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
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
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return t;
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
}
