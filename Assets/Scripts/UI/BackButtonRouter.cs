using System;
using UnityEngine;

/// <summary>
/// Routes the Android hardware back button (mapped to KeyCode.Escape by
/// Unity) based on which overlay screen is currently showing — a real
/// gap caught on-device: there was previously no way back to the main
/// menu or to exit the app at all (see PROGRESS.md). Polls in Update()
/// for the same reason InputHandler's own doc comment already carves out
/// of "no logic in Update() that could be event-driven": there is no
/// single event to hook for "the OS-level back button was pressed."
///
/// Priority order: whichever overlay is currently visible closes first
/// (deepest screen in the stack) before falling through to Home. Home
/// itself is the top of the stack — back from there exits the app,
/// matching standard Android convention. GameOverScreen is deliberately
/// not part of this stack: it already has its own explicit Play Again/
/// Continue buttons and PieceController.IsGameOver already blocks
/// gameplay input underneath it, so there's no dangling state a back
/// press needs to resolve there.
/// </summary>
public sealed class BackButtonRouter : MonoBehaviour
{
    private HomeScreen _homeScreen;
    private GalleryScreen _galleryScreen;
    private SettingsScreen _settingsScreen;
    private DailyChallengeUI _dailyChallengeUI;
    private Func<bool> _isDailyChallengeSessionActive;
    private Action _exitDailyChallengeSession;

    // isDailyChallengeSessionActive/exitDailyChallengeSession are optional
    // hooks for a live Daily Challenge attempt (own grid, own HUD, no
    // overlay screen of its own to report IsVisible the way
    // Gallery/Settings/DailyChallengeUI do) — checked ahead of the
    // fallback "show Home" case so a back-press from inside that separate
    // session properly tears its view down instead of just stacking Home
    // on top of it.
    public void Configure(
        HomeScreen homeScreen,
        GalleryScreen galleryScreen,
        SettingsScreen settingsScreen,
        DailyChallengeUI dailyChallengeUI,
        Func<bool> isDailyChallengeSessionActive = null,
        Action exitDailyChallengeSession = null)
    {
        _homeScreen = homeScreen;
        _galleryScreen = galleryScreen;
        _settingsScreen = settingsScreen;
        _dailyChallengeUI = dailyChallengeUI;
        _isDailyChallengeSessionActive = isDailyChallengeSessionActive;
        _exitDailyChallengeSession = exitDailyChallengeSession;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (_settingsScreen != null && _settingsScreen.IsVisible)
        {
            _settingsScreen.Hide();
            return;
        }

        if (_galleryScreen != null && _galleryScreen.IsVisible)
        {
            _galleryScreen.Hide();
            return;
        }

        if (_dailyChallengeUI != null && _dailyChallengeUI.IsVisible)
        {
            _dailyChallengeUI.Hide();
            return;
        }

        if (_isDailyChallengeSessionActive != null && _isDailyChallengeSessionActive())
        {
            _exitDailyChallengeSession?.Invoke();
            return;
        }

        if (_homeScreen != null && _homeScreen.IsVisible)
        {
            Application.Quit();
            return;
        }

        _homeScreen?.Show();
    }
}
