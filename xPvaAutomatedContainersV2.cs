#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.xPva.Engine;
using SharpDX;
using SharpDX.Direct2D1;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    internal static class xPvaV2Nt8Adapter
    {
        public const string FixturePrefix = "[APVA V2][FIXTURE] ";
        public const string FixtureBeginPrefix = "[APVA V2][FIXTURE-BEGIN] ";
        public const string FixtureEndPrefix = "[APVA V2][FIXTURE-END] ";
        public const string FixturePreviewPrefix = "[APVA V2][FIXTURE-PREVIEW] ";

        public static xPvaV2BarRelation ToV2Relation(xPvaBarRelation relation)
        {
            switch (relation)
            {
                case xPvaBarRelation.HHHL: return xPvaV2BarRelation.HHHL;
                case xPvaBarRelation.LLLH: return xPvaV2BarRelation.LLLH;
                case xPvaBarRelation.FTP: return xPvaV2BarRelation.FTP;
                case xPvaBarRelation.FBP: return xPvaV2BarRelation.FBP;
                case xPvaBarRelation.StitchLong: return xPvaV2BarRelation.StitchLong;
                case xPvaBarRelation.StitchShort: return xPvaV2BarRelation.StitchShort;
                case xPvaBarRelation.InsideBar: return xPvaV2BarRelation.InsideBar;
                case xPvaBarRelation.OutsideBullish: return xPvaV2BarRelation.OutsideBullish;
                case xPvaBarRelation.OutsideBearish: return xPvaV2BarRelation.OutsideBearish;
                case xPvaBarRelation.SameHighSameLow: return xPvaV2BarRelation.SameHighSameLow;
                case xPvaBarRelation.HighReversal: return xPvaV2BarRelation.HighReversal;
                case xPvaBarRelation.LowReversal: return xPvaV2BarRelation.LowReversal;
                default: return xPvaV2BarRelation.Unknown;
            }
        }

        public static bool ResolveBounds(bool debug, int configuredStartBar, int configuredEndBar, int currentBar, out int start, out int end)
        {
            start = configuredStartBar;
            end = configuredEndBar <= 0 ? currentBar : configuredEndBar;
            if (!debug)
            {
                start = Math.Max(1, currentBar - 300);
                end = currentBar;
            }

            if (start < 1 || end <= start || end > currentBar)
                return false;
            return true;
        }

        public static string RenderSegmentTag(xPvaV2RenderSegment segment)
        {
            return "APVA_V2_"
                + segment.ContainerId
                + "_"
                + segment.Kind
                + "_"
                + segment.StartBar
                + "_"
                + segment.EndBar;
        }

        public static string ReplayCommandSummary(int bar, xPvaV2CommandResult result)
        {
            if (result == null)
                return "bar=" + bar + " applied=False id=0 reason=null";

            return "bar=" + bar
                + " applied=" + result.Applied
                + " id=" + result.ContainerId
                + " reason=" + result.Reason;
        }

        public static string ReplayTraceSummary(xPvaV2TraceEntry trace)
        {
            if (trace == null)
                return "bar=0 kind=None applied=False id=0 detail=null";

            return "bar=" + trace.Bar
                + " kind=" + trace.Kind
                + " applied=" + trace.Applied
                + " id=" + trace.ContainerId
                + " detail=" + trace.Detail;
        }

        public static string ReplayFixtureRow(xPvaV2Bar bar)
        {
            return bar.Index
                + ","
                + bar.High.ToString(CultureInfo.InvariantCulture)
                + ","
                + bar.Low.ToString(CultureInfo.InvariantCulture)
                + ","
                + bar.RelationToPrevious;
        }

        public static string FixtureBegin(int startBar, int endBar)
        {
            return FixtureBeginPrefix + startBar + "-" + endBar;
        }

        public static string FixtureEnd(int startBar, int endBar)
        {
            return FixtureEndPrefix + startBar + "-" + endBar;
        }

        public static string FixturePreviewSummary(string summary)
        {
            return FixturePreviewPrefix + (summary ?? string.Empty);
        }

        public static string SelfTestSummary(int failureCount, int testCount)
        {
            return "selfTestFailures=" + failureCount + " selfTestChecks=" + testCount;
        }
    }

    public class xPvaAutomatedContainersV2 : Indicator
    {
        private readonly List<xPvaV2Bar> bars = new List<xPvaV2Bar>();
        private xPvaV2Engine engine;
        private xPvaDiscreteEventEngine eventEngine;
        private SolidColorBrush upBrushDx;
        private SolidColorBrush downBrushDx;
        private StrokeStyle dashStrokeStyle;

        [NinjaScriptProperty]
        [Display(Name = "Debug", GroupName = "Debug", Order = 1)]
        public bool Debug { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Start Bar", GroupName = "Debug", Order = 2)]
        public int StartBar { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "End Bar", GroupName = "Debug", Order = 3)]
        public int EndBar { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaAutomatedContainersV2";
                Description = "APVA V2 container model prototype.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DrawOnPricePanel = true;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;
                PrintTo = PrintTo.OutputTab2;
                Debug = true;
                StartBar = 0;
                EndBar = 0;
            }
            else if (State == State.DataLoaded)
            {
                eventEngine = new xPvaDiscreteEventEngine(TickSize);
            }
            else if (State == State.Terminated)
            {
                DisposeRenderResources();
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2)
                return;

            int start;
            int end;
            if (!ResolveBounds(out start, out end))
                return;
            if (CurrentBar < end)
                return;

            BuildModel(start, end);
        }

        public override void OnRenderTargetChanged()
        {
            DisposeRenderResources();
            if (RenderTarget == null)
                return;

            upBrushDx = new SolidColorBrush(RenderTarget, new SharpDX.Color(0, 0, 255, 255));
            downBrushDx = new SolidColorBrush(RenderTarget, SharpDX.Color.Red);
            dashStrokeStyle = new StrokeStyle(Core.Globals.D2DFactory,
                new StrokeStyleProperties { DashStyle = SharpDX.Direct2D1.DashStyle.Dash });
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (chartControl == null || chartScale == null || ChartBars == null || RenderTarget == null || engine == null)
                return;
            if (upBrushDx == null || downBrushDx == null)
                OnRenderTargetChanged();
            if (upBrushDx == null || downBrushDx == null)
                return;

            xPvaV2RenderSnapshot snapshot = engine.RenderSnapshot;
            AntialiasMode oldMode = RenderTarget.AntialiasMode;
            RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

            for (int i = 0; i < snapshot.Count; i++)
            {
                xPvaV2RenderSegment segment = snapshot[i];
                if (segment.EndBar < ChartBars.FromIndex || segment.StartBar > ChartBars.ToIndex)
                    continue;

                int startBar = Math.Max(segment.StartBar, ChartBars.FromIndex);
                int endBar = Math.Min(segment.EndBar, ChartBars.ToIndex);
                if (endBar < startBar)
                    continue;

                double slope = segment.EndBar == segment.StartBar
                    ? 0.0
                    : (segment.EndPrice - segment.StartPrice) / (segment.EndBar - segment.StartBar);
                double startPrice = segment.StartPrice + slope * (startBar - segment.StartBar);
                double endPrice = segment.StartPrice + slope * (endBar - segment.StartBar);

                float x1 = chartControl.GetXByBarIndex(ChartBars, startBar);
                float x2 = chartControl.GetXByBarIndex(ChartBars, endBar);
                float y1 = chartScale.GetYByValue(startPrice);
                float y2 = chartScale.GetYByValue(endPrice);
                SolidColorBrush brush = segment.Direction == xPvaV2Direction.Up ? upBrushDx : downBrushDx;
                float width = Math.Max(1.0f, 4.0f - segment.VisualLevel);
                StrokeStyle style = segment.Status == xPvaV2ContainerStatus.Active || segment.Status == xPvaV2ContainerStatus.Adjusted
                    ? null
                    : dashStrokeStyle;

                RenderTarget.DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), brush, width, style);
            }

            RenderTarget.AntialiasMode = oldMode;
        }

        private void BuildModel(int start, int end)
        {
            engine = new xPvaV2Engine();
            bars.Clear();
            var fixtureDebugOutput = new StringBuilder();
            if (Debug)
            {
                Print(xPvaV2Nt8Adapter.FixtureBegin(start, end));
                fixtureDebugOutput.AppendLine(xPvaV2Nt8Adapter.FixtureBegin(start, end));
            }

            xPvaV2Bar previous = AnalyzeBar(start);
            bars.Add(previous);
            if (Debug)
            {
                string fixtureRow = xPvaV2Nt8Adapter.FixturePrefix + xPvaV2Nt8Adapter.ReplayFixtureRow(previous);
                Print(fixtureRow);
                fixtureDebugOutput.AppendLine(fixtureRow);
            }

            for (int bar = start + 1; bar <= end; bar++)
            {
                xPvaV2Bar current = AnalyzeBar(bar);
                bars.Add(current);
                if (Debug)
                {
                    string fixtureRow = xPvaV2Nt8Adapter.FixturePrefix + xPvaV2Nt8Adapter.ReplayFixtureRow(current);
                    Print(fixtureRow);
                    fixtureDebugOutput.AppendLine(fixtureRow);
                }
                if (previous.Index != 0)
                {
                    IList<xPvaV2CommandResult> results = ProcessBar(previous, current);
                    if (Debug)
                    {
                        IList<xPvaV2TraceEntry> trace = engine.LastTrace;
                        for (int i = 0; i < trace.Count; i++)
                            Print("[APVA V2][TRACE] " + xPvaV2Nt8Adapter.ReplayTraceSummary(trace[i]));
                        for (int i = 0; i < results.Count; i++)
                        {
                            xPvaV2CommandResult result = results[i];
                            Print("[APVA V2][COMMAND] " + xPvaV2Nt8Adapter.ReplayCommandSummary(bar, result));
                        }
                    }
                }

                previous = current;
            }

            if (Debug)
            {
                Print(xPvaV2Nt8Adapter.FixtureEnd(start, end));
                fixtureDebugOutput.AppendLine(xPvaV2Nt8Adapter.FixtureEnd(start, end));
                IList<string> previewSummaries = xPvaV2FixtureReplay.PreviewCatalogReplacementSummaries(fixtureDebugOutput.ToString());
                for (int i = 0; i < previewSummaries.Count; i++)
                    Print(xPvaV2Nt8Adapter.FixturePreviewSummary(previewSummaries[i]));
                IList<string> failures = xPvaV2ModelSelfTest.Run();
                Print("[APVA V2] build complete: bars=" + start + "-" + end + " " + xPvaV2Nt8Adapter.SelfTestSummary(failures.Count, xPvaV2ModelSelfTest.TestCount));
                for (int i = 0; i < failures.Count; i++)
                    Print("[APVA V2][SELFTEST] " + failures[i]);
                PrintModelSummary();
            }
        }

        private IList<xPvaV2CommandResult> ProcessBar(xPvaV2Bar previous, xPvaV2Bar current)
        {
            return engine.ProcessSequentialBar(previous, current, 1, 1);
        }

        private xPvaV2Bar AnalyzeBar(int bar)
        {
            xPvaBarFacts current = FactsAt(bar);
            xPvaBarFacts previous = FactsAt(bar - 1);
            xPvaBarRelation relation = eventEngine.ClassifyRelation(current, previous);
            return new xPvaV2Bar(bar, High.GetValueAt(bar), Low.GetValueAt(bar), xPvaV2Nt8Adapter.ToV2Relation(relation));
        }

        private bool ResolveBounds(out int start, out int end)
        {
            return xPvaV2Nt8Adapter.ResolveBounds(Debug, StartBar, EndBar, CurrentBar, out start, out end);
        }

        private xPvaBarFacts FactsAt(int bar)
        {
            return new xPvaBarFacts(
                bar,
                Times[0].GetValueAt(bar),
                Open.GetValueAt(bar),
                High.GetValueAt(bar),
                Low.GetValueAt(bar),
                Close.GetValueAt(bar),
                Volume.GetValueAt(bar),
                TickSize);
        }

        private void PrintModelSummary()
        {
            if (engine == null)
                return;

            IList<xPvaV2Container> containers = engine.Model.ContainersByStart();
            int activeCount = 0;
            int frozenCount = 0;
            int joinedCount = 0;
            int inactiveCount = 0;
            for (int i = 0; i < containers.Count; i++)
            {
                switch (containers[i].Status)
                {
                    case xPvaV2ContainerStatus.Active:
                    case xPvaV2ContainerStatus.Adjusted:
                        activeCount++;
                        break;
                    case xPvaV2ContainerStatus.Frozen:
                        frozenCount++;
                        break;
                    case xPvaV2ContainerStatus.Joined:
                        joinedCount++;
                        break;
                    default:
                        inactiveCount++;
                        break;
                }
            }

            int relationshipCount = 0;
            foreach (xPvaV2Relationship relationship in engine.Model.Relationships)
                relationshipCount++;

            Print("[APVA V2] model containers=" + containers.Count
                + " active=" + activeCount
                + " frozen=" + frozenCount
                + " joined=" + joinedCount
                + " inactive=" + inactiveCount
                + " relationships=" + relationshipCount
                + " renderSegments=" + engine.RenderSnapshot.Count);

            for (int i = 0; i < containers.Count; i++)
                Print("[APVA V2][CONTAINER] " + FormatContainer(containers[i]));

            foreach (xPvaV2Relationship relationship in engine.Model.Relationships)
                Print("[APVA V2][REL] " + relationship.Kind
                    + " " + relationship.SourceContainerId
                    + "->" + relationship.TargetContainerId
                    + " point=" + relationship.SourcePoint
                    + " bar=" + relationship.Bar
                    + " price=" + relationship.Price);
        }

        private string FormatContainer(xPvaV2Container container)
        {
            var text = new StringBuilder();
            text.Append("id=").Append(container.Id);
            text.Append(" ").Append(container.Direction);
            text.Append(" ").Append(container.StartBar).Append("-").Append(container.EndBar);
            text.Append(" status=").Append(container.Status);
            text.Append(" VL=").Append(container.VisualLevel);
            text.Append(" SL=").Append(container.StructuralLevel);
            text.Append(" P1=").Append(FormatPoint(container.P1));
            text.Append(" P2=").Append(FormatPoint(container.P2));
            text.Append(" P3=").Append(FormatPoint(container.P3));
            text.Append(" origin=").Append(container.OriginKind);
            if (container.OriginContainerId != 0)
            {
                text.Append("@").Append(container.OriginContainerId);
                text.Append(":").Append(container.OriginPoint);
                text.Append(":").Append(container.OriginBar);
            }
            AppendIds(text, " children=", container.ContainmentChildIds);
            AppendIds(text, " components=", container.JoinComponentIds);
            return text.ToString();
        }

        private static string FormatPoint(xPvaV2PricePoint point)
        {
            return point.Bar + "@" + point.Price;
        }

        private static void AppendIds(StringBuilder text, string label, IList<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return;

            text.Append(label);
            for (int i = 0; i < ids.Count; i++)
            {
                if (i > 0)
                    text.Append(",");
                text.Append(ids[i]);
            }
        }

        private void DisposeRenderResources()
        {
            if (upBrushDx != null)
            {
                upBrushDx.Dispose();
                upBrushDx = null;
            }
            if (downBrushDx != null)
            {
                downBrushDx.Dispose();
                downBrushDx = null;
            }
            if (dashStrokeStyle != null)
            {
                dashStrokeStyle.Dispose();
                dashStrokeStyle = null;
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaAutomatedContainersV2[] cachexPvaAutomatedContainersV2;
		public xPvaAutomatedContainersV2 xPvaAutomatedContainersV2(bool debug, int startBar, int endBar)
		{
			return xPvaAutomatedContainersV2(Input, debug, startBar, endBar);
		}

		public xPvaAutomatedContainersV2 xPvaAutomatedContainersV2(ISeries<double> input, bool debug, int startBar, int endBar)
		{
			if (cachexPvaAutomatedContainersV2 != null)
				for (int idx = 0; idx < cachexPvaAutomatedContainersV2.Length; idx++)
					if (cachexPvaAutomatedContainersV2[idx] != null && cachexPvaAutomatedContainersV2[idx].Debug == debug && cachexPvaAutomatedContainersV2[idx].StartBar == startBar && cachexPvaAutomatedContainersV2[idx].EndBar == endBar && cachexPvaAutomatedContainersV2[idx].EqualsInput(input))
						return cachexPvaAutomatedContainersV2[idx];
			return CacheIndicator<xPvaAutomatedContainersV2>(new xPvaAutomatedContainersV2(){ Debug = debug, StartBar = startBar, EndBar = endBar }, input, ref cachexPvaAutomatedContainersV2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaAutomatedContainersV2 xPvaAutomatedContainersV2(bool debug, int startBar, int endBar)
		{
			return indicator.xPvaAutomatedContainersV2(Input, debug, startBar, endBar);
		}

		public Indicators.xPvaAutomatedContainersV2 xPvaAutomatedContainersV2(ISeries<double> input , bool debug, int startBar, int endBar)
		{
			return indicator.xPvaAutomatedContainersV2(input, debug, startBar, endBar);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaAutomatedContainersV2 xPvaAutomatedContainersV2(bool debug, int startBar, int endBar)
		{
			return indicator.xPvaAutomatedContainersV2(Input, debug, startBar, endBar);
		}

		public Indicators.xPvaAutomatedContainersV2 xPvaAutomatedContainersV2(ISeries<double> input , bool debug, int startBar, int endBar)
		{
			return indicator.xPvaAutomatedContainersV2(input, debug, startBar, endBar);
		}
	}
}

#endregion
