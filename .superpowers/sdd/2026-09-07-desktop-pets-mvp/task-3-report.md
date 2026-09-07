# Task 3 Report: Core Physics & State Machine Abstractions

**Date:** 2026-09-07  
**Status:** Completed  
**Commit:** `e04d0aa069634875610ecc97169ae5fa27aedc2a` (`feat: implement 2D physics and generic state machine`)

---

## 1. Summary of Work

Implemented core mathematical, physical, and state machine abstractions required by Desktop Pets MVP:
- **`Core/Physics/Vector2D.cs`**: Immutable record struct representing 2D vectors with arithmetic operators (`+`, `-`, `*` scalar) and `Vector2D.Zero`.
- **`Core/Physics/Transform2D.cs`**: Encapsulates entity positioning, scaling, orientation (`IsFacingLeft`), dimensions (`BaseWidth`, `BaseHeight`, `ScaledWidth`, `ScaledHeight`), and calculated `BoundingBox` (`System.Windows.Rect`).
- **`Core/Physics/PhysicsBody.cs`**: Kinematic physics simulation with velocity integration, gravity acceleration, floor clamping, and `IsOnGround` status tracking.
- **`Core/StateMachine/IState.cs`**: Generic state contract with `Enter(TContext)`, `Update(TContext, double dt)`, and `Exit(TContext)` lifecycle hooks.
- **`Core/StateMachine/StateMachine.cs`**: Generic state machine managing state transitions and invoking lifecycle callbacks.
- **`Creature.Tests/Core/PhysicsTests.cs`**: Unit tests verifying gravity integration, ground collision / landing, vector arithmetic, and transform scale/bounding box calculations.
- **`Creature.Tests/Core/StateMachineTests.cs`**: Unit tests validating entry, update, and exit lifecycle callbacks across transitions.

---

## 2. TDD Workflow Verification

1. **RED Phase**:
   - Authored unit test suites in `Creature.Tests/Core/PhysicsTests.cs` and `Creature.Tests/Core/StateMachineTests.cs`.
   - Executed `dotnet test --filter "FullyQualifiedName~Core"`.
   - Confirmed expected build failure: `CS0234: The type or namespace name 'Core' does not exist in the namespace 'Creature'`.
2. **GREEN Phase**:
   - Implemented `Vector2D`, `Transform2D`, `PhysicsBody`, `IState<TContext>`, and `StateMachine<TContext>`.
   - Executed `dotnet test`.
   - Verified that all 12 tests passed successfully (0 failed, 0 warnings, 0 errors).
3. **Commit**:
   - Committed changes with message `feat: implement 2D physics and generic state machine` (`e04d0aa`).

---

## 3. Findings & Noteworthy Observations

- **Zero Allocations for Vectors**: `Vector2D` is implemented as a `readonly record struct`, minimizing heap allocation overhead during 60 FPS physics tick updates.
- **WPF Interoperability**: `Transform2D.BoundingBox` returns a `System.Windows.Rect` for direct integration with WPF layout and rendering boundaries.
- **Ground Snapping**: When `PhysicsBody.Update(dt)` detects that `newY >= FloorY`, it clamps position strictly to `FloorY`, zeroes vertical velocity, and sets `IsOnGround = true`.

---

## 4. Test Results

```
Test run for D:\projects\Creature\Creature.Tests\bin\Debug\net10.0-windows\Creature.Tests.dll (.NETCoreApp,Version=v10.0)
  Passed Creature.Tests.SanityTests.Environment_ShouldTargetNet10 [59 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_CalledMultipleTimes_ShouldReturnCachedInstance [146 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_WithBunnyJump_ShouldLoadEighteenFrames [2 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.GetClip_WhenAnimationLoaded_ShouldReturnSameClip [1 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.GetClip_WhenAnimationNotLoaded_ShouldReturnNull [< 1 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_WithBunnyLieDown_ShouldLoadFourFramesWith100msDuration [4 ms]
  Passed Creature.Tests.Graphics.SpriteManagerTests.LoadAnimation_WithBunnyRun_ShouldLoadFiveFrames [1 ms]
  Passed Creature.Tests.Core.PhysicsTests.PhysicsBody_WhenAboveFloor_ShouldApplyGravityUntilLanding [2 ms]
  Passed Creature.Tests.Core.PhysicsTests.Vector2D_ArithmeticOperations_ShouldBehaveCorrectly [< 1 ms]
  Passed Creature.Tests.Core.PhysicsTests.Transform2D_ScaledDimensionsAndBoundingBox_ShouldReflectScaleAndPosition [< 1 ms]
  Passed Creature.Tests.Core.StateMachineTests.StateMachine_ShouldExecuteEnterUpdateAndExitCallbacks [2 ms]
  Passed Creature.Tests.Core.StateMachineTests.StateMachine_WhenTransitioningBetweenStates_ShouldExitOldAndEnterNew [< 1 ms]

Total tests: 12
Passed: 12
Failed: 0
Total time: 0.985 Seconds
```

---

## 5. Concerns & Dependencies

- None. Implementation matches specification precisely and integrates cleanly with the existing codebase.
