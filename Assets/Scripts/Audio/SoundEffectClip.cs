using System;
using UnityEngine;

/// <summary>
/// Inspector-assignable mapping from a <see cref="SoundEffect"/> trigger to
/// its clip. No audio assets exist yet (CLAUDE.md §8.3 clips land in Phase 5)
/// — <see cref="AudioManager"/> tolerates an unassigned clip by logging a
/// warning instead of throwing.
/// </summary>
[Serializable]
public struct SoundEffectClip
{
    public SoundEffect Effect;
    public AudioClip Clip;
}
