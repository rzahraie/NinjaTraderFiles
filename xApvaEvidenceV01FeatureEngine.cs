using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    // Instrumentation-only feature extraction for the APVA Evidence v0.1 exporter.
    // This engine records deterministic bar evidence without prediction or trading logic.
    public sealed class ApvaEvidenceV01FeatureEngine
    {
        public ApvaEvidenceRow Build(
            int barIndex,
            DateTime time,
            double open,
            double high,
            double low,
            double close,
            double volume,
            double priorOpen,
            double priorHigh,
            double priorLow,
            double priorClose,
            double priorVolume,
            IReadOnlyList<double> recentVolumes,
            IReadOnlyList<double> recentRanges,
            IReadOnlyList<double> recentBodies)
        {
            double range = high - low;
            double body = Math.Abs(close - open);
            double priorRange = priorHigh - priorLow;
            double priorBody = Math.Abs(priorClose - priorOpen);

            var row = new ApvaEvidenceRow
            {
                BarIndex = barIndex,
                Time = time,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume,
                Geometry = ClassifyGeometry(high, low, priorHigh, priorLow),
                VolumePolarity = ClassifyPolarity(open, close),
                VolumeDelta = ClassifyDelta(volume, priorVolume),
                VolumeRank20 = ComputePercentileRank(volume, recentVolumes, 20),
                VolumeRank50 = ComputePercentileRank(volume, recentVolumes, 50),
                Range = range,
                RangeDelta = ClassifyDelta(range, priorRange),
                RangeRank20 = ComputePercentileRank(range, recentRanges, 20),
                Body = body,
                BodyDelta = ClassifyDelta(body, priorBody),
                BodyRank20 = ComputePercentileRank(body, recentBodies, 20),
                CloseLocation = range > 0.0
                    ? (close - low) / range
                    : 0.5,
                OverlapRatio = ComputeOverlapRatio(
                    high,
                    low,
                    priorHigh,
                    priorLow,
                    range),
                CloseInsidePriorRange =
                    close <= priorHigh && close >= priorLow,
                CloseInsidePriorBody = IsCloseInsidePriorBody(
                    close,
                    priorOpen,
                    priorClose),
                BreaksPriorHigh = high > priorHigh,
                BreaksPriorLow = low < priorLow
            };

            row.ParticipationState = ClassifyParticipationState(
                row.VolumeRank20,
                row.VolumeRank50,
                volume,
                priorVolume);
            row.ExpansionState = ClassifyExpansionState(row);
            row.CompressionState = ClassifyCompressionState(row);
            row.DissipationState = ClassifyDissipationState(
                row,
                volume,
                priorVolume,
                priorRange);
            row.AcceptanceState = ClassifyAcceptanceState(row);
            row.SignificanceState = ClassifySignificanceState(row);
            row.EvidenceFlags = BuildEvidenceFlags(row);

            return row;
        }

        private static double ComputePercentileRank(
            double current,
            IReadOnlyList<double> values,
            int requiredCount)
        {
            if (values == null || values.Count < requiredCount)
                return 0.0;

            int startIndex = values.Count - requiredCount;
            int countLessThanOrEqual = 0;

            for (int i = startIndex; i < values.Count; i++)
            {
                if (values[i] <= current)
                    countLessThanOrEqual++;
            }

            return (double)countLessThanOrEqual / requiredCount;
        }

        private static ApvaEvidenceDeltaState ClassifyDelta(
            double current,
            double prior)
        {
            if (current > prior)
                return ApvaEvidenceDeltaState.Up;

            if (current < prior)
                return ApvaEvidenceDeltaState.Down;

            return ApvaEvidenceDeltaState.Flat;
        }

        private static ApvaEvidenceGeometry ClassifyGeometry(
            double high,
            double low,
            double priorHigh,
            double priorLow)
        {
            if (high > priorHigh && low > priorLow)
                return ApvaEvidenceGeometry.HHHL;

            if (high < priorHigh && low < priorLow)
                return ApvaEvidenceGeometry.LLLH;

            if (high == priorHigh && low == priorLow)
                return ApvaEvidenceGeometry.Symmetric;

            if (high <= priorHigh && low >= priorLow)
                return ApvaEvidenceGeometry.InsideBar;

            if (high >= priorHigh && low <= priorLow)
                return ApvaEvidenceGeometry.OutsideBar;

            return ApvaEvidenceGeometry.Unknown;
        }

        private static ApvaEvidencePolarity ClassifyPolarity(
            double open,
            double close)
        {
            if (close > open)
                return ApvaEvidencePolarity.Black;

            if (close < open)
                return ApvaEvidencePolarity.Red;

            return ApvaEvidencePolarity.Neutral;
        }

        private static double ComputeOverlapRatio(
            double high,
            double low,
            double priorHigh,
            double priorLow,
            double range)
        {
            if (range <= 0.0)
                return 0.0;

            double overlapHigh = Math.Min(high, priorHigh);
            double overlapLow = Math.Max(low, priorLow);
            double overlap = Math.Max(0.0, overlapHigh - overlapLow);

            return overlap / range;
        }

        private static bool IsCloseInsidePriorBody(
            double close,
            double priorOpen,
            double priorClose)
        {
            double priorBodyHigh = Math.Max(priorOpen, priorClose);
            double priorBodyLow = Math.Min(priorOpen, priorClose);

            return close <= priorBodyHigh && close >= priorBodyLow;
        }

        private static ApvaParticipationState ClassifyParticipationState(
            double volumeRank20,
            double volumeRank50,
            double volume,
            double priorVolume)
        {
            if (volumeRank50 >= 0.95)
                return ApvaParticipationState.Climactic;

            if (volumeRank20 >= 0.80)
                return ApvaParticipationState.Peak;

            if (volume > priorVolume)
                return ApvaParticipationState.Rising;

            if (volume < priorVolume)
                return ApvaParticipationState.Falling;

            return ApvaParticipationState.Normal;
        }

        private static ApvaExpansionState ClassifyExpansionState(
            ApvaEvidenceRow row)
        {
            if (row.RangeRank20 >= 0.95)
                return ApvaExpansionState.Climactic;

            if (row.RangeRank20 >= 0.80)
                return ApvaExpansionState.Strong;

            if (row.RangeDelta == ApvaEvidenceDeltaState.Up &&
                (row.Geometry == ApvaEvidenceGeometry.HHHL ||
                 row.Geometry == ApvaEvidenceGeometry.LLLH ||
                 row.Geometry == ApvaEvidenceGeometry.OutsideBar))
            {
                return ApvaExpansionState.Local;
            }

            if (row.RangeDelta == ApvaEvidenceDeltaState.Up &&
                row.CloseInsidePriorRange)
            {
                return ApvaExpansionState.Failed;
            }

            return ApvaExpansionState.Absent;
        }

        private static ApvaCompressionState ClassifyCompressionState(
            ApvaEvidenceRow row)
        {
            if (row.OverlapRatio >= 0.80 &&
                row.RangeDelta != ApvaEvidenceDeltaState.Up)
            {
                return ApvaCompressionState.Clustered;
            }

            if (row.Geometry == ApvaEvidenceGeometry.InsideBar ||
                row.Geometry == ApvaEvidenceGeometry.Symmetric ||
                row.CloseInsidePriorRange)
            {
                return ApvaCompressionState.Local;
            }

            return ApvaCompressionState.Absent;
        }

        private static ApvaDissipationState ClassifyDissipationState(
            ApvaEvidenceRow row,
            double volume,
            double priorVolume,
            double priorRange)
        {
            if ((row.BreaksPriorHigh && volume < priorVolume) ||
                (row.BreaksPriorLow && volume < priorVolume) ||
                (volume > priorVolume && row.Range < priorRange))
            {
                return ApvaDissipationState.Local;
            }

            return ApvaDissipationState.Absent;
        }

        private static ApvaAcceptanceState ClassifyAcceptanceState(
            ApvaEvidenceRow row)
        {
            bool breaksPriorBoundary =
                row.BreaksPriorHigh || row.BreaksPriorLow;

            if (!row.CloseInsidePriorRange && breaksPriorBoundary)
                return ApvaAcceptanceState.Accepted;

            if (breaksPriorBoundary && row.CloseInsidePriorRange)
                return ApvaAcceptanceState.Contained;

            if (row.Geometry == ApvaEvidenceGeometry.InsideBar ||
                row.Geometry == ApvaEvidenceGeometry.Symmetric)
            {
                return ApvaAcceptanceState.Unresolved;
            }

            return ApvaAcceptanceState.Unknown;
        }

        private static ApvaSignificanceState ClassifySignificanceState(
            ApvaEvidenceRow row)
        {
            if (row.VolumeRank50 >= 0.95 && row.RangeRank20 >= 0.95)
                return ApvaSignificanceState.SessionDefining;

            if (row.VolumeRank50 >= 0.90 || row.RangeRank20 >= 0.90)
                return ApvaSignificanceState.Structural;

            if (row.VolumeRank20 >= 0.80 || row.RangeRank20 >= 0.80)
                return ApvaSignificanceState.Major;

            if (row.VolumeRank20 >= 0.60 || row.RangeRank20 >= 0.60)
                return ApvaSignificanceState.Moderate;

            return ApvaSignificanceState.Minor;
        }

        private static string BuildEvidenceFlags(ApvaEvidenceRow row)
        {
            var flags = new List<string>();

            AddFlag(flags, row.VolumeRank20 >= 0.80, "VolumePeak20");
            AddFlag(flags, row.VolumeRank50 >= 0.95, "VolumeClimactic50");
            AddFlag(flags, row.RangeRank20 >= 0.80, "RangePeak20");
            AddFlag(flags, row.RangeRank20 >= 0.95, "RangeClimactic20");
            AddFlag(
                flags,
                row.VolumeRank20 >= 0.80 && row.RangeRank20 <= 0.40,
                "HighVolumeLowRange");
            AddFlag(
                flags,
                row.VolumeRank20 <= 0.40 && row.RangeRank20 >= 0.80,
                "LowVolumeHighRange");
            AddFlag(flags, row.BreaksPriorHigh, "BreaksPriorHigh");
            AddFlag(flags, row.BreaksPriorLow, "BreaksPriorLow");
            AddFlag(
                flags,
                row.CloseInsidePriorRange,
                "CloseInsidePriorRange");
            AddFlag(flags, row.CloseInsidePriorBody, "CloseInsidePriorBody");
            AddFlag(
                flags,
                row.DissipationState == ApvaDissipationState.Local,
                "DissipationLocal");
            AddFlag(
                flags,
                row.AcceptanceState == ApvaAcceptanceState.Contained,
                "AcceptanceContained");
            AddFlag(
                flags,
                row.AcceptanceState == ApvaAcceptanceState.Accepted,
                "AcceptanceAccepted");
            AddFlag(
                flags,
                row.CompressionState == ApvaCompressionState.Local,
                "CompressionLocal");
            AddFlag(
                flags,
                row.CompressionState == ApvaCompressionState.Clustered,
                "CompressionClustered");

            return string.Join(";", flags);
        }

        private static void AddFlag(
            ICollection<string> flags,
            bool condition,
            string flag)
        {
            if (condition)
                flags.Add(flag);
        }
    }
}
