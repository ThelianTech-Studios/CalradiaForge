param(
	[string]$Filter = '*',
	[string]$ArtifactsPath = 'source/CalradiaForge.Benchmarks/BenchmarkDotNet.Artifacts'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$resolvedArtifactsPath = Join-Path $repositoryRoot $ArtifactsPath
$projectPath = Join-Path $repositoryRoot 'source/CalradiaForge.Benchmarks/CalradiaForge.Benchmarks.csproj'

New-Item -ItemType Directory -Force -Path $resolvedArtifactsPath | Out-Null

$metadataPath = Join-Path $resolvedArtifactsPath 'phase4-environment.txt'
$branch = git -C $repositoryRoot rev-parse --abbrev-ref HEAD
$commit = git -C $repositoryRoot rev-parse HEAD
$status = git -C $repositoryRoot status --short

@(
	'Status: Infrastructure validation / Provisional pre-refactor baseline'
	'Comparability: Not comparable to final post-refactor baseline'
	"CapturedUtc: $([DateTime]::UtcNow.ToString('O'))"
	"Branch: $branch"
	"Commit: $commit"
	"WorkingTreeStatus: $(if ($status) { 'Modified' } else { 'Clean' })"
	'BuildConfiguration: Release'
	'BenchmarkFramework: BenchmarkDotNet 0.15.2'
	'FilesystemAndAntivirusNotes: Record local conditions here before interpreting filesystem results.'
) | Set-Content -LiteralPath $metadataPath

dotnet --info | Add-Content -LiteralPath $metadataPath

Push-Location $repositoryRoot
$benchmarkStartUtc = [DateTime]::UtcNow
try {
	dotnet run --project $projectPath -c Release --no-restore -- --filter $Filter --artifacts $resolvedArtifactsPath
	$benchmarkExitCode = $LASTEXITCODE
} finally {
	Pop-Location
}

if ($benchmarkExitCode -ne 0) {
	exit $benchmarkExitCode
}

$resultPath = Join-Path $resolvedArtifactsPath 'results'
$currentReports = Get-ChildItem -LiteralPath $resultPath -Filter '*report-github.md' -ErrorAction SilentlyContinue |
	Where-Object { $_.LastWriteTimeUtc -ge $benchmarkStartUtc.AddSeconds(-2) }

if (-not $currentReports) {
	Write-Error 'BenchmarkDotNet did not produce a current result report.'
	exit 1
}

$invalidReport = $currentReports |
	Where-Object { (Get-Content -Raw -LiteralPath $_.FullName) -match '\|\s+NA\s+\|' } |
	Select-Object -First 1
if ($invalidReport) {
	Write-Error "BenchmarkDotNet produced a report without measurements: $($invalidReport.FullName)"
	exit 1
}
