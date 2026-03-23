namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualContainerBridge
    {
        private static ManualContainerAnalysis? latest;
        private static int version = 0;

        public static void Publish(ManualContainerAnalysis analysis)
        {
            latest = analysis;
            version++;
        }

        public static bool TryGetLatest(out ManualContainerAnalysis analysis, out int currentVersion)
        {
            currentVersion = version;

            if (latest.HasValue)
            {
                analysis = latest.Value;
                return true;
            }

            analysis = default;
            return false;
        }
    }
}

