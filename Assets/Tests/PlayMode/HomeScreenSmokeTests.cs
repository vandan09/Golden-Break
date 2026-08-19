using System.Collections;
using System.IO;
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

        yield return CaptureSnapshot(canvas, "homescreen_playmode_snapshot.png");
    }

    private static IEnumerator CaptureSnapshot(Canvas canvas, string fileName)
    {
        var camObject = new GameObject("SnapshotCamera");
        var cam = camObject.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        const int width = 1080, height = 2336;
        var rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;

        RenderMode originalMode = canvas.renderMode;
        Camera originalCamera = canvas.worldCamera;

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;

        yield return null;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        byte[] png = tex.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, "..", "Logs", fileName);
        File.WriteAllBytes(path, png);
        Debug.Log($"[SNAPSHOT] Saved {fileName} ({width}x{height}) to {path}");

        canvas.renderMode = originalMode;
        canvas.worldCamera = originalCamera;

        Object.Destroy(tex);
        Object.Destroy(camObject);
        rt.Release();
    }
}
