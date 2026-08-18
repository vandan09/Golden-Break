using NUnit.Framework;

public class ReviewManagerTests
{
    [Test]
    public void ShouldRequestReview_NoCompletedCeramicsYet_ReturnsFalse()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");

        Assert.IsFalse(ReviewManager.ShouldRequestReview(data));
    }

    [Test]
    public void ShouldRequestReview_OneCompletedCeramicAndNotYetRequested_ReturnsTrue()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.Gallery.Add(new GalleryEntryData { Tier = 1, Date = "2026-08-18", Score = 500 });

        Assert.IsTrue(ReviewManager.ShouldRequestReview(data));
    }

    [Test]
    public void ShouldRequestReview_AlreadyRequested_ReturnsFalseEvenWithCompletedCeramics()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.Gallery.Add(new GalleryEntryData { Tier = 1, Date = "2026-08-18", Score = 500 });
        data.ReviewRequested = true;

        Assert.IsFalse(ReviewManager.ShouldRequestReview(data));
    }

    [Test]
    public void RequestReviewIfEligible_Eligible_MarksRequested()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");
        data.Gallery.Add(new GalleryEntryData { Tier = 1, Date = "2026-08-18", Score = 500 });

        ReviewManager.RequestReviewIfEligible(data);

        Assert.IsTrue(data.ReviewRequested);
    }

    [Test]
    public void RequestReviewIfEligible_NotEligible_DoesNotThrowAndLeavesFlagUnset()
    {
        SaveData data = SaveData.CreateFresh("2026-08-18");

        Assert.DoesNotThrow(() => ReviewManager.RequestReviewIfEligible(data));
        Assert.IsFalse(data.ReviewRequested);
    }

    [Test]
    public void ShouldRequestReview_NullSaveData_ReturnsFalseWithoutThrowing()
    {
        Assert.DoesNotThrow(() => ReviewManager.ShouldRequestReview(null));
        Assert.IsFalse(ReviewManager.ShouldRequestReview(null));
    }
}
