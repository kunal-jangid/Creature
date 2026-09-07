# Task 4 Report: Pet Entity Model & Bunny MVP Behaviors

**Date:** 2026-09-07  
**Status:** Completed  
**Commit:** `a5c3425c2df12c557d1ce22e60dc93e5a5678de5` (`feat: implement BunnyEntity with Idle, Roam, and Jump FSM states`)

---

## 1. Summary of Work

Implemented the desktop pet entity model, WPF visual representation, and Bunny MVP finite state machine behaviors:
- **`Entities/DesktopPet.cs`**: Abstract base class owning `Transform2D`, `PhysicsBody`, `StateMachine<DesktopPet>`, and WPF `Image` (`VisualElement`). Manages animation frame stepping, nearest-neighbor bitmap scaling, WPF `Canvas` positioning and scale transform sync, monitor floor recalculation (`UpdateFloor()`), bounding box containment hit testing (`HitTest(Point)`), and the abstract `OnCursorHover()` hook.
- **`Entities/Bunny/BunnyEntity.cs`**: Concrete `DesktopPet` subclass initialized to `BunnyIdleState`. Implements `OnCursorHover()` which triggers a flinch jump transition (`BunnyJumpState`) when not already jumping, plus `SetRoamDirection(bool movingRight, double speed)` helper.
- **`Entities/Bunny/BunnyIdleState.cs`**: Plays `BunnyLieDown` looping animation, zeroes physics velocity, and remains idle for 3–7 seconds before transitioning to `BunnyRoamState`.
- **`Entities/Bunny/BunnyRoamState.cs`**: Plays `BunnyRun` looping animation, moves at ~50 px/sec, flips `ScaleTransform.ScaleX` on direction changes, bounces/reverses direction when reaching the monitor boundaries (`Left` or `Right`), and transitions back to `BunnyIdleState` after 2–5 seconds.
- **`Entities/Bunny/BunnyJumpState.cs`**: Plays `BunnyJump` non-looping animation (18 frames * 100ms = 1.8s), imparts upward jump velocity `Vy = -350`, follows a physics-driven parabolic arc, and transitions back to `BunnyIdleState` once the jump duration completes and the bunny lands on the ground.
- **`Creature.Tests/Entities/BunnyEntityTests.cs`**: Suite of 10 tests utilizing `[StaFact]` (for WPF UI thread safety) covering initial idle state, hover-to-jump trigger, hover jump idempotency, boundary bounce logic (left and right), hit testing, floor dynamic recalculation, WPF Canvas / ScaleTransform visual syncing, and animation frame advancement.

---

## 2. TDD Workflow Verification

1. **RED Phase**:
   - Authored test cases in `Creature.Tests/Entities/BunnyEntityTests.cs`.
   - Executed `dotnet test --filter "FullyQualifiedName~BunnyEntityTests"`.
   - Verified compilation failure as expected (`CS0234: The type or namespace name 'Entities' does not exist in the namespace 'Creature'`).
2. **GREEN Phase**:
   - Implemented `DesktopPet.cs`, `BunnyEntity.cs`, `BunnyIdleState.cs`, `BunnyRoamState.cs`, and `BunnyJumpState.cs`.
   - Resolved type ambiguities between `System.Windows.Controls.Image` / `System.Drawing.Image` and `System.Windows.Point` / `System.Drawing.Point` resulting from `<UseWindowsForms>true</UseWindowsForms>`.
   - Executed `dotnet test`.
   - Verified all 22 tests across the solution passed with zero errors or warnings.
3. **COMMIT**:
   - Committed changes via `git commit -m "feat: implement BunnyEntity with Idle, Roam, and Jump FSM states"` (`a5c3425`).

---

## 3. Key Technical Decisions & Observations

