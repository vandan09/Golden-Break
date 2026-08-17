using NUnit.Framework;
using UnityEngine;

public class GameOverDetectorTests
{
    private static PieceDefinition MakePiece(params Vector2Int[] cells)
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = "test";
        piece.cells = cells;
        piece.spawnWeight = 1;
        return piece;
    }

    private static void FillEntireBoard(BoardState board, PieceDefinition single)
    {
        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                board.Place(single, x, y, colourId: 0);
            }
        }
    }

    [Test]
    public void HasAnyValidMove_EmptyBoard_ReturnsTrue()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));

        Assert.IsTrue(GameOverDetector.HasAnyValidMove(board, new[] { single }));
    }

    [Test]
    public void HasAnyValidMove_CompletelyFullBoard_ReturnsFalse()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        FillEntireBoard(board, single);

        Assert.IsFalse(GameOverDetector.HasAnyValidMove(board, new[] { single }));
    }

    [Test]
    public void HasAnyValidMove_OneEmptyCellFitsSinglePieceHand_ReturnsTrue()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        FillEntireBoard(board, single);
        board.Clear();
        board.Place(single, 0, 0, colourId: 0);
        // Refill everything except (7,7).
        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                if (x == 7 && y == 7)
                {
                    continue;
                }

                if (!board.IsFilled(x, y))
                {
                    board.Place(single, x, y, colourId: 0);
                }
            }
        }

        Assert.IsTrue(GameOverDetector.HasAnyValidMove(board, new[] { single }));
    }

    [Test]
    public void HasAnyValidMove_OnlyOneEmptyCellButHandNeedsTwoAdjacent_ReturnsFalse()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        PieceDefinition domino = MakePiece(new Vector2Int(0, 0), new Vector2Int(1, 0));

        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                if (x == 7 && y == 7)
                {
                    continue;
                }

                board.Place(single, x, y, colourId: 0);
            }
        }

        Assert.IsFalse(GameOverDetector.HasAnyValidMove(board, new[] { domino }));
    }

    [Test]
    public void IsGameOver_IsExactInverseOfHasAnyValidMove()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));

        Assert.AreEqual(!GameOverDetector.HasAnyValidMove(board, new[] { single }), GameOverDetector.IsGameOver(board, new[] { single }));
    }

    [Test]
    public void HasAnyValidMove_NullBoardOrHand_ReturnsFalseWithoutThrowing()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));

        Assert.DoesNotThrow(() => GameOverDetector.HasAnyValidMove(null, new[] { single }));
        Assert.DoesNotThrow(() => GameOverDetector.HasAnyValidMove(board, null));
        Assert.IsFalse(GameOverDetector.HasAnyValidMove(null, new[] { single }));
        Assert.IsFalse(GameOverDetector.HasAnyValidMove(board, null));
    }

    [Test]
    public void HasAnyValidMove_HandContainsNullEntry_SkipsItWithoutThrowing()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));

        Assert.DoesNotThrow(() => GameOverDetector.HasAnyValidMove(board, new[] { null, single, null }));
        Assert.IsTrue(GameOverDetector.HasAnyValidMove(board, new[] { null, single, null }));
    }
}
