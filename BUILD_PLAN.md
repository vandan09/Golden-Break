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
- [x] Phase 0 — Project setup (fresh build, no GLYPH reuse — see PROGRESS.md deviations)
- [x] Phase 1 — Grid and pieces
- [x] Phase 2 — Clearing and scoring
- [x] Phase 3 — kintsugi meta
- [ ] Phase 4 — Retention and monetization
- [ ] Phase 5 — Polish, QA, and launch

See PROGRESS.md for the live, detailed log of decisions, deviations, and verification
evidence per phase — this checklist just mirrors its top-line status.

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
- [x] Implement full save data structure per spec §7.3
- [x] Implement save triggers: after every game-over, ceramic completion, coin change, settings change, `OnApplicationPause`
- [x] Implement save corruption handling (try-catch, reset to fresh, log)
- [x] Implement `save_version` and migration hook

**Coins and economy:**
- [x] Implement `CoinManager` per spec §4.5 economy table
- [x] Implement undo mechanic: remove last-placed piece, cost 50 coins or rewarded ad
- [x] Implement piece refresh: discard current 3 pieces, get new 3, cost 75 coins or rewarded ad
- [x] Implement undo and refresh UI buttons during gameplay

**Retention systems:**
- [x] Implement daily streak per spec §4.1
- [x] Implement daily challenge per spec §4.2 (seeded RNG, same pieces for everyone) — reworked mid-Phase-4 into a fully separate object graph plus two hard-mode mechanics (large-piece-only pool, pre-filled obstacles) and its own per-attempt kintsugi medallion, per direct player feedback — see PROGRESS.md
- [x] Implement personal best system and milestone rewards per spec §4.3
- [ ] Implement push notification (day 3 permission, 7pm reminder) — gating logic implemented and tested (`PushNotificationManager`); the actual native Android permission request/notification scheduling is still a `TODO` stub (no `AndroidJavaObject` calls made yet). Doesn't need a paid SDK/account, just native integration work not yet done.
- [x] Implement first-launch sequence: no ads or permissions in first 3 games
- [ ] Implement Google Play In-App Review API (trigger after 3-star game-over at 20+ total games) — gating logic implemented and tested (`ReviewManager`), triggered on first completed ceramic instead (documented interpretation, see its own doc comment); the actual Play Core `RequestReviewFlow` call is still a `TODO` stub.

