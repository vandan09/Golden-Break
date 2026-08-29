using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings panel (CLAUDE.md §7.3 settings block, §9.4 cross-promotion):
/// sound/music/haptics/high-contrast toggles and a cross-promotion link.
/// Same runtime-built-uGUI full-screen-overlay pattern as GalleryScreen/
/// GameOverScreen — no hand-authored scene objects (see PROGRESS.md).
///
/// High-contrast (CLAUDE.md §3.9: "colourblind pattern opacity 15% ->
/// 40%") toggles <see cref="UiPalette.HighContrastEnabled"/>, which every
/// call to <see cref="UiPalette.GetBlockSprite"/> reads live — no need to
/// push the new value through every screen individually. The injected
/// onHighContrastChanged callback exists only so the composition root can
/// force the currently-visible board(s) to repaint immediately, since a
/// board that's already drawn won't otherwise notice the static flag
/// changed until its next natural redraw.
/// </summary>
public sealed class SettingsScreen : MonoBehaviour
{
    private const int RowLabelFontSize = 16;
    private const float RowHeight = 56f;
    private const float RowSpacing = 10f;

    // Rows hang off the shared header rather than a hardcoded offset that
    // was tuned for the centred title this screen used to have.
    private const float ContentTopGap = 24f;

    // Extra breathing room before Remove Ads: it is an action, not another
    // setting, and sitting it flush in the toggle stack read as a fifth row.
    private const float ActionGap = 22f;

    // Switch geometry. The knob is inset 3px from the track on both sides.
    private const float TrackWidth = 52f;
    private const float TrackHeight = 30f;
    private const float KnobSize = 24f;
    private const float KnobInset = 3f;

    private SaveManager _saveManager;
    private InputHandler _inputHandler;
    private IapManager _iapManager;
    private Action _onHighContrastChanged;
    private HomeScreen _homeScreen;
    private GameObject _panel;
    private Toggle _soundToggle;
    private Toggle _musicToggle;
    private Toggle _hapticsToggle;
    private Toggle _highContrastToggle;
    private GameObject _removeAdsButton;
    private Text _removeAdsButtonLabel;

    // Set post-construction (GameplayController builds HomeScreen last).
    // Settings is only ever reachable from Home, so closing it must
    // return there — real bug caught on-device: Home's own panel never
    // hid itself when opening Settings, so Settings opened invisibly
    // behind it and never received a single tap.
    public void SetHomeScreen(HomeScreen homeScreen)
    {
        _homeScreen = homeScreen;
    }

    public void Configure(SaveManager saveManager, InputHandler inputHandler, IapManager iapManager = null, Action onHighContrastChanged = null)
    {
        _saveManager = saveManager;
        _inputHandler = inputHandler;
        _iapManager = iapManager;
        _onHighContrastChanged = onHighContrastChanged;

        BuildUi();
        _panel.SetActive(false);
    }

    private void BuildUi()
    {
        Canvas canvas = ResponsiveCanvasSetup.BuildCanvas(transform, "SettingsCanvas", 20);
        RectTransform safeArea = ResponsiveCanvasSetup.BuildSafeArea(canvas.transform);

        _panel = new GameObject("Panel");
        _panel.transform.SetParent(safeArea, false);
        var panelImage = _panel.AddComponent<Image>();
        panelImage.color = UiPalette.Background;
        var panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        UiKit.BuildBackHeader(_panel.transform, Strings.SettingsTitle, Hide);

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

    private Toggle BuildToggleRow(Transform parent, string label, int rowIndex)
    {
        var rowObject = new GameObject($"Row_{label}");
        rowObject.transform.SetParent(parent, false);
        var rowBg = rowObject.AddComponent<Image>();
        rowBg.sprite = RoundedRectSprite.Get(Mathf.RoundToInt(RowHeight * 0.5f));
        rowBg.type = Image.Type.Sliced;
        rowBg.color = UiPalette.Surface;

        var rowRect = rowObject.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.08f, 1f);
        rowRect.anchorMax = new Vector2(0.92f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -RowTop(rowIndex));
        rowRect.sizeDelta = new Vector2(0f, RowHeight);

        AddBorder(rowRect, Mathf.RoundToInt(RowHeight * 0.5f), UiPalette.CardBorder);

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(rowObject.transform, false);
        var labelText = labelObject.AddComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = RowLabelFontSize;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = UiPalette.TextPrimary;
        labelText.text = label;
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0.7f, 1f);
        labelRect.offsetMin = new Vector2(22, 0);
        labelRect.offsetMax = Vector2.zero;

