namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualVolumeStateClassifier
    {
        public static ManualVolumeState Classify(
		    ManualVolumeSequence sequence)
		{
		    if (string.IsNullOrEmpty(sequence.SequenceText) || sequence.SequenceText == "NONE")
		        return ManualVolumeState.Unknown;
		
		    // Too many flips = real chop
		    if (sequence.FlipCount >= 6)
		        return ManualVolumeState.MixedChop;
		
		    // Controlled alternation (this is GOOD, not chop)
		    if (sequence.HasDominantShift && sequence.HasNonDominantReturn)
		        return ManualVolumeState.BalancedAlternation;
		
		    if (sequence.HasDominantShift && !sequence.HasNonDominantReturn)
		        return ManualVolumeState.DominantPulse;
		
		    if (!sequence.HasDominantShift && sequence.HasNonDominantReturn)
		        return ManualVolumeState.NonDominantReturn;
		
		    return ManualVolumeState.Unknown;
		}
    }
}
