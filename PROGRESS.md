# Golden Break Build Progress

**Current phase:** 0
**Last updated:** 2026-08-17
**Blocked on:** nothing — clarifications resolved, proceeding with Phase 0.

## Phase status
- [~] Phase 0 — Project setup (fresh build, no GLYPH reuse — see deviations)
- [ ] Phase 1 — Grid and pieces
- [ ] Phase 2 — Clearing and scoring
- [ ] Phase 3 — kintsugi meta
- [ ] Phase 4 — Retention and monetization
- [ ] Phase 5 — Polish, QA, and launch

## Reused from GLYPH
(list of files/systems copied and any modifications made)

## Deviations from spec
- **Piece set is 20 shapes, not 18.** Spec §3.1 prose says "18 shapes" but both the ASCII diagram and the spawn-weight table enumerate 20 distinct pieces (single, 1x2, 2x1, 1x3, 3x1, 1x4, 4x1, 1x5, 5x1, 2x2, L, J, S, Z, T, 2x3, 3x2, L3, J3, 3x3). Decision (user, 2026-08-17): keep all 20 as literally specified. L3/J3 are distinct taller trominoes from L/J — exact cell coordinates to be defined in Phase 1 when `PieceDefinition` assets are created.
- **GameAnalytics game key and AppLovin MAX ad unit IDs are placeholders.** These require dashboard access I don't have. Decision (user, 2026-08-17): proceed through Phase 0 with placeholder/dummy IDs clearly marked as TODO in `AdManager`/`AnalyticsManager`; real IDs to be swapped in later once the user creates the accounts.
- **`AudioManager.cs`, `HapticManager.cs`, `AdManager.cs`, `AnalyticsManager.cs` do not exist in GLYPH to copy from.** Verified directly against `D:\GLYPH`'s actual codebase: GLYPH is at Phase 6, and its own PROGRESS.md confirms ads/analytics SDK wiring is blocked on real account creation (a human task GLYPH hasn't done either) — `Assets/Plugins/AppLovinMAX/` and `Assets/Plugins/GameAnalytics/` in GLYPH are empty folders, no SDK ever imported, no wrapper classes written.
- **SUPERSEDED — Golden Break is fully greenfield, zero reuse from GLYPH.** Decision (user, 2026-08-17): "GLYPH is incomplete project, this project is now priority, so make it from scratch and complete bug free." Every file in Golden Break — including `ObjectPool.cs`, `SaveManager.cs`, `Constants.cs`/`Strings.cs`, DOTween setup, and all four managers above — is written original for this project. `D:\GLYPH` is reference/inspiration only (e.g. its architecture rules, its batch-mode Unity automation technique), never a copy source. BUILD_PLAN.md's Phase 0 task list ("copy from GLYPH") is treated as superseded by this instruction throughout. Every system built here is also expected to be actually verified (in-Editor / on-device / tests passing), not left at "compiles but never run" — a bar GLYPH itself did not consistently hit.
- DOTween is a third-party plugin (Demigiant), not GLYPH's own code, so obtaining it counts as a dependency install, not reuse — acquired the same way GLYPH did (direct download from Demigiant, no Asset Store login), not copied from `D:\GLYPH`'s folder.
- **Two real bugs found and fixed in `Assets/Editor/ProjectSetup.cs` (original script, written for Golden Break) during Phase 0 verification, not left unverified:**
  1. `CreateFolderStructure()`'s folder list omitted `Assets/Scenes`. `EditorSceneManager.SaveScene` failed with "Parent directory must exist" — logged as an `Debug.LogError` but did not throw, so `CreateScenes()` proceeded to add three nonexistent scene paths to `EditorBuildSettings.scenes` without anything surfacing the failure. Fixed by adding the folder, and by making `CreateScenes()` verify `File.Exists` after each save and throw if it's missing, per BUILD_PLAN Part 1's "never fail silently" rule.
  2. `new EditorBuildSettingsScene(path, true)` (the string-path constructor) serializes an all-zero GUID into `EditorBuildSettings.asset` even though the scene's own `.meta` file has a correct one — confirmed across three separate rebuild attempts, including one with a post-save `AssetDatabase.Refresh()` that made no difference. Switched to the `(GUID, bool)` constructor via `AssetDatabase.AssetPathToGUID` + `GUID.TryParse`, which is guaranteed correct — but the on-disk field *still* serializes as zero. Investigated further and confirmed this doesn't matter functionally: `BuildPlayerOptions.scenes` is populated directly from the `.path` field (see `BuildAndroidInternal`), never from this cached GUID, and an actual empty Android build succeeded end-to-end (`Builds/Android/GoldenBreak.apk`, 26.4MB, well under the 35MB budget) — so this is a cosmetic serialization quirk of scripted `EditorBuildSettings.scenes` assignment in batch mode on this Unity version, not a real defect. Left the GUID-typed constructor in place since it's the more correct call regardless.
