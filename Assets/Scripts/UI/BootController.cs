using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Minimal placeholder: loads straight into Gameplay so Phase 1 can be
/// verified end-to-end on-device. Real Boot->MainMenu->Gameplay flow
/// (save loading, streak status, daily challenge, etc.) is later-phase
/// scope (CLAUDE.md §4, §5.3) — this only exists so there's something to
/// actually reach for now.
/// </summary>
public sealed class BootController : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // Correct to use the fire-once AfterSceneLoad attribute here,
        // unlike GameplayController: Boot is always the first scene
        // loaded, so "fires once after the first scene load" is exactly
        // the semantics this needs.
        if (SceneManager.GetActiveScene().name != "Boot")
        {
            return;
        }

        SceneManager.LoadScene("Gameplay");
    }
}
