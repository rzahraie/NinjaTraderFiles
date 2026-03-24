namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum ManualBarPolarity
    {
        Unknown = 0,
        Black = 1,
        Red = 2,
        Doji = 3
    }
	
	public enum DominanceType
	{
	    Unknown = 0,
	    Dominant = 1,
	    NonDominant = 2
	}

    public readonly struct ManualVolumeBar
    {
        public readonly int BarIndex;
        public readonly long Volume;
        public readonly ManualBarPolarity Polarity;
		public readonly DominanceType Dominance;

        public ManualVolumeBar(
            int barIndex,
            long volume,
            ManualBarPolarity polarity,
			DominanceType dominance)
        {
            BarIndex = barIndex;
            Volume = volume;
            Polarity = polarity;
			Dominance = dominance;
        }
    }
}

