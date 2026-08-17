# Golden Break — Build Execution Plan

**Companion to:** `GoldenBreak_Spec.md` (the specification)
**Engineering standards:** Identical to GLYPH `BUILD_PLAN.md` Part 1. All naming conventions, architecture rules, error handling, performance rules, testing requirements, git discipline, and definition-of-done apply unchanged. Do not duplicate them — reference them.
**Prerequisite:** GLYPH Phase 5 complete (game feel done, infrastructure exists).
**Timeline:** 4 weeks (weeks 13-16 of the overall project).

---

## How to use this document

Same protocol as GLYPH:

1. Read `GoldenBreak_PROGRESS.md` to find the current phase and next unchecked task.
2. Read the spec sections referenced by that phase before writing code.
3. Work through tasks in order. Do not skip phases.
4. Update `GoldenBreak_PROGRESS.md` after every completed task.
5. Reuse GLYPH code wherever specified — copy and adapt, don't rewrite from scratch.

**Starting prompt for Claude Code:**

```
Read GoldenBreak_Spec.md and GoldenBreak_Build_Plan.md. Also read GLYPH's 
BUILD_PLAN.md Part 1 for engineering standards (they apply identically). 
Check GoldenBreak_PROGRESS.md for current phase. Continue from where we left off.
```

---

## Progress tracking

Create `GoldenBreak_PROGRESS.md` at Phase 0 start:

```markdown
# Golden Break Build Progress

**Current phase:** 0
**Last updated:** YYYY-MM-DD
**Blocked on:** (nothing / description)

## Phase status
- [ ] Phase 0 — Project setup (reuse from GLYPH)
- [ ] Phase 1 — Grid and pieces
- [ ] Phase 2 — Clearing and scoring
- [ ] Phase 3 — kintsugi meta
- [ ] Phase 4 — Retention and monetization
- [ ] Phase 5 — Polish, QA, and launch

## Reused from GLYPH
(list of files/systems copied and any modifications made)

## Deviations from spec
(anything built differently, and why)
```

---

## Phase 0 — Project setup

**Objective:** New Unity project configured with all GLYPH infrastructure copied in. No game logic yet.

**Time:** Half a day.

### Tasks

- [ ] Create new Unity 2022 LTS 2D project named Golden Break
- [ ] Copy Player Settings from GLYPH (portrait, API 24, IL2CPP, ARM64+ARMv7, High stripping)
- [ ] Copy folder structure from GLYPH, adjusted for Golden Break scripts (spec §7.2)
- [ ] Copy from GLYPH (unchanged): `ObjectPool.cs`, `AudioManager.cs`, `HapticManager.cs`, DOTween config
- [ ] Copy from GLYPH (adapt): `SaveManager.cs` (new save structure per spec §7.3), `AdManager.cs` (new ad unit IDs), `AnalyticsManager.cs` (new events per spec §6.2), `Constants.cs`, `Strings.cs`
- [ ] Install same packages: Newtonsoft JSON, Unity Test Framework, DOTween
- [ ] Copy UI kit assets, Inter font, palette ScriptableObjects from GLYPH
- [ ] Create three scenes: Boot, MainMenu, Gameplay
- [ ] Create `GoldenBreak_PROGRESS.md`
- [ ] Initialize git, create `.gitignore` (same as GLYPH)
- [ ] Create new GameAnalytics game key for Golden Break
- [ ] Create new AppLovin MAX ad unit IDs for Golden Break
- [ ] Initial commit

### Acceptance criteria

- Project opens without errors
- Empty APK builds and installs on test device
- Copied utility classes compile without modifications to their interfaces

### QA gate

- [ ] Build empty APK → installs and launches on device
- [ ] Git clean after build

---

## Phase 1 — Grid and pieces

**Objective:** An 8×8 grid where the player can drag and place pieces. No clearing, no scoring yet.

**Spec references:** §3.1 (core mechanic), §3.3 (controls), §7.4 (piece data format)

### Tasks

- [ ] Create `PieceDefinition` ScriptableObject class per spec §7.4
- [ ] Create all 18 piece definitions as ScriptableObject assets with spawn weights
- [ ] Implement `GridManager`: 8×8 cell array, cell states (empty/filled/colour), cell-to-world position
- [ ] Implement grid rendering: 64 pooled SpriteRenderers, empty/filled states
- [ ] Implement `PieceSpawner`: weighted random selection, deal 3 pieces at a time
- [ ] Implement piece tray: display 3 pieces below the grid, properly spaced
- [ ] Implement `PieceController` drag-and-drop:
  - Touch piece → lifts and follows finger
  - Drag over grid → ghost preview shows at nearest valid position
  - Invalid position → ghost tints red
  - Release on valid position → piece snaps to grid, cells fill, piece removed from tray
  - Release on invalid position → piece bounces back to tray
