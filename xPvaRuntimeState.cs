using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaRuntimeState
    {
		public int OppositePressureBars { get; set; }
		public bool OppositePressureArmed { get; set; }
		public bool ShockReversalArmed { get; set; }
		public string ShockReason { get; set; }
		public xPvaContainer LastContainer { get; set; }
		
        public xPvaBarFeatures? LastBarFeatures;
        public xPvaDirectionResult LastDirection;
        public xPvaDominanceResult LastDominance;
        public xPvaSequenceStats LastSequenceStats;
        public xPvaImbalanceResult LastImbalance;
        public xPvaLateralResult LastLateral;
        public xPvaSignalResult LastSignal;
        public xPvaExecutionResult LastExecution;

        public readonly Queue<xPvaBarFeatures> FeatureWindow = new Queue<xPvaBarFeatures>();
        public readonly Queue<DominanceState> DominanceWindow = new Queue<DominanceState>();
        public readonly Queue<PricePolarity> PolarityWindow = new Queue<PricePolarity>();

        public int CurrentPosition; // -1 short, 0 flat, +1 long

        public int ActiveLateralStartBar = -1;
        public double ActiveLateralHigh = double.NaN;
        public double ActiveLateralLow = double.NaN;

        public int StableSignalBars = 0;
        public int DegradingSignalBars = 0;
    }
}


