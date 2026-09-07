# Review Package for Task 1

## Commits
`
d42e84d chore: setup test project and net10 WPF configuration

`

## Diff Stat
`
 .gitignore                           |  7 +++++++
 Creature.Tests/Creature.Tests.csproj | 33 +++++++++++++++++++++++++++++++++
 Creature.Tests/SanityTests.cs        | 13 +++++++++++++
 Creature.csproj                      | 27 +++++++++++++++++++++++++++
 Creature.slnx                        |  4 ++++
 5 files changed, 84 insertions(+)

`

## Full Diff
`diff
diff --git a/.gitignore b/.gitignore
new file mode 100644
index 0000000..645dbfa
--- /dev/null
+++ b/.gitignore
@@ -0,0 +1,7 @@
+[Bb]in/
+[Oo]bj/
+*.user
+*.userprefs
+.vs/
+.vscode/
+.idea/
diff --git a/Creature.Tests/Creature.Tests.csproj b/Creature.Tests/Creature.Tests.csproj
new file mode 100644
index 0000000..423d831
--- /dev/null
+++ b/Creature.Tests/Creature.Tests.csproj
@@ -0,0 +1,33 @@
+<Project Sdk="Microsoft.NET.Sdk">
+
+  <PropertyGroup>
+    <TargetFramework>net10.0-windows</TargetFramework>
+    <Nullable>enable</Nullable>
+    <ImplicitUsings>enable</ImplicitUsings>
+    <UseWPF>true</UseWPF>
+    <IsPackable>false</IsPackable>
+    <IsTestProject>true</IsTestProject>
+  </PropertyGroup>
+
+  <ItemGroup>
+    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
+    <PackageReference Include="xunit" Version="2.9.3" />
+    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2">
+      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
+      <PrivateAssets>all</PrivateAssets>
+    </PackageReference>
+    <PackageReference Include="FluentAssertions" Version="8.1.1" />
+  </ItemGroup>
+
+  <ItemGroup>
+    <ProjectReference Include="..\Creature.csproj" />
+  </ItemGroup>
+
+  <ItemGroup>
+    <Content Include="..\docs\assets\**\*.*">
+      <Link>Assets\%(RecursiveDir)%(Filename)%(Extension)</Link>
+      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
+    </Content>
+  </ItemGroup>
+
+</Project>
diff --git a/Creature.Tests/SanityTests.cs b/Creature.Tests/SanityTests.cs
new file mode 100644
index 0000000..926fa54
--- /dev/null
+++ b/Creature.Tests/SanityTests.cs
@@ -0,0 +1,13 @@
+using FluentAssertions;
+using Xunit;
+
+namespace Creature.Tests;
+
+public class SanityTests
+{
+    [Fact]
+    public void Environment_ShouldTargetNet10()
+    {
+        Environment.Version.Major.Should().BeGreaterThanOrEqualTo(10);
+    }
+}
diff --git a/Creature.csproj b/Creature.csproj
new file mode 100644
index 0000000..d857991
--- /dev/null
+++ b/Creature.csproj
@@ -0,0 +1,27 @@
+<Project Sdk="Microsoft.NET.Sdk">
+
+  <PropertyGroup>
+    <OutputType>WinExe</OutputType>
+    <TargetFramework>net10.0-windows</TargetFramework>
+    <Nullable>enable</Nullable>
+    <ImplicitUsings>enable</ImplicitUsings>
+    <UseWPF>true</UseWPF>
+    <UseWindowsForms>true</UseWindowsForms>
+    <RootNamespace>Creature</RootNamespace>
+    <AssemblyName>Creature</AssemblyName>
+    <DefaultItemExcludes>$(DefaultItemExcludes);Creature.Tests\**</DefaultItemExcludes>
+  </PropertyGroup>
+
+  <ItemGroup>
+    <Content Include="docs\assets\**\*.*">
+      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
+    </Content>
+  </ItemGroup>
+
+  <ItemGroup>
+    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
+      <_Parameter1>Creature.Tests</_Parameter1>
+    </AssemblyAttribute>
+  </ItemGroup>
+
+</Project>
diff --git a/Creature.slnx b/Creature.slnx
new file mode 100644
index 0000000..089bdb6
--- /dev/null
+++ b/Creature.slnx
@@ -0,0 +1,4 @@
+<Solution>
+  <Project Path="Creature.csproj" />
+  <Project Path="Creature.Tests/Creature.Tests.csproj" />
+</Solution>

`
