using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NinjaTrader.NinjaScript.xPva.Engine;

namespace NinjaTrader.NinjaScript.Indicators
{
    internal enum xPvaV2Direction
    {
        Unknown,
        Up,
        Down
    }

    internal enum xPvaV2ContainerStatus
    {
        Active,
        Adjusted,
        Frozen,
        Broken,
        Joined,
        StructurallyDeactivated
    }

    internal enum xPvaV2OriginKind
    {
        Unknown,
        TwoBarConstruction,
        Extension,
        OppositeChild,
        SameDirectionResponse,
        Promotion,
        Join,
        FttBreakout,
        P3Continuation
    }

    internal enum xPvaV2OriginPoint
    {
        None,
        P1,
        P2,
        P3,
        Ftt
    }

    internal enum xPvaV2RelationshipKind
    {
        Containment,
        JoinComponent,
        StructuralContext,
        P3Continuation
    }

    internal enum xPvaV2JoinRejectReason
    {
        None,
        MissingMember,
        UnknownDirection,
        NotAlternating,
        NotStartOrdered,
        P1GeometryFailed,
        MiddleFttFailed,
        SameDirectionExtremeFailed,
        ResumeFailed,
        P3GeometryFailed,
        InvalidJoinPoint
    }

    internal enum xPvaV2BarRelation
    {
        Unknown,
        HHHL,
        LLLH,
        FTP,
        FBP,
        StitchLong,
        StitchShort,
        InsideBar,
        OutsideBullish,
        OutsideBearish,
        SameHighSameLow,
        HighReversal,
        LowReversal
    }

    internal enum xPvaV2ConstructionRejectReason
    {
        None,
        MissingBar,
        UnknownDirection,
        SameBar,
        SecondBarOutside,
        UpP3NotAboveP1,
        DownP3NotBelowP1,
        RelationDoesNotConstructDirection
    }

    internal enum xPvaV2ExtensionRejectReason
    {
        None,
        MissingContainer,
        MissingBar,
        UnknownDirection,
        BarBeforeContainer,
        P3GeometryFailed,
        RelationOpposesContainer
    }

    internal enum xPvaV2BreakoutRejectReason
    {
        None,
        MissingContainer,
        MissingBar,
        UnknownDirection,
        BarBeforeContainer,
        NoBreakout
    }

    internal enum xPvaV2PromotionRejectReason
    {
        None,
        MissingBrokenParent,
        BrokenParentStillLive,
        UnknownDirection,
        NoPromotableChild
    }

    internal enum xPvaV2TraceKind
    {
        BeginBar,
        BreakoutContext,
        BreakoutResult,
        PromotionResult,
        ExtensionContext,
        ExtensionResult,
        ConstructionContext,
        ConstructionResult,
        ContainmentResult,
        P3JoinResult,
        EndBar
    }

    internal struct xPvaV2PricePoint
    {
        public readonly int Bar;
        public readonly double Price;

        public xPvaV2PricePoint(int bar, double price)
        {
            Bar = bar;
            Price = price;
        }
    }

    internal struct xPvaV2Bar
    {
        public readonly int Index;
        public readonly double High;
        public readonly double Low;
        public readonly xPvaV2BarRelation RelationToPrevious;

        public xPvaV2Bar(int index, double high, double low, xPvaV2BarRelation relationToPrevious)
        {
            Index = index;
            High = high;
            Low = low;
            RelationToPrevious = relationToPrevious;
        }
    }

    internal sealed class xPvaV2TraceEntry
    {
        public readonly xPvaV2TraceKind Kind;
        public readonly int Bar;
        public readonly int ContainerId;
        public readonly bool Applied;
        public readonly string Detail;

        public xPvaV2TraceEntry(xPvaV2TraceKind kind, int bar, int containerId, bool applied, string detail)
        {
            Kind = kind;
            Bar = bar;
            ContainerId = containerId;
            Applied = applied;
            Detail = detail ?? string.Empty;
        }
    }

    internal enum xPvaV2RenderLineKind
    {
        Rtl,
        Ltl,
        Ve
    }

    internal struct xPvaV2RenderSegment
    {
        public readonly int ContainerId;
        public readonly int VisualLevel;
        public readonly xPvaV2Direction Direction;
        public readonly xPvaV2RenderLineKind Kind;
        public readonly int StartBar;
        public readonly int EndBar;
        public readonly double StartPrice;
        public readonly double EndPrice;
        public readonly xPvaV2ContainerStatus Status;

        public xPvaV2RenderSegment(int containerId, int visualLevel, xPvaV2Direction direction, xPvaV2RenderLineKind kind, int startBar, int endBar, double startPrice, double endPrice, xPvaV2ContainerStatus status)
        {
            ContainerId = containerId;
            VisualLevel = visualLevel;
            Direction = direction;
            Kind = kind;
            StartBar = startBar;
            EndBar = endBar;
            StartPrice = startPrice;
            EndPrice = endPrice;
            Status = status;
        }
    }

    internal sealed class xPvaV2RenderSnapshot
    {
        private readonly xPvaV2RenderSegment[] segments;

        public xPvaV2RenderSnapshot(IList<xPvaV2RenderSegment> source)
        {
            if (source == null || source.Count == 0)
            {
                segments = new xPvaV2RenderSegment[0];
                return;
            }

            segments = new xPvaV2RenderSegment[source.Count];
            for (int i = 0; i < source.Count; i++)
                segments[i] = source[i];
        }

        public int Count
        {
            get { return segments.Length; }
        }

        public xPvaV2RenderSegment this[int index]
        {
            get { return segments[index]; }
        }
    }

    internal sealed class xPvaV2Container
    {
        public int Id;
        public xPvaV2Direction Direction;
        public xPvaV2ContainerStatus Status;
        public int StartBar;
        public int EndBar;
        public xPvaV2PricePoint P1;
        public xPvaV2PricePoint P2;
        public xPvaV2PricePoint P3;
        public int VisualLevel;
        public int StructuralLevel;
        public xPvaV2OriginKind OriginKind;
        public int OriginContainerId;
        public xPvaV2OriginPoint OriginPoint;
        public int OriginBar;
        public double OriginPrice;
        public readonly List<int> ContainmentChildIds = new List<int>();
        public readonly List<int> JoinComponentIds = new List<int>();

        public xPvaV2Container()
        {
            Id = 0;
            Direction = xPvaV2Direction.Unknown;
            Status = xPvaV2ContainerStatus.Active;
            StartBar = 0;
            EndBar = 0;
            P1 = new xPvaV2PricePoint(0, 0.0);
            P2 = new xPvaV2PricePoint(0, 0.0);
            P3 = new xPvaV2PricePoint(0, 0.0);
            VisualLevel = 1;
            StructuralLevel = 1;
            OriginKind = xPvaV2OriginKind.Unknown;
            OriginContainerId = 0;
            OriginPoint = xPvaV2OriginPoint.None;
            OriginBar = 0;
            OriginPrice = 0.0;
        }
    }

    internal sealed class xPvaV2Relationship
    {
        public int SourceContainerId;
        public int TargetContainerId;
        public xPvaV2RelationshipKind Kind;
        public xPvaV2OriginPoint SourcePoint;
        public int Bar;
        public double Price;
    }

    internal sealed class xPvaV2Model
    {
        private readonly Dictionary<int, xPvaV2Container> containers = new Dictionary<int, xPvaV2Container>();
        private readonly List<int> containerOrder = new List<int>();
        private readonly HashSet<int> containerOrderIds = new HashSet<int>();
        private readonly List<xPvaV2Relationship> relationships = new List<xPvaV2Relationship>();
        private readonly HashSet<string> relationshipKeys = new HashSet<string>();
        private readonly Dictionary<string, List<xPvaV2Relationship>> relationshipsByContainerKind = new Dictionary<string, List<xPvaV2Relationship>>();
        private int nextContainerId = 1;

        public IEnumerable<xPvaV2Container> Containers
        {
            get
            {
                foreach (int id in containerOrder)
                    yield return containers[id];
            }
        }

        public IEnumerable<xPvaV2Relationship> Relationships
        {
            get { return relationships; }
        }

        public IList<xPvaV2Container> ActiveContainers()
        {
            var result = new List<xPvaV2Container>();
            foreach (int id in containerOrder)
            {
                xPvaV2Container container = containers[id];
                if (xPvaV2Rules.IsLive(container))
                    result.Add(container);
            }

            return result;
        }

        public IList<xPvaV2Container> ContainersByStart()
        {
            var result = new List<xPvaV2Container>();
            foreach (int id in containerOrder)
                result.Add(containers[id]);

            result.Sort(CompareByStartThenId);
            return result;
        }

        public IList<xPvaV2Container> ActiveContainersByStart()
        {
            var result = new List<xPvaV2Container>();
            foreach (int id in containerOrder)
            {
                xPvaV2Container container = containers[id];
                if (xPvaV2Rules.IsLive(container))
                    result.Add(container);
            }

            result.Sort(CompareByStartThenId);
            return result;
        }

        public xPvaV2Container LastLiveContainerByStart()
        {
            xPvaV2Container selected = null;
            foreach (int id in containerOrder)
            {
                xPvaV2Container container = containers[id];
                if (!xPvaV2Rules.IsLive(container))
                    continue;
                if (selected == null || CompareByStartThenId(selected, container) < 0)
                    selected = container;
            }

            return selected;
        }

        public IList<xPvaV2Container> ContainmentChildrenOf(int parentId)
        {
            var result = new List<xPvaV2Container>();
            xPvaV2Container parent = Find(parentId);
            if (parent == null)
                return result;

            foreach (int childId in parent.ContainmentChildIds)
            {
                xPvaV2Container child = Find(childId);
                if (child != null)
                    result.Add(child);
            }

            result.Sort(CompareByStartThenId);
            return result;
        }

        public IList<xPvaV2Container> JoinComponentsOf(int parentId)
        {
            var result = new List<xPvaV2Container>();
            xPvaV2Container parent = Find(parentId);
            if (parent == null)
                return result;

            foreach (int childId in parent.JoinComponentIds)
            {
                xPvaV2Container child = Find(childId);
                if (child != null)
                    result.Add(child);
            }

            result.Sort(CompareByStartThenId);
            return result;
        }

        public IList<xPvaV2Relationship> RelationshipsOf(int containerId, xPvaV2RelationshipKind kind)
        {
            var result = new List<xPvaV2Relationship>();
            List<xPvaV2Relationship> bucket;
            if (!relationshipsByContainerKind.TryGetValue(RelationshipBucketKey(containerId, kind), out bucket))
                return result;

            foreach (xPvaV2Relationship relationship in bucket)
                result.Add(relationship);

            return result;
        }

        public xPvaV2Container AddContainer(xPvaV2Container container)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            if (container.Id == 0)
                container.Id = nextContainerId++;
            else if (container.Id >= nextContainerId)
                nextContainerId = container.Id + 1;

            containers[container.Id] = container;
            if (containerOrderIds.Add(container.Id))
                containerOrder.Add(container.Id);

            return container;
        }

        public xPvaV2Container Find(int id)
        {
            xPvaV2Container container;
            return containers.TryGetValue(id, out container) ? container : null;
        }

        private static int CompareByStartThenId(xPvaV2Container a, xPvaV2Container b)
        {
            if (a == null && b == null)
                return 0;
            if (a == null)
                return -1;
            if (b == null)
                return 1;

            int startCompare = a.StartBar.CompareTo(b.StartBar);
            if (startCompare != 0)
                return startCompare;
            return a.Id.CompareTo(b.Id);
        }

        public bool LinkContainment(int parentId, int childId)
        {
            string rejectReason;
            return LinkContainment(parentId, childId, out rejectReason);
        }

        public bool LinkContainment(int parentId, int childId, out string rejectReason)
        {
            xPvaV2Container parent = Find(parentId);
            xPvaV2Container child = Find(childId);
            rejectReason = string.Empty;
            if (parent == null || child == null)
            {
                rejectReason = "missing containment endpoint";
                return false;
            }
            if (parentId == childId)
            {
                rejectReason = "containment self-link";
                return false;
            }
            if (HasRelationship(parentId, childId, xPvaV2RelationshipKind.Containment))
            {
                rejectReason = "duplicate containment link";
                return false;
            }
            if (HasContainmentPath(childId, parentId))
            {
                rejectReason = "cyclic containment link";
                return false;
            }

            AddRelationship(parentId, childId, xPvaV2RelationshipKind.Containment, xPvaV2OriginPoint.None, child.StartBar, 0.0);
            if (!parent.ContainmentChildIds.Contains(childId))
                parent.ContainmentChildIds.Add(childId);
            return true;
        }

        public bool LinkJoinComponent(int joinParentId, int componentId)
        {
            string rejectReason;
            return LinkJoinComponent(joinParentId, componentId, out rejectReason);
        }

        public bool LinkJoinComponent(int joinParentId, int componentId, out string rejectReason)
        {
            xPvaV2Container parent = Find(joinParentId);
            xPvaV2Container component = Find(componentId);
            rejectReason = string.Empty;
            if (parent == null || component == null)
            {
                rejectReason = "missing join-component endpoint";
                return false;
            }
            if (joinParentId == componentId)
            {
                rejectReason = "join-component self-link";
                return false;
            }
            if (HasRelationship(joinParentId, componentId, xPvaV2RelationshipKind.JoinComponent))
            {
                rejectReason = "duplicate join-component link";
                return false;
            }
            if (HasJoinComponentPath(componentId, joinParentId))
            {
                rejectReason = "cyclic join-component link";
                return false;
            }

            AddRelationship(joinParentId, componentId, xPvaV2RelationshipKind.JoinComponent, xPvaV2OriginPoint.None, component.StartBar, 0.0);
            if (!parent.JoinComponentIds.Contains(componentId))
                parent.JoinComponentIds.Add(componentId);
            return true;
        }

        public bool LinkP3Continuation(int sourceContainerId, int continuationContainerId)
        {
            string rejectReason;
            return LinkP3Continuation(sourceContainerId, continuationContainerId, out rejectReason);
        }

        public bool LinkP3Continuation(int sourceContainerId, int continuationContainerId, out string rejectReason)
        {
            xPvaV2Container source = Find(sourceContainerId);
            xPvaV2Container continuation = Find(continuationContainerId);
            rejectReason = string.Empty;
            if (source == null || continuation == null)
            {
                rejectReason = "missing P3-continuation endpoint";
                return false;
            }
            if (sourceContainerId == continuationContainerId)
            {
                rejectReason = "P3-continuation self-link";
                return false;
            }
            if (!xPvaV2Rules.CanLinkP3Continuation(source, continuation))
            {
                rejectReason = "invalid P3-continuation geometry";
                return false;
            }
            if (HasRelationship(source.Id, continuation.Id, xPvaV2RelationshipKind.P3Continuation))
            {
                rejectReason = "duplicate P3-continuation link";
                return false;
            }

            continuation.OriginKind = xPvaV2OriginKind.P3Continuation;
            continuation.OriginContainerId = source.Id;
            continuation.OriginPoint = xPvaV2OriginPoint.P3;
            continuation.OriginBar = source.P3.Bar;
            continuation.OriginPrice = source.P3.Price;

            AddRelationship(source.Id, continuation.Id, xPvaV2RelationshipKind.P3Continuation, xPvaV2OriginPoint.P3, source.P3.Bar, source.P3.Price);
            return true;
        }

        private bool HasJoinComponentPath(int startId, int targetId)
        {
            var visited = new List<int>();
            return HasJoinComponentPath(startId, targetId, visited);
        }

        private bool HasJoinComponentPath(int currentId, int targetId, List<int> visited)
        {
            if (currentId == targetId)
                return true;
            if (visited.Contains(currentId))
                return false;

            visited.Add(currentId);
            xPvaV2Container current = Find(currentId);
            if (current == null)
                return false;

            foreach (int componentId in current.JoinComponentIds)
                if (HasJoinComponentPath(componentId, targetId, visited))
                    return true;

            return false;
        }

        private bool HasContainmentPath(int startId, int targetId)
        {
            var visited = new List<int>();
            return HasContainmentPath(startId, targetId, visited);
        }

        private bool HasContainmentPath(int currentId, int targetId, List<int> visited)
        {
            if (currentId == targetId)
                return true;
            if (visited.Contains(currentId))
                return false;

            visited.Add(currentId);
            xPvaV2Container current = Find(currentId);
            if (current == null)
                return false;

            foreach (int childId in current.ContainmentChildIds)
                if (HasContainmentPath(childId, targetId, visited))
                    return true;

            return false;
        }

        public bool HasRelationship(int sourceId, int targetId, xPvaV2RelationshipKind kind)
        {
            return relationshipKeys.Contains(RelationshipKey(sourceId, targetId, kind));
        }

        private void AddRelationship(int sourceId, int targetId, xPvaV2RelationshipKind kind, xPvaV2OriginPoint sourcePoint, int bar, double price)
        {
            if (!relationshipKeys.Add(RelationshipKey(sourceId, targetId, kind)))
                return;

            var relationship = new xPvaV2Relationship
            {
                SourceContainerId = sourceId,
                TargetContainerId = targetId,
                Kind = kind,
                SourcePoint = sourcePoint,
                Bar = bar,
                Price = price
            };
            relationships.Add(relationship);
            AddRelationshipToBucket(sourceId, kind, relationship);
            if (targetId != sourceId)
                AddRelationshipToBucket(targetId, kind, relationship);
        }

        private static string RelationshipKey(int sourceId, int targetId, xPvaV2RelationshipKind kind)
        {
            return sourceId.ToString() + "|" + targetId.ToString() + "|" + kind.ToString();
        }

        private void AddRelationshipToBucket(int containerId, xPvaV2RelationshipKind kind, xPvaV2Relationship relationship)
        {
            string key = RelationshipBucketKey(containerId, kind);
            List<xPvaV2Relationship> bucket;
            if (!relationshipsByContainerKind.TryGetValue(key, out bucket))
            {
                bucket = new List<xPvaV2Relationship>();
                relationshipsByContainerKind[key] = bucket;
            }

            bucket.Add(relationship);
        }

