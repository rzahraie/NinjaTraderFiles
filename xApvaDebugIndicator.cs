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
                ContainerDirection.Up,
                hasValidP3: true,
                expectedContinuationFailed: true);

            Print("----- APVA DEBUG -----");
            Print("Bar: " + CurrentBar + " Time: " + Time[0]);

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
        }
    }
}