using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class GameModeSwitcherTests
{
    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceController _controller;
    private PieceDefinition _regularPiece;
    private PieceDefinition _dailyPiece;
    private PieceSpawner _regularSpawner;

    [SetUp]
    public void CreateController()
    {
        _grid = new GameObject("Grid").AddComponent<GridManager>();
        _grid.BuildGrid();

        _tray = new GameObject("Tray").AddComponent<PieceTrayController>();
        _tray.BuildSlots();

        // Distinct piece IDs per pool so tests can prove which spawner a
        // given hand actually came from.
        _regularPiece = ScriptableObject.CreateInstance<PieceDefinition>();
        _regularPiece.pieceId = "regular";
        _regularPiece.cells = new[] { new Vector2Int(0, 0) };
        _regularPiece.spawnWeight = 1;

        _dailyPiece = ScriptableObject.CreateInstance<PieceDefinition>();
        _dailyPiece.pieceId = "daily";
        _dailyPiece.cells = new[] { new Vector2Int(0, 0) };
        _dailyPiece.spawnWeight = 1;

        _regularSpawner = new PieceSpawner(new[] { _regularPiece }, new Random(1));

        _controller = new GameObject("Controller").AddComponent<PieceController>();
        _controller.Configure(_grid, _tray, _regularSpawner, new ScoreManager(0));
    }

    [TearDown]
    public void DestroyAll()
    {
        Object.DestroyImmediate(_controller.gameObject);
        Object.DestroyImmediate(_tray.gameObject);
        Object.DestroyImmediate(_grid.gameObject);
    }

    private GameModeSwitcher CreateSwitcher()
    {
        return new GameModeSwitcher(_controller, _regularSpawner, handsAlreadyDealt =>
        {
            var spawner = new PieceSpawner(new[] { _dailyPiece }, new Random(42));
            for (int i = 0; i < handsAlreadyDealt; i++)
            {
                spawner.DealHand(Constants.PieceHandSize);
            }
            return spawner;
        });
    }

    private void PlaceAt(int slotIndex, int x, int y)
    {
        Vector3 target = _grid.transform.TransformPoint(GridManager.CellToLocalPosition(x, y));
        _controller.BeginDrag(slotIndex, _tray.Slots[slotIndex].transform.position);
        _controller.UpdateDrag(target);
        _controller.EndDrag();
    }

    [Test]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new GameModeSwitcher(null, _regularSpawner, i => _regularSpawner));
        Assert.Throws<System.ArgumentNullException>(() => new GameModeSwitcher(_controller, null, i => _regularSpawner));
        Assert.Throws<System.ArgumentNullException>(() => new GameModeSwitcher(_controller, _regularSpawner, null));
    }

    [Test]
    public void ResumeOrStartRegularGame_FirstCall_LeavesTheAlreadyDealtHandUntouched()
    {
        var switcher = CreateSwitcher();
        PieceDefinition[] handBefore = (PieceDefinition[])_controller.Hand.Clone();

        switcher.ResumeOrStartRegularGame();

        CollectionAssert.AreEqual(handBefore, _controller.Hand);
        Assert.IsFalse(_controller.IsDailyChallengeSession);
    }

    [Test]
    public void StartOrResumeDailyChallenge_FirstTime_StartsFreshFromTheDailyPool()
    {
        var switcher = CreateSwitcher();

        switcher.StartOrResumeDailyChallenge();

        Assert.IsTrue(_controller.IsDailyChallengeSession);
        foreach (PieceDefinition piece in _controller.Hand)
        {
            Assert.AreEqual("daily", piece.pieceId);
        }
    }

    [Test]
    public void SwitchingToDailyAndBackToRegular_PreservesRegularBoardExactly()
    {
        var switcher = CreateSwitcher();
        PlaceAt(0, 3, 3);
        int scoreBefore = _controller.Score.CurrentScore;

        switcher.StartOrResumeDailyChallenge();
        Assert.IsTrue(_controller.IsDailyChallengeSession, "sanity: switched into daily");

        switcher.ResumeOrStartRegularGame();

        Assert.IsFalse(_controller.IsDailyChallengeSession);
        Assert.IsTrue(_grid.Board.IsFilled(3, 3), "the regular game's placed piece must survive the round trip");
        Assert.AreEqual(scoreBefore, _controller.Score.CurrentScore);
    }

    [Test]
    public void SwitchingBackToDaily_ResumesInProgressAttemptExactly()
    {
        var switcher = CreateSwitcher();
        switcher.StartOrResumeDailyChallenge();
        PlaceAt(0, 5, 5); // progress within the daily attempt

        switcher.ResumeOrStartRegularGame(); // pause daily, switch to regular
        switcher.StartOrResumeDailyChallenge(); // switch back

        Assert.IsTrue(_controller.IsDailyChallengeSession);
        Assert.IsTrue(_grid.Board.IsFilled(5, 5), "in-progress daily placement must be resumed, not lost");
    }

    [Test]
    public void CompletedDailyChallenge_TappingPlayAgain_RestartsFreshNotResumingFinishedBoard()
    {
        var switcher = CreateSwitcher();
        switcher.StartOrResumeDailyChallenge();

        // Force the daily attempt to game-over by filling the board
        // completely, then re-dealing.
        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                _grid.Board.Place(_dailyPiece, x, y, colourId: 0);
            }
        }
        _controller.DealNewHand();
        Assert.IsTrue(_controller.IsGameOver, "sanity: daily attempt is over");

        switcher.ResumeOrStartRegularGame(); // leave the finished daily session
        switcher.StartOrResumeDailyChallenge(); // "try again"

        Assert.IsFalse(_controller.IsGameOver, "a fresh attempt must not still be game-over");
        Assert.IsFalse(_grid.Board.IsFilled(0, 0), "board must be cleared, not resuming the finished one");
    }

    [Test]
    public void RegularGameScoreAndStreakSurviveARoundTripThroughDailyChallenge()
    {
        var switcher = CreateSwitcher();

        // Score a clear in the regular game first (fills row 0).
        for (int x = 0; x < Constants.GridSize - 1; x++)
        {
            _grid.Board.Place(_regularPiece, x, 0, colourId: 0);
        }
        PlaceAt(0, Constants.GridSize - 1, 0);
        int scoreAfterClear = _controller.Score.CurrentScore;
        Assert.AreEqual(10, scoreAfterClear, "sanity: one clear should be worth 10");

        switcher.StartOrResumeDailyChallenge();
        switcher.ResumeOrStartRegularGame();

        Assert.AreEqual(scoreAfterClear, _controller.Score.CurrentScore);
    }
}
