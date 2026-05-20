using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01ScoringEngine
    {
        private const double BaseDecayRate = 0.92;

        public ApvaScores UpdateScores(
            ApvaScores priorScores,
            IEnumerable<ApvaEvent> events,
            ApvaSequenceState sequence)
        {
            var scores = priorScores != null
                ? Clone(priorScores)
                : new ApvaScores();

            ApplyBaseDecay(scores);
            ApplySequenceBias(scores, sequence);

            if (events != null)
            {
                foreach (var e in events)
                    ApplyEvent(scores, e);
            }

            ApplyInternalConflictRules(scores);
            Clamp(scores);
            return scores;
        }

        private static void ApplyBaseDecay(ApvaScores s)
        {
            s.DominanceScore *= BaseDecayRate;
            s.DegradationScore *= BaseDecayRate;
            s.BalanceScore *= BaseDecayRate;
            s.TransitionScore *= BaseDecayRate;
            s.AmbiguityScore *= BaseDecayRate;
        }

        private static void ApplySequenceBias(ApvaScores s, ApvaSequenceState sequence)
        {
            if (sequence == null)
                return;

            if (sequence.AuthorityScore >= 0.65)
                s.DominanceScore += 0.06;

            if (sequence.AuthorityScore >= 0.80)
                s.DominanceScore += 0.04;

            if (sequence.PeakRatio > 0.0 && sequence.PeakRatio < 0.90)
                s.DegradationScore += 0.05;

            if (sequence.Maturity == ApvaMaturityLevel.Mature ||
                sequence.Maturity == ApvaMaturityLevel.Late)
                s.DegradationScore += 0.02;
        }

        private static void ApplyEvent(ApvaScores s, ApvaEvent e)
        {
            s.DominanceScore += e.EffectOnDominance;
            s.DegradationScore += e.EffectOnDegradation;
            s.BalanceScore += e.EffectOnBalance;
            s.TransitionScore += e.EffectOnTransition;
            s.AmbiguityScore += e.EffectOnAmbiguity;

            if (e.EventType == ApvaEventType.FailedContinuation)
            {
                s.DegradationScore += 0.04;
                s.AmbiguityScore += 0.03;
            }

            if (e.EventType == ApvaEventType.DominanceReassertion)
            {
                s.DominanceScore += 0.12;
                s.DegradationScore -= 0.10;
                s.TransitionScore -= 0.10;
                s.AmbiguityScore -= 0.12;
                s.BalanceScore -= 0.08;
            }

            if (e.EventType == ApvaEventType.SFCandidate)
            {
                s.DegradationScore += 0.03;
                s.TransitionScore += 0.01;
                s.AmbiguityScore += 0.02;
            }

            if (e.EventType == ApvaEventType.LateralSeed)
            {
                s.DominanceScore -= 0.04;
            }
        }

        private static void ApplyInternalConflictRules(ApvaScores s)
        {
            if (s.BalanceScore >= 0.65)
            {
                s.DominanceScore *= 0.88;
                s.TransitionScore *= 0.96;
            }

            if (s.AmbiguityScore >= 0.65)
            {
                s.DominanceScore *= 0.86;
            }

            if (s.DominanceScore >= 0.70 && s.DegradationScore < 0.45)
            {
                s.AmbiguityScore *= 0.82;
                s.BalanceScore *= 0.90;
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