- [ ] Implement `InputHandler`: touch tracking, piece selection, drag state machine
- [ ] Implement placement validation: check all piece cells against grid bounds and existing blocks
- [ ] Implement "all 3 placed" detection → deal 3 new pieces
- [ ] Implement `GameOverDetector` per spec §3.1 algorithm
- [ ] Add basic placement sound and haptic (copy audio infrastructure from GLYPH)
- [ ] Implement DDA-aware piece weights (spec §3.7): `DDAManager` tracks last 10 scores, adjusts weights ±15%
- [ ] Write tests: `GridPlacementTests` — valid placement, invalid placement (overlap, out of bounds), grid bounds

### Acceptance criteria

- All 18 piece shapes place correctly on the grid
- Ghost preview accurately shows where the piece will land
- Invalid placements rejected with visual feedback
- 3 pieces dealt at a time, new 3 after all placed
- Game-over detected correctly when no piece fits
- Drag-and-drop feels responsive (test on device)

### QA gate

- [ ] Place all 18 piece shapes at least once — all render and place correctly
- [ ] Attempt invalid placements (overlap, edge overflow) — all correctly rejected
- [ ] Fill the grid deliberately until game-over triggers — detection is accurate
- [ ] Drag a piece off-screen and release — returns to tray cleanly
- [ ] Rapid successive placements — no state corruption
- [ ] `GridPlacementTests` pass
- [ ] Device: drag feels responsive, no input lag

---

## Phase 2 — Clearing and scoring

**Objective:** Lines clear, score updates, combos and streaks work, the core loop is complete.

**Spec references:** §3.1 (clearing rules), §3.2 (scoring), §3.8 (combo visuals)

### Tasks

- [ ] Implement `LineClearDetector`: check all 8 rows and 8 columns after each placement
- [ ] Implement simultaneous multi-line clear (combo)
- [ ] Implement clear animation: cells flash white (100ms), dissolve into particles, cells empty
- [ ] Implement combo animation: larger burst, screen shake (3px, 150ms), deeper chime
- [ ] Implement `ScoreManager`: points per clear per spec §3.2 table
- [ ] Implement streak multiplier: consecutive clearing placements, ×1 to ×3, reset on non-clear
- [ ] Implement score display HUD: current score, personal best
- [ ] Implement "New best!" celebration when personal best broken mid-game
- [ ] Implement game-over screen: final score, personal best, "Play again" button
- [ ] Implement instant game restart (sub-1-second transition, no loading screen)
- [ ] Add all clear/combo/streak sound effects
- [ ] Add all clear/combo haptic patterns
- [ ] Write tests: `LineClearTests` (single row, single column, combo, no clear), `ScoreCalculationTests` (all point values, streak multiplier, reset)
- [ ] Write PlayMode smoke test: place pieces from a known sequence, assert correct clears and score

### Acceptance criteria

- Full rows and columns clear correctly
- Multi-line combos score correctly per the table
- Streak multiplier applies and resets correctly
- Score display updates in real-time
- Personal best detection and celebration works
- Game restarts instantly with "Play again"
- Clear animations are satisfying (visual + audio + haptic)
- **Key check: blocks do NOT fall after clearing.** Cleared cells empty, everything else stays.

### QA gate

- [ ] Fill one row completely — it clears correctly, cells empty, particles play
- [ ] Fill one column completely — same
- [ ] Clear 2 lines in one placement — combo scoring and animation both fire
- [ ] Clear 3 placements in a row that each clear lines — streak multiplier reaches ×2
- [ ] Place a piece that clears nothing — streak resets to ×1
- [ ] Break personal best — "New best!" celebration triggers
- [ ] Game over → "Play again" → new game in under 1 second
- [ ] All tests pass
- [ ] Device: 60fps maintained during combo clear with particles

---

## Phase 3 — kintsugi meta

**Objective:** Ceramics appear above the grid, cracks fill with gold on line clears, completed pieces enter the gallery.

**Spec references:** §3.4 (kintsugi meta), §3.5 (gallery), §7.6 (ceramic data format)

### Tasks

