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
                xApvaDirectionInferer.InferDirection(bars),
                hasValidP3: false,
                expectedContinuationFailed: false);
			
			bool interesting =
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

            Print("CurrentDominance: " + result.CurrentDominance);
            Print("HasDominanceSequence: " + result.HasDominanceSequence);
            Print("HasFailureSequence: " + result.HasFailureSequence);
            Print("FTT Candidate: " + result.Ftt.IsCandidate);
            Print("FTT Confirmed: " + result.Ftt.IsConfirmed);
            Print("FTT Reason: " + result.Ftt.Reason);
			Print("CurrentSegmentDominance: " + result.CurrentSegmentDominance);
			Print("ContainerBias: " + result.ContainerBias);
			Print("RecentBias: " + result.RecentBias);
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
