namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaDominanceEngine
    {
        private readonly xPvaEngineParameters p;

        public xPvaDominanceEngine(xPvaEngineParameters parameters)
        {
            p = parameters;
        }

        public xPvaDominanceResult Compute(in xPvaBarFeatures f, in xPvaDirectionResult dir)
        {
            if (dir.Context == DirectionContext.Unknown || dir.Context == DirectionContext.Neutral)
                return new xPvaDominanceResult(DominanceState.Unknown, 0.0);

            bool bodyOk = f.BodyToRange >= p.DominanceBodyToRangeMin;
            bool volOk = f.NormVolume >= p.DominanceNormVolumeMin;
            bool notContracting = f.VolumeBehavior != VolumeBehavior.Contracting;

            if (dir.Context == DirectionContext.Up)
            {
                bool aligned =
                    f.Polarity == PricePolarity.Black &&
                    f.CloseLocation >= p.DominanceCloseLocationMin &&
                    bodyOk &&
                    volOk &&
                    notContracting;

                return aligned
                    ? new xPvaDominanceResult(DominanceState.Dominant, 1.0)
                    : new xPvaDominanceResult(DominanceState.NonDominant, -1.0);
            }

            bool alignedDown =
                f.Polarity == PricePolarity.Red &&
                f.CloseLocation <= 1.0 - p.DominanceCloseLocationMin &&
                bodyOk &&
                volOk &&
                notContracting;

            return alignedDown
                ? new xPvaDominanceResult(DominanceState.Dominant, 1.0)
                : new xPvaDominanceResult(DominanceState.NonDominant, -1.0);
        }
    }
}