using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Live score/best/streak display (CLAUDE.md §3.2 "always visible at the
/// top", §3.8 streak counter). Minimal uGUI on a Screen Space Overlay
/// canvas — layout/typography match nothing final yet (Phase 4/5), this
/// only needs to show correct, always-current numbers.
/// </summary>
public sealed class GameplayHUD : MonoBehaviour
{
    private const int ScoreFontSize = 42;
    private const int BestFontSize = 24;
    private const int StreakFontSize = 22;
    private const int NewBestFontSize = 30;
    private const float NewBestVisibleSeconds = 1.6f;

    private PieceController _pieceController;

    private Text _scoreText;
    private Text _bestText;
    private Text _streakText;
    private Text _newBestText;

    public void Configure(PieceController pieceController, SaveManager saveManager)
    {
        _pieceController = pieceController;

        BuildUi();

        _pieceController.Score.OnScoreChanged += _ => RefreshScoreTexts();
        _pieceController.Score.OnNewBest += ShowNewBestCelebration;
        _pieceController.OnLinesCleared += OnLinesCleared;

        RefreshScoreTexts();
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
        _streakText = CreateText(canvasObject.transform, "StreakText", new Vector2(0f, 1f), new Vector2(24f, -78f), StreakFontSize, TextAnchor.UpperLeft, UiPalette.GoldFill);
        _newBestText = CreateText(canvasObject.transform, "NewBestText", new Vector2(0.5f, 1f), new Vector2(0f, -130f), NewBestFontSize, TextAnchor.UpperCenter, UiPalette.GoldFill);

        _streakText.gameObject.SetActive(false);
        _newBestText.gameObject.SetActive(false);
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

    private void RefreshScoreTexts()
    {
        _scoreText.text = _pieceController.Score.CurrentScore.ToString("N0");
        _bestText.text = $"BEST {_pieceController.Score.BestScore:N0}";
    }

    private void OnLinesCleared(LineClearDetector.ClearResult result, int pointsAwarded)
    {
        RefreshScoreTexts();

        float multiplier = _pieceController.Score.StreakMultiplier;
        if (result.AnyCleared && multiplier > 1f)
        {
            _streakText.gameObject.SetActive(true);
            _streakText.text = $"×{multiplier:0.#} streak";
        }
        else
        {
            _streakText.gameObject.SetActive(false);
        }
    }

    private void ShowNewBestCelebration()
    {
        _newBestText.text = "New best!";
        _newBestText.gameObject.SetActive(true);
        _newBestText.transform.localScale = Vector3.one * 0.7f;

        DOTween.Sequence()
            .Append(_newBestText.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack))
            .AppendInterval(NewBestVisibleSeconds)
            .OnComplete(() => _newBestText.gameObject.SetActive(false));
    }
}
