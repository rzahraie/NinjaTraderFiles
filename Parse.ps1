$dir = "C:\Users\rz0\Documents\NinjaTrader 8\bin\Custom\Indicators"
$base = "NinjaScript Output 5_5_2026 9_45 PMFull.txt.part"
$out  = Join-Path $dir "APVA_LogScan_Summary.txt"

$patterns = @(
    "FTT Confirmed: True",
    "FttDetectionAllowed: True",
    "FttDetectionAllowed: False",
    "FttDetectionBlockReason:",
    "UnknownReference",
    "Blocked by weak/low-score container",
    "Blocked by insufficient warning buildup",
    "WarningDuration: 2",
    "WarningDuration: 3",
    "ImminentFTT: True",
    "FTT Kind:",
    "FTT Reason:",
    "BarsSinceLastFtt:",
    "DistanceToLTL:",
    "Score:",
    "Primary Score:",
    "Secondary Score:",
    "SelectedContainer:",
	"Blocked by non-dominant current segment",
	"SelectedContainerScore:",
	"PrimaryContainerScore:",
	"SecondaryContainerScore:",
	"SelectedContainerScoreSnapshot:",
	"PrimaryContainerScoreSnapshot:",
	"SecondaryContainerScoreSnapshot:",
	"FTT_OUTCOME"
)

$results = @()

"" | Out-File $out

for ($i = 0; $i -le 37; $i++) {
    $file = Join-Path $dir "$base$i.txt"

    if (!(Test-Path $file)) {
        "MISSING: $file" | Tee-Object -FilePath $out -Append
        continue
    }

    "===== part$i =====" | Tee-Object -FilePath $out -Append

    foreach ($p in $patterns) {
        $matches = Select-String -Path $file -Pattern $p
        "$p : $($matches.Count)" | Tee-Object -FilePath $out -Append
    }

	"`n--- FTT Outcomes ---" | Tee-Object -FilePath $out -Append

	$lines = Get-Content $file

	foreach ($line in $lines) {
		if ($line -like "*FTT_OUTCOME*") {
			$line | Tee-Object -FilePath $out -Append
			
		$entry   = [regex]::Match($line, "Entry=(\d+)").Groups[1].Value
		$dir     = [regex]::Match($line, "Dir=(\w+)").Groups[1].Value
		$score   = [regex]::Match($line, "Score=([-\d\.NaN]+)").Groups[1].Value
		$domSeq  = [regex]::Match($line, "DomSeq=(\w+)").Groups[1].Value
		$failSeq = [regex]::Match($line, "FailSeq=(\w+)").Groups[1].Value
		$segDom  = [regex]::Match($line, "SegDom=(\w+)").Groups[1].Value
		$mfe20   = [regex]::Match($line, "MFE20=([-\d\.]+)").Groups[1].Value
		$mae20   = [regex]::Match($line, "MAE20=([-\d\.]+)").Groups[1].Value

			if ($score -ne "NaN" -and $score -ne "") {
				$results += [pscustomobject]@{
					Entry   = [int]$entry
					Dir     = $dir
					Score   = [double]$score
					DomSeq  = $domSeq
					FailSeq = $failSeq
					SegDom  = $segDom
					MFE20   = [double]$mfe20
					MAE20   = [double]$mae20
				}
			}
		}
	}

	for ($n = 0; $n -lt $lines.Count; $n++) {
		if ($lines[$n] -like "*FTT Confirmed: True*") {
			$start = [Math]::Max(0, $n - 20)
			$end   = [Math]::Min($lines.Count - 1, $n + 25)

			">>> Context around line $($n + 1)" | Tee-Object -FilePath $out -Append

			for ($k = $start; $k -le $end; $k++) {
				$lineNo = $k + 1
				"$lineNo`t$($lines[$k])" | Tee-Object -FilePath $out -Append
			}

			"" | Tee-Object -FilePath $out -Append
		}
	}

    "" | Tee-Object -FilePath $out -Append
}

foreach ($line in $lines) {
    if ($line -like "*FTT_OUTCOME*") {

        $entry  = [regex]::Match($line, "Entry=(\d+)").Groups[1].Value
        $dir    = [regex]::Match($line, "Dir=(\w+)").Groups[1].Value
        $score  = [regex]::Match($line, "Score=([\d\.]+)").Groups[1].Value
        $domSeq = [regex]::Match($line, "DomSeq=(\w+)").Groups[1].Value
        $failSeq= [regex]::Match($line, "FailSeq=(\w+)").Groups[1].Value

        $mfe20  = [regex]::Match($line, "MFE20=([-\d\.]+)").Groups[1].Value
        $mae20  = [regex]::Match($line, "MAE20=([-\d\.]+)").Groups[1].Value

        $results += [pscustomobject]@{
            Entry  = [int]$entry
            Dir    = $dir
            Score  = [double]$score
            DomSeq = $domSeq
            FailSeq= $failSeq
            MFE20  = [double]$mfe20
            MAE20  = [double]$mae20
        }
    }
}

"`n===== STRATIFIED OUTCOME STATS =====" | Tee-Object -FilePath $out -Append

$scoreGroups = $results | Group-Object {
    if ($_.Score -ge 8) { "Score8+" }
    elseif ($_.Score -ge 6) { "Score6-7" }
    else { "Score4-5" }
}

foreach ($g in $scoreGroups) {
    $avgMFE = ($g.Group | Measure-Object MFE20 -Average).Average
    $avgMAE = ($g.Group | Measure-Object MAE20 -Average).Average

    "ScoreGroup=$($g.Name) Count=$($g.Count) AvgMFE20=$('{0:F2}' -f $avgMFE) AvgMAE20=$('{0:F2}' -f $avgMAE)" |
        Tee-Object -FilePath $out -Append
}

"`n===== DOMINANCE SEQUENCE GROUPS =====" | Tee-Object -FilePath $out -Append

$seqGroups = $results | Group-Object DomSeq, FailSeq

foreach ($g in $seqGroups) {
    $avgMFE = ($g.Group | Measure-Object MFE20 -Average).Average
    $avgMAE = ($g.Group | Measure-Object MAE20 -Average).Average

    "SeqGroup=$($g.Name) Count=$($g.Count) AvgMFE20=$('{0:F2}' -f $avgMFE) AvgMAE20=$('{0:F2}' -f $avgMAE)" |
        Tee-Object -FilePath $out -Append
}

"`n===== SEGMENT DOMINANCE GROUPS =====" | Tee-Object -FilePath $out -Append

$segGroups = $results | Group-Object SegDom

foreach ($g in $segGroups) {
    $avgMFE = ($g.Group | Measure-Object MFE20 -Average).Average
    $avgMAE = ($g.Group | Measure-Object MAE20 -Average).Average

    "SegDom=$($g.Name) Count=$($g.Count) AvgMFE20=$('{0:F2}' -f $avgMFE) AvgMAE20=$('{0:F2}' -f $avgMAE)" |
        Tee-Object -FilePath $out -Append
}

Write-Host "Done. Summary written to:"
Write-Host $out