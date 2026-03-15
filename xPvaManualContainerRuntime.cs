namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaManualContainerRuntime
    {
        public sealed class State
        {
            public bool HasActiveManualContainer = false;
            public ContainerGeometrySnapshot ActiveContainer;

            public bool FttCandidateIssued = false;
            public bool FttConfirmedIssued = false;
        }

        public static void LoadManualContainer(
            State s,
            in ContainerGeometrySnapshot snapshot)
        {
            s.ActiveContainer = snapshot;
            s.HasActiveManualContainer = true;
            s.FttCandidateIssued = false;
            s.FttConfirmedIssued = false;
        }

        public static FttCandidateEvent? CheckFttCandidate(
            State s,
            in BarSnapshot bar)
        {
            if (!s.HasActiveManualContainer)
                return null;

            if (!s.ActiveContainer.Ltl.HasValue)
                return null;

            if (s.FttCandidateIssued)
                return null;

            double ltlNow = s.ActiveContainer.Ltl.Value.ValueAt(bar.Index);

			bool broke =
			    s.ActiveContainer.Direction == ContainerDirection.Up
			        ? bar.L < ltlNow
			        : bar.H > ltlNow;

            if (!broke)
                return null;

            s.FttCandidateIssued = true;

            return new FttCandidateEvent(
                bar.Index,
                s.ActiveContainer.ContainerId,
                s.ActiveContainer.Direction,
                PriceCase.Unknown,
                0);
        }

        public static FttConfirmedEvent? CheckFttConfirmed(
            State s,
            in BarSnapshot bar)
        {
            if (!s.HasActiveManualContainer)
                return null;

            if (!s.FttCandidateIssued || s.FttConfirmedIssued)
                return null;

            s.FttConfirmedIssued = true;

            return new FttConfirmedEvent(
                bar.Index,
                s.ActiveContainer.ContainerId,
                s.ActiveContainer.Direction,
                PriceCase.Unknown,
                0);
        }
    }
}



