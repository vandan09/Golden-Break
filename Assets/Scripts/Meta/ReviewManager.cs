using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using Google.Play.Review;
#endif

/// <summary>
/// Google Play In-App Review API trigger (BUILD_PLAN Phase 4). Not a
/// MonoBehaviour singleton — same reasoning as HapticManager: not on
/// BUILD_PLAN Part 1's permitted-singleton list, and needs no Unity
/// lifecycle beyond a platform call, so a static class is the correct
/// fit. Gating logic (<see cref="ShouldRequestReview"/>) is a pure
/// function so it's unit-testable without Play Core; the actual review
/// flow is compiled out entirely off-device, same pattern as
/// HapticManager.Vibrate.
///
/// Google's own `Google.Play.Review.ReviewManager` API is coroutine-based
/// (its async operations are meant to be `yield return`ed), which needs
/// an active MonoBehaviour to host `StartCoroutine` — rather than make
/// this class itself a MonoBehaviour (this is exactly what BUILD_PLAN
/// Part 1's singleton list exists to prevent), the caller supplies one.
/// HomeScreen already owns the one call site (session start), so it
/// passes itself.
///
/// Triggered after the player's first completed ceramic — CLAUDE.md
/// §3.4 calls ceramic completion the game's "peak wow moment," and
/// Google's own guidance is to ask right after a positive experience,
/// never mid-session or right after a loss. No explicit trigger point is
/// specified in CLAUDE.md itself; this is a documented interpretation
/// (see PROGRESS.md), not a literal spec requirement.
/// </summary>
public static class ReviewManager
{
    public static bool ShouldRequestReview(SaveData saveData)
    {
        return saveData != null && !saveData.ReviewRequested && saveData.Gallery != null && saveData.Gallery.Count >= 1;
    }

    public static void RequestReviewIfEligible(SaveData saveData, MonoBehaviour coroutineRunner = null)
    {
        if (!ShouldRequestReview(saveData))
        {
            return;
        }

        // Marked requested unconditionally once eligible and attempted —
        // Play Core's real API has no reliable "did the user actually
        // review" signal (the OS throttles/hides the dialog after its own
        // quota), only "did we ask." Re-asking isn't permitted regardless
        // of outcome.
        saveData.ReviewRequested = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (coroutineRunner != null)
        {
            coroutineRunner.StartCoroutine(RunReviewFlow());
        }
        else
        {
            Debug.LogWarning("ReviewManager: eligible to request a review but no MonoBehaviour was supplied to host the flow — skipped.");
        }
#else
        Debug.Log("ReviewManager: in-app review is Android-only; no-op on this platform.");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Play Core's own documented two-step flow: request the review info
    // first, then separately launch it with that info. LaunchReviewFlow
    // does not guarantee the dialog actually appears (Play Store throttles
    // it server-side) — completing without an exception is success from
    // this app's side regardless of whether the player actually saw it.
    private static IEnumerator RunReviewFlow()
    {
        var reviewManager = new Google.Play.Review.ReviewManager();

        var requestFlowOperation = reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;

        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"ReviewManager: RequestReviewFlow failed — {requestFlowOperation.Error}");
            yield break;
        }

        PlayReviewInfo playReviewInfo = requestFlowOperation.GetResult();
        var launchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);
        yield return launchFlowOperation;

        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"ReviewManager: LaunchReviewFlow failed — {launchFlowOperation.Error}");
        }
    }
#endif
}
