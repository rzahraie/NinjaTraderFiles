namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public readonly struct ManualContainerAnalysis
    {
        public readonly ManualContainerSnapshot Snapshot;
        public readonly ManualVolumeEvent[] VolumeEvents;

        public readonly int? FttCandidateBar;
        public readonly int? FttConfirmedBar;

        public readonly StructureState? StructureState;
        public readonly ActionType? ActionType;
        public readonly TradeIntent? TradeIntent;

        public ManualContainerAnalysis(
            ManualContainerSnapshot snapshot,
            ManualVolumeEvent[] volumeEvents,
            int? fttCandidateBar,
            int? fttConfirmedBar,
            StructureState? structureState,
            ActionType? actionType,
            TradeIntent? tradeIntent)
        {
            Snapshot = snapshot;
            VolumeEvents = volumeEvents;
            FttCandidateBar = fttCandidateBar;
            FttConfirmedBar = fttConfirmedBar;
            StructureState = structureState;
            ActionType = actionType;
            TradeIntent = tradeIntent;
        }
    }
}