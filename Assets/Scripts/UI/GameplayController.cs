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
///
/// Builds two fully independent gameplay object graphs — a "regular"
/// one (Grid/Tray/PieceController/HUD/GameOverScreen/Ceramic) and a
/// "daily" one with its own Grid/Tray/PieceController/HUD/GameOverScreen
/// — each grouped under its own root GameObject that this class toggles
/// active/inactive when the player switches between them. Confirmed with
/// the player: Daily Challenge must be a genuinely separate, harder
/// challenge, not regular play reusing the same board/session with a
/// badge on it (see PieceController's own doc comment for the bug this
/// replaced). InputHandler is the one component both share — it owns no
/// game state of its own, just "who does a raw touch currently route to,"
/// so retargeting it when switching views doesn't reintroduce any
/// cross-mode state sharing.
/// </summary>
public sealed class GameplayController : MonoBehaviour
{
    private const string GameplaySceneName = "Gameplay";
    private const float TrayVerticalGap = 1.5f;
    private const float CameraPaddingCells = 1.5f;
    private const float CeramicVerticalGap = 0.6f;
    private const float CeramicAreaHalfHeight = 1.3f;

    private bool _dailyChallengeSessionActive;

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

        var coinManager = new CoinManager(saveManager.Current.Coins);

        var inputHandlerObject = new GameObject("InputHandler");
        var inputHandler = inputHandlerObject.AddComponent<InputHandler>();

        // ---- Regular play ----------------------------------------------
        var regularRoot = new GameObject("RegularRoot");

        var gridObject = new GameObject("Grid");
        gridObject.transform.SetParent(regularRoot.transform, false);
        var grid = gridObject.AddComponent<GridManager>();
        grid.BuildGrid();

        var trayObject = new GameObject("Tray");
        trayObject.transform.SetParent(regularRoot.transform, false);
        float trayY = -GetGridHalfHeight() - TrayVerticalGap;
        trayObject.transform.position = new Vector3(0f, trayY, 0f);
        var tray = trayObject.AddComponent<PieceTrayController>();
        tray.BuildSlots();

        var pieceControllerObject = new GameObject("PieceController");
        pieceControllerObject.transform.SetParent(regularRoot.transform, false);
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

        // Constructed for its subscription side effects only — nothing
        // else in this method needs to hold a reference to it, same as
        // CeramicController's own OnLinesCleared subscription pattern.
        var saveTriggers = new GameplaySaveTriggers(pieceController, coinManager, saveManager.Current, saveManager.Save);

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
        hudObject.transform.SetParent(regularRoot.transform, false);
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
            ceramicObject.transform.SetParent(regularRoot.transform, false);
            ceramicObject.transform.position = new Vector3(0f, GetCeramicCenterY(), 0f);
            var ceramicView = ceramicObject.AddComponent<CeramicView>();
            ceramicView.Initialize();

            var ceramicManager = new CeramicManager(saveManager.Current.CurrentCeramic, saveManager.Current.CeramicCumulativeScore);
            var galleryManager = new GalleryManager(saveManager.Current.Gallery);

            var ceramicControllerObject = new GameObject("CeramicController");
            ceramicControllerObject.transform.SetParent(regularRoot.transform, false);
            ceramicController = ceramicControllerObject.AddComponent<CeramicController>();
            ceramicController.Configure(pieceController, ceramicView, ceramicPool, ceramicManager, galleryManager, saveManager, coinManager);

