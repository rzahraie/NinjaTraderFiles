namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class ManualSignalState
    {
        public int LastContainerId = -1;
        public int? LastConfirmedBar = null;
        public bool? LastDirectionUp = null;

        public string LastSignal = null;
        public double? LastEntry = null;
        public double? LastStop = null;
        public double? LastTarget = null;
		
		public ManualVolumeState? LastVolumeState = null;
		public int? LastAnalysisEndBar = null;
    }
}
