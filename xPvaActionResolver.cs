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

        public static ActionEvent? Step(
            State s,
            in StructureEvent structure,
            in TurnEvent turn)
        {
            ActionType action = Resolve(s, structure, turn);

            if (action == ActionType.Unknown)
                return null;

            if (s.LastBarIndex == structure.BarIndex && s.LastAction == action)
                return null;

            s.LastBarIndex = structure.BarIndex;
            s.LastAction = action;

            switch (action)
            {
                case ActionType.Enter:
                case ActionType.Reverse:
                case ActionType.StayIn:
                case ActionType.Hold:
                    s.InPosition = true;
                    break;

                case ActionType.Sideline:
                    s.InPosition = false;
                    break;
            }

            return new ActionEvent(
				    structure.BarIndex,
				    structure.ContainerId,
				    action,
				    structure.TrendType,
				    turn.Type,
				    turn.SourceKind,
				    turn.Band,
				    turn.Value);
        }

        private static ActionType Resolve(
            State s,
            in StructureEvent structure,
            in TurnEvent turn)
        {
            // Structure-aware deterministic bridge.
            // Still not full move-reversal table, but much less noisy.

            switch (structure.State)
            {
                case StructureState.Building:
                    if (!s.InPosition)
                        return ActionType.Enter;

                    if (turn.Type == TurnType.A)
                        return ActionType.Hold;

                    return ActionType.StayIn;

                case StructureState.Transition:
                    if (s.InPosition)
                        return ActionType.Reverse;

                    return ActionType.Enter;

                case StructureState.Mature:
                    if (s.InPosition)
                    {
                        if (turn.Type == TurnType.B)
                            return ActionType.StayIn;

                        return ActionType.Hold;
                    }

                    return ActionType.Enter;

                case StructureState.Broken:
                    return ActionType.Sideline;
            }

            return ActionType.Unknown;
        }
    }
}


