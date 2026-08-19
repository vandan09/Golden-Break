using System;
using System.Collections.Generic;
using NUnit.Framework;

public class DailyChallengeManagerTests
{
    [Test]
    public void ComputeSeed_KnownDate_MatchesSpecExample()
    {
        int seed = DailyChallengeSeed.ComputeSeed(new DateTime(2026, 11, 22));

        Assert.AreEqual(20261122, seed);
    }

    [Test]
    public void ComputeSeed_DifferentDates_ProduceDifferentSeeds()
    {
        int seedA = DailyChallengeSeed.ComputeSeed(new DateTime(2026, 11, 22));
        int seedB = DailyChallengeSeed.ComputeSeed(new DateTime(2026, 11, 23));

        Assert.AreNotEqual(seedA, seedB);
    }

    [Test]
    public void CreateSpawner_SameDate_ProducesIdenticalSequenceAcrossSeparateInstances()
    {
        PieceDefinition[] pool = { MakePiece("a", 10), MakePiece("b", 10), MakePiece("c", 10) };
        var date = new DateTime(2026, 11, 22);

        PieceSpawner spawnerOne = DailyChallengeManager.CreateSpawner(pool, date);
        PieceSpawner spawnerTwo = DailyChallengeManager.CreateSpawner(pool, date);

        for (int i = 0; i < 20; i++)
        {
            Assert.AreEqual(spawnerOne.SpawnOne().pieceId, spawnerTwo.SpawnOne().pieceId);
        }
    }

    [Test]
    public void IsCompletedToday_DateNotInList_ReturnsFalse()
    {
        var completed = new List<string> { "2026-08-17" };

        Assert.IsFalse(DailyChallengeManager.IsCompletedToday(completed, "2026-08-18"));
    }

    [Test]
    public void IsCompletedToday_DateInList_ReturnsTrue()
    {
        var completed = new List<string> { "2026-08-18" };

        Assert.IsTrue(DailyChallengeManager.IsCompletedToday(completed, "2026-08-18"));
    }

    [Test]
    public void RecordCompletion_FirstAttemptToday_AwardsCoinsAndRecordsBest()
    {
        var completed = new List<string>();
        var bestScores = new Dictionary<string, int>();

        DailyChallengeManager.CompletionResult result = DailyChallengeManager.RecordCompletion(completed, bestScores, "2026-08-18", 850);

        Assert.IsTrue(result.IsFirstCompletionToday);
        Assert.AreEqual(DailyChallengeManager.CompletionRewardCoins, result.CoinsAwarded);
        Assert.IsTrue(result.IsNewBestForToday);
        Assert.IsTrue(completed.Contains("2026-08-18"));
        Assert.AreEqual(850, bestScores["2026-08-18"]);
    }

    [Test]
    public void RecordCompletion_SecondAttemptSameDayLowerScore_DoesNotReAwardOrLowerBest()
    {
        var completed = new List<string>();
        var bestScores = new Dictionary<string, int>();
        DailyChallengeManager.RecordCompletion(completed, bestScores, "2026-08-18", 850);

        DailyChallengeManager.CompletionResult result = DailyChallengeManager.RecordCompletion(completed, bestScores, "2026-08-18", 400);

        Assert.IsFalse(result.IsFirstCompletionToday);
        Assert.AreEqual(0, result.CoinsAwarded, "coins are a one-time-per-day reward, not per-attempt");
        Assert.IsFalse(result.IsNewBestForToday);
        Assert.AreEqual(850, bestScores["2026-08-18"], "best score should not be lowered by a worse replay");
    }

    [Test]
    public void RecordCompletion_SecondAttemptSameDayHigherScore_UpdatesBestButStillNoCoins()
    {
        var completed = new List<string>();
        var bestScores = new Dictionary<string, int>();
        DailyChallengeManager.RecordCompletion(completed, bestScores, "2026-08-18", 850);

        DailyChallengeManager.CompletionResult result = DailyChallengeManager.RecordCompletion(completed, bestScores, "2026-08-18", 1200);

        Assert.IsFalse(result.IsFirstCompletionToday);
        Assert.AreEqual(0, result.CoinsAwarded);
        Assert.IsTrue(result.IsNewBestForToday);
        Assert.AreEqual(1200, bestScores["2026-08-18"]);
    }

