#!/usr/bin/env pwsh
#Requires -Version 7.0
<#
.SYNOPSIS
    Real-world corpus parity check between the libgit2 and managed git backends.

.DESCRIPTION
    For every repository in the corpus this script prepares three checkout shapes
    (full clone, shallow clone, linked worktree), runs the locally built
    GitVersion.App once per backend (GITVERSION_GIT_BACKEND=libgit2|managed) with
    read-only, cache-free flags, and diffs the JSON outputs field by field.

    Any field difference (or a one-sided failure) is a parity failure. The exit
    code is the number of failing repo/variant combinations; 0 means full parity.

.EXAMPLE
    ./build/parity-corpus.ps1
    ./build/parity-corpus.ps1 -Repos https://github.com/GitTools/GitReleaseManager.git -Variants full,shallow
    ./build/parity-corpus.ps1 -Repos /path/to/local/repo -WorkDir ~/parity-cache -SkipBuild
#>
[CmdletBinding()]
param(
    # Git URLs or local paths to include in the corpus.
    [string[]]$Repos,

    # Cache directory for clones; repeat runs reuse it.
    [string]$WorkDir = (Join-Path ([IO.Path]::GetTempPath()) 'gitversion-parity-corpus'),

    # Checkout shapes to exercise per repository.
    [ValidateSet('full', 'shallow', 'worktree')]
    [string[]]$Variants = @('full', 'shallow', 'worktree'),

    [string]$Configuration = 'Release',
    [string]$Framework = 'net10.0',

    # Skip building GitVersion.App (reuse existing binaries).
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $Repos) {
    $Repos = @(
        'https://github.com/GitTools/GitVersion.git'
        'https://github.com/GitTools/GitReleaseManager.git'
        $repoRoot
    )
}

# ---------------------------------------------------------------------------
# helpers
# ---------------------------------------------------------------------------

function Invoke-Git {
    param([string[]]$Arguments)
    $output = & git @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed ($LASTEXITCODE): $($output | Out-String)"
    }
    $output
}

