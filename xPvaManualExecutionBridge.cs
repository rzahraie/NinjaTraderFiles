namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualExecutionBridge
    {
        private static ManualExecutionSnapshot? latest;
        private static int version = 0;

        public static void Publish(ManualExecutionSnapshot snapshot)
        {
            latest = snapshot;
            version++;
        }

        public static bool TryGetLatest(out ManualExecutionSnapshot snapshot, out int currentVersion)
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