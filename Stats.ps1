$patterns = ":\sFttConfirmed\s",":\sAction\s",":\sTradeIntent\s",":\sContainerReport\s"
foreach ($p in $patterns) {
    $count = (Select-String -Path .\Output.txt -Pattern $p).Count
    "{0}: {1}" -f $p, $count
}
	