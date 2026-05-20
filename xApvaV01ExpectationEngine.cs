namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01ExpectationEngine
    {
        public void ApplyExpectations(ApvaStateSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            switch (snapshot.MacroState)
            {
                case ApvaMacroState.Directional:
                    ApplyDirectionalExpectation(snapshot);
                    break;

                case ApvaMacroState.Degrading:
                    ApplyDegradingExpectation(snapshot);
                    break;

                case ApvaMacroState.Balance:
                    ApplyBalanceExpectation(snapshot);
                    break;

                case ApvaMacroState.TransitionAttempt:
                    ApplyTransitionExpectation(snapshot);
                    break;

                case ApvaMacroState.Unresolved:
                    ApplyUnresolvedExpectation(snapshot);
                    break;

                default:
                    ApplyUnknownExpectation(snapshot);
                    break;
            }
        }

        private static void ApplyDirectionalExpectation(ApvaStateSnapshot s)
        {
            s.ExpectedNextBehavior =
                "Continuation or shallow non-dominant retrace expected.";

            s.InvalidationCondition =
                "Invalidated by failed continuation, rising overlap, or opposing authority.";
        }

        private static void ApplyDegradingExpectation(ApvaStateSnapshot s)
        {
            s.ExpectedNextBehavior =
                "Dominance still exists, but efficiency is degrading. Watch for balance or failed continuation.";

            s.InvalidationCondition =
                "Invalidated by clean dominance reassertion with efficient continuation.";
        }

        private static void ApplyBalanceExpectation(ApvaStateSnapshot s)
        {
            s.ExpectedNextBehavior =
                "Balance/lateral behavior active. Directional inference unreliable until accepted breakout.";

            s.InvalidationCondition =
                "Invalidated by accepted breakout followed by efficient continuation.";
        }

        private static void ApplyTransitionExpectation(ApvaStateSnapshot s)
        {
            s.ExpectedNextBehavior =
                "Transition attempt active. Prior dominance must fail reclaim before transfer is confirmed.";

            s.InvalidationCondition =
                "Invalidated by efficient reclaim by prior dominant side.";
        }

        private static void ApplyUnresolvedExpectation(ApvaStateSnapshot s)
        {
            s.ExpectedNextBehavior =
                "Auction unresolved. Ambiguity high. Avoid strong directional inference.";

            s.InvalidationCondition =
                "Resolved by accepted breakout, failed transition, or clear dominance reassertion.";
        }

        private static void ApplyUnknownExpectation(ApvaStateSnapshot s)
        {
            s.ExpectedNextBehavior =
                "Insufficient structure. Continue monitoring.";

            s.InvalidationCondition =
                "N/A";
        }
    }
}