using System;
using System.Globalization;
using System.Text;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    // Instrumentation-only formatting for APVA Evidence v0.1 diagnostics.
    // This formatter performs no feature calculations or file writing.
    public static class ApvaEvidenceV01Formatter
    {
        public static string CsvHeader()
        {
            return string.Join(",",
                "BarIndex",
                "Time",
                "Open",
                "High",
                "Low",
                "Close",
                "Volume",
                "Geometry",
                "VolumePolarity",
                "VolumeDelta",
                "VolumeRank20",
                "VolumeRank50",
                "Range",
                "RangeDelta",
                "RangeRank20",
                "Body",
                "BodyDelta",
                "BodyRank20",
                "CloseLocation",
                "OverlapRatio",
                "CloseInsidePriorRange",
                "CloseInsidePriorBody",
                "BreaksPriorHigh",
                "BreaksPriorLow",
                "ParticipationState",
                "ExpansionState",
                "CompressionState",
                "DissipationState",
                "AcceptanceState",
                "SignificanceState",
                "EvidenceFlags");
        }

        public static string ToCsv(ApvaEvidenceRow row)
        {
            if (row == null)
                return string.Empty;

            return string.Join(",",
                row.BarIndex.ToString(CultureInfo.InvariantCulture),
                Escape(row.Time.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture)),
                FormatDouble(row.Open),
                FormatDouble(row.High),
                FormatDouble(row.Low),
                FormatDouble(row.Close),
                FormatDouble(row.Volume),
                Escape(row.Geometry.ToString()),
                Escape(row.VolumePolarity.ToString()),
                Escape(row.VolumeDelta.ToString()),
                FormatDouble(row.VolumeRank20),
                FormatDouble(row.VolumeRank50),
                FormatDouble(row.Range),
                Escape(row.RangeDelta.ToString()),
                FormatDouble(row.RangeRank20),
                FormatDouble(row.Body),
                Escape(row.BodyDelta.ToString()),
                FormatDouble(row.BodyRank20),
                FormatDouble(row.CloseLocation),
                FormatDouble(row.OverlapRatio),
                row.CloseInsidePriorRange.ToString(),
                row.CloseInsidePriorBody.ToString(),
                row.BreaksPriorHigh.ToString(),
                row.BreaksPriorLow.ToString(),
                Escape(row.ParticipationState.ToString()),
                Escape(row.ExpansionState.ToString()),
                Escape(row.CompressionState.ToString()),
                Escape(row.DissipationState.ToString()),
                Escape(row.AcceptanceState.ToString()),
                Escape(row.SignificanceState.ToString()),
                Escape(row.EvidenceFlags));
        }

        public static string ToDiagnosticString(ApvaEvidenceRow row)
        {
            if (row == null)
                return string.Empty;

            var sb = new StringBuilder();

            sb.AppendLine("Bar " + row.BarIndex.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine(row.Time.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture));
            sb.AppendLine();

            sb.AppendLine("OHLCV:");
            sb.AppendLine("Open=" + FormatDouble(row.Open));
            sb.AppendLine("High=" + FormatDouble(row.High));
            sb.AppendLine("Low=" + FormatDouble(row.Low));
            sb.AppendLine("Close=" + FormatDouble(row.Close));
            sb.AppendLine("Volume=" + FormatDouble(row.Volume));
            sb.AppendLine();

            sb.AppendLine("Bar Facts:");
            sb.AppendLine("Geometry=" + row.Geometry);
            sb.AppendLine("VolumePolarity=" + row.VolumePolarity);
            sb.AppendLine("VolumeDelta=" + row.VolumeDelta);
            sb.AppendLine("VolumeRank20=" + FormatDouble(row.VolumeRank20));
            sb.AppendLine("VolumeRank50=" + FormatDouble(row.VolumeRank50));
            sb.AppendLine("Range=" + FormatDouble(row.Range));
            sb.AppendLine("RangeDelta=" + row.RangeDelta);
            sb.AppendLine("RangeRank20=" + FormatDouble(row.RangeRank20));
            sb.AppendLine("Body=" + FormatDouble(row.Body));
            sb.AppendLine("BodyDelta=" + row.BodyDelta);
            sb.AppendLine("BodyRank20=" + FormatDouble(row.BodyRank20));
            sb.AppendLine("CloseLocation=" + FormatDouble(row.CloseLocation));
            sb.AppendLine("OverlapRatio=" + FormatDouble(row.OverlapRatio));
            sb.AppendLine();

            sb.AppendLine("Boundaries:");
            sb.AppendLine("CloseInsidePriorRange=" + row.CloseInsidePriorRange);
            sb.AppendLine("CloseInsidePriorBody=" + row.CloseInsidePriorBody);
            sb.AppendLine("BreaksPriorHigh=" + row.BreaksPriorHigh);
            sb.AppendLine("BreaksPriorLow=" + row.BreaksPriorLow);
            sb.AppendLine();

            sb.AppendLine("Evidence:");
            sb.AppendLine("Participation=" + row.ParticipationState);
            sb.AppendLine("Expansion=" + row.ExpansionState);
            sb.AppendLine("Compression=" + row.CompressionState);
            sb.AppendLine("Dissipation=" + row.DissipationState);
            sb.AppendLine("Acceptance=" + row.AcceptanceState);
            sb.AppendLine("Significance=" + row.SignificanceState);
            sb.AppendLine();

            sb.AppendLine("Flags:");
            sb.Append(row.EvidenceFlags ?? string.Empty);

            return sb.ToString();
        }

        private static string FormatDouble(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            if (value == null)
                value = string.Empty;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