            var galleryScreenObject = new GameObject("GalleryScreen");
            galleryScreen = galleryScreenObject.AddComponent<GalleryScreen>();
            galleryScreen.Configure(galleryManager, ceramicPool, inputHandler);
        }

        // Constructed for its subscription side effects only, same as
        // GameplaySaveTriggers above.
        _ = new AnalyticsEventWiring(pieceController, saveTriggers, saveManager.Current, ceramicController?.Ceramic);

        var streakPopupObject = new GameObject("StreakPopup");
        streakPopupObject.transform.SetParent(regularRoot.transform, false);
        var streakPopup = streakPopupObject.AddComponent<StreakPopup>();
        streakPopup.Configure();

        var gameOverObject = new GameObject("GameOverScreen");
        gameOverObject.transform.SetParent(regularRoot.transform, false);
        var gameOverScreen = gameOverObject.AddComponent<GameOverScreen>();
        gameOverScreen.Configure(pieceController, saveTriggers, rewardedAdController, interstitialController, streakPopup, ceramicController);

        // ---- Daily Challenge (fully separate session) -------------------
        var dailyRoot = new GameObject("DailyRoot");

        var dailyGridObject = new GameObject("DailyGrid");
        dailyGridObject.transform.SetParent(dailyRoot.transform, false);
        var dailyGrid = dailyGridObject.AddComponent<GridManager>();
        dailyGrid.BuildGrid();

        var dailyTrayObject = new GameObject("DailyTray");
        dailyTrayObject.transform.SetParent(dailyRoot.transform, false);
        dailyTrayObject.transform.position = new Vector3(0f, trayY, 0f);
        var dailyTray = dailyTrayObject.AddComponent<PieceTrayController>();
        dailyTray.BuildSlots();

        var dailyPieceControllerObject = new GameObject("DailyPieceController");
        dailyPieceControllerObject.transform.SetParent(dailyRoot.transform, false);
        var dailyPieceController = dailyPieceControllerObject.AddComponent<PieceController>();

        // No persisted "best score" of its own via ScoreManager — Daily
        // Challenge's notion of "best" is per calendar day
        // (SaveData.DailyBestScores), tracked by DailyChallengeSaveTriggers/
        // DailyChallengeHUD below, not by this in-attempt score engine.
        var dailyScoreManager = new ScoreManager(initialBestScore: 0);

        // Configure() here only stands the controller up (drag/ghost
        // views, event subscriptions) — the hand it deals is immediately
        // discarded, since dailyRoot starts inactive and the real first
        // attempt is dealt by StartOrReplayDailyAttempt below the first
        // time the player actually taps Play.
        dailyPieceController.Configure(dailyGrid, dailyTray, DailyChallengeManager.CreateSpawner(pool, System.DateTime.UtcNow), dailyScoreManager);

        var dailySaveTriggers = new DailyChallengeSaveTriggers(dailyPieceController, coinManager, saveManager.Current, saveManager.Save);

        // A small, per-attempt kintsugi medallion (confirmed with the
        // player: without any visible "shape to fill," Daily Challenge
        // read as aimless and it wasn't obvious it had a natural end).
        // Reuses tier 1's existing CeramicDefinition art — a fresh,
        // never-persisted CeramicManager instance, not the regular
        // ceramic's own persisted progress (see DailyMedallionController's
        // own doc comment). Only built if ceramic art actually loaded,
        // matching the same guard the regular ceramic block above uses.
        DailyMedallionController dailyMedallionController = null;
        if (ceramicPool.Length > 0)
        {
            CeramicDefinition medallionDefinition = FindCeramicDefinitionForTier(ceramicPool, tier: 1);

            var dailyMedallionObject = new GameObject("DailyMedallion");
            dailyMedallionObject.transform.SetParent(dailyRoot.transform, false);
            dailyMedallionObject.transform.position = new Vector3(0f, GetCeramicCenterY(), 0f);
            var dailyMedallionView = dailyMedallionObject.AddComponent<CeramicView>();
            dailyMedallionView.Initialize();

            var dailyMedallionControllerObject = new GameObject("DailyMedallionController");
            dailyMedallionControllerObject.transform.SetParent(dailyRoot.transform, false);
            dailyMedallionController = dailyMedallionControllerObject.AddComponent<DailyMedallionController>();
            dailyMedallionController.Configure(dailyPieceController, dailyMedallionView, medallionDefinition, coinManager);
        }

        var dailyHudObject = new GameObject("DailyChallengeHUD");
        dailyHudObject.transform.SetParent(dailyRoot.transform, false);
        var dailyHud = dailyHudObject.AddComponent<DailyChallengeHUD>();

        var dailyGameOverObject = new GameObject("DailyChallengeGameOverScreen");
        dailyGameOverObject.transform.SetParent(dailyRoot.transform, false);
        var dailyGameOverScreen = dailyGameOverObject.AddComponent<DailyChallengeGameOverScreen>();

        dailyRoot.SetActive(false);

        // ---- Home / overlays ---------------------------------------------
        var settingsScreenObject = new GameObject("SettingsScreen");
        var settingsScreen = settingsScreenObject.AddComponent<SettingsScreen>();
        settingsScreen.Configure(saveManager, inputHandler, iapManager);

        var dailyChallengeUiObject = new GameObject("DailyChallengeUI");
        var dailyChallengeUi = dailyChallengeUiObject.AddComponent<DailyChallengeUI>();
        dailyChallengeUi.Configure(saveManager, inputHandler, EnterDailyChallenge);

        var homeScreenObject = new GameObject("HomeScreen");
        var homeScreen = homeScreenObject.AddComponent<HomeScreen>();
        homeScreen.Configure(saveManager, inputHandler, galleryScreen, settingsScreen, dailyChallengeUi);

        dailyHud.Configure(dailyPieceController, saveManager, ExitDailyChallengeToHome);
        dailyGameOverScreen.Configure(dailyPieceController, dailySaveTriggers, dailyMedallionController, StartOrReplayDailyAttempt, ExitDailyChallengeToHome);

        // Real gap caught on-device: no way back to the main menu or to
        // exit once Play was tapped, and Gallery/Settings/Daily Challenge
        // opened invisibly behind Home's own still-active panel and never
        // received a single tap (see PROGRESS.md). All wired
        // post-construction since HomeScreen is built after them.
        hud.SetHomeScreen(homeScreen);
        galleryScreen?.SetHomeScreen(homeScreen);
        settingsScreen.SetHomeScreen(homeScreen);
        dailyChallengeUi.SetHomeScreen(homeScreen);

        var backButtonRouterObject = new GameObject("BackButtonRouter");
        var backButtonRouter = backButtonRouterObject.AddComponent<BackButtonRouter>();
        backButtonRouter.Configure(homeScreen, galleryScreen, settingsScreen, dailyChallengeUi, () => _dailyChallengeSessionActive, ExitDailyChallengeToHome);

        // Local functions below are referenced above by name — C# hoists
        // local functions within their enclosing method, so this is valid
        // despite appearing after the call sites.
        void EnterDailyChallenge()
        {
            regularRoot.SetActive(false);
            dailyRoot.SetActive(true);
            inputHandler.SetPieceController(dailyPieceController);
            _dailyChallengeSessionActive = true;
            StartOrReplayDailyAttempt();
        }

        void ExitDailyChallengeToHome()
        {
            dailyRoot.SetActive(false);
            regularRoot.SetActive(true);
            inputHandler.SetPieceController(pieceController);
            _dailyChallengeSessionActive = false;
            homeScreen.Show();
        }

        // Shared by "tap Play from the Daily Challenge screen" and
        // "tap Play Again on the Daily Challenge game-over screen" — both
        // must deal the identical seeded sequence from the very start
        // (CLAUDE.md §4.2), which needs a brand-new PieceSpawner each
        // time (see PieceController.RestartGame's own doc comment) plus
        // the day's obstacle layout re-applied before that first hand is
        // dealt.
        void StartOrReplayDailyAttempt()
        {
            System.DateTime today = System.DateTime.UtcNow;
            PieceSpawner freshSpawner = DailyChallengeManager.CreateSpawner(pool, today);
            dailyPieceController.RestartGame(freshSpawner, g => ApplyDailyObstacles(g, today));
            dailyHud.RefreshGhostAndBestTexts();
        }
    }

    private static CeramicDefinition FindCeramicDefinitionForTier(CeramicDefinition[] pool, int tier)
    {
        foreach (CeramicDefinition definition in pool)
        {
            if (definition.tier == tier)
            {
                return definition;
            }
        }

        return null;
    }

    // CLAUDE.md §4.2 hard-mode mechanic 2 (confirmed with the player): a
    // fixed set of cells starts already filled before the player's first
    // move, seeded from the same date as the piece sequence.
    private static void ApplyDailyObstacles(GridManager grid, System.DateTime date)
    {
        foreach (Vector2Int cell in DailyChallengeManager.GetObstacleCells(date))
        {
            grid.Board.FillCell(cell.x, cell.y, Constants.ObstacleColourId);
        }
    }

    // CLAUDE.md §3.7: recomputed from saveManager.Current on every call
    // (not captured once) so DDA weighting reflects the latest game-over
    // history even across a same-session "Play again" restart, which
    // reuses this same PieceSpawner instance rather than reconstructing
    // it.
    private static float ComputeDdaWeightMultiplier(PieceDefinition piece, SaveManager saveManager)
    {
        float last10Average = DDAManager.ComputeLast10Average(saveManager.Current.DdaLast10Scores);
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
