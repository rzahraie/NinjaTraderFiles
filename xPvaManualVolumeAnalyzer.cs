namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum ManualVolumeLabel
    {
        Unknown = 0,
        Peak = 1,
        Trough = 2,
        P1 = 3,
        PP1 = 4
    }

    public readonly struct ManualVolumeEvent
    {
        public readonly int BarIndex;
        public readonly ManualVolumeLabel Label;
        public readonly long Volume;

        public ManualVolumeEvent(
            int barIndex,
            ManualVolumeLabel label,
            long volume)
        {
            BarIndex = barIndex;
            Label = label;
            Volume = volume;
        }
    }

    public static class xPvaManualVolumeAnalyzer
    {
        public static ManualVolumeEvent[] Analyze(
		    int startBar,
		    int endBar,
		    System.Func<int, long> getVolume)
		{
		    if (endBar <= startBar)
		        return new ManualVolumeEvent[0];
		
		    var results = new System.Collections.Generic.List<ManualVolumeEvent>();
		
		    int maxIdx = startBar;
		    long maxVol = getVolume(startBar);
		
		    int minIdx = startBar;
		    long minVol = getVolume(startBar);
		
		    for (int idx = startBar + 1; idx <= endBar; idx++)
		    {
		        long v = getVolume(idx);
		
		        if (v > maxVol)
		        {
		            maxVol = v;
		            maxIdx = idx;
		        }
		
		        if (v < minVol)
		        {
		            minVol = v;
		            minIdx = idx;
		        }
		    }
		
		    results.Add(new ManualVolumeEvent(maxIdx, ManualVolumeLabel.P1, maxVol));
		
		    if (minIdx != maxIdx)
		        results.Add(new ManualVolumeEvent(minIdx, ManualVolumeLabel.PP1, minVol));
		
		    return results.ToArray();
		}
    }
}
