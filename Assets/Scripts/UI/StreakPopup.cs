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
    private GameObject _glowObject;
    private Text _messageText;

    public void Configure()
    {
        BuildUi();
        _panel.SetActive(false);
        _glowObject.SetActive(false);
    }

    private void BuildUi()
    {
        Canvas canvas = ResponsiveCanvasSetup.BuildCanvas(transform, "StreakPopupCanvas", 12);
        RectTransform safeArea = ResponsiveCanvasSetup.BuildSafeArea(canvas.transform);

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(safeArea, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.sprite = RoundedRectSprite.Get(20);
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(UiPalette.Surface.r, UiPalette.Surface.g, UiPalette.Surface.b, 0.95f);

        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.8f);
        panelRect.anchorMax = new Vector2(0.5f, 0.8f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(300f, 80f);

        AddBorder(panelRect, 20, UiPalette.GoldFill, 1);

        // Gold-bordered celebratory panel -- same rule as the gold buttons.
        // The glow is a SIBLING of the panel (it has to be, to draw behind
        // it), so hiding the panel does NOT hide it: it must be toggled
        // alongside, or it hangs on screen permanently.
        _glowObject = UiKit.AddGlowBehind(panelRect, UiPalette.GoldFill, UiKit.ButtonGlowBlur, UiKit.ButtonGlowAlpha, 20).gameObject;

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
        string message = string.Format(Strings.StreakPopupFormat, result.StreakCount, result.CoinsAwarded);
        if (result.GalleryFrameUnlocked != null)
        {
            message += Strings.StreakPopupFrameUnlockedSuffix;
        }

        _messageText.text = message;
        _panel.SetActive(true);
        _glowObject.SetActive(true);
        _panel.transform.localScale = Vector3.one * 0.85f;

        DOTween.Sequence()
            .Append(_panel.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
            .AppendInterval(VisibleSeconds)
            .OnComplete(() =>
            {
                _panel.SetActive(false);
                _glowObject.SetActive(false);
            });
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
