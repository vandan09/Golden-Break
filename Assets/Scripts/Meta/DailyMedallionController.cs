using UnityEngine;

/// <summary>
/// A small, per-attempt kintsugi medallion for Daily Challenge — real
/// player feedback: without any visible "shape to fill," Daily Challenge
/// read as aimless compared to regular play's satisfying gold-crack
/// progress, and it wasn't obvious the run had a natural end at all.
///
/// Reuses <see cref="CeramicManager"/>/<see cref="CeramicView"/> exactly
/// as they already are — both are already generic crack-repair logic/
/// rendering with no gallery or save-persistence coupling of their own
/// (that lives entirely in <see cref="CeramicController"/>) — but wires
/// them up fresh per attempt instead: no <see cref="SaveManager"/>, no
/// <see cref="GalleryManager"/>, no tier advancement, nothing written to
/// the regular ceramic's persisted <c>current_ceramic</c>/<c>gallery</c>
/// save fields. Completing it mid-run is a bonus "perfect run" beat on
/// top of the normal ending, not a replacement for it — Daily Challenge
/// still ends exactly the way regular play does, when
/// <see cref="GameOverDetector"/> finds no valid move for the current
/// hand; this medallion has no bearing on that at all.
/// </summary>
public sealed class DailyMedallionController : MonoBehaviour
{
    private PieceController _pieceController;
    private CeramicView _view;
    private CeramicDefinition _definition;
    private CoinManager _coinManager;
    private CeramicManager _medallion;

    public bool CompletedThisAttempt { get; private set; }

    public void Configure(PieceController pieceController, CeramicView view, CeramicDefinition definition, CoinManager coinManager)
    {
        _pieceController = pieceController;
        _view = view;
        _definition = definition;
        _coinManager = coinManager;

        _pieceController.OnLinesCleared += OnLinesCleared;
        _pieceController.OnGameStarted += ResetForNewAttempt;

        ResetForNewAttempt();
    }

    // Fires on every Configure() and every RestartGame() (CLAUDE.md §4.2
    // "Play Again" replays the day fresh) — the medallion is scoped to a
    // single attempt, so it always starts back at 0/N cracks, never
    // carrying progress from a previous attempt the way the regular
    // ceramic deliberately does.
    private void ResetForNewAttempt()
    {
        CompletedThisAttempt = false;

        var progress = new CeramicProgressData
        {
            Tier = 1,
            TotalCracks = _definition != null ? _definition.totalCracks : 0,
            CracksRepaired = 0,
            ColourVariant = 0
        };
        _medallion = new CeramicManager(progress, cumulativeScoreThisCeramic: 0);
        _view.SetCeramic(_definition, cracksRepaired: 0, colourVariant: 0);
    }

    private void OnLinesCleared(LineClearDetector.ClearResult result, int pointsAwarded)
    {
        if (!result.AnyCleared)
        {
            return;
        }

        int cracksBeforeRepair = _medallion.Progress.CracksRepaired;
        bool completed = _medallion.RepairCracks(result.TotalLinesCleared);
        int cracksAfterRepair = _medallion.Progress.CracksRepaired;

        for (int crackIndex = cracksBeforeRepair; crackIndex < cracksAfterRepair; crackIndex++)
        {
            _view.AnimateCrackFill(crackIndex, null);
        }

        _view.UpdateProgressBar(cracksAfterRepair, _medallion.Progress.TotalCracks);

        if (completed)
        {
            CompletedThisAttempt = true;
            _coinManager?.Earn(Constants.CoinsForDailyPerfectRun);
            AudioManager.Instance?.PlaySound(SoundEffect.CeramicComplete);
            HapticManager.Trigger(HapticPattern.CeramicComplete);
            _view.PlayCompletionCelebration(null);
        }
    }
}