        private static string RelationshipBucketKey(int containerId, xPvaV2RelationshipKind kind)
        {
            return containerId.ToString() + "|" + kind.ToString();
        }
    }

    internal static class xPvaV2Rules
    {
        private const double PriceTolerance = 0.0000001;

        public static xPvaV2Direction Opposite(xPvaV2Direction direction)
        {
            if (direction == xPvaV2Direction.Up)
                return xPvaV2Direction.Down;
            if (direction == xPvaV2Direction.Down)
                return xPvaV2Direction.Up;
            return xPvaV2Direction.Unknown;
        }

        public static bool IsValidP1P3Geometry(xPvaV2Direction direction, double p1Price, double p3Price)
        {
            if (direction == xPvaV2Direction.Up)
                return p3Price > p1Price;
            if (direction == xPvaV2Direction.Down)
                return p3Price < p1Price;
            return false;
        }

        public static bool AreAnchorBarsWithinSpan(int startBar, int endBar, xPvaV2PricePoint p1, xPvaV2PricePoint p2, xPvaV2PricePoint p3)
        {
            return IsBarWithinSpan(startBar, endBar, p1.Bar)
                && IsBarWithinSpan(startBar, endBar, p2.Bar)
                && IsBarWithinSpan(startBar, endBar, p3.Bar);
        }

        private static bool IsBarWithinSpan(int startBar, int endBar, int bar)
        {
            return bar >= startBar && bar <= endBar;
        }

        public static bool IsLive(xPvaV2Container container)
        {
            return container != null
                && (container.Status == xPvaV2ContainerStatus.Active
                    || container.Status == xPvaV2ContainerStatus.Adjusted);
        }

        public static bool CanParticipateInStructure(xPvaV2Container container)
        {
            return container != null
                && (container.Status == xPvaV2ContainerStatus.Active
                    || container.Status == xPvaV2ContainerStatus.Adjusted
                    || container.Status == xPvaV2ContainerStatus.Frozen);
        }

        public static bool IsValidJoinPoint(xPvaV2Direction direction, xPvaV2Container first, xPvaV2Container middle, xPvaV2Container continuation)
        {
            if (first == null || middle == null || continuation == null)
                return false;
            if (first.Direction != direction || continuation.Direction != direction || middle.Direction != Opposite(direction))
                return false;
            if (!StartsAtP2(first, middle))
                return false;
            if (direction == xPvaV2Direction.Up)
                return StartsAtP3(first, continuation);
            if (direction == xPvaV2Direction.Down)
                return StartsAtP3(first, continuation);
            return false;
        }

        private static bool StartsAtP2(xPvaV2Container source, xPvaV2Container middle)
        {
            return (middle.P1.Bar == source.P2.Bar || middle.StartBar == source.P2.Bar)
                && Math.Abs(middle.P1.Price - source.P2.Price) <= PriceTolerance;
        }

        private static bool StartsAtP3(xPvaV2Container source, xPvaV2Container continuation)
        {
            return (continuation.P1.Bar == source.P3.Bar || continuation.StartBar == source.P3.Bar)
                && Math.Abs(continuation.P1.Price - source.P3.Price) <= PriceTolerance;
        }

        public static bool CanLinkP3Continuation(xPvaV2Container source, xPvaV2Container continuation)
        {
            if (source == null || continuation == null)
                return false;
            if (source.Direction == xPvaV2Direction.Unknown || continuation.Direction == xPvaV2Direction.Unknown)
                return false;
            if (source.Direction != continuation.Direction)
                return false;
            if (source.P3.Bar == 0)
                return false;
            if (!StartsAtP3(source, continuation))
                return false;
            return true;
        }

        public static bool CanJoinAtP3Continuation(xPvaV2Container first, xPvaV2Container middle, xPvaV2Container continuation, out xPvaV2JoinRejectReason rejectReason)
        {
            rejectReason = xPvaV2JoinRejectReason.None;
            if (first == null || middle == null || continuation == null)
            {
                rejectReason = xPvaV2JoinRejectReason.MissingMember;
                return false;
            }
            if (first.Direction == xPvaV2Direction.Unknown || middle.Direction == xPvaV2Direction.Unknown || continuation.Direction == xPvaV2Direction.Unknown)
            {
                rejectReason = xPvaV2JoinRejectReason.UnknownDirection;
                return false;
            }
            if (first.Direction != continuation.Direction || middle.Direction != Opposite(first.Direction))
            {
                rejectReason = xPvaV2JoinRejectReason.NotAlternating;
                return false;
            }
            if (!(first.StartBar < middle.StartBar && middle.StartBar < continuation.StartBar))
            {
                rejectReason = xPvaV2JoinRejectReason.NotStartOrdered;
                return false;
            }
            if (!IsValidP1P3Geometry(first.Direction, first.P1.Price, first.P3.Price))
            {
                rejectReason = xPvaV2JoinRejectReason.P3GeometryFailed;
                return false;
            }
            if (!IsValidJoinPoint(first.Direction, first, middle, continuation))
            {
                rejectReason = xPvaV2JoinRejectReason.InvalidJoinPoint;
                return false;
            }

            return true;
        }

        public static bool IsOutsideRelation(xPvaV2BarRelation relation)
        {
            return relation == xPvaV2BarRelation.OutsideBullish
                || relation == xPvaV2BarRelation.OutsideBearish;
        }

        public static xPvaV2Direction DirectionFromRelation(xPvaV2BarRelation relation)
        {
            switch (relation)
            {
                case xPvaV2BarRelation.HHHL:
                case xPvaV2BarRelation.FTP:
                case xPvaV2BarRelation.StitchLong:
                case xPvaV2BarRelation.LowReversal:
                    return xPvaV2Direction.Up;

                case xPvaV2BarRelation.LLLH:
                case xPvaV2BarRelation.FBP:
                case xPvaV2BarRelation.StitchShort:
                case xPvaV2BarRelation.HighReversal:
                    return xPvaV2Direction.Down;

                default:
                    return xPvaV2Direction.Unknown;
            }
        }

        public static bool CanConstructTwoBarContainer(xPvaV2Bar first, xPvaV2Bar second, xPvaV2Direction direction, out xPvaV2ConstructionRejectReason rejectReason)
        {
            rejectReason = xPvaV2ConstructionRejectReason.None;
            if (first.Index == 0 || second.Index == 0)
            {
                rejectReason = xPvaV2ConstructionRejectReason.MissingBar;
                return false;
            }
            if (first.Index == second.Index)
            {
                rejectReason = xPvaV2ConstructionRejectReason.SameBar;
                return false;
            }
            if (direction == xPvaV2Direction.Unknown)
            {
                rejectReason = xPvaV2ConstructionRejectReason.UnknownDirection;
                return false;
            }
            if (IsOutsideRelation(second.RelationToPrevious))
            {
                rejectReason = xPvaV2ConstructionRejectReason.SecondBarOutside;
                return false;
            }
            if (DirectionFromRelation(second.RelationToPrevious) != direction)
            {
                rejectReason = xPvaV2ConstructionRejectReason.RelationDoesNotConstructDirection;
                return false;
            }

            double p1 = direction == xPvaV2Direction.Up ? first.Low : first.High;
            double p3 = direction == xPvaV2Direction.Up ? second.Low : second.High;
            if (direction == xPvaV2Direction.Up && p3 <= p1)
            {
                rejectReason = xPvaV2ConstructionRejectReason.UpP3NotAboveP1;
                return false;
            }
            if (direction == xPvaV2Direction.Down && p3 >= p1)
            {
                rejectReason = xPvaV2ConstructionRejectReason.DownP3NotBelowP1;
                return false;
            }

            return true;
        }

        public static xPvaV2Command PlanTwoBarConstruction(xPvaV2Bar first, xPvaV2Bar second, int visualLevel, int structuralLevel)
        {
            xPvaV2Direction direction = DirectionFromRelation(second.RelationToPrevious);
            xPvaV2ConstructionRejectReason rejectReason;
            if (!CanConstructTwoBarContainer(first, second, direction, out rejectReason))
                return new xPvaV2Command { Kind = xPvaV2CommandKind.None, Reason = rejectReason.ToString() };

            xPvaV2PricePoint p1 = direction == xPvaV2Direction.Up
                ? new xPvaV2PricePoint(first.Index, first.Low)
                : new xPvaV2PricePoint(first.Index, first.High);
            xPvaV2PricePoint p2 = direction == xPvaV2Direction.Up
                ? new xPvaV2PricePoint(second.Index, second.High)
                : new xPvaV2PricePoint(second.Index, second.Low);
            xPvaV2PricePoint p3 = direction == xPvaV2Direction.Up
                ? new xPvaV2PricePoint(second.Index, second.Low)
                : new xPvaV2PricePoint(second.Index, second.High);

            return xPvaV2Command.CreateContainer(
                direction,
                first.Index,
                second.Index,
                p1,
                p2,
                p3,
                visualLevel,
                structuralLevel,
                xPvaV2OriginKind.TwoBarConstruction,
                "two-bar construction");
        }

        public static bool CanExtendContainer(xPvaV2Container container, xPvaV2Bar bar, out xPvaV2ExtensionRejectReason rejectReason)
        {
            rejectReason = xPvaV2ExtensionRejectReason.None;
            if (container == null)
            {
                rejectReason = xPvaV2ExtensionRejectReason.MissingContainer;
                return false;
            }
            if (bar.Index == 0)
            {
                rejectReason = xPvaV2ExtensionRejectReason.MissingBar;
                return false;
            }
            if (container.Direction == xPvaV2Direction.Unknown)
            {
                rejectReason = xPvaV2ExtensionRejectReason.UnknownDirection;
                return false;
            }
            if (bar.Index < container.StartBar)
            {
                rejectReason = xPvaV2ExtensionRejectReason.BarBeforeContainer;
                return false;
            }

            xPvaV2PricePoint adjustedP3 = ExtensionP3(container, bar);
            if (!IsValidP1P3Geometry(container.Direction, container.P1.Price, adjustedP3.Price))
            {
                rejectReason = xPvaV2ExtensionRejectReason.P3GeometryFailed;
                return false;
            }

            xPvaV2Direction relationDirection = DirectionFromRelation(bar.RelationToPrevious);
            if (!IsOutsideRelation(bar.RelationToPrevious)
                && relationDirection != xPvaV2Direction.Unknown
                && relationDirection != container.Direction)
            {
                rejectReason = xPvaV2ExtensionRejectReason.RelationOpposesContainer;
                return false;
            }

            return true;
        }

        public static xPvaV2Command PlanExtension(xPvaV2Container container, xPvaV2Bar bar)
        {
            xPvaV2ExtensionRejectReason rejectReason;
            if (!CanExtendContainer(container, bar, out rejectReason))
                return new xPvaV2Command { Kind = xPvaV2CommandKind.None, ContainerId = container == null ? 0 : container.Id, Reason = rejectReason.ToString() };

            xPvaV2PricePoint adjustedP3 = ExtensionP3(container, bar);
            bool adjustP3 = adjustedP3.Bar != container.P3.Bar || adjustedP3.Price != container.P3.Price;
            return xPvaV2Command.ExtendContainer(container.Id, bar.Index, adjustedP3, adjustP3, adjustP3 ? "extension with P3 adjustment" : "extension");
        }

        private static xPvaV2PricePoint ExtensionP3(xPvaV2Container container, xPvaV2Bar bar)
        {
            if (container.Direction == xPvaV2Direction.Up)
            {
                if (bar.Low < container.P3.Price)
                    return new xPvaV2PricePoint(bar.Index, bar.Low);
            }
            else if (container.Direction == xPvaV2Direction.Down)
            {
                if (bar.High > container.P3.Price)
                    return new xPvaV2PricePoint(bar.Index, bar.High);
            }

            return container.P3;
        }

        public static double RtlValueAt(xPvaV2Container container, int bar)
        {
            if (container == null || container.P3.Bar == container.P1.Bar)
                return 0.0;

            return LineValueAt(container.P1, container.P3, bar);
        }

        public static double LineValueAt(xPvaV2PricePoint start, xPvaV2PricePoint through, int bar)
        {
            if (through.Bar == start.Bar)
                return start.Price;

            double slope = (through.Price - start.Price) / (through.Bar - start.Bar);
            return start.Price + slope * (bar - start.Bar);
        }

        public static bool BreaksRtl(xPvaV2Container container, xPvaV2Bar bar, out xPvaV2BreakoutRejectReason rejectReason)
        {
            rejectReason = xPvaV2BreakoutRejectReason.None;
            if (container == null)
            {
                rejectReason = xPvaV2BreakoutRejectReason.MissingContainer;
                return false;
            }
            if (bar.Index == 0)
            {
                rejectReason = xPvaV2BreakoutRejectReason.MissingBar;
                return false;
            }
            if (container.Direction == xPvaV2Direction.Unknown)
            {
                rejectReason = xPvaV2BreakoutRejectReason.UnknownDirection;
                return false;
            }
            if (bar.Index < container.StartBar)
            {
                rejectReason = xPvaV2BreakoutRejectReason.BarBeforeContainer;
                return false;
            }

            double rtl = RtlValueAt(container, bar.Index);
            bool breaks = container.Direction == xPvaV2Direction.Up
                ? bar.Low < rtl
                : bar.High > rtl;
            if (!breaks)
            {
                rejectReason = xPvaV2BreakoutRejectReason.NoBreakout;
                return false;
            }

            return true;
        }

        public static xPvaV2Command PlanBreakoutFreeze(xPvaV2Container container, xPvaV2Bar bar)
        {
            xPvaV2BreakoutRejectReason rejectReason;
            if (!BreaksRtl(container, bar, out rejectReason))
                return new xPvaV2Command { Kind = xPvaV2CommandKind.None, ContainerId = container == null ? 0 : container.Id, Reason = rejectReason.ToString() };

            return xPvaV2Command.FreezeContainer(container.Id, bar.Index, "RTL breakout freeze");
        }

        public static xPvaV2Command PlanPromotionAfterBreak(xPvaV2Model model, xPvaV2Container brokenParent, int bar, out xPvaV2PromotionRejectReason rejectReason)
        {
            rejectReason = xPvaV2PromotionRejectReason.None;
            if (model == null || brokenParent == null)
            {
                rejectReason = xPvaV2PromotionRejectReason.MissingBrokenParent;
                return new xPvaV2Command { Kind = xPvaV2CommandKind.None, Reason = rejectReason.ToString() };
            }
            if (IsLive(brokenParent))
            {
                rejectReason = xPvaV2PromotionRejectReason.BrokenParentStillLive;
                return new xPvaV2Command { Kind = xPvaV2CommandKind.None, ContainerId = brokenParent.Id, Reason = rejectReason.ToString() };
            }
            if (brokenParent.Direction == xPvaV2Direction.Unknown)
            {
                rejectReason = xPvaV2PromotionRejectReason.UnknownDirection;
                return new xPvaV2Command { Kind = xPvaV2CommandKind.None, ContainerId = brokenParent.Id, Reason = rejectReason.ToString() };
            }

            xPvaV2Direction promotedDirection = Opposite(brokenParent.Direction);
            xPvaV2Container selected = null;
            IList<xPvaV2Container> children = model.ContainmentChildrenOf(brokenParent.Id);
            foreach (xPvaV2Container child in children)
            {
                if (!IsLive(child) || child.Direction != promotedDirection)
                    continue;
                if (selected == null
                    || child.StructuralLevel > selected.StructuralLevel
                    || (child.StructuralLevel == selected.StructuralLevel && child.StartBar > selected.StartBar))
                    selected = child;
            }

            if (selected == null)
            {
                rejectReason = xPvaV2PromotionRejectReason.NoPromotableChild;
                return new xPvaV2Command { Kind = xPvaV2CommandKind.None, ContainerId = brokenParent.Id, Reason = rejectReason.ToString() };
            }

            int promotedLevel = Math.Max(1, brokenParent.VisualLevel);
            int promotedStructuralLevel = Math.Max(1, brokenParent.StructuralLevel);
            return xPvaV2Command.PromoteContainer(selected.Id, 0, promotedLevel, promotedStructuralLevel, bar, "promoted after parent break");
        }
    }

    internal enum xPvaV2CommandKind
    {
        None,
        CreateContainer,
        ExtendContainer,
        FreezeContainer,
        PromoteContainer,
        LinkContainment,
        LinkJoinComponent,
        JoinContainers,
        LinkP3Continuation,
        DeactivateContainer
    }

    internal sealed class xPvaV2Command
    {
        public xPvaV2CommandKind Kind;
        public int ContainerId;
        public int ParentContainerId;
        public int SourceContainerId;
        public int TargetContainerId;
        public readonly List<int> ComponentContainerIds = new List<int>();
        public xPvaV2Direction Direction;
        public int StartBar;
        public int EndBar;
        public xPvaV2PricePoint P1;
        public xPvaV2PricePoint P2;
        public xPvaV2PricePoint P3;
        public bool AdjustP3;
        public int VisualLevel;
        public int StructuralLevel;
        public xPvaV2OriginKind OriginKind;
        public string Reason;

        public xPvaV2Command()
        {
            Kind = xPvaV2CommandKind.None;
            ContainerId = 0;
            ParentContainerId = 0;
            SourceContainerId = 0;
            TargetContainerId = 0;
            Direction = xPvaV2Direction.Unknown;
            StartBar = 0;
            EndBar = 0;
            P1 = new xPvaV2PricePoint(0, 0.0);
            P2 = new xPvaV2PricePoint(0, 0.0);
            P3 = new xPvaV2PricePoint(0, 0.0);
            AdjustP3 = false;
            VisualLevel = 1;
            StructuralLevel = 1;
            OriginKind = xPvaV2OriginKind.Unknown;
            Reason = string.Empty;
        }

        public static xPvaV2Command CreateContainer(xPvaV2Direction direction, int startBar, int endBar, xPvaV2PricePoint p1, xPvaV2PricePoint p2, xPvaV2PricePoint p3, int visualLevel, int structuralLevel, xPvaV2OriginKind originKind, string reason)
        {
            return new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.CreateContainer,
                Direction = direction,
                StartBar = startBar,
                EndBar = endBar,
                P1 = p1,
                P2 = p2,
                P3 = p3,
                VisualLevel = visualLevel,
                StructuralLevel = structuralLevel,
                OriginKind = originKind,
                Reason = reason ?? string.Empty
            };
        }

        public static xPvaV2Command LinkP3Continuation(int sourceContainerId, int targetContainerId)
        {
            return new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkP3Continuation,
                SourceContainerId = sourceContainerId,
                TargetContainerId = targetContainerId,
                Reason = "P3 continuation"
            };
        }

        public static xPvaV2Command LinkContainment(int parentContainerId, int childContainerId)
        {
            return new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkContainment,
                SourceContainerId = parentContainerId,
                TargetContainerId = childContainerId,
                Reason = "containment"
            };
        }

        public static xPvaV2Command ExtendContainer(int containerId, int endBar, xPvaV2PricePoint p3, bool adjustP3, string reason)
        {
            return new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.ExtendContainer,
                ContainerId = containerId,
                EndBar = endBar,
                P3 = p3,
                AdjustP3 = adjustP3,
                Reason = reason ?? string.Empty
            };
        }

        public static xPvaV2Command FreezeContainer(int containerId, int endBar, string reason)
        {
            return new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.FreezeContainer,
                ContainerId = containerId,
                EndBar = endBar,
                Reason = reason ?? string.Empty
            };
        }

        public static xPvaV2Command PromoteContainer(int containerId, int parentContainerId, int visualLevel, int structuralLevel, int endBar, string reason)
        {
            return new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.PromoteContainer,
                ContainerId = containerId,
                ParentContainerId = parentContainerId,
                VisualLevel = visualLevel,
                StructuralLevel = structuralLevel,
                EndBar = endBar,
                Reason = reason ?? string.Empty
            };
        }

        public static xPvaV2Command JoinContainers(IList<int> componentContainerIds, xPvaV2Direction direction, int startBar, int endBar, xPvaV2PricePoint p1, xPvaV2PricePoint p2, xPvaV2PricePoint p3, int visualLevel, int structuralLevel, string reason)
        {
            var command = new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.JoinContainers,
                Direction = direction,
                StartBar = startBar,
                EndBar = endBar,
                P1 = p1,
                P2 = p2,
                P3 = p3,
                VisualLevel = visualLevel,
                StructuralLevel = structuralLevel,
                OriginKind = xPvaV2OriginKind.Join,
                Reason = reason ?? string.Empty
            };

            if (componentContainerIds != null)
            {
                foreach (int id in componentContainerIds)
                    if (!command.ComponentContainerIds.Contains(id))
                        command.ComponentContainerIds.Add(id);
            }

            return command;
        }
    }

    internal sealed class xPvaV2CommandResult
    {
        public bool Applied;
        public int ContainerId;
        public string Reason;

        public static xPvaV2CommandResult Ok(int containerId, string reason)
        {
            return new xPvaV2CommandResult { Applied = true, ContainerId = containerId, Reason = reason ?? string.Empty };
        }

        public static xPvaV2CommandResult Reject(string reason)
        {
            return new xPvaV2CommandResult { Applied = false, ContainerId = 0, Reason = reason ?? string.Empty };
        }

        public static xPvaV2CommandResult Reject(int containerId, string reason)
        {
            return new xPvaV2CommandResult { Applied = false, ContainerId = containerId, Reason = reason ?? string.Empty };
        }
    }

    internal sealed class xPvaV2Reducer
    {
        public xPvaV2CommandResult Apply(xPvaV2Model model, xPvaV2Command command)
        {
            if (model == null)
                return xPvaV2CommandResult.Reject("missing model");
            if (command == null)
                return xPvaV2CommandResult.Reject("missing command");
            if (command.Kind == xPvaV2CommandKind.None)
                return xPvaV2CommandResult.Reject(command.ContainerId, string.IsNullOrEmpty(command.Reason) ? "missing command" : command.Reason);

            switch (command.Kind)
            {
                case xPvaV2CommandKind.CreateContainer:
                    return ApplyCreateContainer(model, command);

                case xPvaV2CommandKind.ExtendContainer:
                    return ApplyExtendContainer(model, command);

                case xPvaV2CommandKind.FreezeContainer:
                    return ApplyFreeze(model, command);

                case xPvaV2CommandKind.PromoteContainer:
                    return ApplyPromotion(model, command);

                case xPvaV2CommandKind.DeactivateContainer:
                    return ApplyStatus(model, command.ContainerId, xPvaV2ContainerStatus.StructurallyDeactivated, "structurally deactivated");

                case xPvaV2CommandKind.LinkContainment:
                    return ApplyLinkContainment(model, command);

                case xPvaV2CommandKind.LinkJoinComponent:
                    return ApplyLinkJoinComponent(model, command);

                case xPvaV2CommandKind.JoinContainers:
                    return ApplyJoinContainers(model, command);

                case xPvaV2CommandKind.LinkP3Continuation:
                    return ApplyLinkP3Continuation(model, command);

                default:
                    return xPvaV2CommandResult.Reject("command not implemented: " + command.Kind);
            }
        }

        private xPvaV2CommandResult ApplyLinkContainment(xPvaV2Model model, xPvaV2Command command)
        {
            string rejectReason;
            if (model.LinkContainment(command.SourceContainerId, command.TargetContainerId, out rejectReason))
                return xPvaV2CommandResult.Ok(command.TargetContainerId, "containment linked");
            return xPvaV2CommandResult.Reject(rejectReason);
        }

        private xPvaV2CommandResult ApplyLinkJoinComponent(xPvaV2Model model, xPvaV2Command command)
        {
            string rejectReason;
            if (model.LinkJoinComponent(command.SourceContainerId, command.TargetContainerId, out rejectReason))
                return xPvaV2CommandResult.Ok(command.TargetContainerId, "join component linked");
            return xPvaV2CommandResult.Reject(rejectReason);
        }

        private xPvaV2CommandResult ApplyLinkP3Continuation(xPvaV2Model model, xPvaV2Command command)
        {
            string rejectReason;
            if (model.LinkP3Continuation(command.SourceContainerId, command.TargetContainerId, out rejectReason))
                return xPvaV2CommandResult.Ok(command.TargetContainerId, "P3 continuation linked");
            return xPvaV2CommandResult.Reject(rejectReason);
        }

        private xPvaV2CommandResult ApplyCreateContainer(xPvaV2Model model, xPvaV2Command command)
        {
            if (command.Direction == xPvaV2Direction.Unknown)
                return xPvaV2CommandResult.Reject("unknown container direction");
            if (command.EndBar < command.StartBar)
                return xPvaV2CommandResult.Reject("container end bar precedes start bar");
            if (!xPvaV2Rules.AreAnchorBarsWithinSpan(command.StartBar, command.EndBar, command.P1, command.P2, command.P3))
                return xPvaV2CommandResult.Reject("container anchor bar outside span");
            if (!xPvaV2Rules.IsValidP1P3Geometry(command.Direction, command.P1.Price, command.P3.Price))
                return xPvaV2CommandResult.Reject("invalid P1/P3 geometry");
            if (HasEquivalentContainer(model, command))
                return xPvaV2CommandResult.Reject("duplicate create container command");

            xPvaV2Container container = model.AddContainer(new xPvaV2Container
            {
                Direction = command.Direction,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = command.StartBar,
                EndBar = command.EndBar,
                P1 = command.P1,
                P2 = command.P2,
                P3 = command.P3,
                VisualLevel = Math.Max(1, command.VisualLevel),
                StructuralLevel = Math.Max(1, command.StructuralLevel),
                OriginKind = command.OriginKind
            });

            return xPvaV2CommandResult.Ok(container.Id, "container created");
        }

        private static bool HasEquivalentContainer(xPvaV2Model model, xPvaV2Command command)
        {
            return HasEquivalentContainer(model, command, command.OriginKind);
        }

        private static bool HasEquivalentContainer(xPvaV2Model model, xPvaV2Command command, xPvaV2OriginKind originKind)
        {
            int visualLevel = Math.Max(1, command.VisualLevel);
            int structuralLevel = Math.Max(1, command.StructuralLevel);
            foreach (xPvaV2Container container in model.ContainersByStart())
            {
                if (container.Direction == command.Direction
                    && container.StartBar == command.StartBar
                    && container.EndBar == command.EndBar
                    && SamePoint(container.P1, command.P1)
                    && SamePoint(container.P2, command.P2)
                    && SamePoint(container.P3, command.P3)
                    && container.VisualLevel == visualLevel
                    && container.StructuralLevel == structuralLevel
                    && container.OriginKind == originKind)
                    return true;
            }

            return false;
        }

        private static bool SamePoint(xPvaV2PricePoint left, xPvaV2PricePoint right)
        {
            return left.Bar == right.Bar && left.Price == right.Price;
        }

        private xPvaV2CommandResult ApplyJoinContainers(xPvaV2Model model, xPvaV2Command command)
        {
            if (command.ComponentContainerIds.Count < 2)
                return xPvaV2CommandResult.Reject("join requires at least two components");
            if (command.Direction == xPvaV2Direction.Unknown)
                return xPvaV2CommandResult.Reject("unknown join direction");
            if (command.EndBar < command.StartBar)
                return xPvaV2CommandResult.Reject("join end bar precedes start bar");
            if (!xPvaV2Rules.AreAnchorBarsWithinSpan(command.StartBar, command.EndBar, command.P1, command.P2, command.P3))
                return xPvaV2CommandResult.Reject("join anchor bar outside span");
            if (!xPvaV2Rules.IsValidP1P3Geometry(command.Direction, command.P1.Price, command.P3.Price))
                return xPvaV2CommandResult.Reject("invalid join P1/P3 geometry");
            if (HasEquivalentContainer(model, command, xPvaV2OriginKind.Join))
                return xPvaV2CommandResult.Reject("duplicate join parent command");

            var seenComponentIds = new List<int>();
            foreach (int componentId in command.ComponentContainerIds)
            {
                if (seenComponentIds.Contains(componentId))
                    return xPvaV2CommandResult.Reject("duplicate join component " + componentId);
                seenComponentIds.Add(componentId);

                xPvaV2Container component = model.Find(componentId);
                if (component == null)
                    return xPvaV2CommandResult.Reject("missing join component " + componentId);
                if (component.Status == xPvaV2ContainerStatus.Joined)
                    return xPvaV2CommandResult.Reject("join component already joined " + componentId);
            }

            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = command.Direction,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = command.StartBar,
                EndBar = command.EndBar,
                P1 = command.P1,
                P2 = command.P2,
                P3 = command.P3,
                VisualLevel = Math.Max(1, command.VisualLevel),
                StructuralLevel = Math.Max(1, command.StructuralLevel),
                OriginKind = xPvaV2OriginKind.Join
            });

            foreach (int componentId in command.ComponentContainerIds)
            {
                model.LinkJoinComponent(parent.Id, componentId);
                xPvaV2Container component = model.Find(componentId);
                if (component != null)
                    component.Status = xPvaV2ContainerStatus.Joined;
            }

            return xPvaV2CommandResult.Ok(parent.Id, "join parent created");
        }

        private xPvaV2CommandResult ApplyExtendContainer(xPvaV2Model model, xPvaV2Command command)
        {
            xPvaV2Container container = model.Find(command.ContainerId);
            if (container == null)
                return xPvaV2CommandResult.Reject("container not found");
            if (command.EndBar < container.EndBar)
                return xPvaV2CommandResult.Reject("cannot shorten container with extend command");
            if (command.AdjustP3
                && !xPvaV2Rules.AreAnchorBarsWithinSpan(container.StartBar, command.EndBar, container.P1, container.P2, command.P3))
                return xPvaV2CommandResult.Reject("extension P3 outside container span");
            if (command.AdjustP3
                && !xPvaV2Rules.IsValidP1P3Geometry(container.Direction, container.P1.Price, command.P3.Price))
                return xPvaV2CommandResult.Reject("extension P3 violates geometry");
            if (command.EndBar == container.EndBar
                && (!command.AdjustP3 || (command.P3.Bar == container.P3.Bar && command.P3.Price == container.P3.Price)))
                return xPvaV2CommandResult.Reject("container already extended");

            container.EndBar = command.EndBar;
            if (command.AdjustP3)
                container.P3 = command.P3;
            return xPvaV2CommandResult.Ok(container.Id, "container extended");
        }

        private xPvaV2CommandResult ApplyStatus(xPvaV2Model model, int containerId, xPvaV2ContainerStatus status, string reason)
        {
            xPvaV2Container container = model.Find(containerId);
            if (container == null)
                return xPvaV2CommandResult.Reject("container not found");
            if (container.Status == status)
                return xPvaV2CommandResult.Reject("container already has requested status");

            container.Status = status;
            return xPvaV2CommandResult.Ok(container.Id, reason);
        }

        private xPvaV2CommandResult ApplyPromotion(xPvaV2Model model, xPvaV2Command command)
        {
            xPvaV2Container container = model.Find(command.ContainerId);
            if (container == null)
                return xPvaV2CommandResult.Reject("container not found");
            if (command.EndBar < container.EndBar)
                return xPvaV2CommandResult.Reject("cannot shorten container with promotion command");
            if (container.OriginKind == xPvaV2OriginKind.Promotion
                && container.VisualLevel == Math.Max(1, command.VisualLevel)
                && container.StructuralLevel == Math.Max(1, command.StructuralLevel)
                && command.EndBar == container.EndBar)
                return xPvaV2CommandResult.Reject("container already promoted");
            if (command.ParentContainerId != 0)
            {
                string rejectReason;
                if (!model.LinkContainment(command.ParentContainerId, container.Id, out rejectReason))
                    return xPvaV2CommandResult.Reject(rejectReason);
            }

            container.VisualLevel = Math.Max(1, command.VisualLevel);
            container.StructuralLevel = Math.Max(1, command.StructuralLevel);
            container.OriginKind = xPvaV2OriginKind.Promotion;
            if (command.EndBar > container.EndBar)
                container.EndBar = command.EndBar;

            return xPvaV2CommandResult.Ok(container.Id, command.Reason);
        }

        private xPvaV2CommandResult ApplyFreeze(xPvaV2Model model, xPvaV2Command command)
        {
            xPvaV2Container container = model.Find(command.ContainerId);
            if (container == null)
                return xPvaV2CommandResult.Reject("container not found");
            if (command.EndBar < container.EndBar)
                return xPvaV2CommandResult.Reject("cannot shorten container with freeze command");
            if (container.Status == xPvaV2ContainerStatus.Frozen && command.EndBar == container.EndBar)
                return xPvaV2CommandResult.Reject("container already frozen at endpoint");

            container.Status = xPvaV2ContainerStatus.Frozen;
            if (command.EndBar > container.EndBar)
                container.EndBar = command.EndBar;
            return xPvaV2CommandResult.Ok(container.Id, command.Reason);
        }
    }

    internal sealed class xPvaV2Planner
    {
        public xPvaV2Command PlanTwoBarConstruction(xPvaV2Bar first, xPvaV2Bar second, int visualLevel, int structuralLevel)
        {
            return xPvaV2Rules.PlanTwoBarConstruction(first, second, visualLevel, structuralLevel);
        }

        public xPvaV2Command PlanExtension(xPvaV2Container container, xPvaV2Bar bar)
        {
            return xPvaV2Rules.PlanExtension(container, bar);
        }

        public xPvaV2Command PlanBreakoutFreeze(xPvaV2Container container, xPvaV2Bar bar)
        {
            return xPvaV2Rules.PlanBreakoutFreeze(container, bar);
        }

        public xPvaV2Command PlanPromotionAfterBreak(xPvaV2Model model, xPvaV2Container brokenParent, int bar)
        {
            xPvaV2PromotionRejectReason rejectReason;
            return xPvaV2Rules.PlanPromotionAfterBreak(model, brokenParent, bar, out rejectReason);
        }

        public IList<xPvaV2Command> PlanP3Continuations(xPvaV2Model model)
        {
            var commands = new List<xPvaV2Command>();
            if (model == null)
                return commands;

            IList<xPvaV2Container> ordered = model.ContainersByStart();
            for (int i = 0; i < ordered.Count; i++)
            {
                xPvaV2Container source = ordered[i];
                if (!xPvaV2Rules.CanParticipateInStructure(source)
                    || source.Direction == xPvaV2Direction.Unknown
                    || source.P3.Bar == 0)
                    continue;

                xPvaV2Container selected = null;
                for (int j = i + 1; j < ordered.Count; j++)
                {
                    xPvaV2Container candidate = ordered[j];
                    if (!xPvaV2Rules.CanParticipateInStructure(candidate))
                        continue;
                    if (candidate.StartBar > source.P3.Bar)
                        break;
                    if (candidate.Id == source.Id)
                        continue;
                    if (!xPvaV2Rules.CanLinkP3Continuation(source, candidate))
                        continue;
                    if (HasRelationship(model, source.Id, candidate.Id, xPvaV2RelationshipKind.P3Continuation))
                        continue;

                    if (selected == null || candidate.Id < selected.Id)
                        selected = candidate;
                }

                if (selected != null)
                    commands.Add(xPvaV2Command.LinkP3Continuation(source.Id, selected.Id));
            }

            return commands;
        }

        public IList<xPvaV2Command> PlanP3ContinuationJoins(xPvaV2Model model)
        {
            var commands = new List<xPvaV2Command>();
            if (model == null)
                return commands;

            IList<xPvaV2Container> ordered = model.ContainersByStart();
            for (int i = 0; i < ordered.Count; i++)
            {
                xPvaV2Container first = ordered[i];
                if (!xPvaV2Rules.CanParticipateInStructure(first))
                    continue;
                for (int k = i + 1; k < ordered.Count; k++)
                {
                    xPvaV2Container continuation = ordered[k];
                    if (!xPvaV2Rules.CanParticipateInStructure(continuation))
                        continue;
                    if (continuation.Direction != first.Direction)
                        continue;

                    xPvaV2Container selectedMiddle = null;
                    for (int j = i + 1; j < k; j++)
                    {
                        xPvaV2Container middle = ordered[j];
                        if (!xPvaV2Rules.CanParticipateInStructure(middle))
                            continue;
                        if (middle.Direction == first.Direction)
                            continue;

                        xPvaV2JoinRejectReason rejectReason;
                        if (!xPvaV2Rules.CanJoinAtP3Continuation(first, middle, continuation, out rejectReason))
                            continue;

                        if (selectedMiddle == null || middle.Id < selectedMiddle.Id)
                            selectedMiddle = middle;
                    }

                    if (selectedMiddle == null)
                        continue;
                    if (HasRelationship(model, first.Id, continuation.Id, xPvaV2RelationshipKind.P3Continuation))
                        continue;
                    if (HasPlannedP3Continuation(commands, first.Id, continuation.Id))
                        continue;

                    commands.Add(xPvaV2Command.LinkP3Continuation(first.Id, continuation.Id));
                    commands.Add(PlanJoinParent(first, selectedMiddle, continuation));
                }
            }

            return commands;
        }

        private static xPvaV2Command PlanJoinParent(xPvaV2Container first, xPvaV2Container middle, xPvaV2Container continuation)
        {
            var components = new List<int> { first.Id, middle.Id, continuation.Id };
            int visualLevel = Math.Min(first.VisualLevel, Math.Min(middle.VisualLevel, continuation.VisualLevel));
            int structuralLevel = Math.Min(first.StructuralLevel, Math.Min(middle.StructuralLevel, continuation.StructuralLevel));
            xPvaV2PricePoint p3 = JoinedP3(first, middle, continuation);
            return xPvaV2Command.JoinContainers(
                components,
                first.Direction,
                first.P1.Bar,
                Math.Max(first.EndBar, Math.Max(middle.EndBar, continuation.EndBar)),
                first.P1,
                first.P2,
                p3,
                visualLevel,
                structuralLevel,
                "P3 continuation join");
        }

        private static xPvaV2PricePoint JoinedP3(xPvaV2Container first, xPvaV2Container middle, xPvaV2Container continuation)
        {
            xPvaV2PricePoint selected = first.Direction == xPvaV2Direction.Up
                ? new xPvaV2PricePoint(middle.P2.Bar, middle.P2.Price)
                : new xPvaV2PricePoint(middle.P2.Bar, middle.P2.Price);

            if (first.Direction == xPvaV2Direction.Up)
            {
                if (middle.P3.Price < selected.Price)
                    selected = middle.P3;
                if (continuation.P3.Price < selected.Price)
                    selected = continuation.P3;
            }
            else if (first.Direction == xPvaV2Direction.Down)
            {
                if (middle.P3.Price > selected.Price)
                    selected = middle.P3;
                if (continuation.P3.Price > selected.Price)
                    selected = continuation.P3;
            }

            return selected;
        }

        private static bool HasRelationship(xPvaV2Model model, int sourceId, int targetId, xPvaV2RelationshipKind kind)
        {
            return model.HasRelationship(sourceId, targetId, kind);
        }

        private static bool HasPlannedP3Continuation(List<xPvaV2Command> commands, int sourceId, int targetId)
        {
            foreach (xPvaV2Command command in commands)
            {
                if (command.Kind == xPvaV2CommandKind.LinkP3Continuation
                    && command.SourceContainerId == sourceId
                    && command.TargetContainerId == targetId)
                    return true;
            }

            return false;
        }
    }

    internal static class xPvaV2RenderProjector
    {
        public static xPvaV2RenderSnapshot BuildSnapshot(xPvaV2Model model)
        {
            var segments = new List<xPvaV2RenderSegment>();
            if (model == null)
                return new xPvaV2RenderSnapshot(segments);

            foreach (xPvaV2Container container in model.Containers)
            {
                if (container == null || container.Direction == xPvaV2Direction.Unknown)
                    continue;
                if (container.EndBar < container.StartBar)
                    continue;
                if (container.P1.Bar == 0 || container.P2.Bar == 0 || container.P3.Bar == 0)
                    continue;

                double rtlEnd = xPvaV2Rules.LineValueAt(container.P1, container.P3, container.EndBar);
                segments.Add(new xPvaV2RenderSegment(
                    container.Id,
                    container.VisualLevel,
                    container.Direction,
                    xPvaV2RenderLineKind.Rtl,
                    container.P1.Bar,
                    container.EndBar,
                    container.P1.Price,
                    rtlEnd,
                    container.Status));

                double ltlEnd = xPvaV2Rules.LineValueAt(container.P2, OffsetPoint(container.P2, container.P3, container.P1), container.EndBar);
                segments.Add(new xPvaV2RenderSegment(
                    container.Id,
                    container.VisualLevel,
                    container.Direction,
                    xPvaV2RenderLineKind.Ltl,
                    container.P2.Bar,
                    container.EndBar,
                    container.P2.Price,
                    ltlEnd,
                    container.Status));
            }

            return new xPvaV2RenderSnapshot(segments);
        }

        private static xPvaV2PricePoint OffsetPoint(xPvaV2PricePoint anchor, xPvaV2PricePoint p3, xPvaV2PricePoint p1)
        {
            return new xPvaV2PricePoint(p3.Bar, anchor.Price + (p3.Price - p1.Price));
        }
    }

    internal sealed class xPvaV2Engine
    {
        private readonly xPvaV2Model model = new xPvaV2Model();
        private readonly xPvaV2Planner planner = new xPvaV2Planner();
        private readonly xPvaV2Reducer reducer = new xPvaV2Reducer();
        private readonly List<xPvaV2TraceEntry> lastTrace = new List<xPvaV2TraceEntry>();
        private xPvaV2RenderSnapshot renderSnapshot = new xPvaV2RenderSnapshot(null);

        public xPvaV2Model Model
        {
            get { return model; }
        }

        public xPvaV2RenderSnapshot RenderSnapshot
        {
            get { return renderSnapshot; }
        }

        public IList<xPvaV2TraceEntry> LastTrace
        {
            get { return new List<xPvaV2TraceEntry>(lastTrace); }
        }

        public xPvaV2CommandResult Apply(xPvaV2Command command)
        {
            xPvaV2CommandResult result = reducer.Apply(model, command);
            if (result.Applied)
                renderSnapshot = xPvaV2RenderProjector.BuildSnapshot(model);
            return result;
        }

        public xPvaV2CommandResult ConstructFromTwoBars(xPvaV2Bar first, xPvaV2Bar second, int visualLevel, int structuralLevel)
        {
            return Apply(planner.PlanTwoBarConstruction(first, second, visualLevel, structuralLevel));
        }

        public xPvaV2CommandResult Extend(xPvaV2Container container, xPvaV2Bar bar)
        {
            return Apply(planner.PlanExtension(container, bar));
        }

        public xPvaV2CommandResult FreezeOnBreakout(xPvaV2Container container, xPvaV2Bar bar)
        {
            return Apply(planner.PlanBreakoutFreeze(container, bar));
        }

        public IList<xPvaV2CommandResult> ApplyP3ContinuationJoins()
        {
            IList<xPvaV2Command> commands = planner.PlanP3ContinuationJoins(model);
            var results = new List<xPvaV2CommandResult>();
            foreach (xPvaV2Command command in commands)
                results.Add(Apply(command));
            return results;
        }

        private void AppendP3ContinuationJoinResults(List<xPvaV2CommandResult> results, int bar)
        {
            IList<xPvaV2CommandResult> p3Results = ApplyP3ContinuationJoins();
            foreach (xPvaV2CommandResult p3Result in p3Results)
            {
                if (p3Result.Applied)
                {
                    results.Add(p3Result);
                    Trace(xPvaV2TraceKind.P3JoinResult, bar, p3Result.ContainerId, true, p3Result.Reason);
                }
            }
        }

        public xPvaV2CommandResult PromoteAfterBreak(xPvaV2Container brokenParent, int bar)
        {
            return Apply(planner.PlanPromotionAfterBreak(model, brokenParent, bar));
        }

        public IList<xPvaV2CommandResult> ProcessSequentialBar(
            xPvaV2Bar previous,
            xPvaV2Bar current,
            int visualLevel,
            int structuralLevel)
        {
            lastTrace.Clear();
            var results = new List<xPvaV2CommandResult>();
            xPvaV2CommandResult breakout = null;
            Trace(xPvaV2TraceKind.BeginBar, current.Index, 0, true,
                "previous=" + previous.Index + " relation=" + current.RelationToPrevious + " high=" + current.High + " low=" + current.Low);

            xPvaV2Container earlyExtensionContext = BestLiveContainerForExtension(current);
            if (earlyExtensionContext != null
                && xPvaV2Rules.DirectionFromRelation(current.RelationToPrevious) == earlyExtensionContext.Direction
                && !HasLowerLevelBreakoutContext(earlyExtensionContext, current))
            {
                TraceContainerChoice(xPvaV2TraceKind.BreakoutContext, current.Index, null);
                TraceContainerChoice(xPvaV2TraceKind.ExtensionContext, current.Index, earlyExtensionContext);
                xPvaV2CommandResult earlyExtended = Extend(earlyExtensionContext, current);
                TraceResult(xPvaV2TraceKind.ExtensionResult, current.Index, earlyExtended);
                if (earlyExtended.Applied)
                {
                    results.Add(earlyExtended);
                    AppendP3ContinuationJoinResults(results, current.Index);
                    Trace(xPvaV2TraceKind.EndBar, current.Index, 0, true, "extension path");
                    return results;
                }
            }

            xPvaV2Container active = BestLiveContainerForBreakout(current);
            TraceContainerChoice(xPvaV2TraceKind.BreakoutContext, current.Index, active);

            if (active != null)
            {
                xPvaV2Container breakingContainer = active;
                breakout = FreezeOnBreakout(active, current);
                TraceResult(xPvaV2TraceKind.BreakoutResult, current.Index, breakout);
                if (breakout.Applied)
                {
                    results.Add(breakout);
                    xPvaV2CommandResult promoted = PromoteAfterBreak(breakingContainer, current.Index);
                    TraceResult(xPvaV2TraceKind.PromotionResult, current.Index, promoted);
                    if (promoted.Applied)
                    {
                        results.Add(promoted);
                        AppendP3ContinuationJoinResults(results, current.Index);
                        Trace(xPvaV2TraceKind.EndBar, current.Index, 0, true, "promotion path");
                        return results;
                    }

                    active = BestLiveContainerForExtension(current);
                    TraceContainerChoice(xPvaV2TraceKind.ExtensionContext, current.Index, active);
                }
            }

            xPvaV2Container extensionContext = BestLiveContainerForExtension(current);
            if (extensionContext != null)
                active = extensionContext;
            TraceContainerChoice(xPvaV2TraceKind.ExtensionContext, current.Index, extensionContext);

            if (active != null)
            {
                xPvaV2CommandResult extended = Extend(active, current);
                TraceResult(xPvaV2TraceKind.ExtensionResult, current.Index, extended);
                if (extended.Applied)
                {
                    results.Add(extended);
                    AppendP3ContinuationJoinResults(results, current.Index);
                    Trace(xPvaV2TraceKind.EndBar, current.Index, 0, true, "extension path");
                    return results;
                }
            }

            if (active == null && extensionContext == null)
            {
                xPvaV2Container rejectedExtensionContext = BestLiveContainerForExtensionRejection(current);
                if (rejectedExtensionContext != null)
                {
                    TraceContainerChoice(xPvaV2TraceKind.ExtensionContext, current.Index, rejectedExtensionContext);
                    TraceResult(xPvaV2TraceKind.ExtensionResult, current.Index, Extend(rejectedExtensionContext, current));
                }
            }

            if (active == null)
                active = BestLiveContainerForConstructionContext(current);

            int childVisualLevel = active == null ? visualLevel : active.VisualLevel + 1;
            int childStructuralLevel = active == null ? structuralLevel : active.StructuralLevel + 1;
            Trace(xPvaV2TraceKind.ConstructionContext, current.Index, active == null ? 0 : active.Id, true,
                "VL=" + childVisualLevel + " SL=" + childStructuralLevel);
            xPvaV2CommandResult constructed = ConstructFromTwoBars(previous, current, childVisualLevel, childStructuralLevel);
            TraceResult(xPvaV2TraceKind.ConstructionResult, current.Index, constructed);
            if (constructed.Applied || results.Count == 0)
                results.Add(constructed);
            if (constructed.Applied && active != null)
            {
                xPvaV2Container child = model.Find(constructed.ContainerId);
                if (child != null && child.Direction == xPvaV2Rules.Opposite(active.Direction))
                {
                    xPvaV2CommandResult linked = Apply(xPvaV2Command.LinkContainment(active.Id, child.Id));
                    TraceResult(xPvaV2TraceKind.ContainmentResult, current.Index, linked);
                    if (linked.Applied)
                        results.Add(linked);
                }
            }

            if (constructed.Applied)
                AppendP3ContinuationJoinResults(results, current.Index);

            Trace(xPvaV2TraceKind.EndBar, current.Index, 0, true, constructed.Applied ? "construction path" : "no structural action");
            return results;
        }

        private void TraceResult(xPvaV2TraceKind kind, int bar, xPvaV2CommandResult result)
        {
            if (result == null)
            {
                Trace(kind, bar, 0, false, "no result");
                return;
            }

            Trace(kind, bar, result.ContainerId, result.Applied, result.Reason);
        }

        private void TraceContainerChoice(xPvaV2TraceKind kind, int bar, xPvaV2Container container)
        {
            if (container == null)
            {
                Trace(kind, bar, 0, false, "none");
                return;
            }

            Trace(kind, bar, container.Id, true,
                container.Direction + " " + container.StartBar + "-" + container.EndBar
                + " VL=" + container.VisualLevel
                + " SL=" + container.StructuralLevel
                + " status=" + container.Status);
        }

        private void Trace(xPvaV2TraceKind kind, int bar, int containerId, bool applied, string detail)
        {
            lastTrace.Add(new xPvaV2TraceEntry(kind, bar, containerId, applied, detail));
        }

        public xPvaV2Container BestLiveContainerForBreakout(xPvaV2Bar bar)
        {
            IList<xPvaV2Container> active = model.ActiveContainersByStart();
            xPvaV2Container selected = null;
            for (int i = 0; i < active.Count; i++)
            {
                xPvaV2Container candidate = active[i];
                xPvaV2BreakoutRejectReason rejectReason;
                if (!xPvaV2Rules.BreaksRtl(candidate, bar, out rejectReason))
                    continue;
                if (xPvaV2Rules.DirectionFromRelation(bar.RelationToPrevious) == candidate.Direction)
                    continue;
                if (HasLowerLevelExtensionContext(candidate, bar))
                    continue;
                if (selected == null
                    || candidate.StructuralLevel < selected.StructuralLevel
                    || (candidate.StructuralLevel == selected.StructuralLevel && candidate.StartBar > selected.StartBar))
                    selected = candidate;
            }

            return selected;
        }

        private bool HasLowerLevelExtensionContext(xPvaV2Container candidate, xPvaV2Bar bar)
        {
            IList<xPvaV2Container> active = model.ActiveContainersByStart();
            for (int i = 0; i < active.Count; i++)
            {
                xPvaV2Container parent = active[i];
                if (parent.Id == candidate.Id)
                    continue;
                if (parent.StructuralLevel >= candidate.StructuralLevel)
                    continue;

                xPvaV2ExtensionRejectReason extensionRejectReason;
                xPvaV2BreakoutRejectReason breakoutRejectReason;
                if (xPvaV2Rules.CanExtendContainer(parent, bar, out extensionRejectReason)
                    && !xPvaV2Rules.BreaksRtl(parent, bar, out breakoutRejectReason))
                    return true;
            }

            return false;
        }

        private bool HasLowerLevelBreakoutContext(xPvaV2Container candidate, xPvaV2Bar bar)
        {
            IList<xPvaV2Container> active = model.ActiveContainersByStart();
            for (int i = 0; i < active.Count; i++)
            {
                xPvaV2Container parent = active[i];
                if (parent.Id == candidate.Id)
                    continue;
                if (parent.StructuralLevel >= candidate.StructuralLevel)
                    continue;

                xPvaV2BreakoutRejectReason breakoutRejectReason;
                if (xPvaV2Rules.BreaksRtl(parent, bar, out breakoutRejectReason)
                    && xPvaV2Rules.DirectionFromRelation(bar.RelationToPrevious) != parent.Direction)
                    return true;
            }

            return false;
        }

        public xPvaV2Container BestLiveContainerForExtension(xPvaV2Bar bar)
        {
            IList<xPvaV2Container> active = model.ActiveContainersByStart();
            xPvaV2Container selected = null;
            for (int i = 0; i < active.Count; i++)
            {
                xPvaV2Container candidate = active[i];
                xPvaV2ExtensionRejectReason extensionRejectReason;
                xPvaV2BreakoutRejectReason breakoutRejectReason;
                if (!xPvaV2Rules.CanExtendContainer(candidate, bar, out extensionRejectReason))
                    continue;
                if (xPvaV2Rules.BreaksRtl(candidate, bar, out breakoutRejectReason)
                    && xPvaV2Rules.DirectionFromRelation(bar.RelationToPrevious) != candidate.Direction)
                    continue;
                if (selected == null
                    || candidate.StructuralLevel > selected.StructuralLevel
                    || (candidate.StructuralLevel == selected.StructuralLevel && candidate.StartBar > selected.StartBar))
                    selected = candidate;
            }

            return selected;
        }

        private xPvaV2Container BestLiveContainerForExtensionRejection(xPvaV2Bar bar)
        {
            xPvaV2Direction relationDirection = xPvaV2Rules.DirectionFromRelation(bar.RelationToPrevious);
            if (relationDirection == xPvaV2Direction.Unknown)
                return null;

            IList<xPvaV2Container> active = model.ActiveContainersByStart();
            xPvaV2Container selected = null;
            for (int i = 0; i < active.Count; i++)
            {
                xPvaV2Container candidate = active[i];
                if (candidate.Direction != relationDirection)
                    continue;

                xPvaV2ExtensionRejectReason extensionRejectReason;
                if (xPvaV2Rules.CanExtendContainer(candidate, bar, out extensionRejectReason))
                    continue;

                if (selected == null
                    || candidate.StructuralLevel > selected.StructuralLevel
                    || (candidate.StructuralLevel == selected.StructuralLevel && candidate.StartBar > selected.StartBar))
                    selected = candidate;
            }

            return selected;
        }

        public xPvaV2Container BestLiveContainerForConstructionContext(xPvaV2Bar bar)
        {
            IList<xPvaV2Container> active = model.ActiveContainersByStart();
            xPvaV2Container selected = null;
            for (int i = 0; i < active.Count; i++)
            {
                xPvaV2Container candidate = active[i];
                xPvaV2BreakoutRejectReason breakoutRejectReason;
                if (xPvaV2Rules.BreaksRtl(candidate, bar, out breakoutRejectReason))
                    continue;
                if (selected == null
                    || candidate.StructuralLevel > selected.StructuralLevel
                    || (candidate.StructuralLevel == selected.StructuralLevel && candidate.StartBar > selected.StartBar))
                    selected = candidate;
            }

            return selected;
        }

        public xPvaV2Container LastLiveContainer()
        {
            return model.LastLiveContainerByStart();
        }
    }

    internal sealed class xPvaV2ReplayReport
    {
        public readonly xPvaV2Engine Engine;
        public readonly IList<xPvaV2Bar> Bars;
        public readonly IList<string> CommandSummaries;
        public readonly IList<string> TraceSummaries;

        public xPvaV2ReplayReport(
            xPvaV2Engine engine,
            IList<xPvaV2Bar> bars,
            IList<string> commandSummaries,
            IList<string> traceSummaries)
        {
            Engine = engine;
            Bars = bars ?? new List<xPvaV2Bar>();
            CommandSummaries = commandSummaries ?? new List<string>();
            TraceSummaries = traceSummaries ?? new List<string>();
        }
    }

    internal sealed class xPvaV2FixtureReplacementPreview
    {
        public readonly string WindowKey;
        public readonly string GeneratedFixture;
        public readonly int GeneratedRowCount;
        public readonly int CatalogRowCount;
        public readonly bool MatchesCatalog;

        public xPvaV2FixtureReplacementPreview(
            string windowKey,
            string generatedFixture,
            int generatedRowCount,
            int catalogRowCount,
            bool matchesCatalog)
        {
            WindowKey = windowKey ?? string.Empty;
            GeneratedFixture = generatedFixture ?? string.Empty;
            GeneratedRowCount = generatedRowCount;
            CatalogRowCount = catalogRowCount;
            MatchesCatalog = matchesCatalog;
        }

        public string Summary()
        {
            return "window=" + WindowKey
                + " matchesCatalog=" + MatchesCatalog
                + " generatedRows=" + GeneratedRowCount
                + " catalogRows=" + CatalogRowCount;
        }
    }

    internal static class xPvaV2FixtureReplay
    {
        public static IList<xPvaV2Bar> ParseBars(string fixture)
        {
            var bars = new List<xPvaV2Bar>();
            if (string.IsNullOrWhiteSpace(fixture))
                return bars;

            string[] lines = fixture.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripComment(lines[i]).Trim();
                if (line.Length == 0)
                    continue;

                string[] fields = line.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length != 4)
                    throw new FormatException("fixture row must be: bar,high,low,relation");

                int bar = int.Parse(fields[0], CultureInfo.InvariantCulture);
                double high = double.Parse(fields[1], CultureInfo.InvariantCulture);
                double low = double.Parse(fields[2], CultureInfo.InvariantCulture);
                xPvaV2BarRelation relation;
                if (!Enum.TryParse(fields[3], true, out relation))
                    throw new FormatException("unknown fixture relation: " + fields[3]);

                bars.Add(new xPvaV2Bar(bar, high, low, relation));
            }

            return bars;
        }

        public static IList<xPvaV2Bar> ParseDebugFixtureRows(string debugOutput)
        {
            var fixture = new StringBuilder();
            if (string.IsNullOrWhiteSpace(debugOutput))
                return ParseBars(string.Empty);

            string[] lines = debugOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                int prefix = lines[i].IndexOf(xPvaV2Nt8Adapter.FixturePrefix, StringComparison.Ordinal);
                if (prefix < 0)
                    continue;

                fixture.AppendLine(lines[i].Substring(prefix + xPvaV2Nt8Adapter.FixturePrefix.Length));
            }

            return ParseBars(fixture.ToString());
        }

        public static string BuildWindowFixtureFromDebugOutput(string debugOutput, int startBar, int endBar, string header)
        {
            IList<xPvaV2Bar> bars = ParseDebugFixtureRows(debugOutput);
            var fixture = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(header))
                fixture.Append("# ").Append(header.Trim()).Append('\n');

            for (int i = 0; i < bars.Count; i++)
            {
                if (bars[i].Index < startBar || bars[i].Index > endBar)
                    continue;

                fixture.Append(xPvaV2Nt8Adapter.ReplayFixtureRow(bars[i])).Append('\n');
            }

            return fixture.ToString().TrimEnd();
        }

        public static string BuildMarkedWindowFixtureFromDebugOutput(string debugOutput, int startBar, int endBar, string header)
        {
            string block = ExtractMarkedFixtureBlock(debugOutput, startBar, endBar);
            if (block.Length == 0)
                return BuildWindowFixtureFromDebugOutput(debugOutput, startBar, endBar, header);
            return BuildWindowFixtureFromDebugOutput(block, startBar, endBar, header);
        }

        public static string BuildMarkedWindowFixtureFromDebugOutput(string debugOutput, string windowKey)
        {
            xPvaV2EvidenceFixtures.Window window;
            if (!xPvaV2EvidenceFixtures.TryGetWindow(windowKey, out window))
                throw new ArgumentException("unknown evidence fixture window: " + windowKey, "windowKey");

            return BuildMarkedWindowFixtureFromDebugOutput(debugOutput, window.StartBar, window.EndBar, window.Header);
        }

        public static xPvaV2FixtureReplacementPreview PreviewCatalogReplacement(string debugOutput, string windowKey)
        {
            xPvaV2EvidenceFixtures.Window window;
            if (!xPvaV2EvidenceFixtures.TryGetWindow(windowKey, out window))
                throw new ArgumentException("unknown evidence fixture window: " + windowKey, "windowKey");

            string generated = BuildPreviewFixture(debugOutput, window);
            return new xPvaV2FixtureReplacementPreview(
                window.Key,
                generated,
                ParseBars(generated).Count,
                ParseBars(window.Fixture).Count,
                string.Equals(NormalizeFixtureText(window.Fixture), NormalizeFixtureText(generated), StringComparison.Ordinal));
        }

        private static string BuildPreviewFixture(string debugOutput, xPvaV2EvidenceFixtures.Window window)
        {
            string exactBlock;
            if (TryExtractMarkedFixtureBlock(debugOutput, window.StartBar, window.EndBar, out exactBlock))
                return BuildWindowFixtureFromDebugOutput(exactBlock, window.StartBar, window.EndBar, window.Header);

            string fullRangeBlock;
            if (TryExtractMarkedFixtureBlock(debugOutput, 740, 850, out fullRangeBlock))
            {
                string generated = BuildWindowFixtureFromDebugOutput(fullRangeBlock, window.StartBar, window.EndBar, window.Header);
                IList<xPvaV2Bar> generatedBars = ParseBars(generated);
                if (ContainsBar(generatedBars, window.StartBar) && ContainsBar(generatedBars, window.EndBar))
                    return generated;
            }

            return HeaderOnlyFixture(window.Header);
        }

        public static IList<xPvaV2FixtureReplacementPreview> PreviewCatalogReplacements(string debugOutput)
        {
            var previews = new List<xPvaV2FixtureReplacementPreview>();
            xPvaV2EvidenceFixtures.Window[] windows = xPvaV2EvidenceFixtures.Windows;
            for (int i = 0; i < windows.Length; i++)
                previews.Add(PreviewCatalogReplacement(debugOutput, windows[i].Key));
            return previews;
        }

        public static IList<string> PreviewCatalogReplacementSummaries(string debugOutput)
        {
            IList<xPvaV2FixtureReplacementPreview> previews = PreviewCatalogReplacements(debugOutput);
            var summaries = new List<string>();
            for (int i = 0; i < previews.Count; i++)
                summaries.Add(previews[i].Summary());
            return summaries;
        }

        private static string ExtractMarkedFixtureBlock(string debugOutput, int startBar, int endBar)
        {
            string block;
            if (TryExtractMarkedFixtureBlock(debugOutput, startBar, endBar, out block))
                return block;
            return string.Empty;
        }

        private static bool TryExtractMarkedFixtureBlock(string debugOutput, int startBar, int endBar, out string extractedBlock)
        {
            extractedBlock = string.Empty;
            if (string.IsNullOrWhiteSpace(debugOutput))
                return false;

            string begin = xPvaV2Nt8Adapter.FixtureBegin(startBar, endBar);
            string end = xPvaV2Nt8Adapter.FixtureEnd(startBar, endBar);
            var block = new StringBuilder();
            bool inside = false;
            string[] lines = debugOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                if (!inside)
                {
                    if (lines[i].IndexOf(begin, StringComparison.Ordinal) >= 0)
                        inside = true;
                    continue;
                }

                if (lines[i].IndexOf(end, StringComparison.Ordinal) >= 0)
                {
                    extractedBlock = block.ToString();
                    return true;
                }

                block.AppendLine(lines[i]);
            }

            if (inside)
                throw new FormatException("fixture block missing end marker: " + end);

            return false;
        }

        private static bool ContainsBar(IList<xPvaV2Bar> bars, int bar)
        {
            for (int i = 0; i < bars.Count; i++)
                if (bars[i].Index == bar)
                    return true;
            return false;
        }

        private static string HeaderOnlyFixture(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return string.Empty;
            return "# " + header.Trim();
        }

        private static string NormalizeFixtureText(string fixture)
        {
            return (fixture ?? string.Empty).Replace("\r\n", "\n").Trim();
        }

        public static xPvaV2ReplayReport Replay(string fixture, int fromBar, int toBar)
        {
            IList<xPvaV2Bar> bars = ParseBars(fixture);
            var engine = new xPvaV2Engine();
            var commandSummaries = new List<string>();
            var traceSummaries = new List<string>();

            for (int i = 1; i < bars.Count; i++)
            {
                xPvaV2Bar current = bars[i];
                IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(bars[i - 1], current, 1, 1);
                if (current.Index < fromBar || current.Index > toBar)
                    continue;

                for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
                    commandSummaries.Add(xPvaV2Nt8Adapter.ReplayCommandSummary(current.Index, results[resultIndex]));

                IList<xPvaV2TraceEntry> trace = engine.LastTrace;
                for (int traceIndex = 0; traceIndex < trace.Count; traceIndex++)
                    traceSummaries.Add(xPvaV2Nt8Adapter.ReplayTraceSummary(trace[traceIndex]));
            }

            return new xPvaV2ReplayReport(engine, bars, commandSummaries, traceSummaries);
        }

        private static string StripComment(string line)
        {
            int comment = line.IndexOf('#');
            return comment < 0 ? line : line.Substring(0, comment);
        }

    }

    internal static class xPvaV2EvidenceFixtures
    {
        public struct Window
        {
            public readonly string Key;
            public readonly int StartBar;
            public readonly int EndBar;
            public readonly string Header;
            public readonly string Fixture;

            public Window(string key, int startBar, int endBar, string header, string fixture)
            {
                Key = key ?? string.Empty;
                StartBar = startBar;
                EndBar = endBar;
                Header = header ?? string.Empty;
                Fixture = fixture ?? string.Empty;
            }
        }

        public const string Window805To808 =
            "# Historical malformed 805-808 construction/extension window\n"
            + "805,1700,1650,LLLH\n"
            + "806,1720,1660,HHHL\n"
            + "807,1698,1648,LLLH\n"
            + "808,1715,1640,LLLH";

        public const string Window824To831 =
            "# Historical breakout/promotion window around 824 and 831\n"
            + "823,1700,1640,LLLH\n"
            + "824,1695,1630,LLLH\n"
            + "825,1682.89,1620,HHHL\n"
            + "826,1682,1600,LLLH\n"
            + "827,1715,1650,HHHL\n"
            + "828,1710,1660,LLLH\n"
            + "829,1720,1670,HHHL\n"
            + "830,1690,1620,HHHL\n"
            + "831,1700,1665,HHHL";

        public const string Window793To850 =
            "# Historical mixed-level P3 join skeleton: 793 -> 808 -> 825 -> 850\n"
            + "793,1764.65,1710,LLLH\n"
            + "808,1685,1588,LLLH\n"
            + "809,1695,1605,HHHL\n"
            + "810,1680,1595,LLLH\n"
            + "825,1682.89,1620,HHHL\n"
            + "850,1710,1580,LLLH";

        public static readonly string[] All = new[]
        {
            Window805To808,
            Window824To831,
            Window793To850
        };

        public static readonly Window[] Windows = new[]
        {
            new Window("805-808", 805, 808, "Historical malformed 805-808 construction/extension window", Window805To808),
            new Window("824-831", 823, 831, "Historical breakout/promotion window around 824 and 831", Window824To831),
            new Window("793-850", 793, 850, "Historical mixed-level P3 join skeleton: 793 -> 808 -> 825 -> 850", Window793To850)
        };

        public static bool TryGetWindow(string key, out Window window)
        {
            for (int i = 0; i < Windows.Length; i++)
            {
                if (string.Equals(Windows[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    window = Windows[i];
                    return true;
                }
            }

            window = new Window();
            return false;
        }
    }

    internal static class xPvaV2ModelSelfTest
    {
        public static readonly int TestCount = 114;

        public static IList<string> Run()
        {
            var failures = new List<string>();
            AssertUpP3ContinuationLinks(failures);
            AssertDownP3ContinuationLinks(failures);
            AssertContinuationRejectsWrongPoint(failures);
            AssertReducerKeepsP3ContinuationSeparate(failures);
            AssertQueryHelpersReturnStableOrdering(failures);
            AssertContainerOrderIndexKeepsReplacementStable(failures);
            AssertLastLiveContainerByStartMatchesSortedQuery(failures);
            AssertRelationshipIndexPreventsDuplicateLinks(failures);
            AssertRelationshipQueriesReturnSourceAndTargetBuckets(failures);
            AssertPlannerFindsP3Continuation(failures);
            AssertPlannerChoosesSingleP3ContinuationPerSource(failures);
            AssertPlannerFindsDownP3ContinuationJoin(failures);
            AssertPlannerChoosesSingleMiddleForP3Join(failures);
            AssertP3ContinuationJoinAllowsMixedLevels(failures);
            AssertMixedLevelP3JoinCreatesOuterLiveParent(failures);
            AssertPlannerRejectsP3JoinFromWrongPrice(failures);
            AssertPlannerRejectsP3JoinFromWrongMiddleAnchor(failures);
            AssertPlannerSkipsExistingP3Continuation(failures);
            AssertDownP3ContinuationJoinIsIdempotent(failures);
            AssertPlannerIgnoresJoinedComponentsForNewP3Joins(failures);
            AssertReducerRejectsReplayedJoinCommand(failures);
            AssertReducerRejectsDuplicateJoinParentCommand(failures);
            AssertReducerRejectsDuplicateJoinComponents(failures);
            AssertReducerRejectsInvalidJoinComponentLink(failures);
            AssertReducerRejectsCyclicJoinComponentLink(failures);
            AssertReducerRejectsInvalidContainmentLink(failures);
            AssertReducerRejectsCyclicContainmentLink(failures);
            AssertReducerRejectsInvalidP3ContinuationLink(failures);
            AssertReducerRejectsDuplicateDirectRelationshipLinks(failures);
            AssertReducerReportsSpecificRelationshipRejectReasons(failures);
            AssertReducerRejectsDuplicateCreateContainerCommand(failures);
            AssertReducerRejectsBackwardContainerSpans(failures);
            AssertReducerRejectsOutOfSpanAnchorBars(failures);
            AssertReducerCreatesJoinParentWithComponents(failures);
            AssertReducerRejectsShorteningFreezeAndPromotion(failures);
            AssertReducerRejectsPromotionWhenParentLinkFails(failures);
            AssertReducerRejectsDuplicateStatusCommands(failures);
            AssertReducerRejectsDuplicatePromotionCommand(failures);
            AssertReducerRejectsDuplicatePromotionBeforeParentLink(failures);
            AssertTwoBarUpConstructionRequiresHigherP3(failures);
            AssertTwoBarDownConstructionRequiresLowerP3(failures);
            AssertTwoBarConstructionRejectsSecondBarOutside(failures);
            AssertOutsideBarCanBeFirstBarInTwoBarConstruction(failures);
            AssertOutsideBarCanExtendWithP3Adjustment(failures);
            AssertInsideAndSameHighSameLowCanExtendExistingContainer(failures);
            AssertReducerRejectsInvalidExtensionP3Adjustment(failures);
            AssertReducerRejectsNoOpExtensionCommand(failures);
            AssertExtensionRejectsOppositeNonOutsideRelation(failures);
            AssertUpBreakoutFreezesContainer(failures);
            AssertDownBreakoutFreezesContainer(failures);
            AssertBreakoutRejectsNonBreak(failures);
            AssertPromotionSelectsBestOppositeChild(failures);
            AssertPromotionRejectsWhenParentStillLive(failures);
            AssertRenderSnapshotIncludesJoinedAndFrozenContainers(failures);
            AssertRenderSnapshotIncludesEveryContainerStatus(failures);
            AssertRenderSnapshotIncludesP3JoinParentAndComponents(failures);
            AssertNt8AdapterTranslatesBarRelations(failures);
            AssertNt8AdapterResolvesDebugBoundsWithoutChangingModelRules(failures);
            AssertNt8AdapterCreatesDeterministicRenderSegmentTags(failures);
            AssertNt8AdapterExportsReplayCommandSummaries(failures);
            AssertNt8AdapterExportsReplayTraceSummaries(failures);
            AssertNt8AdapterFormatsSelfTestSummary(failures);
            AssertNt8AdapterExportsCompactFixtureRows(failures);
            AssertNt8AdapterFixtureRowsRoundTripThroughReplayParser(failures);
            AssertNt8AdapterExportsFixtureBlockMarkers(failures);
            AssertNt8AdapterExportsFixturePreviewSummaries(failures);
            AssertFixtureReplayExtractsRowsFromDebugOutput(failures);
            AssertFixtureReplayIgnoresFixtureBlockMarkers(failures);
            AssertFixtureReplayIgnoresFixturePreviewLines(failures);
            AssertFixtureReplayIgnoresDebugOutputWithoutFixtureRows(failures);
            AssertFixtureReplayBuildsWindowFixtureFromDebugOutput(failures);
            AssertFixtureReplayBuiltWindowRoundTripsThroughParser(failures);
            AssertFixtureReplayBuildsMarkedWindowFixtureFromDebugOutput(failures);
            AssertFixtureReplayMarkedWindowMissingReturnsHeaderOnly(failures);
            AssertFixtureReplayRejectsUnterminatedMarkedWindow(failures);
            AssertFixtureReplayBuildsMarkedWindowFixtureByCatalogKey(failures);
            AssertFixtureReplayRejectsUnknownCatalogKey(failures);
            AssertFixtureReplayPreviewsMatchingCatalogReplacement(failures);
            AssertFixtureReplayPreviewsChangedCatalogReplacement(failures);
            AssertFixtureReplayPreviewSummaryReportsRowCounts(failures);
            AssertFixtureReplayPreviewsAllCatalogReplacements(failures);
            AssertFixtureReplayPreviewsPartialCatalogReplacements(failures);
            AssertFixtureReplayPreviewSummariesFollowCatalogOrder(failures);
            AssertFixtureReplayPreviewFallsBackToFullRangeFixtureBlock(failures);
            AssertFixtureReplayParsesCompactBarRows(failures);
            AssertFixtureReplayRejectsMalformedRows(failures);
            AssertFixtureReplayRejectsUnknownRelations(failures);
            AssertFixtureReplayEmitsFilteredCommandAndTraceSummaries(failures);
            AssertFixtureReplayUsesNt8ExportSummaryFormat(failures);
            AssertEvidenceFixtureCatalogParsesNamedWindows(failures);
            AssertEvidenceFixtureCatalogUsesChronologicalRows(failures);
            AssertEvidenceFixtureCatalogReplaysProblemWindows(failures);
            AssertEvidenceFixtureCatalogContainsHistoricalAnchorBars(failures);
            AssertEvidenceFixtureReplayFiltersTargetBars(failures);
            AssertEvidenceFixtureCatalogFindsWindowsByKey(failures);
            AssertEvidenceFixtureCatalogWindowMetadataMatchesFixtures(failures);
            AssertEngineAppliesP3ContinuationJoinFlow(failures);
            AssertEngineTracksLastLiveContainer(failures);
            AssertEngineProcessesBreakoutBeforeExtension(failures);
            AssertEnginePromotesAfterBreakoutFreeze(failures);
            AssertEngineLinksConstructedOppositeChildContainment(failures);
            AssertEngineReturnsExtensionFocusToParent(failures);
            AssertEngineBreakoutFocusCanReturnToParent(failures);
            AssertSequentialProcessingAppliesP3ContinuationJoinFlow(failures);
            AssertSequentialProcessingAppliesDownP3ContinuationJoinFlow(failures);
            AssertSequentialTraceCapturesRejectedConstruction(failures);
            AssertFixture805To808RejectsMalformedDownContainer(failures);
            AssertFixture827To829DoesNotAdjustDownContainerUpward(failures);
            AssertFixture824BreakoutPromotesExistingDownChild(failures);
            AssertFixture831BreakoutPromotesExistingUpChild(failures);
            return failures;
        }

        private static void AssertUpP3ContinuationLinks(List<string> failures)
        {
            var model = new xPvaV2Model();
            xPvaV2Container up1 = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            xPvaV2Container up2 = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 781,
                EndBar = 864,
                P1 = new xPvaV2PricePoint(781, 1590.01),
                P2 = new xPvaV2PricePoint(864, 1783.89),
                P3 = new xPvaV2PricePoint(850, 1625.37)
            });

            if (!model.LinkP3Continuation(up1.Id, up2.Id)
                || up2.OriginKind != xPvaV2OriginKind.P3Continuation
                || up2.OriginContainerId != up1.Id
                || up2.OriginPoint != xPvaV2OriginPoint.P3
                || up2.OriginBar != 781)
                failures.Add("Expected Up continuation at source P3 781 to link 740-origin container to 781-origin container.");
        }

        private static void AssertDownP3ContinuationLinks(List<string> failures)
        {
            var model = new xPvaV2Model();
            xPvaV2Container down1 = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 793,
                EndBar = 850,
                P1 = new xPvaV2PricePoint(793, 1764.65),
                P2 = new xPvaV2PricePoint(808, 1588.0),
                P3 = new xPvaV2PricePoint(825, 1682.89)
            });
            xPvaV2Container down2 = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 825,
                EndBar = 850,
                P1 = new xPvaV2PricePoint(825, 1682.89),
                P2 = new xPvaV2PricePoint(850, 1625.37),
                P3 = new xPvaV2PricePoint(836, 1682.0)
            });

            if (!model.LinkP3Continuation(down1.Id, down2.Id)
                || down2.OriginKind != xPvaV2OriginKind.P3Continuation
                || down2.OriginPoint != xPvaV2OriginPoint.P3)
                failures.Add("Expected Down continuation at source P3 to link.");
        }

        private static void AssertContinuationRejectsWrongPoint(List<string> failures)
        {
            var model = new xPvaV2Model();
            xPvaV2Container source = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            xPvaV2Container badContinuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 782,
                EndBar = 864,
                P1 = new xPvaV2PricePoint(782, 1600.0),
                P2 = new xPvaV2PricePoint(864, 1783.89),
                P3 = new xPvaV2PricePoint(850, 1625.37)
            });

            if (model.LinkP3Continuation(source.Id, badContinuation.Id))
                failures.Add("Expected P3 continuation to reject when continuation does not start at source P3.");
        }

        private static void AssertReducerKeepsP3ContinuationSeparate(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2CommandResult firstResult = reducer.Apply(model, xPvaV2Command.CreateContainer(
                xPvaV2Direction.Up,
                740,
                783,
                new xPvaV2PricePoint(740, 1500.0),
                new xPvaV2PricePoint(769, 1684.68),
                new xPvaV2PricePoint(781, 1590.01),
                1,
                1,
                xPvaV2OriginKind.Join,
                "source up container"));

            xPvaV2CommandResult secondResult = reducer.Apply(model, xPvaV2Command.CreateContainer(
                xPvaV2Direction.Up,
                781,
                864,
                new xPvaV2PricePoint(781, 1590.01),
                new xPvaV2PricePoint(864, 1783.89),
                new xPvaV2PricePoint(850, 1625.37),
                1,
                1,
                xPvaV2OriginKind.Join,
                "continuation up container"));

            xPvaV2CommandResult linkResult = reducer.Apply(model, xPvaV2Command.LinkP3Continuation(firstResult.ContainerId, secondResult.ContainerId));
            xPvaV2Container first = model.Find(firstResult.ContainerId);
            xPvaV2Container second = model.Find(secondResult.ContainerId);

            if (!firstResult.Applied || !secondResult.Applied || !linkResult.Applied)
            {
                failures.Add("Expected reducer to create two Up containers and link P3 continuation.");
                return;
            }
            if (first.ContainmentChildIds.Contains(second.Id) || first.JoinComponentIds.Contains(second.Id))
                failures.Add("P3 continuation must not be recorded as containment or join-component relationship.");
            if (second.OriginKind != xPvaV2OriginKind.P3Continuation || second.OriginContainerId != first.Id)
                failures.Add("P3 continuation command did not stamp continuation origin metadata.");
        }

        private static void AssertQueryHelpersReturnStableOrdering(List<string> failures)
        {
            var model = new xPvaV2Model();
            xPvaV2Container later = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 850,
                EndBar = 855,
                P1 = new xPvaV2PricePoint(850, 1700.0),
                P2 = new xPvaV2PricePoint(855, 1600.0),
                P3 = new xPvaV2PricePoint(853, 1650.0)
            });
            xPvaV2Container earlier = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 781,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            xPvaV2Container frozen = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Frozen,
                StartBar = 700,
                EndBar = 720,
                P1 = new xPvaV2PricePoint(700, 1400.0),
                P2 = new xPvaV2PricePoint(710, 1500.0),
                P3 = new xPvaV2PricePoint(720, 1450.0)
            });

            IList<xPvaV2Container> byStart = model.ContainersByStart();
            IList<xPvaV2Container> activeByStart = model.ActiveContainersByStart();

            if (byStart.Count != 3 || byStart[0].Id != frozen.Id || byStart[1].Id != earlier.Id || byStart[2].Id != later.Id)
                failures.Add("Expected ContainersByStart to return all containers sorted by StartBar then Id.");
            if (activeByStart.Count != 2 || activeByStart[0].Id != earlier.Id || activeByStart[1].Id != later.Id)
                failures.Add("Expected ActiveContainersByStart to exclude frozen containers and preserve start ordering.");
        }

        private static void AssertContainerOrderIndexKeepsReplacementStable(List<string> failures)
        {
            var model = new xPvaV2Model();
            model.AddContainer(new xPvaV2Container
            {
                Id = 10,
                Direction = xPvaV2Direction.Up,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 100.0),
                P2 = new xPvaV2PricePoint(25, 140.0),
                P3 = new xPvaV2PricePoint(28, 120.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Id = 11,
                Direction = xPvaV2Direction.Down,
                StartBar = 40,
                EndBar = 50,
                P1 = new xPvaV2PricePoint(40, 150.0),
                P2 = new xPvaV2PricePoint(45, 100.0),
                P3 = new xPvaV2PricePoint(48, 130.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Id = 10,
                Direction = xPvaV2Direction.Up,
                StartBar = 5,
                EndBar = 15,
                P1 = new xPvaV2PricePoint(5, 90.0),
                P2 = new xPvaV2PricePoint(10, 130.0),
                P3 = new xPvaV2PricePoint(12, 110.0)
            });

            int iterationCount = 0;
            int firstIterationId = 0;
            int secondIterationId = 0;
            foreach (xPvaV2Container container in model.Containers)
            {
                iterationCount++;
                if (iterationCount == 1)
                    firstIterationId = container.Id;
                if (iterationCount == 2)
                    secondIterationId = container.Id;
            }

            IList<xPvaV2Container> byStart = model.ContainersByStart();
            if (iterationCount != 2
                || firstIterationId != 10
                || secondIterationId != 11
                || byStart.Count != 2
                || byStart[0].Id != 10
                || byStart[0].StartBar != 5
                || byStart[1].Id != 11)
                failures.Add("Expected container order index to keep replacement IDs unique while preserving iteration order.");
        }

        private static void AssertLastLiveContainerByStartMatchesSortedQuery(List<string> failures)
        {
            var model = new xPvaV2Model();
            model.AddContainer(new xPvaV2Container
            {
                Id = 1,
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 100.0),
                P2 = new xPvaV2PricePoint(25, 140.0),
                P3 = new xPvaV2PricePoint(28, 120.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Id = 2,
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Frozen,
                StartBar = 90,
                EndBar = 100,
                P1 = new xPvaV2PricePoint(90, 160.0),
                P2 = new xPvaV2PricePoint(95, 100.0),
                P3 = new xPvaV2PricePoint(98, 130.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Id = 3,
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 40,
                EndBar = 50,
                P1 = new xPvaV2PricePoint(40, 110.0),
                P2 = new xPvaV2PricePoint(45, 150.0),
                P3 = new xPvaV2PricePoint(48, 130.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Id = 4,
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 40,
                EndBar = 55,
                P1 = new xPvaV2PricePoint(40, 112.0),
                P2 = new xPvaV2PricePoint(46, 152.0),
                P3 = new xPvaV2PricePoint(49, 132.0)
            });

            IList<xPvaV2Container> activeByStart = model.ActiveContainersByStart();
            xPvaV2Container selected = model.LastLiveContainerByStart();

            if (activeByStart.Count != 3
                || selected == null
                || selected.Id != activeByStart[activeByStart.Count - 1].Id
                || selected.Id != 4)
                failures.Add("Expected direct last-live lookup to match ActiveContainersByStart final item and exclude frozen containers.");
        }

        private static void AssertRelationshipIndexPreventsDuplicateLinks(List<string> failures)
        {
            var model = new xPvaV2Model();
            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });
            xPvaV2Container child = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 30,
                EndBar = 60,
                P1 = new xPvaV2PricePoint(30, 120.0),
                P2 = new xPvaV2PricePoint(40, 160.0),
                P3 = new xPvaV2PricePoint(50, 130.0)
            });

            bool containmentFirst = model.LinkContainment(parent.Id, child.Id);
            bool containmentDuplicate = model.LinkContainment(parent.Id, child.Id);
            bool joinFirst = model.LinkJoinComponent(parent.Id, child.Id);
            bool joinDuplicate = model.LinkJoinComponent(parent.Id, child.Id);
            bool p3First = model.LinkP3Continuation(parent.Id, continuation.Id);
            bool p3Duplicate = model.LinkP3Continuation(parent.Id, continuation.Id);

            int relationshipCount = 0;
            foreach (xPvaV2Relationship relationship in model.Relationships)
                relationshipCount++;

            if (!containmentFirst
                || containmentDuplicate
                || !joinFirst
                || joinDuplicate
                || !p3First
                || p3Duplicate
                || relationshipCount != 3
                || parent.ContainmentChildIds.Count != 1
                || parent.JoinComponentIds.Count != 1)
                failures.Add("Expected relationship index to reject duplicate model links while preserving one relationship per kind.");
        }

        private static void AssertRelationshipQueriesReturnSourceAndTargetBuckets(List<string> failures)
        {
            var model = new xPvaV2Model();
            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });
            xPvaV2Container child = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 30,
                EndBar = 60,
                P1 = new xPvaV2PricePoint(30, 120.0),
                P2 = new xPvaV2PricePoint(40, 160.0),
                P3 = new xPvaV2PricePoint(50, 130.0)
            });

            model.LinkContainment(parent.Id, child.Id);
            model.LinkP3Continuation(parent.Id, continuation.Id);

            IList<xPvaV2Relationship> parentContainment = model.RelationshipsOf(parent.Id, xPvaV2RelationshipKind.Containment);
            IList<xPvaV2Relationship> childContainment = model.RelationshipsOf(child.Id, xPvaV2RelationshipKind.Containment);
            IList<xPvaV2Relationship> parentP3 = model.RelationshipsOf(parent.Id, xPvaV2RelationshipKind.P3Continuation);
            IList<xPvaV2Relationship> continuationP3 = model.RelationshipsOf(continuation.Id, xPvaV2RelationshipKind.P3Continuation);

            if (parentContainment.Count != 1
                || childContainment.Count != 1
                || parentContainment[0].SourceContainerId != parent.Id
                || childContainment[0].TargetContainerId != child.Id
                || parentP3.Count != 1
                || continuationP3.Count != 1
                || parentP3[0].TargetContainerId != continuation.Id
                || continuationP3[0].SourceContainerId != parent.Id
                || model.RelationshipsOf(parent.Id, xPvaV2RelationshipKind.JoinComponent).Count != 0)
                failures.Add("Expected indexed relationship queries to return both source-side and target-side buckets by kind.");
        }

        private static void AssertPlannerFindsP3Continuation(List<string> failures)
        {
            var model = new xPvaV2Model();
            var planner = new xPvaV2Planner();

            xPvaV2Container source = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 769,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(769, 1684.68),
                P2 = new xPvaV2PricePoint(781, 1590.01),
                P3 = new xPvaV2PricePoint(776, 1640.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 781,
                EndBar = 864,
                P1 = new xPvaV2PricePoint(781, 1590.01),
                P2 = new xPvaV2PricePoint(864, 1783.89),
                P3 = new xPvaV2PricePoint(850, 1625.37)
            });

            IList<xPvaV2Command> commands = planner.PlanP3ContinuationJoins(model);
            if (commands.Count != 2
                || commands[0].Kind != xPvaV2CommandKind.LinkP3Continuation
                || commands[0].SourceContainerId != source.Id
                || commands[0].TargetContainerId != continuation.Id
                || commands[1].Kind != xPvaV2CommandKind.JoinContainers
                || commands[1].ComponentContainerIds.Count != 3)
                failures.Add("Expected planner to find P3 continuation link plus join-parent command across source/middle/continuation triad.");
        }

        private static void AssertPlannerChoosesSingleP3ContinuationPerSource(List<string> failures)
        {
            var model = new xPvaV2Model();
            var planner = new xPvaV2Planner();

            xPvaV2Container source = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            xPvaV2Container firstCandidate = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 781,
                EndBar = 840,
                P1 = new xPvaV2PricePoint(781, 1590.01),
                P2 = new xPvaV2PricePoint(840, 1710.0),
                P3 = new xPvaV2PricePoint(825, 1625.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 781,
                EndBar = 864,
                P1 = new xPvaV2PricePoint(781, 1590.01),
                P2 = new xPvaV2PricePoint(864, 1783.89),
                P3 = new xPvaV2PricePoint(850, 1625.37)
            });

            IList<xPvaV2Command> commands = planner.PlanP3Continuations(model);
            if (commands.Count != 1
                || commands[0].SourceContainerId != source.Id
                || commands[0].TargetContainerId != firstCandidate.Id)
                failures.Add("Expected planner to choose a single deterministic P3 continuation per source.");
        }

        private static void AssertPlannerFindsDownP3ContinuationJoin(List<string> failures)
        {
            var model = new xPvaV2Model();
            var planner = new xPvaV2Planner();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 793,
                EndBar = 808,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(793, 1764.65),
                P2 = new xPvaV2PricePoint(808, 1588.0),
                P3 = new xPvaV2PricePoint(825, 1682.89)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 808,
                EndBar = 825,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(808, 1588.0),
                P2 = new xPvaV2PricePoint(825, 1700.0),
                P3 = new xPvaV2PricePoint(810, 1600.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 825,
                EndBar = 850,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(825, 1682.89),
                P2 = new xPvaV2PricePoint(850, 1625.37),
                P3 = new xPvaV2PricePoint(836, 1682.0)
            });

            IList<xPvaV2Command> commands = planner.PlanP3ContinuationJoins(model);
            if (commands.Count != 2
                || commands[0].Kind != xPvaV2CommandKind.LinkP3Continuation
                || commands[0].SourceContainerId != first.Id
                || commands[0].TargetContainerId != continuation.Id
                || commands[1].Kind != xPvaV2CommandKind.JoinContainers
                || commands[1].Direction != xPvaV2Direction.Down
                || commands[1].StartBar != first.StartBar
                || commands[1].P3.Price != middle.P2.Price
                || commands[1].ComponentContainerIds.Count != 3)
            {
                failures.Add("Expected planner to find Down P3 continuation link plus valid Down join-parent command across 793/808/825 triad.");
                return;
            }

            xPvaV2CommandResult linkResult = reducer.Apply(model, commands[0]);
            xPvaV2CommandResult joinResult = reducer.Apply(model, commands[1]);
            xPvaV2Container joinParent = model.Find(joinResult.ContainerId);
            if (!linkResult.Applied
                || !joinResult.Applied
                || joinParent == null
                || joinParent.Direction != xPvaV2Direction.Down
                || joinParent.StartBar != 793
                || joinParent.P3.Price != middle.P2.Price
                || continuation.OriginKind != xPvaV2OriginKind.P3Continuation
                || continuation.OriginPoint != xPvaV2OriginPoint.P3
                || first.Status != xPvaV2ContainerStatus.Joined
                || middle.Status != xPvaV2ContainerStatus.Joined
                || continuation.Status != xPvaV2ContainerStatus.Joined
                || joinParent.JoinComponentIds.Count != 3
                || joinParent.ContainmentChildIds.Count != 0)
                failures.Add("Expected Down P3 continuation join to apply as a joined parent with three components and no containment mutation.");
        }

        private static void AssertPlannerChoosesSingleMiddleForP3Join(List<string> failures)
        {
            var model = new xPvaV2Model();
            var planner = new xPvaV2Planner();

            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });
            xPvaV2Container selectedMiddle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(26, 112.0),
                P3 = new xPvaV2PricePoint(29, 132.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 30,
                EndBar = 60,
                P1 = new xPvaV2PricePoint(30, 120.0),
                P2 = new xPvaV2PricePoint(40, 160.0),
                P3 = new xPvaV2PricePoint(50, 130.0)
            });

            IList<xPvaV2Command> commands = planner.PlanP3ContinuationJoins(model);
            if (commands.Count != 2
                || commands[0].SourceContainerId != first.Id
                || commands[0].TargetContainerId != continuation.Id
                || commands[1].ComponentContainerIds.Count != 3
                || !commands[1].ComponentContainerIds.Contains(selectedMiddle.Id))
                failures.Add("Expected P3 join planner to choose one deterministic middle component for a first/continuation pair.");
        }

        private static void AssertP3ContinuationJoinAllowsMixedLevels(List<string> failures)
        {
            var model = new xPvaV2Model();
            var planner = new xPvaV2Planner();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 793,
                EndBar = 808,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(793, 1764.65),
                P2 = new xPvaV2PricePoint(808, 1588.0),
                P3 = new xPvaV2PricePoint(825, 1682.89)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 808,
                EndBar = 825,
                VisualLevel = 3,
                StructuralLevel = 3,
                P1 = new xPvaV2PricePoint(808, 1588.0),
                P2 = new xPvaV2PricePoint(825, 1700.0),
                P3 = new xPvaV2PricePoint(810, 1600.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 825,
                EndBar = 850,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(825, 1682.89),
                P2 = new xPvaV2PricePoint(850, 1625.37),
                P3 = new xPvaV2PricePoint(836, 1682.0)
            });

            IList<xPvaV2Command> commands = planner.PlanP3ContinuationJoins(model);
            if (commands.Count != 2
                || commands[1].Kind != xPvaV2CommandKind.JoinContainers
                || commands[1].VisualLevel != 1
                || commands[1].StructuralLevel != 1
                || commands[1].ComponentContainerIds.Count != 3)
            {
                failures.Add("Expected P3 continuation join to allow mixed component levels and choose the outermost join-parent level.");
                return;
            }

            reducer.Apply(model, commands[0]);
            xPvaV2CommandResult joinResult = reducer.Apply(model, commands[1]);
            xPvaV2Container joinParent = model.Find(joinResult.ContainerId);
            if (!joinResult.Applied
                || joinParent == null
                || joinParent.VisualLevel != 1
                || joinParent.StructuralLevel != 1
                || first.Status != xPvaV2ContainerStatus.Joined
                || middle.Status != xPvaV2ContainerStatus.Joined
                || continuation.Status != xPvaV2ContainerStatus.Joined)
                failures.Add("Expected mixed-level P3 continuation join to apply and preserve joined component statuses.");
        }

        private static void AssertMixedLevelP3JoinCreatesOuterLiveParent(List<string> failures)
        {
            var model = new xPvaV2Model();
            var planner = new xPvaV2Planner();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 793,
                EndBar = 808,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(793, 1764.65),
                P2 = new xPvaV2PricePoint(808, 1588.0),
                P3 = new xPvaV2PricePoint(825, 1682.89)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 808,
                EndBar = 825,
                VisualLevel = 3,
                StructuralLevel = 3,
                P1 = new xPvaV2PricePoint(808, 1588.0),
                P2 = new xPvaV2PricePoint(825, 1700.0),
                P3 = new xPvaV2PricePoint(810, 1600.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 825,
                EndBar = 850,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(825, 1682.89),
                P2 = new xPvaV2PricePoint(850, 1625.37),
                P3 = new xPvaV2PricePoint(836, 1682.0)
            });

            IList<xPvaV2Command> commands = planner.PlanP3ContinuationJoins(model);
            if (commands.Count != 2)
            {
                failures.Add("Expected mixed-level 793/808/825/850 fixture to produce P3 link plus join parent commands.");
                return;
            }

            xPvaV2CommandResult linkResult = reducer.Apply(model, commands[0]);
            xPvaV2CommandResult joinResult = reducer.Apply(model, commands[1]);
            xPvaV2Container joinParent = model.Find(joinResult.ContainerId);
            IList<xPvaV2Container> live = model.ActiveContainersByStart();

            if (!linkResult.Applied
                || !joinResult.Applied
                || joinParent == null
                || joinParent.Direction != xPvaV2Direction.Down
                || joinParent.StartBar != 793
                || joinParent.EndBar != 850
                || joinParent.VisualLevel != 1
                || joinParent.StructuralLevel != 1
                || joinParent.P1.Bar != first.P1.Bar
                || joinParent.P2.Bar != first.P2.Bar
                || joinParent.P3.Bar != middle.P2.Bar
                || joinParent.P3.Price != middle.P2.Price
                || joinParent.JoinComponentIds.Count != 3
                || first.Status != xPvaV2ContainerStatus.Joined
                || middle.Status != xPvaV2ContainerStatus.Joined
                || continuation.Status != xPvaV2ContainerStatus.Joined
                || live.Count != 1
                || live[0].Id != joinParent.Id)
                failures.Add("Expected mixed-level P3 join to create one live outer 793-850 level-1 parent and retire all components from live selection.");
        }

        private static void AssertPlannerRejectsP3JoinFromWrongPrice(List<string> failures)
        {
            var model = new xPvaV2Model();
            var planner = new xPvaV2Planner();

            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 769,
                EndBar = 781,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(769, 1684.68),
                P2 = new xPvaV2PricePoint(781, 1588.0),
                P3 = new xPvaV2PricePoint(775, 1630.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 781,
                EndBar = 864,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(781, 1600.0),
                P2 = new xPvaV2PricePoint(864, 1783.89),
                P3 = new xPvaV2PricePoint(850, 1625.37)
            });

            IList<xPvaV2Command> commands = planner.PlanP3ContinuationJoins(model);
            if (commands.Count != 0)
                failures.Add("Expected planner to reject P3 continuation join when continuation starts on the source P3 bar but not the source P3 price.");
        }

        private static void AssertPlannerRejectsP3JoinFromWrongMiddleAnchor(List<string> failures)
        {
            var model = new xPvaV2Model();
            var planner = new xPvaV2Planner();

            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 769,
                EndBar = 781,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(769, 1680.0),
                P2 = new xPvaV2PricePoint(781, 1588.0),
                P3 = new xPvaV2PricePoint(775, 1630.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 781,
                EndBar = 864,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(781, 1590.01),
                P2 = new xPvaV2PricePoint(864, 1783.89),
                P3 = new xPvaV2PricePoint(850, 1625.37)
            });

            IList<xPvaV2Command> commands = planner.PlanP3ContinuationJoins(model);
            if (commands.Count != 0)
                failures.Add("Expected planner to reject P3 continuation join when middle component starts on the source P2 bar but not the source P2 price.");
        }

        private static void AssertPlannerSkipsExistingP3Continuation(List<string> failures)
        {
            var model = new xPvaV2Model();
            var planner = new xPvaV2Planner();

            xPvaV2Container source = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 781,
                EndBar = 864,
                P1 = new xPvaV2PricePoint(781, 1590.01),
                P2 = new xPvaV2PricePoint(864, 1783.89),
                P3 = new xPvaV2PricePoint(850, 1625.37)
            });

            model.LinkP3Continuation(source.Id, continuation.Id);
            IList<xPvaV2Command> commands = planner.PlanP3Continuations(model);
            if (commands.Count != 0)
                failures.Add("Expected planner to skip already-linked P3 continuation.");
        }

        private static void AssertDownP3ContinuationJoinIsIdempotent(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container first = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 793,
                EndBar = 808,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(793, 1764.65),
                P2 = new xPvaV2PricePoint(808, 1588.0),
                P3 = new xPvaV2PricePoint(825, 1682.89)
            });
            engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 808,
                EndBar = 825,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(808, 1588.0),
                P2 = new xPvaV2PricePoint(825, 1700.0),
                P3 = new xPvaV2PricePoint(810, 1600.0)
            });
            xPvaV2Container continuation = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 825,
                EndBar = 850,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(825, 1682.89),
                P2 = new xPvaV2PricePoint(850, 1625.37),
                P3 = new xPvaV2PricePoint(836, 1682.0)
            });

            IList<xPvaV2CommandResult> firstPass = engine.ApplyP3ContinuationJoins();
            IList<xPvaV2CommandResult> secondPass = engine.ApplyP3ContinuationJoins();

            int joinParentCount = 0;
            int p3RelationshipCount = 0;
            foreach (xPvaV2Container container in engine.Model.ContainersByStart())
                if (container.OriginKind == xPvaV2OriginKind.Join)
                    joinParentCount++;
            foreach (xPvaV2Relationship relationship in engine.Model.Relationships)
                if (relationship.Kind == xPvaV2RelationshipKind.P3Continuation
                    && relationship.SourceContainerId == first.Id
                    && relationship.TargetContainerId == continuation.Id)
                    p3RelationshipCount++;

            if (firstPass.Count != 2
                || secondPass.Count != 0
                || joinParentCount != 1
                || p3RelationshipCount != 1
                || engine.RenderSnapshot.Count != 8)
                failures.Add("Expected repeated Down P3 continuation join pass to avoid duplicate links, join parents, and render segments.");
        }

        private static void AssertPlannerIgnoresJoinedComponentsForNewP3Joins(List<string> failures)
        {
            var model = new xPvaV2Model();
            var planner = new xPvaV2Planner();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 30,
                EndBar = 60,
                P1 = new xPvaV2PricePoint(30, 120.0),
                P2 = new xPvaV2PricePoint(40, 160.0),
                P3 = new xPvaV2PricePoint(50, 130.0)
            });

            IList<xPvaV2Command> firstJoin = planner.PlanP3ContinuationJoins(model);
            if (firstJoin.Count != 2)
            {
                failures.Add("Expected initial P3 join fixture to produce link and join commands.");
                return;
            }

            reducer.Apply(model, firstJoin[0]);
            reducer.Apply(model, firstJoin[1]);

            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 40,
                EndBar = 50,
                P1 = new xPvaV2PricePoint(40, 160.0),
                P2 = new xPvaV2PricePoint(45, 115.0),
                P3 = new xPvaV2PricePoint(48, 150.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 50,
                EndBar = 70,
                P1 = new xPvaV2PricePoint(50, 130.0),
                P2 = new xPvaV2PricePoint(70, 180.0),
                P3 = new xPvaV2PricePoint(60, 150.0)
            });

            IList<xPvaV2Command> duplicateJoin = planner.PlanP3ContinuationJoins(model);
            if (first.Status != xPvaV2ContainerStatus.Joined
                || middle.Status != xPvaV2ContainerStatus.Joined
                || continuation.Status != xPvaV2ContainerStatus.Joined
                || duplicateJoin.Count != 0)
                failures.Add("Expected P3 join planner to ignore already-joined components when looking for new join parents.");
        }

        private static void AssertReducerRejectsReplayedJoinCommand(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 30,
                EndBar = 60,
                P1 = new xPvaV2PricePoint(30, 120.0),
                P2 = new xPvaV2PricePoint(40, 160.0),
                P3 = new xPvaV2PricePoint(50, 130.0)
            });

            var componentIds = new List<int> { first.Id, middle.Id, continuation.Id };
            xPvaV2Command joinCommand = xPvaV2Command.JoinContainers(
                componentIds,
                xPvaV2Direction.Up,
                10,
                60,
                first.P1,
                first.P2,
                new xPvaV2PricePoint(25, 110.0),
                1,
                1,
                "replay guard");

            xPvaV2CommandResult firstResult = reducer.Apply(model, joinCommand);
            xPvaV2CommandResult replayResult = reducer.Apply(model, joinCommand);

            int joinParentCount = 0;
            foreach (xPvaV2Container container in model.ContainersByStart())
                if (container.OriginKind == xPvaV2OriginKind.Join)
                    joinParentCount++;

            if (!firstResult.Applied
                || replayResult.Applied
                || joinParentCount != 1)
                failures.Add("Expected reducer to reject replayed join commands once components are already joined.");
        }

        private static void AssertReducerRejectsDuplicateJoinParentCommand(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 10,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 30,
                EndBar = 60,
                P1 = new xPvaV2PricePoint(30, 120.0),
                P2 = new xPvaV2PricePoint(40, 160.0),
                P3 = new xPvaV2PricePoint(50, 130.0)
            });
            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 10,
                EndBar = 60,
                P1 = first.P1,
                P2 = first.P2,
                P3 = new xPvaV2PricePoint(25, 110.0),
                VisualLevel = 1,
                StructuralLevel = 1,
                OriginKind = xPvaV2OriginKind.Join
            });

            xPvaV2CommandResult duplicate = reducer.Apply(
                model,
                xPvaV2Command.JoinContainers(
                    new List<int> { first.Id, middle.Id, continuation.Id },
                    xPvaV2Direction.Up,
                    10,
                    60,
                    first.P1,
                    first.P2,
                    new xPvaV2PricePoint(25, 110.0),
                    1,
                    1,
                    "duplicate join parent"));

            int joinParentCount = 0;
            foreach (xPvaV2Container container in model.ContainersByStart())
                if (container.OriginKind == xPvaV2OriginKind.Join)
                    joinParentCount++;

            if (duplicate.Applied
                || duplicate.Reason != "duplicate join parent command"
                || joinParentCount != 1
                || first.Status != xPvaV2ContainerStatus.Active
                || middle.Status != xPvaV2ContainerStatus.Active
                || continuation.Status != xPvaV2ContainerStatus.Active)
                failures.Add("Expected reducer to reject duplicate join-parent geometry without mutating active components.");
        }

        private static void AssertReducerRejectsDuplicateJoinComponents(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });

            var command = new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.JoinContainers,
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                P1 = first.P1,
                P2 = first.P2,
                P3 = new xPvaV2PricePoint(25, 110.0),
                VisualLevel = 1,
                StructuralLevel = 1
            };
            command.ComponentContainerIds.Add(first.Id);
            command.ComponentContainerIds.Add(middle.Id);
            command.ComponentContainerIds.Add(first.Id);

            xPvaV2CommandResult result = reducer.Apply(model, command);

            int joinParentCount = 0;
            foreach (xPvaV2Container container in model.ContainersByStart())
                if (container.OriginKind == xPvaV2OriginKind.Join)
                    joinParentCount++;

            if (result.Applied
                || joinParentCount != 0
                || first.Status == xPvaV2ContainerStatus.Joined
                || middle.Status == xPvaV2ContainerStatus.Joined)
                failures.Add("Expected reducer to reject join commands with duplicate component IDs without mutating components.");
        }

        private static void AssertReducerRejectsInvalidJoinComponentLink(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                OriginKind = xPvaV2OriginKind.Join,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });

            xPvaV2Command selfLink = new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkJoinComponent,
                SourceContainerId = parent.Id,
                TargetContainerId = parent.Id
            };
            xPvaV2Command missingLink = new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkJoinComponent,
                SourceContainerId = parent.Id,
                TargetContainerId = 99999
            };

            xPvaV2CommandResult selfResult = reducer.Apply(model, selfLink);
            xPvaV2CommandResult missingResult = reducer.Apply(model, missingLink);

            if (selfResult.Applied
                || missingResult.Applied
                || parent.JoinComponentIds.Count != 0
                || model.RelationshipsOf(parent.Id, xPvaV2RelationshipKind.JoinComponent).Count != 0)
                failures.Add("Expected reducer to reject invalid direct join-component links without mutating relationships.");
        }

        private static void AssertReducerRejectsCyclicJoinComponentLink(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 50,
                OriginKind = xPvaV2OriginKind.Join,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(30, 150.0),
                P3 = new xPvaV2PricePoint(40, 125.0)
            });
            xPvaV2Container component = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 40,
                OriginKind = xPvaV2OriginKind.Join,
                P1 = new xPvaV2PricePoint(20, 145.0),
                P2 = new xPvaV2PricePoint(30, 105.0),
                P3 = new xPvaV2PricePoint(35, 130.0)
            });
            xPvaV2Container nestedComponent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 25,
                EndBar = 35,
                P1 = new xPvaV2PricePoint(25, 110.0),
                P2 = new xPvaV2PricePoint(32, 135.0),
                P3 = new xPvaV2PricePoint(34, 120.0)
            });

            xPvaV2CommandResult first = reducer.Apply(model, new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkJoinComponent,
                SourceContainerId = parent.Id,
                TargetContainerId = component.Id
            });
            xPvaV2CommandResult second = reducer.Apply(model, new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkJoinComponent,
                SourceContainerId = component.Id,
                TargetContainerId = nestedComponent.Id
            });
            xPvaV2CommandResult cycle = reducer.Apply(model, new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkJoinComponent,
                SourceContainerId = nestedComponent.Id,
                TargetContainerId = parent.Id
            });

            int joinComponentCount = 0;
            foreach (xPvaV2Relationship relationship in model.Relationships)
                if (relationship.Kind == xPvaV2RelationshipKind.JoinComponent)
                    joinComponentCount++;

            if (!first.Applied
                || !second.Applied
                || cycle.Applied
                || parent.JoinComponentIds.Count != 1
                || component.JoinComponentIds.Count != 1
                || nestedComponent.JoinComponentIds.Count != 0
                || joinComponentCount != 2)
                failures.Add("Expected reducer to reject cyclic join-component links without mutating join graph.");
        }

        private static void AssertReducerRejectsInvalidContainmentLink(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });

            xPvaV2Command selfLink = new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkContainment,
                SourceContainerId = parent.Id,
                TargetContainerId = parent.Id
            };
            xPvaV2Command missingLink = new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkContainment,
                SourceContainerId = parent.Id,
                TargetContainerId = 99999
            };

            xPvaV2CommandResult selfResult = reducer.Apply(model, selfLink);
            xPvaV2CommandResult missingResult = reducer.Apply(model, missingLink);

            if (selfResult.Applied
                || missingResult.Applied
                || parent.ContainmentChildIds.Count != 0
                || model.RelationshipsOf(parent.Id, xPvaV2RelationshipKind.Containment).Count != 0)
                failures.Add("Expected reducer to reject invalid direct containment links without mutating relationships.");
        }

        private static void AssertReducerRejectsCyclicContainmentLink(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 50,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(30, 150.0),
                P3 = new xPvaV2PricePoint(40, 125.0)
            });
            xPvaV2Container child = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(20, 145.0),
                P2 = new xPvaV2PricePoint(30, 105.0),
                P3 = new xPvaV2PricePoint(35, 130.0)
            });
            xPvaV2Container grandchild = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 25,
                EndBar = 35,
                P1 = new xPvaV2PricePoint(25, 110.0),
                P2 = new xPvaV2PricePoint(32, 135.0),
                P3 = new xPvaV2PricePoint(34, 120.0)
            });

            xPvaV2CommandResult first = reducer.Apply(model, xPvaV2Command.LinkContainment(parent.Id, child.Id));
            xPvaV2CommandResult second = reducer.Apply(model, xPvaV2Command.LinkContainment(child.Id, grandchild.Id));
            xPvaV2CommandResult cycle = reducer.Apply(model, xPvaV2Command.LinkContainment(grandchild.Id, parent.Id));

            int containmentCount = 0;
            foreach (xPvaV2Relationship relationship in model.Relationships)
                if (relationship.Kind == xPvaV2RelationshipKind.Containment)
                    containmentCount++;

            if (!first.Applied
                || !second.Applied
                || cycle.Applied
                || parent.ContainmentChildIds.Count != 1
                || child.ContainmentChildIds.Count != 1
                || grandchild.ContainmentChildIds.Count != 0
                || containmentCount != 2)
                failures.Add("Expected reducer to reject cyclic containment links without mutating containment graph.");
        }

        private static void AssertReducerRejectsInvalidP3ContinuationLink(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container source = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });
            xPvaV2Container badContinuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 30,
                EndBar = 60,
                P1 = new xPvaV2PricePoint(30, 121.0),
                P2 = new xPvaV2PricePoint(50, 160.0),
                P3 = new xPvaV2PricePoint(45, 130.0)
            });

            xPvaV2CommandResult selfResult = reducer.Apply(model, xPvaV2Command.LinkP3Continuation(source.Id, source.Id));
            xPvaV2CommandResult wrongPriceResult = reducer.Apply(model, xPvaV2Command.LinkP3Continuation(source.Id, badContinuation.Id));

            if (selfResult.Applied
                || wrongPriceResult.Applied
                || source.OriginKind == xPvaV2OriginKind.P3Continuation
                || badContinuation.OriginKind == xPvaV2OriginKind.P3Continuation
                || model.RelationshipsOf(source.Id, xPvaV2RelationshipKind.P3Continuation).Count != 0
                || model.RelationshipsOf(badContinuation.Id, xPvaV2RelationshipKind.P3Continuation).Count != 0)
                failures.Add("Expected reducer to reject invalid P3 continuation links without mutating origin metadata or relationships.");
        }

        private static void AssertReducerRejectsDuplicateDirectRelationshipLinks(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Container upParent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                OriginKind = xPvaV2OriginKind.Join,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });
            xPvaV2Container downChild = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });
            xPvaV2Container upContinuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 30,
                EndBar = 60,
                P1 = new xPvaV2PricePoint(30, 120.0),
                P2 = new xPvaV2PricePoint(50, 160.0),
                P3 = new xPvaV2PricePoint(55, 135.0)
            });

            xPvaV2CommandResult firstContainment = reducer.Apply(model, xPvaV2Command.LinkContainment(upParent.Id, downChild.Id));
            xPvaV2CommandResult duplicateContainment = reducer.Apply(model, xPvaV2Command.LinkContainment(upParent.Id, downChild.Id));
            xPvaV2CommandResult firstJoinComponent = reducer.Apply(model, new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkJoinComponent,
                SourceContainerId = upParent.Id,
                TargetContainerId = downChild.Id
            });
            xPvaV2CommandResult duplicateJoinComponent = reducer.Apply(model, new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkJoinComponent,
                SourceContainerId = upParent.Id,
                TargetContainerId = downChild.Id
            });
            xPvaV2CommandResult firstP3 = reducer.Apply(model, xPvaV2Command.LinkP3Continuation(upParent.Id, upContinuation.Id));
            xPvaV2CommandResult duplicateP3 = reducer.Apply(model, xPvaV2Command.LinkP3Continuation(upParent.Id, upContinuation.Id));

            int containmentCount = 0;
            int joinComponentCount = 0;
            int p3Count = 0;
            foreach (xPvaV2Relationship relationship in model.Relationships)
            {
                if (relationship.Kind == xPvaV2RelationshipKind.Containment)
                    containmentCount++;
                if (relationship.Kind == xPvaV2RelationshipKind.JoinComponent)
                    joinComponentCount++;
                if (relationship.Kind == xPvaV2RelationshipKind.P3Continuation)
                    p3Count++;
            }

            if (!firstContainment.Applied
                || duplicateContainment.Applied
                || !firstJoinComponent.Applied
                || duplicateJoinComponent.Applied
                || !firstP3.Applied
                || duplicateP3.Applied
                || containmentCount != 1
                || joinComponentCount != 1
                || p3Count != 1
                || upParent.ContainmentChildIds.Count != 1
                || upParent.JoinComponentIds.Count != 1)
                failures.Add("Expected reducer to reject duplicate direct relationship links without reporting replayed mutations.");
        }

        private static void AssertReducerReportsSpecificRelationshipRejectReasons(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 40,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(30, 120.0)
            });
            xPvaV2Container child = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });
            xPvaV2Container badContinuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 31,
                EndBar = 50,
                P1 = new xPvaV2PricePoint(31, 121.0),
                P2 = new xPvaV2PricePoint(40, 150.0),
                P3 = new xPvaV2PricePoint(45, 130.0)
            });

            reducer.Apply(model, xPvaV2Command.LinkContainment(parent.Id, child.Id));
            xPvaV2CommandResult duplicateContainment = reducer.Apply(model, xPvaV2Command.LinkContainment(parent.Id, child.Id));
            xPvaV2CommandResult selfJoinComponent = reducer.Apply(model, new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.LinkJoinComponent,
                SourceContainerId = parent.Id,
                TargetContainerId = parent.Id
            });
            xPvaV2CommandResult invalidP3 = reducer.Apply(model, xPvaV2Command.LinkP3Continuation(parent.Id, badContinuation.Id));

            if (duplicateContainment.Applied
                || duplicateContainment.Reason != "duplicate containment link"
                || selfJoinComponent.Applied
                || selfJoinComponent.Reason != "join-component self-link"
                || invalidP3.Applied
                || invalidP3.Reason != "invalid P3-continuation geometry")
                failures.Add("Expected reducer relationship rejects to report specific diagnostic reasons.");
        }

        private static void AssertReducerRejectsDuplicateCreateContainerCommand(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Command command = xPvaV2Command.CreateContainer(
                xPvaV2Direction.Up,
                10,
                20,
                new xPvaV2PricePoint(10, 100.0),
                new xPvaV2PricePoint(15, 140.0),
                new xPvaV2PricePoint(18, 120.0),
                2,
                2,
                xPvaV2OriginKind.TwoBarConstruction,
                "create replay");

            xPvaV2CommandResult first = reducer.Apply(model, command);
            xPvaV2CommandResult duplicate = reducer.Apply(model, command);

            int count = 0;
            foreach (xPvaV2Container container in model.ContainersByStart())
                count++;

            if (!first.Applied
                || duplicate.Applied
                || duplicate.Reason != "duplicate create container command"
                || count != 1)
                failures.Add("Expected reducer to reject duplicate create-container commands without adding a second container.");
        }

        private static void AssertReducerRejectsBackwardContainerSpans(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Command createCommand = xPvaV2Command.CreateContainer(
                xPvaV2Direction.Up,
                40,
                30,
                new xPvaV2PricePoint(40, 100.0),
                new xPvaV2PricePoint(45, 140.0),
                new xPvaV2PricePoint(30, 120.0),
                1,
                1,
                xPvaV2OriginKind.TwoBarConstruction,
                "bad span");
            xPvaV2CommandResult createResult = reducer.Apply(model, createCommand);

            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 20,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(15, 140.0),
                P3 = new xPvaV2PricePoint(18, 120.0)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });

            var componentIds = new List<int> { first.Id, middle.Id };
            xPvaV2Command joinCommand = xPvaV2Command.JoinContainers(
                componentIds,
                xPvaV2Direction.Up,
                40,
                30,
                new xPvaV2PricePoint(40, 100.0),
                new xPvaV2PricePoint(45, 140.0),
                new xPvaV2PricePoint(30, 120.0),
                1,
                1,
                "bad join span");
            xPvaV2CommandResult joinResult = reducer.Apply(model, joinCommand);

            int joinParentCount = 0;
            int totalContainers = 0;
            foreach (xPvaV2Container container in model.ContainersByStart())
            {
                totalContainers++;
                if (container.OriginKind == xPvaV2OriginKind.Join)
                    joinParentCount++;
            }

            if (createResult.Applied
                || joinResult.Applied
                || totalContainers != 2
                || joinParentCount != 0
                || first.Status == xPvaV2ContainerStatus.Joined
                || middle.Status == xPvaV2ContainerStatus.Joined)
                failures.Add("Expected reducer to reject backward container spans without adding containers or joining components.");
        }

        private static void AssertReducerRejectsOutOfSpanAnchorBars(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();

            xPvaV2Command createCommand = xPvaV2Command.CreateContainer(
                xPvaV2Direction.Up,
                10,
                30,
                new xPvaV2PricePoint(9, 100.0),
                new xPvaV2PricePoint(20, 140.0),
                new xPvaV2PricePoint(25, 120.0),
                1,
                1,
                xPvaV2OriginKind.TwoBarConstruction,
                "bad anchor");
            xPvaV2CommandResult createResult = reducer.Apply(model, createCommand);

            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 20,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(15, 140.0),
                P3 = new xPvaV2PricePoint(18, 120.0)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 20,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(20, 140.0),
                P2 = new xPvaV2PricePoint(25, 110.0),
                P3 = new xPvaV2PricePoint(28, 130.0)
            });

            var componentIds = new List<int> { first.Id, middle.Id };
            xPvaV2Command joinCommand = xPvaV2Command.JoinContainers(
                componentIds,
                xPvaV2Direction.Up,
                10,
                30,
                new xPvaV2PricePoint(10, 100.0),
                new xPvaV2PricePoint(35, 140.0),
                new xPvaV2PricePoint(25, 120.0),
                1,
                1,
                "bad join anchor");
            xPvaV2CommandResult joinResult = reducer.Apply(model, joinCommand);

            int joinParentCount = 0;
            int totalContainers = 0;
            foreach (xPvaV2Container container in model.ContainersByStart())
            {
                totalContainers++;
                if (container.OriginKind == xPvaV2OriginKind.Join)
                    joinParentCount++;
            }

            if (createResult.Applied
                || joinResult.Applied
                || totalContainers != 2
                || joinParentCount != 0
                || first.Status == xPvaV2ContainerStatus.Joined
                || middle.Status == xPvaV2ContainerStatus.Joined)
                failures.Add("Expected reducer to reject out-of-span anchor bars without adding containers or joining components.");
        }

        private static void AssertReducerCreatesJoinParentWithComponents(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container first = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            xPvaV2Container middle = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 769,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(769, 1684.68),
                P2 = new xPvaV2PricePoint(781, 1590.01),
                P3 = new xPvaV2PricePoint(776, 1640.0)
            });
            xPvaV2Container continuation = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 781,
                EndBar = 864,
                P1 = new xPvaV2PricePoint(781, 1590.01),
                P2 = new xPvaV2PricePoint(864, 1783.89),
                P3 = new xPvaV2PricePoint(850, 1625.37)
            });

            var components = new List<int> { first.Id, middle.Id, continuation.Id };
            xPvaV2Command command = xPvaV2Command.JoinContainers(
                components,
                xPvaV2Direction.Up,
                740,
                864,
                first.P1,
                first.P2,
                continuation.P3,
                1,
                1,
                "test join");

            xPvaV2CommandResult result = reducer.Apply(model, command);
            xPvaV2Container parent = model.Find(result.ContainerId);
            if (!result.Applied
                || parent == null
                || parent.JoinComponentIds.Count != 3
                || parent.ContainmentChildIds.Count != 0
                || first.ContainmentChildIds.Contains(parent.Id)
                || first.Status != xPvaV2ContainerStatus.Joined
                || middle.Status != xPvaV2ContainerStatus.Joined
                || continuation.Status != xPvaV2ContainerStatus.Joined)
                failures.Add("Expected reducer join parent to record join components without containment mutation.");
        }

        private static void AssertReducerRejectsShorteningFreezeAndPromotion(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container freezeCandidate = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 10,
                EndBar = 30,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(25, 120.0)
            });
            xPvaV2Container promotionCandidate = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 40,
                EndBar = 60,
                VisualLevel = 3,
                StructuralLevel = 3,
                OriginKind = xPvaV2OriginKind.OppositeChild,
                P1 = new xPvaV2PricePoint(40, 150.0),
                P2 = new xPvaV2PricePoint(50, 100.0),
                P3 = new xPvaV2PricePoint(55, 130.0)
            });

            xPvaV2CommandResult freezeResult = reducer.Apply(
                model,
                xPvaV2Command.FreezeContainer(freezeCandidate.Id, 20, "bad freeze"));
            xPvaV2CommandResult promotionResult = reducer.Apply(
                model,
                xPvaV2Command.PromoteContainer(promotionCandidate.Id, 0, 1, 1, 50, "bad promotion"));

            if (freezeResult.Applied
                || promotionResult.Applied
                || freezeCandidate.Status != xPvaV2ContainerStatus.Active
                || freezeCandidate.EndBar != 30
                || promotionCandidate.VisualLevel != 3
                || promotionCandidate.StructuralLevel != 3
                || promotionCandidate.OriginKind != xPvaV2OriginKind.OppositeChild
                || promotionCandidate.EndBar != 60)
                failures.Add("Expected reducer to reject shortening freeze and promotion commands without mutating containers.");
        }

        private static void AssertReducerRejectsPromotionWhenParentLinkFails(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Frozen,
                StartBar = 10,
                EndBar = 50,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(30, 150.0),
                P3 = new xPvaV2PricePoint(40, 125.0)
            });
            xPvaV2Container child = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 20,
                EndBar = 40,
                VisualLevel = 3,
                StructuralLevel = 3,
                OriginKind = xPvaV2OriginKind.OppositeChild,
                P1 = new xPvaV2PricePoint(20, 145.0),
                P2 = new xPvaV2PricePoint(30, 105.0),
                P3 = new xPvaV2PricePoint(35, 130.0)
            });
            model.LinkContainment(parent.Id, child.Id);

            xPvaV2CommandResult result = reducer.Apply(
                model,
                xPvaV2Command.PromoteContainer(child.Id, parent.Id, 1, 1, 45, "duplicate parent link"));

            int containmentCount = 0;
            foreach (xPvaV2Relationship relationship in model.Relationships)
                if (relationship.Kind == xPvaV2RelationshipKind.Containment)
                    containmentCount++;

            if (result.Applied
                || result.Reason != "duplicate containment link"
                || child.VisualLevel != 3
                || child.StructuralLevel != 3
                || child.OriginKind != xPvaV2OriginKind.OppositeChild
                || child.EndBar != 40
                || containmentCount != 1
                || parent.ContainmentChildIds.Count != 1)
                failures.Add("Expected promotion to reject when parent containment link fails without mutating promoted container.");
        }

        private static void AssertReducerRejectsDuplicateStatusCommands(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container deactivationCandidate = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 10,
                EndBar = 30,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(20, 140.0),
                P3 = new xPvaV2PricePoint(25, 120.0)
            });
            xPvaV2Container freezeCandidate = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 40,
                EndBar = 60,
                P1 = new xPvaV2PricePoint(40, 150.0),
                P2 = new xPvaV2PricePoint(50, 100.0),
                P3 = new xPvaV2PricePoint(55, 130.0)
            });

            xPvaV2Command deactivate = new xPvaV2Command
            {
                Kind = xPvaV2CommandKind.DeactivateContainer,
                ContainerId = deactivationCandidate.Id
            };
            xPvaV2CommandResult firstDeactivate = reducer.Apply(model, deactivate);
            xPvaV2CommandResult duplicateDeactivate = reducer.Apply(model, deactivate);
            xPvaV2CommandResult firstFreeze = reducer.Apply(
                model,
                xPvaV2Command.FreezeContainer(freezeCandidate.Id, 65, "freeze"));
            xPvaV2CommandResult duplicateFreeze = reducer.Apply(
                model,
                xPvaV2Command.FreezeContainer(freezeCandidate.Id, 65, "freeze replay"));

            if (!firstDeactivate.Applied
                || duplicateDeactivate.Applied
                || deactivationCandidate.Status != xPvaV2ContainerStatus.StructurallyDeactivated
                || !firstFreeze.Applied
                || duplicateFreeze.Applied
                || freezeCandidate.Status != xPvaV2ContainerStatus.Frozen
                || freezeCandidate.EndBar != 65)
                failures.Add("Expected reducer to reject duplicate status commands while preserving first status mutation.");
        }

        private static void AssertReducerRejectsDuplicatePromotionCommand(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container candidate = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 40,
                EndBar = 60,
                VisualLevel = 3,
                StructuralLevel = 3,
                OriginKind = xPvaV2OriginKind.OppositeChild,
                P1 = new xPvaV2PricePoint(40, 150.0),
                P2 = new xPvaV2PricePoint(50, 100.0),
                P3 = new xPvaV2PricePoint(55, 130.0)
            });
            xPvaV2Command command = xPvaV2Command.PromoteContainer(candidate.Id, 0, 1, 1, 65, "promotion");

            xPvaV2CommandResult first = reducer.Apply(model, command);
            xPvaV2CommandResult duplicate = reducer.Apply(model, command);

            if (!first.Applied
                || duplicate.Applied
                || duplicate.Reason != "container already promoted"
                || candidate.VisualLevel != 1
                || candidate.StructuralLevel != 1
                || candidate.OriginKind != xPvaV2OriginKind.Promotion
                || candidate.EndBar != 65)
                failures.Add("Expected reducer to reject duplicate parentless promotion commands without mutating promoted container.");
        }

        private static void AssertReducerRejectsDuplicatePromotionBeforeParentLink(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Frozen,
                StartBar = 10,
                EndBar = 70,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(40, 150.0),
                P3 = new xPvaV2PricePoint(60, 125.0)
            });
            xPvaV2Container candidate = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 40,
                EndBar = 65,
                VisualLevel = 1,
                StructuralLevel = 1,
                OriginKind = xPvaV2OriginKind.Promotion,
                P1 = new xPvaV2PricePoint(40, 150.0),
                P2 = new xPvaV2PricePoint(50, 100.0),
                P3 = new xPvaV2PricePoint(55, 130.0)
            });

            xPvaV2CommandResult duplicate = reducer.Apply(
                model,
                xPvaV2Command.PromoteContainer(candidate.Id, parent.Id, 1, 1, 65, "duplicate promotion with parent"));

            if (duplicate.Applied
                || duplicate.Reason != "container already promoted"
                || candidate.OriginContainerId != 0
                || parent.ContainmentChildIds.Count != 0
                || model.RelationshipsOf(parent.Id, xPvaV2RelationshipKind.Containment).Count != 0)
                failures.Add("Expected duplicate promotion to reject before adding a parent containment link.");
        }

        private static void AssertTwoBarUpConstructionRequiresHigherP3(List<string> failures)
        {
            xPvaV2Bar first = new xPvaV2Bar(805, 1700.0, 1650.0, xPvaV2BarRelation.LLLH);
            xPvaV2Bar second = new xPvaV2Bar(806, 1720.0, 1660.0, xPvaV2BarRelation.HHHL);
            xPvaV2Command command = xPvaV2Rules.PlanTwoBarConstruction(first, second, 1, 1);

            if (command.Kind != xPvaV2CommandKind.CreateContainer
                || command.Direction != xPvaV2Direction.Up
                || command.P1.Price != 1650.0
                || command.P3.Price != 1660.0)
                failures.Add("Expected valid Up two-bar construction when second low is above first low.");

            xPvaV2Bar badSecond = new xPvaV2Bar(806, 1720.0, 1640.0, xPvaV2BarRelation.HHHL);
            xPvaV2Command rejected = xPvaV2Rules.PlanTwoBarConstruction(first, badSecond, 1, 1);
            if (rejected.Kind != xPvaV2CommandKind.None || rejected.Reason != xPvaV2ConstructionRejectReason.UpP3NotAboveP1.ToString())
                failures.Add("Expected Up construction to reject when second low is not above first low.");
        }

        private static void AssertTwoBarDownConstructionRequiresLowerP3(List<string> failures)
        {
            xPvaV2Bar first = new xPvaV2Bar(810, 1700.0, 1650.0, xPvaV2BarRelation.HHHL);
            xPvaV2Bar second = new xPvaV2Bar(811, 1690.0, 1600.0, xPvaV2BarRelation.LLLH);
            xPvaV2Command command = xPvaV2Rules.PlanTwoBarConstruction(first, second, 1, 1);

            if (command.Kind != xPvaV2CommandKind.CreateContainer
                || command.Direction != xPvaV2Direction.Down
                || command.P1.Price != 1700.0
                || command.P3.Price != 1690.0)
                failures.Add("Expected valid Down two-bar construction when second high is below first high.");

            xPvaV2Bar badSecond = new xPvaV2Bar(811, 1710.0, 1600.0, xPvaV2BarRelation.LLLH);
            xPvaV2Command rejected = xPvaV2Rules.PlanTwoBarConstruction(first, badSecond, 1, 1);
            if (rejected.Kind != xPvaV2CommandKind.None || rejected.Reason != xPvaV2ConstructionRejectReason.DownP3NotBelowP1.ToString())
                failures.Add("Expected Down construction to reject when second high is not below first high.");
        }

        private static void AssertTwoBarConstructionRejectsSecondBarOutside(List<string> failures)
        {
            xPvaV2Bar first = new xPvaV2Bar(802, 1700.0, 1650.0, xPvaV2BarRelation.HHHL);
            xPvaV2Bar second = new xPvaV2Bar(803, 1720.0, 1640.0, xPvaV2BarRelation.OutsideBullish);
            xPvaV2ConstructionRejectReason rejectReason;
            bool accepted = xPvaV2Rules.CanConstructTwoBarContainer(first, second, xPvaV2Direction.Up, out rejectReason);

            if (accepted || rejectReason != xPvaV2ConstructionRejectReason.SecondBarOutside)
                failures.Add("Expected two-bar construction to reject when the second bar is outside.");
        }

        private static void AssertOutsideBarCanBeFirstBarInTwoBarConstruction(List<string> failures)
        {
            xPvaV2Bar outsideFirst = new xPvaV2Bar(803, 1720.0, 1640.0, xPvaV2BarRelation.OutsideBullish);
            xPvaV2Bar downSecond = new xPvaV2Bar(804, 1710.0, 1635.0, xPvaV2BarRelation.LLLH);
            xPvaV2Command down = xPvaV2Rules.PlanTwoBarConstruction(outsideFirst, downSecond, 1, 1);

            xPvaV2Bar upSecond = new xPvaV2Bar(804, 1730.0, 1650.0, xPvaV2BarRelation.HHHL);
            xPvaV2Command up = xPvaV2Rules.PlanTwoBarConstruction(outsideFirst, upSecond, 1, 1);

            if (down.Kind != xPvaV2CommandKind.CreateContainer
                || down.Direction != xPvaV2Direction.Down
                || down.P1.Bar != 803
                || down.P1.Price != 1720.0
                || down.P3.Bar != 804
                || down.P3.Price != 1710.0
                || up.Kind != xPvaV2CommandKind.CreateContainer
                || up.Direction != xPvaV2Direction.Up
                || up.P1.Bar != 803
                || up.P1.Price != 1640.0
                || up.P3.Bar != 804
                || up.P3.Price != 1650.0)
                failures.Add("Expected outside first bar to be valid for two-bar construction when the second bar supplies direction and valid P1/P3 geometry.");
        }

        private static void AssertOutsideBarCanExtendWithP3Adjustment(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container container = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 805,
                EndBar = 806,
                P1 = new xPvaV2PricePoint(805, 1650.0),
                P2 = new xPvaV2PricePoint(806, 1720.0),
                P3 = new xPvaV2PricePoint(806, 1660.0)
            });
            xPvaV2Bar outside = new xPvaV2Bar(807, 1730.0, 1655.0, xPvaV2BarRelation.OutsideBullish);

            xPvaV2Command command = xPvaV2Rules.PlanExtension(container, outside);
            xPvaV2CommandResult result = reducer.Apply(model, command);

            if (command.Kind != xPvaV2CommandKind.ExtendContainer
                || !command.AdjustP3
                || !result.Applied
                || container.EndBar != 807
                || container.P3.Bar != 807
                || container.P3.Price != 1655.0)
                failures.Add("Expected outside bar to extend Up container and adjust P3 while preserving valid geometry.");
        }

        private static void AssertInsideAndSameHighSameLowCanExtendExistingContainer(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container container = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 805,
                EndBar = 806,
                P1 = new xPvaV2PricePoint(805, 1650.0),
                P2 = new xPvaV2PricePoint(806, 1720.0),
                P3 = new xPvaV2PricePoint(806, 1660.0)
            });

            xPvaV2Bar inside = new xPvaV2Bar(807, 1710.0, 1665.0, xPvaV2BarRelation.InsideBar);
            xPvaV2Command insideExtension = xPvaV2Rules.PlanExtension(container, inside);
            xPvaV2CommandResult insideResult = reducer.Apply(model, insideExtension);

            xPvaV2Bar sameHighSameLow = new xPvaV2Bar(808, 1710.0, 1665.0, xPvaV2BarRelation.SameHighSameLow);
            xPvaV2Command sameHighSameLowExtension = xPvaV2Rules.PlanExtension(container, sameHighSameLow);
            xPvaV2CommandResult sameHighSameLowResult = reducer.Apply(model, sameHighSameLowExtension);

            if (insideExtension.Kind != xPvaV2CommandKind.ExtendContainer
                || insideExtension.AdjustP3
                || !insideResult.Applied
                || sameHighSameLowExtension.Kind != xPvaV2CommandKind.ExtendContainer
                || sameHighSameLowExtension.AdjustP3
                || !sameHighSameLowResult.Applied
                || container.EndBar != 808
                || container.P3.Bar != 806
                || container.P3.Price != 1660.0)
                failures.Add("Expected InsideBar and SameHighSameLow bars to extend an existing valid container without creating malformed P3 adjustments.");
        }

        private static void AssertReducerRejectsInvalidExtensionP3Adjustment(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container container = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 20,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(15, 140.0),
                P3 = new xPvaV2PricePoint(18, 120.0)
            });

            xPvaV2CommandResult outOfSpan = reducer.Apply(
                model,
                xPvaV2Command.ExtendContainer(container.Id, 25, new xPvaV2PricePoint(26, 125.0), true, "bad P3 bar"));
            xPvaV2CommandResult badGeometry = reducer.Apply(
                model,
                xPvaV2Command.ExtendContainer(container.Id, 25, new xPvaV2PricePoint(25, 95.0), true, "bad P3 price"));

            if (outOfSpan.Applied
                || badGeometry.Applied
                || container.EndBar != 20
                || container.P3.Bar != 18
                || container.P3.Price != 120.0)
                failures.Add("Expected reducer to reject invalid extension P3 adjustments without mutating the container.");
        }

        private static void AssertReducerRejectsNoOpExtensionCommand(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container container = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 10,
                EndBar = 20,
                P1 = new xPvaV2PricePoint(10, 100.0),
                P2 = new xPvaV2PricePoint(15, 140.0),
                P3 = new xPvaV2PricePoint(18, 120.0)
            });

            xPvaV2CommandResult noOp = reducer.Apply(
                model,
                xPvaV2Command.ExtendContainer(container.Id, 20, container.P3, false, "replay extension"));
            xPvaV2CommandResult p3Adjustment = reducer.Apply(
                model,
                xPvaV2Command.ExtendContainer(container.Id, 20, new xPvaV2PricePoint(20, 125.0), true, "same-endpoint P3 adjustment"));

            if (noOp.Applied
                || noOp.Reason != "container already extended"
                || !p3Adjustment.Applied
                || container.EndBar != 20
                || container.P3.Bar != 20
                || container.P3.Price != 125.0)
                failures.Add("Expected reducer to reject no-op extension while allowing same-endpoint valid P3 adjustment.");
        }

        private static void AssertExtensionRejectsOppositeNonOutsideRelation(List<string> failures)
        {
            xPvaV2Container container = new xPvaV2Container
            {
                Id = 1,
                Direction = xPvaV2Direction.Up,
                StartBar = 805,
                EndBar = 806,
                P1 = new xPvaV2PricePoint(805, 1650.0),
                P2 = new xPvaV2PricePoint(806, 1720.0),
                P3 = new xPvaV2PricePoint(806, 1660.0)
            };
            xPvaV2Bar opposite = new xPvaV2Bar(807, 1710.0, 1640.0, xPvaV2BarRelation.LLLH);

            xPvaV2Command command = xPvaV2Rules.PlanExtension(container, opposite);
            if (command.Kind != xPvaV2CommandKind.None || command.Reason != xPvaV2ExtensionRejectReason.P3GeometryFailed.ToString())
                failures.Add("Expected opposite non-outside relation that violates P3 geometry to reject extension.");
        }

        private static void AssertUpBreakoutFreezesContainer(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container container = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.0)
            });
            xPvaV2Bar bar = new xPvaV2Bar(784, 1700.0, 1580.0, xPvaV2BarRelation.LLLH);

            xPvaV2Command command = xPvaV2Rules.PlanBreakoutFreeze(container, bar);
            xPvaV2CommandResult result = reducer.Apply(model, command);
            if (command.Kind != xPvaV2CommandKind.FreezeContainer || !result.Applied || container.Status != xPvaV2ContainerStatus.Frozen || container.EndBar != 784)
                failures.Add("Expected Up RTL breakout to freeze container and extend frozen endpoint.");
        }

        private static void AssertDownBreakoutFreezesContainer(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container container = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 793,
                EndBar = 850,
                P1 = new xPvaV2PricePoint(793, 1764.65),
                P2 = new xPvaV2PricePoint(808, 1588.0),
                P3 = new xPvaV2PricePoint(825, 1682.89)
            });
            xPvaV2Bar bar = new xPvaV2Bar(851, 1700.0, 1620.0, xPvaV2BarRelation.HHHL);

            xPvaV2Command command = xPvaV2Rules.PlanBreakoutFreeze(container, bar);
            xPvaV2CommandResult result = reducer.Apply(model, command);
            if (command.Kind != xPvaV2CommandKind.FreezeContainer || !result.Applied || container.Status != xPvaV2ContainerStatus.Frozen || container.EndBar != 851)
                failures.Add("Expected Down RTL breakout to freeze container and extend frozen endpoint.");
        }

        private static void AssertBreakoutRejectsNonBreak(List<string> failures)
        {
            xPvaV2Container container = new xPvaV2Container
            {
                Id = 1,
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.0)
            };
            xPvaV2Bar bar = new xPvaV2Bar(784, 1700.0, 1600.0, xPvaV2BarRelation.HHHL);

            xPvaV2Command command = xPvaV2Rules.PlanBreakoutFreeze(container, bar);
            if (command.Kind != xPvaV2CommandKind.None || command.Reason != xPvaV2BreakoutRejectReason.NoBreakout.ToString())
                failures.Add("Expected non-breakout bar not to freeze container.");
        }

        private static void AssertPromotionSelectsBestOppositeChild(List<string> failures)
        {
            var model = new xPvaV2Model();
            var reducer = new xPvaV2Reducer();
            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Frozen,
                StartBar = 740,
                EndBar = 825,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            xPvaV2Container shallow = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 769,
                EndBar = 780,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(769, 1684.68),
                P2 = new xPvaV2PricePoint(780, 1600.0),
                P3 = new xPvaV2PricePoint(776, 1640.0)
            });
            xPvaV2Container best = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 810,
                EndBar = 824,
                VisualLevel = 3,
                StructuralLevel = 3,
                P1 = new xPvaV2PricePoint(810, 1700.0),
                P2 = new xPvaV2PricePoint(824, 1600.0),
                P3 = new xPvaV2PricePoint(818, 1660.0)
            });
            model.LinkContainment(parent.Id, shallow.Id);
            model.LinkContainment(parent.Id, best.Id);

            xPvaV2PromotionRejectReason rejectReason;
            xPvaV2Command command = xPvaV2Rules.PlanPromotionAfterBreak(model, parent, 825, out rejectReason);
            xPvaV2CommandResult result = reducer.Apply(model, command);

            if (rejectReason != xPvaV2PromotionRejectReason.None
                || command.Kind != xPvaV2CommandKind.PromoteContainer
                || command.ContainerId != best.Id
                || !result.Applied
                || best.VisualLevel != 1
                || best.StructuralLevel != 1
                || best.EndBar != 825
                || shallow.Status != xPvaV2ContainerStatus.Active)
                failures.Add("Expected promotion to select deepest/most recent opposite child without hiding sibling containers.");
        }

        private static void AssertPromotionRejectsWhenParentStillLive(List<string> failures)
        {
            var model = new xPvaV2Model();
            xPvaV2Container parent = model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 740,
                EndBar = 825,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });

            xPvaV2PromotionRejectReason rejectReason;
            xPvaV2Command command = xPvaV2Rules.PlanPromotionAfterBreak(model, parent, 825, out rejectReason);
            if (command.Kind != xPvaV2CommandKind.None || rejectReason != xPvaV2PromotionRejectReason.BrokenParentStillLive)
                failures.Add("Expected promotion to reject while parent is still live.");
        }

        private static void AssertRenderSnapshotIncludesJoinedAndFrozenContainers(List<string> failures)
        {
            var model = new xPvaV2Model();
            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Joined,
                StartBar = 740,
                EndBar = 783,
                VisualLevel = 2,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Frozen,
                StartBar = 793,
                EndBar = 850,
                VisualLevel = 1,
                P1 = new xPvaV2PricePoint(793, 1764.65),
                P2 = new xPvaV2PricePoint(808, 1588.0),
                P3 = new xPvaV2PricePoint(825, 1682.89)
            });

            xPvaV2RenderSnapshot snapshot = xPvaV2RenderProjector.BuildSnapshot(model);
            bool hasJoined = false;
            bool hasFrozen = false;
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i].Status == xPvaV2ContainerStatus.Joined)
                    hasJoined = true;
                if (snapshot[i].Status == xPvaV2ContainerStatus.Frozen)
                    hasFrozen = true;
            }

            if (snapshot.Count != 4 || !hasJoined || !hasFrozen)
                failures.Add("Expected render snapshot to include RTL/LTL segments for joined and frozen containers.");
        }

        private static void AssertRenderSnapshotIncludesEveryContainerStatus(List<string> failures)
        {
            var model = new xPvaV2Model();
            xPvaV2ContainerStatus[] statuses =
            {
                xPvaV2ContainerStatus.Active,
                xPvaV2ContainerStatus.Adjusted,
                xPvaV2ContainerStatus.Frozen,
                xPvaV2ContainerStatus.Broken,
                xPvaV2ContainerStatus.Joined,
                xPvaV2ContainerStatus.StructurallyDeactivated
            };

            for (int i = 0; i < statuses.Length; i++)
            {
                int startBar = 600 + (i * 10);
                model.AddContainer(new xPvaV2Container
                {
                    Direction = i % 2 == 0 ? xPvaV2Direction.Up : xPvaV2Direction.Down,
                    Status = statuses[i],
                    StartBar = startBar,
                    EndBar = startBar + 5,
                    VisualLevel = i + 1,
                    P1 = new xPvaV2PricePoint(startBar, i % 2 == 0 ? 100.0 + i : 160.0 + i),
                    P2 = new xPvaV2PricePoint(startBar + 3, i % 2 == 0 ? 140.0 + i : 110.0 + i),
                    P3 = new xPvaV2PricePoint(startBar + 4, i % 2 == 0 ? 120.0 + i : 145.0 + i)
                });
            }

            xPvaV2RenderSnapshot snapshot = xPvaV2RenderProjector.BuildSnapshot(model);
            bool[] found = new bool[statuses.Length];
            for (int i = 0; i < snapshot.Count; i++)
            {
                for (int statusIndex = 0; statusIndex < statuses.Length; statusIndex++)
                    if (snapshot[i].Status == statuses[statusIndex])
                        found[statusIndex] = true;
            }

            bool sawEveryStatus = true;
            for (int i = 0; i < found.Length; i++)
                if (!found[i])
                    sawEveryStatus = false;

            if (snapshot.Count != statuses.Length * 2 || !sawEveryStatus)
                failures.Add("Expected render snapshot to preserve RTL/LTL visibility for active, adjusted, frozen, broken, joined, and structurally deactivated containers.");
        }

        private static void AssertRenderSnapshotIncludesP3JoinParentAndComponents(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container first = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 793,
                EndBar = 808,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(793, 1764.65),
                P2 = new xPvaV2PricePoint(808, 1588.0),
                P3 = new xPvaV2PricePoint(825, 1682.89)
            });
            xPvaV2Container middle = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 808,
                EndBar = 825,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(808, 1588.0),
                P2 = new xPvaV2PricePoint(825, 1700.0),
                P3 = new xPvaV2PricePoint(810, 1600.0)
            });
            xPvaV2Container continuation = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 825,
                EndBar = 850,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(825, 1682.89),
                P2 = new xPvaV2PricePoint(850, 1625.37),
                P3 = new xPvaV2PricePoint(836, 1682.0)
            });

            IList<xPvaV2CommandResult> results = engine.ApplyP3ContinuationJoins();
            xPvaV2Container joinParent = null;
            foreach (xPvaV2Container container in engine.Model.ContainersByStart())
                if (container.OriginKind == xPvaV2OriginKind.Join)
                    joinParent = container;

            xPvaV2RenderSnapshot snapshot = engine.RenderSnapshot;
            int firstSegments = 0;
            int middleSegments = 0;
            int continuationSegments = 0;
            int joinParentSegments = 0;
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (snapshot[i].ContainerId == first.Id && snapshot[i].Status == xPvaV2ContainerStatus.Joined)
                    firstSegments++;
                if (snapshot[i].ContainerId == middle.Id && snapshot[i].Status == xPvaV2ContainerStatus.Joined)
                    middleSegments++;
                if (snapshot[i].ContainerId == continuation.Id && snapshot[i].Status == xPvaV2ContainerStatus.Joined)
                    continuationSegments++;
                if (joinParent != null
                    && snapshot[i].ContainerId == joinParent.Id
                    && snapshot[i].Status == xPvaV2ContainerStatus.Active
                    && snapshot[i].Direction == xPvaV2Direction.Down)
                    joinParentSegments++;
            }

            if (results.Count != 2
                || joinParent == null
                || snapshot.Count != 8
                || firstSegments != 2
                || middleSegments != 2
                || continuationSegments != 2
                || joinParentSegments != 2)
                failures.Add("Expected render snapshot to keep all P3-joined components visible and render the Down join parent.");
        }

        private static void AssertNt8AdapterTranslatesBarRelations(List<string> failures)
        {
            if (xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.HHHL) != xPvaV2BarRelation.HHHL
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.LLLH) != xPvaV2BarRelation.LLLH
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.FTP) != xPvaV2BarRelation.FTP
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.FBP) != xPvaV2BarRelation.FBP
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.StitchLong) != xPvaV2BarRelation.StitchLong
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.StitchShort) != xPvaV2BarRelation.StitchShort
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.InsideBar) != xPvaV2BarRelation.InsideBar
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.OutsideBullish) != xPvaV2BarRelation.OutsideBullish
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.OutsideBearish) != xPvaV2BarRelation.OutsideBearish
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.SameHighSameLow) != xPvaV2BarRelation.SameHighSameLow
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.HighReversal) != xPvaV2BarRelation.HighReversal
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.LowReversal) != xPvaV2BarRelation.LowReversal
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.OutsideBar) != xPvaV2BarRelation.Unknown
                || xPvaV2Nt8Adapter.ToV2Relation(xPvaBarRelation.Unknown) != xPvaV2BarRelation.Unknown)
                failures.Add("Expected NT8 adapter to translate known bar relations exactly and leave generic/unknown relations as Unknown.");
        }

        private static void AssertNt8AdapterResolvesDebugBoundsWithoutChangingModelRules(List<string> failures)
        {
            int debugStart;
            int debugEnd;
            bool debugOk = xPvaV2Nt8Adapter.ResolveBounds(true, 740, 850, 900, out debugStart, out debugEnd);

            int liveStart;
            int liveEnd;
            bool liveOk = xPvaV2Nt8Adapter.ResolveBounds(false, 740, 850, 900, out liveStart, out liveEnd);

            int invalidStart;
            int invalidEnd;
            bool invalid = xPvaV2Nt8Adapter.ResolveBounds(true, 0, 850, 900, out invalidStart, out invalidEnd);

            if (!debugOk
                || debugStart != 740
                || debugEnd != 850
                || !liveOk
                || liveStart != 600
                || liveEnd != 900
                || invalid)
                failures.Add("Expected NT8 adapter bounds to restrict only input scope: debug uses requested bars, live mode uses trailing window, invalid debug bounds reject.");
        }

        private static void AssertNt8AdapterCreatesDeterministicRenderSegmentTags(List<string> failures)
        {
            xPvaV2RenderSegment segment = new xPvaV2RenderSegment(
                42,
                3,
                xPvaV2Direction.Down,
                xPvaV2RenderLineKind.Ltl,
                793,
                850,
                1764.65,
                1682.89,
                xPvaV2ContainerStatus.Joined);

            string first = xPvaV2Nt8Adapter.RenderSegmentTag(segment);
            string second = xPvaV2Nt8Adapter.RenderSegmentTag(segment);
            if (first != "APVA_V2_42_Ltl_793_850" || second != first)
                failures.Add("Expected NT8 adapter render segment tags to be deterministic from container id, line kind, and bar span.");
        }

        private static void AssertNt8AdapterExportsReplayCommandSummaries(List<string> failures)
        {
            string applied = xPvaV2Nt8Adapter.ReplayCommandSummary(
                824,
                xPvaV2CommandResult.Ok(17, "container extended"));
            string rejected = xPvaV2Nt8Adapter.ReplayCommandSummary(
                825,
                xPvaV2CommandResult.Reject("construction rejected: SecondBarOutside"));
            string missing = xPvaV2Nt8Adapter.ReplayCommandSummary(826, null);

            if (applied != "bar=824 applied=True id=17 reason=container extended"
                || rejected != "bar=825 applied=False id=0 reason=construction rejected: SecondBarOutside"
                || missing != "bar=826 applied=False id=0 reason=null")
                failures.Add("Expected NT8 adapter replay command export to match model-only replay summary format.");
        }

        private static void AssertNt8AdapterExportsReplayTraceSummaries(List<string> failures)
        {
            string exported = xPvaV2Nt8Adapter.ReplayTraceSummary(
                new xPvaV2TraceEntry(xPvaV2TraceKind.EndBar, 831, 12, true, "promotion path"));
            string missing = xPvaV2Nt8Adapter.ReplayTraceSummary(null);

            if (exported != "bar=831 kind=EndBar applied=True id=12 detail=promotion path"
                || missing != "bar=0 kind=None applied=False id=0 detail=null")
                failures.Add("Expected NT8 adapter replay trace export to match model-only replay summary format.");
        }

        private static void AssertNt8AdapterFormatsSelfTestSummary(List<string> failures)
        {
            string summary = xPvaV2Nt8Adapter.SelfTestSummary(2, 123);
            if (summary != "selfTestFailures=2 selfTestChecks=123")
                failures.Add("Expected NT8 adapter self-test summary to include failure and check counts.");
        }

        private static void AssertNt8AdapterExportsCompactFixtureRows(List<string> failures)
        {
            string row = xPvaV2Nt8Adapter.ReplayFixtureRow(
                new xPvaV2Bar(805, 1700.25, 1650.0, xPvaV2BarRelation.LLLH));

            if (row != "805,1700.25,1650,LLLH")
                failures.Add("Expected NT8 adapter to export compact fixture rows as bar,high,low,relation.");
        }

        private static void AssertNt8AdapterFixtureRowsRoundTripThroughReplayParser(List<string> failures)
        {
            string fixture =
                xPvaV2Nt8Adapter.ReplayFixtureRow(new xPvaV2Bar(805, 1700.25, 1650.0, xPvaV2BarRelation.LLLH))
                + "\n"
                + xPvaV2Nt8Adapter.ReplayFixtureRow(new xPvaV2Bar(806, 1720.5, 1660.25, xPvaV2BarRelation.HHHL));

            IList<xPvaV2Bar> bars = xPvaV2FixtureReplay.ParseBars(fixture);
            if (bars.Count != 2
                || bars[0].Index != 805
                || bars[0].High != 1700.25
                || bars[1].Index != 806
                || bars[1].RelationToPrevious != xPvaV2BarRelation.HHHL)
                failures.Add("Expected NT8 exported fixture rows to round-trip through the compact replay parser.");
        }

        private static void AssertNt8AdapterExportsFixtureBlockMarkers(List<string> failures)
        {
            string begin = xPvaV2Nt8Adapter.FixtureBegin(805, 808);
            string end = xPvaV2Nt8Adapter.FixtureEnd(805, 808);

            if (begin != "[APVA V2][FIXTURE-BEGIN] 805-808"
                || end != "[APVA V2][FIXTURE-END] 805-808")
                failures.Add("Expected NT8 adapter fixture block markers to identify copied evidence ranges.");
        }

        private static void AssertNt8AdapterExportsFixturePreviewSummaries(List<string> failures)
        {
            string summary = xPvaV2Nt8Adapter.FixturePreviewSummary(
                "window=805-808 matchesCatalog=False generatedRows=4 catalogRows=4");

            if (summary != "[APVA V2][FIXTURE-PREVIEW] window=805-808 matchesCatalog=False generatedRows=4 catalogRows=4")
                failures.Add("Expected NT8 adapter fixture preview summaries to use a stable debug prefix.");
        }

        private static void AssertFixtureReplayExtractsRowsFromDebugOutput(List<string> failures)
        {
            string debugOutput =
                "[APVA V2][TRACE] bar=805 kind=BeginBar applied=True id=0 detail=begin\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "805,1700.25,1650,LLLH\n"
                + "[APVA V2][COMMAND] bar=805 applied=False id=0 reason=no structural action\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720.5,1660.25,HHHL";

            IList<xPvaV2Bar> bars = xPvaV2FixtureReplay.ParseDebugFixtureRows(debugOutput);
            if (bars.Count != 2
                || bars[0].Index != 805
                || bars[0].RelationToPrevious != xPvaV2BarRelation.LLLH
                || bars[1].Index != 806
                || bars[1].High != 1720.5)
                failures.Add("Expected fixture replay to extract compact fixture rows from noisy NT8 debug output.");
        }

        private static void AssertFixtureReplayIgnoresFixtureBlockMarkers(List<string> failures)
        {
            string debugOutput =
                xPvaV2Nt8Adapter.FixtureBegin(805, 806) + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "805,1700.25,1650,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720.5,1660.25,HHHL\n"
                + xPvaV2Nt8Adapter.FixtureEnd(805, 806);

            IList<xPvaV2Bar> bars = xPvaV2FixtureReplay.ParseDebugFixtureRows(debugOutput);
            if (bars.Count != 2
                || bars[0].Index != 805
                || bars[1].Index != 806)
                failures.Add("Expected fixture replay parser to ignore fixture block markers while extracting rows.");
        }

        private static void AssertFixtureReplayIgnoresFixturePreviewLines(List<string> failures)
        {
            string debugOutput =
                xPvaV2Nt8Adapter.FixtureBegin(805, 806) + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "805,1700.25,1650,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePreviewSummary("window=805-808 matchesCatalog=False generatedRows=4 catalogRows=4") + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720.5,1660.25,HHHL\n"
                + xPvaV2Nt8Adapter.FixtureEnd(805, 806);

            IList<xPvaV2Bar> bars = xPvaV2FixtureReplay.ParseDebugFixtureRows(debugOutput);
            if (bars.Count != 2
                || bars[0].Index != 805
                || bars[1].Index != 806)
                failures.Add("Expected fixture replay parser to ignore fixture preview lines while extracting rows.");
        }

        private static void AssertFixtureReplayIgnoresDebugOutputWithoutFixtureRows(List<string> failures)
        {
            IList<xPvaV2Bar> bars = xPvaV2FixtureReplay.ParseDebugFixtureRows(
                "[APVA V2][TRACE] bar=805 kind=BeginBar applied=True id=0 detail=begin\n"
                + "[APVA V2][COMMAND] bar=805 applied=False id=0 reason=no structural action");

            if (bars.Count != 0)
                failures.Add("Expected fixture replay debug-output parser to ignore logs without fixture rows.");
        }

        private static void AssertFixtureReplayBuildsWindowFixtureFromDebugOutput(List<string> failures)
        {
            string debugOutput =
                xPvaV2Nt8Adapter.FixturePrefix + "804,1690,1640,InsideBar\n"
                + "[APVA V2][TRACE] bar=805 kind=BeginBar applied=True id=0 detail=begin\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "805,1700.25,1650,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720.5,1660.25,HHHL\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "807,1698,1648,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "808,1715,1640,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "809,1725,1655,HHHL";

            string fixture = xPvaV2FixtureReplay.BuildWindowFixtureFromDebugOutput(
                debugOutput,
                805,
                808,
                "Historical 805-808 exact rows");

            string expected =
                "# Historical 805-808 exact rows\n"
                + "805,1700.25,1650,LLLH\n"
                + "806,1720.5,1660.25,HHHL\n"
                + "807,1698,1648,LLLH\n"
                + "808,1715,1640,LLLH";

            if (fixture != expected)
                failures.Add("Expected fixture replay to build a filtered named window fixture from raw NT8 debug output.");
        }

        private static void AssertFixtureReplayBuiltWindowRoundTripsThroughParser(List<string> failures)
        {
            string debugOutput =
                xPvaV2Nt8Adapter.FixturePrefix + "805,1700.25,1650,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720.5,1660.25,HHHL";

            string fixture = xPvaV2FixtureReplay.BuildWindowFixtureFromDebugOutput(debugOutput, 805, 806, string.Empty);
            IList<xPvaV2Bar> bars = xPvaV2FixtureReplay.ParseBars(fixture);

            if (bars.Count != 2
                || bars[0].Index != 805
                || bars[1].Index != 806
                || bars[1].Low != 1660.25)
                failures.Add("Expected fixture replay built from debug output to round-trip through the compact parser.");
        }

        private static void AssertFixtureReplayBuildsMarkedWindowFixtureFromDebugOutput(List<string> failures)
        {
            string debugOutput =
                xPvaV2Nt8Adapter.FixtureBegin(740, 742) + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "740,1600,1500,HHHL\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "741,1610,1510,HHHL\n"
                + xPvaV2Nt8Adapter.FixtureEnd(740, 742) + "\n"
                + xPvaV2Nt8Adapter.FixtureBegin(805, 808) + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "805,1700.25,1650,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720.5,1660.25,HHHL\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "807,1698,1648,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "808,1715,1640,LLLH\n"
                + xPvaV2Nt8Adapter.FixtureEnd(805, 808);

            string fixture = xPvaV2FixtureReplay.BuildMarkedWindowFixtureFromDebugOutput(
                debugOutput,
                805,
                808,
                "Marked 805-808");

            string expected =
                "# Marked 805-808\n"
                + "805,1700.25,1650,LLLH\n"
                + "806,1720.5,1660.25,HHHL\n"
                + "807,1698,1648,LLLH\n"
                + "808,1715,1640,LLLH";

            if (fixture != expected)
                failures.Add("Expected fixture replay to build a fixture from the requested marked debug-output block only.");
        }

        private static void AssertFixtureReplayMarkedWindowMissingReturnsHeaderOnly(List<string> failures)
        {
            string fixture = xPvaV2FixtureReplay.BuildMarkedWindowFixtureFromDebugOutput(
                xPvaV2Nt8Adapter.FixtureBegin(740, 742) + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "740,1600,1500,HHHL\n"
                + xPvaV2Nt8Adapter.FixtureEnd(740, 742),
                805,
                808,
                "Missing 805-808");

            if (fixture != "# Missing 805-808")
                failures.Add("Expected missing marked fixture block extraction to return the requested header without rows.");
        }

        private static void AssertFixtureReplayRejectsUnterminatedMarkedWindow(List<string> failures)
        {
            bool rejected = false;
            try
            {
                xPvaV2FixtureReplay.BuildMarkedWindowFixtureFromDebugOutput(
                    xPvaV2Nt8Adapter.FixtureBegin(805, 808) + "\n"
                    + xPvaV2Nt8Adapter.FixturePrefix + "805,1700.25,1650,LLLH\n",
                    805,
                    808,
                    "Unterminated 805-808");
            }
            catch (FormatException ex)
            {
                rejected = ex.Message.IndexOf("fixture block missing end marker", StringComparison.Ordinal) >= 0;
            }

            if (!rejected)
                failures.Add("Expected marked fixture extraction to reject copied blocks missing the end marker.");
        }

        private static void AssertFixtureReplayBuildsMarkedWindowFixtureByCatalogKey(List<string> failures)
        {
            string debugOutput =
                xPvaV2Nt8Adapter.FixtureBegin(805, 808) + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "805,1700.25,1650,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720.5,1660.25,HHHL\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "807,1698,1648,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "808,1715,1640,LLLH\n"
                + xPvaV2Nt8Adapter.FixtureEnd(805, 808);

            string fixture = xPvaV2FixtureReplay.BuildMarkedWindowFixtureFromDebugOutput(debugOutput, "805-808");
            if (fixture.IndexOf("# Historical malformed 805-808 construction/extension window", StringComparison.Ordinal) != 0
                || fixture.IndexOf("805,1700.25,1650,LLLH", StringComparison.Ordinal) < 0
                || fixture.IndexOf("808,1715,1640,LLLH", StringComparison.Ordinal) < 0)
                failures.Add("Expected marked fixture extraction by catalog key to use catalog start/end and header.");
        }

        private static void AssertFixtureReplayRejectsUnknownCatalogKey(List<string> failures)
        {
            bool rejected = false;
            try
            {
                xPvaV2FixtureReplay.BuildMarkedWindowFixtureFromDebugOutput(string.Empty, "unknown");
            }
            catch (ArgumentException ex)
            {
                rejected = ex.Message.IndexOf("unknown evidence fixture window", StringComparison.Ordinal) >= 0;
            }

            if (!rejected)
                failures.Add("Expected marked fixture extraction by catalog key to reject unknown window keys.");
        }

        private static void AssertFixtureReplayPreviewsMatchingCatalogReplacement(List<string> failures)
        {
            string debugOutput =
                xPvaV2Nt8Adapter.FixtureBegin(805, 808) + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "805,1700,1650,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720,1660,HHHL\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "807,1698,1648,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "808,1715,1640,LLLH\n"
                + xPvaV2Nt8Adapter.FixtureEnd(805, 808);

            xPvaV2FixtureReplacementPreview preview =
                xPvaV2FixtureReplay.PreviewCatalogReplacement(debugOutput, "805-808");

            if (preview.WindowKey != "805-808"
                || !preview.MatchesCatalog
                || preview.GeneratedRowCount != 4
                || preview.CatalogRowCount != 4
                || preview.GeneratedFixture != xPvaV2EvidenceFixtures.Window805To808)
                failures.Add("Expected fixture replacement preview to identify unchanged catalog fixture output.");
        }

        private static void AssertFixtureReplayPreviewsChangedCatalogReplacement(List<string> failures)
        {
            string debugOutput =
                xPvaV2Nt8Adapter.FixtureBegin(805, 808) + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "805,1700.25,1650,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720.5,1660.25,HHHL\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "807,1698,1648,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "808,1715,1640,LLLH\n"
                + xPvaV2Nt8Adapter.FixtureEnd(805, 808);

            xPvaV2FixtureReplacementPreview preview =
                xPvaV2FixtureReplay.PreviewCatalogReplacement(debugOutput, "805-808");

            if (preview.WindowKey != "805-808"
                || preview.MatchesCatalog
                || preview.GeneratedRowCount != 4
                || preview.CatalogRowCount != 4
                || preview.GeneratedFixture.IndexOf("1700.25", StringComparison.Ordinal) < 0)
                failures.Add("Expected fixture replacement preview to report changed generated fixture output.");
        }

        private static void AssertFixtureReplayPreviewSummaryReportsRowCounts(List<string> failures)
        {
            string debugOutput =
                xPvaV2Nt8Adapter.FixtureBegin(805, 808) + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "805,1700.25,1650,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720.5,1660.25,HHHL\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "807,1698,1648,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "808,1715,1640,LLLH\n"
                + xPvaV2Nt8Adapter.FixtureEnd(805, 808);

            xPvaV2FixtureReplacementPreview preview =
                xPvaV2FixtureReplay.PreviewCatalogReplacement(debugOutput, "805-808");

            if (preview.Summary() != "window=805-808 matchesCatalog=False generatedRows=4 catalogRows=4")
                failures.Add("Expected fixture replacement preview summary to report match status and row counts.");
        }

        private static void AssertFixtureReplayPreviewsAllCatalogReplacements(List<string> failures)
        {
            string debugOutput =
                MarkedBlock(805, 808, "805,1700,1650,LLLH\n806,1720,1660,HHHL\n807,1698,1648,LLLH\n808,1715,1640,LLLH")
                + "\n"
                + MarkedBlock(823, 831, "823,1700,1640,LLLH\n824,1695,1630,LLLH\n825,1682.89,1620,HHHL\n826,1682,1600,LLLH\n827,1715,1650,HHHL\n828,1710,1660,LLLH\n829,1720,1670,HHHL\n830,1690,1620,HHHL\n831,1700,1665,HHHL")
                + "\n"
                + MarkedBlock(793, 850, "793,1764.65,1710,LLLH\n808,1685,1588,LLLH\n809,1695,1605,HHHL\n810,1680,1595,LLLH\n825,1682.89,1620,HHHL\n850,1710,1580,LLLH");

            IList<xPvaV2FixtureReplacementPreview> previews = xPvaV2FixtureReplay.PreviewCatalogReplacements(debugOutput);
            if (previews.Count != 3
                || !previews[0].MatchesCatalog
                || previews[0].GeneratedRowCount != 4
                || !previews[1].MatchesCatalog
                || previews[1].GeneratedRowCount != 9
                || !previews[2].MatchesCatalog
                || previews[2].GeneratedRowCount != 6)
                failures.Add("Expected fixture replay to preview all catalog replacements from marked debug output.");
        }

        private static void AssertFixtureReplayPreviewsPartialCatalogReplacements(List<string> failures)
        {
            string debugOutput = MarkedBlock(805, 808, "805,1700,1650,LLLH\n806,1720,1660,HHHL\n807,1698,1648,LLLH\n808,1715,1640,LLLH");
            IList<xPvaV2FixtureReplacementPreview> previews = xPvaV2FixtureReplay.PreviewCatalogReplacements(debugOutput);

            if (previews.Count != 3
                || !previews[0].MatchesCatalog
                || previews[0].GeneratedRowCount != 4
                || previews[1].MatchesCatalog
                || previews[1].GeneratedRowCount != 0
                || previews[2].MatchesCatalog
                || previews[2].GeneratedRowCount != 0)
                failures.Add("Expected fixture replay replacement preview to show missing marked blocks as zero-row generated fixtures.");
        }

        private static void AssertFixtureReplayPreviewSummariesFollowCatalogOrder(List<string> failures)
        {
            string debugOutput = MarkedBlock(805, 808, "805,1700,1650,LLLH\n806,1720,1660,HHHL\n807,1698,1648,LLLH\n808,1715,1640,LLLH");
            IList<string> summaries = xPvaV2FixtureReplay.PreviewCatalogReplacementSummaries(debugOutput);

            if (summaries.Count != 3
                || summaries[0] != "window=805-808 matchesCatalog=True generatedRows=4 catalogRows=4"
                || summaries[1] != "window=824-831 matchesCatalog=False generatedRows=0 catalogRows=9"
                || summaries[2] != "window=793-850 matchesCatalog=False generatedRows=0 catalogRows=6")
                failures.Add("Expected fixture replacement preview summaries to follow catalog order with stable row-count text.");
        }

        private static void AssertFixtureReplayPreviewFallsBackToFullRangeFixtureBlock(List<string> failures)
        {
            string debugOutput =
                xPvaV2Nt8Adapter.FixtureBegin(740, 850) + "\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "805,1700,1650,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "806,1720,1660,HHHL\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "807,1698,1648,LLLH\n"
                + xPvaV2Nt8Adapter.FixturePrefix + "808,1715,1640,LLLH\n"
                + xPvaV2Nt8Adapter.FixtureEnd(740, 850);

            IList<string> summaries = xPvaV2FixtureReplay.PreviewCatalogReplacementSummaries(debugOutput);
            if (summaries.Count != 3
                || summaries[0] != "window=805-808 matchesCatalog=True generatedRows=4 catalogRows=4"
                || summaries[1] != "window=824-831 matchesCatalog=False generatedRows=0 catalogRows=9"
                || summaries[2] != "window=793-850 matchesCatalog=False generatedRows=0 catalogRows=6")
                failures.Add("Expected fixture replacement preview to fall back to full-range fixture rows when catalog-specific markers are absent.");
        }

        private static string MarkedBlock(int startBar, int endBar, string rows)
        {
            var text = new StringBuilder();
            text.AppendLine(xPvaV2Nt8Adapter.FixtureBegin(startBar, endBar));
            string[] lines = rows.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
                if (lines[i].Trim().Length > 0)
                    text.AppendLine(xPvaV2Nt8Adapter.FixturePrefix + lines[i]);
            text.Append(xPvaV2Nt8Adapter.FixtureEnd(startBar, endBar));
            return text.ToString();
        }

        private static void AssertFixtureReplayParsesCompactBarRows(List<string> failures)
        {
            IList<xPvaV2Bar> bars = xPvaV2FixtureReplay.ParseBars(
                "# bar,high,low,relation\n"
                + "805,1700.25,1650.00,LLLH\n"
                + "806 1720.50 1660.25 HHHL # whitespace also accepted\n"
                + "807,1730.00,1665.00,OutsideBullish");

            if (bars.Count != 3
                || bars[0].Index != 805
                || bars[0].High != 1700.25
                || bars[1].RelationToPrevious != xPvaV2BarRelation.HHHL
                || bars[2].RelationToPrevious != xPvaV2BarRelation.OutsideBullish)
                failures.Add("Expected compact fixture replay parser to read commented comma/whitespace bar rows into V2 bars.");
        }

        private static void AssertFixtureReplayRejectsMalformedRows(List<string> failures)
        {
            bool rejected = false;
            try
            {
                xPvaV2FixtureReplay.ParseBars("805,1700,1650");
            }
            catch (FormatException ex)
            {
                rejected = ex.Message.IndexOf("bar,high,low,relation", StringComparison.Ordinal) >= 0;
            }

            if (!rejected)
                failures.Add("Expected compact fixture replay parser to reject malformed rows without four fields.");
        }

        private static void AssertFixtureReplayRejectsUnknownRelations(List<string> failures)
        {
            bool rejected = false;
            try
            {
                xPvaV2FixtureReplay.ParseBars("805,1700,1650,NotARelation");
            }
            catch (FormatException ex)
            {
                rejected = ex.Message.IndexOf("unknown fixture relation", StringComparison.Ordinal) >= 0;
            }

            if (!rejected)
                failures.Add("Expected compact fixture replay parser to reject unknown bar relations.");
        }

        private static void AssertFixtureReplayEmitsFilteredCommandAndTraceSummaries(List<string> failures)
        {
            string fixture =
                "805,1700,1650,LLLH\n"
                + "806,1720,1660,HHHL\n"
                + "807,1730,1665,HHHL\n"
                + "808,1710,1640,LLLH";

            xPvaV2ReplayReport report = xPvaV2FixtureReplay.Replay(fixture, 806, 807);
            bool sawConstruction = false;
            bool sawExtension = false;
            bool sawBeginTrace = false;
            bool sawEndTrace = false;

            for (int i = 0; i < report.CommandSummaries.Count; i++)
            {
                if (report.CommandSummaries[i].IndexOf("bar=806", StringComparison.Ordinal) >= 0
                    && report.CommandSummaries[i].IndexOf("container created", StringComparison.Ordinal) >= 0)
                    sawConstruction = true;
                if (report.CommandSummaries[i].IndexOf("bar=807", StringComparison.Ordinal) >= 0
                    && report.CommandSummaries[i].IndexOf("container extended", StringComparison.Ordinal) >= 0)
                    sawExtension = true;
            }

            for (int i = 0; i < report.TraceSummaries.Count; i++)
            {
                if (report.TraceSummaries[i].IndexOf("bar=806 kind=BeginBar", StringComparison.Ordinal) >= 0)
                    sawBeginTrace = true;
                if (report.TraceSummaries[i].IndexOf("bar=807 kind=EndBar", StringComparison.Ordinal) >= 0)
                    sawEndTrace = true;
            }

            if (report.Bars.Count != 4
                || !sawConstruction
                || !sawExtension
                || !sawBeginTrace
                || !sawEndTrace
                || report.Engine.RenderSnapshot.Count == 0)
                failures.Add("Expected compact fixture replay to run model-only and emit filtered command/trace summaries for requested bar range.");
        }

        private static void AssertFixtureReplayUsesNt8ExportSummaryFormat(List<string> failures)
        {
            string fixture =
                "805,1700,1650,LLLH\n"
                + "806,1720,1660,HHHL\n"
                + "807,1730,1665,HHHL";

            xPvaV2ReplayReport report = xPvaV2FixtureReplay.Replay(fixture, 806, 807);
            bool commandShapeOk = false;
            bool traceShapeOk = false;

            for (int i = 0; i < report.CommandSummaries.Count; i++)
                if (report.CommandSummaries[i].IndexOf(" applied=", StringComparison.Ordinal) >= 0
                    && report.CommandSummaries[i].IndexOf(" id=", StringComparison.Ordinal) >= 0
                    && report.CommandSummaries[i].IndexOf(" reason=", StringComparison.Ordinal) >= 0)
                    commandShapeOk = true;

            for (int i = 0; i < report.TraceSummaries.Count; i++)
                if (report.TraceSummaries[i].IndexOf(" kind=", StringComparison.Ordinal) >= 0
                    && report.TraceSummaries[i].IndexOf(" applied=", StringComparison.Ordinal) >= 0
                    && report.TraceSummaries[i].IndexOf(" id=", StringComparison.Ordinal) >= 0
                    && report.TraceSummaries[i].IndexOf(" detail=", StringComparison.Ordinal) >= 0)
                    traceShapeOk = true;

            if (!commandShapeOk || !traceShapeOk)
                failures.Add("Expected fixture replay summaries to use the NT8 adapter replay export format.");
        }

        private static void AssertEvidenceFixtureCatalogParsesNamedWindows(List<string> failures)
        {
            IList<xPvaV2Bar> window805 = xPvaV2FixtureReplay.ParseBars(xPvaV2EvidenceFixtures.Window805To808);
            IList<xPvaV2Bar> window824 = xPvaV2FixtureReplay.ParseBars(xPvaV2EvidenceFixtures.Window824To831);
            IList<xPvaV2Bar> window793 = xPvaV2FixtureReplay.ParseBars(xPvaV2EvidenceFixtures.Window793To850);

            if (window805.Count != 4
                || window805[0].Index != 805
                || window805[3].Index != 808
                || window824.Count != 9
                || window824[0].Index != 823
                || window824[8].Index != 831
                || window793.Count != 6
                || window793[0].Index != 793
                || window793[5].Index != 850)
                failures.Add("Expected evidence fixture catalog to parse the named 805-808, 824-831, and 793-850 windows.");
        }

        private static void AssertEvidenceFixtureCatalogUsesChronologicalRows(List<string> failures)
        {
            string[] fixtures = xPvaV2EvidenceFixtures.All;
            for (int fixtureIndex = 0; fixtureIndex < fixtures.Length; fixtureIndex++)
            {
                IList<xPvaV2Bar> bars = xPvaV2FixtureReplay.ParseBars(fixtures[fixtureIndex]);
                for (int barIndex = 1; barIndex < bars.Count; barIndex++)
                    if (bars[barIndex].Index <= bars[barIndex - 1].Index)
                        failures.Add("Expected evidence fixture catalog rows to be strictly chronological.");
            }
        }

        private static void AssertEvidenceFixtureCatalogReplaysProblemWindows(List<string> failures)
        {
            xPvaV2ReplayReport window805 = xPvaV2FixtureReplay.Replay(xPvaV2EvidenceFixtures.Window805To808, 805, 808);
            xPvaV2ReplayReport window824 = xPvaV2FixtureReplay.Replay(xPvaV2EvidenceFixtures.Window824To831, 824, 831);
            xPvaV2ReplayReport window793 = xPvaV2FixtureReplay.Replay(xPvaV2EvidenceFixtures.Window793To850, 793, 850);

            if (window805.CommandSummaries.Count == 0
                || window805.TraceSummaries.Count == 0
                || window824.CommandSummaries.Count == 0
                || window824.TraceSummaries.Count == 0
                || window793.CommandSummaries.Count == 0
                || window793.TraceSummaries.Count == 0)
                failures.Add("Expected each named evidence fixture window to replay with command and trace summaries.");
        }

        private static void AssertEvidenceFixtureCatalogContainsHistoricalAnchorBars(List<string> failures)
        {
            IList<xPvaV2Bar> window805 = xPvaV2FixtureReplay.ParseBars(xPvaV2EvidenceFixtures.Window805To808);
            IList<xPvaV2Bar> window824 = xPvaV2FixtureReplay.ParseBars(xPvaV2EvidenceFixtures.Window824To831);
            IList<xPvaV2Bar> window793 = xPvaV2FixtureReplay.ParseBars(xPvaV2EvidenceFixtures.Window793To850);

            if (!ContainsBar(window805, 805)
                || !ContainsBar(window805, 808)
                || !ContainsBar(window824, 824)
                || !ContainsBar(window824, 831)
                || !ContainsBar(window793, 793)
                || !ContainsBar(window793, 808)
                || !ContainsBar(window793, 825)
                || !ContainsBar(window793, 850))
                failures.Add("Expected evidence fixture catalog to retain the historical anchor bars for each problem window.");
        }

        private static void AssertEvidenceFixtureReplayFiltersTargetBars(List<string> failures)
        {
            xPvaV2ReplayReport report = xPvaV2FixtureReplay.Replay(xPvaV2EvidenceFixtures.Window824To831, 827, 829);

            if (report.CommandSummaries.Count == 0 || report.TraceSummaries.Count == 0)
            {
                failures.Add("Expected evidence fixture replay to emit summaries for the requested target range.");
                return;
            }

            if (!SummariesStayInsideRange(report.CommandSummaries, 827, 829)
                || !SummariesStayInsideRange(report.TraceSummaries, 827, 829))
                failures.Add("Expected evidence fixture replay summaries to stay inside the requested target bar range.");
        }

        private static void AssertEvidenceFixtureCatalogFindsWindowsByKey(List<string> failures)
        {
            xPvaV2EvidenceFixtures.Window window805;
            xPvaV2EvidenceFixtures.Window windowMissing;
            bool found805 = xPvaV2EvidenceFixtures.TryGetWindow("805-808", out window805);
            bool foundLower = xPvaV2EvidenceFixtures.TryGetWindow("824-831", out xPvaV2EvidenceFixtures.Window window824);
            bool foundMissing = xPvaV2EvidenceFixtures.TryGetWindow("missing", out windowMissing);

            if (!found805
                || !foundLower
                || foundMissing
                || window805.StartBar != 805
                || window805.EndBar != 808
                || window824.StartBar != 823
                || window824.EndBar != 831)
                failures.Add("Expected evidence fixture catalog to find known windows by key and reject unknown keys.");
        }

        private static void AssertEvidenceFixtureCatalogWindowMetadataMatchesFixtures(List<string> failures)
        {
            xPvaV2EvidenceFixtures.Window[] windows = xPvaV2EvidenceFixtures.Windows;
            if (windows.Length != xPvaV2EvidenceFixtures.All.Length)
            {
                failures.Add("Expected evidence fixture catalog metadata count to match fixture count.");
                return;
            }

            for (int i = 0; i < windows.Length; i++)
            {
                IList<xPvaV2Bar> bars = xPvaV2FixtureReplay.ParseBars(windows[i].Fixture);
                if (bars.Count == 0
                    || bars[0].Index != windows[i].StartBar
                    || bars[bars.Count - 1].Index != windows[i].EndBar
                    || windows[i].Fixture != xPvaV2EvidenceFixtures.All[i]
                    || windows[i].Header.Length == 0)
                    failures.Add("Expected evidence fixture catalog metadata to match fixture start/end and stored fixture text.");
            }
        }

        private static bool ContainsBar(IList<xPvaV2Bar> bars, int bar)
        {
            for (int i = 0; i < bars.Count; i++)
                if (bars[i].Index == bar)
                    return true;
            return false;
        }

        private static bool SummariesStayInsideRange(IList<string> summaries, int startBar, int endBar)
        {
            for (int i = 0; i < summaries.Count; i++)
            {
                int bar = ExtractSummaryBar(summaries[i]);
                if (bar < startBar || bar > endBar)
                    return false;
            }

            return true;
        }

        private static int ExtractSummaryBar(string summary)
        {
            if (string.IsNullOrEmpty(summary) || !summary.StartsWith("bar=", StringComparison.Ordinal))
                return -1;

            int end = summary.IndexOf(' ');
            if (end < 0)
                end = summary.Length;

            int bar;
            if (!int.TryParse(summary.Substring(4, end - 4), NumberStyles.Integer, CultureInfo.InvariantCulture, out bar))
                return -1;
            return bar;
        }

        private static void AssertEngineAppliesP3ContinuationJoinFlow(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container first = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 740,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            xPvaV2Container middle = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                StartBar = 769,
                EndBar = 783,
                P1 = new xPvaV2PricePoint(769, 1684.68),
                P2 = new xPvaV2PricePoint(781, 1590.01),
                P3 = new xPvaV2PricePoint(776, 1640.0)
            });
            xPvaV2Container continuation = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                StartBar = 781,
                EndBar = 864,
                P1 = new xPvaV2PricePoint(781, 1590.01),
                P2 = new xPvaV2PricePoint(864, 1783.89),
                P3 = new xPvaV2PricePoint(850, 1625.37)
            });

            IList<xPvaV2CommandResult> results = engine.ApplyP3ContinuationJoins();
            bool linked = false;
            bool joined = false;
            foreach (xPvaV2Relationship relationship in engine.Model.Relationships)
            {
                if (relationship.Kind == xPvaV2RelationshipKind.P3Continuation
                    && relationship.SourceContainerId == first.Id
                    && relationship.TargetContainerId == continuation.Id)
                    linked = true;
                if (relationship.Kind == xPvaV2RelationshipKind.JoinComponent
                    && relationship.TargetContainerId == middle.Id)
                    joined = true;
            }

            if (results.Count != 2
                || !linked
                || !joined
                || continuation.OriginKind != xPvaV2OriginKind.P3Continuation
                || engine.RenderSnapshot.Count == 0)
                failures.Add("Expected engine to apply P3 continuation link, create join parent, and refresh render snapshot.");
        }

        private static void AssertEngineTracksLastLiveContainer(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2CommandResult first = engine.ConstructFromTwoBars(
                new xPvaV2Bar(805, 1700.0, 1650.0, xPvaV2BarRelation.LLLH),
                new xPvaV2Bar(806, 1720.0, 1660.0, xPvaV2BarRelation.HHHL),
                1,
                1);
            xPvaV2Container live = engine.LastLiveContainer();

            if (!first.Applied || live == null || live.Id != first.ContainerId)
            {
                failures.Add("Expected engine to return most recent live container after construction.");
                return;
            }

            xPvaV2CommandResult extended = engine.Extend(live, new xPvaV2Bar(807, 1730.0, 1665.0, xPvaV2BarRelation.HHHL));
            if (!extended.Applied || live.EndBar != 807)
                failures.Add("Expected engine to extend last live container.");
        }

        private static void AssertEngineProcessesBreakoutBeforeExtension(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Bar first = new xPvaV2Bar(805, 1700.0, 1650.0, xPvaV2BarRelation.LLLH);
            xPvaV2Bar second = new xPvaV2Bar(806, 1720.0, 1660.0, xPvaV2BarRelation.HHHL);
            xPvaV2Bar breakout = new xPvaV2Bar(807, 1710.0, 1640.0, xPvaV2BarRelation.LLLH);

            xPvaV2CommandResult created = engine.ConstructFromTwoBars(first, second, 1, 1);
            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(second, breakout, 1, 1);
            xPvaV2Container original = engine.Model.Find(created.ContainerId);

            if (!created.Applied || results.Count == 0 || !results[0].Applied || results[0].ContainerId != created.ContainerId)
                failures.Add("Expected sequential processing to freeze a broken live container before extension or construction.");
            if (original == null || original.Status != xPvaV2ContainerStatus.Frozen)
                failures.Add("Expected broken live container to remain visible as frozen.");
            if (engine.LastLiveContainer() != null && engine.LastLiveContainer().Id == created.ContainerId)
                failures.Add("Expected frozen container not to remain the engine's last live container.");
        }

        private static void AssertEnginePromotesAfterBreakoutFreeze(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container child = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 804,
                EndBar = 806,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(804, 1710.0),
                P2 = new xPvaV2PricePoint(806, 1660.0),
                P3 = new xPvaV2PricePoint(805, 1700.0)
            });
            xPvaV2Container parent = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 805,
                EndBar = 806,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(805, 1650.0),
                P2 = new xPvaV2PricePoint(806, 1720.0),
                P3 = new xPvaV2PricePoint(806, 1660.0)
            });
            engine.Model.LinkContainment(parent.Id, child.Id);

            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(
                new xPvaV2Bar(806, 1720.0, 1660.0, xPvaV2BarRelation.HHHL),
                new xPvaV2Bar(807, 1710.0, 1640.0, xPvaV2BarRelation.LLLH),
                1,
                1);

            if (results.Count != 2
                || !results[0].Applied
                || !results[1].Applied
                || results[0].ContainerId != parent.Id
                || results[1].ContainerId != child.Id)
                failures.Add("Expected sequential processing to freeze broken parent and promote its opposite child on the same bar.");
            if (parent.Status != xPvaV2ContainerStatus.Frozen)
                failures.Add("Expected broken parent to remain visible as frozen after promotion.");
            if (child.Status != xPvaV2ContainerStatus.Active || child.VisualLevel != 1 || child.StructuralLevel != 1 || child.EndBar != 807)
                failures.Add("Expected promoted child to become the visible level 1 live container.");
        }

        private static void AssertEngineLinksConstructedOppositeChildContainment(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container parent = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 800,
                EndBar = 806,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(800, 1500.0),
                P2 = new xPvaV2PricePoint(806, 1720.0),
                P3 = new xPvaV2PricePoint(806, 1650.0)
            });

            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(
                new xPvaV2Bar(806, 1720.0, 1680.0, xPvaV2BarRelation.HHHL),
                new xPvaV2Bar(807, 1710.0, 1676.0, xPvaV2BarRelation.LLLH),
                1,
                1);

            xPvaV2Container child = results.Count == 0 ? null : engine.Model.Find(results[0].ContainerId);
            IList<xPvaV2Container> children = engine.Model.ContainmentChildrenOf(parent.Id);

            if (results.Count != 2
                || child == null
                || child.Direction != xPvaV2Direction.Down
                || child.VisualLevel != 2
                || child.StructuralLevel != 2
                || children.Count != 1
                || children[0].Id != child.Id)
                failures.Add("Expected constructed opposite child to inherit parent+1 levels and link as containment.");
        }

        private static void AssertEngineReturnsExtensionFocusToParent(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container parent = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 800,
                EndBar = 807,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(800, 1500.0),
                P2 = new xPvaV2PricePoint(806, 1720.0),
                P3 = new xPvaV2PricePoint(806, 1650.0)
            });
            xPvaV2Container child = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 806,
                EndBar = 807,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(806, 1720.0),
                P2 = new xPvaV2PricePoint(807, 1676.0),
                P3 = new xPvaV2PricePoint(807, 1710.0)
            });
            engine.Model.LinkContainment(parent.Id, child.Id);

            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(
                new xPvaV2Bar(807, 1710.0, 1676.0, xPvaV2BarRelation.LLLH),
                new xPvaV2Bar(808, 1725.0, 1685.0, xPvaV2BarRelation.HHHL),
                1,
                1);

            if (results.Count != 1
                || !results[0].Applied
                || results[0].ContainerId != parent.Id
                || parent.EndBar != 808
                || child.EndBar != 807)
                failures.Add("Expected extension focus to return to parent when the child cannot accept the bar.");
        }

        private static void AssertEngineBreakoutFocusCanReturnToParent(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container parent = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 800,
                EndBar = 807,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(800, 1500.0),
                P2 = new xPvaV2PricePoint(806, 1720.0),
                P3 = new xPvaV2PricePoint(806, 1650.0)
            });
            xPvaV2Container child = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 806,
                EndBar = 807,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(806, 1720.0),
                P2 = new xPvaV2PricePoint(807, 1676.0),
                P3 = new xPvaV2PricePoint(807, 1710.0)
            });
            engine.Model.LinkContainment(parent.Id, child.Id);

            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(
                new xPvaV2Bar(807, 1710.0, 1676.0, xPvaV2BarRelation.LLLH),
                new xPvaV2Bar(808, 1695.0, 1690.0, xPvaV2BarRelation.LLLH),
                1,
                1);

            if (results.Count != 2
                || !results[0].Applied
                || !results[1].Applied
                || results[0].ContainerId != parent.Id
                || results[1].ContainerId != child.Id
                || parent.Status != xPvaV2ContainerStatus.Frozen
                || child.VisualLevel != 1
                || child.StructuralLevel != 1)
                failures.Add("Expected breakout focus to return to parent when parent breaks and latest child does not.");
        }

        private static void AssertSequentialProcessingAppliesP3ContinuationJoinFlow(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container first = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Frozen,
                StartBar = 740,
                EndBar = 783,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(740, 1500.0),
                P2 = new xPvaV2PricePoint(769, 1684.68),
                P3 = new xPvaV2PricePoint(781, 1590.01)
            });
            xPvaV2Container middle = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Frozen,
                StartBar = 769,
                EndBar = 781,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(769, 1684.68),
                P2 = new xPvaV2PricePoint(781, 1590.01),
                P3 = new xPvaV2PricePoint(776, 1640.0)
            });

            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(
                new xPvaV2Bar(781, 1640.0, 1590.01, xPvaV2BarRelation.LLLH),
                new xPvaV2Bar(782, 1665.0, 1600.0, xPvaV2BarRelation.HHHL),
                1,
                1);

            xPvaV2Container continuation = null;
            foreach (xPvaV2Container container in engine.Model.ContainersByStart())
                if (container.StartBar == 781 && container.Direction == xPvaV2Direction.Up && container.Id != first.Id)
                    continuation = container;

            bool linked = false;
            bool joined = false;
            foreach (xPvaV2Relationship relationship in engine.Model.Relationships)
            {
                if (continuation != null
                    && relationship.Kind == xPvaV2RelationshipKind.P3Continuation
                    && relationship.SourceContainerId == first.Id
                    && relationship.TargetContainerId == continuation.Id)
                    linked = true;
                if (relationship.Kind == xPvaV2RelationshipKind.JoinComponent
                    && relationship.TargetContainerId == middle.Id)
                    joined = true;
            }

            if (results.Count != 3
                || continuation == null
                || continuation.OriginKind != xPvaV2OriginKind.P3Continuation
                || continuation.Status != xPvaV2ContainerStatus.Joined
                || !linked
                || !joined)
                failures.Add("Expected sequential processing to create a P3 continuation and immediately join the first, middle, and continuation containers.");

            IList<xPvaV2CommandResult> duplicatePass = engine.ProcessSequentialBar(
                new xPvaV2Bar(782, 1665.0, 1600.0, xPvaV2BarRelation.HHHL),
                new xPvaV2Bar(783, 1675.0, 1610.0, xPvaV2BarRelation.HHHL),
                1,
                1);

            int joinParentCount = 0;
            foreach (xPvaV2Container container in engine.Model.ContainersByStart())
                if (container.OriginKind == xPvaV2OriginKind.Join)
                    joinParentCount++;

            if (joinParentCount != 1)
                failures.Add("Expected stream-side P3 continuation join to avoid duplicate join parents on later bars.");
            if (duplicatePass.Count != 1 || !duplicatePass[0].Applied)
                failures.Add("Expected later sequential processing to continue normally after the P3 join is complete.");
        }

        private static void AssertSequentialProcessingAppliesDownP3ContinuationJoinFlow(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container first = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Frozen,
                StartBar = 793,
                EndBar = 808,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(793, 1764.65),
                P2 = new xPvaV2PricePoint(808, 1588.0),
                P3 = new xPvaV2PricePoint(825, 1682.89)
            });
            xPvaV2Container middle = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Frozen,
                StartBar = 808,
                EndBar = 825,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(808, 1588.0),
                P2 = new xPvaV2PricePoint(825, 1700.0),
                P3 = new xPvaV2PricePoint(810, 1600.0)
            });

            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(
                new xPvaV2Bar(825, 1682.89, 1620.0, xPvaV2BarRelation.HHHL),
                new xPvaV2Bar(826, 1682.0, 1600.0, xPvaV2BarRelation.LLLH),
                1,
                1);

            xPvaV2Container continuation = null;
            xPvaV2Container joinParent = null;
            foreach (xPvaV2Container container in engine.Model.ContainersByStart())
            {
                if (container.StartBar == 825
                    && container.Direction == xPvaV2Direction.Down
                    && container.Id != first.Id)
                    continuation = container;
                if (container.OriginKind == xPvaV2OriginKind.Join
                    && container.Direction == xPvaV2Direction.Down)
                    joinParent = container;
            }

            bool linked = false;
            bool joined = false;
            foreach (xPvaV2Relationship relationship in engine.Model.Relationships)
            {
                if (continuation != null
                    && relationship.Kind == xPvaV2RelationshipKind.P3Continuation
                    && relationship.SourceContainerId == first.Id
                    && relationship.TargetContainerId == continuation.Id)
                    linked = true;
                if (relationship.Kind == xPvaV2RelationshipKind.JoinComponent
                    && relationship.TargetContainerId == middle.Id)
                    joined = true;
            }

            if (results.Count != 3
                || continuation == null
                || joinParent == null
                || continuation.OriginKind != xPvaV2OriginKind.P3Continuation
                || continuation.OriginPoint != xPvaV2OriginPoint.P3
                || continuation.Status != xPvaV2ContainerStatus.Joined
                || first.Status != xPvaV2ContainerStatus.Joined
                || middle.Status != xPvaV2ContainerStatus.Joined
                || joinParent.P3.Price != middle.P2.Price
                || !linked
                || !joined)
                failures.Add("Expected sequential processing to create a Down P3 continuation and immediately join the first, middle, and continuation containers.");
        }

        private static void AssertSequentialTraceCapturesRejectedConstruction(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(
                new xPvaV2Bar(803, 1720.0, 1640.0, xPvaV2BarRelation.OutsideBullish),
                new xPvaV2Bar(804, 1710.0, 1635.0, xPvaV2BarRelation.InsideBar),
                1,
                1);

            bool sawBegin = false;
            bool sawReject = false;
            bool sawEnd = false;
            IList<xPvaV2TraceEntry> trace = engine.LastTrace;
            for (int i = 0; i < trace.Count; i++)
            {
                if (trace[i].Kind == xPvaV2TraceKind.BeginBar && trace[i].Bar == 804)
                    sawBegin = true;
                if (trace[i].Kind == xPvaV2TraceKind.ConstructionResult
                    && !trace[i].Applied
                    && trace[i].Detail == xPvaV2ConstructionRejectReason.UnknownDirection.ToString())
                    sawReject = true;
                if (trace[i].Kind == xPvaV2TraceKind.EndBar && trace[i].Detail == "no structural action")
                    sawEnd = true;
            }

            if (results.Count != 1
                || results[0].Applied
                || !sawBegin
                || !sawReject
                || !sawEnd)
                failures.Add("Expected sequential trace to capture rejected construction reason and no-action end state.");
        }

        private static void AssertFixture805To808RejectsMalformedDownContainer(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container down = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 805,
                EndBar = 806,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(805, 1710.0),
                P2 = new xPvaV2PricePoint(806, 1660.0),
                P3 = new xPvaV2PricePoint(806, 1700.0)
            });

            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(
                new xPvaV2Bar(807, 1698.0, 1648.0, xPvaV2BarRelation.LLLH),
                new xPvaV2Bar(808, 1715.0, 1640.0, xPvaV2BarRelation.LLLH),
                1,
                1);

            bool sawExtensionGeometryReject = false;
            bool sawConstructionGeometryReject = false;
            IList<xPvaV2TraceEntry> trace = engine.LastTrace;
            for (int i = 0; i < trace.Count; i++)
            {
                if (trace[i].Kind == xPvaV2TraceKind.ExtensionResult
                    && !trace[i].Applied
                    && trace[i].ContainerId == down.Id
                    && trace[i].Detail == xPvaV2ExtensionRejectReason.P3GeometryFailed.ToString())
                    sawExtensionGeometryReject = true;
                if (trace[i].Kind == xPvaV2TraceKind.ConstructionResult
                    && !trace[i].Applied
                    && trace[i].Detail == xPvaV2ConstructionRejectReason.DownP3NotBelowP1.ToString())
                    sawConstructionGeometryReject = true;
            }

            int down805ContainerCount = 0;
            foreach (xPvaV2Container container in engine.Model.ContainersByStart())
                if (container.Direction == xPvaV2Direction.Down && container.StartBar == 805)
                    down805ContainerCount++;

            if (results.Count != 1
                || results[0].Applied
                || down.EndBar != 806
                || down.P3.Bar != 806
                || down.P3.Price != 1700.0
                || down805ContainerCount != 1
                || !sawExtensionGeometryReject
                || !sawConstructionGeometryReject)
                failures.Add("Expected 805-808 fixture to reject malformed Down extension/construction and leave the original container unchanged.");
        }

        private static void AssertFixture827To829DoesNotAdjustDownContainerUpward(List<string> failures)
        {
            xPvaV2Container directDown = new xPvaV2Container
            {
                Id = 1,
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 827,
                EndBar = 828,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(827, 1730.0),
                P2 = new xPvaV2PricePoint(828, 1660.0),
                P3 = new xPvaV2PricePoint(828, 1710.0)
            };
            xPvaV2Bar hhhl = new xPvaV2Bar(829, 1720.0, 1670.0, xPvaV2BarRelation.HHHL);
            xPvaV2Command directExtension = xPvaV2Rules.PlanExtension(directDown, hhhl);
            if (directExtension.Kind != xPvaV2CommandKind.None
                || directExtension.Reason != xPvaV2ExtensionRejectReason.RelationOpposesContainer.ToString())
                failures.Add("Expected 827-829 fixture to reject HHHL as a Down extension before any upward P3 adjustment can apply.");

            var engine = new xPvaV2Engine();
            xPvaV2Container down = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 827,
                EndBar = 828,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(827, 1730.0),
                P2 = new xPvaV2PricePoint(828, 1660.0),
                P3 = new xPvaV2PricePoint(828, 1710.0)
            });

            engine.ProcessSequentialBar(
                new xPvaV2Bar(828, 1710.0, 1660.0, xPvaV2BarRelation.LLLH),
                hhhl,
                1,
                1);

            bool sawDownExtensionApply = false;
            bool sawBreakoutOrExtensionReject = false;
            IList<xPvaV2TraceEntry> trace = engine.LastTrace;
            for (int i = 0; i < trace.Count; i++)
            {
                if (trace[i].Kind == xPvaV2TraceKind.ExtensionResult
                    && trace[i].Applied
                    && trace[i].ContainerId == down.Id)
                    sawDownExtensionApply = true;
                if ((trace[i].Kind == xPvaV2TraceKind.BreakoutResult
                        && trace[i].Applied
                        && trace[i].ContainerId == down.Id)
                    || (trace[i].Kind == xPvaV2TraceKind.ExtensionResult
                        && !trace[i].Applied
                        && trace[i].ContainerId == down.Id
                        && trace[i].Detail == xPvaV2ExtensionRejectReason.RelationOpposesContainer.ToString()))
                    sawBreakoutOrExtensionReject = true;
            }

            if (down.P3.Bar != 828
                || down.P3.Price != 1710.0
                || down.EndBar < 828
                || sawDownExtensionApply
                || !sawBreakoutOrExtensionReject)
                failures.Add("Expected 827-829 fixture to leave Down P3 unchanged and avoid applying a Down extension on HHHL.");
        }

        private static void AssertFixture824BreakoutPromotesExistingDownChild(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container parent = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 805,
                EndBar = 823,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(805, 1650.0),
                P2 = new xPvaV2PricePoint(823, 1740.0),
                P3 = new xPvaV2PricePoint(810, 1660.0)
            });
            xPvaV2Container child = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 810,
                EndBar = 823,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(810, 1760.0),
                P2 = new xPvaV2PricePoint(823, 1640.0),
                P3 = new xPvaV2PricePoint(818, 1740.0)
            });
            engine.Model.LinkContainment(parent.Id, child.Id);

            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(
                new xPvaV2Bar(823, 1700.0, 1640.0, xPvaV2BarRelation.LLLH),
                new xPvaV2Bar(824, 1695.0, 1630.0, xPvaV2BarRelation.LLLH),
                1,
                1);

            int containerCount = 0;
            int newDownAtBreakoutCount = 0;
            bool sawParentBreakout = false;
            bool sawChildPromotion = false;
            bool sawConstructionApply = false;
            foreach (xPvaV2Container container in engine.Model.ContainersByStart())
            {
                containerCount++;
                if (container.Direction == xPvaV2Direction.Down && container.StartBar >= 824)
                    newDownAtBreakoutCount++;
            }

            IList<xPvaV2TraceEntry> trace = engine.LastTrace;
            for (int i = 0; i < trace.Count; i++)
            {
                if (trace[i].Kind == xPvaV2TraceKind.BreakoutResult
                    && trace[i].Applied
                    && trace[i].ContainerId == parent.Id)
                    sawParentBreakout = true;
                if (trace[i].Kind == xPvaV2TraceKind.PromotionResult
                    && trace[i].Applied
                    && trace[i].ContainerId == child.Id)
                    sawChildPromotion = true;
                if (trace[i].Kind == xPvaV2TraceKind.ConstructionResult && trace[i].Applied)
                    sawConstructionApply = true;
            }

            if (results.Count != 2
                || !results[0].Applied
                || !results[1].Applied
                || results[0].ContainerId != parent.Id
                || results[1].ContainerId != child.Id
                || parent.Status != xPvaV2ContainerStatus.Frozen
                || child.Status != xPvaV2ContainerStatus.Active
                || child.VisualLevel != 1
                || child.StructuralLevel != 1
                || child.EndBar != 824
                || containerCount != 2
                || newDownAtBreakoutCount != 0
                || !sawParentBreakout
                || !sawChildPromotion
                || sawConstructionApply)
                failures.Add("Expected 824 fixture to freeze broken Up parent, promote existing 810-origin Down child, and avoid creating a new Down container at the breakout.");
        }

        private static void AssertFixture831BreakoutPromotesExistingUpChild(List<string> failures)
        {
            var engine = new xPvaV2Engine();
            xPvaV2Container parent = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Down,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 810,
                EndBar = 830,
                VisualLevel = 1,
                StructuralLevel = 1,
                P1 = new xPvaV2PricePoint(810, 1760.0),
                P2 = new xPvaV2PricePoint(830, 1600.0),
                P3 = new xPvaV2PricePoint(825, 1680.0)
            });
            xPvaV2Container child = engine.Model.AddContainer(new xPvaV2Container
            {
                Direction = xPvaV2Direction.Up,
                Status = xPvaV2ContainerStatus.Active,
                StartBar = 825,
                EndBar = 830,
                VisualLevel = 2,
                StructuralLevel = 2,
                P1 = new xPvaV2PricePoint(825, 1620.0),
                P2 = new xPvaV2PricePoint(830, 1700.0),
                P3 = new xPvaV2PricePoint(828, 1640.0)
            });
            engine.Model.LinkContainment(parent.Id, child.Id);

            IList<xPvaV2CommandResult> results = engine.ProcessSequentialBar(
                new xPvaV2Bar(830, 1690.0, 1620.0, xPvaV2BarRelation.HHHL),
                new xPvaV2Bar(831, 1700.0, 1665.0, xPvaV2BarRelation.HHHL),
                1,
                1);

            int containerCount = 0;
            int newUpAtBreakoutCount = 0;
            bool sawParentBreakout = false;
            bool sawChildPromotion = false;
            bool sawConstructionApply = false;
            foreach (xPvaV2Container container in engine.Model.ContainersByStart())
            {
                containerCount++;
                if (container.Direction == xPvaV2Direction.Up && container.StartBar >= 831)
                    newUpAtBreakoutCount++;
            }

            IList<xPvaV2TraceEntry> trace = engine.LastTrace;
            for (int i = 0; i < trace.Count; i++)
            {
                if (trace[i].Kind == xPvaV2TraceKind.BreakoutResult
                    && trace[i].Applied
                    && trace[i].ContainerId == parent.Id)
                    sawParentBreakout = true;
                if (trace[i].Kind == xPvaV2TraceKind.PromotionResult
                    && trace[i].Applied
                    && trace[i].ContainerId == child.Id)
                    sawChildPromotion = true;
                if (trace[i].Kind == xPvaV2TraceKind.ConstructionResult && trace[i].Applied)
                    sawConstructionApply = true;
            }

            if (results.Count != 2
                || !results[0].Applied
                || !results[1].Applied
                || results[0].ContainerId != parent.Id
                || results[1].ContainerId != child.Id
                || parent.Status != xPvaV2ContainerStatus.Frozen
                || child.Status != xPvaV2ContainerStatus.Active
                || child.VisualLevel != 1
                || child.StructuralLevel != 1
                || child.EndBar != 831
                || containerCount != 2
                || newUpAtBreakoutCount != 0
                || !sawParentBreakout
                || !sawChildPromotion
                || sawConstructionApply)
                failures.Add("Expected 831 fixture to freeze broken Down parent, promote existing 825-origin Up child, and avoid creating a new Up container at the breakout.");
        }
    }
}
