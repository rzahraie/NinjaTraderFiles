namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaActionResolver
    {
        public sealed class State
        {
            public ActionType LastAction = ActionType.Unknown;
            public int LastBarIndex = -1;
            public bool InPosition = false;
        }

        public static ActionEvent? Step(State s, in TrendTypeEvent trend)
        {
            ActionType action = Resolve(s, trend);

            if (action == ActionType.Unknown)
                return null;

            if (s.LastBarIndex == trend.BarIndex && s.LastAction == action)
                return null;

            s.LastBarIndex = trend.BarIndex;
            s.LastAction = action;

            if (action == ActionType.Enter || action == ActionType.Reverse || action == ActionType.StayIn)
                s.InPosition = true;
            else if (action == ActionType.Sideline)
                s.InPosition = false;

            return new ActionEvent(
                trend.BarIndex,
                action,
                trend.Type,
                trend.SourceTurn,
                trend.SourceKind,
                trend.Band,
                trend.Value);
        }

        private static ActionType Resolve(State s, in TrendTypeEvent trend)
        {
            // Minimal deterministic bridge only.
            // Not full move-reversal table.

            if (!s.InPosition)
            {
                if (trend.Type == TrendType.A || trend.Type == TrendType.B)
                    return ActionType.Enter;

                if (trend.Type == TrendType.C)
                    return ActionType.Sideline;
            }
            else
            {
                if (trend.Type == TrendType.A)
                    return ActionType.Hold;

                if (trend.Type == TrendType.B)
                    return ActionType.StayIn;

                if (trend.Type == TrendType.C)
                    return ActionType.Reverse;

                if (trend.Type == TrendType.D)
                    return ActionType.Sideline;
            }

            return ActionType.Unknown;
        }
    }
}