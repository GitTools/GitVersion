param(
    [Parameter(Mandatory)][ValidateSet('Pack', 'Stage', 'Install')][string]$Mode,
    [Parameter(Mandatory)][string]$Workspace,
    [Parameter(Mandatory)][string]$Bundle,
    [string]$Staging,
    [string]$Coverage,
    [string]$Revision
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$Workspace = [IO.Path]::GetFullPath($Workspace).TrimEnd('/')
$ns = 'http://www.sonarsource.com/msbuild/integration/2015/1'
function Require($Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}
function Resolve-Child([string]$Root, [string]$Relative) {
    Require ($Relative -match '^[a-zA-Z0-9_./ +()-]+$' -and -not ($Relative.Split('/') -contains '..')) 'Invalid relative path'
    $rootPath = [IO.Path]::GetFullPath($Root).TrimEnd('/') + '/'
    $path = [IO.Path]::GetFullPath([IO.Path]::Combine($rootPath, $Relative))
    Require ($path.StartsWith($rootPath, [StringComparison]::Ordinal)) 'Path escapes root'
    for ($check = $path; $check.Length -ge $rootPath.Length; $check = [IO.Path]::GetDirectoryName($check)) {
        if (Test-Path -LiteralPath $check) {
            Require (-not ((Get-Item -Force -LiteralPath $check).Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Linked path'
        }
    }
    return $path
}
function Read-Xml([string]$Path) {
    Require ((Get-Item -LiteralPath $Path).Length -le 134217728) 'Oversized XML'
    $settings = [Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $reader = [Xml.XmlReader]::Create($Path, $settings)
    try {
        $document = [Xml.XmlDocument]::new()
        $document.XmlResolver = $null
        $document.Load($reader)
        return ,$document
    } finally { $reader.Dispose() }
}
function Copy-Data([string]$Source, [string]$Target) {
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Target)) | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Target
}
function Get-XmlFingerprint([Xml.XmlNode]$Node) {
    # Rules/settings are unordered collections. Preserve all names, attributes and values.
    $attributes = @($Node.Attributes | ForEach-Object { $_.Name + '=' + $_.Value } | Sort-Object)
    $children = @($Node.ChildNodes | Where-Object { $_ -is [Xml.XmlElement] } | ForEach-Object { Get-XmlFingerprint $_ } | Sort-Object)
    $value = if ($children.Count) { '' } else { $Node.InnerText }
    $canonical = @($Node.Name, $attributes, $value, $children) | ConvertTo-Json -Depth 10 -Compress
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($canonical)))
}
function Get-Fingerprint {
    $conf = "$Workspace/.sonarqube/conf"
    $result = [ordered]@{}
    Get-ChildItem -LiteralPath $conf -Recurse -File | Where-Object { $_.Extension -eq '.ruleset' -or $_.Name -eq 'SonarLint.xml' } | Sort-Object FullName | ForEach-Object {
        $result[[IO.Path]::GetRelativePath($conf, $_.FullName)] = Get-XmlFingerprint (Read-Xml $_.FullName).DocumentElement
    }
    $xml = Read-Xml "$conf/SonarQubeAnalysisConfig.xml"
    foreach ($plugin in $xml.SelectNodes("//*[local-name()='AnalyzerPlugin']")) {
        foreach ($assembly in $plugin.SelectNodes(".//*[local-name()='Path']")) {
            if ($assembly.InnerText.EndsWith('.dll')) {
                $key = 'analyzer/' + $plugin.GetAttribute('Key') + '/' + $plugin.GetAttribute('Version') + '/' + [IO.Path]::GetFileName($assembly.InnerText)
                $result[$key] = (Get-FileHash -LiteralPath $assembly.InnerText -Algorithm SHA256).Hash
            }
        }
    }
    Require ($result.Count -gt 0) 'Missing analyzer fingerprint'
    return $result
}
if ($Mode -eq 'Pack') {
    Require (-not (Test-Path -LiteralPath $Bundle)) 'Bundle already exists'
    [IO.Directory]::CreateDirectory($Bundle) | Out-Null
    Copy-Item -LiteralPath "$Workspace/.sonarqube/out" -Destination "$Bundle/out" -Recurse
    Get-ChildItem "$Workspace/.sonarqube/conf/*/FilesToAnalyze.txt" | ForEach-Object {
        Copy-Data $_.FullName "$Bundle/conf/$($_.Directory.Name)/FilesToAnalyze.txt"
    }
    $commit = & git -C $Workspace rev-parse HEAD
    Require ($LASTEXITCODE -eq 0) 'Cannot resolve source revision'
    @{ fingerprintFormat = 2; scanner = '11.3.0'; workspace = $Workspace; revision = $commit; fingerprint = (Get-Fingerprint) } |
        ConvertTo-Json -Depth 8 | Set-Content "$Bundle/manifest.json"
    exit
}
Require ((Get-Item -LiteralPath "$Bundle/manifest.json").Length -le 1048576) 'Oversized manifest'
$manifest = Get-Content -Raw -LiteralPath "$Bundle/manifest.json" | ConvertFrom-Json -AsHashtable
Require ($manifest.fingerprintFormat -eq 2 -and $manifest.scanner -eq '11.3.0' -and $manifest.workspace -ceq $Workspace -and $manifest.revision -ceq $Revision) 'Scanner, workspace or source revision mismatch'
if ($Mode -eq 'Install') {
    $fresh = Get-Fingerprint
    Require ($fresh.Count -eq $manifest.fingerprint.Count) 'Analyzer configuration changed'
    foreach ($key in $fresh.Keys) { Require ($manifest.fingerprint[$key] -ceq $fresh[$key]) "Analyzer configuration changed: $key; rerun CI" }
    foreach ($folder in 'out', 'conf') {
        [IO.Directory]::CreateDirectory("$Workspace/.sonarqube/$folder") | Out-Null
        Get-ChildItem -LiteralPath "$Staging/$folder" | Copy-Item -Destination "$Workspace/.sonarqube/$folder" -Recurse -Force
    }
    exit
}
Require (-not (Test-Path -LiteralPath $Staging)) 'Staging already exists'
$files = @(Get-ChildItem -LiteralPath $Bundle -Recurse -Force)
Require ($files.Count -le 20000) 'Too many artifact entries'
$total = 0L
foreach ($file in $files) {
    Require (-not ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) 'Linked artifact entry'
    if (-not $file.PSIsContainer) {
        $total += $file.Length
        Require ($file.Length -le 134217728 -and $total -le 1073741824) 'Oversized artifact'
        Require ($file.Extension -in '.xml', '.json', '.pb', '.txt', '.ucfgs', '.typedefs', '.udg', '.log', '.lock') 'Unexpected artifact file type'
    }
}
$tracked = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
& git -C $Workspace ls-files | ForEach-Object { [void]$tracked.Add($_) }
Require ($LASTEXITCODE -eq 0) 'Cannot enumerate source files'
function Get-Owned([string]$Path) {
    Require ($Path.StartsWith($Workspace + '/', [StringComparison]::Ordinal)) 'Path outside source workspace'
    $relative = $Path.Substring($Workspace.Length + 1)
    Require (-not ($relative.Split('/') | Where-Object { $_ -in '.git', '.sonarqube', 'obj', 'bin' })) 'Reserved analysis source path'
    Require ($tracked.Contains($relative)) 'Untracked analysis source'
    [void](Resolve-Child $Workspace $relative)
    return $relative
}
$expected = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($solution in 'src/GitVersion.slnx', 'new-cli/GitVersion.slnx', 'build/CI.slnx') {
    $xml = Read-Xml "$Workspace/$solution"
    foreach ($project in $xml.SelectNodes('//Project')) {
        $path = Resolve-Child ([IO.Path]::GetDirectoryName("$Workspace/$solution")) $project.GetAttribute('Path')
        [void]$expected.Add((Get-Owned $path))
    }
}
$projects = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($infoPath in Get-ChildItem "$Bundle/out/*/ProjectInfo.xml" | Sort-Object { [int]$_.Directory.Name } -Descending) {
    $folder = $infoPath.Directory.Name
    Require ($folder -match '^\d+$') 'Invalid project output directory'
    $info = Read-Xml $infoPath.FullName
    $project = Get-Owned $info.ProjectInfo.FullPath
    Require ($expected.Contains($project)) "Unexpected project: $project"
    Require ($info.ProjectInfo.ProjectLanguage -eq 'C#' -and $info.ProjectInfo.IsExcluded -eq 'false') 'Unexpected project language or exclusion'
    $digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($project))).Substring(0,32)
    $guid = [Guid]::ParseExact($digest, 'N')
    Require ([Guid]$info.ProjectInfo.ProjectGuid -eq $guid) 'Unexpected project identifier'
    # A solution can rebuild a shared project; retain its latest completed output.
    if (-not $projects.Add($project)) { continue }
    $accepted = @(Get-Content -LiteralPath (Resolve-Child $Bundle "conf/$folder/FilesToAnalyze.txt") | Where-Object {
        $_.StartsWith($Workspace + '/', [StringComparison]::Ordinal) -and $tracked.Contains($_.Substring($Workspace.Length + 1))
    })
    foreach ($source in $accepted) { [void](Get-Owned $source) }
    $list = "$Workspace/.sonarqube/conf/$folder/FilesToAnalyze.txt"
    $stagedList = Resolve-Child $Staging "conf/$folder/FilesToAnalyze.txt"
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($stagedList)) | Out-Null
    [IO.File]::WriteAllLines($stagedList, [string[]]$accepted)
    foreach ($file in Get-ChildItem -LiteralPath $infoPath.Directory.FullName -Recurse -File) {
        if ($file.FullName -eq $infoPath.FullName) { continue }
        Require ($file.Name -ne 'ProjectInfo.xml') 'Nested project metadata'
        $relative = [IO.Path]::GetRelativePath("$Bundle/out", $file.FullName)
        Copy-Data $file.FullName (Resolve-Child $Staging "out/$relative")
    }
    # Reconstruct project metadata. No fork-supplied settings or executable paths survive.
    $clean = [Xml.XmlDocument]::new()
    $root = $clean.CreateElement('ProjectInfo', $ns)
    [void]$clean.AppendChild($root)
    $type = if ($project -match '\.Tests/|/GitVersion.Testing/') { 'Test' } else { 'Product' }
    foreach ($entry in @{
        ProjectName = [IO.Path]::GetFileNameWithoutExtension($project); ProjectLanguage = 'C#'; ProjectType = $type
        ProjectGuid = $guid.ToString(); FullPath = "$Workspace/$project"; IsExcluded = 'false'
        Configuration = 'Release'; Platform = 'AnyCPU'; TargetFramework = 'net10.0'
    }.GetEnumerator()) {
        $node = $clean.CreateElement($entry.Key, $ns); $node.InnerText = $entry.Value; [void]$root.AppendChild($node)
    }
    $results = $clean.CreateElement('AnalysisResultFiles', $ns)
    $result = $clean.CreateElement('AnalysisResultFile', $ns)
    $result.SetAttribute('Id', 'FilesToAnalyze'); $result.SetAttribute('Location', $list)
    [void]$results.AppendChild($result); [void]$root.AppendChild($results)
    $settings = $clean.CreateElement('AnalysisSettings', $ns)
    foreach ($entry in @{
        'sonar.cs.roslyn.reportFilePaths' = "$Workspace/.sonarqube/out/$folder/Issues.json"
        'sonar.cs.analyzer.projectOutPaths' = "$Workspace/.sonarqube/out/$folder"
        'sonar.cs.scanner.telemetry' = "$Workspace/.sonarqube/out/$folder/Telemetry.json"
    }.GetEnumerator()) {
        $node = $clean.CreateElement('Property', $ns); $node.SetAttribute('Name', $entry.Key); $node.InnerText = $entry.Value; [void]$settings.AppendChild($node)
    }
    [void]$root.AppendChild($settings)
    $target = Resolve-Child $Staging "out/$folder/ProjectInfo.xml"
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    $clean.Save($target)
}
Require ($projects.SetEquals($expected)) 'Incomplete analysis project inventory'
$reports = @(Get-ChildItem -LiteralPath $Coverage -Recurse -File -Filter '*cobertura*.xml')
$testProjects = @($expected | Where-Object { $_ -like 'src/*.Tests/*.csproj' })
Require ($reports.Count -eq $testProjects.Count) 'Incomplete coverage report inventory'
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($report in $reports) {
    $name = $report.Directory.Parent.Name
    Require ($expected.Contains("src/$name/$name.csproj") -and $seen.Add($name)) 'Unexpected coverage report'
    $xml = Read-Xml $report.FullName
    $sources = @($xml.SelectNodes('/coverage/sources/source') | ForEach-Object { $_.InnerText } | Where-Object { [IO.Path]::IsPathFullyQualified($_) })
    $owned = 0
    foreach ($item in @($xml.SelectNodes('//class'))) {
        $matches = @($sources | ForEach-Object { [IO.Path]::GetFullPath([IO.Path]::Combine($_, $item.GetAttribute('filename'))) } | Where-Object { $_.StartsWith($Workspace + '/', [StringComparison]::Ordinal) })
        if ($matches.Count -eq 0) { [void]$item.ParentNode.RemoveChild($item); continue }
        [void](Get-Owned $matches[0]); $item.SetAttribute('filename', $matches[0]); $owned++
    }
    Require ($owned -gt 0) 'Coverage report has no repository sources'
    foreach ($source in $xml.SelectNodes('/coverage/sources/source')) { $source.InnerText = '/' }
    [IO.Directory]::CreateDirectory("$Staging/coverage") | Out-Null
    $xml.Save("$Staging/coverage/$name.xml")
}
Write-Host "Validated $($projects.Count) projects and $($reports.Count) coverage reports."
