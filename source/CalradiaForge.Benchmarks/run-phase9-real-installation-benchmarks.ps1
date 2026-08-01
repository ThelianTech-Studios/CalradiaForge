param(
	[switch]$ConsentToReadRealInstallation,
	[string]$ArtifactsPath = 'source/CalradiaForge.Benchmarks/BenchmarkDotNet.Artifacts/phase9-real-installation'
)

$ErrorActionPreference = 'Stop'
if (-not $ConsentToReadRealInstallation) {
	Write-Error 'Real-installation benchmarks require -ConsentToReadRealInstallation. They read local Bannerlord and Workshop metadata in place.'
}

$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$artifactBase = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'source/CalradiaForge.Benchmarks/BenchmarkDotNet.Artifacts'))
$resolvedArtifactsPath = [IO.Path]::GetFullPath((Join-Path $repositoryRoot $ArtifactsPath))
$artifactBasePrefix = $artifactBase.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $resolvedArtifactsPath.StartsWith($artifactBasePrefix, [StringComparison]::OrdinalIgnoreCase)) {
	Write-Error 'The artifacts path must be a child of the benchmark project BenchmarkDotNet.Artifacts directory.'
}
$projectPath = Join-Path $repositoryRoot 'source/CalradiaForge.Benchmarks/CalradiaForge.Benchmarks.csproj'
$benchmarkStartUtc = [DateTime]::UtcNow

function Get-SteamRoot {
	$candidates = @(
		[Microsoft.Win32.Registry]::GetValue('HKEY_CURRENT_USER\SOFTWARE\Valve\Steam', 'SteamPath', $null),
		[Microsoft.Win32.Registry]::GetValue('HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam', 'InstallPath', $null)
	) | Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Container) } | Select-Object -Unique
	if (-not $candidates) {
		throw 'Steam installation metadata was not found.'
	}
	return [IO.Path]::GetFullPath([string]$candidates[0])
}

function Get-SteamLibraries([string]$steamRoot) {
	$libraries = [Collections.Generic.List[string]]::new()
	$libraries.Add($steamRoot)
	$catalog = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
	if (Test-Path -LiteralPath $catalog -PathType Leaf) {
		$content = Get-Content -Raw -LiteralPath $catalog
		foreach ($match in [regex]::Matches($content, '"path"\s+"([^"]+)"')) {
			$candidate = $match.Groups[1].Value -replace '\\\\', '\'
			if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Container) -and -not $libraries.Contains($candidate)) {
				$libraries.Add([IO.Path]::GetFullPath($candidate))
			}
		}
	}
	return @($libraries)
}

function Resolve-BannerlordInstallation([string[]]$libraries) {
	foreach ($library in $libraries) {
		$manifest = Join-Path $library 'steamapps\appmanifest_261550.acf'
		if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
			continue
		}
		$content = Get-Content -Raw -LiteralPath $manifest
		$installMatch = [regex]::Match($content, '"installdir"\s+"([^"]+)"')
		if (-not $installMatch.Success) {
			continue
		}
		$gameRoot = Join-Path $library (Join-Path 'steamapps\common' $installMatch.Groups[1].Value)
		$modulesRoot = Join-Path $gameRoot 'Modules'
		if (-not (Test-Path -LiteralPath (Join-Path $modulesRoot 'Native\SubModule.xml') -PathType Leaf)) {
			continue
		}

		$workshopRoot = Join-Path $library 'steamapps\workshop\content\261550'
		if (-not (Test-Path -LiteralPath $workshopRoot -PathType Container)) {
			$workshopRoot = $libraries |
				ForEach-Object { Join-Path $_ 'steamapps\workshop\content\261550' } |
				Where-Object { Test-Path -LiteralPath $_ -PathType Container } |
				Select-Object -First 1
		}
		if (-not $workshopRoot) {
			throw 'Bannerlord was found, but no readable Workshop content root was found.'
		}
		return [pscustomobject]@{
			GameRoot = [IO.Path]::GetFullPath($gameRoot)
			ModulesRoot = [IO.Path]::GetFullPath($modulesRoot)
			WorkshopRoot = [IO.Path]::GetFullPath($workshopRoot)
		}
	}
	throw 'A valid Bannerlord Steam installation was not found.'
}

