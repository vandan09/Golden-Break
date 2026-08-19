using System;
using UnityEngine;

/// <summary>
/// Score, streak multiplier, and personal-best tracking (CLAUDE.md §3.2).
/// Plain C# — not a MonoBehaviour singleton, since ScoreManager isn't on
/// BUILD_PLAN Part 1's permitted-singleton list and has genuine per-game
/// state that doesn't fit a static class either. Owned/instantiated by
/// whatever orchestrates a game session (PieceController for now).
/// </summary>
public sealed class ScoreManager
{
    public event Action<int> OnScoreChanged;
    public event Action OnNewBest;

    public int CurrentScore { get; private set; }
    public int BestScore { get; private set; }
    public float StreakMultiplier { get; private set; } = 1f;

    public ScoreManager(int initialBestScore)
    {
        BestScore = initialBestScore;
    }

    public int ApplyLineClear(int linesCleared)
    {
        if (linesCleared <= 0)
        {
            StreakMultiplier = 1f;
            return 0;
        }

        int basePoints = CalculateBasePoints(linesCleared);
        int points = Mathf.RoundToInt(basePoints * StreakMultiplier);

        CurrentScore += points;
        OnScoreChanged?.Invoke(CurrentScore);

        if (CurrentScore > BestScore)
        {
            BestScore = CurrentScore;
            OnNewBest?.Invoke();
        }

        StreakMultiplier = Mathf.Min(StreakMultiplier + Constants.StreakMultiplierStep, Constants.StreakMultiplierMax);

        return points;
    }

    public void ResetForNewGame()
    {
        CurrentScore = 0;
        StreakMultiplier = 1f;
        OnScoreChanged?.Invoke(CurrentScore);
    }

    public static int CalculateBasePoints(int linesCleared)
    {
        switch (linesCleared)
        {
            case 0:
                return 0;
            case 1:
                return Constants.PointsPerSingleLine;
            case 2:
                return Constants.PointsPerDoubleLine;
            case 3:
                return Constants.PointsPerTripleLine;
            case 4:
                return Constants.PointsPerQuadLine;
            default:
                return Constants.PointsBaseForFivePlusLines + ((linesCleared - 5) * Constants.PointsPerAdditionalLineBeyondFive);
        }
    }
}
