using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaContainerGeometry
    {
        private sealed class GeometryRecord
        {
            public int ContainerId;
            public ContainerDirection Direction;

            public int StartBarIndex;
            public int ExtremeBarIndex;
            public int ConfirmBarIndex;

            public double StartPrice;
            public double ExtremePrice;
            public double ConfirmPrice;
        }

        public sealed class State
        {
            private readonly Dictionary<int, GeometryRecord> records = new Dictionary<int, GeometryRecord>();

            public void OnPersistentContainer(in PersistentContainerEvent e)
            {
                if (!records.TryGetValue(e.ContainerId, out GeometryRecord r))
                {
                    r = new GeometryRecord
                    {
                        ContainerId = e.ContainerId,
                        Direction = e.Direction,
                        StartBarIndex = e.StartBarIndex,
                        ExtremeBarIndex = e.ExtremeBarIndex,
                        ConfirmBarIndex = e.ConfirmBarIndex ?? e.LastBarIndex,
                        StartPrice = 0.0,
                        ExtremePrice = e.ExtremePrice,
                        ConfirmPrice = 0.0
                    };
                    records[e.ContainerId] = r;
                }
                else
                {
                    r.Direction = e.Direction;
                    r.StartBarIndex = e.StartBarIndex;
                    r.ExtremeBarIndex = e.ExtremeBarIndex;
                    r.ExtremePrice = e.ExtremePrice;
                    r.ConfirmBarIndex = e.ConfirmBarIndex ?? e.LastBarIndex;
                }
            }

            public ContainerGeometryEvent? OnAction(
                in ActionEvent action,
                IReadOnlyDictionary<int, BarSnapshot> barsByIndex)
            {
                if (!records.TryGetValue(action.ContainerId, out GeometryRecord r))
                    return null;

                if (!barsByIndex.TryGetValue(r.StartBarIndex, out BarSnapshot startBar))
                    return null;

                if (!barsByIndex.TryGetValue(r.ConfirmBarIndex, out BarSnapshot confirmBar))
                    return null;

                r.StartPrice = PriceForDirection(r.Direction, startBar);
                r.ConfirmPrice = PriceForDirection(r.Direction, confirmBar);

                return new ContainerGeometryEvent(
                    r.ContainerId,
                    r.Direction,
                    r.StartBarIndex,
                    r.ExtremeBarIndex,
                    r.ConfirmBarIndex,
                    r.StartPrice,
                    r.ExtremePrice,
                    r.ConfirmPrice);
            }

            private static double PriceForDirection(ContainerDirection dir, in BarSnapshot bar)
            {
                if (dir == ContainerDirection.Up)
                    return bar.H;

                if (dir == ContainerDirection.Down)
                    return bar.L;

                return bar.C;
            }
        }
    }
}