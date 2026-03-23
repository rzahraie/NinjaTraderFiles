namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum ManualBarPolarity
    {
        Unknown = 0,
        Black = 1,
        Red = 2,
        Doji = 3
    }

    public readonly struct ManualVolumeBar
    {
        public readonly int BarIndex;
        public readonly long Volume;
        public readonly ManualBarPolarity Polarity;

        public ManualVolumeBar(
            int barIndex,
            long volume,
            ManualBarPolarity polarity)
        {
            BarIndex = barIndex;
            Volume = volume;
            Polarity = polarity;
        }
    }
}