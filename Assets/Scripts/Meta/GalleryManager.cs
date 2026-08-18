using System;
using System.Collections.Generic;

/// <summary>
/// Completed-ceramic gallery (CLAUDE.md §3.5), wrapping the existing
/// <c>List&lt;GalleryEntryData&gt;</c> save field from Phase 0. Plain C#.
/// </summary>
public sealed class GalleryManager
{
    private readonly List<GalleryEntryData> _entries;

    public IReadOnlyList<GalleryEntryData> Entries => _entries;

    public GalleryManager(List<GalleryEntryData> entries)
    {
        _entries = entries ?? throw new ArgumentNullException(nameof(entries));
    }

    public void AddCompletedCeramic(int tier, string completionDateIso, int cumulativeScore)
    {
        _entries.Add(new GalleryEntryData
        {
            Tier = tier,
            Date = completionDateIso,
            Score = cumulativeScore
        });
    }
}
