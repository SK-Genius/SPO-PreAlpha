param([int] $MaximumLineWidth = 120)

$ErrorActionPreference = 'Stop'
$SpoRoot = Split-Path $PSScriptRoot -Parent
$Sources = @(
	Get-ChildItem -LiteralPath (Join-Path $SpoRoot 'Apps'), (Join-Path $SpoRoot 'Libs') -File -Recurse |
		Where-Object { $_.Extension -in '.spo', '.sig' }
	Get-ChildItem -LiteralPath (Join-Path $SpoRoot 'Runtime') -File -Recurse -Filter '*.cs'
	Get-Item -LiteralPath (Join-Path $SpoRoot 'Tests/GUI.Scopes.spo'),
		(Join-Path $SpoRoot 'Tests/GUI.Tests.cs'), (Join-Path $SpoRoot 'Tests/SDL3.Tests.cs'),
		(Join-Path $SpoRoot 'Tests/Launcher.Tests.cs')
	Get-Item -LiteralPath (Join-Path $SpoRoot 'src/Program.cs'), (Join-Path $SpoRoot 'src/mStartup.cs')

)
$Findings = @(
	foreach ($Source in $Sources) {
		$LineNumber = 0
		foreach ($Line in [System.IO.File]::ReadLines($Source.FullName)) {
			$LineNumber += 1
			$Location = "$($Source.FullName):$LineNumber"
			if ($Line.Replace("`t", '    ').Length -gt $MaximumLineWidth) {
				"${Location}: split the line so it can be reviewed without horizontal scrolling."
			}
			if (
				$Source.Directory.Name -eq 'Snake' -and
				$Line -match 'WindowPosTL|WindowOrigin|BufferSize|KeyCode|HasFlag|WithModifiers'
			) {
				"${Location}: move native coordinates or input encoding into the runtime."
			}
			if ($Line -match 'Write\.\.\.WithStyle|CursorVisible\.\.\.|\.\.\.(Foreground|Background|Blink)\.\.\.') {
				"${Location}: use the explicit drawing, cursor or immutable style API."
			}
		}
	}
)
if ($Findings.Count -gt 0) {
	$Findings | Write-Output
	throw "$($Findings.Count) readability findings; refactor before accepting the change."
}
Write-Output "OK: $($Sources.Count) source files pass the mechanical readability checks."
Write-Output 'Also perform the call-site and line-by-line review in SPO/API-Design.md; this scan cannot judge meaning.'
