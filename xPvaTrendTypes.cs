namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaTrendTypes
    {
        public sealed class State
        {
            public TrendType LastType = TrendType.Unknown;
            public int LastBarIndex = -1;
            public int MatureCount = 0;
        }

        public static TrendTypeEvent? Step(State s, in TurnEvent turn)
        {
            TrendType type = Classify(s, turn);

            if (type == TrendType.Unknown)
                return null;

            if (s.LastBarIndex == turn.BarIndex && s.LastType == type)
                return null;

            s.LastType = type;
            s.LastBarIndex = turn.BarIndex;

            if (type == TrendType.C || type == TrendType.D)
                s.MatureCount++;
            else
                s.MatureCount = 0;

            return new TrendTypeEvent(
                turn.BarIndex,
                type,
                turn.Type,
                turn.SourceKind,
                turn.Band,
                turn.Value);
        }

        private static TrendType Classify(State s, in TurnEvent turn)
        {
            // Minimal deterministic bridge, not full Hershey trend typing.

            if (turn.Type == TurnType.A)
                return TrendType.A;

            if (turn.Type == TurnType.B)
                return TrendType.B;

            if (turn.Type == TurnType.C)
            {
                if (s.MatureCount >= 1)
                    return TrendType.D;

                return TrendType.C;
            }

            return TrendType.Unknown;
        }
    }
}