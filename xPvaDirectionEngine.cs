using System;
using System.Collections.Generic;
using System.Linq;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaDirectionEngine
    {
        private readonly xPvaEngineParameters p;

        public xPvaDirectionEngine(xPvaEngineParameters parameters)
        {
            p = parameters;
        }

        public xPvaDirectionResult Compute(IReadOnlyCollection<xPvaBarFeatures> window, double tickSize)
        {
            if (window == null || window.Count == 0)
                return new xPvaDirectionResult(DirectionContext.Unknown, 0.0);

            double eps = Math.Max(tickSize * p.EpsilonTicks, 1e-12);

            double score = 0.0;
            foreach (xPvaBarFeatures f in window)
            {
                int sign = xPvaMath.SignEps(f.BodyDelta, eps);
                score += sign * f.BodyToRange * f.NormVolume;
            }

            DirectionContext ctx =
                score >= p.DirectionThreshold ? DirectionContext.Up :
                score <= -p.DirectionThreshold ? DirectionContext.Down :
                DirectionContext.Neutral;

            return new xPvaDirectionResult(ctx, score);
        }
    }
}