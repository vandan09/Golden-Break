# Golden Break — Game Specification

**Version:** 1.0
**Date:** August 5, 2026
**Developer:** Solo (AI-assisted via Claude Code)
**Platform:** Android (Google Play) — iOS deferred
**Engine:** Unity 2022 LTS (C#)
**Monetization:** Ad-first (rewarded + interstitial), light IAP
**Orientation:** Portrait locked
**Min Android:** API 24 (Android 7.0)
**Target APK:** < 35MB
**Target FPS:** 60fps on Redmi Note 10 class (3-4GB RAM)
**Relationship to GLYPH:** Shares engine, ad stack, analytics, UI kit, audio system, object pooling, save structure, and cozy-dark aesthetic. Built after GLYPH Phase 5.
**Status:** Spec locked. Build from this.

---

## 1. Game overview

### 1.1 One-line pitch

An 8×8 block puzzle where every cleared line fills a cracked ceramic with gold — turning something broken into something beautiful.

### 1.2 Core concept

Golden Break is an endless block-placement puzzle. The player drags Tetris-like pieces onto an 8×8 grid. Filling a complete row or column clears it. The game ends when no remaining piece fits anywhere on the grid.

The differentiating layer is the kintsugi meta: above the grid, a cracked ceramic piece (bowl, plate, vase, teapot) is displayed. Each line clear sends golden light into one crack. When all cracks are repaired, the piece is complete — it joins the player's gallery, and a new, more complex broken piece appears.

The core mechanic is proven (Block Blast: $17.5M/month). The differentiation is entirely in the meta and the aesthetic — a cozy-dark, lo-fi visual identity that stands apart from every bright-coloured competitor.

### 1.3 Aesthetic direction

Identical philosophy to GLYPH: cozy-minimalist, dark navy-indigo backgrounds, soft glowing blocks, lo-fi ambient music, ASMR-style sounds, gentle haptics. Same palette family, slightly warmer undertone to distinguish it.

### 1.4 No fallback needed

The core mechanic is proven at massive scale. There is no unproven twist to test. The only risk is execution quality and discovery, not design.

---

## 2. Target audience and positioning

### 2.1 Primary audience

Same as GLYPH: global, tier-1 priority, skews female 25-55, seeking calm no-pressure puzzle gameplay. Golden Break targets the slightly broader "block puzzle" audience while GLYPH targets the narrower "sort puzzle" segment. No cannibalization — different keywords, different mechanics.

### 2.2 ASO positioning

**Store title: "Golden Break: Block Puzzle""

**Keywords:** block puzzle, wood block, tile puzzle, relaxing puzzle, cozy puzzle, offline puzzle, grid puzzle, brain training, satisfying puzzle, ASMR puzzle, block game

**Icon:** Dark background (#1a1a2e), a single golden crack line glowing across a dark ceramic surface. No text. Instantly different from the bright-coloured block puzzle icons.

**Short description:** "Place blocks. Clear lines. Repair with gold. A calm, beautiful puzzle."

### 2.3 Differentiation from Block Blast and clones

| Block Blast / clones | Golden Break |
|---|---|
| Bright generic colours, white background | Dark, cozy, minimalist |
| Score counter only — no meta purpose | Every clear repairs a ceramic with gold |
| No long-term collection | Gallery of repaired artworks |
| Ads every round / aggressive | Ads every 3rd game-over, none in first 3 games |
| No daily structure | Daily challenge + streak |
| Generic sound | ASMR ceramic + gold sounds |

---

## 3. Game design specification

### 3.1 Core mechanic

**Grid:** 8×8 cells. Fixed size, never changes.

**Pieces:** The player receives 3 random pieces at a time, displayed below the grid. All 3 must be placed before 3 new pieces are dealt. Pieces cannot be rotated (same as Block Blast — rotation adds complexity without adding fun in this format).

**Piece set (18 shapes):**

```
1×2  ██           2×1  █        1×3  ███       3×1  █
                       █                            █
                                                    █

1×4  ████         4×1  █        1×5  █████     5×1  █
                       █                            █
                       █                            █
                       █                            █
                                                    █

2×2  ██           L    ██       J    ██        S    █
     ██                █             █              ██

Z    ██           T    ███     2×3   ██        3×2  ███
      █                █            ██              ███
                                    ██

L3   █            J3   █
     █                 █
     ██                ██

single  █         3×3  ███
                       ███
                       ███
```

**Piece distribution weights:** Smaller pieces (1×2, 2×1, single, 2×2) are more common early, larger pieces (3×3, 1×5, 5×1) are rarer. This creates natural difficulty scaling as the session progresses — not because rules change, but because the piece pool shifts.

```
single, 1×2, 2×1:           weight 12 each
2×2, L, J, S, Z, T:         weight 10 each
1×3, 3×1, 2×3, 3×2:         weight 8 each
L3, J3:                     weight 6 each
1×4, 4×1:                   weight 5 each
1×5, 5×1, 3×3:              weight 3 each
```

Weights are tunable. Store them in a `ScriptableObject` so they can be A/B tested post-launch without code changes.

**Placement:** The player drags a piece from the tray toward the grid. As the piece hovers, a ghost preview shows where it would snap. Lifting the finger drops it. If the position is invalid (overlaps existing blocks or extends outside the grid), the piece snaps back to the tray with a gentle bounce.

**Clearing:** When all 8 cells in a row OR all 8 cells in a column are filled, that line clears simultaneously. Multiple lines can clear in one move (combo). Blocks above a cleared row do NOT fall down — they stay in place. This is the Block Blast model, not Tetris.

**Key design note: blocks do not fall.** This is critical. In Tetris, blocks fall after a clear, which creates cascades. In Block Blast and Golden Break, blocks are static. A cleared row simply empties those cells. This makes the game more spatial and strategic — you must plan around permanent gaps.

**Game over:** After placing all 3 pieces and receiving 3 new ones, the game checks if any of the 3 new pieces can fit anywhere on the grid. If none can fit, the game is over. Game over also triggers immediately if any single piece from the current hand cannot fit and the player has no more moves.

**Game-over detection algorithm:**

```
For each of the 3 current pieces:
  For each cell (x, y) on the grid:
    Check if the piece fits at (x, y) without overlap
    If yes: at least one valid placement exists → game continues
If no piece fits anywhere: game over
```

This runs after every placement. On an 8×8 grid with max 18 piece cells, it completes in microseconds — no performance concern.

### 3.2 Scoring

**Points per clear:**

| Lines cleared in one move | Points |
|---|---|
| 1 line | 10 |
| 2 lines (combo) | 30 |
| 3 lines | 60 |
| 4 lines | 100 |
| 5+ lines | 150 + 50 per additional line |

**Streak bonus:** Consecutive placements that each clear at least one line multiply points: ×1, ×1.5, ×2, ×2.5, ×3 (capped). Placing a piece that clears nothing resets the streak to ×1.

**Score display:** Current score and personal best, always visible at the top.

### 3.3 Controls

**Primary input:** Drag and drop.

1. Touch a piece in the tray → piece lifts and follows the finger
2. Drag over the grid → ghost preview shows placement (valid = coloured ghost, invalid = red-tinted ghost)
3. Release finger → piece snaps to grid position (if valid) or bounces back to tray (if invalid)
4. Piece placement animation: 100ms scale-pop (0.95 → 1.0) with a satisfying click sound

**Snap threshold:** The piece snaps to the nearest valid grid position when the centre of the piece is within the grid bounds. Edge cases: if the finger drags off-screen, the piece returns to the tray.

**No rotation button.** This is deliberate. Block Blast doesn't have rotation and it works — the constraint creates the puzzle. Adding rotation makes the game easier and reduces tension.

**Grid coordinate system.** (0,0) is top-left. X increases rightward, Y increases downward. "Bottom rows" = rows with Y = 6 and Y = 7. This matters for the continue mechanic (§5.1).

**Piece tray layout.** The tray is a horizontal row below the grid. Each piece occupies a fixed-width slot (grid-width ÷ 3). Within each slot, the piece is centred. Large pieces (3×3) fill the slot; small pieces (single, 1×2) sit centred with space around them. This prevents visual jitter when piece sizes vary. Pieces in the tray render at 0.7× grid cell size. On drag-start, the piece scales to 1.0× and lifts above the grid (z-order change so it renders on top).

### 3.4 The kintsugi meta

This is the entire differentiator. Without it, the game is a bare Block Blast clone.

**Ceramic pieces:** A sequence of ceramic artworks, each with a defined number of cracks:

| Tier | Ceramic | Cracks | Lines to complete |
|---|---|---|---|
| 1 | Simple bowl | 4 | 4 |
| 2 | Tea cup | 5 | 5 |
| 3 | Plate | 6 | 6 |
| 4 | Tall vase | 7 | 7 |
| 5 | Teapot | 8 | 8 |
| 6 | Large bowl | 9 | 9 |
| 7 | Ornate plate | 10 | 10 |
| 8 | Sake set | 11 | 11 |
| 9 | Temple bowl | 12 | 12 |
| 10+ | Repeat 6-9 with colour variants | 10-12 | 10-12 |

**Crack repair flow:**

1. Player clears a line
2. Golden particles rise from the cleared row toward the ceramic above
3. One crack fills with gold — a 500ms animation of gold flowing along the crack line with a warm metallic chime
4. The progress bar beneath the ceramic updates

**Combo repair:** Clearing 2+ lines in one move repairs 2+ cracks simultaneously with a more dramatic gold-flow effect and a deeper chime.

**Ceramic completion:**

1. Final crack fills → 1-second pause
2. The entire ceramic glows with gold → celebration particle burst
3. "Beautiful" text fades in, then the piece slides into the gallery
4. New cracked ceramic fades in above the grid
5. Play continues seamlessly — no interruption, no popup

**Persistence across game-overs.** This is critical: the ceramic does NOT reset when the game ends. If the player repaired 3 of 6 cracks, then died, those 3 cracks stay repaired in the next game. Progress on the current ceramic persists until it's complete. This is the primary retention driver — "I'm 2 cracks away from finishing this vase, one more game."

**Visual design of ceramics:** Each ceramic is a simple 2D illustration — a silhouette with crack lines overlaid. Cracks are paths (bezier curves). Gold-filled cracks use the same paths with a golden stroke and subtle glow. Total art per ceramic: 1 silhouette SVG + 1 crack-path data file. Budget: ~2 hours for all 10 tiers.

### 3.5 Gallery

A scrollable screen showing all completed ceramics. Each one displayed with its gold repairs glowing. Shows the date completed and the score achieved during that ceramic's lifespan.

**Purpose:** Long-term progression and bragging rights. The gallery grows indefinitely.

**Score displayed per ceramic:** The total cumulative score earned across ALL games played while working on that ceramic (not the best single-game score). This rewards persistence — a ceramic completed over 5 games of 500 points each shows 2,500. The date shown is the completion date.

**Practical implementation:** A `ScrollRect` with instantiated card prefabs. Each card: ceramic thumbnail + completion date + cumulative score. Data stored in save file as a list.

### 3.6 Session design

**Target session:** 3-10 minutes. One run lasts until game-over (typically 5-15 minutes for a decent player).

**Session flow:**

```
Open app → see streak status + current ceramic progress
→ Tap "Play" → game starts immediately, no level select
→ Place pieces, clear lines, repair cracks
→ Game over → score summary + ceramic progress
→ "Watch ad to continue?" (rewarded, optional)
→ If declined: game-over screen with score, best score, ceramic status
→ "Play again" → new game, ceramic progress carries over
→ After every 3rd game-over: interstitial (if eligible)
```

**No loading screens between games.** The grid clears and new pieces appear. Sub-1-second transition. Friction between sessions kills endless-game retention.

### 3.7 Difficulty curve

There are no levels and no explicit difficulty settings. Difficulty emerges from:

1. **Piece RNG weights** — larger pieces are rarer, so as the session goes on, you're more likely to get an awkward large piece when the grid is already crowded
2. **Grid entropy** — each non-clearing placement adds blocks that fragment the remaining space, making future placements harder
3. **Player skill ceiling** — better spatial planning extends runs naturally

**Dynamic difficulty adjustment (DDA) — simple version for v1:**

Track the player's average score over their last 10 games. If the average is in the bottom quartile, slightly increase the weight of smaller pieces (making the game marginally easier). If in the top quartile, slightly increase large-piece weights. Adjustments are ±15% maximum — imperceptible but measurable in retention data.

Store the DDA state in save data. Log it to analytics for monitoring.

### 3.8 Combo and cascade visual feedback

**Single line clear:** Row/column cells flash white (100ms), then dissolve into particles. Cells empty. Score pops up at the clear position (+10). Gold particles rise to the ceramic.

**Multi-line combo (2+ lines in one move):** All clearing lines flash simultaneously. Larger particle burst. Score popup shows the combo multiplier ("×2 COMBO · +30"). Deeper chime. More gold particles. Screen shake (subtle, 3px, 150ms).

**4+ line combo (rare, placing a piece that completes 2 rows AND 2 columns):** Same as multi-line but with a full-screen golden flash (100ms, 15% opacity gold overlay) and a unique deep harmonic chime. All 4 cracks fill simultaneously with a dramatic gold-flow animation where particles converge from all four cleared lines. This is the peak "wow" moment and should feel exceptional.

**Streak (consecutive clearing placements):** Streak counter appears below the score ("×2 streak"). Each streak level adds a subtle pulsing glow to the grid border. Streak-breaking placement dims the glow and resets the counter silently (no punishing animation — keep it cozy).

### 3.9 Accessibility — colourblind support

Block colours in this game are purely cosmetic — they don't carry gameplay meaning (unlike GLYPH, where colours match gates). However, to distinguish pieces in the tray, each colour carries a subtle inner texture pattern:

| Colour | Hex | Pattern |
|---|---|---|
| Coral | #e06070 | Tiny dots |
| Blue | #60b0e0 | Diagonal lines |
| Green | #70d0a0 | Crosshatch |
| Gold | #e8c060 | Horizontal lines |
| Purple | #a080d0 | Circles |

Patterns render at low opacity (15%) so they're subtle for normal vision but distinguishable for colourblind players. High-contrast mode (Settings) increases pattern opacity to 40%.

### 3.10 End of content

There is no end. The game is endless. The ceramic gallery grows indefinitely (tier 10+ repeats with colour variants). Daily challenges are generated from a seed. This game never runs out of content.

---

## 4. Retention systems

### 4.1 Daily streak

Identical to GLYPH: complete at least 1 game per day.

| Day | Reward |
|---|---|
| 1-6 | 10, 20, 30, 40, 50, 60 coins |
| 7 | 100 coins + gallery frame unlock |
| 14 | 150 coins + gallery frame |
| 21 | 200 coins + rare gallery frame |
| 28+ | Cycle repeats, +25% coins |

Push notification at 7pm local if streak ≥3 and app not opened today. Permission requested day 3.

### 4.2 Daily challenge

**Mechanic:** One fixed game per day. Same piece sequence for all players (seeded RNG from the date). Compete for the highest score.

**Implementation:** The seed must produce identical piece sequences across all Android devices and OS versions. `string.GetHashCode()` is NOT stable across platforms in .NET — it can return different values on different runtimes. Instead use a deterministic hash:

```csharp
int DailyChallengeSeed(DateTime date) {
    // Stable across all platforms
    int y = date.Year;
    int m = date.Month;
    int d = date.Day;
    return y * 10000 + m * 100 + d;  // e.g. 20261122
}
```

This integer seeds `System.Random(seed)`. Same date → same seed → same piece sequence on every device. Verify with a unit test: assert that seed 20261122 produces a known, hardcoded first-20-pieces sequence.

**Reward:** 30 coins. Weekly leaderboard against AI ghost scores (no server needed for v1).

### 4.3 Personal best system

**Per-game best score** displayed at the top during play. Breaking your personal best triggers a celebration animation mid-game ("New best!" with gold text and a brief haptic burst). This is a micro-retention moment that costs nothing to build.

**Milestone rewards at score thresholds:**

| Score milestone | Reward |
|---|---|
| 500 | 50 coins + gallery frame |
| 1,000 | 100 coins + theme unlock |
| 2,500 | 150 coins + gallery frame |
| 5,000 | 200 coins + theme unlock |
| 10,000 | 300 coins + rare gallery frame |

These trigger once, ever. They give early players visible goals.

### 4.4 The ceramic itself IS the retention

This is the most important retention mechanism and it's built into the core loop, not bolted on:

- The ceramic does not reset on game-over
- A half-repaired piece is a visible promise of completion
- "One more game to finish this vase" is the daily-return driver
- Completing a ceramic feels like a genuine achievement
- The gallery grows as proof of sustained play

This is structurally identical to why merge games retain — the player is always mid-project, never at a natural stopping point.

### 4.5 Coin economy

**Earning:**

| Source | Amount |
|---|---|
| Game over (any score) | 5 coins |
| Score above personal best | +10 bonus |
| Ceramic completed | 25 coins |
| Daily streak (per day) | 10-60 (escalating) |
| Daily streak day 7 | 100 |
| Daily challenge completion | 30 |
| Score milestone (one-time) | 50-300 |
| Rewarded "double coins" | 2× game-over amount |

**Spending:**

| Item | Cost |
|---|---|
| Undo last placement | 50 coins |
| Refresh pieces (discard current 3, get new 3) | 75 coins |
| Gallery frame (cosmetic border for completed ceramics) | 100-200 |

**Undo restrictions — critical:**
- Undo can only revert the most recent placement. You cannot undo twice in a row to remove two pieces.
- **If the last placement caused a line clear, undo is NOT available.** The clear has already happened — gold has already flowed into a crack, score has already been awarded. Reversing this would create a confusing state (does the gold un-flow? does the crack re-break?). The simple rule: once lines clear, that move is permanent.
- Undo is available only while the current hand of 3 still has unplaced pieces (the removed piece goes back to its tray slot). Once all 3 are placed and new pieces are dealt, the previous placements are permanent.
- Maximum 1 undo per hand of 3 pieces.

**Refresh restrictions:**
- Can only refresh when all 3 pieces in the current hand are unplaced. You cannot place 1 piece, then refresh the remaining 2.
- Maximum 1 refresh per hand.
- The discarded pieces are gone — the new 3 are a fresh random draw from the weighted pool.

**Balance check:** A player averaging 3 games/day earns ~15 from game-overs + ~25 from streak + ~30 from daily = ~70 coins/day. That's roughly one undo per day from earned currency. The rewarded ad remains attractive because the undo is genuinely valuable at critical moments.

---

## 5. Monetization

### 5.1 Ad placements

**Rewarded (player-initiated):**

| Placement | Trigger | Expected opt-in |
|---|---|---|
| Continue after game-over | Game over screen | 55-70% |
| Double coins | Game over screen | 40-55% |
| Free undo | During gameplay (button always visible) | 30-45% |
| Free piece refresh | During gameplay (button visible) | 25-40% |

**Continue mechanic:** The player gets one continue per game. On continue:

1. The bottom 2 rows (Y=6 and Y=7) are completely cleared — all cells in those rows become empty, regardless of what was there. Blocks in rows 0-5 are untouched (blocks never fall or shift).
2. The current 3 pieces that caused game-over are discarded.
3. 3 new random pieces are dealt (using the standard weighted pool, not the DDA-adjusted pool — the continue should feel like a genuine second chance, not an easy handout).
4. The game-over detection re-runs. If the new 3 pieces still don't fit anywhere (extremely unlikely after clearing 16 cells, but possible), the game is truly over.
5. No score penalty. No crack un-repair. The game simply continues.
6. Only one continue per game. The "continue" button does not appear on the second game-over.

**Why clear rows, not random cells:** Clearing the bottom creates a clean foundation to build on, which is immediately readable. Random cell clearing looks chaotic and the player can't plan around it.

**Target:** 2-3 rewarded impressions per DAU.

**Interstitial:** Every 3rd game-over. None in the first 3 games (protect new player experience). Cap 6/day. Counter persists in save data.

**Banner:** Home screen and gallery screen only. Never during gameplay.

**Failure handling:** Same as GLYPH — toast "Ad unavailable," no reward granted, player never blocked.

### 5.2 IAP

| Item | Price |
|---|---|
| Remove interstitials | ₹249 / $2.99 |
| Theme pack | ₹79-249 / $0.99-2.99 |
| Coins (500) | ₹79 / $0.99 |
| Coins (2000) | ₹249 / $2.99 |

No pay-to-win. Undo and refresh are available via coins (earned or bought) or rewarded ads. No "buy extra moves" or "buy easier pieces."

### 5.3 Revenue projections

Same model as GLYPH:

| Scenario | ARPDAU | DAU for $1,000/mo | DAU for $3,000/mo |
|---|---|---|---|
| India-heavy | $0.02 | ~1,700 | ~5,000 |
| Blended global | $0.05 | ~667 | ~2,000 |
| Tier-1 heavy | $0.08-0.12 | ~280-420 | ~830-1,250 |

### 5.4 Mediation

Reuse GLYPH's AppLovin MAX setup entirely. Same networks, same accounts. Just create new ad unit IDs for Golden Break.

---

## 6. Analytics

### 6.1 SDK: GameAnalytics (same account as GLYPH, new game key)

### 6.2 Events

**Gameplay:**

| Event | Parameters |
|---|---|
| game_start | session_number, ceramic_tier |
| game_over | score, lines_cleared, pieces_placed, max_combo, ceramics_completed, seconds |
| line_clear | lines_in_move, combo_multiplier, streak_length |
| ceramic_complete | ceramic_tier, games_to_complete |
| continue_used | score_at_continue |
| undo_used | source (coins/rewarded), score_at_undo |
| refresh_used | source (coins/rewarded) |

**Retention:**

| Event | Parameters |
|---|---|
| daily_streak | streak_count |
| daily_challenge_complete | score, rank_vs_ghosts |
| milestone_reached | milestone_score |
| session_start | session_number, days_since_install |
| personal_best | new_best_score, previous_best |

**Monetization:**

| Event | Parameters |
|---|---|
| rewarded_offered | placement |
| rewarded_watched | placement |
| rewarded_failed | placement, error |
| interstitial_shown | game_count, daily_count |
| iap_purchased | item_id, price_usd |

### 6.3 Benchmarks

| Metric | Kill | Acceptable | Good |
|---|---|---|---|
| D1 retention | <22% | 22-30% | >30% |
| D7 retention | <8% | 8-15% | >15% |
| Games per session | <2 | 2-4 | >4 |
| Continue opt-in rate | <30% | 30-55% | >55% |
| Rewarded opt-in (avg) | <25% | 25-50% | >50% |
| ARPDAU (tier-1) | <$0.03 | $0.03-0.08 | >$0.08 |

### 6.4 Decision thresholds (after 1,000 installs)

- D1 <22% → Piece RNG or placement feel is wrong. Tune DDA. Check drag-drop responsiveness.
- D7 <8% → Ceramic meta isn't pulling people back. Make progress more visible on home screen.
- Games per session <2 → Game-over transition is too slow, or "play again" isn't prominent enough.
- Continue opt-in <30% → The continue reward isn't generous enough. Clear more rows.
- D1 >28% AND D7 >12% → Green light. Scale.

---

## 7. Technical architecture

### 7.1 Shared with GLYPH

| System | Reuse level | Notes |
|---|---|---|
| Unity project template | 100% | Same engine, same settings |
| `ObjectPool<T>` | 100% | Same code |
| `AudioManager` | 100% | Different sound files |
| `HapticManager` | 100% | Same code, different patterns |
| `SaveManager` (structure) | 90% | Different save fields |
| `AdManager` (AppLovin MAX) | 100% | New ad unit IDs only |
| `AnalyticsManager` (structure) | 90% | Different events |
| `Constants.cs` / `Strings.cs` pattern | 100% | Different values |
| UI kit / font / palette system | 100% | Warmer sub-palette |
| DOTween configuration | 100% | Same setup |
| Git / build / keystore practices | 100% | Same process |
| Privacy policy | 95% | Add Golden Break app name |

### 7.2 New code required

```
Golden Break/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameManager.cs           // State: playing, game-over, paused
│   │   │   ├── GridManager.cs           // 8×8 grid, cell state, placement validation
│   │   │   ├── PieceSpawner.cs          // Weighted random piece generation
│   │   │   ├── PieceController.cs       // Drag-and-drop, ghost preview, snap
│   │   │   ├── LineClearDetector.cs     // Row/column full detection + clearing
│   │   │   ├── GameOverDetector.cs      // Check if any piece fits anywhere
│   │   │   └── InputHandler.cs          // Touch/drag handling
│   │   ├── Scoring/
│   │   │   ├── ScoreManager.cs          // Points, combo, streak
│   │   │   └── DDAManager.cs            // Dynamic difficulty adjustment
│   │   ├── Meta/
│   │   │   ├── CeramicManager.cs        // Crack tracking, repair flow, tier progression
│   │   │   ├── GalleryManager.cs        // Completed ceramics collection
│   │   │   ├── CoinManager.cs           // Earn, spend, balance
│   │   │   ├── DailyChallenge.cs        // Seeded RNG, daily score
│   │   │   ├── StreakManager.cs          // Daily streak
│   │   │   ├── MilestoneManager.cs      // Score milestone rewards
│   │   │   └── SaveManager.cs           // Local save
│   │   ├── Ads/
│   │   │   ├── AdManager.cs             // AppLovin MAX (copied from GLYPH)
│   │   │   ├── RewardedAdController.cs  // Continue, double, undo, refresh
│   │   │   └── InterstitialController.cs
│   │   ├── Analytics/
│   │   │   └── AnalyticsManager.cs
│   │   ├── UI/
│   │   │   ├── HomeScreen.cs
│   │   │   ├── GameplayHUD.cs
│   │   │   ├── GameOverScreen.cs
│   │   │   ├── GalleryScreen.cs
│   │   │   ├── SettingsScreen.cs
│   │   │   ├── DailyChallengeUI.cs
│   │   │   └── StreakPopup.cs
│   │   ├── Audio/
│   │   │   ├── AudioManager.cs          // Copied from GLYPH
│   │   │   └── HapticManager.cs         // Copied from GLYPH
│   │   └── Util/
│   │       ├── ObjectPool.cs            // Copied from GLYPH
│   │       ├── Constants.cs
│   │       └── Strings.cs
│   ├── Tests/
│   │   ├── EditMode/
│   │   │   ├── GridPlacementTests.cs
│   │   │   ├── LineClearTests.cs
│   │   │   ├── GameOverDetectionTests.cs
│   │   │   ├── ScoreCalculationTests.cs
│   │   │   └── CoinEconomyTests.cs
│   │   └── PlayMode/
│   │       └── GameplaySmokeTest.cs
│   ├── Prefabs/
│   │   ├── Block.prefab
│   │   ├── PieceTray.prefab
│   │   ├── GhostPreview.prefab
│   │   └── ParticleEffects/
│   ├── Resources/
│   │   ├── PieceDefinitions/            // ScriptableObjects per piece shape
│   │   ├── CeramicDefinitions/          // ScriptableObjects per ceramic tier
│   │   ├── Palettes/
│   │   ├── Audio/
│   │   └── Fonts/Inter.ttf
│   ├── Scenes/
│   │   ├── Boot.unity
│   │   ├── MainMenu.unity
│   │   └── Gameplay.unity
│   └── Plugins/
│       ├── AppLovinMAX/
│       └── GameAnalytics/
└── ProjectSettings/
```

### 7.3 Save data

```json
{
  "save_version": 1,
  "best_score": 2891,
  "total_games": 127,
  "total_lines_cleared": 4523,
  "coins": 245,
  "current_ceramic": {
    "tier": 5,
    "total_cracks": 8,
    "cracks_repaired": 3
  },
  "gallery": [
    {"tier": 1, "date": "2026-11-01", "score": 342},
    {"tier": 2, "date": "2026-11-05", "score": 891},
    {"tier": 3, "date": "2026-11-12", "score": 1247},
    {"tier": 4, "date": "2026-11-20", "score": 2103}
  ],
  "milestones_claimed": [500, 1000],
  "streak_count": 5,
  "streak_last_date": "2026-11-22",
  "daily_completed": ["2026-11-20", "2026-11-21", "2026-11-22"],
  "daily_best_scores": {"2026-11-22": 1847},
  "interstitial_counter": 23,
  "interstitial_today_count": 2,
  "interstitial_today_date": "2026-11-22",
  "iap_remove_ads": false,
  "iap_themes_owned": ["midnight"],
  "settings": {"sound": true, "music": true, "haptics": true, "high_contrast": false},
  "dda_avg_score": 1150,
  "dda_last_10_scores": [980, 1100, 1247, 890, 1300, 1050, 1200, 1180, 1100, 1450],
  "notification_asked": false,
  "notification_granted": false,
  "first_launch_date": "2026-10-28",
  "total_sessions": 45
}
```

### 7.4 Piece data format

Each piece shape is a `ScriptableObject`:

```csharp
[CreateAssetMenu(fileName = "Piece", menuName = "Golden Break/PieceDefinition")]
public class PieceDefinition : ScriptableObject {
    public string pieceId;           // "L", "T", "2x2", "1x5", etc.
    public Vector2Int[] cells;       // relative cell positions, origin (0,0)
    public int spawnWeight;          // for weighted random selection
}
```

Example for an L-shape:

```
pieceId: "L"
cells: [(0,0), (0,1), (1,0)]     // two cells tall on left, one cell on right bottom
spawnWeight: 10
```

### 7.5 Performance

**Grid:** 64 cells = 64 SpriteRenderers in a pool. Never instantiated or destroyed at runtime.

**Pieces:** 3 active piece previews + 1 ghost preview = max ~15 block SpriteRenderers at once. Pooled.

**Particles:** Line-clear dissolve + gold-rise particles. Max 100 particles on screen simultaneously. Pooled.

**Total draw calls:** Under 100. This game is trivially lightweight — the grid is static sprites.

**GC:** Same zero-allocation rules as GLYPH. No allocations in the gameplay loop.

### 7.6 Ceramic data format

Each ceramic tier is a `ScriptableObject`:

```csharp
[CreateAssetMenu(fileName = "Ceramic", menuName = "Golden Break/CeramicDefinition")]
public class CeramicDefinition : ScriptableObject {
    public int tier;
    public string displayName;       // "Simple bowl", "Tea cup", etc.
    public Sprite silhouette;        // the ceramic outline
    public CrackPath[] cracks;       // bezier paths for each crack
    public int totalCracks;
}

[System.Serializable]
public struct CrackPath {
    public Vector2[] controlPoints;  // cubic bezier control points
}
```

Gold fill animation follows the same bezier path with a `DOTween` percentage tween from 0 to 1, drawing a `LineRenderer` segment that grows along the path.

---

## 8. Art and audio

### 8.1 Visual

**Grid cells:** Rounded rectangles, same style as GLYPH. Empty: #1e1e38 with #2a2a4a border. Filled: coloured with inner texture pattern.

**Pieces in tray:** Same block style, slightly smaller scale (0.7× grid cell size). Active drag piece scales to full size.

**Ghost preview:** Same colour as the piece, 30% opacity, no glow.

**Ceramics:** Simple 2D silhouettes. Dark fill (#1e1e38) with a thin border (#3a3a5a). Crack lines rendered as bezier paths. Gold-filled cracks: #e8c060 stroke, 3px, with a subtle additive glow. Production: each ceramic is one SVG exported from Figma. ~2 hours for 10 tiers.

**Background:** Gradient, same as GLYPH but shifted slightly warmer: #1a1a2e → #1a162b.

### 8.2 Palette — "Golden dark" (default)

```
Background         #1a1a2e → #1a162b
Block coral        #e06070    dots pattern
Block blue         #60b0e0    diagonal lines
Block green        #70d0a0    crosshatch
Block gold         #e8c060    horizontal lines
Block purple       #a080d0    circles
Gold fill          #e8c060 (glow: #f0d890)
Text primary       #c0c0d8
Text secondary     #7a7a9a
Surface            #252545
Border             #3a3a5a
```

### 8.3 Audio

| Sound | Description | Trigger |
|---|---|---|
| piece_pickup | Soft lift sound | Touch piece in tray |
| piece_place | Satisfying click/thunk | Valid placement |
| piece_invalid | Gentle rejection tone | Invalid placement |
| line_clear | Glass shatter + whoosh | Row/column clears |
| combo_clear | Deeper shatter + ascending chime | 2+ lines clear |
| gold_flow | Warm metallic chime | Gold fills a crack |
| ceramic_complete | 3-note golden chime + sparkle | All cracks repaired |
| game_over | Soft descending tone | Game ends |
| new_best | Brief triumphant chime | Personal best broken |

Source: Freesound.org, Mixkit. Same lo-fi music loop library as GLYPH.

**Haptics:** pickup 5ms; place 15ms; clear 25ms; combo 40ms; gold flow 20ms; ceramic complete 50ms; game over 30ms double-pulse.

---

## 9. Store and legal

### 9.1 Screenshots (6)

1. Gameplay with grid mid-game, ceramic above — "Place blocks. Repair with gold."
2. Gold flowing into crack close-up — "Every clear makes something beautiful."
3. Combo clear with particles — "Combos repair faster."
4. Gallery of completed ceramics — "Build a golden gallery."
5. Daily challenge — "New challenge every day."
6. Cozy aesthetic full view — "Your calm moment. Play offline."

### 9.2 Store description

```
Golden Break is a cozy block puzzle with a golden twist.

Place pieces on the grid. Fill a row or column to clear it. Every 
clear sends gold flowing into the cracks of a broken ceramic above. 
Watch it transform — fractures become art.

Inspired by the Japanese art of kintsugi: repairing broken pottery 
with gold, making it more beautiful than before.

WHAT MAKES Golden Break DIFFERENT

• Every clear repairs something beautiful — not just a score counter
• Cozy dark design with ASMR sounds and gentle haptics
• Your ceramic progress never resets — pick up where you left off
• No timers, no pressure — play at your own pace
• Works offline — train, plane, anywhere
• Colorblind friendly

FEATURES

• Endless block puzzle gameplay
• 10+ ceramic pieces to repair with gold
• Golden gallery of your completed works
• Daily challenge with streak rewards
• Score milestones and personal bests
• Multiple cozy themes
• No ads in your first 3 games

Place a block. Clear a line. Fill a crack with gold.
```

### 9.3 Legal

Same as GLYPH: privacy policy (add Golden Break to existing GitHub Pages), GDPR CMP via MAX, COPPA "not designed for children," India PROG Act compliant, content rating Everyone. Update `LICENSES.md` with any new audio assets.

### 9.4 Cross-promotion

Both GLYPH and Golden Break show a small "More cozy puzzles" card in Settings linking to the other game's Play Store page. Free cross-installs, zero cost.

---

## 10. Risk register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Differentiation too thin (just a reskin) | Medium | High | kintsugi meta adds genuine emotional reward. Ad creative focuses on gold-repair, not gameplay. |
| Block puzzle keywords too competitive | High | Medium | Target long-tail: "cozy block puzzle," "relaxing tile game." Cross-promote from GLYPH. |
| Drag-and-drop feels sluggish | Medium | High | Ghost preview, snap magnetism, 100ms place animation. Test on budget device week 1. |
| Ceramic art takes too long | Low | Low | Simple silhouettes + bezier cracks. 2 hours total. |
| Discovery / zero installs | High | High | Same three-channel strategy as GLYPH. Cross-promotion. |
| Traffic skews India | High | Medium | Same mitigation as GLYPH. |
| Block Blast dominates search | High | Medium | Don't compete head-on. "Cozy" + "gold" + dark aesthetic = different audience segment. |
| Burnout (second game after GLYPH) | Medium | Medium | Golden Break is simpler. Most systems copied. 3-4 weeks, not 12. |
| Undo after line-clear confusion | Low | Medium | Undo is disabled after clears. Visual: button greys out with a "Clear is permanent" tooltip on tap. |
| Daily challenge gives different pieces on different devices | Medium | High | Seeded with deterministic integer hash, not `string.GetHashCode()`. Cross-device test in QA. |
| Continue mechanic feels unfair | Low | Medium | Clear bottom 2 rows (generous, readable). Only 1 per game. No score penalty. |

---

## 11. Budget (incremental beyond GLYPH)

| Item | Amount |
|---|---|
| Google Play: already paid | ₹0 |
| UI kit: reuse GLYPH's | ₹0 |
| New audio assets | ₹0-500 |
| Ceramic art (Figma, self-made) | ₹0 |
| Marketing creative test | ₹10,000-15,000 |
| Scale (if metrics justify) | ₹15,000-25,000 |
| **Total incremental** | **₹25,000-40,500** |
