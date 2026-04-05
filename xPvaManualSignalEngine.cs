namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualSignalEngine
    {
		
		//
		public static ManualSignalDecision Evaluate(
		    ManualContainerAnalysis analysis,
		    ManualSignalState state)
		{
		    bool isTradable = false;
		
		    switch (analysis.VolumeState)
		    {
		        case ManualVolumeState.BalancedAlternation:
		        case ManualVolumeState.DominantPulse:
		            isTradable = analysis.FttConfirmedBar.HasValue;
		            break;
		
		        default:
		            isTradable = false;
		            break;
		    }
		
		    string signal = "SKIP";
		    if (isTradable && analysis.FttConfirmedBar.HasValue)
		        signal = analysis.Snapshot.IsUpContainer ? "LONG" : "SHORT";
		
		    string transition = "FIRST";
		    if (state.LastDirectionUp.HasValue)
		    {
		        bool nowUp = analysis.Snapshot.IsUpContainer;
		
		        if (state.LastSignal == "SKIP")
		            transition = "FRESH";
		        else if (state.LastDirectionUp.Value == nowUp)
		            transition = "CONTINUATION";
		        else
		            transition = "REVERSAL";
		    }
		
		    ManualSignalTransition phase = ManualSignalTransition.Unknown;
		
		    bool sameContainer =
		        state.LastContainerId == analysis.Snapshot.ContainerId;
		
		    if (sameContainer)
		    {
		        bool wasTradable =
		            state.LastVolumeState.HasValue &&
		            (state.LastVolumeState.Value == ManualVolumeState.BalancedAlternation ||
		             state.LastVolumeState.Value == ManualVolumeState.DominantPulse);
		
		        bool nowTradable =
		            analysis.VolumeState == ManualVolumeState.BalancedAlternation ||
		            analysis.VolumeState == ManualVolumeState.DominantPulse;
		
		        if (!wasTradable && nowTradable)
		            phase = ManualSignalTransition.EarlyValid;
		        else if (wasTradable && nowTradable)
		            phase = ManualSignalTransition.StableValid;
		        else if (wasTradable && !nowTradable)
		            phase = ManualSignalTransition.Degrading;
		        else if (!wasTradable && !nowTradable)
		            phase = ManualSignalTransition.Invalidated;
		    }
		    else
		    {
		        if (isTradable)
		            phase = ManualSignalTransition.EarlyValid;
		        else
		            phase = ManualSignalTransition.Unknown;
		    }
		
		    state.LastContainerId = analysis.Snapshot.ContainerId;
		    state.LastConfirmedBar = analysis.FttConfirmedBar;
		    state.LastDirectionUp = analysis.Snapshot.IsUpContainer;
		    state.LastSignal = signal;
		    state.LastVolumeState = analysis.VolumeState;
		    state.LastAnalysisEndBar = analysis.Snapshot.AnalysisEndBarIndex;
		
		    return new ManualSignalDecision(signal, transition, phase);
		}
		//
        public static string EvaluateSignal(
            ManualContainerAnalysis analysis,
            ManualSignalState state)
        {
            bool isTradable = false;

            switch (analysis.VolumeState)
            {
                case ManualVolumeState.BalancedAlternation:
                    isTradable = analysis.FttConfirmedBar.HasValue;
                    break;

                case ManualVolumeState.DominantPulse:
                    isTradable = analysis.FttConfirmedBar.HasValue;
                    break;

                case ManualVolumeState.NonDominantReturn:
                    isTradable = false;
                    break;

                case ManualVolumeState.MixedChop:
                    isTradable = false;
                    break;

                default:
                    isTradable = false;
                    break;
            }

            string signal = "SKIP";

            if (isTradable && analysis.FttConfirmedBar.HasValue)
                signal = analysis.Snapshot.IsUpContainer ? "LONG" : "SHORT";

            state.LastContainerId = analysis.Snapshot.ContainerId;
            state.LastConfirmedBar = analysis.FttConfirmedBar;
            state.LastDirectionUp = analysis.Snapshot.IsUpContainer;
            state.LastSignal = signal;

            return signal;
        }
    }
	
	public readonly struct ManualSignalDecision
	{
	    public readonly string Signal;
	    public readonly string Transition;
	    public readonly ManualSignalTransition Phase;
	
	    public ManualSignalDecision(
	        string signal,
	        string transition,
	        ManualSignalTransition phase)
	    {
	        Signal = signal;
	        Transition = transition;
	        Phase = phase;
	    }
	}
}


