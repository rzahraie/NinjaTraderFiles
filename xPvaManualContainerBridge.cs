namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualContainerBridge
    {
        private static ManualContainerSnapshot? latest;
        private static int version = 0;

        public static void Publish(ManualContainerSnapshot snapshot)
        {
            latest = snapshot;
            version++;
        }

        public static bool TryGetLatest(out ManualContainerSnapshot snapshot, out int currentVersion)
        {
            currentVersion = version;

            if (latest.HasValue)
            {
                snapshot = latest.Value;
                return true;
            }

            snapshot = default;
            return false;
        }
    }
}
