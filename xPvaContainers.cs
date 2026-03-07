namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaContainers
    {
        public sealed class State
        {
            public ContainerDirection CurrentDirection = ContainerDirection.Unknown;
            public int RunLength = 0;
            public int LastBarIndex = -1;
            public bool HasActiveContainer = false;
            public bool FttTriggeredInCurrentContainer = false;
        }

        public static ContainerEvent? Step(State s, in PriceCaseEvent priceCase)
        {
            ContainerDirection dir = MapDirection(priceCase.Case);

            if (dir == ContainerDirection.Unknown)
                return null;

            bool isNewContainer = false;
            FttEvent? ftt = null;
            bool hasFtt = false;

            if (!s.HasActiveContainer)
            {
                s.HasActiveContainer = true;
                s.CurrentDirection = dir;
                s.RunLength = 1;
                s.FttTriggeredInCurrentContainer = false;
                s.LastBarIndex = priceCase.BarIndex;
                isNewContainer = true;

                return new ContainerEvent(
                    priceCase.BarIndex,
                    s.CurrentDirection,
                    s.RunLength,
                    isNewContainer,
                    false,
                    null);
            }

            if (dir == s.CurrentDirection)
            {
                s.RunLength++;
                s.LastBarIndex = priceCase.BarIndex;

                return new ContainerEvent(
                    priceCase.BarIndex,
                    s.CurrentDirection,
                    s.RunLength,
                    false,
                    false,
                    null);
            }

            // Direction break.
            // Minimal provisional FTT rule:
            // if we had an established run (>= 2) and no FTT yet, emit FTT.
            if (s.RunLength >= 2 && !s.FttTriggeredInCurrentContainer)
            {
                hasFtt = true;
                ftt = new FttEvent(priceCase.BarIndex, s.CurrentDirection, priceCase.Case);
                s.FttTriggeredInCurrentContainer = true;
            }

            // Start new container in opposite direction immediately after break.
            s.CurrentDirection = dir;
            s.RunLength = 1;
            s.LastBarIndex = priceCase.BarIndex;
            s.HasActiveContainer = true;
            s.FttTriggeredInCurrentContainer = false;
            isNewContainer = true;

            return new ContainerEvent(
                priceCase.BarIndex,
                s.CurrentDirection,
                s.RunLength,
                isNewContainer,
                hasFtt,
                ftt);
        }

        private static ContainerDirection MapDirection(PriceCase pc)
        {
            if (pc == PriceCase.XB)
                return ContainerDirection.Up;

            if (pc == PriceCase.XR)
                return ContainerDirection.Down;

            return ContainerDirection.Unknown;
        }
    }
}