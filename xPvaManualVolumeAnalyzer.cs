namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum ManualVolumeLabel
    {
        Unknown = 0,
        Peak = 1,
        Trough = 2,
        P1 = 3,
        PP1 = 4
    }

    public readonly struct ManualVolumeEvent
    {
        public readonly int BarIndex;
        public readonly ManualVolumeLabel Label;
        public readonly long Volume;

        public ManualVolumeEvent(
            int barIndex,
            ManualVolumeLabel label,
            long volume)
        {
            BarIndex = barIndex;
            Label = label;
            Volume = volume;
        }
    }

    public static class xPvaManualVolumeAnalyzer
    {
        public static ManualVolumeEvent[] Analyze(
            int startBar,
            int endBar,
            System.Func<int, long> getVolume)
        {
            if (endBar - startBar < 2)
                return new ManualVolumeEvent[0];

            var results = new System.Collections.Generic.List<ManualVolumeEvent>();

            for (int idx = startBar + 1; idx < endBar; idx++)
            {
                long vPrev = getVolume(idx - 1);
                long vCur = getVolume(idx);
                long vNext = getVolume(idx + 1);

                if (vCur > vPrev && vCur > vNext)
                    results.Add(new ManualVolumeEvent(idx, ManualVolumeLabel.Peak, vCur));
                else if (vCur < vPrev && vCur < vNext)
                    results.Add(new ManualVolumeEvent(idx, ManualVolumeLabel.Trough, vCur));
            }

            if (results.Count > 0)
            {
                var first = results[0];
                results[0] = new ManualVolumeEvent(first.BarIndex, ManualVolumeLabel.P1, first.Volume);

                if (results.Count > 1)
                {
                    var second = results[1];
                    results[1] = new ManualVolumeEvent(second.BarIndex, ManualVolumeLabel.PP1, second.Volume);
                }
            }

            return results.ToArray();
        }
    }
}