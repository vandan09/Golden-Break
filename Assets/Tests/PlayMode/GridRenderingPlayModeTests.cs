using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Investigates the open "placed pieces don't render" bug (see PROGRESS.md's
/// "OPEN BUG" section) by actually rendering a frame and reading back real
/// pixel colours, rather than just asserting C# SpriteRenderer state (which
/// GridManagerTests already does, and which reports everything as correct).
/// This is the first test in the project that renders real pixels — the gap
/// PROGRESS.md's own QA notes flagged as missing PlayMode coverage.
///
/// Renders to an explicit RenderTexture via Camera.Render() rather than
/// relying on WaitForEndOfFrame + the screen backbuffer — WaitForEndOfFrame
/// never fires in -batchmode (no display surface to present to), which hung
/// the first version of this test indefinitely.
/// </summary>
public class GridRenderingPlayModeTests
{
    private const int RenderSize = 512;

    private GameObject _cameraObject;
    private GameObject _gridObject;
    private Camera _camera;
    private GridManager _grid;
    private RenderTexture _renderTexture;

    [TearDown]
    public void Cleanup()
    {
        if (_camera != null) _camera.targetTexture = null;
        RenderTexture.active = null;
        if (_renderTexture != null) _renderTexture.Release();
        if (_cameraObject != null) Object.Destroy(_cameraObject);
        if (_gridObject != null) Object.Destroy(_gridObject);
    }

    private void BuildCameraAndGrid()
    {
        _cameraObject = new GameObject("TestCamera");
        _camera = _cameraObject.AddComponent<Camera>();
        _camera.orthographic = true;
        _camera.orthographicSize = 5f;
        _camera.transform.position = new Vector3(0f, 0f, -10f);
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Color.black;
        _camera.tag = "MainCamera";

        _renderTexture = new RenderTexture(RenderSize, RenderSize, 24);
        _camera.targetTexture = _renderTexture;

        _gridObject = new GameObject("Grid");
        _grid = _gridObject.AddComponent<GridManager>();
        _grid.BuildGrid();
    }

    private Color ReadPixelAtWorldPoint(Vector3 worldPoint)
    {
        _camera.Render();

        Vector3 screenPoint = _camera.WorldToScreenPoint(worldPoint);

        var tex = new Texture2D(RenderSize, RenderSize, TextureFormat.RGB24, false);
        RenderTexture.active = _renderTexture;
        tex.ReadPixels(new Rect(0, 0, RenderSize, RenderSize), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        int px = Mathf.Clamp(Mathf.RoundToInt(screenPoint.x), 0, RenderSize - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(screenPoint.y), 0, RenderSize - 1);
        Color pixel = tex.GetPixel(px, py);

        Debug.Log($"[PIXELTEST] RenderSize={RenderSize} worldPoint={worldPoint} screenPoint=({screenPoint.x:F1},{screenPoint.y:F1}) sampledAt=({px},{py}) pixel={pixel}");

        Object.Destroy(tex);
        return pixel;
    }

    [UnityTest]
    public IEnumerator EmptyCell_RendersAsEmptyCellFillColour()
    {
        BuildCameraAndGrid();

        yield return null;

        Vector3 worldPos = _gridObject.transform.TransformPoint(GridManager.CellToLocalPosition(3, 3));
        Color pixel = ReadPixelAtWorldPoint(worldPos);
        Color expected = UiPalette.EmptyCellFill;

        Assert.AreEqual(expected.r, pixel.r, 0.08f, $"R channel: expected ~{expected} got {pixel}");
        Assert.AreEqual(expected.g, pixel.g, 0.08f, $"G channel: expected ~{expected} got {pixel}");
        Assert.AreEqual(expected.b, pixel.b, 0.08f, $"B channel: expected ~{expected} got {pixel}");
    }

    [UnityTest]
    public IEnumerator PlacedPiece_ViaDirectBoardPlaceAndRefreshCell_RendersBlockColourOnScreen()
    {
        BuildCameraAndGrid();

        yield return null;

        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = "single";
        piece.cells = new[] { new Vector2Int(0, 0) };

        _grid.Board.Place(piece, 0, 0, colourId: 0);
        _grid.RefreshCell(0, 0);

        yield return null;

        Vector3 worldPos = _gridObject.transform.TransformPoint(GridManager.CellToLocalPosition(0, 0));
        Color pixel = ReadPixelAtWorldPoint(worldPos);
        Color expected = UiPalette.GetBlockColour(0);

        Assert.AreEqual(expected.r, pixel.r, 0.15f, $"R channel: expected ~{expected} got {pixel} — reproduces the 'placed piece invisible' bug if this fails");
        Assert.AreEqual(expected.g, pixel.g, 0.15f, $"G channel: expected ~{expected} got {pixel}");
        Assert.AreEqual(expected.b, pixel.b, 0.15f, $"B channel: expected ~{expected} got {pixel}");

        Object.DestroyImmediate(piece);
    }

    [UnityTest]
    public IEnumerator PlacedPiece_ViaFullDragLifecycle_RendersBlockColourOnScreen()
    {
        BuildCameraAndGrid();

        var trayObject = new GameObject("Tray");
        float trayY = -(Constants.GridSize * Constants.CellWorldSize * 0.5f) - 1.5f;
        trayObject.transform.position = new Vector3(0f, trayY, 0f);
        var tray = trayObject.AddComponent<PieceTrayController>();
        tray.BuildSlots();

        var pieceControllerObject = new GameObject("PieceController");
        var pieceController = pieceControllerObject.AddComponent<PieceController>();

        var pool = Resources.LoadAll<PieceDefinition>("PieceDefinitions");
        Assert.Greater(pool.Length, 0, "PieceDefinitions must exist under a Resources folder for this test to load them, same as GameplayController does at runtime.");

        var spawner = new PieceSpawner(pool, new System.Random(12345));
        var scoreManager = new ScoreManager(initialBestScore: 0);
        pieceController.Configure(_grid, tray, spawner, scoreManager);

        yield return null;

        int slotIndex = 0;
        Assert.IsNotNull(pieceController.Hand[slotIndex], "Slot 0 should have a dealt piece.");
        PieceDefinition dealtPiece = pieceController.Hand[slotIndex];

        Vector3 slotWorldPos = tray.Slots[slotIndex].transform.position;
        pieceController.BeginDrag(slotIndex, slotWorldPos);

        Vector3 targetOriginLocal = GridManager.CellToLocalPosition(0, 0);
        Vector3 targetWorldPos = _gridObject.transform.TransformPoint(targetOriginLocal);
        pieceController.UpdateDrag(targetWorldPos);

        yield return null;

        pieceController.EndDrag();

        yield return null;

        Vector2Int firstCell = dealtPiece.cells[0];
        Vector3 worldPos = _gridObject.transform.TransformPoint(GridManager.CellToLocalPosition(firstCell.x, firstCell.y));
        Color pixel = ReadPixelAtWorldPoint(worldPos);

        bool isFilled = _grid.Board.IsFilled(firstCell.x, firstCell.y);
        Debug.Log($"[PIXELTEST] Board.IsFilled({firstCell.x},{firstCell.y})={isFilled} after full drag lifecycle");
        Assert.IsTrue(isFilled, "Board should report the cell as filled after a valid placement — if this fails, the drag itself didn't land where expected, not a rendering issue.");

        Color expected = UiPalette.GetBlockColour(0);
        Assert.AreEqual(expected.r, pixel.r, 0.15f, $"R channel: expected ~{expected} got {pixel} — reproduces the bug via the exact same BeginDrag/UpdateDrag/EndDrag path real touch input uses");
        Assert.AreEqual(expected.g, pixel.g, 0.15f, $"G channel: expected ~{expected} got {pixel}");
        Assert.AreEqual(expected.b, pixel.b, 0.15f, $"B channel: expected ~{expected} got {pixel}");

        Object.Destroy(trayObject);
        Object.Destroy(pieceControllerObject);
    }
}
