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

    private static PieceDefinition MakePiece(string id, int weight)
    {
        var piece = UnityEngine.ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = id;
        piece.cells = new[] { new UnityEngine.Vector2Int(0, 0) };
        piece.spawnWeight = weight;
        return piece;
    }
}
