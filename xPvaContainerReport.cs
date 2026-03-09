using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaContainerReport
    {
        private sealed class ContainerRecord
        {
            public int ContainerId;
            public int StartBarIndex;
            public int? DirectionBreakBarIndex;
            public int? FttCandidateBarIndex;
            public int? FttConfirmedBarIndex;
            public StructureState StructureState = StructureState.Unknown;
            public ActionType ActionType = ActionType.Unknown;
        }

        public sealed class State
        {
            private readonly Dictionary<int, ContainerRecord> records = new Dictionary<int, ContainerRecord>();

            public void OnContainer(in ContainerEvent e)
            {
                if (!records.TryGetValue(e.ContainerId, out ContainerRecord record))
                {
                    record = new ContainerRecord
                    {
                        ContainerId = e.ContainerId,
                        StartBarIndex = e.BarIndex
                    };
                    records[e.ContainerId] = record;
                }
            }

            public void OnDirectionBreak(in DirectionBreakEvent e)
            {
                if (records.TryGetValue(e.ContainerId, out ContainerRecord record))
                    record.DirectionBreakBarIndex = e.BarIndex;
            }

            public void OnFttCandidate(in FttCandidateEvent e)
            {
                if (records.TryGetValue(e.ContainerId, out ContainerRecord record))
                    record.FttCandidateBarIndex = e.BarIndex;
            }

            public void OnFttConfirmed(in FttConfirmedEvent e)
            {
                if (records.TryGetValue(e.ContainerId, out ContainerRecord record))
                    record.FttConfirmedBarIndex = e.BarIndex;
            }

            public void OnStructure(in StructureEvent e)
            {
                if (records.TryGetValue(e.ContainerId, out ContainerRecord record))
                    record.StructureState = e.State;
            }

            public ContainerReportEvent? OnAction(in ActionEvent e)
            {
                if (!records.TryGetValue(e.ContainerId, out ContainerRecord record))
                    return null;

                record.ActionType = e.Action;

                if (!record.FttConfirmedBarIndex.HasValue)
                    return null;

                return new ContainerReportEvent(
                    record.ContainerId,
                    record.StartBarIndex,
                    record.DirectionBreakBarIndex,
                    record.FttCandidateBarIndex,
                    record.FttConfirmedBarIndex,
                    record.StructureState,
                    record.ActionType);
            }
        }
    }
}