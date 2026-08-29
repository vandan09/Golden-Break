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

    // DEV ONLY view — the catalogue of every ceramic and colour variant,
    // reached by five taps on the Gallery title. Verified here because it
    // is the only way to see all 21 without playing to tier 21, and a
    // hidden gesture is exactly the kind of thing that rots unnoticed.
    [UnityTest]
    public IEnumerator GalleryScreen_DevPreview_RendersEveryCeramicAndVariant()
    {
        yield return SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Single);
        yield return null;
        yield return null;

        var galleryScreen = Object.FindObjectOfType<GalleryScreen>();
        Assert.IsNotNull(galleryScreen);

        galleryScreen.Show();
        yield return null;

        Transform toggle = galleryScreen.transform.Find(
            "GalleryCanvas/Panel/SafeArea/Header/DevPreviewToggle");
        Assert.IsNotNull(toggle, "dev preview is reached from a toggle in the header");

        var toggleButton = toggle.GetComponent<UnityEngine.UI.Button>();
        Assert.IsNotNull(toggleButton);

        // Raycast at the toggle's own screen position rather than calling
        // onClick directly. Invoking the handler proves the cards build but
        // NOT that a tap ever reaches the button — which is exactly how the
        // previous hidden gesture shipped broken: the header behind it also
        // carries a Button, so a click landing on the wrong graphic closes
        // the screen instead of toggling.
        var toggleRect = (RectTransform)toggle;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, toggleRect.position);

        var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = screenPoint,
        };

        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);

        Assert.Greater(hits.Count, 0, "a tap on the toggle must hit something");
        Assert.AreEqual(
            toggle.gameObject,
            hits[0].gameObject,
            "the toggle must be the topmost hit — otherwise the tap goes to the header behind it");

        UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(
            hits[0].gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);

        yield return null;

        Transform content = galleryScreen.transform.Find(
            "GalleryCanvas/Panel/SafeArea/ScrollView/Viewport/Content");
        Assert.IsNotNull(content, "card grid");

        // 9 tiers in the base metal, plus tiers 6-9 in each further metal.
        int expected = 9 + ((CeramicGold.VariantCount - 1) * 4);
        Assert.AreEqual(expected, content.childCount, "every ceramic and variant should have a card");

        Canvas canvas = galleryScreen.GetComponentInChildren<Canvas>(includeInactive: true);
        yield return Snapshot.Capture(canvas, "gallery_dev_preview_snapshot.png");
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
