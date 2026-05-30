using System;
using System.Collections.Generic;
using System.Text;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    // Research-only descriptive statistics for APVA Evidence v0.1 rows.
    public sealed class ApvaEvidenceV01Analyzer
    {
        private readonly object syncRoot = new object();

        private long totalBars;

        private readonly Dictionary<string, long> participationCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> expansionCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> compressionCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> dissipationCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> acceptanceCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> significanceCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> geometryCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> volumePolarityCounts =
            new Dictionary<string, long>();

        private readonly Dictionary<string, long> dissipationAcceptanceCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> participationAcceptanceCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> participationCompressionCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> participationExpansionCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> significanceAcceptanceCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> geometryAcceptanceCounts =
            new Dictionary<string, long>();
        private readonly Dictionary<string, long> geometryCompressionCounts =
            new Dictionary<string, long>();

        private readonly Dictionary<string, long> evidenceFlagCounts =
            new Dictionary<string, long>();

        public void Add(ApvaEvidenceRow row)
        {
            if (row == null)
                return;

            lock (syncRoot)
            {
                totalBars++;

                Increment(participationCounts, row.ParticipationState.ToString());
                Increment(expansionCounts, row.ExpansionState.ToString());
                Increment(compressionCounts, row.CompressionState.ToString());
                Increment(dissipationCounts, row.DissipationState.ToString());
                Increment(acceptanceCounts, row.AcceptanceState.ToString());
                Increment(significanceCounts, row.SignificanceState.ToString());
                Increment(geometryCounts, row.Geometry.ToString());
                Increment(volumePolarityCounts, row.VolumePolarity.ToString());

                Increment(
                    dissipationAcceptanceCounts,
                    Join(row.DissipationState, row.AcceptanceState));
                Increment(
                    participationAcceptanceCounts,
                    Join(row.ParticipationState, row.AcceptanceState));
                Increment(
                    participationCompressionCounts,
                    Join(row.ParticipationState, row.CompressionState));
                Increment(
                    participationExpansionCounts,
                    Join(row.ParticipationState, row.ExpansionState));
                Increment(
                    significanceAcceptanceCounts,
                    Join(row.SignificanceState, row.AcceptanceState));
                Increment(
                    geometryAcceptanceCounts,
                    Join(row.Geometry, row.AcceptanceState));
                Increment(
                    geometryCompressionCounts,
                    Join(row.Geometry, row.CompressionState));

                CountEvidenceFlags(row.EvidenceFlags);
            }
        }

        public ApvaEvidenceV01Summary BuildSummary()
        {
            lock (syncRoot)
            {
                return new ApvaEvidenceV01Summary
                {
                    TotalBars = totalBars,
                    ParticipationCounts = Copy(participationCounts),
                    ExpansionCounts = Copy(expansionCounts),
                    CompressionCounts = Copy(compressionCounts),
                    DissipationCounts = Copy(dissipationCounts),
                    AcceptanceCounts = Copy(acceptanceCounts),
                    SignificanceCounts = Copy(significanceCounts),
                    GeometryCounts = Copy(geometryCounts),
                    VolumePolarityCounts = Copy(volumePolarityCounts),
                    DissipationAcceptanceCounts =
                        Copy(dissipationAcceptanceCounts),
                    ParticipationAcceptanceCounts =
                        Copy(participationAcceptanceCounts),
                    ParticipationCompressionCounts =
                        Copy(participationCompressionCounts),
                    ParticipationExpansionCounts =
                        Copy(participationExpansionCounts),
                    SignificanceAcceptanceCounts =
                        Copy(significanceAcceptanceCounts),
                    GeometryAcceptanceCounts = Copy(geometryAcceptanceCounts),
                    GeometryCompressionCounts = Copy(geometryCompressionCounts),
                    EvidenceFlagCounts = Copy(evidenceFlagCounts)
                };
            }
        }

        public void Reset()
        {
            lock (syncRoot)
            {
                totalBars = 0;

                participationCounts.Clear();
                expansionCounts.Clear();
                compressionCounts.Clear();
                dissipationCounts.Clear();
                acceptanceCounts.Clear();
                significanceCounts.Clear();
                geometryCounts.Clear();
                volumePolarityCounts.Clear();

                dissipationAcceptanceCounts.Clear();
                participationAcceptanceCounts.Clear();
                participationCompressionCounts.Clear();
                participationExpansionCounts.Clear();
                significanceAcceptanceCounts.Clear();
                geometryAcceptanceCounts.Clear();
                geometryCompressionCounts.Clear();

                evidenceFlagCounts.Clear();
            }
        }

        private void CountEvidenceFlags(string evidenceFlags)
        {
            if (string.IsNullOrWhiteSpace(evidenceFlags))
                return;

            string[] flags = evidenceFlags.Split(';');

            foreach (string value in flags)
            {
                string flag = value.Trim();

                if (!string.IsNullOrEmpty(flag))
                    Increment(evidenceFlagCounts, flag);
            }
        }

        private static string Join(object first, object second)
        {
            return first + "|" + second;
        }

        private static void Increment(
            IDictionary<string, long> counts,
            string key)
        {
            long count;

            if (!counts.TryGetValue(key, out count))
                count = 0;

            counts[key] = count + 1;
        }

        private static Dictionary<string, long> Copy(
            IDictionary<string, long> source)
        {
            return new Dictionary<string, long>(source);
        }
    }

    // Snapshot of analyzer counts. Dictionaries are copied when the snapshot is built.
    public sealed class ApvaEvidenceV01Summary
    {
        public long TotalBars { get; set; }

        public Dictionary<string, long> ParticipationCounts { get; set; }
        public Dictionary<string, long> ExpansionCounts { get; set; }
        public Dictionary<string, long> CompressionCounts { get; set; }
        public Dictionary<string, long> DissipationCounts { get; set; }
        public Dictionary<string, long> AcceptanceCounts { get; set; }
        public Dictionary<string, long> SignificanceCounts { get; set; }
        public Dictionary<string, long> GeometryCounts { get; set; }
        public Dictionary<string, long> VolumePolarityCounts { get; set; }

        public Dictionary<string, long> DissipationAcceptanceCounts { get; set; }
        public Dictionary<string, long> ParticipationAcceptanceCounts { get; set; }
        public Dictionary<string, long> ParticipationCompressionCounts { get; set; }
        public Dictionary<string, long> ParticipationExpansionCounts { get; set; }
        public Dictionary<string, long> SignificanceAcceptanceCounts { get; set; }
        public Dictionary<string, long> GeometryAcceptanceCounts { get; set; }
        public Dictionary<string, long> GeometryCompressionCounts { get; set; }

        public Dictionary<string, long> EvidenceFlagCounts { get; set; }

        public string ToDiagnosticString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Total Bars: " + TotalBars);
            sb.AppendLine();

            AppendSection(sb, "Participation", ParticipationCounts);
            AppendSection(sb, "Expansion", ExpansionCounts);
            AppendSection(sb, "Compression", CompressionCounts);
            AppendSection(sb, "Dissipation", DissipationCounts);
            AppendSection(sb, "Acceptance", AcceptanceCounts);
            AppendSection(sb, "Significance", SignificanceCounts);
            AppendSection(sb, "Geometry", GeometryCounts);
            AppendSection(sb, "Volume Polarity", VolumePolarityCounts);

            AppendSection(
                sb,
                "Dissipation + Acceptance",
                DissipationAcceptanceCounts);
            AppendSection(
                sb,
                "Participation + Acceptance",
                ParticipationAcceptanceCounts);
            AppendSection(
                sb,
                "Participation + Compression",
                ParticipationCompressionCounts);
            AppendSection(
                sb,
                "Participation + Expansion",
                ParticipationExpansionCounts);
            AppendSection(
                sb,
                "Significance + Acceptance",
                SignificanceAcceptanceCounts);
            AppendSection(
                sb,
                "Geometry + Acceptance",
                GeometryAcceptanceCounts);
            AppendSection(
                sb,
                "Geometry + Compression",
                GeometryCompressionCounts);

            AppendSection(sb, "Evidence Flags", EvidenceFlagCounts);

            return sb.ToString().TrimEnd();
        }

        private static void AppendSection(
            StringBuilder sb,
            string title,
            IDictionary<string, long> counts)
        {
            sb.AppendLine(title + ":");

            if (counts != null)
            {
                var keys = new List<string>(counts.Keys);
                keys.Sort(StringComparer.Ordinal);

                foreach (string key in keys)
                    sb.AppendLine("  " + key + ": " + counts[key]);
            }

            sb.AppendLine();
        }
    }
}