    [Test]
    public void RecordCompletion_DifferentDay_IsTreatedAsANewFirstCompletion()
    {
        var completed = new List<string>();
        var bestScores = new Dictionary<string, int>();
        DailyChallengeManager.RecordCompletion(completed, bestScores, "2026-08-17", 500);

        DailyChallengeManager.CompletionResult result = DailyChallengeManager.RecordCompletion(completed, bestScores, "2026-08-18", 300);

        Assert.IsTrue(result.IsFirstCompletionToday);
        Assert.AreEqual(DailyChallengeManager.CompletionRewardCoins, result.CoinsAwarded);
        Assert.AreEqual(2, completed.Count);
    }

    [Test]
    public void ComputeGhostScore_SameDate_IsDeterministic()
    {
        var date = new DateTime(2026, 11, 22);

        int scoreOne = DailyChallengeManager.ComputeGhostScore(date);
        int scoreTwo = DailyChallengeManager.ComputeGhostScore(date);

        Assert.AreEqual(scoreOne, scoreTwo);
    }

    [Test]
    public void ComputeGhostScore_WithinDocumentedRange()
    {
        int score = DailyChallengeManager.ComputeGhostScore(new DateTime(2026, 11, 22));

        Assert.That(score, Is.InRange(400, 1199));
    }

    [Test]
    public void GetHardPool_OnlyReturnsLargeSizeTierPieces()
    {
        PieceDefinition[] pool =
        {
            MakePiece("single", 12), // small
            MakePiece("2x2", 10),    // small
            MakePiece("1x5", 3),     // large
            MakePiece("3x3", 3),     // large
        };

        PieceDefinition[] hardPool = DailyChallengeManager.GetHardPool(pool);

        Assert.AreEqual(2, hardPool.Length);
        CollectionAssert.AreEquivalent(new[] { "1x5", "3x3" }, Array.ConvertAll(hardPool, p => p.pieceId));
    }

    [Test]
    public void CreateSpawner_OnlyDealsFromTheHardPool()
    {
        PieceDefinition[] pool = { MakePiece("single", 12), MakePiece("3x3", 3) };
        PieceSpawner spawner = DailyChallengeManager.CreateSpawner(pool, new DateTime(2026, 11, 22));

        for (int i = 0; i < 20; i++)
        {
            Assert.AreEqual("3x3", spawner.SpawnOne().pieceId, "the small 'single' piece must never be dealt by a Daily Challenge spawner");
        }
    }

    [Test]
    public void GetObstacleCells_ReturnsExactlyTheConfiguredCount()
    {
        UnityEngine.Vector2Int[] cells = DailyChallengeManager.GetObstacleCells(new DateTime(2026, 11, 22));

        Assert.AreEqual(Constants.DailyChallengeObstacleCellCount, cells.Length);
    }

    [Test]
    public void GetObstacleCells_AllCellsAreWithinGridBounds()
    {
        UnityEngine.Vector2Int[] cells = DailyChallengeManager.GetObstacleCells(new DateTime(2026, 11, 22));

        foreach (UnityEngine.Vector2Int cell in cells)
        {
            Assert.That(cell.x, Is.InRange(0, Constants.GridSize - 1));
            Assert.That(cell.y, Is.InRange(0, Constants.GridSize - 1));
        }
    }

    [Test]
    public void GetObstacleCells_SameDate_IsDeterministic()
    {
        var date = new DateTime(2026, 11, 22);

        UnityEngine.Vector2Int[] cellsOne = DailyChallengeManager.GetObstacleCells(date);
        UnityEngine.Vector2Int[] cellsTwo = DailyChallengeManager.GetObstacleCells(date);

        CollectionAssert.AreEqual(cellsOne, cellsTwo);
    }

    [Test]
    public void GetObstacleCells_DifferentDates_ProduceDifferentLayouts()
    {
        UnityEngine.Vector2Int[] cellsA = DailyChallengeManager.GetObstacleCells(new DateTime(2026, 11, 22));
        UnityEngine.Vector2Int[] cellsB = DailyChallengeManager.GetObstacleCells(new DateTime(2026, 11, 23));

        CollectionAssert.AreNotEqual(cellsA, cellsB);
    }

    private static PieceDefinition MakePiece(string id, int weight)
    {
        var piece = UnityEngine.ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = id;
        piece.cells = new[] { new UnityEngine.Vector2Int(0, 0) };
        piece.spawnWeight = weight;
        return piece;
    }
}
