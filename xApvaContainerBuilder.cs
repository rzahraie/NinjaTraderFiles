using System.Collections.Generic;

namespace APVA.Core
{
    public static class xApvaContainerBuilder
    {
        public static xApvaContainerCandidate BuildFromSwings(
            IReadOnlyList<Bar> bars,
            int swingStrength,
            double tickTolerance)
        {
            if (bars == null || bars.Count < 6)
                return null;

            List<xApvaSwingPoint> swings =
                xApvaSwingPointDetector.Detect(
                    bars,
                    swingStrength,
                    tickTolerance);

            if (swings.Count < 3)
                return null;

            xApvaContainerCandidate up = TryBuildUpContainer(swings);
            xApvaContainerCandidate down = TryBuildDownContainer(swings);

            if (up != null && down == null)
                return up;

            if (down != null && up == null)
                return down;

            if (up != null && down != null)
            {
                int upAge = up.P3.Index;
                int downAge = down.P3.Index;

                return upAge >= downAge ? up : down;
            }

            return null;
        }

        private static xApvaContainerCandidate TryBuildUpContainer(
            IReadOnlyList<xApvaSwingPoint> swings)
        {
            xApvaSwingPoint p1 = null;
            xApvaSwingPoint p2 = null;
            xApvaSwingPoint p3 = null;

            for (int i = swings.Count - 1; i >= 0; i--)
            {
                if (p3 == null && swings[i].Type == SwingPointType.SwingLow)
                {
                    p3 = swings[i];
                    continue;
                }

                if (p3 != null && p2 == null &&
                    swings[i].Type == SwingPointType.SwingHigh &&
                    swings[i].Index < p3.Index)
                {
                    p2 = swings[i];
                    continue;
                }

                if (p3 != null && p2 != null && p1 == null &&
                    swings[i].Type == SwingPointType.SwingLow &&
                    swings[i].Index < p2.Index)
                {
                    p1 = swings[i];
                    break;
                }
            }

            if (p1 == null || p2 == null || p3 == null)
                return null;

            if (p3.Price < p1.Price)
                return null;

            var candidate = new xApvaContainerCandidate
            {
                Direction = ContainerDirection.Up,
                P1 = new xApvaPoint(p1.Index, p1.Price),
                P2 = new xApvaPoint(p2.Index, p2.Price),
                P3 = new xApvaPoint(p3.Index, p3.Price)
            };

            candidate.RTL = new xApvaLine(candidate.P1, candidate.P3);

            double rtlAtP2 = candidate.RTL.ValueAt(candidate.P2.Index);
            double offset = candidate.P2.Price - rtlAtP2;

            candidate.LTL = new xApvaLine(
                new xApvaPoint(candidate.P1.Index, candidate.P1.Price + offset),
                new xApvaPoint(candidate.P3.Index, candidate.P3.Price + offset));

            return candidate;
        }

        private static xApvaContainerCandidate TryBuildDownContainer(
            IReadOnlyList<xApvaSwingPoint> swings)
        {
            xApvaSwingPoint p1 = null;
            xApvaSwingPoint p2 = null;
            xApvaSwingPoint p3 = null;

            for (int i = swings.Count - 1; i >= 0; i--)
            {
                if (p3 == null && swings[i].Type == SwingPointType.SwingHigh)
                {
                    p3 = swings[i];
                    continue;
                }

                if (p3 != null && p2 == null &&
                    swings[i].Type == SwingPointType.SwingLow &&
                    swings[i].Index < p3.Index)
                {
                    p2 = swings[i];
                    continue;
                }

                if (p3 != null && p2 != null && p1 == null &&
                    swings[i].Type == SwingPointType.SwingHigh &&
                    swings[i].Index < p2.Index)
                {
                    p1 = swings[i];
                    break;
                }
            }

            if (p1 == null || p2 == null || p3 == null)
                return null;

            if (p3.Price > p1.Price)
                return null;

            var candidate = new xApvaContainerCandidate
            {
                Direction = ContainerDirection.Down,
                P1 = new xApvaPoint(p1.Index, p1.Price),
                P2 = new xApvaPoint(p2.Index, p2.Price),
                P3 = new xApvaPoint(p3.Index, p3.Price)
            };

            candidate.RTL = new xApvaLine(candidate.P1, candidate.P3);

            double rtlAtP2 = candidate.RTL.ValueAt(candidate.P2.Index);
            double offset = candidate.P2.Price - rtlAtP2;

            candidate.LTL = new xApvaLine(
                new xApvaPoint(candidate.P1.Index, candidate.P1.Price + offset),
                new xApvaPoint(candidate.P3.Index, candidate.P3.Price + offset));

            return candidate;
        }
    }
}