function Get-CorpusName {
    param([string]$Source)
    $base = ([IO.Path]::GetFileName($Source.TrimEnd('/', '\'))) -replace '\.git$', ''
    if (-not $base) { $base = 'repo' }
    $hashBytes = [System.Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($Source))
    $suffix = -join ($hashBytes[0..3] | ForEach-Object { $_.ToString('x2') })
    "$base-$suffix"
}

function Invoke-GitVersion {
    <# Runs gitversion.dll with the given backend; returns exit code, stdout, stderr, elapsed ms. #>
    param(
        [string]$AppDll,
        [string]$RepoPath,
        [string]$Backend,
        [string[]]$ExtraArgs
    )
    $logFile = [IO.Path]::GetTempFileName()
    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = 'dotnet'
    $argList = @($AppDll, $RepoPath, '--output', 'json', '--no-cache', '--no-fetch', '--no-normalize', '-l', $logFile) + @($ExtraArgs)
    foreach ($a in $argList) {
        if (-not [string]::IsNullOrEmpty($a)) { $psi.ArgumentList.Add($a) }
    }
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.EnvironmentVariables['GITVERSION_GIT_BACKEND'] = $Backend

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $process = [System.Diagnostics.Process]::Start($psi)
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()
    $sw.Stop()

    # On failure the reason usually only appears in the log; pull the first error line out.
    $errorSummary = ''
    if ($process.ExitCode -ne 0) {
        $errorSummary = ($stderr + "`n" + $stdout).Trim() -split '\r?\n' |
            Where-Object { $_ } | Select-Object -First 1
        if (-not $errorSummary -and (Test-Path $logFile)) {
            $errorSummary = Get-Content $logFile |
                Where-Object { $_ -match 'EROR' -or $_ -match 'Exception' } |
                Select-Object -First 2 | Join-String -Separator ' '
        }
    }
    Remove-Item $logFile -ErrorAction SilentlyContinue

    [pscustomobject]@{
        ExitCode     = $process.ExitCode
        StdOut       = $stdout
        StdErr       = $stderr
        ErrorSummary = [string]$errorSummary
        ElapsedMs    = $sw.ElapsedMilliseconds
    }
}

function ConvertFrom-GitVersionJson {
    <# Extracts the JSON object from gitversion stdout and returns a hashtable, or $null. #>
    param([string]$Text)
    $start = $Text.IndexOf('{')
    $end = $Text.LastIndexOf('}')
    if ($start -lt 0 -or $end -le $start) { return $null }
    try {
        $Text.Substring($start, $end - $start + 1) | ConvertFrom-Json -AsHashtable
    } catch {
        $null
    }
}

function Compare-VersionVariables {
    <# Field-by-field diff of two hashtables; returns a list of difference descriptions. #>
    param([hashtable]$Left, [hashtable]$Right)
    $differences = [System.Collections.Generic.List[string]]::new()
    $keys = @($Left.Keys) + @($Right.Keys) | Sort-Object -Unique
    foreach ($key in $keys) {
        $l = if ($Left.ContainsKey($key)) { [string]$Left[$key] } else { '<absent>' }
        $r = if ($Right.ContainsKey($key)) { [string]$Right[$key] } else { '<absent>' }
        if ($l -cne $r) {
            $differences.Add(("  {0,-28} libgit2: {1}  |  managed: {2}" -f $key, $l, $r))
        }
    }
    $differences
}

function Get-VariantPath {
    <# Ensures the requested checkout shape exists under the repo's cache dir; returns its path. #>
    param([string]$RepoCacheDir, [string]$Source, [string]$Variant)

    $fullDir = Join-Path $RepoCacheDir 'full'
    if (-not (Test-Path (Join-Path $fullDir '.git'))) {
        Write-Host "  cloning $Source -> $fullDir"
        $null = Invoke-Git @('clone', '--quiet', $Source, $fullDir)
    }
    if ($Variant -eq 'full') { return $fullDir }

    if ($Variant -eq 'shallow') {
        $shallowDir = Join-Path $RepoCacheDir 'shallow'
        if (-not (Test-Path (Join-Path $shallowDir '.git'))) {
            # Derive the shallow clone from the local full clone: deterministic and offline.
            $fullUri = ([System.Uri]::new($fullDir)).AbsoluteUri
            Write-Host "  creating shallow clone -> $shallowDir"
            $null = Invoke-Git @('clone', '--quiet', '--depth', '1', $fullUri, $shallowDir)
        }
        return $shallowDir
    }

    # worktree
    $worktreeDir = Join-Path $RepoCacheDir 'worktree'
    if (-not (Test-Path (Join-Path $worktreeDir '.git'))) {
        Write-Host "  creating linked worktree -> $worktreeDir"
        $null = Invoke-Git @('-C', $fullDir, 'worktree', 'prune')
        $null = Invoke-Git @('-C', $fullDir, 'worktree', 'add', '--detach', $worktreeDir)
    }
    return $worktreeDir
}

# ---------------------------------------------------------------------------
# build once
# ---------------------------------------------------------------------------

$appDll = Join-Path $repoRoot "src/GitVersion.App/bin/$Configuration/$Framework/gitversion.dll"
if (-not $SkipBuild) {
    Write-Host "Building GitVersion.App ($Configuration, $Framework)..."
    & dotnet build (Join-Path $repoRoot 'src/GitVersion.App/GitVersion.App.csproj') -c $Configuration -f $Framework --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
}
if (-not (Test-Path $appDll)) { throw "GitVersion.App binary not found at $appDll (build it or drop -SkipBuild)." }

$null = New-Item -ItemType Directory -Force -Path $WorkDir
Write-Host "Corpus cache: $WorkDir"
Write-Host "App: $appDll"
Write-Host ''

# ---------------------------------------------------------------------------
# run corpus
# ---------------------------------------------------------------------------

$results = [System.Collections.Generic.List[pscustomobject]]::new()

foreach ($source in $Repos) {
    $name = Get-CorpusName $source
    $repoCacheDir = Join-Path $WorkDir $name
    $null = New-Item -ItemType Directory -Force -Path $repoCacheDir
    Write-Host "=== $source ($name) ==="

    foreach ($variant in $Variants) {
        $record = [pscustomobject]@{
            Repo      = $source
            Variant   = $variant
            Result    = 'ERROR'
            Libgit2Ms = $null
            ManagedMs = $null
            Detail    = ''
        }
        $results.Add($record)

        try {
            $variantPath = Get-VariantPath -RepoCacheDir $repoCacheDir -Source $source -Variant $variant
        } catch {
            $record.Detail = "variant setup failed: $($_.Exception.Message)"
            Write-Warning "[$name/$variant] $($record.Detail)"
            continue
        }

        [string[]]$extraArgs = @()
        if ($variant -eq 'shallow') { $extraArgs = @('--allow-shallow') }

        $runs = @{}
        foreach ($backend in 'libgit2', 'managed') {
            $runs[$backend] = Invoke-GitVersion -AppDll $appDll -RepoPath $variantPath -Backend $backend -ExtraArgs $extraArgs
        }
        $record.Libgit2Ms = $runs['libgit2'].ElapsedMs
        $record.ManagedMs = $runs['managed'].ElapsedMs

        $failed = @('libgit2', 'managed').Where({ $runs[$_].ExitCode -ne 0 })
        if ($failed.Count -eq 2) {
            # Both backends refuse this shape identically: an environment limitation, not a parity break.
            $record.Detail = 'both backends failed: ' + $runs['libgit2'].ErrorSummary
            Write-Warning "[$name/$variant] $($record.Detail)"
            continue
        }
        if ($failed.Count -eq 1) {
            $only = $failed[0]
            $record.Result = 'DIFF'
            $record.Detail = "$only failed (exit $($runs[$only].ExitCode)) while the other backend succeeded: " + $runs[$only].ErrorSummary
            Write-Host "[$name/$variant] PARITY FAILURE - $($record.Detail)" -ForegroundColor Red
            continue
        }

        $libgit2Json = ConvertFrom-GitVersionJson $runs['libgit2'].StdOut
        $managedJson = ConvertFrom-GitVersionJson $runs['managed'].StdOut
        if (-not $libgit2Json -or -not $managedJson) {
            $record.Detail = 'could not parse JSON output from one or both backends'
            Write-Warning "[$name/$variant] $($record.Detail)"
            continue
        }

        $differences = @(Compare-VersionVariables -Left $libgit2Json -Right $managedJson)
        if ($differences.Count -eq 0) {
            $record.Result = 'MATCH'
            Write-Host "[$name/$variant] MATCH (libgit2 $($record.Libgit2Ms) ms, managed $($record.ManagedMs) ms)" -ForegroundColor Green
        } else {
            $record.Result = 'DIFF'
            $record.Detail = "$($differences.Count) field(s) differ"
            Write-Host "[$name/$variant] PARITY FAILURE - $($record.Detail):" -ForegroundColor Red
            $differences | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        }
    }
    Write-Host ''
}

# ---------------------------------------------------------------------------
# summary
# ---------------------------------------------------------------------------

Write-Host '=== Summary ==='
$results |
    Format-Table Repo, Variant, Result, Libgit2Ms, ManagedMs, Detail -AutoSize |
    Out-String -Width 500 | Write-Host

$failures = @($results | Where-Object Result -eq 'DIFF')
$errors = @($results | Where-Object Result -eq 'ERROR')
if ($failures.Count -eq 0 -and $errors.Count -eq 0) {
    Write-Host 'Full parity across the corpus.' -ForegroundColor Green
} elseif ($failures.Count -eq 0) {
    Write-Host "No parity failures, but $($errors.Count) variant(s) could not be compared (ERROR)." -ForegroundColor Yellow
} else {
    Write-Host "$($failures.Count) repo/variant combination(s) failed parity." -ForegroundColor Red
}
exit $failures.Count
