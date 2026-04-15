using System.Collections.Generic;
using System.Linq;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaSequenceEngine
    {
        public xPvaSequenceStats Compute(
            IReadOnlyList<PricePolarity> polarities,
            IReadOnlyList<DominanceState> dominanceStates)
        {
            int polarityRun = ComputeRunLength(polarities);
            int dominanceRun = ComputeRunLength(dominanceStates);

            int flipCount = ComputeFlipCount(dominanceStates);
            int maxDom = ComputeMaxRun(dominanceStates, DominanceState.Dominant);
            int maxNd = ComputeMaxRun(dominanceStates, DominanceState.NonDominant);

            return new xPvaSequenceStats(polarityRun, dominanceRun, flipCount, maxDom, maxNd);
        }

        private static int ComputeRunLength<T>(IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
                return 0;

            T last = items[items.Count - 1];
            int n = 1;
            for (int i = items.Count - 2; i >= 0; i--)
            {
                if (!EqualityComparer<T>.Default.Equals(items[i], last))
                    break;
                n++;
            }
            return n;
        }

        private static int ComputeFlipCount(IReadOnlyList<DominanceState> states)
        {
            if (states == null || states.Count == 0)
                return 0;

            var filtered = states.Where(s => s != DominanceState.Unknown).ToList();
            if (filtered.Count <= 1)
                return 0;

            int flips = 0;
            DominanceState prev = filtered[0];
            for (int i = 1; i < filtered.Count; i++)
            {
                if (filtered[i] != prev)
                {
                    flips++;
                    prev = filtered[i];
                }
            }
            return flips;
        }

        private static int ComputeMaxRun(IReadOnlyList<DominanceState> states, DominanceState target)
        {
            int best = 0, cur = 0;
            foreach (var s in states)
            {
                if (s == target)
                {
                    cur++;
                    if (cur > best) best = cur;
                }
                else
                {
                    cur = 0;
                }
            }
            return best;
        }
    }
}