- [ ] Create `CeramicDefinition` ScriptableObject class per spec §7.6
- [ ] Create ceramic assets for tiers 1-10: silhouette sprite + crack bezier paths per tier
- [ ] Implement `CeramicManager`: current tier, crack tracking, repair on line clear
- [ ] Implement gold-flow animation: particles rise from cleared row → gold fills one crack along the bezier path (500ms tween)
- [ ] Implement combo repair: 2+ lines = 2+ cracks repaired simultaneously
- [ ] Implement ceramic progress bar below the ceramic display
- [ ] Implement ceramic completion flow per spec §3.4: glow → celebration → gallery → new ceramic
- [ ] Implement persistence: ceramic progress survives game-over (does NOT reset)
- [ ] Implement tier progression: complete ceramic → increment tier → load next ceramic definition
- [ ] Implement tier 10+ looping with colour variants
- [ ] Implement `GalleryManager`: list of completed ceramics with dates and scores
- [ ] Build Gallery screen: scrollable grid of completed ceramics with thumbnails
- [ ] Implement gallery data in save file
- [ ] Add gold-flow and ceramic-completion audio
- [ ] Add ceramic-related haptic patterns

### Acceptance criteria

- Line clears visibly fill cracks with gold
- Combos repair multiple cracks
- Ceramic completion triggers the full celebration flow
- New ceramic appears seamlessly after completion
- Ceramic progress persists across game-overs (verified by dying and replaying)
- Gallery shows all completed ceramics with correct dates
- Gold-flow animation looks and sounds satisfying
- Tier 10+ loops correctly

### QA gate

- [ ] Clear one line → one crack fills with gold, progress bar updates
- [ ] Clear a 3-line combo → 3 cracks fill simultaneously
- [ ] Die with 3/6 cracks repaired → restart → cracks still show 3/6
- [ ] Complete a ceramic through normal play → celebration plays → new ceramic loads → gallery updated
- [ ] Open gallery → completed ceramic shows with correct thumbnail, date, score
- [ ] Reach tier 10, complete it → tier 11 loads as a colour variant of tier 6-9
- [ ] Gold-flow animation runs smoothly on budget device

---

## Phase 4 — Retention and monetization

**Objective:** All meta systems, ads, IAP, analytics, and save/load working.

**Spec references:** §4 (retention), §5 (monetization), §6 (analytics), §7.3 (save data)

### Tasks

**Save and load:**
- [ ] Implement full save data structure per spec §7.3
- [ ] Implement save triggers: after every game-over, ceramic completion, coin change, settings change, `OnApplicationPause`
- [ ] Implement save corruption handling (try-catch, reset to fresh, log)
- [ ] Implement `save_version` and migration hook

**Coins and economy:**
- [ ] Implement `CoinManager` per spec §4.5 economy table
- [ ] Implement undo mechanic: remove last-placed piece, cost 50 coins or rewarded ad
- [ ] Implement piece refresh: discard current 3 pieces, get new 3, cost 75 coins or rewarded ad
- [ ] Implement undo and refresh UI buttons during gameplay

**Retention systems:**
- [ ] Implement daily streak per spec §4.1
- [ ] Implement daily challenge per spec §4.2 (seeded RNG, same pieces for everyone)
- [ ] Implement personal best system and milestone rewards per spec §4.3
- [ ] Implement push notification (day 3 permission, 7pm reminder)
- [ ] Implement first-launch sequence: no ads or permissions in first 3 games
- [ ] Implement Google Play In-App Review API (trigger after 3-star game-over at 20+ total games)

**Monetization:**
- [ ] Integrate AppLovin MAX with Golden Break ad unit IDs
- [ ] Implement 4 rewarded placements per spec §5.1
- [ ] Implement continue mechanic: clear bottom 2 rows + deal new pieces
- [ ] Implement ad failure handling
- [ ] Implement interstitial controller: every 3rd game-over, none in first 3, cap 6/day
- [ ] Implement banner on home and gallery screens
- [ ] Implement IAP: remove interstitials, themes, coin bundles
- [ ] Implement IAP restore purchases
- [ ] Implement GDPR consent flow

**Analytics:**
- [ ] Integrate GameAnalytics with new game key
- [ ] Implement all events per spec §6.2
- [ ] Implement custom dimensions: country, DDA state, total games

**UI screens:**
- [ ] Build Home screen: play button, daily challenge, streak display, current ceramic preview, gallery button, settings
- [ ] Build Game Over screen: score, best score, ceramic progress, continue offer, play again, double coins
- [ ] Build Gallery screen: scrollable completed ceramics
- [ ] Build Settings screen: sound, music, haptics, high contrast, cross-promo link to GLYPH
- [ ] Build Streak popup
- [ ] Build Daily Challenge UI
- [ ] Move all player-facing strings to `Strings.cs`

