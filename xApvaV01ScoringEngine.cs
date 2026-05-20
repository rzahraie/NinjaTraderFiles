using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01ScoringEngine
    {
        private const double DecayRate = 0.92;

        public ApvaScores UpdateScores(
            ApvaScores priorScores,
            IEnumerable<ApvaEvent> events,
            ApvaSequenceState sequence)
        {
            var scores = priorScores != null
                ? Clone(priorScores)
                : new ApvaScores();

            ApplyDecay(scores);
            ApplySequenceBias(scores, sequence);

            if (events != null)
            {
                foreach (var e in events)
                    ApplyEvent(scores, e);
            }

            Clamp(scores);
            return scores;
        }

        private static void ApplyDecay(ApvaScores s)
        {
            s.DominanceScore *= DecayRate;
            s.DegradationScore *= DecayRate;
            s.BalanceScore *= DecayRate;
            s.TransitionScore *= DecayRate;
            s.AmbiguityScore *= DecayRate;
        }

        private static void ApplySequenceBias(
            ApvaScores s,
            ApvaSequenceState sequence)
        {
            if (sequence == null)
                return;

            if (sequence.AuthorityScore >= 0.65)
                s.DominanceScore += 0.08;

            if (sequence.AuthorityScore >= 0.80)
                s.DominanceScore += 0.05;

            if (sequence.PeakRatio > 0.0 && sequence.PeakRatio < 0.90)
                s.DegradationScore += 0.06;

            if (sequence.Maturity == ApvaMaturityLevel.Mature ||
                sequence.Maturity == ApvaMaturityLevel.Late)
                s.DegradationScore += 0.03;

            if (sequence.IsFailed)
            {
                s.DegradationScore += 0.10;
                s.AmbiguityScore += 0.08;
            }
        }

        private static void ApplyEvent(
            ApvaScores s,
            ApvaEvent e)
        {
            s.DominanceScore += e.EffectOnDominance;
            s.DegradationScore += e.EffectOnDegradation;
            s.BalanceScore += e.EffectOnBalance;
            s.TransitionScore += e.EffectOnTransition;
            s.AmbiguityScore += e.EffectOnAmbiguity;

            if (e.EventType == ApvaEventType.FBO)
            {
                s.TransitionScore += 0.10;
                s.AmbiguityScore += 0.10;
            }

            if (e.EventType == ApvaEventType.FailedContinuation)
            {
                s.DegradationScore += 0.05;
                s.AmbiguityScore += 0.05;
            }

            if (e.EventType == ApvaEventType.DominanceReassertion)
            {
                s.DominanceScore += 0.10;
                s.DegradationScore -= 0.08;
                s.TransitionScore -= 0.08;
            }

            if (e.EventType == ApvaEventType.SFCandidate)
            {
                s.DegradationScore += 0.06;
                s.TransitionScore += 0.03;
            }
        }

        private static void Clamp(ApvaScores s)
        {
            s.DominanceScore = Clamp01(s.DominanceScore);
            s.DegradationScore = Clamp01(s.DegradationScore);
            s.BalanceScore = Clamp01(s.BalanceScore);
            s.TransitionScore = Clamp01(s.TransitionScore);
            s.AmbiguityScore = Clamp01(s.AmbiguityScore);
        }

        private static ApvaScores Clone(ApvaScores s)
        {
            return new ApvaScores
            {
                DominanceScore = s.DominanceScore,
                DegradationScore = s.DegradationScore,
                BalanceScore = s.BalanceScore,
                TransitionScore = s.TransitionScore,
                AmbiguityScore = s.AmbiguityScore
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