using System;
using System.Collections.Generic;
using System.Linq;
using NinjaTrader.NinjaScript.xPva.Engine;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaBarFeatureEngine
    {
        private readonly xPvaEngineParameters p;
        private readonly Queue<long> recentVolumes = new Queue<long>();

        public xPvaBarFeatureEngine(xPvaEngineParameters parameters)
        {
            p = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public xPvaBarFeatures Compute(
            in BarSnapshot cur,
            in BarSnapshot prev,
            double tickSize)
        {
            double eps = Math.Max(tickSize * p.EpsilonTicks, 1e-12);

            recentVolumes.Enqueue(cur.V);
            while (recentVolumes.Count > p.VolumeNormLookback)
                recentVolumes.Dequeue();

            double avgVol = recentVolumes.Count > 0 ? recentVolumes.Average(v => (double)v) : 0.0;
            double normVol = xPvaMath.SafeDiv(cur.V, avgVol, 1.0);

            double prevAvgVol = avgVol <= 0.0 ? 1.0 : avgVol;
            double prevNormVol = xPvaMath.SafeDiv(prev.V, prevAvgVol, 1.0);
            double volDelta = normVol - prevNormVol;

            VolumeBehavior volBehavior =
                volDelta >= p.VolumeExpandThreshold ? VolumeBehavior.Expanding :
                volDelta <= -p.VolumeContractThreshold ? VolumeBehavior.Contracting :
                VolumeBehavior.Flat;

            double bodyDelta = cur.C - cur.O;
            double bodyAbs = Math.Abs(bodyDelta);
            double range = Math.Max(cur.H - cur.L, 0.0);
            double trueRange = Math.Max(cur.H - cur.L,
                Math.Max(Math.Abs(cur.H - prev.C), Math.Abs(cur.L - prev.C)));

            double closeLocation = range <= eps
                ? 0.5
                : xPvaMath.Clamp01((cur.C - cur.L) / range);

            double bodyToRange = xPvaMath.SafeDiv(bodyAbs, range, 0.0);
            double spreadEfficiency = xPvaMath.SafeDiv(Math.Abs(cur.C - prev.C), range, 0.0);

            PricePolarity polarity =
                xPvaMath.Gt(cur.C, cur.O, eps) ? PricePolarity.Black :
                xPvaMath.Lt(cur.C, cur.O, eps) ? PricePolarity.Red :
                PricePolarity.Doji;

            // For now, reuse existing taxonomy. Later this should be upgraded to tick-aware comparisons internally.
            PriceCase priceCase = xPvaPriceCases.Classify(
                new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(cur.TimeUtc, cur.O, cur.H, cur.L, cur.C, cur.V, cur.Index),
                new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(prev.TimeUtc, prev.O, prev.H, prev.L, prev.C, prev.V, prev.Index));

            return new xPvaBarFeatures(
                cur.Index,
                cur.TimeUtc,
                cur.O,
                cur.H,
                cur.L,
                cur.C,
                cur.V,
                priceCase,
                polarity,
                bodyDelta,
                bodyAbs,
                range,
                trueRange,
                closeLocation,
                bodyToRange,
                spreadEfficiency,
                normVol,
                volDelta,
                volBehavior);
        }
    }
}