**Tests:**
- [ ] Write `CoinEconomyTests`: earn, spend, balance, prevent negative
- [ ] Write `GameOverDetectionTests`: edge cases with various piece shapes
- [ ] Write `UndoTests`: undo after non-clearing placement works; undo blocked after line-clear; undo blocked after all 3 placed; max 1 undo per hand
- [ ] Write `RefreshTests`: refresh only when all 3 unplaced; refresh deals new 3; max 1 per hand
- [ ] Write `DailyChallengeSeedTest`: assert seed 20261122 produces a hardcoded known first-20-pieces sequence (cross-platform stability check)

### Acceptance criteria

- Save survives app kill and device restart
- Corrupted save resets gracefully
- Coins earn and spend correctly per the economy table
- Undo removes last piece and restores cells
- Piece refresh deals new 3, discards old
- Streak increments daily, resets on miss
- Daily challenge uses seeded RNG (same seed = same pieces, verified)
- All 4 rewarded placements tested on device
- Interstitial cadence exact
- Analytics events visible in GameAnalytics dashboard
- No hardcoded strings in UI code

### QA gate

- [ ] Complete a game, force-kill app, relaunch — score, ceramic, coins preserved
- [ ] Corrupt save manually — game resets gracefully
- [ ] Earn coins, spend on undo — balance correct, piece removed, cells restored
- [ ] Piece refresh with all 3 unplaced — old pieces gone, new 3 appear
- [ ] Piece refresh after placing 1 piece — button disabled/hidden, cannot refresh partial hand
- [ ] Undo after a non-clearing placement — piece returns to tray, cells restored
- [ ] Undo after a clearing placement — undo button disabled (clear is permanent)
- [ ] Undo twice in one hand — second undo blocked
- [ ] Set device date forward → streak resets correctly
- [ ] Play daily challenge twice in one day — second attempt shows same piece sequence
- [ ] Continue after game-over (rewarded) — bottom rows cleared, new pieces dealt, play continues
- [ ] Airplane mode → trigger rewarded → "Ad unavailable" toast, no reward, no crash
- [ ] Play 10 games → count interstitials → exactly matches the rule
- [ ] Purchase remove-ads → interstitials stop, rewarded stays
- [ ] All tests pass
- [ ] Fresh install → first 3 games have zero interruptions

---

## Phase 5 — Polish, QA, and launch

**Objective:** Visually polished, stable, store-ready, live.

**Spec references:** §8 (art and audio), §9 (store and legal)

### Tasks

**Visual polish:**
- [ ] Finalize block rendering: colour, inner texture pattern, glow
- [ ] Finalize grid rendering: cell states, borders, clear effects
- [ ] Implement palette system as ScriptableObjects (Golden Dark default + 1 additional)
- [ ] Implement high-contrast mode for colourblind accessibility
- [ ] Finalize ceramic rendering: silhouettes, crack paths, gold fill
- [ ] Final UI polish: alignment, spacing, transitions, consistent padding
- [ ] Implement cross-promotion card in Settings (link to GLYPH)

