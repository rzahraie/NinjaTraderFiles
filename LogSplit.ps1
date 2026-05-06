$input = "NinjaScript Output 5_5_2026 9_45 PMFull.txt"
$barsPerFile = 50

$lines = Get-Content $input

$barCount = 0
$fileIndex = 0
$output = @()

foreach ($line in $lines) {
    if ($line -like "*APVA DEBUG*") {
        if ($barCount -gt 0 -and ($barCount % $barsPerFile) -eq 0) {
            $output | Out-File "$input.part$fileIndex.txt"
            $output = @()
            $fileIndex++
        }
        $barCount++
    }
    $output += $line
}

if ($output.Count -gt 0) {
    $output | Out-File "$input.part$fileIndex.txt"
}