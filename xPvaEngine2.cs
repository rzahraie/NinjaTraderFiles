using System;
using System.Collections.Generic;
using System.Linq;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
	

    public sealed class xPvaEngine2
    {
		private sealed class PendingReview
		{
		    public int BarIndex;
		    public ExecutionIntent Intent;
		    public string Reason = string.Empty;
		    public double EntryClose;
		}
		
		private readonly List<PendingReview> pendingReviews = new List<PendingReview>();
		
        private readonly xPvaEngineParameters p;
        private readonly xPvaRuntimeState s;

        private readonly xPvaBarFeatureEngine barEngine;
        private readonly xPvaDirectionEngine directionEngine;
        private readonly xPvaDominanceEngine dominanceEngine;
        private readonly xPvaSequenceEngine sequenceEngine;
        private readonly xPvaImbalanceEngine imbalanceEngine;
        private readonly xPvaLateralEngine lateralEngine;
        private readonly xPvaSignalEngine2 signalEngine;
        private readonly xPvaExecutionEngine2 executionEngine;

        private BarSnapshot? lastBar;

        public xPvaEngine2(xPvaEngineParameters parameters)
        {
            p = parameters ?? throw new ArgumentNullException(nameof(parameters));
            s = new xPvaRuntimeState();

            barEngine = new xPvaBarFeatureEngine(p);
            directionEngine = new xPvaDirectionEngine(p);
            dominanceEngine = new xPvaDominanceEngine(p);
            sequenceEngine = new xPvaSequenceEngine();
            imbalanceEngine = new xPvaImbalanceEngine();
            lateralEngine = new xPvaLateralEngine(p);
            signalEngine = new xPvaSignalEngine2(p);
            executionEngine = new xPvaExecutionEngine2();
        }

        public xPvaRuntimeState State => s;

        public bool Step(in BarSnapshot cur, double tickSize)
        {
            if (!lastBar.HasValue)
            {
                lastBar = cur;
                return false;
            }

            var prev = lastBar.Value;
            xPvaBarFeatures f = barEngine.Compute(cur, prev, tickSize);
            lastBar = cur;

            s.FeatureWindow.Enqueue(f);
            while (s.FeatureWindow.Count > Math.Max(p.DirectionLookback, p.ImbalanceLookback))
                s.FeatureWindow.Dequeue();

            var featureList = s.FeatureWindow.ToList();

            xPvaDirectionResult dir = directionEngine.Compute(
                featureList.Skip(Math.Max(0, featureList.Count - p.DirectionLookback)).ToList(),
                tickSize);

            xPvaDominanceResult dom = dominanceEngine.Compute(f, dir);

            s.DominanceWindow.Enqueue(dom.State);
            while (s.DominanceWindow.Count > p.FlipLookback)
                s.DominanceWindow.Dequeue();

            s.PolarityWindow.Enqueue(f.Polarity);
            while (s.PolarityWindow.Count > p.FlipLookback)
                s.PolarityWindow.Dequeue();

            xPvaSequenceStats seq = sequenceEngine.Compute(
                s.PolarityWindow.ToList(),
                s.DominanceWindow.ToList());

            var imbFeatures = featureList.Skip(Math.Max(0, featureList.Count - p.ImbalanceLookback)).ToList();
            var imbDominance = s.DominanceWindow.Skip(Math.Max(0, s.DominanceWindow.Count - imbFeatures.Count)).ToList();
            xPvaImbalanceResult imb = imbalanceEngine.Compute(imbFeatures, imbDominance);

            xPvaLateralResult lat = lateralEngine.Compute(s, featureList, imb, tickSize);
            xPvaSignalResult sig = signalEngine.Compute(dir, dom, seq, imb, lat);
			
			bool inLong = s.CurrentPosition > 0;
			bool inShort = s.CurrentPosition < 0;
			
			bool oppositeValid =
			    (inLong && sig.Phase == SignalPhase.ShortValid) ||
			    (inShort && sig.Phase == SignalPhase.LongValid);
			
			bool oppositeCandidate =
			    (inLong && sig.Phase == SignalPhase.ShortCandidate) ||
			    (inShort && sig.Phase == SignalPhase.LongCandidate);
			
			bool oppositeStrongCandidate =
			    (inLong && sig.Phase == SignalPhase.ShortCandidate &&
			     sig.Score >= p.OppositePressureStrongCandidateThreshold) ||
			    (inShort && sig.Phase == SignalPhase.LongCandidate &&
			     sig.Score >= p.OppositePressureStrongCandidateThreshold);
			
			if (oppositeValid)
			{
			    s.OppositePressureBars = Math.Max(s.OppositePressureBars + 1, 2);
			    s.OppositePressureArmed = true;
			}
			else if (oppositeStrongCandidate)
			{
			    s.OppositePressureBars += 1;
			    s.OppositePressureArmed = s.OppositePressureBars >= 2;
			}
			else if (oppositeCandidate)
			{
			    s.OppositePressureBars += 1;
			    s.OppositePressureArmed = s.OppositePressureBars >= 2;
			}
			else
			{
			    s.OppositePressureBars = Math.Max(0, s.OppositePressureBars - 1);
			    s.OppositePressureArmed = s.OppositePressureBars >= 2;
			}
			
			inLong = s.CurrentPosition > 0;
			inShort = s.CurrentPosition < 0;
			
			bool alignedValid =
			    (inLong && sig.Phase == SignalPhase.LongValid) ||
			    (inShort && sig.Phase == SignalPhase.ShortValid);
			
			oppositeCandidate =
			    (inLong && sig.Phase == SignalPhase.ShortCandidate) ||
			    (inShort && sig.Phase == SignalPhase.LongCandidate);
			
			oppositeValid =
			    (inLong && sig.Phase == SignalPhase.ShortValid) ||
			    (inShort && sig.Phase == SignalPhase.LongValid);
			
			bool contextAgainstPosition =
			    (inLong && dir.Context == DirectionContext.Down) ||
			    (inShort && dir.Context == DirectionContext.Up);
			
			if (alignedValid)
			{
			    s.StableSignalBars = 0;
			    s.DegradingSignalBars = 0;
			}
			else if (oppositeValid)
			{
			    s.DegradingSignalBars += 2;
			    s.StableSignalBars = 0;
			}
			else if (oppositeCandidate || contextAgainstPosition)
			{
			    s.DegradingSignalBars += 1;
			    s.StableSignalBars = 0;
			}
			else
			{
			    s.DegradingSignalBars = Math.Max(0, s.DegradingSignalBars - 1);
			    s.StableSignalBars = 0;
			}
			
			inLong = s.CurrentPosition > 0;
			inShort = s.CurrentPosition < 0;
			
			bool shockReverseToShort =
			    inLong &&
			    sig.Phase == SignalPhase.ShortCandidate &&
			    sig.Score >= 0.60 &&
			    s.OppositePressureBars >= 2;
			
			bool shockReverseToLong =
			    inShort &&
			    sig.Phase == SignalPhase.LongCandidate &&
			    sig.Score >= 0.60 &&
			    s.OppositePressureBars >= 2;
			
			s.ShockReversalArmed = false;
			s.ShockReason = string.Empty;
			
			s.ShockReason = $"DBG inLong={inLong} inShort={inShort} dir={dir.Context} sig={sig.Phase}";

			if (p.EnableShockReversal)
			{
			    if (shockReverseToShort)
			    {
			        s.ShockReversalArmed = true;
			        s.ShockReason = "shock_reverse_to_short";
			    }
			    else if (shockReverseToLong)
			    {
			        s.ShockReversalArmed = true;
			        s.ShockReason = "shock_reverse_to_long";
			    }
			}
			
			int preDeg = s.DegradingSignalBars;
			int preOpp = s.OppositePressureBars;
			bool preArm = s.OppositePressureArmed;
			bool preShock = s.ShockReversalArmed;
			
            xPvaExecutionResult exe = executionEngine.Compute(
					    s.CurrentPosition,
					    sig,
					    s.DegradingSignalBars,
					    p.MaxNoneBarsInPosition,
					    p.EnableOppositePressureOverride,
					    s.OppositePressureArmed,
					    s.OppositePressureBars,
						s.ShockReversalArmed,
						s.ShockReason);
			
			switch (exe.Intent)
			{
			    case ExecutionIntent.EnterLong:
			    case ExecutionIntent.HoldLong:
			    case ExecutionIntent.ReverseToLong:
			        s.CurrentPosition = 1;
			        break;
			
			    case ExecutionIntent.EnterShort:
			    case ExecutionIntent.HoldShort:
			    case ExecutionIntent.ReverseToShort:
			        s.CurrentPosition = -1;
			        break;
			
			    case ExecutionIntent.ExitLong:
			    case ExecutionIntent.ExitShort:
			    case ExecutionIntent.StandAside:
			        s.CurrentPosition = 0;
			        break;
			}
			
			switch (exe.Intent)
			{
			    case ExecutionIntent.EnterLong:
			    case ExecutionIntent.EnterShort:
			    case ExecutionIntent.ReverseToLong:
			    case ExecutionIntent.ReverseToShort:
			    case ExecutionIntent.ExitLong:
			    case ExecutionIntent.ExitShort:
			        s.StableSignalBars = 0;
			        s.DegradingSignalBars = 0;
			        s.OppositePressureBars = 0;
			        s.OppositePressureArmed = false;
			        break;
			}

            s.LastBarFeatures = f;
            s.LastDirection = dir;
            s.LastDominance = dom;
            s.LastSequenceStats = seq;
            s.LastImbalance = imb;
            s.LastLateral = lat;
            s.LastSignal = sig;
            s.LastExecution = exe;
			

            if (lat.State == LateralStateKind.Active)
            {
                s.ActiveLateralStartBar = lat.StartBarIndex;
                s.ActiveLateralHigh = lat.High;
                s.ActiveLateralLow = lat.Low;
            }
			
			if (exe.Intent != ExecutionIntent.HoldLong &&
			    exe.Intent != ExecutionIntent.HoldShort &&
			    exe.Intent != ExecutionIntent.StandAside &&
			    exe.Intent != ExecutionIntent.None)
			{
			    pendingReviews.Add(new PendingReview
			    {
			        BarIndex = cur.Index,
			        Intent = exe.Intent,
			        Reason = exe.Reason,
			        EntryClose = cur.C
			    });
			}
			
			for (int i = pendingReviews.Count - 1; i >= 0; i--)
			{
			    var r = pendingReviews[i];
			    int age = cur.Index - r.BarIndex;
			
			    if (age == 3 || age == 5 || age == 10)
			    {
			        double delta = cur.C - r.EntryClose;
			
			        System.Diagnostics.Debug.WriteLine(
			            $"REVIEW srcBar={r.BarIndex} age={age} " +
			            $"intent={r.Intent} reason={r.Reason} " +
			            $"entryClose={r.EntryClose:F2} nowClose={cur.C:F2} delta={delta:F2}");
			    }
			
			    if (age > 10)
			        pendingReviews.RemoveAt(i);
			}

			System.Diagnostics.Debug.WriteLine(
			    $"BAR={cur.Index} POS={s.CurrentPosition} " +
			    $"DIR={dir.Context} SIG={sig.Phase} SCORE={sig.Score:F2} " +
			    $"DEG={preDeg} OPP={preOpp} ARM={preArm} SHOCK={preShock} " +
			    $"EXE={exe.Intent} RSN={exe.Reason}");

            return true;
        }
    }
}



































