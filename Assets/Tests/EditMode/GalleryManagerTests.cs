using System.Collections.Generic;
using NUnit.Framework;

public class GalleryManagerTests
{
    [Test]
    public void AddCompletedCeramic_AppendsEntryWithCorrectFields()
    {
        var entries = new List<GalleryEntryData>();
        var gallery = new GalleryManager(entries);

        gallery.AddCompletedCeramic(tier: 1, completionDateIso: "2026-08-18", cumulativeScore: 2140);

        Assert.AreEqual(1, gallery.Entries.Count);
        Assert.AreEqual(1, gallery.Entries[0].Tier);
        Assert.AreEqual("2026-08-18", gallery.Entries[0].Date);
        Assert.AreEqual(2140, gallery.Entries[0].Score);
    }

    [Test]
    public void AddCompletedCeramic_MultipleEntries_PreservesOrder()
    {
        var entries = new List<GalleryEntryData>();
        var gallery = new GalleryManager(entries);

        gallery.AddCompletedCeramic(1, "2026-08-01", 1000);
        gallery.AddCompletedCeramic(2, "2026-08-10", 2000);

        Assert.AreEqual(2, gallery.Entries.Count);
        Assert.AreEqual(1, gallery.Entries[0].Tier);
        Assert.AreEqual(2, gallery.Entries[1].Tier);
    }

    [Test]
    public void Constructor_WithExistingEntries_PreservesThem()
    {
        var existing = new List<GalleryEntryData>
        {
            new GalleryEntryData { Tier = 1, Date = "2026-07-01", Score = 500 }
        };

        var gallery = new GalleryManager(existing);

        Assert.AreEqual(1, gallery.Entries.Count);
        Assert.AreEqual(500, gallery.Entries[0].Score);
    }

    [Test]
    public void Constructor_NullList_ThrowsArgumentNullException()
    {
        Assert.Throws<System.ArgumentNullException>(() => new GalleryManager(null));
    }
}
