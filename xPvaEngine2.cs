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
            xPvaExecutionResult exe = executionEngine.Compute(s.CurrentPosition, sig);
			
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