- **WPF / WinForms Namespace Disambiguation**: Because the project enables both `<UseWPF>` and `<UseWindowsForms>` (for notify icon tray support in future tasks), `Point` and `Image` were explicitly aliased to `System.Windows.Point` and `System.Windows.Controls.Image`.
- **Visual Element NearestNeighbor Scaling**: `RenderOptions.SetBitmapScalingMode(VisualElement, BitmapScalingMode.NearestNeighbor)` is initialized directly on the pet's WPF `Image`, ensuring pixel art remains crisp when scaled.
- **Decoupled Physics & Visual Synchronization**: `DesktopPet.Update(dt)` runs the state machine, advances kinematic physics, steps animation frame timers, and synchronizes `Canvas.SetLeft`, `Canvas.SetTop`, and `VisualElement.RenderTransform` (mirroring with `ScaleX = -Scale` when `IsFacingLeft == true`).
- **STA Unit Testing**: Because WPF `Image` and UI transforms require a single-threaded apartment (STA) thread, all entity tests use `[StaFact]` from `Xunit.StaFact`.

---

## 4. Test Results

```
Test run for D:\projects\Creature\Creature.Tests\bin\Debug\net10.0-windows\Creature.Tests.dll (.NETCoreApp,Version=v10.0)
  Passed Creature.Tests.SanityTests.Environment_ShouldTargetNet10 [18 ms]
  Passed Creature.Tests.Core.PhysicsTests.Transform2D_ScaledDimensionsAndBoundingBox_ShouldReflectScaleAndPosition [18 ms]
  Passed Creature.Tests.Core.StateMachineTests.StateMachine_ShouldExecuteEnterUpdateAndExitCallbacks [18 ms]
  Passed Creature.Tests.Core.StateMachineTests.StateMachine_WhenTransitioningBetweenStates_ShouldExitOldAndEnterNew [< 1 ms]
  Passed Creature.Tests.Core.PhysicsTests.Vector2D_ArithmeticOperations_ShouldBehaveCorrectly [1 ms]
  Passed Creature.Tests.Core.PhysicsTests.PhysicsBody_WhenAboveFloor_ShouldApplyGravityUntilLanding [2 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_CalledMultipleTimes_ShouldReturnCachedInstance [94 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_WithBunnyJump_ShouldLoadEighteenFrames [1 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.GetClip_WhenAnimationLoaded_ShouldReturnSameClip [1 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.GetClip_WhenAnimationNotLoaded_ShouldReturnNull [< 1 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_WithBunnyLieDown_ShouldLoadFourFramesWith100msDuration [5 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_WithBunnyRun_ShouldLoadFiveFrames [1 ms]
  Passed Creature.Tests.Entities.BunnyEntityTests.BunnyEntity_UpdateAnimation_ShouldAdvanceFrames [332 ms]
  Passed Creature.Tests.Entities.BunnyEntityTests.BunnyEntity_WhenReachingLeftBoundary_ShouldTurnAround [4 ms]
  Passed Creature.Tests.Entities.BunnyEntityTests.BunnyEntity_WhenReachingBoundary_ShouldTurnAround [2 ms]
  Passed Creature.Tests.Entities.BunnyEntityTests.BunnyEntity_OnCursorHover_ShouldTransitionToJumpStateImmediately [2 ms]
  Passed Creature.Tests.Entities.BunnyEntityTests.BunnyEntity_JumpState_ShouldCompleteAndLandBackInIdle [2 ms]
  Passed Creature.Tests.Entities.BunnyEntityTests.BunnyEntity_UpdateFloor_ShouldUpdateFloorYWhenBoundsChange [2 ms]
  Passed Creature.Tests.Entities.BunnyEntityTests.BunnyEntity_HitTest_ShouldCorrectlyDetectContainment [3 ms]
  Passed Creature.Tests.Entities.BunnyEntityTests.BunnyEntity_SyncVisualTransform_ShouldUpdateWpfImageProperties [6 ms]
  Passed Creature.Tests.Entities.BunnyEntityTests.BunnyEntity_ShouldStartInIdleState [2 ms]
  Passed Creature.Tests.Entities.BunnyEntityTests.BunnyEntity_OnCursorHover_WhenAlreadyJumping_ShouldNotReenterJump [3 ms]

Total tests: 22
Passed: 22
Failed: 0
Total time: 1.162 Seconds
```

---

## 5. Concerns & Dependencies

- None. Task 4 is fully verified and ready for Task 5 (Multi-Monitor Overlay & Win32 Interop).
