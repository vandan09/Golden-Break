using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies mobile-appropriate import settings to everything in
/// Resources/Audio.
///
/// The supplied effects are 24-bit 48kHz stereo, which is studio delivery
/// format, not shipping format: uncompressed they would add megabytes to
/// the APK for sounds that last under a second and play through a phone
/// speaker. Vorbis at a modest quality is inaudible here and a fraction of
/// the size, mono halves it again (these are UI effects with no meaningful
/// stereo image), and DecompressOnLoad avoids a decode hitch on a sound
/// that fires on every single piece placement.
///
/// Music, if one is ever added, deliberately keeps stereo and streams
/// instead — a loop is long, plays continuously, and is the one place a
/// stereo image is worth having.
/// </summary>
public static class AudioImportSetup
{
    private const string AudioFolder = "Assets/Resources/Audio";
    private const string MusicAssetName = "MusicLoop";

    public static void ApplySettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"AudioImportSetup: no clips found in {AudioFolder}.");
            return;
        }

        int updated = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                continue;
            }

            bool isMusic = System.IO.Path.GetFileNameWithoutExtension(path) == MusicAssetName;

            var settings = importer.defaultSampleSettings;
            settings.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = isMusic ? 0.7f : 0.5f;
            settings.preloadAudioData = !isMusic;

            importer.defaultSampleSettings = settings;
            importer.forceToMono = !isMusic;
            importer.loadInBackground = isMusic;

            importer.SaveAndReimport();
            updated++;

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            Debug.Log($"AudioImportSetup: {System.IO.Path.GetFileName(path)} — {clip.length:0.00}s, {clip.channels}ch");
        }

        AssetDatabase.Refresh();
        Debug.Log($"AudioImportSetup: configured {updated} clip(s).");
    }
}
