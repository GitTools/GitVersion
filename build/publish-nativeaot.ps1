#requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('net10.0', 'net11.0')][string]$Framework,
    [Parameter(Mandatory)][ValidateSet('win-x64', 'win-arm64', 'linux-x64', 'linux-arm64', 'linux-musl-x64', 'linux-musl-arm64', 'osx-arm64')][string]$RuntimeIdentifier,
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')][string]$RuntimeVersion,
    [string]$Dotnet = 'dotnet',
    [string]$Python = 'python3',
    [string]$OutputDirectory,
    [string]$ParityRepository,
    [switch]$Package
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path ([IO.Path]::GetTempPath()) ('gitversion-nativeaot-' + [guid]::NewGuid().ToString('N')) }
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path $output) { throw "Output directory must be new: $output" }
if (!$ParityRepository) { $ParityRepository = $repo }
$ParityRepository = (Resolve-Path $ParityRepository).Path
$nativeOutput = Join-Path $output 'native'
$managedOutput = Join-Path $output 'managed'
$executable = if ($RuntimeIdentifier.StartsWith('win-')) { 'gitversion.exe' } else { 'gitversion' }

function Invoke-Checked([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE" }
}

function Invoke-SourceGit([string[]]$Arguments) {
    $start = [Diagnostics.ProcessStartInfo]::new('git')
    $start.UseShellExecute = $false
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    foreach ($name in @('GIT_DIR', 'GIT_WORK_TREE', 'GIT_INDEX_FILE', 'GIT_COMMON_DIR',
        'GIT_OBJECT_DIRECTORY', 'GIT_ALTERNATE_OBJECT_DIRECTORIES', 'GIT_CONFIG', 'GIT_CONFIG_COUNT')) {
        $start.Environment.Remove($name) | Out-Null
    }
    $start.Environment['GIT_CONFIG_GLOBAL'] = if ($IsWindows) { 'NUL' } else { '/dev/null' }
    $start.Environment['GIT_CONFIG_NOSYSTEM'] = '1'
    $process = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (!$process.WaitForExit(60000)) {
            $process.Kill($true)
            throw 'Source identity Git command timed out.'
        }
        if ($process.ExitCode -ne 0) { throw "Source identity Git command failed: $($stderr.GetAwaiter().GetResult())" }
        return ($stdout.GetAwaiter().GetResult().TrimEnd() -split '\r?\n')
    }
    finally { $process.Dispose() }
}

function Get-SourceIdentity {
    $head = Invoke-SourceGit @('-C', $repo, 'rev-parse', 'HEAD')
    # Include dirty and new implementation files, plus imported build inputs. Never
    # hash ignored obj/bin output; that changes as the two flavors are built.
    $paths = Invoke-SourceGit @('-C', $repo, '-c', 'core.quotepath=false', 'ls-files', '--cached', '--others', '--exclude-standard', '--', 'src', 'global.json', '.editorconfig', '.globalconfig', 'LICENSE', 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'NuGet.Config', 'nuget.config', '.config', 'build/nativeaot-parity.py', 'build/publish-nativeaot.ps1')
    $hash = [Security.Cryptography.IncrementalHash]::CreateHash([Security.Cryptography.HashAlgorithmName]::SHA256)
    try {
        foreach ($path in ($paths | Sort-Object -Unique -CaseSensitive)) {
            $file = Join-Path $repo $path
            $hash.AppendData([Text.Encoding]::UTF8.GetBytes($path + "`0"))
            if (Test-Path $file -PathType Leaf) { $hash.AppendData([IO.File]::ReadAllBytes($file)) }
            else { $hash.AppendData([Text.Encoding]::UTF8.GetBytes('<deleted>')) }
        }
        return "$head`n$([Convert]::ToHexString($hash.GetHashAndReset()))"
    }
    finally { $hash.Dispose() }
}

function Assert-SourceIdentity([string]$Expected) {
    if ((Get-SourceIdentity) -cne $Expected) { throw 'Source inputs changed during validation. Re-run with stable inputs and a new output directory.' }
}