        return BuildSwitch(rowObject.transform);
    }

    // A real track-and-knob switch. This was previously a rounded rect
    // that simply filled gold when on, with no knob and nothing that
    // moved -- it read as a lit box rather than a switch, and gave no
    // clue which side was "on".
    private Toggle BuildSwitch(Transform parent)
    {
        var toggleObject = new GameObject("Toggle");
        toggleObject.transform.SetParent(parent, false);
        var toggleRect = toggleObject.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(1f, 0.5f);
        toggleRect.anchorMax = new Vector2(1f, 0.5f);
        toggleRect.pivot = new Vector2(1f, 0.5f);
        toggleRect.anchoredPosition = new Vector2(-22f, 0f);
        toggleRect.sizeDelta = new Vector2(TrackWidth, TrackHeight);

        int trackRadius = Mathf.RoundToInt(TrackHeight * 0.5f);

        var track = toggleObject.AddComponent<Image>();
        track.sprite = RoundedRectSprite.Get(trackRadius);
        track.type = Image.Type.Sliced;
        track.color = UiPalette.EmptyCellFill;

        // The gold "on" state is a separate overlay so Toggle.graphic can
        // cross-fade it, leaving the neutral track underneath for "off".
        var onFillObject = new GameObject("OnFill");
        onFillObject.transform.SetParent(toggleObject.transform, false);
        var onFillRect = onFillObject.AddComponent<RectTransform>();
        onFillRect.anchorMin = Vector2.zero;
        onFillRect.anchorMax = Vector2.one;
        onFillRect.offsetMin = Vector2.zero;
        onFillRect.offsetMax = Vector2.zero;
        var onFill = onFillObject.AddComponent<Image>();
        onFill.sprite = RoundedRectSprite.Get(trackRadius);
        onFill.type = Image.Type.Sliced;
        onFill.color = UiPalette.GoldFill;

        var knobObject = new GameObject("Knob");
        knobObject.transform.SetParent(toggleObject.transform, false);
        var knobRect = knobObject.AddComponent<RectTransform>();
        knobRect.anchorMin = new Vector2(0.5f, 0.5f);
        knobRect.anchorMax = new Vector2(0.5f, 0.5f);
        knobRect.pivot = new Vector2(0.5f, 0.5f);
        knobRect.sizeDelta = new Vector2(KnobSize, KnobSize);
        var knob = knobObject.AddComponent<Image>();
        knob.sprite = RoundedRectSprite.Get(Mathf.RoundToInt(KnobSize * 0.5f));
        knob.type = Image.Type.Sliced;
        knob.color = UiPalette.Background;
        knob.raycastTarget = false;

        var toggle = toggleObject.AddComponent<Toggle>();
        toggle.targetGraphic = track;
        toggle.graphic = onFill;
        toggle.isOn = true;

        toggle.onValueChanged.AddListener(_ => SyncSwitchVisual(toggle));
        SyncSwitchVisual(toggle);

        return toggle;
    }

    // Also called after SetIsOnWithoutNotify in Show(), which by design
    // does not fire onValueChanged and would otherwise leave the knob
    // parked on the wrong side of the track.
    private static void SyncSwitchVisual(Toggle toggle)
    {
        Transform knob = toggle.transform.Find("Knob");
        if (knob == null)
        {
            return;
        }

        float travel = (TrackWidth * 0.5f) - (KnobSize * 0.5f) - KnobInset;
        ((RectTransform)knob).anchoredPosition = new Vector2(toggle.isOn ? travel : -travel, 0f);
    }

    private static float RowTop(int rowIndex)
    {
        return UiKit.TopPadding + UiKit.HeaderHeight + ContentTopGap + (rowIndex * (RowHeight + RowSpacing));
    }

    // CLAUDE.md §5.2: "Remove interstitials | ₹249 / $2.99." The one IAP
    // entry point this screen offers directly — theme packs/coin bundles
    // belong to a dedicated shop UI that isn't in CLAUDE.md's own file
    // tree as a separate screen; this gives IapManager's real, tested
    // entitlement logic an actual on-screen trigger without inventing a
    // new screen file for it.
    private void BuildRemoveAdsButton(Transform parent, int rowIndex)
    {
        Button button = UiKit.BuildButton(parent, "Row_RemoveAds", Strings.SettingsRemoveAdsLabel, OnRemoveAdsClicked, UiKit.ButtonStyle.Primary, RowHeight);

        var rowRect = button.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.08f, 1f);
        rowRect.anchorMax = new Vector2(0.92f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, -(RowTop(rowIndex) + ActionGap));
        rowRect.sizeDelta = new Vector2(0f, RowHeight);

        UiKit.AddPrimaryGlow(button, RowHeight);

        _removeAdsButton = button.gameObject;
        _removeAdsButtonLabel = UiKit.GetLabel(button);
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
        Button button = UiKit.BuildButton(parent, "CrossPromoCard", Strings.SettingsCrossPromoLabel, OnCrossPromoClicked, UiKit.ButtonStyle.Secondary, UiKit.SecondaryButtonHeight);

        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0f);
        rect.anchorMax = new Vector2(0.92f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 30f);
        rect.sizeDelta = new Vector2(0f, UiKit.SecondaryButtonHeight);
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
        UiPalette.HighContrastEnabled = value;
        _onHighContrastChanged?.Invoke();
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

    public bool IsVisible => _panel != null && _panel.activeSelf;

    public void Show()
    {
        SaveSettingsData settings = _saveManager.Current.Settings;
        _soundToggle.SetIsOnWithoutNotify(settings.Sound);
        _musicToggle.SetIsOnWithoutNotify(settings.Music);
        _hapticsToggle.SetIsOnWithoutNotify(settings.Haptics);
        _highContrastToggle.SetIsOnWithoutNotify(settings.HighContrast);

        SyncSwitchVisual(_soundToggle);
        SyncSwitchVisual(_musicToggle);
        SyncSwitchVisual(_hapticsToggle);
        SyncSwitchVisual(_highContrastToggle);

        RefreshRemoveAdsButton();

        _panel.SetActive(true);
        UiKit.PlayOverlayShow(_panel);
        if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = false;
        }
    }

    public void Hide()
    {
        _panel.SetActive(false);

        if (_homeScreen != null)
        {
            _homeScreen.Show();
        }
        else if (_inputHandler != null)
        {
            _inputHandler.InputEnabled = true;
        }
    }
}