**Monetization:**
- [ ] Integrate AppLovin MAX with Golden Break ad unit IDs — blocked on a real AppLovin MAX account/ad unit IDs, not on more code; `AdManager` always reports "unavailable" by design until these exist (CLAUDE.md §5.1's required failure behavior)
- [x] Implement 4 rewarded placements per spec §5.1
- [x] Implement continue mechanic: clear bottom 2 rows + deal new pieces
- [x] Implement ad failure handling
- [x] Implement interstitial controller: every 3rd game-over, none in first 3, cap 6/day
- [x] Implement banner on home and gallery screens
- [x] Implement IAP: remove interstitials, themes, coin bundles (entitlement bookkeeping fully implemented/tested; the actual store purchase flow is a stub blocked on real Play Billing product IDs)
- [ ] Implement IAP restore purchases — reports "unavailable" without touching any entitlement (never silently grants/revokes); a real restore needs the store SDK this project doesn't have yet
- [ ] Implement GDPR consent flow — resolves immediately to the conservative "no personalized ads" default; the real CMP dialog ships with the AppLovin MAX SDK, which isn't integrated yet

**Analytics:**
- [ ] Integrate GameAnalytics with new game key — blocked on a real GameAnalytics account/game key; `AnalyticsManager` logs every event locally instead of throwing, ready to forward the moment the SDK exists
- [x] Implement all events per spec §6.2
- [x] Implement custom dimensions: country, DDA state, total games

**UI screens:**
- [x] Build Home screen: play button, daily challenge, streak display, current ceramic preview, gallery button, settings
- [x] Build Game Over screen: score, best score, ceramic progress, continue offer, play again, double coins
- [x] Build Gallery screen: scrollable completed ceramics
- [x] Build Settings screen: sound, music, haptics, high contrast, cross-promo link to GLYPH
- [x] Build Streak popup
- [x] Build Daily Challenge UI
- [x] Move all player-facing strings to `Strings.cs`

**Tests:**
- [x] Write `CoinEconomyTests`: earn, spend, balance, prevent negative
- [x] Write `GameOverDetectionTests`: edge cases with various piece shapes
- [x] Write `UndoTests`: undo after non-clearing placement works; undo blocked after line-clear; undo blocked after all 3 placed; max 1 undo per hand
- [x] Write `RefreshTests`: refresh only when all 3 unplaced; refresh deals new 3; max 1 per hand
- [x] Write `DailyChallengeSeedTest`: assert seed 20261122 produces a hardcoded known first-20-pieces sequence (cross-platform stability check)

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
- [x] Finalize block rendering: colour, inner texture pattern, glow — the 5 patterns (dots/diagonal lines/crosshatch/horizontal lines/circles per §8.2) generated procedurally (`PatternSprite`), no sourced art needed; glow itself still needs real shader/art work, not done
- [ ] Finalize grid rendering: cell states, borders, clear effects — cell states/clear-flash/pattern fill all done; a distinct border stroke around each cell (§8.1: "#2a2a4a border") not added yet
- [ ] Implement palette system as ScriptableObjects (Golden Dark default + 1 additional) — `UiPalette` is currently a static class with Golden Dark's values hardcoded; a real ScriptableObject-based system touching every screen that references it is a bigger, focused refactor, not started yet
- [x] Implement high-contrast mode for colourblind accessibility — `SettingsScreen`'s toggle now live-updates `UiPalette.HighContrastEnabled` (§3.9's "pattern opacity 15% -> 40%"), which the same procedural patterns read directly; both grids repaint immediately on toggle
- [ ] Finalize ceramic rendering: silhouettes, crack paths, gold fill — crack-path/gold-fill/progress-bar/completion-celebration logic is fully built and tested (`CeramicView`/`CeramicManager`); the actual per-tier silhouette art is still a flat placeholder shape — §8.1 calls for "one SVG exported from Figma" per tier, real design work this can't substitute for
- [ ] Final UI polish: alignment, spacing, transitions, consistent padding — no Figma mockups exist to polish toward; current layout is functional placeholder spacing, same as every other "no final art yet" system in this project
- [x] Implement cross-promotion card in Settings (link to GLYPH) — built (`SettingsScreen.BuildCrossPromoCard`); logs intent instead of opening a URL since GLYPH's Play Store listing doesn't exist yet (**HUMAN**, once published)

