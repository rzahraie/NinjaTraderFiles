#region Using declarations
using System;
using System.Collections.Generic;
using System.Text;
#endregion

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public enum xPvaContainerLevel
    {
        Unknown,
        Level1,
        Level2,
        Level3
    }

    public enum xPvaContainerState
    {
        Unknown,
        Building,
        Complete,
        Failed,
        Suspended
    }

    public enum xPvaContainerDirection
    {
        Unknown,
        Black,
        Red,
        Lateral
    }

    public enum xPvaLevel3ContextState
    {
        Unknown,
        BuildingBlackDominance,
        BuildingRedDominance,
        BlackDominant,
        RedDominant,
        Transitioning,
        Mixed
    }

    public sealed class xPvaContainerContext
    {
        public int StartBar;
        public int EndBar;
        public DateTime StartTime;
        public DateTime EndTime;

        public xPvaContainerLevel Level;
        public xPvaContainerDirection Direction;
        public xPvaContainerState State;

        public string Pattern;
        public string Reason;
        public string CompletionReason;

        public int ParentStartBar;
        public int ParentEndBar;
        public xPvaContainerLevel ParentLevel;

        internal xPvaContainerContext Clone()
        {
            return (xPvaContainerContext)MemberwiseClone();
        }
    }

    public sealed class xPvaLevel3Context
    {
        public int StartBar;
        public int EndBar;
        public DateTime StartTime;
        public DateTime EndTime;

        public xPvaLevel3ContextState State;
        public xPvaContainerDirection Direction;

        public int CompletedLevel2BlackCount;
        public int CompletedLevel2RedCount;

        public int LastCompletedLevel2StartBar;
        public int LastCompletedLevel2EndBar;
        public xPvaContainerDirection LastCompletedLevel2Direction;

        public string Pattern;
        public string Reason;

        internal xPvaLevel3Context Clone()
        {
            return (xPvaLevel3Context)MemberwiseClone();
        }
    }

    /// <summary>
    /// Internal-only container context above Level 1 objects.
    /// This first pass intentionally under-labels and does not draw or trade.
    /// </summary>
    public sealed class xPvaContainerContextEngine
    {
        private sealed class Level1Phase
        {
            public xPvaContainerContext Context;
            public xPvaLevel1Role Role;
            public xPvaRoleConfidence RoleConfidence;
        }

        private readonly List<Level1Phase> level1Phases = new List<Level1Phase>();
        private readonly List<xPvaContainerContext> completedLevel2Contexts = new List<xPvaContainerContext>();
        private xPvaContainerContext lastContext;
        private xPvaLevel3Context lastLevel3Context;

        public xPvaContainerContext LastContext
        {
            get { return lastContext == null ? null : lastContext.Clone(); }
        }

        public xPvaLevel3Context LastLevel3Context
        {
            get { return lastLevel3Context == null ? BuildUnknownLevel3Context() : lastLevel3Context.Clone(); }
        }

        public xPvaContainerContext Update(xPvaLevel1Object level1)
        {
            if (level1 == null)
                return LastContext;

            xPvaContainerContext level1Context = BuildLevel1Context(level1);
            lastContext = level1Context;

            AddOrReplaceLevel1Context(level1Context, level1);

            xPvaContainerContext level2Context = TryBuildLevel2Context();
            if (level2Context != null)
            {
                lastContext = level2Context;
                if (level2Context.State == xPvaContainerState.Complete)
                    ConsumeCompletedLevel2(level2Context);
            }

            return LastContext;
        }

        public xPvaLevel3Context UpdateLevel3Context(xPvaLevel1Object level1)
        {
            Update(level1);
            return LastLevel3Context;
        }

        private xPvaContainerContext BuildLevel1Context(xPvaLevel1Object level1)
        {
            return new xPvaContainerContext
            {
                StartBar = level1.StartBar,
                EndBar = level1.EndBar,
                StartTime = level1.StartTime,
                EndTime = level1.EndTime,
                Level = xPvaContainerLevel.Level1,
                Direction = MapDirection(level1.Direction),
                State = MapState(level1.State),
                Pattern = "L1 " + Safe(level1.Pattern),
                Reason = "mirrors Level 1 object; role=" + level1.Role + ", confidence=" + level1.RoleConfidence,
                CompletionReason = "Level 1 mirror; Level 2 completion not evaluated",
                ParentStartBar = 0,
                ParentEndBar = 0,
                ParentLevel = xPvaContainerLevel.Unknown
            };
        }

        private void AddOrReplaceLevel1Context(xPvaContainerContext context, xPvaLevel1Object level1)
        {
            if (context == null || level1 == null || context.Direction == xPvaContainerDirection.Unknown)
                return;

            var phase = new Level1Phase
            {
                Context = context.Clone(),
                Role = level1.Role,
                RoleConfidence = level1.RoleConfidence
            };

            if (level1Phases.Count > 0)
            {
                Level1Phase last = level1Phases[level1Phases.Count - 1];
                if (last.Context.StartBar == context.StartBar)
                {
                    level1Phases[level1Phases.Count - 1] = phase;
                    Trim(level1Phases, 20);
                    return;
                }
            }

            level1Phases.Add(phase);
            Trim(level1Phases, 20);
        }

        private xPvaContainerContext TryBuildLevel2Context()
        {
            xPvaContainerContext complete = TryBuildCompleteLevel2Context();
            if (complete != null)
                return complete;

            return TryBuildBuildingLevel2Context();
        }

        private xPvaContainerContext TryBuildCompleteLevel2Context()
        {
            if (level1Phases.Count < 3)
                return null;

            Level1Phase first = level1Phases[level1Phases.Count - 3];
            Level1Phase middle = level1Phases[level1Phases.Count - 2];
            Level1Phase last = level1Phases[level1Phases.Count - 1];

            if (!IsCompleteLevel2Sequence(first, middle, last))
                return null;

            return new xPvaContainerContext
            {
                StartBar = first.Context.StartBar,
                EndBar = last.Context.EndBar,
                StartTime = first.Context.StartTime,
                EndTime = last.Context.EndTime,
                Level = xPvaContainerLevel.Level2,
                Direction = first.Context.Direction,
                State = xPvaContainerState.Complete,
                Pattern = BuildLevel2Pattern(first, middle, last),
                Reason = "recognized completed Level 2 from DominantFirstLeg -> RetraceLeg -> DominantFinalLeg",
                CompletionReason = "Level 2 Complete: three role phases are present in order; first and final legs share "
                    + first.Context.Direction + " direction; retrace is opposite/non-dominant; final leg resumes original direction",
                ParentStartBar = 0,
                ParentEndBar = 0,
                ParentLevel = xPvaContainerLevel.Unknown
            };
        }

        private xPvaContainerContext TryBuildBuildingLevel2Context()
        {
            if (level1Phases.Count < 1)
                return null;

            Level1Phase last = level1Phases[level1Phases.Count - 1];
            if (last.Role != xPvaLevel1Role.DominantFirstLeg && last.Role != xPvaLevel1Role.RetraceLeg)
                return null;

            Level1Phase first = last;
            Level1Phase middle = null;

            if (level1Phases.Count >= 2)
            {
                Level1Phase prior = level1Phases[level1Phases.Count - 2];
                if (prior.Role == xPvaLevel1Role.DominantFirstLeg
                    && last.Role == xPvaLevel1Role.RetraceLeg
                    && AreOppositeOrNonDominant(prior.Context.Direction, last.Context.Direction))
                {
                    first = prior;
                    middle = last;
                }
            }

            if (middle == null && first.Role != xPvaLevel1Role.DominantFirstLeg)
                return null;

            return new xPvaContainerContext
            {
                StartBar = first.Context.StartBar,
                EndBar = last.Context.EndBar,
                StartTime = first.Context.StartTime,
                EndTime = last.Context.EndTime,
                Level = xPvaContainerLevel.Level2,
                Direction = first.Context.Direction,
                State = xPvaContainerState.Building,
                Pattern = middle == null
                    ? RolePattern(first)
                    : RolePattern(first) + " | " + RolePattern(middle),
                Reason = middle == null
                    ? "building Level 2 candidate from DominantFirstLeg only"
                    : "building Level 2 candidate from DominantFirstLeg -> RetraceLeg",
                CompletionReason = middle == null
                    ? "Level 2 incomplete: waiting for RetraceLeg and DominantFinalLeg"
                    : "Level 2 incomplete: waiting for DominantFinalLeg to resume original direction",
                ParentStartBar = 0,
                ParentEndBar = 0,
                ParentLevel = xPvaContainerLevel.Unknown
            };
        }

        private void ConsumeCompletedLevel2(xPvaContainerContext level2)
        {
            if (level2 == null || level2.State != xPvaContainerState.Complete)
                return;

            if (completedLevel2Contexts.Count > 0)
            {
                xPvaContainerContext prior = completedLevel2Contexts[completedLevel2Contexts.Count - 1];
                if (prior.StartBar == level2.StartBar && prior.EndBar == level2.EndBar)
                {
                    completedLevel2Contexts[completedLevel2Contexts.Count - 1] = level2.Clone();
                    lastLevel3Context = BuildLevel3Context();
                    return;
                }
            }

            completedLevel2Contexts.Add(level2.Clone());
            Trim(completedLevel2Contexts, 8);
            lastLevel3Context = BuildLevel3Context();
        }

        private xPvaLevel3Context BuildLevel3Context()
        {
            if (completedLevel2Contexts.Count == 0)
                return BuildUnknownLevel3Context();

            int blackCount = CountRecentCompletedLevel2(xPvaContainerDirection.Black);
            int redCount = CountRecentCompletedLevel2(xPvaContainerDirection.Red);
            xPvaContainerContext first = completedLevel2Contexts[0];
            xPvaContainerContext last = completedLevel2Contexts[completedLevel2Contexts.Count - 1];

            xPvaLevel3ContextState state;
            xPvaContainerDirection direction;
            string reason;

            if (completedLevel2Contexts.Count == 1)
            {
                direction = last.Direction;
                if (last.Direction == xPvaContainerDirection.Black)
                {
                    state = xPvaLevel3ContextState.BuildingBlackDominance;
                    reason = "one completed Black Level 2 exists; dominance requires persistence";
                }
                else if (last.Direction == xPvaContainerDirection.Red)
                {
                    state = xPvaLevel3ContextState.BuildingRedDominance;
                    reason = "one completed Red Level 2 exists; dominance requires persistence";
                }
                else
                {
                    state = xPvaLevel3ContextState.Unknown;
                    direction = xPvaContainerDirection.Unknown;
                    reason = "completed Level 2 direction is not useful for Level 3 context";
                }
            }
            else if (HasEstablishedOppositeDominance(last.Direction))
            {
                state = xPvaLevel3ContextState.Transitioning;
                direction = last.Direction;
                reason = "opposite completed Level 2 appeared after established dominance";
            }
            else if (HasPersistence(xPvaContainerDirection.Black) && !RecentDirectionMeaningfullyInterrupted(xPvaContainerDirection.Black))
            {
                state = xPvaLevel3ContextState.BlackDominant;
                direction = xPvaContainerDirection.Black;
                reason = "two or more recent completed Black Level 2 objects exist without meaningful Red interruption";
            }
            else if (HasPersistence(xPvaContainerDirection.Red) && !RecentDirectionMeaningfullyInterrupted(xPvaContainerDirection.Red))
            {
                state = xPvaLevel3ContextState.RedDominant;
                direction = xPvaContainerDirection.Red;
                reason = "two or more recent completed Red Level 2 objects exist without meaningful Black interruption";
            }
            else if (RecentAlternation())
            {
                state = xPvaLevel3ContextState.Mixed;
                direction = xPvaContainerDirection.Lateral;
                reason = "recent completed Level 2 objects alternate direction without persistence";
            }
            else if (blackCount > redCount)
            {
                state = xPvaLevel3ContextState.BuildingBlackDominance;
                direction = xPvaContainerDirection.Black;
                reason = "Black completed Level 2 count leads, but persistence is not yet strong enough";
            }
            else if (redCount > blackCount)
            {
                state = xPvaLevel3ContextState.BuildingRedDominance;
                direction = xPvaContainerDirection.Red;
                reason = "Red completed Level 2 count leads, but persistence is not yet strong enough";
            }
            else
            {
                state = xPvaLevel3ContextState.Mixed;
                direction = xPvaContainerDirection.Lateral;
                reason = "recent completed Level 2 context is balanced or unclear";
            }

            return new xPvaLevel3Context
            {
                StartBar = first.StartBar,
                EndBar = last.EndBar,
                StartTime = first.StartTime,
                EndTime = last.EndTime,
                State = state,
                Direction = direction,
                CompletedLevel2BlackCount = blackCount,
                CompletedLevel2RedCount = redCount,
                LastCompletedLevel2StartBar = last.StartBar,
                LastCompletedLevel2EndBar = last.EndBar,
                LastCompletedLevel2Direction = last.Direction,
                Pattern = BuildLevel3Pattern(),
                Reason = reason
            };
        }

        private xPvaLevel3Context BuildUnknownLevel3Context()
        {
            return new xPvaLevel3Context
            {
                State = xPvaLevel3ContextState.Unknown,
                Direction = xPvaContainerDirection.Unknown,
                Pattern = "",
                Reason = "no useful completed Level 2 context"
            };
        }

        private bool IsCompleteLevel2Sequence(Level1Phase first, Level1Phase middle, Level1Phase last)
        {
            if (first == null || middle == null || last == null)
                return false;

            if (first.Role != xPvaLevel1Role.DominantFirstLeg
                || middle.Role != xPvaLevel1Role.RetraceLeg
                || last.Role != xPvaLevel1Role.DominantFinalLeg)
                return false;

            if (first.Context.Direction == xPvaContainerDirection.Unknown
                || last.Context.Direction == xPvaContainerDirection.Unknown)
                return false;

            if (first.Context.Direction != last.Context.Direction)
                return false;

            return AreOppositeOrNonDominant(first.Context.Direction, middle.Context.Direction);
        }

        private int CountRecentCompletedLevel2(xPvaContainerDirection direction)
        {
            int count = 0;
            for (int i = 0; i < completedLevel2Contexts.Count; i++)
            {
                if (completedLevel2Contexts[i].Direction == direction)
                    count++;
            }
            return count;
        }

        private bool HasPersistence(xPvaContainerDirection direction)
        {
            int count = 0;
            for (int i = completedLevel2Contexts.Count - 1; i >= 0; i--)
            {
                if (completedLevel2Contexts[i].Direction == direction)
                {
                    count++;
                    if (count >= 2)
                        return true;
                }
                else if (completedLevel2Contexts[i].Direction != xPvaContainerDirection.Unknown)
                {
                    return false;
                }
            }

            return false;
        }

        private bool RecentDirectionMeaningfullyInterrupted(xPvaContainerDirection dominantDirection)
        {
            if (completedLevel2Contexts.Count < 2)
                return false;

            xPvaContainerDirection opposite = dominantDirection == xPvaContainerDirection.Black
                ? xPvaContainerDirection.Red
                : dominantDirection == xPvaContainerDirection.Red
                ? xPvaContainerDirection.Black
                : xPvaContainerDirection.Unknown;

            if (opposite == xPvaContainerDirection.Unknown)
                return false;

            xPvaContainerContext last = completedLevel2Contexts[completedLevel2Contexts.Count - 1];
            return last.Direction == opposite;
        }

        private bool HasEstablishedOppositeDominance(xPvaContainerDirection newDirection)
        {
            if (completedLevel2Contexts.Count < 3)
                return false;

            xPvaContainerDirection opposite = newDirection == xPvaContainerDirection.Black
                ? xPvaContainerDirection.Red
                : newDirection == xPvaContainerDirection.Red
                ? xPvaContainerDirection.Black
                : xPvaContainerDirection.Unknown;

            if (opposite == xPvaContainerDirection.Unknown)
                return false;

            int count = 0;
            for (int i = completedLevel2Contexts.Count - 2; i >= 0; i--)
            {
                if (completedLevel2Contexts[i].Direction == opposite)
                {
                    count++;
                    if (count >= 2)
                        return true;
                }
                else if (completedLevel2Contexts[i].Direction != xPvaContainerDirection.Unknown)
                {
                    return false;
                }
            }

            return false;
        }

        private bool RecentAlternation()
        {
            if (completedLevel2Contexts.Count < 3)
                return false;

            int start = Math.Max(1, completedLevel2Contexts.Count - 3);
            for (int i = start; i < completedLevel2Contexts.Count; i++)
            {
                xPvaContainerDirection previous = completedLevel2Contexts[i - 1].Direction;
                xPvaContainerDirection current = completedLevel2Contexts[i].Direction;
                if (previous == xPvaContainerDirection.Unknown
                    || current == xPvaContainerDirection.Unknown
                    || previous == current)
                    return false;
            }

            return true;
        }

        private string BuildLevel3Pattern()
        {
            var sb = new StringBuilder();
            int start = Math.Max(0, completedLevel2Contexts.Count - 5);
            for (int i = start; i < completedLevel2Contexts.Count; i++)
            {
                if (sb.Length > 0)
                    sb.Append(" -> ");

                xPvaContainerContext level2 = completedLevel2Contexts[i];
                sb.Append("L2 ");
                sb.Append(level2.Direction);
                sb.Append(" [");
                sb.Append(level2.StartBar);
                sb.Append("-");
                sb.Append(level2.EndBar);
                sb.Append("]");
            }

            return sb.ToString();
        }

        private bool AreOppositeOrNonDominant(xPvaContainerDirection dominantDirection, xPvaContainerDirection retraceDirection)
        {
            if (retraceDirection == xPvaContainerDirection.Lateral || retraceDirection == xPvaContainerDirection.Unknown)
                return true;

            return IsOppositeDirection(dominantDirection, retraceDirection);
        }

        private bool IsOppositeDirection(xPvaContainerDirection left, xPvaContainerDirection right)
        {
            return (left == xPvaContainerDirection.Black && right == xPvaContainerDirection.Red)
                || (left == xPvaContainerDirection.Red && right == xPvaContainerDirection.Black);
        }

        private string BuildLevel2Pattern(Level1Phase first, Level1Phase middle, Level1Phase last)
        {
            return RolePattern(first) + " | " + RolePattern(middle) + " | " + RolePattern(last);
        }

        private string RolePattern(Level1Phase phase)
        {
            if (phase == null || phase.Context == null)
                return "";

            return phase.Role + " " + phase.Context.Direction + " [" + phase.Context.StartBar + "-" + phase.Context.EndBar + "] " + Safe(phase.Context.Pattern);
        }

        private xPvaContainerDirection MapDirection(xPvaLevel1ObjectDirection direction)
        {
            if (direction == xPvaLevel1ObjectDirection.Black) return xPvaContainerDirection.Black;
            if (direction == xPvaLevel1ObjectDirection.Red) return xPvaContainerDirection.Red;
            return xPvaContainerDirection.Unknown;
        }

        private xPvaContainerState MapState(xPvaLevel1ObjectState state)
        {
            if (state == xPvaLevel1ObjectState.Complete) return xPvaContainerState.Complete;
            if (state == xPvaLevel1ObjectState.Failed || state == xPvaLevel1ObjectState.Fragmented) return xPvaContainerState.Failed;
            if (state == xPvaLevel1ObjectState.Suspended) return xPvaContainerState.Suspended;
            if (state == xPvaLevel1ObjectState.Incomplete) return xPvaContainerState.Building;
            return xPvaContainerState.Unknown;
        }

        private void Trim(List<Level1Phase> phases, int maxCount)
        {
            while (phases.Count > maxCount)
                phases.RemoveAt(0);
        }

        private void Trim(List<xPvaContainerContext> contexts, int maxCount)
        {
            while (contexts.Count > maxCount)
                contexts.RemoveAt(0);
        }

        private string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : value;
        }
    }
}
