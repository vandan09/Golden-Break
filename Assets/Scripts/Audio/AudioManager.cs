using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton owner of sound-effect and music playback. Clip assignment is
/// Inspector-driven (<see cref="_soundEffectClips"/>/<see cref="_musicLoop"/>)
/// since no audio assets exist yet — an unassigned clip logs a warning and
/// is otherwise a no-op rather than throwing, so gameplay code can call
/// PlaySound freely from Phase 1 onward without waiting on Phase 5 audio.
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField]
    private SoundEffectClip[] _soundEffectClips = new SoundEffectClip[0];

    [SerializeField]
    private AudioClip _musicLoop;

    [SerializeField]
    private AudioSource _sfxSource;

    [SerializeField]
    private AudioSource _musicSource;

    private readonly Dictionary<SoundEffect, AudioClip> _clipLookup = new Dictionary<SoundEffect, AudioClip>();
    private bool _soundEnabled = true;
    private bool _musicEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
        }

        if (_musicSource == null)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
        }

        foreach (SoundEffectClip entry in _soundEffectClips)
        {
            if (entry.Clip != null)
            {
                _clipLookup[entry.Effect] = entry.Clip;
            }
        }

        LoadClipsFromResources();
    }

    /// <summary>
    /// Fills in any effect that has no clip from
    /// <c>Resources/Audio/&lt;SoundEffect&gt;</c>.
    ///
    /// The serialized array above only works for an AudioManager placed in
    /// a scene and wired in the Inspector, but this one is created at
    /// runtime by GameplayController — so that array is always empty and
    /// every sound stayed silent no matter what was imported. Loading by
    /// enum name means adding a sound is just dropping a correctly-named
    /// file into Resources/Audio, with no scene or code change.
    ///
    /// Inspector assignments still win, so this cannot override a
    /// deliberately wired clip.
    /// </summary>
    private void LoadClipsFromResources()
    {
        foreach (SoundEffect effect in System.Enum.GetValues(typeof(SoundEffect)))
        {
            if (_clipLookup.ContainsKey(effect))
            {
                continue;
            }

            var clip = Resources.Load<AudioClip>($"Audio/{effect}");
            if (clip != null)
            {
                _clipLookup[effect] = clip;
            }
        }

        if (_musicLoop == null)
        {
            _musicLoop = Resources.Load<AudioClip>("Audio/MusicLoop");
        }
    }

    public void SetSoundEnabled(bool enabled)
    {
        _soundEnabled = enabled;
    }

    public void SetMusicEnabled(bool enabled)
    {
        _musicEnabled = enabled;
        if (_musicSource != null)
        {
            _musicSource.mute = !enabled;
        }
    }

    public void PlaySound(SoundEffect effect)
    {
        if (!_soundEnabled || _sfxSource == null)
        {
            return;
        }

        if (_clipLookup.TryGetValue(effect, out AudioClip clip) && clip != null)
        {
            _sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"AudioManager: no clip assigned for {effect} yet.");
        }
    }

    public void PlayMusic()
    {
        if (_musicSource == null || _musicLoop == null)
        {
            return;
        }

        _musicSource.clip = _musicLoop;
        _musicSource.loop = true;
        _musicSource.mute = !_musicEnabled;
        _musicSource.Play();
    }

    public void StopMusic()
    {
        if (_musicSource != null)
        {
            _musicSource.Stop();
        }
    }
}
