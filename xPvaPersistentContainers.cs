using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaPersistentContainers
    {
        private sealed class PersistentContainer
        {
            public int ContainerId;
            public ContainerLifecycleState LifecycleState;
            public ContainerDirection Direction;

            public int StartBarIndex;
            public int LastBarIndex;

            public int ExtremeBarIndex;
            public double ExtremePrice;

            public int? BreakBarIndex;
            public int? ConfirmBarIndex;
        }

        public sealed class State
        {
            private readonly Dictionary<int, PersistentContainer> containers = new Dictionary<int, PersistentContainer>();

            public PersistentContainerEvent? OnContainer(in ContainerEvent e, in BarSnapshot bar)
            {
                if (!containers.TryGetValue(e.ContainerId, out PersistentContainer c))
                {
                    c = new PersistentContainer
                    {
                        ContainerId = e.ContainerId,
                        LifecycleState = ContainerLifecycleState.Developing,
                        Direction = e.Direction,
                        StartBarIndex = e.BarIndex,
                        LastBarIndex = e.BarIndex,
                        ExtremeBarIndex = e.BarIndex,
                        ExtremePrice = e.Direction == ContainerDirection.Up ? bar.H : bar.L,
                        BreakBarIndex = null,
                        ConfirmBarIndex = null
                    };

                    containers[e.ContainerId] = c;

                    return ToEvent(c);
                }

                c.LastBarIndex = e.BarIndex;

                double candidateExtreme = c.Direction == ContainerDirection.Up ? bar.H : bar.L;
                bool improved =
                    (c.Direction == ContainerDirection.Up && candidateExtreme >= c.ExtremePrice) ||
                    (c.Direction == ContainerDirection.Down && candidateExtreme <= c.ExtremePrice);

                if (improved)
                {
                    c.ExtremePrice = candidateExtreme;
                    c.ExtremeBarIndex = e.BarIndex;
                }

                return ToEvent(c);
            }

            public PersistentContainerEvent? OnDirectionBreak(in DirectionBreakEvent e)
            {
                if (!containers.TryGetValue(e.ContainerId, out PersistentContainer c))
                    return null;

                c.LifecycleState = ContainerLifecycleState.CandidateBreak;
                c.BreakBarIndex = e.BarIndex;
                c.LastBarIndex = e.BarIndex;

                return ToEvent(c);
            }

            public PersistentContainerEvent? OnFttConfirmed(in FttConfirmedEvent e)
            {
                if (!containers.TryGetValue(e.ContainerId, out PersistentContainer c))
                    return null;

                c.LifecycleState = ContainerLifecycleState.ConfirmedBreak;
                c.ConfirmBarIndex = e.BarIndex;
                c.LastBarIndex = e.BarIndex;

                return ToEvent(c);
            }

            public PersistentContainerEvent? OnAction(in ActionEvent e)
            {
                if (!containers.TryGetValue(e.ContainerId, out PersistentContainer c))
                    return null;

                c.LastBarIndex = e.BarIndex;

                if (e.Action == ActionType.Enter || e.Action == ActionType.Reverse || e.Action == ActionType.Sideline)
                    c.LifecycleState = ContainerLifecycleState.Closed;

                return ToEvent(c);
            }

            private static PersistentContainerEvent ToEvent(PersistentContainer c)
            {
                return new PersistentContainerEvent(
                    c.ContainerId,
                    c.LifecycleState,
                    c.Direction,
                    c.StartBarIndex,
                    c.LastBarIndex,
                    c.ExtremeBarIndex,
                    c.ExtremePrice,
                    c.BreakBarIndex,
                    c.ConfirmBarIndex);
            }
        }
    }
}