- Phase 0 QA gate item **"Build empty APK → installs and launches on device"** — the build half is done and verified (`ProjectSetup.BuildAndroid`, `BuildResult.Succeeded`). Install/launch verification on a physical device or emulator has not been done yet.
- **Two more real bugs found while wiring up assembly definitions, both fixed:**
  1. `com.unity.nuget.newtonsoft-json` ships as raw precompiled DLLs (`Runtime/Newtonsoft.Json.dll`) with no `.asmdef` of its own — unlike NUnit, there's no assembly name to add to `references`. `GoldenBreak.Tests.EditMode.asmdef`'s `overrideReferences: true` (needed for `nunit.framework.dll`) also silently cut off Unity's automatic precompiled-DLL referencing, so `SaveManagerTests.cs`'s `using Newtonsoft.Json;` failed to compile. Fixed by adding `"Newtonsoft.Json.dll"` alongside `"nunit.framework.dll"` in `precompiledReferences`.
  2. `SaveManager.LoadFrom`, `HapticManager.GetPatternMilliseconds`, and `AnalyticsManager.FormatParameters` were deliberately `internal` (test seams, not public API) — but `GoldenBreak.Runtime` and `GoldenBreak.Tests.EditMode` are separate assemblies once asmdefs exist, and `internal` doesn't cross assembly boundaries. Fixed with `Assets/Scripts/AssemblyInfo.cs` (`[assembly: InternalsVisibleTo("GoldenBreak.Tests.EditMode")]`) rather than widening these to `public` just to make them reachable.
- **All 23 EditMode tests passing, 0 compile errors** (`ObjectPoolTests` ×8, `SaveManagerTests` ×4, `HapticManagerTests` ×5, `AdManagerTests` ×2, `AnalyticsManagerTests` ×4) — verified via `Unity.exe -runTests -testPlatform EditMode` (note: `-runTests` must NOT be combined with `-quit`; the two race and `-quit` can win before the test runner writes results, which happened on the first attempt).

## Environment notes
- Unity 2022.3.62f3 (2022 LTS) confirmed installed at `D:\Unity\Editor\2022.3.62f3\Editor\Unity.exe`, with Android Player support present.
- GLYPH reference project confirmed at `D:\GLYPH` (real Unity project with git history, Assets/Scripts, ProjectSettings, PROGRESS.md, LICENSES.md) — this is the source for Phase 0 reuse.
- GLYPH's `BUILD_PLAN.md` Part 1 (engineering standards, referenced by this project's BUILD_PLAN.md) is not present in `D:\GLYPH` itself — it only exists at `C:\Users\vanda\Downloads\GLYPH_Build_Execution_Plan.md`. Recommend copying it into this repo so the standards are version-controlled alongside the project that depends on them.
- Claude Design project "Golden Break UI Design" (`f6bc7123-19dc-4328-bc46-d8a2b6f97e64`) reviewed — contains a static HTML/CSS visual mockup of 6 screens plus the generic Claude Design rendering runtime. No portable game logic; useful only as a visual/UX reference for the Unity UI build in Phase 4-5.
