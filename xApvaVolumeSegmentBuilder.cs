using System;
using System.Collections.Generic;
using System.Linq;

namespace APVA.Core
{
    public static class xApvaVolumeSegmentBuilder
    {
        public static List<VolumeSegment> BuildSegments(
            IReadOnlyList<Bar> bars,
            IReadOnlyList<ClassifiedBar> classifiedBars)
        {
            var segments = new List<VolumeSegment>();

            if (bars == null || classifiedBars == null)
                return segments;

            if (bars.Count == 0 || classifiedBars.Count == 0)
                return segments;

            if (bars.Count != classifiedBars.Count)
                throw new ArgumentException("bars and classifiedBars must have the same count.");

            VolumeSegment current = CreateSegment(bars[0], classifiedBars[0]);

            for (int i = 1; i < bars.Count; i++)
            {
                VolumeColor nextColor = classifiedBars[i].VolumeColor;
                SegmentDirection nextDirection = GetDirection(bars[i]);

                bool split =
                    nextColor != current.Color ||
                    nextDirection != current.Direction;

                if (split)
                {
                    segments.Add(current);
                    current = CreateSegment(bars[i], classifiedBars[i]);
                }
                else
                {
                    ExtendSegment(current, bars[i]);
                }
            }

            segments.Add(current);
            AssignVolumeRanks(segments);

            return segments;
        }

        private static VolumeSegment CreateSegment(
		    Bar bar,
		    ClassifiedBar classifiedBar)
		{
		    return new VolumeSegment
		    {
		        StartIndex = bar.Index,
		        EndIndex = bar.Index,
		        Color = classifiedBar.VolumeColor,
		        Direction = GetDirection(bar),
		        Open = bar.Open,
		        High = bar.High,
		        Low = bar.Low,
		        Close = bar.Close,
		        TotalVolume = bar.Volume
		    };
		}

        private static void ExtendSegment(
		    VolumeSegment segment,
		    Bar bar)
		{
		    segment.EndIndex = bar.Index;
		    segment.High = Math.Max(segment.High, bar.High);
		    segment.Low = Math.Min(segment.Low, bar.Low);
		    segment.Close = bar.Close;
		    segment.TotalVolume += bar.Volume;
		
		    segment.Direction =
		        segment.Close > segment.Open ? SegmentDirection.Up :
		        segment.Close < segment.Open ? SegmentDirection.Down :
		        SegmentDirection.Sideways;
		}

        private static SegmentDirection GetDirection(Bar bar)
        {
            if (bar.Close > bar.Open)
                return SegmentDirection.Up;

            if (bar.Close < bar.Open)
                return SegmentDirection.Down;

            return SegmentDirection.Sideways;
        }

        private static void AssignVolumeRanks(List<VolumeSegment> segments)
        {
            if (segments == null || segments.Count == 0)
                return;

            var ordered = segments
                .Select(s => s.AverageVolume)
                .OrderBy(v => v)
                .ToList();

            foreach (VolumeSegment segment in segments)
            {
                double percentile = PercentileRank(ordered, segment.AverageVolume);

                segment.Rank =
                    percentile < 0.20 ? VolumeRank.Low :
                    percentile < 0.60 ? VolumeRank.Normal :
                    percentile < 0.85 ? VolumeRank.Elevated :
                    percentile < 0.95 ? VolumeRank.Peak :
                                         VolumeRank.Climax;
            }
        }

        private static double PercentileRank(List<double> sortedValues, double value)
        {
            if (sortedValues == null || sortedValues.Count == 0)
                return 0.0;

            int countBelowOrEqual = sortedValues.Count(v => v <= value);
            return (double)countBelowOrEqual / sortedValues.Count;
        }
    }
}
