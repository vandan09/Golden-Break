using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class RewardedAdControllerTests
{
    private GridManager _grid;
    private PieceTrayController _tray;
    private PieceController _controller;
    private PieceDefinition _singleCellPiece;
    private CoinManager _coins;
    private bool _adSucceeds;

    [SetUp]
    public void CreateAll()
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

        _coins = new CoinManager(initialBalance: 0);
        _adSucceeds = true;
    }

    [TearDown]
    public void DestroyAll()
    {
        Object.DestroyImmediate(_controller.gameObject);
        Object.DestroyImmediate(_tray.gameObject);
        Object.DestroyImmediate(_grid.gameObject);
    }

    private AdPlacement? _lastOfferedPlacement;

    private RewardedAdController CreateController()
    {
        _lastOfferedPlacement = null;
        return new RewardedAdController(
            (placement, onReward, onFailure) =>
            {
                _lastOfferedPlacement = placement;
                if (_adSucceeds)
                {
                    onReward();
                }
                else
                {
                    onFailure("simulated failure");
                }
            },
            _controller,
            _coins);
    }

    private void PlaceAt(int slotIndex, int x, int y)
    {
        Vector3 target = _grid.transform.TransformPoint(GridManager.CellToLocalPosition(x, y));
        _controller.BeginDrag(slotIndex, _tray.Slots[slotIndex].transform.position);
        _controller.UpdateDrag(target);
        _controller.EndDrag();
    }

    private void ForceGameOver()
    {
        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                if (!_grid.Board.IsFilled(x, y))
                {
                    _grid.Board.Place(_singleCellPiece, x, y, colourId: 0);
                }
            }
        }

        _controller.DealNewHand();
    }

    [Test]
    public void Constructor_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new RewardedAdController(null, _controller, _coins));
        Assert.Throws<System.ArgumentNullException>(() => new RewardedAdController((p, r, f) => { }, null, _coins));
        Assert.Throws<System.ArgumentNullException>(() => new RewardedAdController((p, r, f) => { }, _controller, null));
    }

    [Test]
    public void RequestContinue_NotGameOver_SkipsAdAndReturnsFalse()
    {
        var rewardedAdController = CreateController();
        bool? result = null;

        rewardedAdController.RequestContinue(r => result = r);

        Assert.IsFalse(result);
        Assert.IsNull(_lastOfferedPlacement, "ad should never have been offered — CanContinue was false");
    }

    [Test]
    public void RequestContinue_GameOverAndAdWatched_CallsTryContinueAndReturnsTrue()
    {
        ForceGameOver();
        var rewardedAdController = CreateController();
        bool? result = null;

        rewardedAdController.RequestContinue(r => result = r);

        Assert.IsTrue(result);
        Assert.AreEqual(AdPlacement.ContinueAfterGameOver, _lastOfferedPlacement);
        Assert.IsFalse(_controller.IsGameOver, "continue should have actually resolved the game-over");
    }

    [Test]
    public void RequestContinue_AdFails_DoesNotContinueAndReturnsFalse()
    {
        ForceGameOver();
        _adSucceeds = false;
        var rewardedAdController = CreateController();
        bool? result = null;

        rewardedAdController.RequestContinue(r => result = r);

        Assert.IsFalse(result);
        Assert.IsTrue(_controller.IsGameOver, "a failed ad must not grant the continue");
    }

    [Test]
    public void RequestDoubleCoins_AdWatched_EarnsBaseAmountAgain()
    {
        _coins.Earn(100);
        var rewardedAdController = CreateController();
        bool? result = null;

        rewardedAdController.RequestDoubleCoins(15, r => result = r);

        Assert.IsTrue(result);
        Assert.AreEqual(115, _coins.Balance);
        Assert.AreEqual(AdPlacement.DoubleCoins, _lastOfferedPlacement);
    }

    [Test]
    public void RequestDoubleCoins_ZeroOrNegativeBaseAmount_SkipsAdAndReturnsFalse()
    {
        var rewardedAdController = CreateController();
        bool? result = null;

        rewardedAdController.RequestDoubleCoins(0, r => result = r);

        Assert.IsFalse(result);
        Assert.IsNull(_lastOfferedPlacement);
    }

    [Test]
    public void RequestDoubleCoins_AdFails_DoesNotEarnAnything()
    {
        _coins.Earn(50);
        _adSucceeds = false;
        var rewardedAdController = CreateController();

        rewardedAdController.RequestDoubleCoins(15, null);

        Assert.AreEqual(50, _coins.Balance);
    }

    [Test]
    public void RequestFreeUndo_NoPlacementYet_SkipsAdAndReturnsFalse()
    {
        var rewardedAdController = CreateController();
        bool? result = null;

        rewardedAdController.RequestFreeUndo(r => result = r);

        Assert.IsFalse(result);
        Assert.IsNull(_lastOfferedPlacement);
    }

    [Test]
    public void RequestFreeUndo_ValidPlacementAndAdWatched_UndoesForFree()
    {
        PlaceAt(0, 3, 3);
        var rewardedAdController = CreateController();
        bool? result = null;

        rewardedAdController.RequestFreeUndo(r => result = r);

        Assert.IsTrue(result);
        Assert.AreEqual(AdPlacement.FreeUndo, _lastOfferedPlacement);
        Assert.IsFalse(_grid.Board.IsFilled(3, 3));
    }

    [Test]
    public void RequestFreeRefresh_AllUnplacedAndAdWatched_RefreshesForFree()
    {
        var rewardedAdController = CreateController();
        bool? result = null;

        rewardedAdController.RequestFreeRefresh(r => result = r);

        Assert.IsTrue(result);
        Assert.AreEqual(AdPlacement.FreePieceRefresh, _lastOfferedPlacement);
    }

    [Test]
    public void RequestFreeRefresh_AfterOnePiecePlaced_SkipsAdAndReturnsFalse()
    {
        PlaceAt(0, 3, 3);
        var rewardedAdController = CreateController();
        bool? result = null;

        rewardedAdController.RequestFreeRefresh(r => result = r);

        Assert.IsFalse(result);
        Assert.IsNull(_lastOfferedPlacement);
    }
}
