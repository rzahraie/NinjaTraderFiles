using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.Indicators
{
    internal sealed class xPvaContainerEngineV2
    {
        internal enum Direction
        {
            Unknown,
            Up,
            Down
        }

        internal enum Status
        {
            Active,
            Frozen,
            Inactive,
            Joined
        }

        internal enum DecisionKind
        {
            None,
            CreateRoot,
            CreateChild,
            ExtendParent,
            PromoteChild,
            Suppress
        }

        internal sealed class Container
        {
            public int Id;
            public int ParentId;
            public int Level;
            public Direction Direction;
            public Status Status;
            public int StartBar;
            public int EndBar;
            public string Reason;
            public readonly List<int> ChildIds = new List<int>();
        }

        internal sealed class BarContext
        {
            public int Bar;
            public int OriginBar;
            public Direction TapeDirection;
            public bool IsTransitional;
            public bool IsTranslational;
            public int LocalParentId;
            public int OppositeChildBreakoutId { get; set; }
            public readonly List<int> FrozenParentIds = new List<int>();
        }

        internal sealed class Decision
        {
            public DecisionKind Kind;
            public int ContainerId;
            public int ParentId;
            public Direction Direction;
            public int StartBar;
            public int EndBar;
            public int Level;
            public string Reason;
        }

        private readonly List<Container> containers = new List<Container>();
        private int nextId = 1;

        internal IReadOnlyList<Container> Containers
        {
            get { return containers; }
        }

        internal Container SeedContainer(int id, int parentId, Direction direction, int level, Status status, int startBar, int endBar, string reason)
        {
            var container = new Container
            {
                Id = id,
                ParentId = parentId,
                Direction = direction,
                Level = level,
                Status = status,
                StartBar = startBar,
                EndBar = endBar,
                Reason = reason ?? string.Empty
            };

            containers.Add(container);
            if (id >= nextId)
                nextId = id + 1;

            Container parent = Find(parentId);
            if (parent != null && !parent.ChildIds.Contains(id))
                parent.ChildIds.Add(id);

            return container;
        }

        internal Decision Process(BarContext context)
        {
            if (context == null || context.Bar <= 0)
                return None("missing bar context");

            Decision promoted = TryPromoteAfterParentBreak(context);
            if (promoted.Kind != DecisionKind.None)
                return promoted;

            if (!context.IsTransitional)
                return None(context.IsTranslational ? "translational bar" : "non-transitional bar");

            if (context.TapeDirection == Direction.Unknown)
                return None("unknown tape direction");

            Container parent = ResolveLocalParent(context);
            if (parent == null)
                return CreateRoot(context);

            if (context.TapeDirection != parent.Direction)
                return CreateChild(context, parent, "opposite tape inside local parent");

            if (IsDirectOppositeChildBreakout(parent, context.OppositeChildBreakoutId))
                return CreateChild(context, parent, "same-direction tape after opposite child breakout");

            parent.EndBar = Math.Max(parent.EndBar, context.Bar);
            return new Decision
            {
                Kind = DecisionKind.ExtendParent,
                ContainerId = parent.Id,
                ParentId = parent.ParentId,
                Direction = parent.Direction,
                StartBar = parent.StartBar,
                EndBar = parent.EndBar,
                Level = parent.Level,
                Reason = "same-direction tape extended local parent"
            };
        }

        private Decision TryPromoteAfterParentBreak(BarContext context)
        {
            foreach (int parentId in context.FrozenParentIds)
            {
                Container parent = Find(parentId);
                if (parent == null || parent.Direction == Direction.Unknown)
                    continue;

                Direction promotedDirection = Opposite(parent.Direction);
                Container child = FindBestActiveDirectChild(parent.Id, promotedDirection);
                if (child == null)
                    continue;

                DeactivateDescendantsExcept(parent.Id, child.Id);
                Detach(child);
                child.ParentId = parent.ParentId;
                child.Level = parent.Level;
                child.EndBar = Math.Max(child.EndBar, context.Bar);
                parent.Status = Status.Frozen;

                Container inheritedParent = Find(child.ParentId);
                if (inheritedParent != null && !inheritedParent.ChildIds.Contains(child.Id))
                    inheritedParent.ChildIds.Add(child.Id);

                return new Decision
                {
                    Kind = DecisionKind.PromoteChild,
                    ContainerId = child.Id,
                    ParentId = child.ParentId,
                    Direction = child.Direction,
                    StartBar = child.StartBar,
                    EndBar = child.EndBar,
                    Level = child.Level,
                    Reason = "promoted active opposite child after parent break"
                };
            }

            return None("no promotable broken-parent child");
        }

        private Decision CreateRoot(BarContext context)
        {
            Container created = AddContainer(0, context.TapeDirection, 1, context.OriginBar, context.Bar, "root tape");
            return CreatedDecision(DecisionKind.CreateRoot, created);
        }

        private Decision CreateChild(BarContext context, Container parent, string reason)
        {
            int origin = Math.Max(context.OriginBar, parent.StartBar);
            Container created = AddContainer(parent.Id, context.TapeDirection, parent.Level + 1, origin, context.Bar, reason);
            return CreatedDecision(DecisionKind.CreateChild, created);
        }

        private Container AddContainer(int parentId, Direction direction, int level, int startBar, int endBar, string reason)
        {
            var container = new Container
            {
                Id = nextId++,
                ParentId = parentId,
                Direction = direction,
                Level = Math.Max(1, level),
                Status = Status.Active,
                StartBar = startBar,
                EndBar = endBar,
                Reason = reason
            };

            containers.Add(container);
            Container parent = Find(parentId);
            if (parent != null && !parent.ChildIds.Contains(container.Id))
                parent.ChildIds.Add(container.Id);

            return container;
        }

        private Decision CreatedDecision(DecisionKind kind, Container container)
        {
            return new Decision
            {
                Kind = kind,
                ContainerId = container.Id,
                ParentId = container.ParentId,
                Direction = container.Direction,
                StartBar = container.StartBar,
                EndBar = container.EndBar,
                Level = container.Level,
                Reason = container.Reason
            };
        }

        private Container ResolveLocalParent(BarContext context)
        {
            Container explicitParent = Find(context.LocalParentId);
            if (IsLive(explicitParent))
                return explicitParent;

            Container selected = null;
            foreach (Container container in containers)
            {
                if (!IsLive(container))
                    continue;
                if (container.StartBar > context.Bar || container.EndBar < context.OriginBar)
                    continue;
                if (selected == null
                    || container.Level > selected.Level
                    || (container.Level == selected.Level && container.StartBar > selected.StartBar))
                    selected = container;
            }

            return selected;
        }

        private Container FindBestActiveDirectChild(int parentId, Direction direction)
        {
            Container selected = null;
            foreach (Container container in containers)
            {
                if (container.ParentId != parentId || container.Direction != direction || !IsLive(container))
                    continue;
                if (selected == null
                    || container.Level > selected.Level
                    || (container.Level == selected.Level && container.StartBar > selected.StartBar))
                    selected = container;
            }

            return selected;
        }

        private bool IsDirectOppositeChildBreakout(Container parent, int childId)
        {
            if (parent == null || childId == 0)
                return false;

            Container child = Find(childId);
            return child != null
                && child.ParentId == parent.Id
                && child.Direction != parent.Direction
                && child.Direction != Direction.Unknown;
        }

        private void DeactivateDescendantsExcept(int parentId, int preservedRootId)
        {
            foreach (Container container in containers)
            {
                if (container.Id == parentId || container.Id == preservedRootId)
                    continue;
                if (IsDescendantOf(container, preservedRootId))
                    continue;
                if (IsDescendantOf(container, parentId) && container.Status == Status.Active)
                    container.Status = Status.Inactive;
            }
        }

        private bool IsDescendantOf(Container container, int ancestorId)
        {
            if (container == null || ancestorId == 0)
                return false;

            int parentId = container.ParentId;
            int guard = 0;
            while (parentId != 0 && guard++ < containers.Count)
            {
                if (parentId == ancestorId)
                    return true;

                Container parent = Find(parentId);
                if (parent == null)
                    return false;

                parentId = parent.ParentId;
            }

            return false;
        }

        private void Detach(Container container)
        {
            if (container == null || container.ParentId == 0)
                return;

            Container parent = Find(container.ParentId);
            if (parent != null)
                parent.ChildIds.Remove(container.Id);
        }

        private bool IsLive(Container container)
        {
            return container != null && (container.Status == Status.Active || container.Status == Status.Joined);
        }

        private Container Find(int id)
        {
            if (id == 0)
                return null;

            foreach (Container container in containers)
                if (container.Id == id)
                    return container;

            return null;
        }

        private static Direction Opposite(Direction direction)
        {
            if (direction == Direction.Up)
                return Direction.Down;
            if (direction == Direction.Down)
                return Direction.Up;
            return Direction.Unknown;
        }

        private static Decision None(string reason)
        {
            return new Decision { Kind = DecisionKind.None, Reason = reason };
        }

        internal static IList<string> RunKnownWindowSelfTest()
        {
            var failures = new List<string>();
            AssertPromotes810DownChildAfter824Break(failures);
            AssertCreates854To855DownChild(failures);
            AssertPromotes825UpChildAfter831Break(failures);
            AssertPromotes850UpChildAfter858Break(failures);
            AssertSuppressesSameDirectionChildWithoutDirectBreakout(failures);
            return failures;
        }

        private static void AssertPromotes810DownChildAfter824Break(List<string> failures)
        {
            var engine = new xPvaContainerEngineV2();
            engine.SeedContainer(17, 0, Direction.Up, 1, Status.Active, 740, 825, "joined triad 8,15,16");
            engine.SeedContainer(30, 17, Direction.Down, 2, Status.Active, 810, 824, "joined triad 27,28,29");

            var context = new BarContext
            {
                Bar = 825,
                OriginBar = 824,
                TapeDirection = Direction.Down,
                IsTransitional = false,
                IsTranslational = true
            };
            context.FrozenParentIds.Add(17);

            Decision decision = engine.Process(context);
            Container promoted = engine.Find(30);

            if (decision.Kind != DecisionKind.PromoteChild
                || promoted == null
                || promoted.ParentId != 0
                || promoted.Level != 1
                || promoted.EndBar != 825)
            {
                failures.Add("824/825 expected promotion of Down child 30 to root level 1, got " + Describe(decision));
            }
        }

        private static void AssertCreates854To855DownChild(List<string> failures)
        {
            var engine = new xPvaContainerEngineV2();
            engine.SeedContainer(33, 0, Direction.Down, 1, Status.Active, 810, 855, "joined triad 30,31,32");
            engine.SeedContainer(34, 33, Direction.Up, 2, Status.Active, 850, 854, "opposite child inside active container 33");

            Decision decision = engine.Process(new BarContext
            {
                Bar = 855,
                OriginBar = 854,
                TapeDirection = Direction.Down,
                IsTransitional = true,
                LocalParentId = 34
            });

            if (decision.Kind != DecisionKind.CreateChild
                || decision.ParentId != 34
                || decision.Direction != Direction.Down
                || decision.StartBar != 854
                || decision.EndBar != 855)
            {
                failures.Add("854-855 expected Down child under Up parent 34, got " + Describe(decision));
            }
        }

        private static void AssertPromotes825UpChildAfter831Break(List<string> failures)
        {
            var engine = new xPvaContainerEngineV2();
            engine.SeedContainer(30, 0, Direction.Down, 1, Status.Active, 810, 832, "joined triad 27,28,29");
            engine.SeedContainer(31, 30, Direction.Up, 2, Status.Active, 825, 831, "opposite child inside active container 30");

            var context = new BarContext
            {
                Bar = 832,
                OriginBar = 831,
                TapeDirection = Direction.Up,
                IsTransitional = false,
                IsTranslational = true
            };
            context.FrozenParentIds.Add(30);

            Decision decision = engine.Process(context);
            Container promoted = engine.Find(31);

            if (decision.Kind != DecisionKind.PromoteChild
                || promoted == null
                || promoted.ParentId != 0
                || promoted.Level != 1
                || promoted.EndBar != 832)
            {
                failures.Add("831/832 expected promotion of Up child 31 to root level 1, got " + Describe(decision));
            }
        }

        private static void AssertPromotes850UpChildAfter858Break(List<string> failures)
        {
            var engine = new xPvaContainerEngineV2();
            engine.SeedContainer(33, 0, Direction.Down, 1, Status.Active, 810, 859, "joined triad 30,31,32");
            engine.SeedContainer(34, 33, Direction.Up, 2, Status.Active, 850, 858, "opposite child inside active container 33");

            var context = new BarContext
            {
                Bar = 859,
                OriginBar = 858,
                TapeDirection = Direction.Up,
                IsTransitional = false,
                IsTranslational = true
            };
            context.FrozenParentIds.Add(33);

            Decision decision = engine.Process(context);
            Container promoted = engine.Find(34);

            if (decision.Kind != DecisionKind.PromoteChild
                || promoted == null
                || promoted.ParentId != 0
                || promoted.Level != 1
                || promoted.EndBar != 859)
            {
                failures.Add("858/859 expected promotion of Up child 34 to root level 1, got " + Describe(decision));
            }
        }

        private static void AssertSuppressesSameDirectionChildWithoutDirectBreakout(List<string> failures)
        {
            var engine = new xPvaContainerEngineV2();
            engine.SeedContainer(3, 1, Direction.Down, 2, Status.Active, 755, 760, "opposite child inside active container 1");
            engine.SeedContainer(4, 3, Direction.Up, 3, Status.Active, 756, 759, "opposite child inside active container 3");

            Decision decision = engine.Process(new BarContext
            {
                Bar = 759,
                OriginBar = 758,
                TapeDirection = Direction.Up,
                IsTransitional = true,
                LocalParentId = 4,
                OppositeChildBreakoutId = 3
            });

            if (decision.Kind == DecisionKind.CreateChild)
                failures.Add("759 expected same-direction child suppression unless broken opposite child is direct to parent, got " + Describe(decision));
        }

        private static string Describe(Decision decision)
        {
            if (decision == null)
                return "null";

            return decision.Kind
                + " id=" + decision.ContainerId
                + " parent=" + decision.ParentId
                + " dir=" + decision.Direction
                + " start=" + decision.StartBar
                + " end=" + decision.EndBar
                + " level=" + decision.Level
                + " reason=" + decision.Reason;
        }
    }
}
