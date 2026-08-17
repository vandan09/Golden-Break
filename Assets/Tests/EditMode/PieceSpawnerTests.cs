using System;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

public class PieceSpawnerTests
{
    private static PieceDefinition MakePiece(string id, int weight)
    {
        var piece = ScriptableObject.CreateInstance<PieceDefinition>();
        piece.pieceId = id;
        piece.cells = new[] { new Vector2Int(0, 0) };
        piece.spawnWeight = weight;
        return piece;
    }

    [Test]
    public void DealHand_RequestedSize_ReturnsThatManyPieces()
    {
        var pool = new[] { MakePiece("a", 1) };
        var spawner = new PieceSpawner(pool, new Random(1));

        PieceDefinition[] hand = spawner.DealHand(Constants.PieceHandSize);

        Assert.AreEqual(Constants.PieceHandSize, hand.Length);
        Assert.IsTrue(Array.TrueForAll(hand, p => p != null));
    }

    [Test]
    public void SpawnOne_SinglePieceInPool_AlwaysReturnsThatPiece()
    {
        PieceDefinition only = MakePiece("only", 5);
        var spawner = new PieceSpawner(new[] { only }, new Random(42));

        for (int i = 0; i < 20; i++)
        {
            Assert.AreSame(only, spawner.SpawnOne());
        }
    }

    [Test]
    public void SpawnOne_SameSeed_ProducesDeterministicSequence()
    {
        var pool = new[] { MakePiece("a", 10), MakePiece("b", 10), MakePiece("c", 10) };

        var spawnerOne = new PieceSpawner(pool, new Random(123));
        var spawnerTwo = new PieceSpawner(pool, new Random(123));

        for (int i = 0; i < 30; i++)
        {
            Assert.AreEqual(spawnerOne.SpawnOne().pieceId, spawnerTwo.SpawnOne().pieceId);
        }
    }

    [Test]
    public void SpawnOne_OverManySamples_RoughlyMatchesWeightRatio()
    {
        PieceDefinition heavy = MakePiece("heavy", 90);
        PieceDefinition light = MakePiece("light", 10);
        var spawner = new PieceSpawner(new[] { heavy, light }, new Random(7));

        int heavyCount = 0;
        const int sampleSize = 10000;
        for (int i = 0; i < sampleSize; i++)
        {
            if (spawner.SpawnOne() == heavy)
            {
                heavyCount++;
            }
        }

        float heavyRatio = heavyCount / (float)sampleSize;
        Assert.That(heavyRatio, Is.InRange(0.85f, 0.95f), $"expected ~90% heavy, got {heavyRatio:P1}");
    }

    [Test]
    public void SpawnOne_WeightMultiplierZeroForAPiece_NeverSelectsIt()
    {
        PieceDefinition excluded = MakePiece("excluded", 50);
        PieceDefinition included = MakePiece("included", 50);
        var spawner = new PieceSpawner(
            new[] { excluded, included },
            new Random(7),
            weightMultiplier: p => p == excluded ? 0f : 1f);

        for (int i = 0; i < 200; i++)
        {
            Assert.AreNotSame(excluded, spawner.SpawnOne());
        }
    }

    [Test]
    public void Constructor_EmptyPool_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new PieceSpawner(new PieceDefinition[0], new Random(1)));
    }

    [Test]
    public void Constructor_NullRandom_ThrowsArgumentNullException()
    {
        var pool = new[] { MakePiece("a", 1) };
        Assert.Throws<ArgumentNullException>(() => new PieceSpawner(pool, null));
    }
}
