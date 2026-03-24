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
        public readonly ManualBarPolarity Polarity;

        public ManualVolumeEvent(
            int barIndex,
            ManualVolumeLabel label,
            long volume,
            ManualBarPolarity polarity)
        {
            BarIndex = barIndex;
            Label = label;
            Volume = volume;
            Polarity = polarity;
        }
    }

    public static class xPvaManualVolumeAnalyzer
    {
        public static ManualVolumeEvent[] Analyze(
            int startBar,
            int endBar,
            System.Func<int, long> getVolume,
            System.Func<int, double> getOpen,
            System.Func<int, double> getClose,
            bool isUpContainer)
        {
            if (endBar <= startBar)
                return new ManualVolumeEvent[0];

            var bars = new System.Collections.Generic.List<ManualVolumeBar>();

            for (int idx = startBar; idx <= endBar; idx++)
            {
                double o = getOpen(idx);
                double c = getClose(idx);

                ManualBarPolarity polarity;
                if (c > o)
                    polarity = ManualBarPolarity.Black;
                else if (c < o)
                    polarity = ManualBarPolarity.Red;
                else
                    polarity = ManualBarPolarity.Doji;

                bars.Add(new ManualVolumeBar(
                    idx,
                    getVolume(idx),
                    polarity));
            }

            var extrema = new System.Collections.Generic.List<ManualVolumeEvent>();

            for (int i = 1; i < bars.Count - 1; i++)
            {
                var prev = bars[i - 1];
                var cur = bars[i];
                var next = bars[i + 1];

                if (cur.Volume > prev.Volume && cur.Volume > next.Volume)
                {
                    extrema.Add(new ManualVolumeEvent(
                        cur.BarIndex,
                        ManualVolumeLabel.Peak,
                        cur.Volume,
                        cur.Polarity));
                }
                else if (cur.Volume < prev.Volume && cur.Volume < next.Volume)
                {
                    extrema.Add(new ManualVolumeEvent(
                        cur.BarIndex,
                        ManualVolumeLabel.Trough,
                        cur.Volume,
                        cur.Polarity));
                }
            }

            if (extrema.Count == 0)
                return new ManualVolumeEvent[0];
			
			long maxExtremaVol = 0;
			for (int i = 0; i < extrema.Count; i++)
			{
			    if (extrema[i].Volume > maxExtremaVol)
			        maxExtremaVol = extrema[i].Volume;
			}
			
			long minKeepVol = (long)(maxExtremaVol * 0.40);
			
			var filteredExtrema = new System.Collections.Generic.List<ManualVolumeEvent>();
			for (int i = 0; i < extrema.Count; i++)
			{
			    if (extrema[i].Volume >= minKeepVol)
			        filteredExtrema.Add(extrema[i]);
			}
			
			if (filteredExtrema.Count == 0)
			    return new ManualVolumeEvent[0];

            // First pass labeling rule:
            // In an up container, prefer first Red extreme as P1, then next Black extreme as PP1.
            // In a down container, prefer first Black extreme as P1, then next Red extreme as PP1.
            int p1Index = -1;
			int pp1Index = -1;
			
			ManualBarPolarity p1Wanted = isUpContainer
			    ? ManualBarPolarity.Red
			    : ManualBarPolarity.Black;
			
			ManualBarPolarity pp1Wanted = isUpContainer
			    ? ManualBarPolarity.Black
			    : ManualBarPolarity.Red;
			
			// Choose P1 as the largest-volume preferred-polarity extreme from the filtered set.
			long bestP1Vol = long.MinValue;
			
			for (int i = 0; i < filteredExtrema.Count; i++)
			{
			    if (filteredExtrema[i].Polarity == p1Wanted && filteredExtrema[i].Volume > bestP1Vol)
			    {
			        bestP1Vol = filteredExtrema[i].Volume;
			        p1Index = i;
			    }
			}
			
			// Fallback: largest-volume filtered extreme overall.
			if (p1Index < 0)
			{
			    for (int i = 0; i < filteredExtrema.Count; i++)
			    {
			        if (filteredExtrema[i].Volume > bestP1Vol)
			        {
			            bestP1Vol = filteredExtrema[i].Volume;
			            p1Index = i;
			        }
			    }
			}
			
			int p1BarIndex = filteredExtrema[p1Index].BarIndex;

			// Choose PP1 as the first preferred-polarity extremum after P1 from the full original extrema list.
			for (int i = 0; i < extrema.Count; i++)
			{
			    if (extrema[i].BarIndex > p1BarIndex && extrema[i].Polarity == pp1Wanted)
			    {
			        pp1Index = i;
			        break;
			    }
			}
			
			// Fallback: first later extremum after P1 from the full original extrema list.
			if (pp1Index < 0)
			{
			    for (int i = 0; i < extrema.Count; i++)
			    {
			        if (extrema[i].BarIndex > p1BarIndex)
			        {
			            pp1Index = i;
			            break;
			        }
			    }
			}
			
			// Choose PP1 as the first preferred-polarity extreme after P1.
			if (p1Index >= 0)
			{
			    for (int i = p1Index + 1; i < extrema.Count; i++)
			    {
			        if (extrema[i].Polarity == pp1Wanted)
			        {
			            pp1Index = i;
			            break;
			        }
			    }
			}
			
			// Fallback: if none found, use the next extreme after P1.
			if (pp1Index < 0 && p1Index >= 0 && p1Index + 1 < extrema.Count)
			    pp1Index = p1Index + 1;
            var results = new System.Collections.Generic.List<ManualVolumeEvent>();

		for (int i = 0; i < extrema.Count; i++)
		{
		    var e = extrema[i];
		
		    if (e.BarIndex == filteredExtrema[p1Index].BarIndex)
		    {
		        results.Add(new ManualVolumeEvent(
		            e.BarIndex,
		            ManualVolumeLabel.P1,
		            e.Volume,
		            e.Polarity));
		    }
		    else if (pp1Index >= 0 && i == pp1Index)
		    {
		        results.Add(new ManualVolumeEvent(
		            e.BarIndex,
		            ManualVolumeLabel.PP1,
		            e.Volume,
		            e.Polarity));
		    }
		    else
		    {
		        results.Add(e);
		    }
		}

            return results.ToArray();
        }
    }
}



