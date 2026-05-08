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
    "ImminentFTT: True"
)

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

    "" | Tee-Object -FilePath $out -Append
}

Write-Host "Done. Summary written to:"
Write-Host $out