function Get-DescriptorCorpusState([string[]]$roots) {
	$entries = [Collections.Generic.List[string]]::new()
	$files = [Collections.Generic.List[IO.FileInfo]]::new()
	$directoryCount = 0
	for ($rootIndex = 0; $rootIndex -lt $roots.Count; $rootIndex++) {
		$root = [IO.Path]::GetFullPath($roots[$rootIndex])
		$rootPrefix = $root.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
		foreach ($directory in Get-ChildItem -LiteralPath $root -Recurse -Directory -ErrorAction Stop) {
			$relative = $directory.FullName.Substring($rootPrefix.Length)
			$entries.Add("D|$rootIndex|$relative")
			$directoryCount++
		}
		foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -Filter SubModule.xml -File -ErrorAction Stop) {
			$relative = $file.FullName.Substring($rootPrefix.Length)
			$contentHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
			$entries.Add("F|$rootIndex|$relative|$($file.Length)|$contentHash")
			$files.Add($file)
		}
	}
	$joinedEntries = [string]::Join('|', @($entries | Sort-Object))
	$sha256 = [Security.Cryptography.SHA256]::Create()
	try {
		$digestBytes = $sha256.ComputeHash([Text.Encoding]::UTF8.GetBytes($joinedEntries))
	} finally {
		$sha256.Dispose()
	}
	return [pscustomobject]@{
		Count = $files.Count
		Bytes = ($files | Measure-Object -Property Length -Sum).Sum
		DirectoryCount = $directoryCount
		Digest = [BitConverter]::ToString($digestBytes).Replace('-', '')
	}
}

function Protect-ArtifactText(
	[string]$artifactRoot,
	[Collections.Generic.Dictionary[string,string]]$replacements) {
	$textExtensions = @('.log', '.txt', '.md', '.csv', '.html')
	Get-ChildItem -LiteralPath $artifactRoot -Recurse -File -ErrorAction SilentlyContinue |
		Where-Object { $textExtensions -contains $_.Extension.ToLowerInvariant() } |
		ForEach-Object {
			$content = Get-Content -Raw -Encoding UTF8 -LiteralPath $_.FullName
			$updated = $content
			foreach ($entry in $replacements.GetEnumerator()) {
				if (-not [string]::IsNullOrWhiteSpace($entry.Key)) {
					$updated = [regex]::Replace(
						$updated,
						[regex]::Escape($entry.Key),
						[Text.RegularExpressions.MatchEvaluator]{ param($match) $entry.Value },
						[Text.RegularExpressions.RegexOptions]::IgnoreCase)
				}
			}
			if ($updated -ne $content) {
				Set-Content -LiteralPath $_.FullName -Value $updated -NoNewline -Encoding UTF8
			}
		}
}

$steamRoot = Get-SteamRoot
$libraries = Get-SteamLibraries $steamRoot
$installation = Resolve-BannerlordInstallation $libraries
$before = Get-DescriptorCorpusState @($installation.ModulesRoot, $installation.WorkshopRoot)
$localTopLevelCount = @(Get-ChildItem -LiteralPath $installation.ModulesRoot -Directory).Count
$workshopTopLevelCount = @(Get-ChildItem -LiteralPath $installation.WorkshopRoot -Directory).Count

New-Item -ItemType Directory -Force -Path $resolvedArtifactsPath | Out-Null
$metadataPath = Join-Path $resolvedArtifactsPath 'phase9-real-installation-environment.txt'
$branch = git -C $repositoryRoot rev-parse --abbrev-ref HEAD
$commit = git -C $repositoryRoot rev-parse HEAD
$status = git -C $repositoryRoot status --short
$productionSourceStatus = git -C $repositoryRoot status --short -- 'source/CalradiaForge.Core' 'source/CalradiaForge.UI' 'source/CalradiaForge.Nexus' 'source/CalradiaForge.ConsoleUtils' 'source/Languages'

@(
	'Status: Owner-approved supplemental real-installation evidence'
	'Comparability: Informational; owner-installation-specific; not a CI or universal baseline'
	"CapturedUtc: $([DateTime]::UtcNow.ToString('O'))"
	"Branch: $branch"
	"Commit: $commit"
	"WorkingTreeStatus: $(if ($status) { 'Modified' } else { 'Clean' })"
	"ProductionSourceStatus: $(if ($productionSourceStatus) { 'Modified' } else { 'Clean' })"
	'InputDiscovery: Local Steam registry and metadata; no application configuration mutation'
	'LiteralInputPathsRetained: False'
	'ArtifactRedaction: Exact input, repository, and user-profile literals replaced after execution'
	'BuildConfiguration: Release'
	'BenchmarkFramework: BenchmarkDotNet 0.15.2'
	'BenchmarkJob: One launch; three warmups; seven measurement iterations; BenchmarkDotNet-selected invocation count'
	'FilesystemCacheState: Warm/repeated; inventory and preflight occur before measurement; no cold-cache control'
	'RuntimeBoundary: Offline BenchmarkDotNet process; no WPF application runtime, dispatcher, navigation, or production logger'
	"RegisteredSteamLibraryCount: $($libraries.Count)"
	"LocalTopLevelDirectoryCount: $localTopLevelCount"
	"WorkshopTopLevelDirectoryCount: $workshopTopLevelCount"
	"DescriptorCount: $($before.Count)"
	"DescriptorBytes: $($before.Bytes)"
	"RecursiveDirectoryCount: $($before.DirectoryCount)"
	'InputMutationPolicy: Read-only; descriptor content and relevant directory-topology fingerprint must remain unchanged'
) | Set-Content -LiteralPath $metadataPath

