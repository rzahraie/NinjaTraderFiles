using System;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01SequenceEngine
    {
        private int nextSequenceId = 1;

        public ApvaSequenceState Update(
            ApvaBarFeatures current,
            ApvaSequenceState priorSequence)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            var direction = DirectionFromPolarity(current.VolumePolarity);

            if (priorSequence == null || priorSequence.Phase == ApvaSequencePhase.Unknown)
                return StartNewSequence(current, direction);

            var s = Clone(priorSequence);
            s.CurrentBar = current.BarIndex;

            if (direction == ApvaDirection.Unknown || direction == ApvaDirection.Mixed)
            {
                UpdateMaturity(s);
                UpdateAuthority(s);
                return s;
            }

            bool sameDirection = direction == s.Direction;

            if (sameDirection)
            {
                UpdateDominantSide(s, current);
            }
            else
            {
                UpdateOpposingSide(s, current, direction);
            }

            UpdatePeakRatio(s);
            UpdateMaturity(s);
            UpdateAuthority(s);

            return s;
        }

        private ApvaSequenceState StartNewSequence(
            ApvaBarFeatures current,
            ApvaDirection direction)
        {
            var phase = PhaseFromDirection(direction);

            var s = new ApvaSequenceState
            {
                SequenceId = nextSequenceId++,
                Direction = direction,
                Phase = phase,
                StartBar = current.BarIndex,
                CurrentBar = current.BarIndex,
                P1Bar = current.BarIndex,
                P1Volume = current.Volume,
                P1Range = current.Range,
                Maturity = ApvaMaturityLevel.Early,
                AuthorityScore = 0.20
            };

            return s;
        }

        private static void UpdateDominantSide(
            ApvaSequenceState s,
            ApvaBarFeatures current)
        {
            if (s.Phase == ApvaSequencePhase.TwoB || s.Phase == ApvaSequencePhase.TwoR)
            {
                s.IsComplete = true;
            }

            if (!s.P1Bar.HasValue)
            {
                s.P1Bar = current.BarIndex;
                s.P1Volume = current.Volume;
                s.P1Range = current.Range;
                return;
            }

            if (!s.P2Bar.HasValue && current.Volume > 0)
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

        private static void UpdateOpposingSide(
            ApvaSequenceState s,
            ApvaBarFeatures current,
            ApvaDirection opposingDirection)
        {
            if (s.Direction == ApvaDirection.Up)
                s.Phase = ApvaSequencePhase.TwoR;
            else if (s.Direction == ApvaDirection.Down)
                s.Phase = ApvaSequencePhase.TwoB;
            else
            {
                s.Direction = opposingDirection;
                s.Phase = PhaseFromDirection(opposingDirection);
            }
        }

        private static void UpdatePeakRatio(ApvaSequenceState s)
        {
            if (s.P1Volume > 0.0 && s.P2Volume > 0.0)
                s.PeakRatio = s.P2Volume / s.P1Volume;
            else
                s.PeakRatio = 0.0;
        }

        private static void UpdateMaturity(ApvaSequenceState s)
        {
            int bars = Math.Max(0, s.CurrentBar - s.StartBar + 1);

            if (s.IsComplete)
            {
                s.Maturity = ApvaMaturityLevel.Resolved;
                return;
            }

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

            if (s.PeakRatio >= 1.20)
                score += 0.25;
            else if (s.PeakRatio >= 1.00)
                score += 0.15;
            else if (s.PeakRatio > 0.0 && s.PeakRatio < 0.90)
                score -= 0.10;

            if (s.Maturity == ApvaMaturityLevel.Mature)
                score += 0.10;

            if (s.IsComplete)
                score += 0.10;

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