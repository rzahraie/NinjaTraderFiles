#region Using declarations
using System;
using System.Collections.Generic;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using APVA.Core;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xApvaDebugIndicator : Indicator
    {
		private ApvaAnalyzerState _state = new ApvaAnalyzerState();
		
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xApvaDebugIndicator";
                Description = "Temporary APVA debug indicator.";
                IsOverlay = true;
                Calculate = Calculate.OnBarClose;
            }
        }

		private string FormatContainer(string name, xApvaContainerCandidate c)
		{
		    if (c == null)
		        return name + ": none";
		
		    return name + ": " +
		        "Dir=" + c.Direction + " " +
		        "P1=" + c.P1.Index + "@" + c.P1.Price + " " +
		        "P2=" + c.P2.Index + "@" + c.P2.Price + " " +
		        "P3=" + c.P3.Index + "@" + c.P3.Price + " " +
		        "ValidP3=" + c.HasValidP3 + " " +
				"Score=" + c.Score.ToString("F2");
		}

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 25)
                return;

            var bars = new List<Bar>();
            var classified = new List<ClassifiedBar>();

            for (int barsAgo = 19; barsAgo >= 0; barsAgo--)
            {
                int absoluteIndex = CurrentBar - barsAgo;

                bars.Add(new Bar
                {
                    Index = absoluteIndex,
                    Time = Time[barsAgo],
                    Open = Open[barsAgo],
                    High = High[barsAgo],
                    Low = Low[barsAgo],
                    Close = Close[barsAgo],
                    Volume = Volume[barsAgo]
                });

                VolumeColor color =
                    Close[barsAgo] > Open[barsAgo] ? VolumeColor.Black :
                    Close[barsAgo] < Open[barsAgo] ? VolumeColor.Red :
                    VolumeColor.Neutral;

                classified.Add(new ClassifiedBar
                {
                    Index = absoluteIndex,
                    VolumeColor = color,
                    TwoBarType = TwoBarType.Unknown
                });
            }

           ApvaAnalysisResult result = xApvaAnalyzer.Analyze(
				    bars,
				    classified,
				    TickSize,
					_state);
			
			foreach (var evt in result.CompletedFttEvents)
			{
			    Print($"FTT_OUTCOME | Entry={evt.EntryBarIndex} | Dir={evt.Direction} | " +
				      $"Score={result.SelectedContainerScoreSnapshot:F2} | " +
				      $"DomSeq={result.HasDominanceSequence} | FailSeq={result.HasFailureSequence} | " +
				      $"SegDom={result.CurrentSegmentDominance} | " +
				      $"MFE5={evt.MaxFavorableExcursion5:F2} MAE5={evt.MaxAdverseExcursion5:F2} | " +
				      $"MFE10={evt.MaxFavorableExcursion10:F2} MAE10={evt.MaxAdverseExcursion10:F2} | " +
				      $"MFE20={evt.MaxFavorableExcursion20:F2} MAE20={evt.MaxAdverseExcursion20:F2}");
			}

			bool continuationFailed =
			    result.Container != null &&
			    result.Container.ExpectedContinuationFailed(
			        bars[bars.Count - 1],
			        TickSize);
			
			bool interesting =
			    continuationFailed ||
			    result.Ftt.IsCandidate ||
			    result.Ftt.IsConfirmed ||
			    result.CurrentSegmentDominance == DominanceState.CounterDominant ||
			    result.ContainerBias == DominanceState.CounterDominant ||
			    result.HasDominanceSequence ||
			    result.HasFailureSequence;
			
			if (!interesting)
			    return;

            Print("----- APVA DEBUG -----");
            Print("Bar: " + CurrentBar + " Time: " + Time[0]);
			Print("InferredDirection: " + xApvaDirectionInferer.InferDirection(bars));
			
            foreach (VolumeSegment seg in result.Segments)
            {
                Print(
                    seg.StartIndex + "-" + seg.EndIndex + " " +
                    seg.Color + " " +
                    seg.Direction + " " +
                    "AvgVol=" + seg.AverageVolume.ToString("F0") + " " +
                    "Rank=" + seg.Rank + " " +
                    "Phase=" + seg.Phase + " " +
                    "Dom=" + seg.Dominance);
            }

            
            Print("HasDominanceSequence: " + result.HasDominanceSequence);
            Print("HasFailureSequence: " + result.HasFailureSequence);
            Print("FTT Candidate: " + result.Ftt.IsCandidate);
            Print("FTT Confirmed: " + result.Ftt.IsConfirmed);
            Print("FTT Reason: " + result.Ftt.Reason);
			Print("FttDetectionAllowed: " + result.FttDetectionAllowed);
			Print("FttDetectionBlockReason: " + result.FttDetectionBlockReason);
			Print("CurrentSegmentDominance: " + result.CurrentSegmentDominance);
			Print("ContainerBias: " + result.ContainerBias);
			Print("RecentBias: " + result.RecentBias);
			Print(FormatContainer("Primary", _state.PrimaryContainer));
			Print(FormatContainer("Secondary", _state.SecondaryContainer));
			Print(FormatContainer("PendingSecondary", _state.PendingSecondaryContainer));
			Print("PendingSecondaryConfirmBars: " + _state.PendingSecondaryConfirmBars);
			
			string selected = "None";
			
			if (result.Container != null)
			{
			    if (ReferenceEquals(result.Container, _state.PrimaryContainer))
			        selected = "Primary";
			    else if (ReferenceEquals(result.Container, _state.SecondaryContainer))
			        selected = "Secondary";
			    else if (ReferenceEquals(result.Container, _state.PendingSecondaryContainer))
			        selected = "PendingSecondary";
			    else
			        selected = "UnknownReference";
			}
			
			Print("SelectedContainer: " + selected);
			Print("SelectedContainerScore: " +
			    (result.Container != null ? result.Container.Score.ToString("F2") : "none"));
			
			Print("PrimaryContainerScore: " +
			    (_state.PrimaryContainer != null ? _state.PrimaryContainer.Score.ToString("F2") : "none"));
			
			Print("SecondaryContainerScore: " +
			    (_state.SecondaryContainer != null ? _state.SecondaryContainer.Score.ToString("F2") : "none"));
			
			Print("SelectedContainerScoreSnapshot: " +
			    (!double.IsNaN(result.SelectedContainerScoreSnapshot)
			        ? result.SelectedContainerScoreSnapshot.ToString("F2")
			        : "none"));
			
			Print("PrimaryContainerScoreSnapshot: " +
			    (!double.IsNaN(result.PrimaryContainerScoreSnapshot)
			        ? result.PrimaryContainerScoreSnapshot.ToString("F2")
			        : "none"));
			
			Print("SecondaryContainerScoreSnapshot: " +
			    (!double.IsNaN(result.SecondaryContainerScoreSnapshot)
			        ? result.SecondaryContainerScoreSnapshot.ToString("F2")
			        : "none"));
			
			if (result.Container != null)
			{
				double ltlValue = result.Container.LTL.ValueAt(bars[bars.Count - 1].Index);
				Bar lastBar = bars[bars.Count - 1];
				
				Print("LTL@" + lastBar.Index + ": " + ltlValue);
				Print("LastHigh: " + lastBar.High + " LastClose: " + lastBar.Close);
			    Print("ContainerDirection: " + result.Container.Direction);
			    Print("P1: " + result.Container.P1.Index + " " + result.Container.P1.Price);
			    Print("P2: " + result.Container.P2.Index + " " + result.Container.P2.Price);
			    Print("P3: " + result.Container.P3.Index + " " + result.Container.P3.Price);
			    Print("HasValidP3: " + result.Container.HasValidP3);
				Print("FTT Kind: " + result.Ftt.Kind);
				Print("WarningDuration: " + result.WarningDuration);
				Print("ImminentFTT: " + result.ImminentFtt);
			    Print("ExpectedContinuationFailed: " + continuationFailed);
				Print("DistanceToLTL: " + result.DistanceToLtl);
				Print("DistanceDelta: " + result.DistanceToLtlDelta);
				Print("IneffectiveDominance: " + result.IneffectiveDominance);
				Print("BarsSinceLastFTT: " + (result.BarsSinceLastFtt == int.MaxValue ? "None": result.BarsSinceLastFtt.ToString()));
				Print("P2P3Distance: " + result.P2P3Distance);
				Print("IsWeakContainer: " + result.IsWeakContainer);
				Print("ContainerAgeBars: " + result.ContainerAgeBars);
				Print("IsMatureContainer: " + result.IsMatureContainer);
			}
			else
			{
			    Print("Container: none");
			}
			
		 	
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xApvaDebugIndicator[] cachexApvaDebugIndicator;
		public xApvaDebugIndicator xApvaDebugIndicator()
		{
			return xApvaDebugIndicator(Input);
		}

		public xApvaDebugIndicator xApvaDebugIndicator(ISeries<double> input)
		{
			if (cachexApvaDebugIndicator != null)
				for (int idx = 0; idx < cachexApvaDebugIndicator.Length; idx++)
					if (cachexApvaDebugIndicator[idx] != null &&  cachexApvaDebugIndicator[idx].EqualsInput(input))
						return cachexApvaDebugIndicator[idx];
			return CacheIndicator<xApvaDebugIndicator>(new xApvaDebugIndicator(), input, ref cachexApvaDebugIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xApvaDebugIndicator xApvaDebugIndicator()
		{
			return indicator.xApvaDebugIndicator(Input);
		}

		public Indicators.xApvaDebugIndicator xApvaDebugIndicator(ISeries<double> input )
		{
			return indicator.xApvaDebugIndicator(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xApvaDebugIndicator xApvaDebugIndicator()
		{
			return indicator.xApvaDebugIndicator(Input);
		}

		public Indicators.xApvaDebugIndicator xApvaDebugIndicator(ISeries<double> input )
		{
			return indicator.xApvaDebugIndicator(input);
		}
	}
}

#endregion
