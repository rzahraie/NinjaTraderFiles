#region Using declarations
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaLevel1ObjectState
    {
        None,
        Incomplete,
        Complete,
        Failed,
        Suspended,
        Fragmented
    }

    public enum xPvaLevel1ObjectDirection
    {
        Unknown,
        Black,
        Red
    }

    public enum xPvaLevel1Role
    {
        Unknown,
        DominantFirstLeg,
        RetraceLeg,
        DominantFinalLeg,
        StandaloneTape
    }

    public enum xPvaRoleConfidence
    {
        Unknown,
        Low,
        Medium,
        High
    }

    public sealed class xPvaLevel1Object
    {
        public int StartBar;
        public int EndBar;
        public DateTime StartTime;
        public DateTime EndTime;

        public xPvaLevel1ObjectDirection Direction;
        public xPvaLevel1ObjectState State;
        public xPvaLevel1Role Role;
        public xPvaRoleConfidence RoleConfidence;

        public string Pattern;
        public string Reason;
        public string RoleReason;
        public string RoleConfidenceReason;
        public bool IsMinimumObject;
        public bool IsExtendedObject;

        internal xPvaLevel1Object Clone()
        {
            return (xPvaLevel1Object)MemberwiseClone();
        }
    }

    /// <summary>
    /// Level 1 object grammar.
    /// Two-bar sequences remain candidates. A completed object requires a
    /// side-specific dominance / non-dominance / dominance grammar.
    /// </summary>
    public sealed class xPvaLevel1ObjectEngine
    {
        private sealed class Token
        {
            public int BarIndex;
            public DateTime Time;
            public xPvaLevel1ObjectDirection Direction;
            public bool IsDominant;
            public string Label;
        }

        private readonly List<Token> activeTokens = new List<Token>();
        private xPvaLevel1Object lastObservation;
        private xPvaLevel1Object lastCompletedObject;
        private xPvaLevel1ObjectDirection roleAnchorDirection = xPvaLevel1ObjectDirection.Unknown;
        private xPvaLevel1Role roleContextStage = xPvaLevel1Role.Unknown;

        public xPvaLevel1Object LastObservation
        {
            get { return lastObservation == null ? null : lastObservation.Clone(); }
        }

        public xPvaLevel1Object LastCompletedObject
        {
            get { return lastCompletedObject == null ? null : lastCompletedObject.Clone(); }
        }

        public xPvaLevel1Object Update(xPvaDiscreteEvent ev)
        {
            if (ev == null)
                return LastObservation;

            Token token = BuildToken(ev);
            if (token == null)
            {
                lastObservation = BuildNeutralObservation(ev);
                AssignRole(lastObservation, false);
                return lastObservation.Clone();
            }

            if (activeTokens.Count > 0 && activeTokens[0].Direction != token.Direction)
            {
                if (token.IsDominant && CanFragmentActiveCandidate())
                {
                    xPvaLevel1Object fragmented = BuildFragmentedObject(token);
                    AssignRole(fragmented, true);
                    activeTokens.Clear();
                    activeTokens.Add(token);
                    lastObservation = fragmented;
                    return lastObservation.Clone();
                }

                xPvaLevel1Object incomplete = BuildOppositeSideIncompleteObject(token);
                AssignRole(incomplete, true);
                activeTokens.Clear();
                activeTokens.Add(token);
                lastObservation = incomplete;
                return lastObservation.Clone();
            }

            activeTokens.Add(token);

            xPvaLevel1Object completed = TryBuildCompleteObject();
            if (completed != null)
            {
                MarkPriorFailedIfContradicted(completed);
                AssignRole(completed, true);
                lastCompletedObject = completed.Clone();
                lastObservation = completed;
                activeTokens.Clear();
                return lastObservation.Clone();
            }

            lastObservation = BuildIncompleteObject(null);
            AssignRole(lastObservation, false);
            return lastObservation.Clone();
        }

        private Token BuildToken(xPvaDiscreteEvent ev)
        {
            xPvaLevel1ObjectDirection direction = MapDirection(ev.VolumePolarity);
            if (direction == xPvaLevel1ObjectDirection.Unknown)
                return null;

            bool dominant = ev.VolumeChange == xPvaVolumeChange.Plus;

            return new Token
            {
                BarIndex = ev.BarIndex,
                Time = ev.Time,
                Direction = direction,
                IsDominant = dominant,
                Label = BuildTokenLabel(direction, ev.VolumeChange)
            };
        }

        private xPvaLevel1ObjectDirection MapDirection(xPvaVolumePolarity polarity)
        {
            if (polarity == xPvaVolumePolarity.B) return xPvaLevel1ObjectDirection.Black;
            if (polarity == xPvaVolumePolarity.R) return xPvaLevel1ObjectDirection.Red;
            return xPvaLevel1ObjectDirection.Unknown;
        }

        private string BuildTokenLabel(xPvaLevel1ObjectDirection direction, xPvaVolumeChange change)
        {
            string side = direction == xPvaLevel1ObjectDirection.Black ? "B" : "R";
            string sign = change == xPvaVolumeChange.Plus ? "+" : change == xPvaVolumeChange.Minus ? "-" : "=";
            return side + sign;
        }

        private xPvaLevel1Object TryBuildCompleteObject()
        {
            if (activeTokens.Count < 3)
                return null;

            List<bool> reduced = ReducedDominanceStates(activeTokens);
            bool minimum = IsMinimumGrammar(reduced);
            bool extended = IsExtendedGrammar(reduced);

            if (!minimum && !extended)
                return null;

            Token first = activeTokens[0];
            Token last = activeTokens[activeTokens.Count - 1];
            return new xPvaLevel1Object
            {
                StartBar = first.BarIndex,
                EndBar = last.BarIndex,
                StartTime = first.Time,
                EndTime = last.Time,
                Direction = first.Direction,
                State = xPvaLevel1ObjectState.Complete,
                Role = xPvaLevel1Role.Unknown,
                RoleConfidence = xPvaRoleConfidence.Unknown,
                Pattern = BuildPattern(activeTokens),
                Reason = minimum
                    ? "complete minimum D-ND-D grammar"
                    : "complete extended ND-D-ND-D grammar",
                RoleReason = "",
                RoleConfidenceReason = "",
                IsMinimumObject = minimum,
                IsExtendedObject = extended
            };
        }

        private List<bool> ReducedDominanceStates(List<Token> tokens)
        {
            var reduced = new List<bool>();
            for (int i = 0; i < tokens.Count; i++)
            {
                bool state = tokens[i].IsDominant;
                if (reduced.Count == 0 || reduced[reduced.Count - 1] != state)
                    reduced.Add(state);
            }
            return reduced;
        }

        private bool IsMinimumGrammar(List<bool> reduced)
        {
            return reduced.Count == 3 && reduced[0] && !reduced[1] && reduced[2];
        }

        private bool IsExtendedGrammar(List<bool> reduced)
        {
            return reduced.Count == 4 && !reduced[0] && reduced[1] && !reduced[2] && reduced[3];
        }

        private bool CanFragmentActiveCandidate()
        {
            if (activeTokens.Count < 3)
                return false;

            List<bool> reduced = ReducedDominanceStates(activeTokens);
            return IsRareFragmentablePrefix(reduced);
        }

        private bool IsRareFragmentablePrefix(List<bool> reduced)
        {
            return reduced.Count == 3 && !reduced[0] && reduced[1] && !reduced[2];
        }

        private xPvaLevel1Object BuildIncompleteObject(string reasonOverride)
        {
            Token first = activeTokens[0];
            Token last = activeTokens[activeTokens.Count - 1];

            string reason = !string.IsNullOrEmpty(reasonOverride)
                ? reasonOverride
                : activeTokens.Count < 3
                ? "candidate incomplete"
                : "candidate incomplete; minimum D-ND-D grammar has not completed";

            return new xPvaLevel1Object
            {
                StartBar = first.BarIndex,
                EndBar = last.BarIndex,
                StartTime = first.Time,
                EndTime = last.Time,
                Direction = first.Direction,
                State = xPvaLevel1ObjectState.Incomplete,
                Role = xPvaLevel1Role.Unknown,
                RoleConfidence = xPvaRoleConfidence.Unknown,
                Pattern = BuildPattern(activeTokens),
                Reason = reason,
                RoleReason = "",
                RoleConfidenceReason = "",
                IsMinimumObject = false,
                IsExtendedObject = false
            };
        }

        private xPvaLevel1Object BuildOppositeSideIncompleteObject(Token interruptingToken)
        {
            Token first = activeTokens[0];
            var tokens = new List<Token>(activeTokens);
            tokens.Add(interruptingToken);

            return new xPvaLevel1Object
            {
                StartBar = first.BarIndex,
                EndBar = interruptingToken.BarIndex,
                StartTime = first.Time,
                EndTime = interruptingToken.Time,
                Direction = first.Direction,
                State = xPvaLevel1ObjectState.Incomplete,
                Role = xPvaLevel1Role.Unknown,
                RoleConfidence = xPvaRoleConfidence.Unknown,
                Pattern = BuildPattern(tokens),
                Reason = DirectionName(first.Direction) + " candidate incomplete; opposite side appeared before completion",
                RoleReason = "",
                RoleConfidenceReason = "",
                IsMinimumObject = false,
                IsExtendedObject = false
            };
        }

        private xPvaLevel1Object BuildSuspendedObject(Token interruptingToken)
        {
            Token first = activeTokens[0];
            var tokens = new List<Token>(activeTokens);
            tokens.Add(interruptingToken);

            return new xPvaLevel1Object
            {
                StartBar = first.BarIndex,
                EndBar = interruptingToken.BarIndex,
                StartTime = first.Time,
                EndTime = interruptingToken.Time,
                Direction = first.Direction,
                State = xPvaLevel1ObjectState.Suspended,
                Role = xPvaLevel1Role.Unknown,
                RoleConfidence = xPvaRoleConfidence.Unknown,
                Pattern = BuildPattern(tokens),
                Reason = DirectionName(first.Direction) + " candidate incomplete; weak opposite ND appeared before completion",
                RoleReason = "",
                RoleConfidenceReason = "",
                IsMinimumObject = false,
                IsExtendedObject = false
            };
        }

        private xPvaLevel1Object BuildFragmentedObject(Token interruptingToken)
        {
            Token first = activeTokens[0];
            var tokens = new List<Token>(activeTokens);
            tokens.Add(interruptingToken);

            return new xPvaLevel1Object
            {
                StartBar = first.BarIndex,
                EndBar = interruptingToken.BarIndex,
                StartTime = first.Time,
                EndTime = interruptingToken.Time,
                Direction = first.Direction,
                State = xPvaLevel1ObjectState.Fragmented,
                Role = xPvaLevel1Role.Unknown,
                RoleConfidence = xPvaRoleConfidence.Unknown,
                Pattern = BuildPattern(tokens),
                Reason = DirectionName(first.Direction) + " candidate fragmented; strong opposite D disrupted extended candidate before completion",
                RoleReason = "",
                RoleConfidenceReason = "",
                IsMinimumObject = false,
                IsExtendedObject = false
            };
        }

        private xPvaLevel1Object BuildNeutralObservation(xPvaDiscreteEvent ev)
        {
            return new xPvaLevel1Object
            {
                StartBar = ev.BarIndex,
                EndBar = ev.BarIndex,
                StartTime = ev.Time,
                EndTime = ev.Time,
                Direction = xPvaLevel1ObjectDirection.Unknown,
                State = activeTokens.Count > 0 ? xPvaLevel1ObjectState.Incomplete : xPvaLevel1ObjectState.None,
                Role = xPvaLevel1Role.Unknown,
                RoleConfidence = xPvaRoleConfidence.Unknown,
                Pattern = activeTokens.Count > 0 ? BuildPattern(activeTokens) : "",
                Reason = activeTokens.Count > 0
                    ? "candidate incomplete; neutral event appeared before completion"
                    : "no directional Level 1 candidate",
                RoleReason = "",
                RoleConfidenceReason = "",
                IsMinimumObject = false,
                IsExtendedObject = false
            };
        }

        private void AssignRole(xPvaLevel1Object obj, bool commitContext)
        {
            if (obj == null)
                return;

            obj.Role = xPvaLevel1Role.Unknown;
            obj.RoleReason = "role uncertain; insufficient Level 1 context";
            obj.RoleConfidence = xPvaRoleConfidence.Unknown;
            obj.RoleConfidenceReason = "confidence unavailable because Level 1 role is unknown";

            if (obj.Direction == xPvaLevel1ObjectDirection.Unknown || obj.State == xPvaLevel1ObjectState.None)
            {
                obj.RoleReason = "role unavailable; no directional Level 1 object";
                obj.RoleConfidenceReason = "confidence unavailable because there is no directional Level 1 role";
                return;
            }

            bool startsDominant = StartsWithDominantToken(obj);

            if (obj.IsMinimumObject && roleContextStage == xPvaLevel1Role.Unknown)
            {
                obj.Role = xPvaLevel1Role.StandaloneTape;
                obj.RoleReason = "minimum D-ND-D tape with no active broader Level 1 role context";
            }
            else if (roleContextStage == xPvaLevel1Role.Unknown)
            {
                if (startsDominant)
                {
                    obj.Role = xPvaLevel1Role.DominantFirstLeg;
                    obj.RoleReason = "same-side sequence begins from clear Level 1 role reset";
                }
            }
            else if (roleContextStage == xPvaLevel1Role.DominantFirstLeg)
            {
                if (IsOppositeDirection(obj.Direction, roleAnchorDirection))
                {
                    obj.Role = xPvaLevel1Role.RetraceLeg;
                    obj.RoleReason = "counter-directional Level 1 object after dominant first leg";
                }
                else
                {
                    obj.RoleReason = "same direction after dominant first leg; role not forced";
                }
            }
            else if (roleContextStage == xPvaLevel1Role.RetraceLeg)
            {
                if (obj.Direction == roleAnchorDirection)
                {
                    obj.Role = xPvaLevel1Role.DominantFinalLeg;
                    obj.RoleReason = "resumes original dominant Level 1 direction after retrace";
                }
                else
                {
                    obj.RoleReason = "additional counter-directional object after retrace; role not forced";
                }
            }

            if (commitContext)
                CommitRoleContext(obj);

            AssignRoleConfidence(obj);
        }

        private void AssignRoleConfidence(xPvaLevel1Object obj)
        {
            obj.RoleConfidence = xPvaRoleConfidence.Unknown;
            obj.RoleConfidenceReason = "confidence unavailable because Level 1 role is unknown";

            if (obj.Role == xPvaLevel1Role.Unknown)
                return;

            if (obj.Role == xPvaLevel1Role.StandaloneTape
                && obj.State == xPvaLevel1ObjectState.Complete
                && obj.IsMinimumObject)
            {
                obj.RoleConfidence = xPvaRoleConfidence.High;
                obj.RoleConfidenceReason = "completed minimum D-ND-D tape provides clear local standalone role evidence";
                return;
            }

            if (obj.State == xPvaLevel1ObjectState.Complete)
            {
                obj.RoleConfidence = xPvaRoleConfidence.Medium;
                obj.RoleConfidenceReason = "role is inferred from completed local Level 1 sequence context; parent container context is unavailable";
                return;
            }

            obj.RoleConfidence = xPvaRoleConfidence.Low;
            obj.RoleConfidenceReason = "role is assigned from developing local Level 1 context and may depend on parent container context";
        }

        private void CommitRoleContext(xPvaLevel1Object obj)
        {
            if (obj.Role == xPvaLevel1Role.DominantFirstLeg)
            {
                roleAnchorDirection = obj.Direction;
                roleContextStage = xPvaLevel1Role.DominantFirstLeg;
            }
            else if (obj.Role == xPvaLevel1Role.RetraceLeg)
            {
                roleContextStage = xPvaLevel1Role.RetraceLeg;
            }
            else if (obj.Role == xPvaLevel1Role.DominantFinalLeg || obj.Role == xPvaLevel1Role.StandaloneTape)
            {
                roleAnchorDirection = xPvaLevel1ObjectDirection.Unknown;
                roleContextStage = xPvaLevel1Role.Unknown;
            }
        }

        private bool StartsWithDominantToken(xPvaLevel1Object obj)
        {
            return !string.IsNullOrEmpty(obj.Pattern)
                && (obj.Pattern.StartsWith("B+", StringComparison.Ordinal)
                    || obj.Pattern.StartsWith("R+", StringComparison.Ordinal));
        }

        private bool IsOppositeDirection(xPvaLevel1ObjectDirection left, xPvaLevel1ObjectDirection right)
        {
            return (left == xPvaLevel1ObjectDirection.Black && right == xPvaLevel1ObjectDirection.Red)
                || (left == xPvaLevel1ObjectDirection.Red && right == xPvaLevel1ObjectDirection.Black);
        }

        private void MarkPriorFailedIfContradicted(xPvaLevel1Object completed)
        {
            if (lastCompletedObject == null)
                return;

            if (lastCompletedObject.Direction == completed.Direction)
                return;

            if (completed.StartBar <= lastCompletedObject.EndBar + 1)
            {
                lastCompletedObject.State = xPvaLevel1ObjectState.Failed;
                lastCompletedObject.Reason = "failed by immediate opposite completed Level 1 object";
                completed.Reason += "; immediately contradicts prior " + lastCompletedObject.Direction + " Level 1 object";
            }
        }

        private string DirectionName(xPvaLevel1ObjectDirection direction)
        {
            if (direction == xPvaLevel1ObjectDirection.Black) return "black";
            if (direction == xPvaLevel1ObjectDirection.Red) return "red";
            return "unknown";
        }

        private string BuildPattern(List<Token> tokens)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < tokens.Count; i++)
            {
                if (i > 0)
                    sb.Append(" -> ");
                sb.Append(tokens[i].Label);
            }
            return sb.ToString();
        }
    }
}
