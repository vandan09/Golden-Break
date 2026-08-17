using System;

/// <summary>
/// Weighted-random piece selection (CLAUDE.md §3.1 weight table). Pool and
/// RNG are constructor dependencies so this is testable with a fixed pool
/// and seeded <see cref="Random"/>, without touching Resources/AssetDatabase.
/// The optional weight multiplier lets <see cref="DDAManager"/> bias
/// selection without mutating the shared <see cref="PieceDefinition"/>
/// assets.
/// </summary>
public sealed class PieceSpawner
{
    private readonly PieceDefinition[] _pool;
    private readonly Random _random;
    private readonly Func<PieceDefinition, float> _weightMultiplier;

    public PieceSpawner(PieceDefinition[] pool, Random random, Func<PieceDefinition, float> weightMultiplier = null)
    {
        if (pool == null || pool.Length == 0)
        {
            throw new ArgumentException("PieceSpawner: pool must contain at least one piece.", nameof(pool));
        }

        _pool = pool;
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _weightMultiplier = weightMultiplier ?? (_ => 1f);
    }

    public PieceDefinition[] DealHand(int handSize)
    {
        var hand = new PieceDefinition[handSize];
        for (int i = 0; i < handSize; i++)
        {
            hand[i] = SpawnOne();
        }

        return hand;
    }

    public PieceDefinition SpawnOne()
    {
        float totalWeight = 0f;
        foreach (PieceDefinition piece in _pool)
        {
            totalWeight += EffectiveWeight(piece);
        }

        if (totalWeight <= 0f)
        {
            throw new InvalidOperationException("PieceSpawner: total effective weight must be positive.");
        }

        double roll = _random.NextDouble() * totalWeight;
        float cumulative = 0f;

        foreach (PieceDefinition piece in _pool)
        {
            cumulative += EffectiveWeight(piece);
            if (roll < cumulative)
            {
                return piece;
            }
        }

        // Unreachable given totalWeight is the exact sum, but guards
        // against floating-point rounding at the boundary with a safe
        // fallback instead of a null reference downstream.
        return _pool[_pool.Length - 1];
    }

    private float EffectiveWeight(PieceDefinition piece)
    {
        return piece.spawnWeight * _weightMultiplier(piece);
    }
}
