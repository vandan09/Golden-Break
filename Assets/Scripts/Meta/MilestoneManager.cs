using System.Collections.Generic;

/// <summary>
/// Score-milestone rewards (CLAUDE.md §4.3), each triggering once ever.
/// Plain C#, static — no per-instance state; the "already claimed" list
/// lives in SaveData (milestones_claimed).
/// </summary>
public static class MilestoneManager
{
    private struct MilestoneDefinition
    {
        public int Score;
        public int Coins;
        public string GalleryFrameId;
        public string ThemeId;
    }

    // §4.3's table. Theme/frame ids are opaque placeholders — real theme
    // palettes and frame art are Phase 5 scope, same as every other
    // cosmetic asset; this only tracks which reward a given milestone
    // grants.
    private static readonly MilestoneDefinition[] Milestones =
    {
        new MilestoneDefinition { Score = 500, Coins = 50, GalleryFrameId = "milestone_500", ThemeId = null },
        new MilestoneDefinition { Score = 1000, Coins = 100, GalleryFrameId = null, ThemeId = "milestone_1000" },
        new MilestoneDefinition { Score = 2500, Coins = 150, GalleryFrameId = "milestone_2500", ThemeId = null },
        new MilestoneDefinition { Score = 5000, Coins = 200, GalleryFrameId = null, ThemeId = "milestone_5000" },
        new MilestoneDefinition { Score = 10000, Coins = 300, GalleryFrameId = "milestone_10000_rare", ThemeId = null },
    };

    public readonly struct MilestoneResult
    {
        public readonly int MilestoneScore;
        public readonly int CoinsAwarded;
        public readonly string GalleryFrameUnlocked;
        public readonly string ThemeUnlocked;

        public MilestoneResult(int milestoneScore, int coinsAwarded, string galleryFrameUnlocked, string themeUnlocked)
        {
            MilestoneScore = milestoneScore;
            CoinsAwarded = coinsAwarded;
            GalleryFrameUnlocked = galleryFrameUnlocked;
            ThemeUnlocked = themeUnlocked;
        }
    }

    // Checks which milestones a just-finished game's score newly
    // qualifies for (not already in alreadyClaimed) and marks them
    // claimed in place. Returns them in ascending score order — more than
    // one can trigger from a single game (e.g. a first game landing
    // straight at 3000 claims both 500 and 2500).
    public static List<MilestoneResult> CheckNewlyReached(int score, List<int> alreadyClaimed)
    {
        var results = new List<MilestoneResult>();
        foreach (MilestoneDefinition milestone in Milestones)
        {
            if (score >= milestone.Score && !alreadyClaimed.Contains(milestone.Score))
            {
                alreadyClaimed.Add(milestone.Score);
                results.Add(new MilestoneResult(milestone.Score, milestone.Coins, milestone.GalleryFrameId, milestone.ThemeId));
            }
        }

        return results;
    }
}
