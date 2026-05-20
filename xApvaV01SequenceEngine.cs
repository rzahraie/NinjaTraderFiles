using System;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01SequenceEngine
    {
        private int nextSequenceId = 1;
        private int sameDirectionRun;
        private int oppositeDirectionRun;

        public ApvaSequenceState Update(ApvaBarFeatures current, ApvaSequenceState priorSequence)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            var barDirection = DirectionFromPolarity(current.VolumePolarity);

            if (priorSequence == null || priorSequence.Direction == ApvaDirection.Unknown)
                return StartNewSequence(current, barDirection);

            var s = Clone(priorSequence);
            s.CurrentBar = current.BarIndex;

            if (barDirection == ApvaDirection.Unknown)
            {
                UpdateMaturity(s);
                UpdateAuthority(s);
                return s;
            }

            if (barDirection == s.Direction)
            {
                sameDirectionRun++;
                oppositeDirectionRun = 0;

                if (s.Phase == ApvaSequencePhase.TwoB || s.Phase == ApvaSequencePhase.TwoR)
                    s.Phase = PhaseFromDirection(s.Direction);

                UpdateDominantPeak(s, current);
            }
            else
            {
                oppositeDirectionRun++;
                sameDirectionRun = 0;

                if (oppositeDirectionRun >= 2)
                {
                    return StartNewSequence(current, barDirection);
                }

                s.Phase = s.Direction == ApvaDirection.Up
                    ? ApvaSequencePhase.TwoR
                    : ApvaSequencePhase.TwoB;
            }

            UpdatePeakRatio(s);
            UpdateMaturity(s);
            UpdateAuthority(s);

            return s;
        }

        private ApvaSequenceState StartNewSequence(ApvaBarFeatures current, ApvaDirection direction)
        {
            sameDirectionRun = direction == ApvaDirection.Unknown ? 0 : 1;
            oppositeDirectionRun = 0;

            return new ApvaSequenceState
            {
                SequenceId = nextSequenceId++,
                Direction = direction,
                Phase = PhaseFromDirection(direction),
                StartBar = current.BarIndex,
                CurrentBar = current.BarIndex,
                P1Bar = current.BarIndex,
                P1Volume = current.Volume,
                P1Range = current.Range,
                Maturity = ApvaMaturityLevel.Early,
                AuthorityScore = 0.20
            };
        }

        private static void UpdateDominantPeak(ApvaSequenceState s, ApvaBarFeatures current)
        {
            if (!s.P1Bar.HasValue)
            {
                s.P1Bar = current.BarIndex;
                s.P1Volume = current.Volume;
                s.P1Range = current.Range;
                return;
            }

            if (!s.P2Bar.HasValue && current.BarIndex > s.P1Bar.Value)
            {
                s.P2Bar = current.BarIndex;
                s.P2Volume = current.Volume;
                s.P2Range = current.Range;
                return;
            }

            if (s.P2Bar.HasValue && current.Volume > s.P2Volume)
            {
                s.P2Bar = current.BarIndex;
                s.P2Volume = current.Volume;
                s.P2Range = current.Range;
            }
        }

        private static void UpdatePeakRatio(ApvaSequenceState s)
        {
            s.PeakRatio = s.P1Volume > 0.0 && s.P2Volume > 0.0
                ? s.P2Volume / s.P1Volume
                : 0.0;
        }

        private static void UpdateMaturity(ApvaSequenceState s)
        {
            int bars = Math.Max(0, s.CurrentBar - s.StartBar + 1);

            if (bars <= 2)
                s.Maturity = ApvaMaturityLevel.Early;
            else if (bars <= 5)
                s.Maturity = ApvaMaturityLevel.Developing;
            else if (s.P2Bar.HasValue && s.PeakRatio >= 1.0)
                s.Maturity = ApvaMaturityLevel.Mature;
            else if (bars >= 8)
                s.Maturity = ApvaMaturityLevel.Late;
            else
                s.Maturity = ApvaMaturityLevel.Developing;
        }

        private static void UpdateAuthority(ApvaSequenceState s)
        {
            double score = 0.20;

            if (s.P1Bar.HasValue)
                score += 0.15;

            if (s.P2Bar.HasValue)
                score += 0.20;

            if (s.PeakRatio >= 1.50)
                score += 0.30;
            else if (s.PeakRatio >= 1.20)
                score += 0.22;
            else if (s.PeakRatio >= 1.00)
                score += 0.12;
            else if (s.PeakRatio > 0.0 && s.PeakRatio < 0.90)
                score -= 0.12;

            if (s.Maturity == ApvaMaturityLevel.Mature)
                score += 0.08;
            else if (s.Maturity == ApvaMaturityLevel.Late)
                score += 0.03;

            s.AuthorityScore = Clamp01(score);
        }

        private static ApvaDirection DirectionFromPolarity(ApvaVolumePolarity polarity)
        {
            if (polarity == ApvaVolumePolarity.Black)
                return ApvaDirection.Up;

            if (polarity == ApvaVolumePolarity.Red)
                return ApvaDirection.Down;

            return ApvaDirection.Unknown;
        }

        private static ApvaSequencePhase PhaseFromDirection(ApvaDirection direction)
        {
            if (direction == ApvaDirection.Up)
                return ApvaSequencePhase.B2B;

            if (direction == ApvaDirection.Down)
                return ApvaSequencePhase.R2R;

            return ApvaSequencePhase.Unknown;
        }

        private static ApvaSequenceState Clone(ApvaSequenceState s)
        {
            return new ApvaSequenceState
            {
                SequenceId = s.SequenceId,
                Direction = s.Direction,
                Phase = s.Phase,
                StartBar = s.StartBar,
                CurrentBar = s.CurrentBar,
                P1Bar = s.P1Bar,
                P1Volume = s.P1Volume,
                P1Range = s.P1Range,
                P2Bar = s.P2Bar,
                P2Volume = s.P2Volume,
                P2Range = s.P2Range,
                PeakRatio = s.PeakRatio,
                Maturity = s.Maturity,
                AuthorityScore = s.AuthorityScore,
                IsComplete = s.IsComplete,
                IsFailed = s.IsFailed
            };
        }

        private static double Clamp01(double value)
        {
            if (value < 0.0)
                return 0.0;

            if (value > 1.0)
                return 1.0;

            return value;
        }
    }
}
