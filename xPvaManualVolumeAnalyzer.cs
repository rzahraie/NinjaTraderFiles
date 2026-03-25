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
		public readonly DominanceType Dominance;

        public ManualVolumeEvent(
		    int barIndex,
		    ManualVolumeLabel label,
		    long volume,
		    ManualBarPolarity polarity,
		    DominanceType dominance)
		{
		    BarIndex = barIndex;
		    Label = label;
		    Volume = volume;
		    Polarity = polarity;
		    Dominance = dominance;
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

			long prevVolume = -1;
			
			for (int idx = startBar; idx <= endBar; idx++)
			{
			    double o = getOpen(idx);
			    double c = getClose(idx);
			    long v = getVolume(idx);
			
			    ManualBarPolarity polarity;
			    if (c > o)
			        polarity = ManualBarPolarity.Black;
			    else if (c < o)
			        polarity = ManualBarPolarity.Red;
			    else
			        polarity = ManualBarPolarity.Doji;
			
			    DominanceType dominance;
			
			    if (prevVolume < 0)
			    {
			        dominance = DominanceType.Unknown;
			    }
			    else
			    {
			        bool isIncreasing = v > prevVolume;
			
			        if (isUpContainer)
			            dominance = isIncreasing ? DominanceType.Dominant : DominanceType.NonDominant;
			        else
			            dominance = isIncreasing ? DominanceType.NonDominant : DominanceType.Dominant;
			    }
			
			    bars.Add(new ManualVolumeBar(
			        idx,
			        v,
			        polarity,
			        dominance));
			
			    prevVolume = v;
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
                        cur.Polarity,
						cur.Dominance));
                }
                else if (cur.Volume < prev.Volume && cur.Volume < next.Volume)
                {
                    extrema.Add(new ManualVolumeEvent(
                        cur.BarIndex,
                        ManualVolumeLabel.Trough,
                        cur.Volume,
                        cur.Polarity,
						cur.Dominance));
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
			
			long maxFilteredVol = 0;
			for (int i = 0; i < filteredExtrema.Count; i++)
			{
			    if (filteredExtrema[i].Volume > maxFilteredVol)
			        maxFilteredVol = filteredExtrema[i].Volume;
			}
			
			ManualBarPolarity p1Wanted = isUpContainer
			    ? ManualBarPolarity.Red
			    : ManualBarPolarity.Black;
			
			ManualBarPolarity pp1Wanted = isUpContainer
			    ? ManualBarPolarity.Black
			    : ManualBarPolarity.Red;
			
			int ScoreExtremum(ManualVolumeEvent e)
			{
			    int score = 0;
			
			    // Dominance matters most.
			    if (e.Dominance == DominanceType.Dominant)
			        score += 3;
			
			    // Preferred polarity for the container direction.
			    if (e.Polarity == p1Wanted)
			        score += 2;
			
			    // Slight preference for peaks over troughs as anchors.
			    if (e.Label == ManualVolumeLabel.Peak)
			        score += 1;
			
			    // Volume rank bonus, normalized to the strongest filtered extremum.
			    if (maxFilteredVol > 0)
			    {
			        double frac = (double)e.Volume / (double)maxFilteredVol;
			
			        if (frac >= 0.90)
			            score += 3;
			        else if (frac >= 0.70)
			            score += 2;
			        else if (frac >= 0.50)
			            score += 1;
			    }
			
			    return score;
			}
			
			int bestP1Score = int.MinValue;
			long bestP1Vol = long.MinValue;
			
			// Choose P1 as the highest-scoring filtered extremum.
			// Break ties with larger volume, then later bar.
			for (int i = 0; i < filteredExtrema.Count; i++)
			{
			    int score = ScoreExtremum(filteredExtrema[i]);
			    long vol = filteredExtrema[i].Volume;
			
			    if (score > bestP1Score ||
			        (score == bestP1Score && vol > bestP1Vol) ||
			        (score == bestP1Score && vol == bestP1Vol &&
			         filteredExtrema[i].BarIndex > filteredExtrema[p1Index].BarIndex))
			    {
			        bestP1Score = score;
			        bestP1Vol = vol;
			        p1Index = i;
			    }
			}
			
			// Fallback 1: dominant only
			if (p1Index < 0)
			{
			    for (int i = 0; i < filteredExtrema.Count; i++)
			    {
			        if (filteredExtrema[i].Dominance == DominanceType.Dominant &&
			            filteredExtrema[i].Volume > bestP1Vol)
			        {
			            bestP1Vol = filteredExtrema[i].Volume;
			            p1Index = i;
			        }
			    }
			}
			
			// Fallback 2: preferred polarity only
			if (p1Index < 0)
			{
			    for (int i = 0; i < filteredExtrema.Count; i++)
			    {
			        if (filteredExtrema[i].Polarity == p1Wanted &&
			            filteredExtrema[i].Volume > bestP1Vol)
			        {
			            bestP1Vol = filteredExtrema[i].Volume;
			            p1Index = i;
			        }
			    }
			}
			
			// Final fallback: largest filtered extreme overall
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

			int bestPP1Score = int.MinValue;
			long bestPP1Vol = long.MinValue;
			
			// Choose PP1 from the original extrema list, but only after P1.
			// Prefer the expected opposite polarity, then volume/dominance.
			for (int i = 0; i < extrema.Count; i++)
			{
			    var e = extrema[i];
			    if (e.BarIndex <= p1BarIndex)
			        continue;
			
			    int score = 0;
			
			    if (e.Polarity == pp1Wanted)
			        score += 2;
			
			    if (e.Dominance == DominanceType.NonDominant)
			        score += 2;
			
			    if (e.Label == ManualVolumeLabel.Trough)
			        score += 1;
			
			    long vol = e.Volume;
			
			    if (score > bestPP1Score ||
			        (score == bestPP1Score && vol > bestPP1Vol) ||
			        (score == bestPP1Score && vol == bestPP1Vol &&
			         e.BarIndex < extrema[pp1Index].BarIndex))
			    {
			        bestPP1Score = score;
			        bestPP1Vol = vol;
			        pp1Index = i;
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
			            e.Polarity,
						e.Dominance));
			    }
			    else if (pp1Index >= 0 && i == pp1Index)
			    {
			        results.Add(new ManualVolumeEvent(
			            e.BarIndex,
			            ManualVolumeLabel.PP1,
			            e.Volume,
			            e.Polarity,
						e.Dominance));
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