**Audio polish:**
- [ ] Integrate all 9 sound effects per spec §8.3 — **HUMAN:** needs real clips sourced from Freesound.org/Mixkit per the spec's own instruction; `AudioManager`'s `SoundEffect` enum and every `PlaySound` call site already exist and are wired, waiting on actual `AudioClip` assets
- [ ] Integrate lo-fi background music loop (reuse GLYPH's or source a second track) — **HUMAN:** same sourcing gap
- [ ] Verify audio doesn't clip on rapid combos — can't verify without real audio files
- [ ] Verify music loops seamlessly — can't verify without a real audio file

**Store assets:**
- [ ] **HUMAN:** create app icon 512×512 per spec §2.2
- [ ] **HUMAN:** create feature graphic 1024×500
- [ ] **HUMAN:** create 6 screenshots per spec §9.1
- [ ] **HUMAN:** write store listing per spec §9.2
- [ ] **HUMAN:** update privacy policy to include Golden Break
- [ ] **HUMAN:** complete content rating questionnaire
- [x] Complete `LICENSES.md` for any new assets — created; documents DOTween (the one third-party asset actually imported) and flags sound/music/ceramic-art as pending entries once those are sourced

**QA and testing:**
- [x] Run full EditMode test suite — all pass (377/377)
- [ ] Run PlayMode smoke test — passes — no PlayMode test assembly exists yet, only EditMode; on-device manual verification has substituted for this so far
- [ ] Profile on budget device: frame time < 16.6ms, 0 GC alloc in gameplay — not yet profiled with the Unity Profiler; needs a USB-connected profiling session, not just "no crashes observed"
- [ ] Peak memory < 150MB (simpler than GLYPH, should be lower) — not yet measured
- [ ] Verify APK < 35MB — current raw testing APK is 42MB (IL2CPP + dual ARMv7/ARM64 native libs, already at `ManagedStrippingLevel.High`); this is expected for a "fat" universal APK and should be re-measured against the actual Release-stage signed **AAB** instead, which is what the Play Store delivers per-architecture (roughly half this size per device) — not a real regression, but flagging rather than quietly assuming it'll be fine
- [x] Full offline test — verified on emulator: app force-killed, `airplane_mode_on` enabled via `settings`/`am broadcast`, relaunched clean, no crash, no exceptions, save data intact
- [x] Save edge cases: app kill during game, corrupt save — both verified on emulator (force-stop mid-game + relaunch preserves ceramic/coin progress; hand-corrupted `shared_prefs` XML triggers `SaveManager`'s own logged "save data corrupted" catch and resets to a fresh, fully-functional save with zero crash). Fresh install not yet separately verified (implied by every rebuild+reinstall already done this session, but not explicitly checked as its own case)
- [ ] All game-over/continue/restart flows
- [ ] All ad placements on device — **blocked until Phase 6's AppLovin MAX integration exists**
- [ ] All IAP flows on device — **blocked until Phase 6's Play Billing integration exists**
- [ ] 30 minutes continuous play with no crash
- [ ] Memory leak check over 30-minute session
- [ ] Fix all compiler warnings — 1 remains: `AdManager.OnRewardedWatched` is declared but never invoked yet, since the real ad-SDK callback that would fire it is Phase 6 scope (`ShowRewarded`'s `TODO(ads-setup)`); resolves itself once Phase 6 wires the real callback, not worth suppressing or faking now

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
- Analytics events arriving in dashboard — **blocked until Phase 6's GameAnalytics integration exists**; local stub logging is verifiable now

### QA gate — full regression

- [ ] Fresh install → play 5 games without issue
- [ ] Complete one ceramic through normal play — full flow works
- [ ] Daily challenge → daily streak → milestone reward → all function
- [ ] All 4 rewarded placements work on device — **blocked until Phase 6's AppLovin MAX integration exists**
- [ ] Remove-ads IAP tested — **blocked until Phase 6's Play Billing integration exists**
- [ ] Save/load survives app kill, device restart, and corruption — app-kill and corruption both verified on emulator (see Phase 5 notes); a full OS-level device *restart* (not just app force-stop) not separately tested
- [x] Offline: entire game works in airplane mode — verified on emulator (Home screen + relaunch); full gameplay loop specifically under airplane mode not separately re-walked, but nothing in the game has a network dependency yet (no ad/analytics SDK integrated)
- [ ] 30 minutes with no crash and no memory growth
- [ ] Cross-promo link opens GLYPH store page — **blocked until GLYPH's Play Store listing is published (HUMAN)**

---

## Phase 6 — Real SDK & native platform integration

**Objective:** Everything Phase 4 built in stub/placeholder form (per CLAUDE.md §5.1's own required failure-safe behavior: "no reward granted, player never blocked") actually talks to a real service, once the accounts/credentials to do so exist.

**Spec references:** §5 (monetization), §6 (analytics), §4.1/§4.3 (push notification / in-app review)

Not part of the original 5-phase plan — split out once Phase 4's actual application code was verified complete and it became clear the remaining items are blocked on external prerequisites (developer accounts, store product setup, native platform API calls) rather than on more game logic. Every gating/entitlement/analytics-event decision this phase depends on is already built and unit-tested (`AdManager`, `AnalyticsManager`, `IapManager`, `PushNotificationManager`, `ReviewManager`) — every task below is specifically about wiring that already-correct logic to a real backend, not designing new behavior.

### Tasks

**AppLovin MAX (ads):**
- [ ] **HUMAN:** create an AppLovin MAX account, register the app, obtain rewarded/interstitial/banner ad unit IDs
- [ ] Import the AppLovin MAX Unity SDK
- [ ] Replace `AdManager`'s placeholder ad unit ID constants with the real ones
- [ ] Wire `MaxSdk.InitializeSdk()` into `AdManager.InitializeSdk()`
- [ ] Wire real `MaxSdk.ShowRewardedAd`/`ShowInterstitial`/banner calls, replacing the `TODO(ads-setup)` stubs
- [ ] Wire the real AppLovin MAX CMP (`MaxCmpService`) into `AdManager.RequestConsentIfRequired`, replacing the conservative-default stub

**GameAnalytics:**
- [ ] **HUMAN:** create/confirm a GameAnalytics account (same account as GLYPH per §6.1), obtain a new game key for Golden Break
- [ ] Import the GameAnalytics Unity SDK
- [ ] Wire `GameAnalytics.Initialize()` into `AnalyticsManager.InitializeSdk()`
- [ ] Wire real `GameAnalytics.NewDesignEvent` calls into `AnalyticsManager.LogEvent`
- [ ] Wire real `SetCustomDimension01/02/03` calls into `AnalyticsManager.SetCustomDimensions`
- [ ] Verify all §6.2 events actually arrive in the GameAnalytics dashboard

**Play Billing (IAP):**
- [ ] **HUMAN:** create the 4 in-app products in Play Console (remove_ads, theme_pack, coins_500, coins_2000 — ids must match `IapManager.ResolveStoreItemId`)
- [ ] Import Unity IAP (or Play Billing directly)
- [ ] Wire the real purchase flow into `IapManager`'s `showPurchaseFlow` delegate
- [ ] Implement real `IapManager.RestorePurchases` against the store's purchase history, replacing the always-"unavailable" stub

**Native Android platform calls (no external account needed — pure integration work):**
- [ ] Implement the real Android 13+ `POST_NOTIFICATIONS` permission request in `PushNotificationManager.RequestPermission`
- [ ] Implement real 7pm-local daily-reminder scheduling (`AlarmManager`/`WorkManager`) in `PushNotificationManager.ScheduleDailyReminderIfEligible`
- [ ] Import the Play Core review library and wire `ReviewManager.RequestReviewIfEligible`'s real `RequestReviewFlow`/`LaunchReviewFlow` call

### Acceptance criteria

- All 4 rewarded placements + interstitial + banner show real ads on a real device
- The real AppLovin CMP dialog appears for EEA users
- Analytics events are visible in the GameAnalytics dashboard within a few minutes of firing
- A real purchase (sandbox/test account) grants the correct entitlement and persists it
- Restore purchases re-grants entitlements after a fresh install
- The real Android notification permission prompt appears on session 3
- A 7pm-local streak reminder notification actually fires when eligible
- The first completed ceramic triggers the real Play In-App Review sheet

### QA gate

- [ ] Each of the 4 rewarded placements tested end-to-end on device with a real (test) ad
- [ ] Interstitial cadence (every 3rd game-over, none in first 3, cap 6/day) verified against real interstitial calls, not just the counter logic
- [ ] Banner visible on Home/Gallery, absent everywhere else, on device
- [ ] Purchase + restore tested with a Play Console test account
- [ ] GDPR consent dialog tested with a VPN/test device set to an EEA region
- [ ] Analytics dashboard cross-checked against the local stub logs for the same session (event counts match)
- [ ] Push notification permission + 7pm scheduling tested on a real device across a day boundary
- [ ] In-app review sheet appears after a real first ceramic completion
- [ ] This closes the specific gaps flagged in Phase 5's own QA gate ("All 4 rewarded placements work on device," "Remove-ads IAP tested") — those Phase 5 items are blocked until this phase's HUMAN prerequisites are met, not skippable

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
| 6 | Real SDK & native platform integration | 1-2 days once accounts exist | AppLovin MAX account, GameAnalytics account, Play Console product setup |
| **Total** | | **~4 weeks + Phase 6** | |

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
