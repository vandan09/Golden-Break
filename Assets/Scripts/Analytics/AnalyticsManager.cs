using System;
using System.Collections.Generic;
using System.Text;
using Firebase;
using Firebase.Analytics;
using UnityEngine;

/// <summary>
/// Wraps Firebase Analytics behind a stable API so gameplay and meta code
/// can call <see cref="LogEvent"/> without knowing which service is behind
/// it — which is what made swapping the planned GameAnalytics for Firebase
/// a change to this file alone.
///
/// Firebase over GameAnalytics (§6.2 named the latter, inherited from
/// GLYPH): ads moved to AdMob, and Firebase links to AdMob so ad revenue
/// can be read against player behaviour in one place. GameAnalytics cannot
/// see AdMob earnings at all, which is the exact question this game's
/// monetization needs answered.
///
/// Every call is failure-tolerant. Analytics must never be able to break
/// gameplay: if the SDK is unavailable, its dependencies are missing, or an
/// event is malformed, the call logs and returns rather than throwing into
/// a caller that was only trying to record a score.
/// </summary>
public sealed class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    private bool _sdkInitialized;

    // Firebase reserves parameter names beginning with "firebase_",
    // "google_" and "ga_", and silently drops events that use them.
    private static readonly string[] ReservedPrefixes = { "firebase_", "google_", "ga_" };

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
        if (_sdkInitialized)
        {
            return;
        }

        // Firebase checks for Google Play services and may need to update
        // them, so initialisation is asynchronous and can genuinely fail on
        // a device without them. A failure leaves _sdkInitialized false,
        // which downgrades every later call to a log line rather than an
        // exception.
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning($"AnalyticsManager: Firebase dependency check failed — {task.Exception?.Message}");
                return;
            }

            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogWarning($"AnalyticsManager: Firebase unavailable — {task.Result}");
                return;
            }

            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            _sdkInitialized = true;
            Debug.Log("AnalyticsManager: Firebase Analytics initialized.");
        });
    }

    /// <summary>
    /// CLAUDE.md §6's custom dimensions (country, DDA state, total games).
    /// GameAnalytics offered exactly three dimension slots; Firebase instead
    /// uses named user properties, so these map to properties rather than
    /// numbered slots — same data, no arbitrary limit of three.
    ///
    /// Country is deliberately NOT sent: Firebase derives it from the
    /// device already, and sending it again would be duplicate personal
    /// data for no benefit.
    /// </summary>
    public void SetCustomDimensions(string country, string ddaState, int totalGames)
    {
        try
        {
            if (!_sdkInitialized)
            {
                Debug.Log($"AnalyticsManager (not ready): dimensions dda_state={ddaState} total_games={totalGames}");
                return;
            }

            FirebaseAnalytics.SetUserProperty("dda_state", ddaState ?? string.Empty);
            FirebaseAnalytics.SetUserProperty("total_games", totalGames.ToString());
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
                Debug.Log($"AnalyticsManager (not ready): {eventName} {FormatParameters(parameters)}");
                return;
            }

            if (parameters == null || parameters.Count == 0)
            {
                FirebaseAnalytics.LogEvent(eventName);
                return;
            }

            FirebaseAnalytics.LogEvent(eventName, ToFirebaseParameters(parameters));
        }
        catch (Exception e)
        {
            Debug.LogError($"AnalyticsManager: failed to log event '{eventName}' — {e.Message}");
        }
    }

    // Firebase takes typed parameters rather than boxed objects, so each
    // value is routed to the matching overload. Anything unrecognised
    // becomes a string rather than being dropped — a slightly less useful
    // parameter beats a silently missing one when reading a funnel later.
    private static Parameter[] ToFirebaseParameters(IReadOnlyDictionary<string, object> parameters)
    {
        var result = new List<Parameter>(parameters.Count);

        foreach (KeyValuePair<string, object> kvp in parameters)
        {
            if (string.IsNullOrEmpty(kvp.Key) || IsReserved(kvp.Key))
            {
                Debug.LogWarning($"AnalyticsManager: skipping reserved or empty parameter '{kvp.Key}'");
                continue;
            }

            switch (kvp.Value)
            {
                case null:
                    break;
                case int i:
                    result.Add(new Parameter(kvp.Key, i));
                    break;
                case long l:
                    result.Add(new Parameter(kvp.Key, l));
                    break;
                case bool b:
                    result.Add(new Parameter(kvp.Key, b ? 1L : 0L));
                    break;
                case float f:
                    result.Add(new Parameter(kvp.Key, f));
                    break;
                case double d:
                    result.Add(new Parameter(kvp.Key, d));
                    break;
                default:
                    result.Add(new Parameter(kvp.Key, kvp.Value.ToString()));
                    break;
            }
        }

        return result.ToArray();
    }

    private static bool IsReserved(string key)
    {
        foreach (string prefix in ReservedPrefixes)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
