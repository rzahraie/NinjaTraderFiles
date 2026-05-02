using System.Collections.Generic;

namespace APVA.Core
{
    public enum SwingPointType
    {
        Unknown,
        SwingHigh,
        SwingLow
    }

    public sealed class xApvaSwingPoint
    {
        public int Index { get; set; }
        public double Price { get; set; }
        public SwingPointType Type { get; set; } = SwingPointType.Unknown;
    }

    public static class xApvaSwingPointDetector
    {
        public static List<xApvaSwingPoint> Detect(
            IReadOnlyList<Bar> bars,
            int strength,
            double tickTolerance)
        {
            var swings = new List<xApvaSwingPoint>();

            if (bars == null || bars.Count == 0)
                return swings;

            if (strength < 1)
                strength = 1;

            for (int i = strength; i < bars.Count - strength; i++)
            {
                if (IsSwingHigh(bars, i, strength, tickTolerance))
                {
                    swings.Add(new xApvaSwingPoint
                    {
                        Index = bars[i].Index,
                        Price = bars[i].High,
                        Type = SwingPointType.SwingHigh
                    });
                }

                if (IsSwingLow(bars, i, strength, tickTolerance))
                {
                    swings.Add(new xApvaSwingPoint
                    {
                        Index = bars[i].Index,
                        Price = bars[i].Low,
                        Type = SwingPointType.SwingLow
                    });
                }
            }

            return swings;
        }

        private static bool IsSwingHigh(
            IReadOnlyList<Bar> bars,
            int index,
            int strength,
            double tickTolerance)
        {
            double candidateHigh = bars[index].High;

            for (int i = index - strength; i <= index + strength; i++)
            {
                if (i == index)
                    continue;

                if (bars[i].High > candidateHigh + tickTolerance)
                    return false;
            }

            return true;
        }

        private static bool IsSwingLow(
            IReadOnlyList<Bar> bars,
            int index,
            int strength,
            double tickTolerance)
        {
            double candidateLow = bars[index].Low;

            for (int i = index - strength; i <= index + strength; i++)
            {
                if (i == index)
                    continue;

                if (bars[i].Low < candidateLow - tickTolerance)
                    return false;
            }

            return true;
        }
    }
}