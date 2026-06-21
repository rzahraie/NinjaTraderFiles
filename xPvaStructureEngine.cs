#region Using declarations
using System;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaStructureObservation
    {
        public int BarIndex { get; internal set; }
        public DateTime Time { get; internal set; }
        public int SourceSequenceId { get; internal set; }

        public xPvaDominanceSide CurrentSide { get; internal set; }
        public xPvaDominanceSide CurrentObjectSide { get; internal set; }
        public xPvaBinaryState BinaryState { get; internal set; }
        public xPvaStructureState StructureState { get; internal set; }

        public bool IsSpecialPeakCompressed { get; internal set; }
        public bool IsVolumeRangeMismatch { get; internal set; }
        public bool IsConservativeUnknown { get; internal set; }
        public bool ObjectSideChanged { get; internal set; }

        public int SameObjectObservationCount { get; internal set; }
        public int UnknownObservationCount { get; internal set; }

        public string Reason { get; internal set; }

        public string Label
        {
            get
            {
                string s = StructureState.ToString();
                if (CurrentObjectSide != xPvaDominanceSide.Neutral)
                    s += " " + CurrentObjectSide.ToString();
                if (IsSpecialPeakCompressed) s += " PVC";
                if (IsVolumeRangeMismatch) s += " VRM";
                if (!string.IsNullOrEmpty(Reason)) s += " - " + Reason;
                return s;
            }
        }

        internal xPvaStructureObservation Clone()
        {
            return (xPvaStructureObservation)MemberwiseClone();
        }
    }

    /// <summary>
    /// Stage 5 structure layer.
    /// This layer deliberately avoids vocabulary such as tape, traverse, channel,
    /// or BBT. Its only purpose is to describe whether the market appears to be
    /// building the same object, completing the current object, or beginning a
    /// new object.
    ///
    /// It is conservative. Peak/compressed and volume-range mismatch are treated
    /// as structural context, not as automatic new-object proof.
    /// </summary>
    public sealed class xPvaStructureEngine
    {
        private xPvaDominanceSide currentObjectSide = xPvaDominanceSide.Neutral;
        private int sameObjectObservationCount;
        private int unknownObservationCount;
        private xPvaStructureObservation lastObservation;

        public xPvaStructureObservation LastObservation
        {
            get { return lastObservation == null ? null : lastObservation.Clone(); }
        }

        public xPvaDominanceSide CurrentObjectSide
        {
            get { return currentObjectSide; }
        }

        public xPvaStructureObservation Evaluate(xPvaBinaryObservation binary)
        {
            if (binary == null)
                return null;

            var obs = new xPvaStructureObservation
            {
                BarIndex = binary.BarIndex,
                Time = binary.Time,
                SourceSequenceId = binary.SourceSequenceId,
                CurrentSide = binary.CurrentSide,
                BinaryState = binary.BinaryState,
                IsSpecialPeakCompressed = binary.IsSpecialPeakCompressed,
                IsVolumeRangeMismatch = binary.IsVolumeRangeMismatch,
                IsConservativeUnknown = binary.IsConservativeUnknown
            };

            bool hasObject = currentObjectSide != xPvaDominanceSide.Neutral;
            bool hasCurrentSide = binary.CurrentSide != xPvaDominanceSide.Neutral;
            bool sameSide = hasObject && hasCurrentSide && binary.CurrentSide == currentObjectSide;
            bool oppositeSide = hasObject && hasCurrentSide && binary.CurrentSide != currentObjectSide;

            if (!hasObject && hasCurrentSide && binary.BinaryState != xPvaBinaryState.Unknown)
            {
                currentObjectSide = binary.CurrentSide;
                sameObjectObservationCount = 1;
                unknownObservationCount = 0;

                obs.StructureState = xPvaStructureState.BuildingCurrentObject;
                obs.Reason = "initial object side established";
            }
            else if (binary.BinaryState == xPvaBinaryState.Change && oppositeSide)
            {
                xPvaDominanceSide prior = currentObjectSide;
                currentObjectSide = binary.CurrentSide;
                sameObjectObservationCount = 1;
                unknownObservationCount = 0;

                obs.StructureState = xPvaStructureState.BuildingNewObject;
                obs.ObjectSideChanged = true;
                obs.Reason = "binary change; object side changed from " + prior + " to " + currentObjectSide;
            }
            else if (binary.BinaryState == xPvaBinaryState.Continue && sameSide)
            {
                sameObjectObservationCount++;
                unknownObservationCount = 0;

                if (binary.StructureState == xPvaStructureState.CompletingCurrentObject
                    || binary.IsSpecialPeakCompressed)
                {
                    obs.StructureState = xPvaStructureState.CompletingCurrentObject;
                    obs.Reason = "same object; completion context present";
                }
                else if (sameObjectObservationCount <= 1)
                {
                    obs.StructureState = xPvaStructureState.BuildingCurrentObject;
                    obs.Reason = "same object beginning/building";
                }
                else
                {
                    obs.StructureState = xPvaStructureState.ContinuingCurrentObject;
                    obs.Reason = "same object continues";
                }
            }
            else if (binary.BinaryState == xPvaBinaryState.Continue && !hasObject && hasCurrentSide)
            {
                currentObjectSide = binary.CurrentSide;
                sameObjectObservationCount = 1;
                unknownObservationCount = 0;

                obs.StructureState = xPvaStructureState.BuildingCurrentObject;
                obs.Reason = "continue observed without prior object; treating as current object";
            }
            else if (binary.BinaryState == xPvaBinaryState.Unknown)
            {
                unknownObservationCount++;

                if (hasObject && (binary.IsSpecialPeakCompressed || binary.IsVolumeRangeMismatch))
                {
                    obs.StructureState = xPvaStructureState.CompletingCurrentObject;
                    obs.Reason = "unknown binary with special context; possible completion, not confirmed";
                }
                else if (hasObject)
                {
                    obs.StructureState = xPvaStructureState.BuildingCurrentObject;
                    obs.Reason = "unknown binary; no proof of new object";
                }
                else
                {
                    obs.StructureState = xPvaStructureState.Unknown;
                    obs.Reason = "unknown binary and no established object";
                }
            }
            else
            {
                unknownObservationCount++;
                obs.StructureState = xPvaStructureState.Unknown;
                obs.Reason = "structural evidence insufficient";
            }

            obs.CurrentObjectSide = currentObjectSide;
            obs.SameObjectObservationCount = sameObjectObservationCount;
            obs.UnknownObservationCount = unknownObservationCount;

            lastObservation = obs.Clone();
            return obs;
        }
    }
}
