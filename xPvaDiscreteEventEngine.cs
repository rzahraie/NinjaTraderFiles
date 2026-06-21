#region Using declarations
using System;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaCompareResult { Less = -1, Equal = 0, Greater = 1 }

    public enum xPvaBarRelation
    {
        Unknown,
        HHHL,
        LLLH,
        FTP,
        FBP,
        StitchLong,
        StitchShort,
        InsideBar,
        OutsideBar,
        OutsideBullish,
        OutsideBearish,
        SameHighSameLow,
        HighReversal,
        LowReversal
    }

    public enum xPvaVolumePolarity { Neutral, B, R }
    public enum xPvaVolumeChange { Equal, Plus, Minus }
    public enum xPvaRangeChange { Equal, Expanding, Contracting }

    public sealed class xPvaBarFacts
    {
        public int BarIndex { get; private set; }
        public DateTime Time { get; private set; }
        public double Open { get; private set; }
        public double High { get; private set; }
        public double Low { get; private set; }
        public double Close { get; private set; }
        public double Volume { get; private set; }
        public double RangeTicks { get; private set; }
        public double BodyTicks { get; private set; }

        public xPvaBarFacts(int barIndex, DateTime time, double open, double high, double low, double close, double volume, double tickSize)
        {
            BarIndex = barIndex;
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            RangeTicks = tickSize > 0 ? Math.Abs(high - low) / tickSize : 0.0;
            BodyTicks = tickSize > 0 ? Math.Abs(close - open) / tickSize : 0.0;
        }
    }

    public sealed class xPvaDiscreteEvent
    {
        public int BarIndex;
        public DateTime Time;
        public xPvaBarRelation Relation;
        public xPvaVolumePolarity VolumePolarity;
        public xPvaVolumeChange VolumeChange;
        public bool IsStrictPeakVolume;
        public bool IsAcceleratedPeakVolume;
        public xPvaRangeChange RangeChange;
        public bool IsCompressedRange;
        public bool IsVolumeRangeMismatch;
        public string Label;
    }

    public sealed class xPvaDiscreteEventEngine
    {
        private readonly double tickSize;
        private readonly double epsilon;

        public xPvaDiscreteEventEngine(double tickSize)
        {
            this.tickSize = tickSize;
            this.epsilon = Math.Max(tickSize * 0.5, 0.0);
        }

        public xPvaCompareResult Compare(double a, double b)
        {
            if (a > b + epsilon) return xPvaCompareResult.Greater;
            if (a < b - epsilon) return xPvaCompareResult.Less;
            return xPvaCompareResult.Equal;
        }

        public xPvaBarRelation ClassifyRelation(xPvaBarFacts cur, xPvaBarFacts prev)
        {
            var h = Compare(cur.High, prev.High);
            var l = Compare(cur.Low, prev.Low);
            var c = Compare(cur.Close, prev.Close);

            xPvaBarRelation rel;

            if (h == xPvaCompareResult.Equal && l == xPvaCompareResult.Equal)
                rel = xPvaBarRelation.SameHighSameLow;
            else if (h == xPvaCompareResult.Greater && l == xPvaCompareResult.Less)
                rel = Compare(cur.Close, prev.Close) == xPvaCompareResult.Less ? xPvaBarRelation.OutsideBearish : xPvaBarRelation.OutsideBullish;
            else if (h == xPvaCompareResult.Less && l == xPvaCompareResult.Greater)
                rel = xPvaBarRelation.InsideBar;
            else if (h == xPvaCompareResult.Greater && l == xPvaCompareResult.Greater)
                rel = xPvaBarRelation.HHHL;
            else if (h == xPvaCompareResult.Less && l == xPvaCompareResult.Less)
                rel = xPvaBarRelation.LLLH;
            else if (h == xPvaCompareResult.Equal && l == xPvaCompareResult.Greater)
                rel = xPvaBarRelation.FTP;
            else if (h == xPvaCompareResult.Less && l == xPvaCompareResult.Equal)
                rel = xPvaBarRelation.FBP;
            else if (h == xPvaCompareResult.Greater && l == xPvaCompareResult.Equal)
                rel = xPvaBarRelation.StitchLong;
            else if (h == xPvaCompareResult.Equal && l == xPvaCompareResult.Less)
                rel = xPvaBarRelation.StitchShort;
            else
                rel = xPvaBarRelation.Unknown;

            if ((rel == xPvaBarRelation.HHHL || rel == xPvaBarRelation.FTP) && c == xPvaCompareResult.Less)
                return xPvaBarRelation.HighReversal;

            if ((rel == xPvaBarRelation.LLLH || rel == xPvaBarRelation.FBP) && c == xPvaCompareResult.Greater)
                return xPvaBarRelation.LowReversal;

            return rel;
        }

        public xPvaVolumePolarity ClassifyVolumePolarity(xPvaBarRelation relation, xPvaCompareResult closeCompare)
        {
            switch (relation)
            {
                case xPvaBarRelation.HHHL:
                case xPvaBarRelation.StitchLong:
                case xPvaBarRelation.OutsideBullish:
                case xPvaBarRelation.LowReversal:
                    return xPvaVolumePolarity.B;

                case xPvaBarRelation.LLLH:
                case xPvaBarRelation.StitchShort:
                case xPvaBarRelation.OutsideBearish:
                case xPvaBarRelation.HighReversal:
                    return xPvaVolumePolarity.R;

                case xPvaBarRelation.InsideBar:
                case xPvaBarRelation.FTP:
                case xPvaBarRelation.FBP:
                case xPvaBarRelation.SameHighSameLow:
                    if (closeCompare == xPvaCompareResult.Greater) return xPvaVolumePolarity.B;
                    if (closeCompare == xPvaCompareResult.Less) return xPvaVolumePolarity.R;
                    return xPvaVolumePolarity.Neutral;

                default:
                    return xPvaVolumePolarity.Neutral;
            }
        }

        public xPvaDiscreteEvent BuildEvent(xPvaBarFacts cur, xPvaBarFacts prev, xPvaBarFacts prev2)
        {
            var relation = ClassifyRelation(cur, prev);
            var closeCompare = Compare(cur.Close, prev.Close);
            var polarity = ClassifyVolumePolarity(relation, closeCompare);

            var volumeChange = xPvaVolumeChange.Equal;
            if (cur.Volume > prev.Volume) volumeChange = xPvaVolumeChange.Plus;
            else if (cur.Volume < prev.Volume) volumeChange = xPvaVolumeChange.Minus;

            var rangeChange = xPvaRangeChange.Equal;
            if (cur.RangeTicks > prev.RangeTicks) rangeChange = xPvaRangeChange.Expanding;
            else if (cur.RangeTicks < prev.RangeTicks) rangeChange = xPvaRangeChange.Contracting;

            bool strictPeak = prev2 != null && cur.Volume > prev.Volume && cur.Volume > prev2.Volume;
            bool acceleratedPeak = prev2 != null && cur.Volume > prev.Volume && prev.Volume > prev2.Volume;
            bool compressed = cur.RangeTicks < prev.RangeTicks;
            bool mismatch = volumeChange == xPvaVolumeChange.Plus && rangeChange == xPvaRangeChange.Contracting;

            var ev = new xPvaDiscreteEvent
            {
                BarIndex = cur.BarIndex,
                Time = cur.Time,
                Relation = relation,
                VolumePolarity = polarity,
                VolumeChange = volumeChange,
                IsStrictPeakVolume = strictPeak,
                IsAcceleratedPeakVolume = acceleratedPeak,
                RangeChange = rangeChange,
                IsCompressedRange = compressed,
                IsVolumeRangeMismatch = mismatch
            };

            ev.Label = BuildLabel(ev);
            return ev;
        }

        private string BuildLabel(xPvaDiscreteEvent ev)
        {
            string vol = ev.VolumePolarity.ToString();
            if (ev.VolumePolarity != xPvaVolumePolarity.Neutral)
                vol += ev.VolumeChange == xPvaVolumeChange.Plus ? "+" : ev.VolumeChange == xPvaVolumeChange.Minus ? "-" : "=";

            string label = ev.Relation + "-" + vol;
            if (ev.IsAcceleratedPeakVolume) label += " PV>A";
            else if (ev.IsStrictPeakVolume) label += " PV";
            if (ev.IsCompressedRange) label += " CR";
            if (ev.IsVolumeRangeMismatch) label += " VRM";
            return label;
        }
    }
}
