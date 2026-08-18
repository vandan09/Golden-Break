using System;

/// <summary>
/// Keeps regular play and a Daily Challenge session (CLAUDE.md §4.2)
/// genuinely independent — switching to one snapshots and preserves the
/// other, so both are resumable exactly where they were left. A real gap
/// caught on-device: previously, opening Daily Challenge silently wiped
/// an in-progress regular game's board with no way back (see
/// PROGRESS.md).
///
/// Plain C#, not a MonoBehaviour — pure orchestration over
/// PieceController's snapshot/restore API, fully unit-testable.
/// </summary>
public sealed class GameModeSwitcher
{
    private readonly PieceController _pieceController;
    private readonly PieceSpawner _regularSpawner;
    private readonly Func<int, PieceSpawner> _buildDailySpawnerFastForwardedTo;

    private PieceController.GameStateSnapshot _regularSnapshot;
    private PieceController.GameStateSnapshot _dailyChallengeSnapshot;
    private bool _hasStartedRegularGame;

    // buildDailySpawnerFastForwardedTo(handsAlreadyDealt) must return a
    // spawner seeded for *today* and fast-forwarded past that many hands
    // (a fresh System.Random(seed) always restarts at the beginning of
    // the sequence, so resuming mid-session requires replaying past
    // draws) — the caller owns "what day is it" and the piece pool, this
    // class only orchestrates when to call it.
    public GameModeSwitcher(PieceController pieceController, PieceSpawner regularSpawner, Func<int, PieceSpawner> buildDailySpawnerFastForwardedTo)
    {
        _pieceController = pieceController ?? throw new ArgumentNullException(nameof(pieceController));
        _regularSpawner = regularSpawner ?? throw new ArgumentNullException(nameof(regularSpawner));
        _buildDailySpawnerFastForwardedTo = buildDailySpawnerFastForwardedTo ?? throw new ArgumentNullException(nameof(buildDailySpawnerFastForwardedTo));
    }

    // Called when the player taps "Play" from Home.
    public void ResumeOrStartRegularGame()
    {
        if (_pieceController.IsDailyChallengeSession)
        {
            _dailyChallengeSnapshot = _pieceController.CaptureSnapshot();
        }
        else if (_hasStartedRegularGame)
        {
            return; // already the active session — Play just resumes it, nothing to switch
        }

        if (_regularSnapshot != null)
        {
            _pieceController.RestoreSnapshot(_regularSnapshot, _regularSpawner, isDailyChallengeSession: false);
            _regularSnapshot = null;
        }
        // else: the very first regular game was already dealt by
        // PieceController.Configure() — nothing to restore.

        _hasStartedRegularGame = true;
    }

    // Called when the player taps "Play" inside the Daily Challenge
    // panel. Resumes an in-progress attempt exactly where it was left —
    // but a *completed* one (game-over already reached) restarts fresh
    // from the same seed instead of resuming the finished board, matching
    // BUILD_PLAN's own QA gate: "play daily challenge twice in one day —
    // second attempt shows same piece sequence." Those are two different
    // things: pausing mid-attempt should resume; trying again after
    // finishing should replay the same deterministic sequence from the
    // start.
    public void StartOrResumeDailyChallenge()
    {
        if (!_pieceController.IsDailyChallengeSession)
        {
            _regularSnapshot = _pieceController.CaptureSnapshot();
            _hasStartedRegularGame = true;
        }

        if (_dailyChallengeSnapshot != null && !_dailyChallengeSnapshot.IsGameOver)
        {
            PieceController.GameStateSnapshot snapshot = _dailyChallengeSnapshot;
            _dailyChallengeSnapshot = null;
            PieceSpawner resumedSpawner = _buildDailySpawnerFastForwardedTo(snapshot.HandsDealtThisSession);
            _pieceController.RestoreSnapshot(snapshot, resumedSpawner, isDailyChallengeSession: true);
        }
        else
        {
            _dailyChallengeSnapshot = null;
            PieceSpawner freshSpawner = _buildDailySpawnerFastForwardedTo(0);
            _pieceController.StartDailyChallenge(freshSpawner);
        }
    }
}
