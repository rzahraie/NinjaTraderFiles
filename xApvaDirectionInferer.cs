using System.Collections.Generic;

namespace APVA.Core
{
    public static class xApvaDirectionInferer
    {
        public static ContainerDirection InferDirection(IReadOnlyList<Bar> bars)
        {
            if (bars == null || bars.Count < 2)
                return ContainerDirection.Unknown;

            Bar first = bars[0];
            Bar last = bars[bars.Count - 1];

            double firstMid = (first.High + first.Low) / 2.0;
            double lastMid = (last.High + last.Low) / 2.0;

            double range = GetTotalRange(bars);

            if (range <= 0.0)
                return ContainerDirection.Unknown;

            double netMove = lastMid - firstMid;
            double threshold = range * 0.15;

            if (netMove > threshold)
                return ContainerDirection.Up;

            if (netMove < -threshold)
                return ContainerDirection.Down;

            return ContainerDirection.Unknown;
        }

        private static double GetTotalRange(IReadOnlyList<Bar> bars)
        {
            double high = double.MinValue;
            double low = double.MaxValue;

            foreach (Bar bar in bars)
            {
                if (bar.High > high)
                    high = bar.High;

                if (bar.Low < low)
                    low = bar.Low;
            }

            return high - low;
        }
    }
}