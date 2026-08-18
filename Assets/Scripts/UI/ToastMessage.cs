using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small reusable transient text notification. CLAUDE.md §5.1's failure
/// handling requirement is "toast 'Ad unavailable,' no reward granted,
/// player never blocked" — every rewarded-ad call site already correctly
/// does the no-reward/never-blocked half; this is the missing visible
/// half, caught on-device: with 0 starting coins, every undo/refresh tap
/// falls through to the (currently always-failing, no real SDK yet)
/// rewarded-ad path and silently did nothing.
///
/// Not a singleton — each screen that needs one (GameplayHUD,
/// GameOverScreen) builds and owns its own instance, avoiding a shared-
/// state dependency between otherwise-independent screens.
/// </summary>
public sealed class ToastMessage : MonoBehaviour
{
    private const int FontSize = 22;
    private const float VisibleSeconds = 1.6f;

    private GameObject _panel;
    private Text _text;
    private Sequence _activeSequence;

    public void Configure(int sortingOrder)
    {
        BuildUi(sortingOrder);
        _panel.SetActive(false);
    }

    private void BuildUi(int sortingOrder)
    {
        var canvasObject = new GameObject("ToastCanvas");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvasObject.transform, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.85f);
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.12f);
        panelRect.anchorMax = new Vector2(0.5f, 0.12f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(440f, 70f);

        var textObject = new GameObject("Text");
        textObject.transform.SetParent(_panel.transform, false);
        _text = textObject.AddComponent<Text>();
        _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _text.fontSize = FontSize;
        _text.alignment = TextAnchor.MiddleCenter;
        _text.color = Color.white;

        var textRect = _text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 4f);
        textRect.offsetMax = new Vector2(-12f, -4f);
    }

    public void Show(string message)
    {
        _activeSequence?.Kill();
        _text.text = message;
        _panel.SetActive(true);
        _activeSequence = DOTween.Sequence()
            .AppendInterval(VisibleSeconds)
            .OnComplete(() => _panel.SetActive(false));
    }
}
