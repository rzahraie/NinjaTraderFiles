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

            public bool PendingCandidate = false;
            public ContainerDirection PendingPriorDirection = ContainerDirection.Unknown;
            public int PendingPriorRunLength = 0;
            public int PendingBarIndex = -1;

            public int LastConfirmedFttBarIndex = -1000000;
        }

        private const int MinRunForCandidate = 4;
        private const int ConfirmWithinBars = 3;
        private const int CooldownBars = 8;

        public static ContainerEvent? Step(State s, in PriceCaseEvent priceCase)
        {
            ContainerDirection dir = MapDirection(priceCase.Case);

            if (dir == ContainerDirection.Unknown)
                return null;

            bool isNewContainer = false;

            DirectionBreakEvent? directionBreak = null;
            FttCandidateEvent? fttCandidate = null;
            FttConfirmedEvent? fttConfirmed = null;

            bool hasDirectionBreak = false;
            bool hasFttCandidate = false;
            bool hasFttConfirmed = false;

            if (!s.HasActiveContainer)
            {
                s.HasActiveContainer = true;
                s.CurrentDirection = dir;
                s.RunLength = 1;
                s.LastBarIndex = priceCase.BarIndex;
                s.PendingCandidate = false;
                isNewContainer = true;

                return new ContainerEvent(
                    priceCase.BarIndex,
                    s.CurrentDirection,
                    s.RunLength,
                    isNewContainer,
                    false,
                    null,
                    false,
                    null,
                    false,
                    null);
            }

            // Same direction: extend run and possibly confirm pending candidate.
            if (dir == s.CurrentDirection)
            {
                s.RunLength++;

                if (s.PendingCandidate)
				{
				    int barsSinceCandidate = priceCase.BarIndex - s.PendingBarIndex;
				    bool withinWindow = barsSinceCandidate <= ConfirmWithinBars;
				    bool cooldownOk = (priceCase.BarIndex - s.LastConfirmedFttBarIndex) >= CooldownBars;
				
				    // Require at least one full bar after the candidate bar.
				    bool hasFollowThrough = barsSinceCandidate >= 1;
				
				    if (withinWindow && cooldownOk && hasFollowThrough)
				    {
				        hasFttConfirmed = true;
				        fttConfirmed = new FttConfirmedEvent(
				            priceCase.BarIndex,
				            s.PendingPriorDirection,
				            priceCase.Case,
				            s.PendingPriorRunLength);
				
				        s.LastConfirmedFttBarIndex = priceCase.BarIndex;
				    }
				
				    s.PendingCandidate = false;
				}

                s.LastBarIndex = priceCase.BarIndex;

                return new ContainerEvent(
                    priceCase.BarIndex,
                    s.CurrentDirection,
                    s.RunLength,
                    false,
                    false,
                    null,
                    false,
                    null,
                    hasFttConfirmed,
                    fttConfirmed);
            }

            // Direction break
            hasDirectionBreak = true;
            directionBreak = new DirectionBreakEvent(
                priceCase.BarIndex,
                s.CurrentDirection,
                priceCase.Case,
                s.RunLength);

            // Candidate only if established run was long enough and cooldown passed
            if (s.RunLength >= MinRunForCandidate &&
                (priceCase.BarIndex - s.LastConfirmedFttBarIndex) >= CooldownBars)
            {
                hasFttCandidate = true;
                fttCandidate = new FttCandidateEvent(
                    priceCase.BarIndex,
                    s.CurrentDirection,
                    priceCase.Case,
                    s.RunLength);

                s.PendingCandidate = true;
                s.PendingPriorDirection = s.CurrentDirection;
                s.PendingPriorRunLength = s.RunLength;
                s.PendingBarIndex = priceCase.BarIndex;
            }
            else
            {
                s.PendingCandidate = false;
            }

            // Start opposite-direction run
            s.CurrentDirection = dir;
            s.RunLength = 1;
            s.LastBarIndex = priceCase.BarIndex;
            isNewContainer = true;

            return new ContainerEvent(
                priceCase.BarIndex,
                s.CurrentDirection,
                s.RunLength,
                isNewContainer,
                hasDirectionBreak,
                directionBreak,
                hasFttCandidate,
                fttCandidate,
                false,
                null);
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

