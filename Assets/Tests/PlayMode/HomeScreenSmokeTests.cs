using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The "PlayMode smoke test" this project's own QA notes flagged as
/// missing (PROGRESS.md/BUILD_PLAN.md) — boots the real Gameplay scene
/// end to end (the same bootstrap every real launch goes through) and
/// asserts Home actually comes up. Also saves a screenshot for visual
/// review of the Claude Design Home screen rework, using the same
/// camera-targetTexture technique <c>GridRenderingPlayModeTests</c>
/// already validated — ScreenSpaceOverlay canvases draw straight to the
/// backbuffer, which -batchmode can't reliably read back (that's why
/// WaitForEndOfFrame hangs there — no display surface to present to), so
/// the Home canvas is temporarily switched to ScreenSpaceCamera for the
/// capture only.
/// </summary>
public class HomeScreenSmokeTests
{
    [UnityTest]
    public IEnumerator HomeScreen_BootsAndIsVisible_AfterGameplaySceneLoads()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return null;
        yield return null;

        var homeScreen = Object.FindObjectOfType<HomeScreen>();
        Assert.IsNotNull(homeScreen, "HomeScreen should exist after the Gameplay scene finishes bootstrapping.");
        Assert.IsTrue(homeScreen.IsVisible, "HomeScreen should be the visible screen on first boot.");

        Canvas canvas = homeScreen.GetComponentInChildren<Canvas>();
        Assert.IsNotNull(canvas, "HomeScreen should have built its own Canvas.");

        yield return Snapshot.Capture(canvas, "homescreen_playmode_snapshot.png");
    }
}
