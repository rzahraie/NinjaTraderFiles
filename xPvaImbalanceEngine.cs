using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaImbalanceEngine
    {
        public xPvaImbalanceResult Compute(
            IReadOnlyList<xPvaBarFeatures> features,
            IReadOnlyList<DominanceState> dominanceStates)
        {
            if (features == null || dominanceStates == null || features.Count == 0 || features.Count != dominanceStates.Count)
                return new xPvaImbalanceResult(0.0, 0.0, 0.0, 0.0);

            double total = 0.0;
            double dom = 0.0;
            double nd = 0.0;

            for (int i = 0; i < features.Count; i++)
            {
                double weight = Math.Abs(features[i].BodyDelta) * features[i].NormVolume;
                if (dominanceStates[i] == DominanceState.Dominant)
                {
                    dom += weight;
                    total += weight;
                }
                else if (dominanceStates[i] == DominanceState.NonDominant)
                {
                    nd += weight;
                    total += weight;
                }
            }

            double imbalance = total > 0.0 ? (dom - nd) / total : 0.0;
            return new xPvaImbalanceResult(imbalance, total, dom, nd);
        }
    }
}