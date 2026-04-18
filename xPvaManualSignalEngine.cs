namespace NinjaTrader.NinjaScript.xPva.Engine
{
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

    public static class xPvaManualSignalEngine
    {
        public static ManualSignalDecision Evaluate(
            ManualContainerAnalysis analysis,
            ManualSignalState state)
        {
            bool isTradable = false;

            switch (analysis.VolumeState)
			{
			    case ManualVolumeState.DominantPulse:
			        isTradable = analysis.FttConfirmedBar.HasValue;
			        break;
			
			    case ManualVolumeState.BalancedAlternation:
			        isTradable = false;
			        break;
			
			    default:
			        isTradable = false;
			        break;
			}

			if (analysis.StructureState.HasValue &&
			    analysis.StructureState.Value == StructureState.Broken)
			{
			    isTradable = false;
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

           	bool wasTradable =
		    state.LastSignal == "LONG" || state.LastSignal == "SHORT";
		
			bool nowTradable = isTradable;
			
			if (analysis.StructureState.HasValue &&
			    analysis.StructureState.Value == StructureState.Broken)
			{
			    phase = ManualSignalTransition.Invalidated;
			}
			else if (sameContainer)
			{
			    if (!wasTradable && nowTradable)
			        phase = ManualSignalTransition.EarlyValid;
			    else if (wasTradable && nowTradable)
			        phase = ManualSignalTransition.StableValid;
			    else if (wasTradable && !nowTradable)
			        phase = ManualSignalTransition.Degrading;
			    else
			        phase = ManualSignalTransition.Unknown;
			}
			else
			{
			    if (nowTradable)
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

			System.Diagnostics.Debug.WriteLine(
   						 $"[ManualSignalEngineVersion] CORRECTED_LOGIC signal={signal} tradable={isTradable} phase={phase} struct={(analysis.StructureState.HasValue ? analysis.StructureState.Value.ToString() : "null")} volState={analysis.VolumeState}");
            
			return new ManualSignalDecision(signal, transition, phase);
        }
    }
}