New-Item -ItemType Directory -Path $output | Out-Null
Push-Location $repo
try {
    $source = Get-SourceIdentity
    $project = Join-Path $repo 'src/GitVersion.App/GitVersion.App.csproj'
    $common = @('publish', $project, '-c', 'Release', '-f', $Framework, '-r', $RuntimeIdentifier,
        '--self-contained', 'true', "-p:TargetFrameworks=$Framework", "-p:RuntimeFrameworkVersion=$RuntimeVersion",
        '-p:RollForward=Disable', '-p:PackAsTool=false')
    Invoke-Checked $Dotnet ($common + @('-o', $nativeOutput,
        '-p:GitVersionManagedOnly=true', '-p:PublishAot=true', '-p:TrimmerSingleWarn=false',
        '-p:ILLinkTreatWarningsAsErrors=true', '-p:IlcTreatWarningsAsErrors=true'))
    Assert-SourceIdentity $source

    # Inspect before restoring the ordinary flavor: its legitimate LibGit2Sharp
    # dependency must never obscure the managed-only restore closure.
    $assetPath = Join-Path $repo 'src/GitVersion.App/obj/project.assets.json'
    if (!(Test-Path $assetPath -PathType Leaf)) { throw 'No NativeAOT App restore assets found; closure check cannot run.' }
    $nativeAssets = Get-Content $assetPath -Raw | ConvertFrom-Json -AsHashtable
    # Central package version declarations also appear in assets even when unused.
    # Inspect resolved libraries/targets and project references, not that catalog.
    foreach ($section in @($nativeAssets.libraries, $nativeAssets.targets, $nativeAssets.project.restore.frameworks)) {
        if (($section | ConvertTo-Json -Depth 100 -Compress) -match '(?i)libgit2') {
            throw "LibGit2Sharp/libgit2 remains in the NativeAOT restore closure: $assetPath"
        }
    }
    if (!$nativeAssets.libraries.ContainsKey("Microsoft.DotNet.ILCompiler/$RuntimeVersion") -or
        !$nativeAssets.libraries.ContainsKey("runtime.$RuntimeIdentifier.Microsoft.DotNet.ILCompiler/$RuntimeVersion")) {
        throw 'Native compiler restore version does not match the requested runtime version.'
    }
    Copy-Item $assetPath (Join-Path $output 'native-project.assets.json')
    if (Get-ChildItem $nativeOutput -Recurse -File | Where-Object Name -Match '(?i)libgit2') {
        throw 'LibGit2Sharp/libgit2 remains in NativeAOT publication output.'
    }
    $native = Join-Path $nativeOutput $executable
    if (!(Test-Path $native -PathType Leaf)) { throw "Native executable missing: $native" }
    if (Test-Path (Join-Path $nativeOutput 'gitversion.dll')) { throw 'Native output unexpectedly contains a managed CLI assembly.' }

    Invoke-Checked $Dotnet ($common + @('-o', $managedOutput,
        '-p:GitVersionManagedOnly=false', '-p:PublishAot=false', '-p:PublishTrimmed=false', '-p:PublishSingleFile=false'))
    Assert-SourceIdentity $source
    $managed = Join-Path $managedOutput $executable
    if (!(Test-Path $managed -PathType Leaf)) { throw "Managed comparator missing: $managed" }

    $parityPath = Join-Path $output 'parity.json'
    & $Python (Join-Path $PSScriptRoot 'nativeaot-parity.py') --managed $managed --native $native --dotnet $Dotnet --framework $Framework --rid $RuntimeIdentifier --runtime-version $RuntimeVersion --repository $ParityRepository > $parityPath
    if ($LASTEXITCODE -ne 0) { throw "Output parity failed. See $parityPath" }
    $parity = Get-Content $parityPath -Raw | ConvertFrom-Json
    $expectedParityCases = 99
    if ($parity.failed -ne 0 -or $parity.passed -ne $expectedParityCases -or $parity.cases.Count -ne $expectedParityCases -or
        @($parity.cases | Where-Object { !$_.passed }).Count -ne 0 -or
        @($parity.cases.case | Sort-Object -Unique).Count -ne $expectedParityCases -or
        !($parity.cases | Where-Object { $_.case -eq 'existing-history-json' -and $_.passed })) {
        throw 'Parity report is incomplete or unsuccessful.'
    }
    if ($parity.framework -cne $Framework -or $parity.rid -cne $RuntimeIdentifier -or $parity.runtime_version -cne $RuntimeVersion -or
        $parity.source_sha -cne ($source -split "`n")[0] -or
        $parity.native_sha256 -ine (Get-FileHash $native -Algorithm SHA256).Hash -or
        $parity.managed_sha256 -ine (Get-FileHash $managed -Algorithm SHA256).Hash) {
        throw 'Parity report does not match the selected inputs and published executables.'
    }
    Assert-SourceIdentity $source
    $sdk = & $Dotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'Cannot record the selected SDK.' }
    [ordered]@{ source = $source; sdk = $sdk; framework = $Framework; rid = $RuntimeIdentifier; runtime_version = $RuntimeVersion;
        parity_repository = $ParityRepository; native_bytes = (Get-Item $native).Length; managed_executable_bytes = (Get-Item $managed).Length;
        note = 'Managed executable size excludes companion files. See publication directories for complete payloads.' } |
        ConvertTo-Json | Set-Content (Join-Path $output 'inputs.json') -Encoding utf8

    if ($Package) {
        $staging = Join-Path $output 'package'
        New-Item -ItemType Directory -Path $staging | Out-Null
        Copy-Item $native (Join-Path $staging $executable)
        Copy-Item (Join-Path $repo 'LICENSE') (Join-Path $staging 'LICENSE')
        $checksums = foreach ($name in @($executable, 'LICENSE')) {
            "$((Get-FileHash (Join-Path $staging $name) -Algorithm SHA256).Hash.ToLowerInvariant())  $name"
        }
        $checksums | Set-Content (Join-Path $staging 'SHA256SUMS') -Encoding utf8
        $revision = ($source -split "`n")[0].Substring(0, 12)
        $archive = Join-Path $output "gitversion-nativeaot-preview-$Framework-$RuntimeIdentifier-$revision.zip"
        Assert-SourceIdentity $source
        [IO.Compression.ZipFile]::CreateFromDirectory($staging, $archive)
        if (!$RuntimeIdentifier.StartsWith('win-')) {
            $zip = [IO.Compression.ZipFile]::OpenRead($archive)
            try {
                $entry = $zip.GetEntry($executable)
                if (!$entry -or !(($entry.ExternalAttributes -shr 16) -band 64)) {
                    throw 'Preview archive does not preserve the Unix executable permission.'
                }
            }
            catch {
                $zip.Dispose()
                Remove-Item $archive
                throw
            }
            finally { $zip.Dispose() }
        }
        "$((Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($archive))" |
            Set-Content ($archive + '.sha256') -Encoding utf8
        Write-Output "Validated preview archive: $archive"
    }
    Write-Output "Validation evidence: $output"
}
finally { Pop-Location }
