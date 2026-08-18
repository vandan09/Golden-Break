using System;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// BUILD_PLAN Phase 4: "assert seed 20261122 produces a hardcoded known
/// first-20-pieces sequence (cross-platform stability check)." Uses the
/// real CLAUDE.md §3.1 piece-id/weight table (built by hand here, not
/// loaded via Resources, matching every other EditMode test in this
/// suite's convention of not depending on the asset pipeline) — cell
/// shapes don't affect weighted selection, only pieceId/spawnWeight do,
/// so the discovered sequence is exactly what the real Daily Challenge
/// would deal on 2026-11-22 with the shipped piece pool.
/// </summary>
public class DailyChallengeSeedTest
{
    private static PieceDefinition[] BuildRealWeightedPool()
    {
        return new[]
        {
            MakePiece("single", 12),
            MakePiece("1x2", 12),
            MakePiece("2x1", 12),
            MakePiece("2x2", 10),
            MakePiece("L", 10),
            MakePiece("J", 10),
            MakePiece("S", 10),
            MakePiece("Z", 10),
            MakePiece("T", 10),
            MakePiece("1x3", 8),
            MakePiece("3x1", 8),
            MakePiece("2x3", 8),
            MakePiece("3x2", 8),
            MakePiece("L3", 6),
            MakePiece("J3", 6),
            MakePiece("1x4", 5),
            MakePiece("4x1", 5),
            MakePiece("1x5", 3),
            MakePiece("5x1", 3),
            MakePiece("3x3", 3),
        };
    }

    private static PieceDefinition MakePiece(string id, int weight)
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = id;
        piece.cells = new[] { new Vector2Int(0, 0) };
        piece.spawnWeight = weight;
        return piece;
    }

    [Test]
    public void Seed20261122_ProducesHardcodedKnownFirstTwentyPieceSequence()
    {
        PieceDefinition[] pool = BuildRealWeightedPool();
        PieceSpawner spawner = DailyChallengeManager.CreateSpawner(pool, new DateTime(2026, 11, 22));

        // Captured once by running this exact pool/seed through
        // PieceSpawner and logging the output, then baked in here as the
        // permanent regression guard — if System.Random's algorithm ever
        // changed on some future .NET runtime/platform, this test would
        // catch it (CLAUDE.md §4.2's whole point: every device must deal
        // the same sequence).
        string[] expected =
        {
            "2x3", "2x1", "2x3", "L3", "J3", "1x3", "L", "L", "1x2", "2x2",
            "J", "1x2", "J", "1x5", "L", "1x2", "2x2", "single", "1x2", "3x1",
        };

        for (int i = 0; i < expected.Length; i++)
        {
            string actual = spawner.SpawnOne().pieceId;
            Assert.AreEqual(expected[i], actual, $"piece #{i} in the deterministic sequence for seed 20261122");
        }
    }

    [Test]
    public void Seed20261122_IsStableAcrossIndependentSpawnerInstances()
    {
        PieceDefinition[] poolOne = BuildRealWeightedPool();
        PieceDefinition[] poolTwo = BuildRealWeightedPool();
        var date = new DateTime(2026, 11, 22);

        PieceSpawner spawnerOne = DailyChallengeManager.CreateSpawner(poolOne, date);
        PieceSpawner spawnerTwo = DailyChallengeManager.CreateSpawner(poolTwo, date);

        for (int i = 0; i < 20; i++)
        {
            Assert.AreEqual(spawnerOne.SpawnOne().pieceId, spawnerTwo.SpawnOne().pieceId);
        }
    }
}
