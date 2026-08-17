using NUnit.Framework;
using UnityEngine;

public class PieceTrayControllerTests
{
    private PieceTrayController _tray;

    [SetUp]
    public void CreateTray()
    {
        var go = new GameObject("PieceTrayControllerTestInstance");
        _tray = go.AddComponent<PieceTrayController>();
        _tray.BuildSlots();
    }

    [TearDown]
    public void DestroyTray()
    {
        if (_tray != null)
        {
            Object.DestroyImmediate(_tray.gameObject);
        }
    }

    private static PieceDefinition MakePiece(string id)
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = id;
        piece.cells = new[] { new Vector2Int(0, 0) };
        piece.spawnWeight = 1;
        return piece;
    }

    [Test]
    public void BuildSlots_CreatesExactlyThreeSlots()
    {
        Assert.AreEqual(Constants.PieceHandSize, _tray.Slots.Length);
    }

    [Test]
    public void BuildSlots_SlotsAreEvenlySpacedAndOrderedLeftToRight()
    {
        float slot0X = _tray.Slots[0].transform.localPosition.x;
        float slot1X = _tray.Slots[1].transform.localPosition.x;
        float slot2X = _tray.Slots[2].transform.localPosition.x;

        Assert.Less(slot0X, slot1X);
        Assert.Less(slot1X, slot2X);
        Assert.AreEqual(slot1X - slot0X, slot2X - slot1X, 0.0001f, "slots should be evenly spaced");
    }

    [Test]
    public void BuildSlots_MiddleSlotIsCenteredOnZero()
    {
        Assert.AreEqual(0f, _tray.Slots[1].transform.localPosition.x, 0.0001f);
    }

    [Test]
    public void SetHand_ThreePieces_AssignsOneToEachSlotInOrder()
    {
        PieceDefinition a = MakePiece("a");
        PieceDefinition b = MakePiece("b");
        PieceDefinition c = MakePiece("c");

        _tray.SetHand(new[] { a, b, c }, new[] { 0, 1, 2 });

        Assert.AreSame(a, _tray.Slots[0].CurrentPiece);
        Assert.AreSame(b, _tray.Slots[1].CurrentPiece);
        Assert.AreSame(c, _tray.Slots[2].CurrentPiece);
    }

    [Test]
    public void SetHand_UsesTrayPieceScaleNotFullScale()
    {
        PieceDefinition twoCell = ScriptableObject.CreateInstance<PieceDefinition>();
        twoCell.pieceId = "pair";
        twoCell.cells = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) };

        _tray.SetHand(new[] { twoCell }, new[] { 0 });

        float spacing = _tray.Slots[0].ActiveBlocks[1].transform.localPosition.x - _tray.Slots[0].ActiveBlocks[0].transform.localPosition.x;
        Assert.AreEqual(Constants.CellWorldSize * Constants.TrayPieceScale, spacing, 0.0001f);
    }
}
