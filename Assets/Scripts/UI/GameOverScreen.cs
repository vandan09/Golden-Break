using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game-over panel (BUILD_PLAN Phase 2 scope): final score, best score,
/// and a Play Again button that restarts in-place — no scene reload, so
/// the transition is sub-1-second per the acceptance criterion. The full
/// game-over flow (continue offer, double coins, ceramic progress) is
/// Phase 4 scope.
/// </summary>
public sealed class GameOverScreen : MonoBehaviour
{
    private const int TitleFontSize = 32;
    private const int FinalScoreFontSize = 48;
    private const int BestScoreFontSize = 22;
    private const int ButtonLabelFontSize = 26;

    private PieceController _pieceController;
    private GameObject _panel;
    private Text _finalScoreText;
    private Text _bestScoreText;

    public void Configure(PieceController pieceController)
    {
        _pieceController = pieceController;
        BuildUi();
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

        CreateText(_panel.transform, "GAME OVER", new Vector2(0.5f, 0.68f), TitleFontSize, UiPalette.TextPrimary);
        _finalScoreText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.6f), FinalScoreFontSize, UiPalette.GoldFill);
        _bestScoreText = CreateText(_panel.transform, string.Empty, new Vector2(0.5f, 0.53f), BestScoreFontSize, UiPalette.TextSecondary);

        BuildPlayAgainButton(_panel.transform);
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
        rect.sizeDelta = new Vector2(600f, 80f);

        return text;
    }

    private void BuildPlayAgainButton(Transform parent)
    {
        var buttonObject = new GameObject("PlayAgainButton");
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.AddComponent<Image>();
        image.color = UiPalette.Surface;
        var button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(OnPlayAgainClicked);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.35f);
        rect.anchorMax = new Vector2(0.5f, 0.35f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(280f, 70f);

        var label = CreateText(buttonObject.transform, "Play again", Vector2.zero, ButtonLabelFontSize, UiPalette.TextPrimary);
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void Show()
    {
        _finalScoreText.text = _pieceController.Score.CurrentScore.ToString("N0");
        _bestScoreText.text = $"Best {_pieceController.Score.BestScore:N0}";
        _panel.SetActive(true);
    }

    private void OnPlayAgainClicked()
    {
        _panel.SetActive(false);
        _pieceController.RestartGame();
    }
}
