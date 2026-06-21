#region Using declarations
using System;
using System.Collections.Generic;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaSequenceKind
    {
        Unknown,
        NeutralRun,
        SingleB,
        SingleR,
        CandidateB2B,
        CandidateR2R,
        Candidate2B,
        Candidate2R
    }

    public sealed class xPvaEventSequence
    {
        public int SequenceId { get; internal set; }
        public xPvaVolumePolarity Polarity { get; internal set; }
        public xPvaSequenceKind Kind { get; internal set; }

        public int StartBar { get; internal set; }
        public int EndBar { get; internal set; }
        public DateTime StartTime { get; internal set; }
        public DateTime EndTime { get; internal set; }

        public int BarCount { get; internal set; }
        public int PlusCount { get; internal set; }
        public int MinusCount { get; internal set; }
        public int EqualCount { get; internal set; }

        public xPvaVolumeChange FirstVolumeChange { get; internal set; }
        public xPvaVolumeChange LastVolumeChange { get; internal set; }
        public bool HasMinusToPlusTransition { get; internal set; }
        public bool HasPlusToMinusTransition { get; internal set; }

        public int PeakCount { get; internal set; }
        public int AcceleratedPeakCount { get; internal set; }
        public int CompressedRangeCount { get; internal set; }
        public int VolumeRangeMismatchCount { get; internal set; }

        public double MaxVolume { get; internal set; }
        public int MaxVolumeBar { get; internal set; }
        public double MaxRangeTicks { get; internal set; }
        public int MaxRangeBar { get; internal set; }

        public bool ContainsPeakVolume { get { return PeakCount > 0 || AcceleratedPeakCount > 0; } }
        public bool ContainsCompressedRange { get { return CompressedRangeCount > 0; } }
        public bool ContainsVolumeRangeMismatch { get { return VolumeRangeMismatchCount > 0; } }

        public string ChangePattern
        {
            get
            {
                if (BarCount <= 0) return "";
                return FirstVolumeChange.ToString() + "->" + LastVolumeChange.ToString();
            }
        }

        public string Label
        {
            get
            {
                string s = Kind.ToString();
                s += string.Format(" [{0}-{1}] n={2} {3}", StartBar, EndBar, BarCount, ChangePattern);
                if (ContainsPeakVolume) s += " PV";
                if (ContainsCompressedRange) s += " CR";
                if (ContainsVolumeRangeMismatch) s += " VRM";
                return s;
            }
        }

        internal xPvaEventSequence Clone()
        {
            return (xPvaEventSequence)MemberwiseClone();
        }
    }

    /// <summary>
    /// Stage 2 sequence layer.
    /// It groups contiguous same-polarity discrete events into candidate runs.
    /// It does not assert completed Gaussians, containers, FTTs, or trade signals.
    /// </summary>
    public sealed class xPvaEventSequenceEngine
    {
        private int nextSequenceId = 1;
        private xPvaEventSequence current;
        private xPvaEventSequence lastCompletedDirectional;

        public xPvaEventSequence CurrentSequence { get { return current == null ? null : current.Clone(); } }
        public xPvaEventSequence LastCompletedDirectionalSequence { get { return lastCompletedDirectional == null ? null : lastCompletedDirectional.Clone(); } }

        public xPvaEventSequence Update(xPvaDiscreteEvent ev, double volume, double rangeTicks, out xPvaEventSequence completedSequence)
        {
            completedSequence = null;
            if (ev == null)
                return CurrentSequence;

            if (current == null)
            {
                current = StartNewSequence(ev, volume, rangeTicks);
                return CurrentSequence;
            }

            if (ev.VolumePolarity == current.Polarity)
            {
                AddToCurrent(ev, volume, rangeTicks);
                return CurrentSequence;
            }

            completedSequence = FinalizeCurrent();
            if (completedSequence.Polarity != xPvaVolumePolarity.Neutral)
                lastCompletedDirectional = completedSequence.Clone();

            current = StartNewSequence(ev, volume, rangeTicks);
            return CurrentSequence;
        }

        public xPvaEventSequence ForceCloseCurrent()
        {
            if (current == null)
                return null;

            var completed = FinalizeCurrent();
            if (completed.Polarity != xPvaVolumePolarity.Neutral)
                lastCompletedDirectional = completed.Clone();
            current = null;
            return completed;
        }

        private xPvaEventSequence StartNewSequence(xPvaDiscreteEvent ev, double volume, double rangeTicks)
        {
            var seq = new xPvaEventSequence
            {
                SequenceId = nextSequenceId++,
                Polarity = ev.VolumePolarity,
                StartBar = ev.BarIndex,
                EndBar = ev.BarIndex,
                StartTime = ev.Time,
                EndTime = ev.Time,
                BarCount = 0,
                MaxVolume = double.MinValue,
                MaxRangeTicks = double.MinValue
            };

            current = seq;
            AddToCurrent(ev, volume, rangeTicks);
            return current;
        }

        private void AddToCurrent(xPvaDiscreteEvent ev, double volume, double rangeTicks)
        {
            xPvaVolumeChange priorLast = current.LastVolumeChange;

            current.EndBar = ev.BarIndex;
            current.EndTime = ev.Time;

            if (current.BarCount == 0)
                current.FirstVolumeChange = ev.VolumeChange;
            else
            {
                if (priorLast == xPvaVolumeChange.Minus && ev.VolumeChange == xPvaVolumeChange.Plus)
                    current.HasMinusToPlusTransition = true;
                else if (priorLast == xPvaVolumeChange.Plus && ev.VolumeChange == xPvaVolumeChange.Minus)
                    current.HasPlusToMinusTransition = true;
            }

            current.LastVolumeChange = ev.VolumeChange;
            current.BarCount++;

            if (ev.VolumeChange == xPvaVolumeChange.Plus) current.PlusCount++;
            else if (ev.VolumeChange == xPvaVolumeChange.Minus) current.MinusCount++;
            else current.EqualCount++;

            if (ev.IsStrictPeakVolume) current.PeakCount++;
            if (ev.IsAcceleratedPeakVolume) current.AcceleratedPeakCount++;
            if (ev.IsCompressedRange) current.CompressedRangeCount++;
            if (ev.IsVolumeRangeMismatch) current.VolumeRangeMismatchCount++;

            if (volume > current.MaxVolume)
            {
                current.MaxVolume = volume;
                current.MaxVolumeBar = ev.BarIndex;
            }

            if (rangeTicks > current.MaxRangeTicks)
            {
                current.MaxRangeTicks = rangeTicks;
                current.MaxRangeBar = ev.BarIndex;
            }

            current.Kind = ClassifyCurrentKind();
        }

        private xPvaEventSequence FinalizeCurrent()
        {
            current.Kind = ClassifyCurrentKind();
            return current.Clone();
        }

        private xPvaSequenceKind ClassifyCurrentKind()
        {
            if (current == null)
                return xPvaSequenceKind.Unknown;

            if (current.Polarity == xPvaVolumePolarity.Neutral)
                return xPvaSequenceKind.NeutralRun;

            bool candidateDominantRun = current.BarCount >= 2 && current.PlusCount >= 1;

            if (candidateDominantRun)
            {
                if (current.Polarity == xPvaVolumePolarity.B)
                    return IsOppositeOfLastDirectional() ? xPvaSequenceKind.Candidate2B : xPvaSequenceKind.CandidateB2B;

                if (current.Polarity == xPvaVolumePolarity.R)
                    return IsOppositeOfLastDirectional() ? xPvaSequenceKind.Candidate2R : xPvaSequenceKind.CandidateR2R;
            }

            if (current.Polarity == xPvaVolumePolarity.B)
                return xPvaSequenceKind.SingleB;

            if (current.Polarity == xPvaVolumePolarity.R)
                return xPvaSequenceKind.SingleR;

            return xPvaSequenceKind.Unknown;
        }

        private bool IsOppositeOfLastDirectional()
        {
            if (lastCompletedDirectional == null)
                return false;

            if (lastCompletedDirectional.Polarity == xPvaVolumePolarity.B && current.Polarity == xPvaVolumePolarity.R)
                return true;

            if (lastCompletedDirectional.Polarity == xPvaVolumePolarity.R && current.Polarity == xPvaVolumePolarity.B)
                return true;

            return false;
        }
    }
}
