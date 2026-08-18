using UnityEngine;

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

    public static void RequestReviewIfEligible(SaveData saveData)
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
        // TODO(play-core-setup): real Play Core ReviewManager.RequestReviewFlow
        // / LaunchReviewFlow async call goes here once the
        // com.google.android.play:review package is imported.
        Debug.Log("ReviewManager: would request the Play In-App Review flow here once Play Core is imported.");
#else
        Debug.Log("ReviewManager: in-app review is Android-only; no-op on this platform.");
#endif
    }
}
