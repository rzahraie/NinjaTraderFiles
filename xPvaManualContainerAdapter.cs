namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualContainerAdapter
    {
        public static ContainerGeometrySnapshot Adapt(in ManualContainerSnapshot m, int currentBarIndex)
        {
            var p1 = new GeometryPoint(m.P1.BarIndex, m.P1.Price);
            var p2 = new GeometryPoint(m.P2.BarIndex, m.P2.Price);
            var p3 = new GeometryPoint(m.P3.BarIndex, m.P3.Price);

            var rtl = new LineDef(p1, p3);

            GeometryPoint ltlB = new GeometryPoint(
                p2.BarIndex + 1,
                p2.Price + m.LtlSlope);

            var ltl = new LineDef(p2, ltlB);

            double rtlAtP2 = rtl.ValueAt(p2.BarIndex);
            double width = System.Math.Abs(p2.Price - rtlAtP2);

            double ltlNow = ltl.ValueAt(currentBarIndex);

            double ve1 = m.IsUpContainer
                ? ltlNow + width
                : ltlNow - width;

            double ve2 = m.IsUpContainer
                ? ltlNow + 2.0 * width
                : ltlNow - 2.0 * width;

            return new ContainerGeometrySnapshot(
                m.ContainerId,
                m.IsUpContainer ? ContainerDirection.Up : ContainerDirection.Down,
                GeometryState.Active,
                p1,
                p2,
                p3,
                rtl,
                ltl,
                ve1,
                ve2,
                currentBarIndex);
        }
    }
}