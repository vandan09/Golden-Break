using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings panel (CLAUDE.md §7.3 settings block, §9.4 cross-promotion):
/// sound/music/haptics/high-contrast toggles and a cross-promotion link.
/// Same runtime-built-uGUI full-screen-overlay pattern as GalleryScreen/
/// GameOverScreen — no hand-authored scene objects (see PROGRESS.md).
///
/// High-contrast only persists the flag for now — CLAUDE.md §3.9's actual
/// visual effect (colourblind pattern opacity 15% -> 40%) needs the
/// pattern-overlay art that doesn't exist yet (Phase 5/8 scope, same as
/// every other block-colour pattern); toggling it here has no visible
/// effect yet, honestly reflecting that rather than faking one.
/// </summary>
public sealed class SettingsScreen : MonoBehaviour
{
    private const int TitleFontSize = 32;
    private const int RowLabelFontSize = 24;
    private const float RowHeight = 70f;
    private const float RowSpacing = 20f;

    private SaveManager _saveManager;
    private InputHandler _inputHandler;
    private IapManager _iapManager;
    private GameObject _panel;
    private Toggle _soundToggle;
    private Toggle _musicToggle;
    private Toggle _hapticsToggle;
    private Toggle _highContrastToggle;
    private GameObject _removeAdsButton;
    private Text _removeAdsButtonLabel;

    public void Configure(SaveManager saveManager, InputHandler inputHandler, IapManager iapManager = null)
    {
        _saveManager = saveManager;
        _inputHandler = inputHandler;
        _iapManager = iapManager;

        BuildUi();
        _panel.SetActive(false);
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("SettingsCanvas");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20; // above Gallery (15) — can be opened from Home or from Gallery
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(canvasObject.transform, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = UiPalette.Background;
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        BuildTitle(_panel.transform);
        BuildCloseButton(_panel.transform);

        _soundToggle = BuildToggleRow(_panel.transform, Strings.SettingsSoundLabel, 0);
        _musicToggle = BuildToggleRow(_panel.transform, Strings.SettingsMusicLabel, 1);
        _hapticsToggle = BuildToggleRow(_panel.transform, Strings.SettingsHapticsLabel, 2);
        _highContrastToggle = BuildToggleRow(_panel.transform, Strings.SettingsHighContrastLabel, 3);
        BuildRemoveAdsButton(_panel.transform, 4);

        _soundToggle.onValueChanged.AddListener(OnSoundChanged);
        _musicToggle.onValueChanged.AddListener(OnMusicChanged);
        _hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);
        _highContrastToggle.onValueChanged.AddListener(OnHighContrastChanged);

        BuildCrossPromoCard(_panel.transform);
    }

