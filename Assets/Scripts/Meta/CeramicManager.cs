using System;

/// <summary>
/// Crack-repair and tier-progression logic (CLAUDE.md §3.4), operating on
/// a persisted <see cref="CeramicProgressData"/> (already part of the
/// save schema from Phase 0 — this is the first system to actually use it
/// for its own purpose). Plain C#, no MonoBehaviour/scene dependency.
///
/// Deliberately doesn't know about <see cref="CeramicDefinition"/> assets
/// (a Unity asset-loading concern) — the caller supplies the next tier's
/// crack count when advancing, keeping this class fully unit-testable
/// without Resources/AssetDatabase.
/// </summary>
public sealed class CeramicManager
{
    public event Action<int> OnCracksRepaired;
    public event Action OnCeramicCompleted;

    private CeramicProgressData _progress;
    private int _cumulativeScoreThisCeramic;

    public CeramicProgressData Progress => _progress;
    public int CumulativeScoreThisCeramic => _cumulativeScoreThisCeramic;

    public CeramicManager(CeramicProgressData progress, int cumulativeScoreThisCeramic)
    {
        _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        _cumulativeScoreThisCeramic = cumulativeScoreThisCeramic;
    }

    public void AddScore(int points)
    {
        _cumulativeScoreThisCeramic += points;
    }

    // CLAUDE.md §3.4: cracks/lines-to-complete are 1:1, so a 2+ line combo
    // repairs 2+ cracks simultaneously. Caps at the ceramic's total —
    // clearing more lines than remaining cracks doesn't overflow.
    public bool RepairCracks(int lineCount)
    {
        if (lineCount <= 0)
        {
            return false;
        }

        int remaining = _progress.TotalCracks - _progress.CracksRepaired;
        int repaired = Math.Min(lineCount, remaining);
        if (repaired <= 0)
        {
            return false;
        }

        _progress.CracksRepaired += repaired;
        OnCracksRepaired?.Invoke(repaired);

        bool completed = _progress.CracksRepaired >= _progress.TotalCracks;
        if (completed)
        {
            OnCeramicCompleted?.Invoke();
        }

        return completed;
    }

    // Called once the just-completed ceramic has been recorded into the
    // gallery, to start a fresh one at the next tier.
    public void AdvanceToNextTier(int nextTierTotalCracks)
    {
        int nextTier = _progress.Tier + 1;
        _progress = new CeramicProgressData
        {
            Tier = nextTier,
            TotalCracks = nextTierTotalCracks,
            CracksRepaired = 0,
            ColourVariant = CeramicTierResolver.ResolveColourVariant(nextTier)
        };
        _cumulativeScoreThisCeramic = 0;
    }
}
