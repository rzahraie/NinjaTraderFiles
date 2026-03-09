$patterns = ":\sDirectionBreak\s",":\sFttCandidate\s",":\sFttConfirmed\s",":\sStructure\s",":\sAction\s",":\sTrendType\s",":\sContainerReport\s"
foreach ($p in $patterns) {
    $count = (Select-String -Path .\Output.txt -Pattern $p).Count
    "{0}: {1}" -f $p, $count
}
	