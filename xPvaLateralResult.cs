namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public readonly struct xPvaLateralResult
    {
        public readonly LateralStateKind State;
        public readonly LateralBias Bias;
        public readonly double High;
        public readonly double Low;
        public readonly int StartBarIndex;
        public readonly int BarsInState;

        public xPvaLateralResult(
            LateralStateKind state,
            LateralBias bias,
            double high,
            double low,
            int startBarIndex,
            int barsInState)
        {
            State = state;
            Bias = bias;
            High = high;
            Low = low;
            StartBarIndex = startBarIndex;
            BarsInState = barsInState;
        }
    }
}