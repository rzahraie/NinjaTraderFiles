$patterns = ":\sFttConfirmed\s",":\sTradeIntent\s",":\sContainerGeometrySnapshot\s"
foreach ($p in $patterns) {
    $count = (Select-String -Path .\Output.txt -Pattern $p).Count
    "{0}: {1}" -f $p, $count
}
	