$replacements = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
$replacements[$installation.GameRoot] = '<GAME_ROOT>'
$replacements[$installation.WorkshopRoot] = '<WORKSHOP_ROOT>'
$replacements[$steamRoot] = '<STEAM_ROOT>'
$replacements[[string]$repositoryRoot] = '<REPO_ROOT>'
if ($env:USERPROFILE) { $replacements[$env:USERPROFILE] = '<USER_PROFILE>' }

$benchmarkExitCode = 1
$corpusStable = $false
$postRunFailure = $null
$redactionFailure = $null
$env:CALRADIAFORGE_PHASE9_GAME_ROOT = $installation.GameRoot
$env:CALRADIAFORGE_PHASE9_WORKSHOP_ROOT = $installation.WorkshopRoot
try {
	Push-Location $repositoryRoot
	try {
		dotnet run --project $projectPath -c Release --no-restore -- --anyCategories OwnerRealInstallation --artifacts $resolvedArtifactsPath
		$benchmarkExitCode = $LASTEXITCODE
	} finally {
		Pop-Location
	}

	$after = Get-DescriptorCorpusState @($installation.ModulesRoot, $installation.WorkshopRoot)
	$corpusStable = $before.Count -eq $after.Count `
		-and $before.Bytes -eq $after.Bytes `
		-and $before.DirectoryCount -eq $after.DirectoryCount `
		-and $before.Digest -eq $after.Digest
} catch {
	$postRunFailure = 'The real-installation benchmark or post-run inventory failed.'
} finally {
	Remove-Item Env:CALRADIAFORGE_PHASE9_GAME_ROOT -ErrorAction SilentlyContinue
	Remove-Item Env:CALRADIAFORGE_PHASE9_WORKSHOP_ROOT -ErrorAction SilentlyContinue
	try {
		if (Test-Path -LiteralPath $resolvedArtifactsPath -PathType Container) {
			Protect-ArtifactText $resolvedArtifactsPath $replacements
		}
	} catch {
		$redactionFailure = 'Artifact redaction failed; the local results must not be promoted.'
	}
}

if ($redactionFailure) {
	Write-Error $redactionFailure
}

$resultPath = Join-Path $resolvedArtifactsPath 'results'
$currentReports = Get-ChildItem -LiteralPath $resultPath -Filter '*report-github.md' -ErrorAction SilentlyContinue |
	Where-Object { $_.LastWriteTimeUtc -ge $benchmarkStartUtc.AddSeconds(-2) }
if (-not $currentReports) {
	Write-Error 'BenchmarkDotNet did not produce a current real-installation result report.'
}
if ($currentReports | Where-Object { (Get-Content -Raw -LiteralPath $_.FullName) -match '\|\s+NA\s+\|' }) {
	Write-Error 'BenchmarkDotNet produced a real-installation report without measurements.'
}

$leaks = Get-ChildItem -LiteralPath $resolvedArtifactsPath -Recurse -File |
	Where-Object { @('.log', '.txt', '.md', '.csv', '.html') -contains $_.Extension.ToLowerInvariant() } |
	Select-String -SimpleMatch -Pattern @(
		$installation.GameRoot,
		$installation.WorkshopRoot,
		[string]$repositoryRoot,
		$env:USERPROFILE
	) -ErrorAction SilentlyContinue
if ($leaks) {
	Write-Error 'Sensitive literal verification failed after artifact redaction.'
}

if ($postRunFailure) {
	Write-Error $postRunFailure
}
if ($benchmarkExitCode -ne 0) {
	exit $benchmarkExitCode
}
if (-not $corpusStable) {
	Write-Error 'The real-installation descriptor content or directory topology changed during benchmark capture; results are invalid.'
}

Add-Content -LiteralPath $metadataPath -Value @(
	'CorpusStableAfterRun: True'
	'RedactionVerification: Passed'
)
