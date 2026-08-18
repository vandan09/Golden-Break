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

    // CLAUDE.md §3.1's own real 20-piece set (§3.9 deviations record the
    // exact corrected cell coordinates) — edge cases beyond single/domino,
    // per BUILD_PLAN Phase 4's task list.

    private static PieceDefinition MakeLTromino()
    {
        // "L" per PROGRESS.md deviations: missing bottom-right of a 2x2.
        return MakePiece(new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1));
    }

    private static PieceDefinition MakeThreeByThree()
    {
        var cells = new Vector2Int[9];
        int i = 0;
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                cells[i++] = new Vector2Int(x, y);
            }
        }
        return MakePiece(cells);
    }

    [Test]
    public void HasAnyValidMove_ThreeByThreePiece_OnlyFitsAWhole3x3EmptyBlock()
    {
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        PieceDefinition threeByThree = MakeThreeByThree();

        FillEntireBoard(board, single);
        // Open a 2x3 gap at the top-left — one row short of the 3x3 the
        // piece actually needs.
        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                board.ClearCell(x, y);
            }
        }

        Assert.IsFalse(GameOverDetector.HasAnyValidMove(board, new[] { threeByThree }), "a 2x3 gap must not satisfy a 3x3 piece");

        // Now open the full 3x3.
        for (int x = 0; x < 3; x++)
        {
            board.ClearCell(x, 2);
        }

        Assert.IsTrue(GameOverDetector.HasAnyValidMove(board, new[] { threeByThree }));
    }

    [Test]
    public void HasAnyValidMove_CheckerboardFragmentation_DefeatsAnLTromino()
    {
        // No two adjacent cells are ever both free, so an L-tromino
        // (which needs a bent run of 3 connected cells) can never fit
        // anywhere, even though 32 individual cells remain open.
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        PieceDefinition lTromino = MakeLTromino();

        for (int y = 0; y < Constants.GridSize; y++)
        {
            for (int x = 0; x < Constants.GridSize; x++)
            {
                if ((x + y) % 2 == 0)
                {
                    board.Place(single, x, y, colourId: 0);
                }
            }
        }

        Assert.IsFalse(GameOverDetector.HasAnyValidMove(board, new[] { lTromino }));
    }

    [Test]
    public void HasAnyValidMove_OnlyOnePieceInHandFitsTheRemainingGap_ReturnsTrue()
    {
        // A 3-wide, 1-tall gap at the bottom row: a 3x3 and an L-tromino
        // both fail (need vertical room this gap doesn't have), but a
        // plain 1x3 fits exactly. Proves the detector checks every piece
        // in the hand independently rather than stopping at the first
        // failure or requiring all of them to fit.
        var board = new BoardState();
        PieceDefinition single = MakePiece(new Vector2Int(0, 0));
        PieceDefinition threeByThree = MakeThreeByThree();
        PieceDefinition lTromino = MakeLTromino();
        PieceDefinition oneByThree = MakePiece(new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0));

        FillEntireBoard(board, single);
        for (int x = 0; x < 3; x++)
        {
            board.ClearCell(x, Constants.GridSize - 1);
        }

        Assert.IsTrue(GameOverDetector.HasAnyValidMove(board, new[] { threeByThree, lTromino, oneByThree }));
        Assert.IsFalse(GameOverDetector.HasAnyValidMove(board, new[] { threeByThree, lTromino }), "sanity: without the 1x3, neither remaining piece should fit the 1-tall gap");
    }
}
