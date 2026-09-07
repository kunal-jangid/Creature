# Task 1 Report: Test Project Setup & Project Configuration

- **Date:** 2026-09-07
- **Task:** Task 1 of Desktop Pets MVP Implementation Plan
- **Commit:** `d42e84d43c60fb858b1deb70a2eebdbbb4f9fc2e` (`chore: setup test project and net10 WPF configuration`)
- **Status:** Completed Successfully

---

## 1. Summary of Changes

1. **Root Project Configuration (`Creature.csproj`)**
   - Configured target framework: `net10.0-windows`
   - Enabled `UseWPF` and `UseWindowsForms`
   - Configured `RootNamespace` and `AssemblyName` to `Creature`
   - Added content copy rule for `docs\assets\**\*.*` (`PreserveNewest`)
   - Added `InternalsVisibleTo` assembly attribute pointing to `Creature.Tests`
   - Added `<DefaultItemExcludes>$(DefaultItemExcludes);Creature.Tests\**</DefaultItemExcludes>` to prevent MSBuild in the root project from compiling nested test files without xUnit/FluentAssertions references.

2. **Test Project Creation (`Creature.Tests/Creature.Tests.csproj`)**
   - Configured `net10.0-windows`, `UseWPF`, `IsPackable=false`, `IsTestProject=true`
   - Added NuGet package references:
     - `Microsoft.NET.Test.Sdk` (v17.13.0)
     - `xunit` (v2.9.3)
     - `xunit.runner.visualstudio` (v3.0.2)
     - `FluentAssertions` (v8.1.1)
   - Added `ProjectReference` to `..\Creature.csproj`
   - Added linked asset copying to `Assets\` directory in test output.

3. **Sanity Test (`Creature.Tests/SanityTests.cs`)**
   - Implemented `Environment_ShouldTargetNet10` asserting `Environment.Version.Major >= 10`.
   - Used `BeGreaterThanOrEqualTo` (corrected from `BeGreaterOrEqualTo` typo in brief).

4. **Solution Setup & Ignore Files**
   - Created `Creature.slnx` referencing both projects to enable root-level `dotnet test` execution.
   - Added `.gitignore` to prevent committing build artifacts (`bin/`, `obj/`, user files).

5. **Codebase Adjustments**
   - Disambiguated `App : System.Windows.Application` in `App.xaml.cs` to resolve compiler error CS0104 caused by `UseWindowsForms` importing `System.Windows.Forms.Application`.

---

## 2. Test Execution & Verification

Ran `dotnet test` at repository root:

```
  Determining projects to restore...
  Restored D:\projects\Creature\Creature.csproj (in 82 ms).
  Restored D:\projects\Creature\Creature.Tests\Creature.Tests.csproj (in 266 ms).
  Creature -> D:\projects\Creature\bin\Debug\net10.0-windows\Creature.dll
  Creature.Tests -> D:\projects\Creature\Creature.Tests\bin\Debug\net10.0-windows\Creature.Tests.dll
Test run for D:\projects\Creature\Creature.Tests\bin\Debug\net10.0-windows\Creature.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 34 ms - Creature.Tests.dll (net10.0)
```

Build and test results:
- **Compiled Projects:** `Creature`, `Creature.Tests`
- **Tests Executed:** 1
- **Passed:** 1
- **Failed:** 0
- **Skipped:** 0

---

## 3. Notes & Context for Subsequent Tasks

- **`UseWindowsForms` Name Collision:** Because both `UseWPF` and `UseWindowsForms` are enabled, global usings include both `System.Windows` and `System.Windows.Forms`. Whenever referencing `Application`, qualify as `System.Windows.Application` or `System.Windows.Forms.Application`.
- **Root `dotnet test` Discovery:** `Creature.slnx` is tracked in the repository so subsequent subagents can run `dotnet test` or `dotnet test --filter ...` directly from the repo root.
