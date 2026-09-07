# Task 2 Report: Graphics & Asset Pipeline (`SpriteManager`)

**Date:** 2026-09-07  
**Status:** Completed  
**Commit:** `2cf4af9b93c057397af661ca1ec15aa66beb2a79` (`feat: implement Aseprite JSON parsing and CroppedBitmap sprite slicing`)

---

## 1. Summary of Work

Implemented the asset pipeline for Desktop Pets MVP, enabling deserialization of Aseprite JSON metadata, slicing sprite sheets into WPF `CroppedBitmap` frames, and caching animation clips via `SpriteManager`.

### Files Created & Modified
- **`Creature/Graphics/AsepriteMetadata.cs`**: Data records (`AsepriteMetadata`, `AsepriteFrameEntry`, `AsepriteRect`, `AsepriteMeta`, `AsepriteSize`) configured with `[JsonPropertyName]` attributes for JSON deserialization.
- **`Creature/Graphics/AnimationClip.cs`**: Class encapsulating `Name`, `Frames` (`BitmapSource[]`), `FrameDurationMs` (defaults to 100ms if <= 0), and `Loop` flag.
- **`Creature/Graphics/SpriteManager.cs`**: Asset manager providing `LoadAnimation(string name, string jsonPath, string pngPath, bool loop = true)` and `GetClip(string name)`. Loads master `BitmapImage`, performs zero-offset CroppedBitmap sub-rect slicing, freezes images for thread-safety, and caches results by name case-insensitively.
- **`Creature.Tests/Creature.Tests.csproj`**: Added `Xunit.StaFact` 1.1.11 to support `[StaFact]` test methods requiring WPF STA thread context on xUnit 2.x.
- **`Creature.Tests/Graphics/SpriteManagerTests.cs`**: Unit tests verifying animation loading, frame count, duration, dimensions, retrieval, and caching.

---

## 2. TDD Workflow Verification

1. **RED Phase**:
   - Authored tests in `Creature.Tests/Graphics/SpriteManagerTests.cs`.
   - Executed `dotnet test --filter "FullyQualifiedName~SpriteManagerTests"`.
   - Verified expected failure: compilation failed with `CS0234: The type or namespace name 'Graphics' does not exist in the namespace 'Creature'`.
2. **GREEN Phase**:
   - Implemented `AsepriteMetadata`, `AnimationClip`, and `SpriteManager`.
   - Executed `dotnet test`.
   - Verified 7/7 tests passed cleanly (0 failed, 0 warnings, 0 errors).
3. **REFACTOR / Verification**:
   - Ran `dotnet test -v normal` across entire solution.
   - All tests succeeded in under 1 second.

---

## 3. Findings & Noteworthy Observations

- **`BunnyLieDown` Frame Count**: In the initial draft spec, `BunnyLieDown` was noted as having 2 frames. Examination of the actual asset file `BunnyLieDown.json` revealed a 128x32 sprite sheet with 4 distinct 32x32 frames (`BunnyLieDown 0` through `BunnyLieDown 3`), each 100ms. The tests were configured to assert `clip.Frames.Length.Should().Be(4)` to accurately validate the real asset.
- **`Xunit.StaFact` Package Compatibility**: The latest major version of `Xunit.StaFact` (v4.0.23) targets `xunit.v3`. For the existing `xunit 2.9.3` test runner, version `1.1.11` is the compatible release. It was installed and validated against .NET 10.0.

---

## 4. Test Results

```
Test run for D:\projects\Creature\Creature.Tests\bin\Debug\net10.0-windows\Creature.Tests.dll (.NETCoreApp,Version=v10.0)
  Passed Creature.Tests.SanityTests.Environment_ShouldTargetNet10 [63 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_CalledMultipleTimes_ShouldReturnCachedInstance [155 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_WithBunnyJump_ShouldLoadEighteenFrames [2 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.GetClip_WhenAnimationLoaded_ShouldReturnSameClip [1 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.GetClip_WhenAnimationNotLoaded_ShouldReturnNull [< 1 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_WithBunnyLieDown_ShouldLoadFourFramesWith100msDuration [4 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_WithBunnyRun_ShouldLoadFiveFrames [1 ms]

Total tests: 7
Passed: 7
Failed: 0
Total time: 0.9912 Seconds
```

---

## 5. Next Steps

Ready for downstream integration in subsequent tasks (Task 3 State Machine and Task 4 Pet Window rendering loop).