    private void BuildTitle(Transform parent)
    {
        var textObject = new GameObject("Title");
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = TitleFontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = UiPalette.TextPrimary;
        text.text = Strings.SettingsTitle;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -20f);
        rect.sizeDelta = new Vector2(400f, 60f);
    }

    private void BuildCloseButton(Transform parent)
    {
        var buttonObject = new GameObject("CloseButton");
        buttonObject.transform.SetParent(parent, false);
        buttonObject.AddComponent<Image>().color = UiPalette.Surface;
        buttonObject.AddComponent<Button>().onClick.AddListener(Hide);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -20f);
        rect.sizeDelta = new Vector2(60f, 50f);

        BuildCenteredLabel(buttonObject.transform, Strings.CloseButtonSymbol, 24);
    }

    private Toggle BuildToggleRow(Transform parent, string label, int rowIndex)
    {
        var rowObject = new GameObject($"Row_{label}");
        rowObject.transform.SetParent(parent, false);
        var rowRect = rowObject.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.1f, 1f);
        rowRect.anchorMax = new Vector2(0.9f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -100f - (rowIndex * (RowHeight + RowSpacing)));
        rowRect.sizeDelta = new Vector2(0f, RowHeight);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(rowObject.transform, false);
        var labelText = labelObject.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = RowLabelFontSize;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = UiPalette.TextPrimary;
        labelText.text = label;
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0.6f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var toggleObject = new GameObject("Toggle");
        toggleObject.transform.SetParent(rowObject.transform, false);
        var toggleRect = toggleObject.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.8f, 0.5f);
        toggleRect.anchorMax = new Vector2(0.8f, 0.5f);
        toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.sizeDelta = new Vector2(50f, 50f);

        var background = toggleObject.AddComponent<Image>();
        background.color = UiPalette.Surface;

        var checkmarkObject = new GameObject("Checkmark");
        checkmarkObject.transform.SetParent(toggleObject.transform, false);
        var checkmarkImage = checkmarkObject.AddComponent<Image>();
        checkmarkImage.color = UiPalette.GoldFill;
        var checkmarkRect = checkmarkObject.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
        checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
        checkmarkRect.offsetMin = Vector2.zero;
        checkmarkRect.offsetMax = Vector2.zero;

        var toggle = toggleObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmarkImage;
        toggle.isOn = true;

        return toggle;
    }

    // CLAUDE.md §5.2: "Remove interstitials | ₹249 / $2.99." The one IAP
    // entry point this screen offers directly — theme packs/coin bundles
    // belong to a dedicated shop UI that isn't in CLAUDE.md's own file
    // tree as a separate screen; this gives IapManager's real, tested
    // entitlement logic an actual on-screen trigger without inventing a
    // new screen file for it.
    private void BuildRemoveAdsButton(Transform parent, int rowIndex)
    {
        var rowObject = new GameObject("Row_RemoveAds");
        rowObject.transform.SetParent(parent, false);
        rowObject.AddComponent<Image>().color = UiPalette.Surface;
        var button = rowObject.AddComponent<Button>();
        button.onClick.AddListener(OnRemoveAdsClicked);

        var rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.1f, 1f);
        rowRect.anchorMax = new Vector2(0.9f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -100f - (rowIndex * (RowHeight + RowSpacing)));
        rowRect.sizeDelta = new Vector2(0f, RowHeight);

        _removeAdsButton = rowObject;
        _removeAdsButtonLabel = BuildCenteredLabel(rowObject.transform, Strings.SettingsRemoveAdsLabel, RowLabelFontSize);
    }

    private void OnRemoveAdsClicked()
    {
        if (_iapManager == null || _iapManager.IsAdsRemoved)
        {
            return;
        }

        _iapManager.Purchase(IapItem.RemoveAds, success =>
        {
            if (success)
            {
                RefreshRemoveAdsButton();
            }
        });
    }

    private void RefreshRemoveAdsButton()
    {
        if (_iapManager == null || _removeAdsButton == null)
        {
            return;
        }

        bool owned = _iapManager.IsAdsRemoved;
        _removeAdsButton.GetComponent<Button>().interactable = !owned;
        _removeAdsButtonLabel.text = owned ? Strings.SettingsAdsRemovedLabel : Strings.SettingsRemoveAdsLabel;
    }

    private void BuildCrossPromoCard(Transform parent)
    {
        var cardObject = new GameObject("CrossPromoCard");
        cardObject.transform.SetParent(parent, false);
        cardObject.AddComponent<Image>().color = UiPalette.Surface;
        var button = cardObject.AddComponent<Button>();
        button.onClick.AddListener(OnCrossPromoClicked);

        var rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0f);
        rect.anchorMax = new Vector2(0.9f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 40f);
        rect.sizeDelta = new Vector2(0f, 90f);

        BuildCenteredLabel(cardObject.transform, Strings.SettingsCrossPromoLabel, 20);
    }

    private static Text BuildCenteredLabel(Transform parent, string content, int fontSize)
    {
        var textObject = new GameObject("Label");
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = UiPalette.TextPrimary;
        text.text = content;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }

    private void OnSoundChanged(bool value)
    {
        _saveManager.Current.Settings.Sound = value;
        AudioManager.Instance?.SetSoundEnabled(value);
        _saveManager.Save();
    }

    private void OnMusicChanged(bool value)
    {
        _saveManager.Current.Settings.Music = value;
        AudioManager.Instance?.SetMusicEnabled(value);
        _saveManager.Save();
    }

    private void OnHapticsChanged(bool value)
    {
        _saveManager.Current.Settings.Haptics = value;
        HapticManager.Enabled = value;
        _saveManager.Save();
    }

    private void OnHighContrastChanged(bool value)
    {
        _saveManager.Current.Settings.HighContrast = value;
        _saveManager.Save();
    }

    // CLAUDE.md §9.4: "small 'More cozy puzzles' card... linking to the
    // other game's Play Store page." No real GLYPH Play Store listing
    // exists yet to link to (same placeholder category as ad unit IDs/
    // GameAnalytics key) — logs intent instead of opening a dead/guessed
    // URL, matching every other not-yet-real external integration in
    // this project.
    private void OnCrossPromoClicked()
    {
        Debug.Log("SettingsScreen: would open GLYPH's Play Store listing here once it's published (TODO).");
    }

    public void Show()
    {
        SaveSettingsData settings = _saveManager.Current.Settings;
        _soundToggle.SetIsOnWithoutNotify(settings.Sound);
        _musicToggle.SetIsOnWithoutNotify(settings.Music);
        _hapticsToggle.SetIsOnWithoutNotify(settings.Haptics);
        _highContrastToggle.SetIsOnWithoutNotify(settings.HighContrast);
        RefreshRemoveAdsButton();

        _panel.SetActive(true);
        if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = false;
        }
    }

    private void Hide()
    {
        _panel.SetActive(false);
        if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = true;
        }
    }
}
