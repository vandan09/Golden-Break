using System;

/// <summary>
/// Interstitial cadence (CLAUDE.md §5.1): every 3rd game-over, none in
/// the first 3 games, capped at 6/day. Plain C#, operating on SaveData's
/// existing interstitial_counter/interstitial_today_count/
/// interstitial_today_date fields — same dependency-on-plain-data-plus-
/// delegate pattern as GameplaySaveTriggers/RewardedAdController, for the
/// same testability reason (AdManager.ShowInterstitial needs its own
/// MonoBehaviour lifecycle that EditMode tests can't reliably drive).
///
/// Call <see cref="RecordResolvedGameOver"/> once per game-over that has
/// actually finished (the player declined or exhausted their continue) —
/// not on every raw PieceController.OnGameOver firing, since a
/// continued game isn't "another" game-over for cadence purposes. The
/// exact call site is the game-over screen's own flow (a later UI slice),
/// which is the only place that knows whether continue was offered and
/// resolved.
/// </summary>
public sealed class InterstitialController
{
    private const int MinGamesBeforeFirst = 3;
    private const int CadenceInterval = 3;
    private const int DailyCap = 6;

    private readonly SaveData _saveData;
    private readonly Action _showInterstitial;

    public InterstitialController(SaveData saveData, Action showInterstitial)
    {
        _saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
        _showInterstitial = showInterstitial ?? throw new ArgumentNullException(nameof(showInterstitial));
    }

    public void RecordResolvedGameOver(string todayIso)
    {
        if (_saveData.InterstitialTodayDate != todayIso)
        {
            _saveData.InterstitialTodayDate = todayIso;
            _saveData.InterstitialTodayCount = 0;
        }

        _saveData.InterstitialCounter++;

        if (!IsEligibleNow())
        {
            return;
        }

        _saveData.InterstitialTodayCount++;
        _showInterstitial();
    }

    private bool IsEligibleNow()
    {
        if (_saveData.InterstitialCounter <= MinGamesBeforeFirst)
        {
            return false;
        }

        if (_saveData.InterstitialCounter % CadenceInterval != 0)
        {
            return false;
        }

        return _saveData.InterstitialTodayCount < DailyCap;
    }
}
