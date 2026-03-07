using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaEngine
    {
        public sealed class State
        {
            public bool HasPrevBar;
            public BarSnapshot PrevBar;

            public xPvaVolumePivots.State VolPivots;
            public xPvaVolumeOoe.State VolOoe;

            public State(int volPivotWindow)
            {
                HasPrevBar = false;
                PrevBar = default;
                VolPivots = new xPvaVolumePivots.State(volPivotWindow);
                VolOoe = new xPvaVolumeOoe.State();
            }
        }

        private readonly State _s;

        public xPvaEngine(int volPivotWindow = 1)
        {
            _s = new State(volPivotWindow);
        }

        public State GetState() => _s;

        public EngineEvents Step(in BarSnapshot bar)
        {
            var events = new List<EngineEvent>(capacity: 6);

            if (!_s.HasPrevBar)
            {
                _s.PrevBar = bar;
                _s.HasPrevBar = true;
                return EngineEvents.Empty;
            }

            // 1) PriceCase
            PriceCase pc = xPvaPriceCases.Classify(bar, _s.PrevBar);
            events.Add(EngineEvent.From(new PriceCaseEvent(bar.Index, pc)));

            // 2) Permission (phase1: translations only)
            PermissionEvent perm = xPvaPermission.Evaluate(bar.Index, pc);
            events.Add(EngineEvent.From(perm));

            // 3) Volume pivots (confirmed)
            VolPivotEvent? pivot = xPvaVolumePivots.Step(_s.VolPivots, bar);
            if (pivot.HasValue)
            {
                events.Add(EngineEvent.From(pivot.Value));

                // 4) Volume OOE (minimal)
                VolOoeEvent? ooe = xPvaVolumeOoe.Step(_s.VolOoe, pivot.Value, perm);
                if (ooe.HasValue)
                    events.Add(EngineEvent.From(ooe.Value));
            }

            _s.PrevBar = bar;
            return new EngineEvents(events.ToArray());
        }
    }
}
