using System;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public readonly struct xPvaBarFeatures
    {
        public readonly int BarIndex;
        public readonly DateTime TimeUtc;

        public readonly double Open;
        public readonly double High;
        public readonly double Low;
        public readonly double Close;
        public readonly long Volume;

        public readonly NinjaTrader.NinjaScript.xPva.Engine.PriceCase PriceCase;
        public readonly PricePolarity Polarity;

        public readonly double BodyDelta;
        public readonly double BodyAbs;
        public readonly double Range;
        public readonly double TrueRange;
        public readonly double CloseLocation;
        public readonly double BodyToRange;
        public readonly double SpreadEfficiency;

        public readonly double NormVolume;
        public readonly double VolumeDelta;
        public readonly VolumeBehavior VolumeBehavior;

        public xPvaBarFeatures(
            int barIndex,
            DateTime timeUtc,
            double open,
            double high,
            double low,
            double close,
            long volume,
            NinjaTrader.NinjaScript.xPva.Engine.PriceCase priceCase,
            PricePolarity polarity,
            double bodyDelta,
            double bodyAbs,
            double range,
            double trueRange,
            double closeLocation,
            double bodyToRange,
            double spreadEfficiency,
            double normVolume,
            double volumeDelta,
            VolumeBehavior volumeBehavior)
        {
            BarIndex = barIndex;
            TimeUtc = timeUtc;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            PriceCase = priceCase;
            Polarity = polarity;
            BodyDelta = bodyDelta;
            BodyAbs = bodyAbs;
            Range = range;
            TrueRange = trueRange;
            CloseLocation = closeLocation;
            BodyToRange = bodyToRange;
            SpreadEfficiency = spreadEfficiency;
            NormVolume = normVolume;
            VolumeDelta = volumeDelta;
            VolumeBehavior = volumeBehavior;
        }
    }
}