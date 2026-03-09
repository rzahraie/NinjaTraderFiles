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
			public xPvaEndEffects.State EndEffects;
			public xPvaTrendTypes.State TrendTypes;
			public xPvaTurns.State Turns;
			public xPvaActionResolver.State Actions;
			public xPvaContainers.State Containers;
			public xPvaStructureResolver.State Structures;
			public TrendTypeEvent? LastTrendType;
			public xPvaContainerReport.State Reports;
			public xPvaPersistentContainers.State PersistentContainers;
			public xPvaContainerGeometry.State Geometries;
			public Dictionary<int, BarSnapshot> BarsByIndex;

            public State(int volPivotWindow)
            {
                HasPrevBar = false;
                PrevBar = default;
                VolPivots = new xPvaVolumePivots.State(volPivotWindow);
                VolOoe = new xPvaVolumeOoe.State();
				EndEffects = new xPvaEndEffects.State();
				Turns = new xPvaTurns.State();
				TrendTypes = new xPvaTrendTypes.State();
				Actions = new xPvaActionResolver.State();
				Containers = new xPvaContainers.State();
				Structures = new xPvaStructureResolver.State();
				Reports = new xPvaContainerReport.State();
				PersistentContainers = new xPvaPersistentContainers.State();
				Geometries = new xPvaContainerGeometry.State();
				BarsByIndex = new Dictionary<int, BarSnapshot>();
				LastTrendType = null;
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
			_s.BarsByIndex[bar.Index] = bar;

            if (!_s.HasPrevBar)
            {
                _s.PrevBar = bar;
                _s.HasPrevBar = true;
                return EngineEvents.Empty;
            }

            // 1) PriceCase
            PriceCase pc = xPvaPriceCases.Classify(bar, _s.PrevBar);
			PriceCaseEvent pce = new PriceCaseEvent(bar.Index, pc);
			events.Add(EngineEvent.From(pce));
			
			ContainerEvent? container = xPvaContainers.Step(_s.Containers, pce);
			if (container.HasValue)
			{
			    events.Add(EngineEvent.From(container.Value));
				
				_s.Reports.OnContainer(container.Value);
				
				PersistentContainerEvent? pc0 = _s.PersistentContainers.OnContainer(container.Value, bar);
				if (pc0.HasValue)
				{
				    events.Add(EngineEvent.From(pc0.Value));
				    _s.Geometries.OnPersistentContainer(pc0.Value);
				}
			
			    if (container.Value.HasDirectionBreak && container.Value.DirectionBreak.HasValue)
				{
					var db = container.Value.DirectionBreak.Value;

				    events.Add(EngineEvent.From(db));
				
				    _s.Reports.OnDirectionBreak(db);
				
				    PersistentContainerEvent? pc1 = _s.PersistentContainers.OnDirectionBreak(db);
				    if (pc1.HasValue)
				    {
				        events.Add(EngineEvent.From(pc1.Value));
				        _s.Geometries.OnPersistentContainer(pc1.Value);
				    }
				}
			
			    if (container.Value.HasFttCandidate && container.Value.FttCandidate.HasValue) 
				{
					var cand = container.Value.FttCandidate.Value;
			        events.Add(EngineEvent.From(cand));
					_s.Reports.OnFttCandidate(cand);
				}
			
			    if (container.Value.HasFttConfirmed && container.Value.FttConfirmed.HasValue)
				{
				    FttConfirmedEvent confirmed = container.Value.FttConfirmed.Value;
				    events.Add(EngineEvent.From(confirmed));
					_s.Reports.OnFttConfirmed(confirmed);
					
					PersistentContainerEvent? pc2 = _s.PersistentContainers.OnFttConfirmed(confirmed);
					if (pc2.HasValue)
					{
					    events.Add(EngineEvent.From(pc2.Value));
					    _s.Geometries.OnPersistentContainer(pc2.Value);
					}
				
				    if (_s.LastTrendType.HasValue)
					{
					    StructureEvent? structure = xPvaStructureResolver.Step(
					        _s.Structures,
					        confirmed,
					        _s.LastTrendType.Value);
					
					    if (structure.HasValue)
					    {
					        events.Add(EngineEvent.From(structure.Value));
							
							_s.Reports.OnStructure(structure.Value);
					
					        TurnEvent? lastTurn = null;
					        if (_s.Turns.LastType != TurnType.Unknown)
					        {
					            lastTurn = new TurnEvent(
					                structure.Value.BarIndex,
					                _s.Turns.LastType,
					                _s.Turns.LastSourceKind,
					                _s.Turns.LastBand,
					                _s.Turns.LastSource,
					                0);
					        }
					
					        if (lastTurn.HasValue)
					        {
					            ActionEvent? action = xPvaActionResolver.Step(
					                _s.Actions,
					                structure.Value,
					                lastTurn.Value);
					
					            if (action.HasValue) {
					                events.Add(EngineEvent.From(action.Value));
									
									PersistentContainerEvent? pc3 = _s.PersistentContainers.OnAction(action.Value);
									if (pc3.HasValue)
									{
									    events.Add(EngineEvent.From(pc3.Value));
									    _s.Geometries.OnPersistentContainer(pc3.Value);
									}
									
									var report = _s.Reports.OnAction(action.Value);
								    if (report.HasValue)
								        events.Add(EngineEvent.From(report.Value));
									
									ContainerGeometryEvent? geo = _s.Geometries.OnAction(action.Value, _s.BarsByIndex);
									if (geo.HasValue)
									    events.Add(EngineEvent.From(geo.Value));
								}
					        }
					    }
					}
				}
			}

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
				{
				    events.Add(EngineEvent.From(ooe.Value));
				
				    EndEffectEvent? ee = xPvaEndEffects.Step(_s.EndEffects, ooe.Value);
				    if (ee.HasValue)
				    {
				        events.Add(EngineEvent.From(ee.Value));
				
				        TurnEvent? turn = xPvaTurns.Step(_s.Turns, ee.Value);
						if (turn.HasValue)
						{
						    events.Add(EngineEvent.From(turn.Value));
						
						    TrendTypeEvent? trendType = xPvaTrendTypes.Step(_s.TrendTypes, turn.Value);
							if (trendType.HasValue)
							{
							    _s.LastTrendType = trendType.Value;
							    events.Add(EngineEvent.From(trendType.Value));
							}
						}
				    }
				}
            }

            _s.PrevBar = bar;
            return new EngineEvents(events.ToArray());
        }
    }
}

















