using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Guards the convention AudioManager relies on: every sound effect is
/// loaded from <c>Resources/Audio/&lt;SoundEffect&gt;</c> by enum name.
///
/// Worth a test because the failure is silent. AudioManager's serialized
/// clip array only works for an instance placed in a scene, but the one the
/// game uses is created at runtime — so before this loading path existed,
/// every sound stayed quiet no matter what was imported, and nothing failed
/// or warned except a log line nobody was reading. A misspelled filename
/// would now reintroduce exactly that, invisibly.
/// </summary>
public class AudioClipResourcesTests
{
    // The clips supplied so far. Deliberately not the whole enum: the rest
    // are still to be sourced, and asserting on files that do not exist yet
    // would make this test a standing failure rather than a guard.
    private static readonly SoundEffect[] Supplied =
    {
        SoundEffect.PiecePickup,
        SoundEffect.PiecePlace,
        SoundEffect.LineClear,
        SoundEffect.ComboClear,
    };

    [Test]
    public void EverySuppliedEffectResolvesFromResources()
    {
        foreach (SoundEffect effect in Supplied)
        {
            var clip = Resources.Load<AudioClip>($"Audio/{effect}");
            Assert.IsNotNull(clip, $"no clip at Resources/Audio/{effect} — filename must match the enum exactly");
        }
    }

    [Test]
    public void SuppliedClipsAreMonoSoTheyAreNotShippingStereoMasters()
    {
        foreach (SoundEffect effect in Supplied)
        {
            var clip = Resources.Load<AudioClip>($"Audio/{effect}");
            if (clip == null)
            {
                continue;
            }

            Assert.AreEqual(1, clip.channels, $"{effect} should import as mono (see AudioImportSetup)");
        }
    }

    [Test]
    public void PiecePlaceIsShortEnoughToFireOnEveryPlacement()
    {
        var clip = Resources.Load<AudioClip>($"Audio/{SoundEffect.PiecePlace}");
        Assert.IsNotNull(clip);

        // The placement sound fires constantly during play; anything much
        // longer than this overlaps itself and turns into a drone.
        Assert.Less(clip.length, 1f, "PiecePlace must stay under a second");
    }
}
