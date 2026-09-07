# Task 1: Test Project Setup & Project Configuration

**Files:**
- Modify: `Creature.csproj`
- Create: `Creature.Tests/Creature.Tests.csproj`
- Create: `Creature.Tests/SanityTests.cs`

**Interfaces:**
- Produces: `Creature.Tests` test project targeting `net10.0-windows` with xUnit, Microsoft.NET.Test.Sdk, and FluentAssertions referenced.

## Steps to Execute:

- [ ] **Step 1: Update `Creature.csproj` to enable InternalsVisibleTo test project and copy assets**

Update `Creature.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>Creature</RootNamespace>
    <AssemblyName>Creature</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="docs\assets\**\*.*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Creature.Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create `Creature.Tests/Creature.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="8.1.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Creature.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="..\docs\assets\**\*.*">
      <Link>Assets\%(RecursiveDir)%(Filename)%(Extension)</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write sanity test `Creature.Tests/SanityTests.cs`**

```csharp
using FluentAssertions;
using Xunit;

namespace Creature.Tests;

public class SanityTests
{
    [Fact]
    public void Environment_ShouldTargetNet10()
    {
        Environment.Version.Major.Should().BeGreaterOrEqualTo(10);
    }
}
```

- [ ] **Step 4: Run test to verify test project builds and passes**

Run: `dotnet test`
Expected: 1 test passed.

- [ ] **Step 5: Commit**

```bash
git add Creature.csproj Creature.Tests/
git commit -m "chore: setup test project and net10 WPF configuration"
```
