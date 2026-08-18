using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Transient "Day N streak! +X coins" celebration (CLAUDE.md §4.1).
/// Purely a renderer — doesn't decide *when* to appear; the game-over
/// screen (which already has GameplaySaveTriggers.LastStreakResult at
/// hand right when it shows) calls Show(result) directly. Sits above
/// GameOverScreen's own canvas (sortingOrder 10) since it appears
/// alongside it, informational only — doesn't gate input itself.
/// </summary>
public sealed class StreakPopup : MonoBehaviour
{
    private const int MessageFontSize = 26;
    private const float VisibleSeconds = 2.2f;

    private GameObject _panel;
    private Text _messageText;

    public void Configure()
    {
        BuildUi();
        _panel.SetActive(false);
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("StreakPopupCanvas");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvasObject.transform, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = new Color(UiPalette.Surface.r, UiPalette.Surface.g, UiPalette.Surface.b, 0.95f);

        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.8f);
        panelRect.anchorMax = new Vector2(0.5f, 0.8f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(560f, 100f);

        _messageText = new GameObject("Message").AddComponent<Text>();
        _messageText.transform.SetParent(_panel.transform, false);
        _messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _messageText.fontSize = MessageFontSize;
        _messageText.alignment = TextAnchor.MiddleCenter;
        _messageText.color = UiPalette.GoldFill;

        var messageRect = _messageText.GetComponent<RectTransform>();
        messageRect.anchorMin = Vector2.zero;
        messageRect.anchorMax = Vector2.one;
        messageRect.offsetMin = new Vector2(16f, 8f);
        messageRect.offsetMax = new Vector2(-16f, -8f);
    }

    public void Show(StreakManager.StreakResult result)
    {
        string message = $"Day {result.StreakCount} streak! +{result.CoinsAwarded} coins";
        if (result.GalleryFrameUnlocked != null)
        {
            message += "\nNew gallery frame unlocked!";
        }

        _messageText.text = message;
        _panel.SetActive(true);
        _panel.transform.localScale = Vector3.one * 0.85f;

        DOTween.Sequence()
            .Append(_panel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
            .AppendInterval(VisibleSeconds)
            .OnComplete(() => _panel.SetActive(false));
    }
}
