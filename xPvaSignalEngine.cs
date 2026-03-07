#region Using declarations
using System;
using System.Linq;
using System.Collections.Generic;
#endregion

namespace NinjaTrader.NinjaScript.xPva
{
    public sealed class xPvaSignalEngine
    {
        public int LookbackBars { get; set; } = 60;
        public int FreshLabelBars { get; set; } = 40;   // label must be recent
        public int MinScoreToSignal { get; set; } = 10;

        // Volume spike heuristic
        public double VolSpikeMultiplier { get; set; } = 1.6;

        // Risk model
        public double StopAtrMult { get; set; } = 1.2;
        public double TargetRMult { get; set; } = 2.0;

        public xPvaSignal Evaluate(
		    int currentBar,
		    Func<int, double> close,
		    Func<int, double> high,
		    Func<int, double> low,
		    Func<int, double> volume,
		    Func<double> atr,
		    Func<DateTime, int> getBarIndexByChartTime,
		    xPvaDataset dataset)
		{
		    var sig = new xPvaSignal { Type = xPvaSignalType.None };
		
		    // ---------- basic dataset sanity ----------
		    if (dataset?.Labels == null || dataset.Labels.Count == 0)
		        return sig;
		
		    // ---------- pick latest finalized BBT (by UTC) ----------
		    // Why UTC? Your recorder writes FinalizedAt.TimeUtc, which is consistent across loads.
		    xPvaLabel bbt = dataset.Labels
				    .Where(l =>
				        l != null &&
				        l.Type == xPvaLabelType.BBT &&
				        l.Anchors != null &&
				        l.Anchors.Count >= 2)
				    .OrderByDescending(l =>
				        (l.FinalizedAt != null ? l.FinalizedAt.TimeUtc :
				         l.CreatedAt != null ? l.CreatedAt.TimeUtc :
				         DateTime.MinValue))
				    .FirstOrDefault();
		
		    if (bbt == null)
		        return sig;
		
		    // ---------- reject dir mismatch ----------
		    if (bbt.Tags != null && bbt.Tags.Contains("dir_mismatch"))
		        return sig;
		
		    // ---------- determine signal type from label direction ----------
		    xPvaSignalType type =
		        (bbt.Direction == xPvaDirection.Up) ? xPvaSignalType.LongEntry :
		        (bbt.Direction == xPvaDirection.Down) ? xPvaSignalType.ShortEntry :
		        xPvaSignalType.None;
		
		    if (type == xPvaSignalType.None)
		        return sig;
		
		    // ---------- freshness gate based on END anchor ChartTime ----------
		    // We use ChartTime because BarIndex from the JSON is not stable across reloads/history differences.
		    xPvaAnchor endAnchor = bbt.Anchors.FirstOrDefault(a => a.Role == xPvaAnchorRole.End);
		    if (endAnchor == null)
		        return sig;
		
		    // ChartTime is stored as a DateTime without timezone kind; treat it consistently as "chart local time".
		    if (!endAnchor.ChartTime.HasValue)
			    return sig;
			
			DateTime endChartTime = endAnchor.ChartTime.Value;
		
		    int endIdx = getBarIndexByChartTime(endChartTime);
		    if (endIdx < 0)
		        return sig;
		
		    int ageBars = currentBar - endIdx;
		    if (ageBars < 0 || ageBars > FreshLabelBars)
		        return sig;
		
		    // ---------- PV confirmation scoring ----------
		    int score = 0;
		    var reasons = new List<string>();
		
		    // Base score for having a fresh BBT
		    score += 40;
		    reasons.Add("bbt_fresh");
		
		    // Need enough history for vol average
		    int n = Math.Min(LookbackBars, currentBar);
		    if (n < 10)
		        return sig;
		
		    // Volume spike
		    double volNow = volume(0);
		    double volSum = 0;
		    for (int i = 1; i <= n; i++)
		        volSum += volume(i);
		
		    double volAvg = volSum / n;
		
		    if (volAvg > 0 && volNow >= volAvg * VolSpikeMultiplier)
		    {
		        score += 25;
		        reasons.Add("vol_spike");
		    }
		
		    // Range sanity vs ATR
		    double a = atr();
		    if (a <= 0)
		        return sig;
		
		    double rng = high(0) - low(0);
		    if (rng >= 0.5 * a)
		    {
		        score += 10;
		        reasons.Add("range_ok");
		    }
		
		    // ---------- score gate ----------
		    if (score < MinScoreToSignal)
		        return sig;
		
		    // ---------- risk model ----------
		    double entry = close(0);
		    double stopDist = StopAtrMult * a;
		    if (stopDist <= 0)
		        return sig;
		
		    double stop = (type == xPvaSignalType.LongEntry) ? entry - stopDist : entry + stopDist;
		    double target = (type == xPvaSignalType.LongEntry) ? entry + TargetRMult * stopDist : entry - TargetRMult * stopDist;
		
		    // ---------- finalize signal ----------
		    sig.Type = type;
		    sig.Score = Math.Min(100, score);
		    sig.StopPrice = stop;
		    sig.TargetPrice = target;
		    sig.Reasons = reasons;
			
			double boundary = endAnchor.Price;

			if (type == xPvaSignalType.LongEntry)
			{
			    if (close(0) <= boundary)
			        return sig; // no breakout
			}
			else if (type == xPvaSignalType.ShortEntry)
			{
			    if (close(0) >= boundary)
			        return sig; // no breakdown
			}
		
		    return sig;
		}
    }
}








