/// <summary>
/// CLAUDE.md §3.1's game-over algorithm: for each piece in the current
/// hand, check every board cell for a valid placement. Game continues if
/// any piece fits anywhere; otherwise it's over. Pure logic, plain C#.
/// </summary>
public static class GameOverDetector
{
    public static bool HasAnyValidMove(BoardState board, PieceDefinition[] hand)
    {
        if (board == null || hand == null)
        {
            return false;
        }

        foreach (PieceDefinition piece in hand)
        {
            if (piece == null)
            {
                continue;
            }

            for (int y = 0; y < Constants.GridSize; y++)
            {
                for (int x = 0; x < Constants.GridSize; x++)
                {
                    if (board.CanPlace(piece, x, y))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static bool IsGameOver(BoardState board, PieceDefinition[] hand)
    {
        return !HasAnyValidMove(board, hand);
    }
}
