using System;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01BarFeatureEngine
    {
        private const double Epsilon = 1e-10;

        public ApvaBarFeatures Build(
            int barIndex,
            DateTime time,
            double open,
            double high,
            double low,
            double close,
            double volume,
            ApvaBarFeatures prior = null)
        {
            var f = new ApvaBarFeatures
            {
                BarIndex = barIndex,
                Time = time,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume
            };

            f.Range = Math.Max(0.0, high - low);
            f.Body = Math.Abs(close - open);
            f.BodyToRangeRatio = SafeDiv(f.Body, f.Range);

            f.VolumePolarity = ClassifyVolumePolarity(open, close);

            f.CloseEfficiencyUp = SafeDiv(close - low, f.Range);
            f.CloseEfficiencyDown = SafeDiv(high - close, f.Range);

            if (prior != null)
            {
                f.OverlapWithPrior = ComputeOverlap(high, low, prior.High, prior.Low);
                f.OverlapRatio = SafeDiv(f.OverlapWithPrior, f.Range);

                f.DirectionalResultUp = Math.Max(0.0, close - prior.Close);
                f.DirectionalResultDown = Math.Max(0.0, prior.Close - close);

                ClassifyTwoBarFeatures(f, prior);
            }
            else
            {
                f.PriceCase = "Unknown";
            }

            f.IsDoji = f.BodyToRangeRatio <= 0.10;

            return f;
        }

        private static ApvaVolumePolarity ClassifyVolumePolarity(double open, double close)
        {
            if (close > open)
                return ApvaVolumePolarity.Black;

            if (close < open)
                return ApvaVolumePolarity.Red;

            return ApvaVolumePolarity.Neutral;
        }

        private static void ClassifyTwoBarFeatures(ApvaBarFeatures f, ApvaBarFeatures p)
        {
            bool hh = f.High > p.High;
            bool lh = f.High < p.High;
            bool sh = NearlyEqual(f.High, p.High);

            bool hl = f.Low > p.Low;
            bool ll = f.Low < p.Low;
            bool sl = NearlyEqual(f.Low, p.Low);

            f.IsIB = f.High <= p.High && f.Low >= p.Low;
            f.IsOB = f.High > p.High && f.Low < p.Low;

            if (hh && hl)
                f.PriceCase = "HHHL";
            else if (lh && ll)
                f.PriceCase = "LLLH";
            else if (f.IsIB)
                f.PriceCase = "IB";
            else if (f.IsOB)
                f.PriceCase = "OB";
            else if (sh && hl)
                f.PriceCase = "SHHL";
            else if (hh && sl)
                f.PriceCase = "HHSL";
            else if (lh && sl)
                f.PriceCase = "LHSL";
            else if (sh && ll)
                f.PriceCase = "SHLL";
            else if (sh && sl)
                f.PriceCase = "SHSL";
            else
                f.PriceCase = "Mixed";

            f.IsStitch =
                (sh && !sl && !f.IsIB && !f.IsOB) ||
                (sl && !sh && !f.IsIB && !f.IsOB);

            f.IsReversal =
                (ll && f.Close > f.Open) ||
                (hh && f.Close < f.Open);
        }

        private static double ComputeOverlap(
            double high,
            double low,
            double priorHigh,
            double priorLow)
        {
            double upper = Math.Min(high, priorHigh);
            double lower = Math.Max(low, priorLow);

            return Math.Max(0.0, upper - lower);
        }

        private static bool NearlyEqual(double a, double b)
        {
            return Math.Abs(a - b) <= Epsilon;
        }

        private static double SafeDiv(double numerator, double denominator)
        {
            if (Math.Abs(denominator) <= Epsilon)
                return 0.0;

            return numerator / denominator;
        }
    }
}