using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Self-bootstraps the Gameplay scene: grid, tray, HUD, game-over screen,
/// kintsugi ceramic + gallery (Phase 3), and (Phase 4) the full retention/
/// monetization wiring — Home, Settings, Daily Challenge, undo/refresh,
/// continue, rewarded ads, interstitials, and IAP. Loads the
/// <see cref="PieceDefinition"/> and <see cref="CeramicDefinition"/> pools
/// and wires everything together, entirely at runtime (no hand-authored
/// scene objects to edit blind without an interactive Editor session —
/// see PROGRESS.md).
///
/// Bootstraps via <see cref="SceneManager.sceneLoaded"/>, not
/// [RuntimeInitializeOnLoadMethod] directly — that attribute only fires
/// once, right after the very first scene loads (Boot, here). Gameplay
/// can load later (after Boot->Gameplay, and again on "Play again" in a
/// future phase), so this needs a persistent subscription that fires on
/// every scene load, not a one-shot callback. Confirmed on-device: the
/// one-shot version built the entire grid inside the Boot scene, which
/// was then destroyed the instant BootController switched scenes,
/// leaving Gameplay's own untouched default camera on screen.
/// </summary>
public sealed class GameplayController : MonoBehaviour
{
    private const string GameplaySceneName = "Gameplay";
    private const float TrayVerticalGap = 1.5f;
    private const float CameraPaddingCells = 1.5f;
    private const float CeramicVerticalGap = 0.6f;
    private const float CeramicAreaHalfHeight = 1.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedHandler()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameplaySceneName || FindObjectOfType<GameplayController>() != null)
        {
            return;
        }

        var controllerObject = new GameObject("GameplayController");
        controllerObject.AddComponent<GameplayController>();
    }

    private void Awake()
    {
        PieceDefinition[] pool = Resources.LoadAll<PieceDefinition>("PieceDefinitions");
        if (pool.Length == 0)
        {
            Debug.LogError("GameplayController: no PieceDefinition assets found in Resources/PieceDefinitions.");
            return;
        }

        ConfigureCamera();

        SaveManager saveManager = FindObjectOfType<SaveManager>();
        if (saveManager == null)
        {
            saveManager = new GameObject("SaveManager").AddComponent<SaveManager>();
        }

        if (FindObjectOfType<AudioManager>() == null)
        {
            new GameObject("AudioManager").AddComponent<AudioManager>();
        }

        if (FindObjectOfType<AdManager>() == null)
        {
            new GameObject("AdManager").AddComponent<AdManager>();
        }

        if (FindObjectOfType<AnalyticsManager>() == null)
        {
            new GameObject("AnalyticsManager").AddComponent<AnalyticsManager>();
        }

        // Without this, GraphicRaycaster alone never dispatches clicks —
        // no uGUI Button anywhere in the scene receives input at all.
        // Confirmed the hard way: Play Again looked fully wired (Canvas,
        // GraphicRaycaster, Button.onClick) but silently did nothing on a
        // real device tap, because nothing was present to route the tap
        // to the raycaster in the first place.
        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        var gridObject = new GameObject("Grid");
        var grid = gridObject.AddComponent<GridManager>();
        grid.BuildGrid();

        var trayObject = new GameObject("Tray");
        float trayY = -GetGridHalfHeight() - TrayVerticalGap;
        trayObject.transform.position = new Vector3(0f, trayY, 0f);
        var tray = trayObject.AddComponent<PieceTrayController>();
        tray.BuildSlots();

        var pieceControllerObject = new GameObject("PieceController");
        var pieceController = pieceControllerObject.AddComponent<PieceController>();

        // DDA weighting (CLAUDE.md §3.7) reads saveManager.Current live on
        // every spawn rather than capturing fixed averages once — the same
        // SaveManager.Current object is mutated in place by
        // GameplaySaveTriggers.OnGameOver, so this correctly reflects
        // updated DDA history after a "Play again" restart within the same
        // app session too (RestartGame() reuses this same spawner
        // instance, it never gets reconstructed).
        var spawner = new PieceSpawner(pool, new System.Random(), piece => ComputeDdaWeightMultiplier(piece, saveManager));

        // CLAUDE.md §5.1: continue deals from "the standard weighted
        // pool, not the DDA-adjusted pool" — a second, independent
        // spawner with no weight multiplier at all.
        var standardSpawnerForContinue = new PieceSpawner(pool, new System.Random());

        var scoreManager = new ScoreManager(saveManager.Current.BestScore);
        pieceController.Configure(grid, tray, spawner, scoreManager, standardSpawnerForContinue);

        var coinManager = new CoinManager(saveManager.Current.Coins);

        // Constructed for its subscription side effects only — nothing
        // else in this method needs to hold a reference to it, same as
        // CeramicController's own OnLinesCleared subscription pattern.
        var saveTriggers = new GameplaySaveTriggers(pieceController, coinManager, saveManager.Current, saveManager.Save);

        var inputHandlerObject = new GameObject("InputHandler");
        var inputHandler = inputHandlerObject.AddComponent<InputHandler>();
        inputHandler.Configure(pieceController, Camera.main);

        var rewardedAdController = new RewardedAdController(
            (placement, onReward, onFailure) => AdManager.Instance?.ShowRewarded(placement, onReward, onFailure),
            pieceController,
            coinManager);

        var interstitialController = new InterstitialController(
            saveManager.Current,
            () => AdManager.Instance?.ShowInterstitial(() => { }));

        var iapManager = new IapManager(
            saveManager.Current,
            coinManager,
            saveManager.Save,
            (storeItemId, onSuccess, onFailure) =>
            {
                // TODO(iap-setup): no store SDK integrated yet (see
                // IapManager's own doc comment) — always reports
                // unavailable rather than granting a fake entitlement.
                onFailure("no IAP SDK integrated yet");
            });

        var hudObject = new GameObject("GameplayHUD");
        var hud = hudObject.AddComponent<GameplayHUD>();
        hud.Configure(pieceController, saveManager, coinManager, rewardedAdController);

        CeramicController ceramicController = null;
        GalleryScreen galleryScreen = null;

        CeramicDefinition[] ceramicPool = Resources.LoadAll<CeramicDefinition>("CeramicDefinitions");
        if (ceramicPool.Length == 0)
        {
            Debug.LogError("GameplayController: no CeramicDefinition assets found in Resources/CeramicDefinitions.");
        }
        else
        {
            var ceramicObject = new GameObject("Ceramic");
            ceramicObject.transform.position = new Vector3(0f, GetCeramicCenterY(), 0f);
            var ceramicView = ceramicObject.AddComponent<CeramicView>();
            ceramicView.Initialize();

            var ceramicManager = new CeramicManager(saveManager.Current.CurrentCeramic, saveManager.Current.CeramicCumulativeScore);
            var galleryManager = new GalleryManager(saveManager.Current.Gallery);

            var ceramicControllerObject = new GameObject("CeramicController");
            ceramicController = ceramicControllerObject.AddComponent<CeramicController>();
            ceramicController.Configure(pieceController, ceramicView, ceramicPool, ceramicManager, galleryManager, saveManager, coinManager);

            var galleryScreenObject = new GameObject("GalleryScreen");
            galleryScreen = galleryScreenObject.AddComponent<GalleryScreen>();
            galleryScreen.Configure(galleryManager, ceramicPool, inputHandler);
        }

        var streakPopupObject = new GameObject("StreakPopup");
        var streakPopup = streakPopupObject.AddComponent<StreakPopup>();
        streakPopup.Configure();

        var gameOverObject = new GameObject("GameOverScreen");
        var gameOverScreen = gameOverObject.AddComponent<GameOverScreen>();
        gameOverScreen.Configure(pieceController, saveTriggers, rewardedAdController, interstitialController, streakPopup, ceramicController);

        var settingsScreenObject = new GameObject("SettingsScreen");
        var settingsScreen = settingsScreenObject.AddComponent<SettingsScreen>();
        settingsScreen.Configure(saveManager, inputHandler, iapManager);

        var dailyChallengeUiObject = new GameObject("DailyChallengeUI");
        var dailyChallengeUi = dailyChallengeUiObject.AddComponent<DailyChallengeUI>();
        dailyChallengeUi.Configure(saveManager, inputHandler, () => StartDailyChallenge(pieceController, pool));

        var homeScreenObject = new GameObject("HomeScreen");
        var homeScreen = homeScreenObject.AddComponent<HomeScreen>();
        homeScreen.Configure(saveManager, inputHandler, galleryScreen, settingsScreen, dailyChallengeUi);
    }

    // CLAUDE.md §4.2: builds a fresh seeded spawner for *today* and swaps
    // the whole PieceController session onto it — see
    // PieceController.StartDailyChallenge's own doc comment for why the
    // seeded stream needs to hold for the entire session, not just the
    // first hand.
    private static void StartDailyChallenge(PieceController pieceController, PieceDefinition[] pool)
    {
        PieceSpawner dailySpawner = DailyChallengeManager.CreateSpawner(pool, System.DateTime.UtcNow);
        pieceController.StartDailyChallenge(dailySpawner);
    }

    // CLAUDE.md §3.7: recomputed from saveManager.Current on every call
    // (not captured once) so DDA weighting reflects the latest game-over
    // history even across a same-session "Play again" restart, which
    // reuses this same PieceSpawner instance rather than reconstructing
    // it.
    private static float ComputeDdaWeightMultiplier(PieceDefinition piece, SaveManager saveManager)
    {
        System.Collections.Generic.List<int> last10 = saveManager.Current.DdaLast10Scores;
        float last10Average = 0f;
        if (last10 != null && last10.Count > 0)
        {
            float sum = 0f;
            foreach (int score in last10)
            {
                sum += score;
            }

            last10Average = sum / last10.Count;
        }

        return DDAManager.GetWeightMultiplier(piece, last10Average, saveManager.Current.DdaAvgScore);
    }

    // Single source of truth for vertical layout, shared by both the
    // ceramic's own placement and the camera sizing below — the Phase 1
    // camera-clipping bug happened specifically because two places
    // computed overlapping layout math independently and drifted apart.
    private static float GetGridHalfHeight()
    {
        return (Constants.GridSize * Constants.CellWorldSize) * 0.5f;
    }

    private static float GetCeramicCenterY()
    {
        return GetGridHalfHeight() + CeramicVerticalGap + CeramicAreaHalfHeight;
    }

    private static void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("GameplayController: no Main Camera found in the scene.");
            return;
        }

        // Phase 0's batch-mode `-createProject` has no CLI flag for
        // Unity's 2D template, so the scene's default camera came out
        // perspective — configuring it to orthographic here at runtime
        // rather than depending on a scene file hand-edit.
        camera.orthographic = true;

        float gridExtent = GetGridHalfHeight();
        float trayAllowance = TrayVerticalGap + Constants.CellWorldSize;
        float contentTop = GetCeramicCenterY() + CeramicAreaHalfHeight;
        float contentBottom = -gridExtent - trayAllowance;

        float centerY = (contentTop + contentBottom) * 0.5f;
        float sizeForHeight = ((contentTop - contentBottom) * 0.5f) + CameraPaddingCells;

        // A device screen is narrow (portrait), so the grid's own width
        // can be the binding constraint even though only vertical extent
        // was accounted for above — confirmed on-device in Phase 1:
        // sizing for height alone clipped both the grid's right edge and
        // the tray's left edge symmetrically. camera.aspect is the actual
        // runtime viewport ratio, not a guess.
        float sizeForWidth = (gridExtent + CameraPaddingCells) / camera.aspect;

        camera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
        camera.transform.position = new Vector3(0f, centerY, camera.transform.position.z);
    }
}
