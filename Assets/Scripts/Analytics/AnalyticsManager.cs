using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Wraps GameAnalytics SDK calls behind a stable API. No SDK is imported
/// and no real game key exists yet (placeholder — see PROGRESS.md
/// deviations); every call logs what would have been sent instead of
/// throwing, so gameplay/meta code can call LogEvent freely from Phase 1
/// onward without waiting on the real account.
/// </summary>
public sealed class AnalyticsManager : MonoBehaviour
{
    // TODO(analytics-setup): replace with the real GameAnalytics game key once the account exists.
    private const string GameKeyPlaceholder = "TODO_GAMEANALYTICS_GAME_KEY";

    public static AnalyticsManager Instance { get; private set; }

    private bool _sdkInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InitializeSdk()
    {
        // TODO(analytics-setup): call GameAnalytics.Initialize() here once
        // the SDK plugin is imported and GameKeyPlaceholder is replaced.
        _sdkInitialized = false;
        Debug.Log("AnalyticsManager: SDK not yet integrated — deferred until GameAnalytics account and game key exist.");
    }

    // CLAUDE.md §6: custom dimensions (country, DDA state, total games) —
    // real GameAnalytics exposes exactly 3 custom-dimension slots
    // (SetCustomDimension01/02/03), each a short string. total_games is
    // passed as an int and formatted here rather than pushing that
    // formatting choice onto every call site.
    public void SetCustomDimensions(string country, string ddaState, int totalGames)
    {
        try
        {
            if (!_sdkInitialized)
            {
                Debug.Log($"AnalyticsManager (stub): custom dimensions country={country} dda_state={ddaState} total_games={totalGames}");
                return;
            }

            // TODO(analytics-setup): real GameAnalytics.SetCustomDimension01/02/03 calls go here.
        }
        catch (Exception e)
        {
            Debug.LogError($"AnalyticsManager: failed to set custom dimensions — {e.Message}");
        }
    }

    public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
    {
        try
        {
            if (!_sdkInitialized)
            {
                Debug.Log($"AnalyticsManager (stub): {eventName} {FormatParameters(parameters)}");
                return;
            }

            // TODO(analytics-setup): real GameAnalytics.NewDesignEvent call goes here.
        }
        catch (Exception e)
        {
            Debug.LogError($"AnalyticsManager: failed to log event '{eventName}' — {e.Message}");
        }
    }

    public void LogError(string context, string message)
    {
        LogEvent("error", new Dictionary<string, object>
        {
            { "context", context },
            { "message", message }
        });
    }

    internal static string FormatParameters(IReadOnlyDictionary<string, object> parameters)
    {
        if (parameters == null || parameters.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (KeyValuePair<string, object> kvp in parameters)
        {
            sb.Append(kvp.Key).Append('=').Append(kvp.Value).Append(' ');
        }

        return sb.ToString();
    }
}
