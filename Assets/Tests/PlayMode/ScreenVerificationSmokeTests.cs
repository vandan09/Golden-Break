using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// One-off visual verification for the Claude Design UI rework's Game Over
/// and Gallery screens — boots the real Gameplay scene, then calls each
/// screen's own public Show() directly rather than actually playing
/// through a real game to reach game-over naturally (much faster and
/// exactly as reliable, since Show() is the real entry point either way).
/// Same screenshot technique as <see cref="HomeScreenSmokeTests"/>.
/// </summary>
public class ScreenVerificationSmokeTests
{
    [UnityTest]
    public IEnumerator GameOverScreen_ShowsWithoutExceptions_AndRenders()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return null;
        yield return null;

        var gameOverScreen = Object.FindObjectOfType<GameOverScreen>();
        Assert.IsNotNull(gameOverScreen);

        var pieceController = Object.FindObjectOfType<PieceController>();
        Assert.IsNotNull(pieceController);

        // Directly invoke the same OnGameOver event GameOverScreen already
        // subscribes to in Configure(), rather than reaching for a private
        // Show() — this exercises the real wiring, not a bypass of it.
        // Events don't expose a public Invoke from outside the declaring
        // type, so the backing delegate field is found via reflection.
        var onGameOverField = typeof(PieceController).GetField("OnGameOver", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var onGameOverDelegate = onGameOverField?.GetValue(pieceController) as System.Action;
        onGameOverDelegate?.Invoke();

        yield return null;

        Canvas canvas = gameOverScreen.GetComponentInChildren<Canvas>(includeInactive: true);
        Assert.IsNotNull(canvas);

        yield return Snapshot.Capture(canvas, "gameover_playmode_snapshot.png");
    }

    [UnityTest]
    public IEnumerator GalleryScreen_ShowsWithoutExceptions_AndRenders()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return null;
        yield return null;

        var galleryScreen = Object.FindObjectOfType<GalleryScreen>();
        Assert.IsNotNull(galleryScreen);

        // Seed a couple of completed entries so this actually exercises
        // the new 2-column card grid (real ceramic silhouettes), not just
        // the empty state — GalleryManager wraps SaveData.Gallery's own
        // List<> by reference, so appending to it here reaches the
        // GalleryManager GalleryScreen already built from the same list.
        var saveManager = Object.FindObjectOfType<SaveManager>();
        Assert.IsNotNull(saveManager);
        saveManager.Current.Gallery.Add(new GalleryEntryData { Tier = 1, Date = "Jul 28, 2026", Score = 2140 });
        saveManager.Current.Gallery.Add(new GalleryEntryData { Tier = 4, Date = "Jul 19, 2026", Score = 3860 });
        saveManager.Current.Gallery.Add(new GalleryEntryData { Tier = 3, Date = "Jul 6, 2026", Score = 1920 });

        galleryScreen.Show();
        yield return null;

        Canvas canvas = galleryScreen.GetComponentInChildren<Canvas>(includeInactive: true);
        Assert.IsNotNull(canvas);

        yield return Snapshot.Capture(canvas, "gallery_playmode_snapshot.png");
    }
}

internal static class Snapshot
{
    public static IEnumerator Capture(Canvas canvas, string fileName)
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
