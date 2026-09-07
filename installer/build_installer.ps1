param (
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

Write-Host "==> Publishing Desktop Pets Standalone Binary..." -ForegroundColor Cyan
dotnet publish "$PSScriptRoot\..\Creature.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=$Version -o "$PSScriptRoot\..\publish"

# Search for Inno Setup compiler
$isccPaths = @(
    "ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)

$iscc = $null
foreach ($path in $isccPaths) {
    if (Get-Command $path -ErrorAction SilentlyContinue) {
        $iscc = $path
        break
    }
    if (Test-Path $path) {
        $iscc = $path
        break
    }
}

if (-not $iscc) {
    Write-Warning "Inno Setup Compiler (ISCC.exe) not found on local PATH."
    Write-Host "To install Inno Setup on Windows, run:" -ForegroundColor Yellow
    Write-Host "  winget install JRSoftware.InnoSetup -e" -ForegroundColor Green
    Write-Host "Or build through GitHub Actions which has Inno Setup pre-configured." -ForegroundColor Cyan
    exit 1
}

Write-Host "==> Compiling Windows Setup Installer with Inno Setup..." -ForegroundColor Cyan
& $iscc "/dAppVersion=$Version" "$PSScriptRoot\creature_setup.iss"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] Windows Installer created successfully at: dist\DesktopPets-Setup-v$Version.exe" -ForegroundColor Green
} else {
    Write-Error "Inno Setup compilation failed with exit code $LASTEXITCODE"
}