#region Using declarations
using System;
using System.Globalization;
using NinjaTrader.NinjaScript.APVA.V01;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public sealed class xApvaV01SessionStats
    {
        public int TotalBars;

        public int DirectionalBars;
        public int DegradingBars;
        public int BalanceBars;
        public int UnresolvedBars;
        public int UnknownBars;

        public int AcceptedReclaimCount;
        public int RejectedReclaimCount;
        public int FailedContinuationCount;
        public int PeakVolumeCount;
        public int LateralSeedCount;

        public void Accumulate(ApvaStateSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            TotalBars++;

            switch (snapshot.MacroState)
            {
                case ApvaMacroState.Directional:
                    DirectionalBars++;
                    break;

                case ApvaMacroState.Degrading:
                    DegradingBars++;
                    break;

                case ApvaMacroState.Balance:
                    BalanceBars++;
                    break;

                case ApvaMacroState.Unresolved:
                    UnresolvedBars++;
                    break;

                case ApvaMacroState.Unknown:
                default:
                    UnknownBars++;
                    break;
            }

            if (snapshot.Events == null)
                return;

            foreach (ApvaEvent evt in snapshot.Events)
            {
				if (evt == null)
        			continue;
				
                switch (evt.EventType)
                {
                    case ApvaEventType.AcceptedReclaim:
                        AcceptedReclaimCount++;
                        break;

                    case ApvaEventType.RejectedReclaim:
                        RejectedReclaimCount++;
                        break;

                    case ApvaEventType.FailedContinuation:
                        FailedContinuationCount++;
                        break;

                    case ApvaEventType.PeakVolume:
                        PeakVolumeCount++;
                        break;

                    case ApvaEventType.LateralSeed:
                        LateralSeedCount++;
                        break;
                }
            }
        }

		public string ToCsvSummary(
		    string instrument,
		    string sessionContext)
		{
		    return string.Format(
		        CultureInfo.InvariantCulture,
		        "{0},{1},{2},{3:F1},{4:F1},{5:F1},{6:F1},{7:F1},{8},{9},{10},{11},{12}",
		        instrument,
		        sessionContext,
		        TotalBars,
		        Percent(DirectionalBars),
		        Percent(DegradingBars),
		        Percent(BalanceBars),
		        Percent(UnresolvedBars),
		        Percent(UnknownBars),
		        AcceptedReclaimCount,
		        RejectedReclaimCount,
		        FailedContinuationCount,
		        PeakVolumeCount,
		        LateralSeedCount);
}

        public string ToSummaryString(
            string instrument,
            string sessionContext)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}\n" +
                "TotalBars: {2}\n\n" +
                "Directional: {3:F1}%\n" +
                "Degrading: {4:F1}%\n" +
                "Balance: {5:F1}%\n" +
                "Unresolved: {6:F1}%\n" +
                "Unknown: {7:F1}%\n\n" +
                "AcceptedReclaim: {8}\n" +
                "RejectedReclaim: {9}\n" +
                "FailedContinuation: {10}\n" +
                "PeakVolume: {11}\n" +
                "LateralSeed: {12}",
                string.IsNullOrEmpty(instrument) ? "Unknown" : instrument,
                string.IsNullOrEmpty(sessionContext) ? "UnknownSession" : sessionContext,
                TotalBars,
                Percent(DirectionalBars),
                Percent(DegradingBars),
                Percent(BalanceBars),
                Percent(UnresolvedBars),
                Percent(UnknownBars),
                AcceptedReclaimCount,
                RejectedReclaimCount,
                FailedContinuationCount,
                PeakVolumeCount,
                LateralSeedCount);
        }

        private double Percent(int count)
        {
            if (TotalBars <= 0)
                return 0.0;

            return 100.0 * count / TotalBars;
        }
    }
}



