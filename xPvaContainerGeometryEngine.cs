using System;

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum GeometryState
    {
        Unknown = 0,
        SeekingP1 = 1,
        SeekingP2 = 2,
        SeekingP3 = 3,
        Active = 4,
        Broken = 5,
    }

    public readonly struct GeometryPoint
    {
        public readonly int BarIndex;
        public readonly double Price;

        public GeometryPoint(int barIndex, double price)
        {
            BarIndex = barIndex;
            Price = price;
        }
    }

    public readonly struct LineDef
    {
        public readonly GeometryPoint A;
        public readonly GeometryPoint B;
        public readonly double Slope;

        public LineDef(GeometryPoint a, GeometryPoint b)
        {
            A = a;
            B = b;

            int dx = b.BarIndex - a.BarIndex;
            Slope = dx == 0 ? 0.0 : (b.Price - a.Price) / dx;
        }

        public double ValueAt(int barIndex)
        {
            return A.Price + (barIndex - A.BarIndex) * Slope;
        }
    }

    public readonly struct ContainerGeometrySnapshot
    {
        public readonly int ContainerId;
        public readonly ContainerDirection Direction;
        public readonly GeometryState State;

        public readonly GeometryPoint? P1;
        public readonly GeometryPoint? P2;
        public readonly GeometryPoint? P3;

        public readonly LineDef? Rtl;
        public readonly LineDef? Ltl;

        public readonly double Ve1AtCurrent;
        public readonly double Ve2AtCurrent;
        public readonly int CurrentBarIndex;

        public ContainerGeometrySnapshot(
            int containerId,
            ContainerDirection direction,
            GeometryState state,
            GeometryPoint? p1,
            GeometryPoint? p2,
            GeometryPoint? p3,
            LineDef? rtl,
            LineDef? ltl,
            double ve1AtCurrent,
            double ve2AtCurrent,
            int currentBarIndex)
        {
            ContainerId = containerId;
            Direction = direction;
            State = state;
            P1 = p1;
            P2 = p2;
            P3 = p3;
            Rtl = rtl;
            Ltl = ltl;
            Ve1AtCurrent = ve1AtCurrent;
            Ve2AtCurrent = ve2AtCurrent;
            CurrentBarIndex = currentBarIndex;
        }
    }

    public sealed class xPvaContainerGeometryEngine
    {
        public sealed class State
        {
            public int ContainerId = -1;
            public ContainerDirection Direction = ContainerDirection.Unknown;
            public GeometryState GeometryState = GeometryState.SeekingP1;

            public GeometryPoint? P1;
            public GeometryPoint? P2;
            public GeometryPoint? P3;

            public LineDef? Rtl;
            public LineDef? Ltl;

            public bool HasSnapshot;
            public ContainerGeometrySnapshot LastSnapshot;

            public void ResetForContainer(int containerId, ContainerDirection direction)
            {
                ContainerId = containerId;
                Direction = direction;
                GeometryState = GeometryState.SeekingP1;

                P1 = null;
                P2 = null;
                P3 = null;
                Rtl = null;
                Ltl = null;

                HasSnapshot = false;
                LastSnapshot = default;
            }
        }

        public static ContainerGeometrySnapshot? Step(
            State s,
            in BarSnapshot bar,
            PersistentContainerEvent? persistent,
            FttConfirmedEvent? confirmed)
        {
            if (persistent.HasValue)
            {
                var pc = persistent.Value;

                if (s.ContainerId != pc.ContainerId)
                    s.ResetForContainer(pc.ContainerId, pc.Direction);

                UpdateGeometryState(s, bar, pc);
            }

            if (confirmed.HasValue && s.ContainerId == confirmed.Value.ContainerId)
            {
                // A confirmed FTT means the active container is no longer valid geometrically.
                s.GeometryState = GeometryState.Broken;
            }

            if (TryBuildSnapshot(s, bar, out ContainerGeometrySnapshot snapshot))
            {
                s.HasSnapshot = true;
                s.LastSnapshot = snapshot;
                return snapshot;
            }

            return null;
        }

        private static void UpdateGeometryState(State s, in BarSnapshot bar, in PersistentContainerEvent pc)
        {
            switch (s.GeometryState)
            {
                case GeometryState.SeekingP1:
                    BuildP1(s, bar, pc);
                    s.GeometryState = GeometryState.SeekingP2;
                    break;

                case GeometryState.SeekingP2:
                    UpdateP2Candidate(s, bar);
                    if (s.P2.HasValue && IsP2Established(s, pc))
                        s.GeometryState = GeometryState.SeekingP3;
                    break;

                case GeometryState.SeekingP3:
                    UpdateP3Candidate(s, bar);
                    if (s.P3.HasValue)
                    {
                        BuildLines(s);
                        if (s.Rtl.HasValue && s.Ltl.HasValue)
                            s.GeometryState = GeometryState.Active;
                    }
                    break;

                case GeometryState.Active:
                    MaintainActiveGeometry(s, bar);
                    break;

                case GeometryState.Broken:
                    break;
            }
        }

        private static void BuildP1(State s, in BarSnapshot bar, in PersistentContainerEvent pc)
        {
            double p1Price = s.Direction == ContainerDirection.Up ? bar.L : bar.H;
            s.P1 = new GeometryPoint(pc.StartBarIndex, p1Price);
        }

        private static void UpdateP2Candidate(State s, in BarSnapshot bar)
        {
            if (!s.P1.HasValue)
                return;

            double candidate = s.Direction == ContainerDirection.Up ? bar.H : bar.L;

            if (!s.P2.HasValue)
            {
                s.P2 = new GeometryPoint(bar.Index, candidate);
                return;
            }

            if (s.Direction == ContainerDirection.Up)
            {
                if (candidate >= s.P2.Value.Price)
                    s.P2 = new GeometryPoint(bar.Index, candidate);
            }
            else
            {
                if (candidate <= s.P2.Value.Price)
                    s.P2 = new GeometryPoint(bar.Index, candidate);
            }
        }

        private static bool IsP2Established(State s, in PersistentContainerEvent pc)
        {
            if (!s.P2.HasValue)
                return false;

            // Conservative scaffold:
            // accept P2 once it is at least 2 bars away from P1
            return (s.P2.Value.BarIndex - s.P1.Value.BarIndex) >= 2;
        }

        private static void UpdateP3Candidate(State s, in BarSnapshot bar)
        {
            if (!s.P1.HasValue || !s.P2.HasValue)
                return;

            double candidate = s.Direction == ContainerDirection.Up ? bar.L : bar.H;

            if (!s.P3.HasValue)
            {
                // only accept p3 after p2
                if (bar.Index > s.P2.Value.BarIndex)
                    s.P3 = new GeometryPoint(bar.Index, candidate);
                return;
            }

            if (s.Direction == ContainerDirection.Up)
            {
                // p3 is a higher low relative to p1 but after p2
                bool valid = bar.Index > s.P2.Value.BarIndex &&
                             candidate >= s.P1.Value.Price;
                if (valid && candidate <= s.P3.Value.Price)
                    s.P3 = new GeometryPoint(bar.Index, candidate);
            }
            else
            {
                // p3 is a lower high relative to p1 but after p2
                bool valid = bar.Index > s.P2.Value.BarIndex &&
                             candidate <= s.P1.Value.Price;
                if (valid && candidate >= s.P3.Value.Price)
                    s.P3 = new GeometryPoint(bar.Index, candidate);
            }
        }

        private static void BuildLines(State s)
        {
            if (!s.P1.HasValue || !s.P2.HasValue || !s.P3.HasValue)
                return;

            s.Rtl = new LineDef(s.P1.Value, s.P3.Value);

            // LTL starts from P2, parallel to RTL
            GeometryPoint p2 = s.P2.Value;
            double rtlSlope = s.Rtl.Value.Slope;

            GeometryPoint ltlB = new GeometryPoint(
                p2.BarIndex + 1,
                p2.Price + rtlSlope);

            s.Ltl = new LineDef(p2, ltlB);
        }

        private static void MaintainActiveGeometry(State s, in BarSnapshot bar)
        {
            if (!s.Rtl.HasValue || !s.Ltl.HasValue)
                return;

            double ltlNow = s.Ltl.Value.ValueAt(bar.Index);

            bool brokeLtl =
                s.Direction == ContainerDirection.Up
                    ? bar.L < ltlNow
                    : bar.H > ltlNow;

            if (brokeLtl)
            {
                s.GeometryState = GeometryState.Broken;
                return;
            }

            // Optional refinement:
            // If trend continues strongly, allow P2/LTL to step outward later.
            // Not implemented yet; keep scaffold deterministic and conservative.
        }

        private static bool TryBuildSnapshot(
            State s,
            in BarSnapshot bar,
            out ContainerGeometrySnapshot snapshot)
        {
            snapshot = default;

            if (!s.P1.HasValue)
                return false;

            double ve1 = double.NaN;
            double ve2 = double.NaN;

            if (s.Ltl.HasValue && s.Rtl.HasValue)
            {
                double width = s.Direction == ContainerDirection.Up
                    ? s.P2.Value.Price - s.Rtl.Value.ValueAt(s.P2.Value.BarIndex)
                    : s.Rtl.Value.ValueAt(s.P2.Value.BarIndex) - s.P2.Value.Price;

                double ltlNow = s.Ltl.Value.ValueAt(bar.Index);

                if (s.Direction == ContainerDirection.Up)
                {
                    ve1 = ltlNow + width;
                    ve2 = ltlNow + 2.0 * width;
                }
                else
                {
                    ve1 = ltlNow - width;
                    ve2 = ltlNow - 2.0 * width;
                }
            }

            snapshot = new ContainerGeometrySnapshot(
                s.ContainerId,
                s.Direction,
                s.GeometryState,
                s.P1,
                s.P2,
                s.P3,
                s.Rtl,
                s.Ltl,
                ve1,
                ve2,
                bar.Index);

            return true;
        }
    }
}