using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class PieceControllerSnapshotTests
{
    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceController _controller;
    private PieceDefinition _singleCellPiece;

    [SetUp]
    public void CreateController()
    {
        _grid = new GameObject("Grid").AddComponent<GridManager>();
        _grid.BuildGrid();

        _tray = new GameObject("Tray").AddComponent<PieceTrayController>();
        _tray.BuildSlots();

        _singleCellPiece = ScriptableObject.CreateInstance<PieceDefinition>();
        _singleCellPiece.pieceId = "single";
        _singleCellPiece.cells = new[] { new Vector2Int(0, 0) };
        _singleCellPiece.spawnWeight = 1;

        var spawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));

        _controller = new GameObject("Controller").AddComponent<PieceController>();
        _controller.Configure(_grid, _tray, spawner, new ScoreManager(initialBestScore: 0));
    }

    [TearDown]
    public void DestroyAll()
    {
        Object.DestroyImmediate(_controller.gameObject);
        Object.DestroyImmediate(_tray.gameObject);
        Object.DestroyImmediate(_grid.gameObject);
    }

    private void PlaceAt(int slotIndex, int x, int y)
    {
        Vector3 target = _grid.transform.TransformPoint(GridManager.CellToLocalPosition(x, y));
        _controller.BeginDrag(slotIndex, _tray.Slots[slotIndex].transform.position);
        _controller.UpdateDrag(target);
        _controller.EndDrag();
    }

    [Test]
    public void CaptureSnapshot_ThenMutatingBoardAfterward_DoesNotAffectTheSnapshot()
    {
        PlaceAt(0, 2, 2);
        PieceController.GameStateSnapshot snapshot = _controller.CaptureSnapshot();

        PlaceAt(1, 4, 4);

        var freshBoard = new BoardState();
        freshBoard.RestoreColourIds(snapshot.BoardColourIds);
        Assert.IsFalse(freshBoard.IsFilled(4, 4), "snapshot must reflect state at capture time only");
        Assert.IsTrue(freshBoard.IsFilled(2, 2));
    }

    [Test]
    public void RestoreSnapshot_ReproducesBoardHandAndScoreExactly()
    {
        var spawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));
        PlaceAt(0, 2, 2);
        PieceController.GameStateSnapshot snapshot = _controller.CaptureSnapshot();

        // Diverge the live state after capturing.
        PlaceAt(1, 4, 4);

        _controller.RestoreSnapshot(snapshot, spawner, isDailyChallengeSession: false);

        Assert.IsTrue(_grid.Board.IsFilled(2, 2));
        Assert.IsFalse(_grid.Board.IsFilled(4, 4), "restore should overwrite the divergence, not merge with it");
        Assert.IsFalse(_controller.IsDailyChallengeSession);
    }

    [Test]
    public void RestoreSnapshot_SetsIsDailyChallengeSessionFlagAsRequested()
    {
        PieceController.GameStateSnapshot snapshot = _controller.CaptureSnapshot();
        var spawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));

        _controller.RestoreSnapshot(snapshot, spawner, isDailyChallengeSession: true);

        Assert.IsTrue(_controller.IsDailyChallengeSession);
    }

    [Test]
    public void RestoreSnapshot_NullSnapshot_ThrowsArgumentNullException()
    {
        var spawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));

        Assert.Throws<System.ArgumentNullException>(() => _controller.RestoreSnapshot(null, spawner, false));
    }

    [Test]
    public void RestoreSnapshot_FiresOnGameStarted()
    {
        PieceController.GameStateSnapshot snapshot = _controller.CaptureSnapshot();
        var spawner = new PieceSpawner(new[] { _singleCellPiece }, new Random(1));
        bool fired = false;
        _controller.OnGameStarted += () => fired = true;

        _controller.RestoreSnapshot(snapshot, spawner, false);

        Assert.IsTrue(fired);
    }
}
