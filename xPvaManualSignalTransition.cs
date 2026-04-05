namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum ManualSignalTransition
    {
        Unknown = 0,
        EarlyValid = 1,
        StableValid = 2,
        Degrading = 3,
        Invalidated = 4
    }
}