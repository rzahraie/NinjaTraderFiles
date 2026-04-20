using System;
using System.Collections.Generic;
using System.Linq;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaEngine2
    {
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
			    (inLong  && sig.Phase == SignalPhase.ShortValid) ||
			    (inShort && sig.Phase == SignalPhase.LongValid);
			
			bool oppositeStrongCandidate =
			    (inLong  && sig.Phase == SignalPhase.ShortCandidate && sig.Score >= p.OppositePressureStrongCandidateThreshold) ||
			    (inShort && sig.Phase == SignalPhase.LongCandidate  && sig.Score >= p.OppositePressureStrongCandidateThreshold);
			
			if (oppositeValid)
			{
			    s.OppositePressureBars = 2;
			    s.OppositePressureArmed = true;
			}
			else if (oppositeStrongCandidate)
			{
			    s.OppositePressureBars++;
			    s.OppositePressureArmed = true;
			}
			else
			{
			    s.OppositePressureBars = 0;
			    s.OppositePressureArmed = false;
			}
			
			inLong = s.CurrentPosition > 0;
			inShort = s.CurrentPosition < 0;
			
			bool alignedValid =
			    (inLong  && sig.Phase == SignalPhase.LongValid) ||
			    (inShort && sig.Phase == SignalPhase.ShortValid);
			
			bool degrading =
			    (inLong  && sig.Phase == SignalPhase.None && dir.Context != DirectionContext.Up) ||
			    (inShort && sig.Phase == SignalPhase.None && dir.Context != DirectionContext.Down);
			
			bool hardDegrading =
			    (inLong  && sig.Phase == SignalPhase.None && dir.Context != DirectionContext.Up) ||
			    (inShort && sig.Phase == SignalPhase.None && dir.Context != DirectionContext.Down);
			
			bool softOpposition =
			    (inLong  && sig.Phase == SignalPhase.ShortCandidate) ||
			    (inShort && sig.Phase == SignalPhase.LongCandidate);
			
			if (alignedValid)
			{
			    s.StableSignalBars++;
			    s.DegradingSignalBars = 0;
			}
			else if (hardDegrading)
			{
			    s.DegradingSignalBars += 1;
			    s.StableSignalBars = 0;
			}
			else if (softOpposition)
			{
			    s.DegradingSignalBars += 1;
			    s.StableSignalBars = 0;
			}
			else
			{
			    s.StableSignalBars = 0;
			}
			
			inLong = s.CurrentPosition > 0;
			inShort = s.CurrentPosition < 0;
			
			bool shockReverseToShort =
			    inLong &&
			    dir.Context == DirectionContext.Down &&
			    (sig.Phase == SignalPhase.ShortValid || sig.Phase == SignalPhase.ShortCandidate);
			
			bool shockReverseToLong =
			    inShort &&
			    dir.Context == DirectionContext.Up &&
			    (sig.Phase == SignalPhase.LongValid || sig.Phase == SignalPhase.LongCandidate);
			
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

            return true;
        }
    }
}

















