 $patterns = "DirectionBreak","FTT-Candidate","FTT-Confirmed","Structure","Action","TrendType"
 foreach ($p in $patterns) {
	$count = (Select-String -Path .\Output.txt -Pattern $p).Count
	"{0}: {1}" -f $p, $count
}
	