using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Self-bootstraps the Gameplay scene. Phase 1 scope only: grid + pieces —
/// no HUD/score/kintsugi yet, those land in later phases. Loads the 20
/// <see cref="PieceDefinition"/> assets and wires
/// GridManager + PieceTrayController + PieceController + InputHandler
/// together, entirely at runtime (no hand-authored scene objects to edit
/// blind without an interactive Editor session — see PROGRESS.md).
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

        var gridObject = new GameObject("Grid");
        var grid = gridObject.AddComponent<GridManager>();
        grid.BuildGrid();

        var trayObject = new GameObject("Tray");
        float trayY = -((Constants.GridSize * Constants.CellWorldSize) * 0.5f) - TrayVerticalGap;
        trayObject.transform.position = new Vector3(0f, trayY, 0f);
        var tray = trayObject.AddComponent<PieceTrayController>();
        tray.BuildSlots();

        var pieceControllerObject = new GameObject("PieceController");
        var pieceController = pieceControllerObject.AddComponent<PieceController>();
        var spawner = new PieceSpawner(pool, new System.Random());
        pieceController.Configure(grid, tray, spawner);

        var inputHandlerObject = new GameObject("InputHandler");
        var inputHandler = inputHandlerObject.AddComponent<InputHandler>();
        inputHandler.Configure(pieceController, Camera.main);
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

        float gridExtent = (Constants.GridSize * Constants.CellWorldSize) * 0.5f;
        float trayAllowance = TrayVerticalGap + Constants.CellWorldSize;
        float sizeForHeight = gridExtent + trayAllowance + CameraPaddingCells;

        // A device screen is narrow (portrait), so the grid's own width
        // can be the binding constraint even though only vertical extent
        // (grid + tray) was accounted for above — confirmed on-device:
        // sizing for height alone clipped both the grid's right edge and
        // the tray's left edge symmetrically. camera.aspect is the actual
        // runtime viewport ratio, not a guess.
        float sizeForWidth = (gridExtent + CameraPaddingCells) / camera.aspect;

        camera.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
        camera.transform.position = new Vector3(0f, -trayAllowance * 0.5f, camera.transform.position.z);
    }
}
