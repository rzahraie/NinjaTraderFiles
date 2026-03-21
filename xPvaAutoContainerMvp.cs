namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaAutoContainerMvp
    {
        public sealed class State
        {
            public bool IsActive = false;
            public bool IsUp = true;
            public int ContainerId = 0;

            public int P1Bar = -1;
            public int P2Bar = -1;
            public int P3Bar = -1;
        }

        public static ContainerGeometrySnapshot? Step(
            State s,
            in BarSnapshot prevBar,
            in BarSnapshot bar)
        {
            bool isHHHL = bar.H > prevBar.H && bar.L > prevBar.L;
            bool isLHLL = bar.H < prevBar.H && bar.L < prevBar.L;

            if (!s.IsActive)
            {
                if (isHHHL)
                {
                    s.IsActive = true;
                    s.IsUp = true;
                    s.ContainerId++;
                    s.P1Bar = prevBar.Index;
                    s.P2Bar = bar.Index;
                    s.P3Bar = bar.Index;
                }
                else if (isLHLL)
                {
                    s.IsActive = true;
                    s.IsUp = false;
                    s.ContainerId++;
                    s.P1Bar = prevBar.Index;
                    s.P2Bar = bar.Index;
                    s.P3Bar = bar.Index;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                s.P3Bar = bar.Index;
            }

            if (s.P1Bar < 0 || s.P2Bar < 0 || s.P3Bar < 0 || s.P3Bar <= s.P1Bar)
                return null;

            double p1Price = s.IsUp ? prevBar.L : prevBar.H;
            double p2Price = s.IsUp ? bar.H : bar.L;
            double p3Price = s.IsUp ? bar.L : bar.H;

            var p1 = new GeometryPoint(s.P1Bar, p1Price);
            var p2 = new GeometryPoint(s.P2Bar, p2Price);
            var p3 = new GeometryPoint(s.P3Bar, p3Price);

            var rtl = new LineDef(p1, p3);

            var ltlB = new GeometryPoint(
                p2.BarIndex + 1,
                p2.Price + rtl.Slope);

            var ltl = new LineDef(p2, ltlB);

            double rtlAtP2 = rtl.ValueAt(p2.BarIndex);
            double width = System.Math.Abs(p2.Price - rtlAtP2);

            double ltlNow = ltl.ValueAt(bar.Index);
            double ve1 = s.IsUp ? ltlNow + width : ltlNow - width;
            double ve2 = s.IsUp ? ltlNow + 2.0 * width : ltlNow - 2.0 * width;

            return new ContainerGeometrySnapshot(
                s.ContainerId,
                s.IsUp ? ContainerDirection.Up : ContainerDirection.Down,
                GeometryState.Active,
                p1,
                p2,
                p3,
                rtl,
                ltl,
                ve1,
                ve2,
                bar.Index);
        }
    }
}