**Audio polish:**
- [ ] Integrate all 9 sound effects per spec §8.3
- [ ] Integrate lo-fi background music loop (reuse GLYPH's or source a second track)
- [ ] Verify audio doesn't clip on rapid combos
- [ ] Verify music loops seamlessly

**Store assets:**
- [ ] **HUMAN:** create app icon 512×512 per spec §2.2
- [ ] **HUMAN:** create feature graphic 1024×500
- [ ] **HUMAN:** create 6 screenshots per spec §9.1
- [ ] **HUMAN:** write store listing per spec §9.2
- [ ] **HUMAN:** update privacy policy to include Golden Break
- [ ] **HUMAN:** complete content rating questionnaire
- [ ] Complete `LICENSES.md` for any new assets

**QA and testing:**
- [ ] Run full EditMode test suite — all pass
- [ ] Run PlayMode smoke test — passes
- [ ] Profile on budget device: frame time < 16.6ms, 0 GC alloc in gameplay
- [ ] Peak memory < 150MB (simpler than GLYPH, should be lower)
- [ ] Verify APK < 35MB
- [ ] Full offline test
- [ ] Save edge cases: app kill during game, corrupt save, fresh install
- [ ] All game-over/continue/restart flows
- [ ] All ad placements on device
- [ ] All IAP flows on device
- [ ] 30 minutes continuous play with no crash
- [ ] Memory leak check over 30-minute session
- [ ] Fix all compiler warnings

**Release:**
- [ ] **HUMAN:** generate release keystore (or reuse GLYPH publisher account keystore if same publisher)
- [ ] **HUMAN:** back up keystore (if new)
- [ ] Build signed AAB (IL2CPP, ARM64+ARMv7, stripping, ProGuard)
- [ ] Verify AAB size
- [ ] **HUMAN:** upload to Play Console, complete listing, submit for review
- [ ] **HUMAN:** configure closed testing track

**Post-launch (ongoing):**
- [ ] Monitor crashes daily
- [ ] Monitor D1 after 48 hours
- [ ] Apply decision thresholds from spec §6.4 after 1,000 installs
- [ ] Respond to reviews twice weekly
- [ ] Ship first update (bug fixes) within 2 weeks
- [ ] Record 10+ gameplay clips for social media / creative testing
- [ ] Begin daily organic video posts (gold-repair is the scroll-stopper)
- [ ] Cross-promote: add Golden Break link to GLYPH's Settings screen in GLYPH's next update

### Acceptance criteria

- Game looks finished, sounds polished, feels responsive
- All store assets exist and meet Play Store requirements
- No crashes in 30 minutes of continuous play
- APK under 35MB, 60fps on budget device
- Analytics events arriving in dashboard

### QA gate — full regression

- [ ] Fresh install → play 5 games without issue
- [ ] Complete one ceramic through normal play — full flow works
- [ ] Daily challenge → daily streak → milestone reward → all function
- [ ] All 4 rewarded placements work on device
- [ ] Remove-ads IAP tested
- [ ] Save/load survives app kill, device restart, and corruption
- [ ] Offline: entire game works in airplane mode
- [ ] 30 minutes with no crash and no memory growth
- [ ] Cross-promo link opens GLYPH store page

---

## Phase summary

| Phase | Focus | Duration | Human gates |
|---|---|---|---|
| 0 | Project setup + GLYPH reuse | 0.5 day | — |
| 1 | Grid and pieces | 4-5 days | Device drag-drop test |
| 2 | Clearing and scoring | 3-4 days | — |
| 3 | kintsugi meta | 3-4 days | Ceramic art assets |
| 4 | Retention and monetization | 5-6 days | Ad account IDs |
| 5 | Polish, QA, launch | 5-6 days | Icon, screenshots, store listing, keystore |
| **Total** | | **~4 weeks** | |

---

## Launch checklist

### Core gameplay
- [ ] 8×8 grid with placement, clearing, and game-over detection
- [ ] All 18 piece shapes defined and working
- [ ] Drag-and-drop with ghost preview
- [ ] Blocks do NOT fall after clearing
- [ ] Multi-line combo scoring correct
- [ ] Streak multiplier correct
- [ ] DDA piece-weight adjustment active
- [ ] Undo working: blocked after line-clear, blocked after all 3 placed, max 1 per hand
- [ ] Piece refresh working: only when all 3 unplaced, max 1 per hand
- [ ] Continue mechanic: clears bottom 2 rows, deals new 3, max 1 per game
- [ ] Grid coordinates: (0,0) = top-left verified
- [ ] Daily challenge seed produces identical pieces on 2 different devices

### kintsugi meta
- [ ] 10 ceramic tiers with crack paths
- [ ] Gold-flow animation on line clear
- [ ] Ceramic completion celebration
- [ ] Progress persists across game-overs
- [ ] Tier 10+ loops with colour variants
- [ ] Gallery displays all completed ceramics

### Monetization
- [ ] 4 rewarded placements tested on device
- [ ] Continue mechanic (clear bottom 2 rows)
- [ ] Ad failure handling
- [ ] Interstitial: every 3rd game-over, none in first 3, cap 6/day
- [ ] All IAP tested
- [ ] GDPR consent flow
- [ ] Ads initialize only after 3rd game

### Retention
- [ ] Daily streak with rewards
- [ ] Daily challenge with seeded RNG
- [ ] Personal best and milestones
- [ ] Coin economy balanced
- [ ] In-app review prompt
- [ ] Push notification (day 3+, permission-gated)

### Technical
- [ ] Save/load, corruption handling, `save_version`
- [ ] String table
- [ ] 60fps on budget device
- [ ] 0 GC alloc in gameplay
- [ ] APK < 35MB
- [ ] Fully offline
- [ ] Portrait locked, min API 24
- [ ] Object pooling
- [ ] All tests pass

### Store
- [ ] Icon, feature graphic, 6 screenshots
- [ ] Store listing complete
- [ ] Privacy policy updated
- [ ] Content rating done
- [ ] Ads + IAP declared
- [ ] Keystore backed up
- [ ] Cross-promotion to GLYPH in Settings
