namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualContainerBridge
    {
        private static ManualContainerSnapshot? latest;

        public static void Publish(ManualContainerSnapshot snapshot)
        {
            latest = snapshot;
        }

        public static bool TryConsume(out ManualContainerSnapshot snapshot)
        {
            if (latest.HasValue)
            {
                snapshot = latest.Value;
                latest = null;
                return true;
            }

            snapshot = default;
            return false;
        }
    }
}