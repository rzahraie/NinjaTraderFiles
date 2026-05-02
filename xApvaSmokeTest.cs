using System;
using System.Collections.Generic;

namespace APVA.Core
{
    public static class xApvaSmokeTest
    {
        public static void Run()
        {
            var bars = new List<Bar>
            {
                MakeBar(0, 10.00, 10.50,  9.90, 10.40, 1000),
                MakeBar(1, 10.40, 10.90, 10.30, 10.80, 1200),

                MakeBar(2, 10.80, 10.85, 10.20, 10.30, 600),

                MakeBar(3, 10.30, 11.00, 10.25, 10.90, 1500),
                MakeBar(4, 10.90, 11.20, 10.80, 11.10, 1800),

                MakeBar(5, 11.10, 11.15, 10.70, 10.80, 900),

                MakeBar(6, 10.80, 10.95, 10.50, 10.60, 1400)
            };

            var classified = new List<ClassifiedBar>
            {
                MakeClassified(0, VolumeColor.Black),
                MakeClassified(1, VolumeColor.Black),

                MakeClassified(2, VolumeColor.Red),

                MakeClassified(3, VolumeColor.Black),
                MakeClassified(4, VolumeColor.Black),

                MakeClassified(5, VolumeColor.Red),

                MakeClassified(6, VolumeColor.Red)
            };

            ApvaAnalysisResult result = xApvaAnalyzer.Analyze(
                bars,
                classified,
                ContainerDirection.Up,
                hasValidP3: true,
                expectedContinuationFailed: true);

            Console.WriteLine("Segments:");
            foreach (VolumeSegment segment in result.Segments)
            {
                Console.WriteLine(
                    $"{segment.StartIndex}-{segment.EndIndex} " +
                    $"{segment.Color} {segment.Direction} " +
                    $"Vol={segment.AverageVolume:F0} Rank={segment.Rank} " +
                    $"Phase={segment.Phase} Dom={segment.Dominance}");
            }

            Console.WriteLine();
            Console.WriteLine($"CurrentDominance: {result.CurrentDominance}");
            Console.WriteLine($"HasDominanceSequence: {result.HasDominanceSequence}");
            Console.WriteLine($"HasFailureSequence: {result.HasFailureSequence}");
            Console.WriteLine($"FTT Candidate: {result.Ftt.IsCandidate}");
            Console.WriteLine($"FTT Confirmed: {result.Ftt.IsConfirmed}");
            Console.WriteLine($"FTT Reason: {result.Ftt.Reason}");
        }

        private static Bar MakeBar(
            int index,
            double open,
            double high,
            double low,
            double close,
            double volume)
        {
            return new Bar
            {
                Index = index,
                Time = DateTime.MinValue.AddMinutes(index),
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume
            };
        }

        private static ClassifiedBar MakeClassified(
            int index,
            VolumeColor color)
        {
            return new ClassifiedBar
            {
                Index = index,
                VolumeColor = color,
                TwoBarType = TwoBarType.Unknown
            };
        }
    }
}