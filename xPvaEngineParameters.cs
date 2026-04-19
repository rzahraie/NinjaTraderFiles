namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaEngineParameters
    {
		public int MaxNoneBarsInPosition { get; set; } = 3;
		
        public double EpsilonTicks = 0.5;

        public int VolumeNormLookback = 20;
        public int DirectionLookback = 8;
        public int FlipLookback = 12;
        public int ImbalanceLookback = 12;

        public double VolumeExpandThreshold = 0.15;
        public double VolumeContractThreshold = 0.15;

        public double DirectionThreshold = 1.25;

        public double DominanceBodyToRangeMin = 0.35;
        public double DominanceCloseLocationMin = 0.65;
        public double DominanceNormVolumeMin = 0.95;

        public int MinLateralBars = 3;
        public double LateralBiasThreshold = 0.15;

        public double StrongImbalanceThreshold = 0.25;
        public double NeutralImbalanceThreshold = 0.10;
        public int MaxPullbackBars = 3;
        public int StableBarsMin = 2;
    }
}
