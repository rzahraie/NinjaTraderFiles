namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public readonly struct ManualVolumeSequence
    {
        public readonly int ContainerId;
        public readonly string SequenceText;
        public readonly bool HasDominantShift;
        public readonly bool HasNonDominantReturn;
        public readonly bool IsMixed;
		public readonly int FlipCount;

        public ManualVolumeSequence(
            int containerId,
            string sequenceText,
            bool hasDominantShift,
            bool hasNonDominantReturn,
            bool isMixed,
			int flipCount)
        {
            ContainerId = containerId;
            SequenceText = sequenceText;
            HasDominantShift = hasDominantShift;
            HasNonDominantReturn = hasNonDominantReturn;
            IsMixed = isMixed;
			FlipCount = flipCount;
        }
    }
}
