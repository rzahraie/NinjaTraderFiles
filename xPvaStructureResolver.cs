namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaStructureResolver
    {
        public sealed class State
        {
            public StructureState LastState = StructureState.Unknown;
            public int LastBarIndex = -1;
            public ContainerDirection LastDirection = ContainerDirection.Unknown;
        }

        public static StructureEvent? Step(
            State s,
            in FttConfirmedEvent ftt,
            in TrendTypeEvent trend)
        {
            StructureState state = Classify(ftt, trend);

            if (state == StructureState.Unknown)
                return null;

            ContainerDirection direction = Opposite(ftt.PriorDirection);

            if (s.LastBarIndex == ftt.BarIndex &&
                s.LastState == state &&
                s.LastDirection == direction)
            {
                return null;
            }

            s.LastBarIndex = ftt.BarIndex;
            s.LastState = state;
            s.LastDirection = direction;

            return new StructureEvent(
			    ftt.BarIndex,
			    ftt.ContainerId,
			    state,
			    trend.Type,
			    direction);
        }

        private static StructureState Classify(
            in FttConfirmedEvent ftt,
            in TrendTypeEvent trend)
        {
            switch (trend.Type)
            {
                case TrendType.A:
                    return StructureState.Building;

                case TrendType.B:
                    return StructureState.Mature;

                case TrendType.C:
                    return StructureState.Transition;

                case TrendType.D:
                    return StructureState.Broken;
            }

            return StructureState.Unknown;
        }

        private static ContainerDirection Opposite(ContainerDirection dir)
        {
            if (dir == ContainerDirection.Up)
                return ContainerDirection.Down;

            if (dir == ContainerDirection.Down)
                return ContainerDirection.Up;

            return ContainerDirection.Unknown;
        }
    }
}


