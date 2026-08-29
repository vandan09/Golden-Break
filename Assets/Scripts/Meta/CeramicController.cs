using System;
using UnityEngine;

/// <summary>
/// Orchestrates the kintsugi meta (CLAUDE.md §3.4): listens to
/// <see cref="PieceController.OnLinesCleared"/> externally rather than
/// PieceController knowing anything about ceramics — keeps the core
/// gameplay loop ceramic-agnostic, matching BUILD_PLAN Part 1's
/// dependency-direction rule (Core doesn't reach into Meta; Meta reacts
/// to Core's events instead).
///
/// Repairs cracks proportional to lines cleared, tracks cumulative score
/// for the gallery, and on completion records a gallery entry and
/// advances to the next tier. Persists after every repair/completion so
/// ceramic progress survives a game-over (§3.4's explicit "does NOT
/// reset" requirement) — this is the one piece of Phase 4-scoped save
/// persistence pulled forward early, since Phase 3's own acceptance
/// criteria can't be met without it.
/// </summary>
public sealed class CeramicController : MonoBehaviour
{
    private PieceController _pieceController;
    private CeramicView _view;
    private CeramicDefinition[] _ceramicPool;
    private CeramicManager _ceramicManager;
    private GalleryManager _galleryManager;
    private SaveManager _saveManager;
    private CoinManager _coinManager;

    public CeramicManager Ceramic => _ceramicManager;
    public GalleryManager Gallery => _galleryManager;

    public void Configure(
        PieceController pieceController,
        CeramicView view,
        CeramicDefinition[] ceramicPool,
        CeramicManager ceramicManager,
        GalleryManager galleryManager,
        SaveManager saveManager,
        CoinManager coinManager)
    {
        _pieceController = pieceController;
        _view = view;
        _ceramicPool = ceramicPool;
        _ceramicManager = ceramicManager;
        _galleryManager = galleryManager;
        _saveManager = saveManager;
        _coinManager = coinManager;

        _pieceController.OnLinesCleared += OnLinesCleared;

        ReconcileProgressWithArtwork();
        RefreshView();
    }

    // A saved ceramic carries the crack total it was started with. When the
    // artwork changes that count — as the move from 3 shared silhouettes to
    // the design's 9 authored ones did, taking the vase from 3 cracks to 7 —
    // an existing save would otherwise show a ceramic drawing 7 cracks while
    // completing after 3, or report "5 of 3". Rather than resetting the
    // player's progress, the total is adopted from the current artwork and
    // the repaired count clamped into it, so a ceramic already finished
    // under the old count stays finished and one mid-repair keeps its
    // repaired cracks.
    private void ReconcileProgressWithArtwork()
    {
        CeramicProgressData progress = _ceramicManager.Progress;
        int definitionTier = CeramicTierResolver.ResolveDefinitionTier(progress.Tier);
        CeramicDefinition definition = FindDefinitionForTier(definitionTier);

        if (definition == null || definition.totalCracks <= 0)
        {
            return;
        }

        if (progress.TotalCracks == definition.totalCracks)
        {
            return;
        }

        bool wasComplete = progress.TotalCracks > 0 && progress.CracksRepaired >= progress.TotalCracks;

        Debug.Log(
            $"CeramicController: tier {progress.Tier} artwork now has {definition.totalCracks} cracks "
            + $"(save had {progress.TotalCracks}) — reconciling progress.");

        progress.TotalCracks = definition.totalCracks;
        progress.CracksRepaired = wasComplete
            ? definition.totalCracks
            : Mathf.Clamp(progress.CracksRepaired, 0, definition.totalCracks);

        Persist();
    }

    private void OnLinesCleared(LineClearDetector.ClearResult result, int pointsAwarded)
    {
        if (!result.AnyCleared)
        {
            return;
        }

        _ceramicManager.AddScore(pointsAwarded);

        int cracksBeforeRepair = _ceramicManager.Progress.CracksRepaired;
        bool completed = _ceramicManager.RepairCracks(result.TotalLinesCleared);
        int cracksAfterRepair = _ceramicManager.Progress.CracksRepaired;

        for (int crackIndex = cracksBeforeRepair; crackIndex < cracksAfterRepair; crackIndex++)
        {
            _view.AnimateCrackFill(crackIndex, null);
        }

        _view.UpdateProgressBar(cracksAfterRepair, _ceramicManager.Progress.TotalCracks);
        Persist();

        if (completed)
        {
            HandleCompletion();
        }
    }

    private void HandleCompletion()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        _galleryManager.AddCompletedCeramic(_ceramicManager.Progress.Tier, today, _ceramicManager.CumulativeScoreThisCeramic);

        // CLAUDE.md §4.5: "Ceramic completed | 25 coins". Earn() fires
        // CoinManager.OnBalanceChanged synchronously, which
        // GameplaySaveTriggers already turns into its own "coin change"
        // save — the Persist() call below (for cracks/tier/gallery state)
        // runs immediately after, so both land in the same on-disk save.
        _coinManager?.Earn(Constants.CoinsForCeramicCompleted);

        AudioManager.Instance?.PlaySound(SoundEffect.CeramicComplete);
        HapticManager.Trigger(HapticPattern.CeramicComplete);

        _view.PlayCompletionCelebration(() =>
        {
            int nextDefinitionTier = CeramicTierResolver.ResolveDefinitionTier(_ceramicManager.Progress.Tier + 1);
            CeramicDefinition nextDefinition = FindDefinitionForTier(nextDefinitionTier);
            int nextTotalCracks = nextDefinition != null ? nextDefinition.totalCracks : _ceramicManager.Progress.TotalCracks;

            _ceramicManager.AdvanceToNextTier(nextTotalCracks);
            Persist();
            RefreshView();
        });
    }

    private void RefreshView()
    {
        int definitionTier = CeramicTierResolver.ResolveDefinitionTier(_ceramicManager.Progress.Tier);
        CeramicDefinition definition = FindDefinitionForTier(definitionTier);
        _view.SetCeramic(definition, _ceramicManager.Progress.CracksRepaired, _ceramicManager.Progress.ColourVariant);
    }

    private CeramicDefinition FindDefinitionForTier(int definitionTier)
    {
        foreach (CeramicDefinition definition in _ceramicPool)
        {
            if (definition.tier == definitionTier)
            {
                return definition;
            }
        }

        Debug.LogError($"CeramicController: no CeramicDefinition found for tier {definitionTier}.");
        return null;
    }

    private void Persist()
    {
        if (_saveManager == null || _saveManager.Current == null)
        {
            return;
        }

        _saveManager.Current.CurrentCeramic = _ceramicManager.Progress;
        _saveManager.Current.CeramicCumulativeScore = _ceramicManager.CumulativeScoreThisCeramic;
        _saveManager.Current.Gallery = new System.Collections.Generic.List<GalleryEntryData>(_galleryManager.Entries);
        _saveManager.Save();
    }
}
