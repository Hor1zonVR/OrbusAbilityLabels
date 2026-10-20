# OrbusAbilityLabels - build and publish a DLL to GitHub Releases for RebornManager.
# Uses matching local BepInEx/interop references; NEVER installs into the game.
# Usage: .\release.ps1 [-Preview] [-GameDir 'C:\path\to\your\instance']
param(
    [string]$GameDir = '',
    [switch]$Preview
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$repo = 'Hor1zonVR/OrbusAbilityLabels'
$root = $PSScriptRoot
$project = Join-Path $root 'src\OrbusAbilityLabels\OrbusAbilityLabels.csproj'
$plugin = Join-Path $root 'src\OrbusAbilityLabels\Plugin.cs'
$dll = Join-Path $root 'src\OrbusAbilityLabels\bin\Release\net6.0\OrbusAbilityLabels.dll'
$localCache = Join-Path $root '.build-refs-path.txt'
$instanceRoot = Join-Path $env:LOCALAPPDATA 'OrbusRebornModdingInstances'
$sharedCache = Join-Path $instanceRoot '.last-build-references.txt'
$required = @(
    'BepInEx\core\BepInEx.Core.dll',
    'BepInEx\core\BepInEx.Unity.IL2CPP.dll',
    'BepInEx\core\Il2CppInterop.Runtime.dll',
    'BepInEx\interop\Il2Cppmscorlib.dll',
    'BepInEx\interop\UnityEngine.CoreModule.dll'
)

function Assert-Success([string]$name) {
    if ($LASTEXITCODE -ne 0) { throw "$name failed (exit code $LASTEXITCODE)." }
}
function Normalize-Path([string]$raw) {
    return [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($raw.Trim().Trim('"')))
}
function Has-References([string]$candidate) {
    if ([string]::IsNullOrWhiteSpace($candidate)) { return $false }
    try {
        $folder = Normalize-Path $candidate
        foreach ($file in $required) {
            if (-not (Test-Path -LiteralPath (Join-Path $folder $file) -PathType Leaf)) { return $false }
        }
        return $true
    } catch { return $false }
}

try {
    Set-Location $root
    foreach ($cmd in @('dotnet','git','gh')) {
        if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) { throw "Missing '$cmd' command." }
    }
    & gh auth status
    Assert-Success 'GitHub authentication (run gh auth login)'

    $origin = (& git remote get-url origin).Trim()
    Assert-Success 'Git origin'
    if ($origin -notmatch '[/\\:]Hor1zonVR/OrbusAbilityLabels(\.git)?$') {
        throw 'Wrong origin: expected Hor1zonVR/OrbusAbilityLabels.'
    }
    if ((& git branch --show-current).Trim() -ne 'main') {
        throw 'Switch to main before release.'
    }
    if (@(& git status --porcelain).Count -ne 0) {
        throw 'Repository has local changes. Commit or stash them before publishing.'
    }

    & git pull --ff-only origin main
    Assert-Success 'Pulling latest GitHub source'

    if ([string]::IsNullOrWhiteSpace($GameDir)) {
        foreach ($cache in @($localCache, $sharedCache)) {
            if (Test-Path -LiteralPath $cache -PathType Leaf) {
                $saved = (Get-Content -LiteralPath $cache -Raw).Trim()
                if (Has-References $saved) {
                    $GameDir = Normalize-Path $saved
                    Write-Host 'Reusing saved build references - no selection needed.' -ForegroundColor Green
                    break
                }
            }
        }
    }
    if ([string]::IsNullOrWhiteSpace($GameDir)) {
        $candidates = @()
        if (Test-Path -LiteralPath $instanceRoot -PathType Container) {
            $candidates = @(Get-ChildItem -LiteralPath $instanceRoot -Directory |
                Where-Object { Has-References $_.FullName } |
                Sort-Object Name)
        }
        if ($candidates.Count -eq 0) {
            throw "No usable BepInEx instance in '$instanceRoot'. Retry once with -GameDir 'C:\path\to\your\instance'."
        }
        $GameDir = $candidates[0].FullName
        Write-Host "Auto-selected build references: $GameDir" -ForegroundColor Cyan
    }
    if (-not (Has-References $GameDir)) {
        throw "Build references are missing in '$GameDir'."
    }
    $GameDir = Normalize-Path $GameDir
    [IO.File]::WriteAllText($localCache, $GameDir + [Environment]::NewLine)
    if (-not (Test-Path -LiteralPath $instanceRoot)) {
        New-Item -Path $instanceRoot -ItemType Directory -Force | Out-Null
    }
    [IO.File]::WriteAllText($sharedCache, $GameDir + [Environment]::NewLine)

    $source = Get-Content -LiteralPath $plugin -Raw
    $match = [regex]::Match($source, '(?s)\[BepInPlugin\(\s*"[^"]+"\s*,\s*"[^"]+"\s*,\s*"(?<v>\d+\.\d+\.\d+)"')
    if (-not $match.Success) { throw 'Cannot read BepInPlugin version in Plugin.cs.' }
    $tag = 'v' + $match.Groups['v'].Value
    # A first release does not exist yet. In Windows PowerShell 5.1, a
    # 'gh release view' 404 on stderr becomes a terminating error with
    # $ErrorActionPreference='Stop', even when stderr is redirected.
    # git ls-remote succeeds with *empty output* for an unused tag.
    $matchingTags = @(& git ls-remote --tags --refs origin "refs/tags/$tag")
    Assert-Success 'Checking remote version tags'
    if ($matchingTags.Count -gt 0) {
        throw "$tag already exists on GitHub. Increment Plugin.cs version before publishing again."
    }

    Write-Host "[1/2] Building OrbusAbilityLabels $tag against local interop..." -ForegroundColor Yellow
    & dotnet build $project -c Release --no-incremental "-p:GameDir=$GameDir"
    Assert-Success 'Mod build'
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
        throw "Built DLL not found: $dll"
    }

    Write-Host '[2/2] Publishing exact compiled DLL to GitHub Releases...' -ForegroundColor Yellow
    $commit = (& git rev-parse HEAD).Trim()
    Assert-Success 'Reading commit'
    $notes = "Read-only v0.1.0 Bard/Musketeer ability discovery diagnostic. Does not add labels yet. Press Ctrl+Shift+L while each class is equipped; inspect BepInEx/AbilityLabelsReport.txt. No gameplay changes. See README."
    $args = @('release','create',$tag,$dll,'--repo',$repo,'--target',$commit,
              '--title',"OrbusAbilityLabels $tag (diagnostic)",'--notes',$notes)
    if ($Preview) { $args += '--prerelease' }
    & gh @args
    Assert-Success 'GitHub release'
    Write-Host 'Published. This script did not install anything into Orbus.' -ForegroundColor Green
    if ($Preview) {
        Write-Host 'Preview release is not offered through RebornManager.' -ForegroundColor Yellow
    } else {
        Write-Host 'Stable release can be installed through RebornManager when it is catalogued.' -ForegroundColor Green
    }
} catch {
    Write-Host "RELEASE STOPPED: $($_.Exception.Message)" -ForegroundColor Red
    throw
}
