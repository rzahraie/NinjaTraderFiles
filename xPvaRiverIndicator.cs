#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xPvaRiverIndicator : Indicator
    {
        private NinjaTrader.NinjaScript.xPva.Engine.xPvaEngine engine;

        private readonly Dictionary<int, string> upperLane = new Dictionary<int, string>();
        private readonly Dictionary<int, string> middleLane = new Dictionary<int, string>();
        private readonly Dictionary<int, string> lowerLane = new Dictionary<int, string>();

        [NinjaScriptProperty]
        [Range(1, 10)]
        [Display(Name = "VolPivotWindow", Order = 1, GroupName = "Parameters")]
        public int VolPivotWindow { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Print Events", Order = 2, GroupName = "Parameters")]
        public bool PrintEvents { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw FTT", Order = 3, GroupName = "Parameters")]
        public bool DrawFtt { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw Action Boxes", Order = 4, GroupName = "Parameters")]
        public bool DrawActionBoxes { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw Turn/Trend Lane", Order = 5, GroupName = "Parameters")]
        public bool DrawTurnTrendLane { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw OOE Lane", Order = 6, GroupName = "Parameters")]
        public bool DrawOoeLane { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw Container IDs", Order = 7, GroupName = "Parameters")]
        public bool DrawContainerIds { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Draw Provisional Geometry", Order = 8, GroupName = "Parameters")]
        public bool DrawGeometry { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Font Size", Order = 9, GroupName = "Parameters")]
        [Range(8, 18)]
        public int FontSize { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaRiverIndicator";
                Description = "Lane-based river chart view for xPvaEngine.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;

                VolPivotWindow = 1;
                PrintEvents = false;
                DrawFtt = true;
                DrawActionBoxes = true;
                DrawTurnTrendLane = true;
                DrawOoeLane = true;
                DrawContainerIds = true;
                DrawGeometry = false;
                FontSize = 10;
            }
            else if (State == State.DataLoaded)
            {
                engine = new NinjaTrader.NinjaScript.xPva.Engine.xPvaEngine(VolPivotWindow);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1 || engine == null)
                return;

            DateTime timeUtc = DateTime.SpecifyKind(Time[0], DateTimeKind.Local).ToUniversalTime();

            var snap = new NinjaTrader.NinjaScript.xPva.Engine.BarSnapshot(
                timeUtc,
                Open[0],
                High[0],
                Low[0],
                Close[0],
                (long)Volume[0],
                CurrentBar);

            var evs = engine.Step(snap);
            if (evs == null || evs.Events == null || evs.Events.Length == 0)
                return;

            foreach (var e in evs.Events)
            {
                if (PrintEvents)
                    Print(FormatEventLine(e));

                switch (e.Kind)
                {
                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.FttConfirmed:
                        if (DrawFtt && e.FttConfirmed.HasValue)
                            DrawFttConfirmed(e.FttConfirmed.Value);
                        break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.Action:
                        if (DrawActionBoxes && e.Action.HasValue)
                            DrawActionBox(e.Action.Value);
                        break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.ContainerReport:
                        if (DrawContainerIds && e.ContainerReport.HasValue)
                            DrawContainerId(e.ContainerReport.Value);
                        break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.Turn:
                        if (DrawTurnTrendLane && e.Turn.HasValue)
                            QueueMiddleLane(e.Turn.Value.BarIndex, "T" + e.Turn.Value.Type);
                        break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.TrendType:
                        if (DrawTurnTrendLane && e.TrendType.HasValue)
                            QueueMiddleLane(e.TrendType.Value.BarIndex, "TT:" + e.TrendType.Value.Type);
                        break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.VolOoe:
                        if (DrawOoeLane && e.VolOoe.HasValue)
                            QueueLowerLane(e.VolOoe.Value.BarIndex, e.VolOoe.Value.Name.ToString());
                        break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.VolPivot:
                        if (DrawOoeLane && e.VolPivot.HasValue)
                            QueueLowerLane(e.VolPivot.Value.BarIndex,
                                e.VolPivot.Value.Kind == NinjaTrader.NinjaScript.xPva.Engine.VolPivotKind.Peak ? "Pk" : "Tr");
                        break;

                    case NinjaTrader.NinjaScript.xPva.Engine.EventKind.ContainerGeometry:
                        if (DrawGeometry && e.ContainerGeometry.HasValue)
                            DrawContainerGeometryEvent(e.ContainerGeometry.Value);
                        break;
                }
            }

            FlushLanes(CurrentBar);
        }

        private void QueueMiddleLane(int barIndex, string token)
        {
            if (!middleLane.ContainsKey(barIndex))
                middleLane[barIndex] = token;
            else
                middleLane[barIndex] = middleLane[barIndex] + " " + token;
        }

        private void QueueLowerLane(int barIndex, string token)
        {
            if (!lowerLane.ContainsKey(barIndex))
                lowerLane[barIndex] = token;
            else
                lowerLane[barIndex] = lowerLane[barIndex] + " " + token;
        }

        private void FlushLanes(int currentBarIndex)
        {
            DrawLaneToken(upperLane, currentBarIndex, 7 * TickSize, Brushes.LightGray, "U");
            DrawLaneToken(middleLane, currentBarIndex, -7 * TickSize, Brushes.MediumPurple, "M");
            DrawLaneToken(lowerLane, currentBarIndex, -12 * TickSize, Brushes.DeepSkyBlue, "L");
        }

        private void DrawLaneToken(
            Dictionary<int, string> lane,
            int currentBarIndex,
            double yOffset,
            Brush brush,
            string prefix)
        {
            if (!lane.TryGetValue(currentBarIndex, out string text))
                return;

            int barsAgo = BarsAgoFromIndex(currentBarIndex);
            string tag = string.Format("xPvaRiver_{0}_{1}", prefix, currentBarIndex);

            double y = yOffset > 0
                ? High[barsAgo] + yOffset
                : Low[barsAgo] + yOffset;

            Draw.Text(
                this,
                tag,
                false,
                text,
                barsAgo,
                y,
                0,
                brush,
                new SimpleFont("Arial", FontSize),
                TextAlignment.Left,
                Brushes.Transparent,
                Brushes.Transparent,
                0);
        }

        private void DrawFttConfirmed(NinjaTrader.NinjaScript.xPva.Engine.FttConfirmedEvent e)
        {
            int barsAgo = BarsAgoFromIndex(e.BarIndex);
            string tag = string.Format("xPvaRiverFTT_{0}_{1}", e.ContainerId, e.BarIndex);

            if (e.PriorDirection == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up)
            {
                Draw.ArrowDown(this, tag, false, barsAgo, High[barsAgo] + 2 * TickSize, Brushes.Gold);
            }
            else if (e.PriorDirection == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Down)
            {
                Draw.ArrowUp(this, tag, false, barsAgo, Low[barsAgo] - 2 * TickSize, Brushes.Gold);
            }
        }

        private void DrawActionBox(NinjaTrader.NinjaScript.xPva.Engine.ActionEvent e)
        {
            int barsAgo = BarsAgoFromIndex(e.BarIndex);
            string tag = string.Format("xPvaRiverAction_{0}_{1}", e.ContainerId, e.BarIndex);

            string text = ActionToken(e.Action);
            Brush brush = ActionBrush(e.Action);

            double y = High[barsAgo] + 4 * TickSize;

            Draw.Text(
                this,
                tag,
                false,
                text,
                barsAgo,
                y,
                0,
                brush,
                new SimpleFont("Arial", FontSize),
                TextAlignment.Left,
                Brushes.Transparent,
                Brushes.Black,
                0);
        }

        private void DrawContainerId(NinjaTrader.NinjaScript.xPva.Engine.ContainerReportEvent e)
        {
            int anchorBarIndex = e.FttConfirmedBarIndex ?? e.StartBarIndex;
            int barsAgo = BarsAgoFromIndex(anchorBarIndex);
            string tag = string.Format("xPvaRiverCID_{0}_{1}", e.ContainerId, anchorBarIndex);

            Draw.Text(
                this,
                tag,
                false,
                "C#" + e.ContainerId,
                barsAgo,
                High[barsAgo] + 1 * TickSize,
                0,
                Brushes.White,
                new SimpleFont("Arial", FontSize),
                TextAlignment.Left,
                Brushes.Transparent,
                Brushes.Transparent,
                0);
        }

        private void DrawContainerGeometryEvent(NinjaTrader.NinjaScript.xPva.Engine.ContainerGeometryEvent e)
        {
            int startBarsAgo = BarsAgoFromIndex(e.StartBarIndex);
            int extremeBarsAgo = BarsAgoFromIndex(e.ExtremeBarIndex);
            int confirmBarsAgo = BarsAgoFromIndex(e.ConfirmBarIndex);

            string rtlTag = string.Format("xPvaRiverGeoRTL_{0}", e.ContainerId);
            string diagTag = string.Format("xPvaRiverGeoDiag_{0}", e.ContainerId);

            Brush brush = e.Direction == NinjaTrader.NinjaScript.xPva.Engine.ContainerDirection.Up
                ? Brushes.DodgerBlue
                : Brushes.IndianRed;

            Draw.Line(
                this,
                rtlTag,
                false,
                startBarsAgo,
                e.StartPrice,
                confirmBarsAgo,
                e.ConfirmPrice,
                brush,
                NinjaTrader.Gui.DashStyleHelper.Solid,
                2);

            Draw.Line(
                this,
                diagTag,
                false,
                extremeBarsAgo,
                e.ExtremePrice,
                confirmBarsAgo,
                e.ConfirmPrice,
                brush,
                NinjaTrader.Gui.DashStyleHelper.Dot,
                1);
        }

        private string ActionToken(NinjaTrader.NinjaScript.xPva.Engine.ActionType action)
        {
            switch (action)
            {
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Enter:
                    return "ENT";
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Reverse:
                    return "REV";
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Sideline:
                    return "SIDE";
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Hold:
                    return "HOLD";
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.StayIn:
                    return "IN";
                default:
                    return "?";
            }
        }

        private Brush ActionBrush(NinjaTrader.NinjaScript.xPva.Engine.ActionType action)
        {
            switch (action)
            {
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Enter:
                    return Brushes.LimeGreen;
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Reverse:
                    return Brushes.OrangeRed;
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Sideline:
                    return Brushes.Gray;
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.Hold:
                case NinjaTrader.NinjaScript.xPva.Engine.ActionType.StayIn:
                    return Brushes.DeepSkyBlue;
                default:
                    return Brushes.White;
            }
        }

        private string FormatEventLine(NinjaTrader.NinjaScript.xPva.Engine.EngineEvent e)
        {
            return string.Format("{0} B{1}: {2} {3}", Instrument.FullName, e.BarIndex, e.Kind, e.Text);
        }

        private int BarsAgoFromIndex(int eventBarIndex)
        {
            int barsAgo = CurrentBar - eventBarIndex;
            if (barsAgo < 0)
                barsAgo = 0;
            return barsAgo;
        }
    }
}