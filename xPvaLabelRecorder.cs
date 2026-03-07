#region Using declarations
using System;
using System.Windows.Input;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.xPva;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xPvaLabelRecorder : Indicator
    {
        private xPvaLabelStore store;
		
		// ---- anchor capture state ----
		private bool armStart;
		private bool armEnd;
		
		private int pendingStartBar = -1;
		private double pendingStartPrice;
		
		private int pendingEndBar = -1;
		private double pendingEndPrice;
		
		// active label being built
		private xPvaLabel activeLabel;
		
		private bool hooksAttached;
		private ChartScale lastChartScale;
		private ChartControl lastChartControl;
		
		private DateTime pendingStartTimeUtc;
		private DateTime pendingEndTimeUtc;
		
		private DateTime pendingStartChartTime;
		private DateTime pendingEndChartTime;
		
		private int pendingStartCurrentBar = -1;
		private int pendingEndCurrentBar   = -1;

		private ChartControl hookedChartControl;
		
		private DateTime lastExportTradingDay = Core.Globals.MinDate;

        [NinjaScriptProperty]
        [Display(Name="ExportFolder", Order=1, GroupName="xPva")]
        public string ExportFolder { get; set; } = @"C:\temp\xPvaLabels";

        [NinjaScriptProperty]
        [Display(Name="AutoExportOnSessionEnd", Order=2, GroupName="xPva")]
        public bool AutoExportOnSessionEnd { get; set; } = true;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaLabelRecorder";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
				AutoExportOnSessionEnd = false;
            }
            else if (State == State.DataLoaded)
            {
                store = new xPvaLabelStore();
            }
			else if (State == State.Terminated)
			{
			   RemoveHooks();
			}
        }

        protected override void OnBarUpdate()
		{
		    if (CurrentBar < 5)
		        return;
		
		    if (!AutoExportOnSessionEnd)
		        return;
		
		    if (!Bars.IsFirstBarOfSession)
		        return;
		
		    // Determine trading day from current bar
		    DateTime tradingDay = Times[0][0].Date;
		
		    // Only export once per trading day
		    if (tradingDay == lastExportTradingDay)
		        return;
		
		    if (store?.Labels?.Count > 0)
		    {
		        ExportDataset();
		        lastExportTradingDay = tradingDay;
		    }
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
		    base.OnRender(chartControl, chartScale);
		    lastChartControl = chartControl;
		    lastChartScale   = chartScale;
			
			EnsureHooks();
		}

		private void OnChartPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
		    if (armStart || armEnd)
		        e.Handled = true;
		}

		private void OnChartPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
		    try
		    {
		        // If we're not armed, let NT handle right-click normally (context menu, etc.)
		        if (!armStart && !armEnd)
		            return;
		
		        // We ARE armed -> swallow the event immediately so the context menu won't appear
		        e.Handled = true;
		
		        // Resolve chart control / scale / bars
		        var cc = sender as ChartControl ?? lastChartControl ?? ChartControl;
		        if (cc == null)
		        {
		            Print("xPva right-click: ChartControl null");
		            return;
		        }
		
		        if (lastChartScale == null)
		        {
		            Print("xPva right-click: lastChartScale null");
		            return;
		        }
		
		        if (ChartBars == null)
		        {
		            Print("xPva right-click: ChartBars null");
		            return;
		        }
		
		        if (Bars == null || Bars.Count <= 0)
		        {
		            Print("xPva right-click: Bars unavailable");
		            return;
		        }
		
		        // Mouse location
		        var mousePt = e.GetPosition(cc);
		
		        // X -> chart bar index
		        int chartBarIndex = ChartBars.GetBarIdxByX(cc, (int)mousePt.X);
		        if (chartBarIndex < 0 || chartBarIndex >= ChartBars.Count)
		        {
		            Print($"xPva right-click: chartBarIndex out of range ({chartBarIndex}) ChartBars.Count={ChartBars.Count}");
		            return;
		        }
		
		        // chart bar index -> chart time
		        DateTime chartTime = ChartBars.GetTimeByBarIdx(cc, chartBarIndex);
		
		        // chart time -> UTC (Phoenix)
		        DateTime timeUtc = ToUtcFromChartTz(chartTime, "US Mountain Standard Time");
				
				
		
		        // chart time -> primary series bar index
		        int seriesBarIndex = Bars.GetBar(chartTime);
		        if (seriesBarIndex < 0 || seriesBarIndex >= Bars.Count)
		        {
		            Print($"xPva right-click: seriesBarIndex invalid ({seriesBarIndex}) Bars.Count={Bars.Count} chartTime={chartTime:O}");
		            return;
		        }
		
		        // seriesBarIndex -> barsAgo
		        int barsAgo = CurrentBar - seriesBarIndex;
		        if (barsAgo < 0 || barsAgo >= Bars.Count)
		        {
		            Print($"xPva right-click: barsAgo invalid ({barsAgo}) CurrentBar={CurrentBar} seriesBarIndex={seriesBarIndex} Bars.Count={Bars.Count}");
		            return;
		        }
		
		        // Y -> price
		        double price = lastChartScale.GetValueByY((float)mousePt.Y);
		
		        // Store the capture
		        if (armStart)
				{
				    pendingStartBar         = seriesBarIndex;
				    pendingStartPrice       = price;
				    pendingStartChartTime   = chartTime;   // ← ADD THIS
				    pendingStartTimeUtc     = timeUtc;
					pendingStartCurrentBar  = CurrentBar;
				    armStart = false;
				
				    Print($"xPva Start captured: seriesBar={seriesBarIndex}, barsAgo={barsAgo}, price={price}, chart={chartTime:O}, utc={timeUtc:O}");
				}
				else if (armEnd)
				{
				    pendingEndBar           = seriesBarIndex;
				    pendingEndPrice         = price;
				    pendingEndChartTime     = chartTime;   // ← ADD THIS
				    pendingEndTimeUtc       = timeUtc;
					pendingEndCurrentBar    = CurrentBar;
				    armEnd = false;
				
				    Print($"xPva End captured: seriesBar={seriesBarIndex}, barsAgo={barsAgo}, price={price}, chart={chartTime:O}, utc={timeUtc:O}");
				}

		    }
		    catch (Exception ex)
		    {
		        // Keep event swallowed while armed, even on error
		        e.Handled = true;
		        Print("xPva right-click exception: " + ex);
		    }
		}



		private void OnChartPreviewKeyDown(object sender, KeyEventArgs e)
		{
		    try
		    {
		        ModifierKeys mods = Keyboard.Modifiers;
		
		        bool ctrl = (mods & ModifierKeys.Control) == ModifierKeys.Control;
		        bool alt  = (mods & ModifierKeys.Alt) == ModifierKeys.Alt;
		
		        // Require Ctrl + Alt
		        if (!ctrl || !alt)
		            return;
		
		        if (e.Key == Key.S)
		        {
		            ArmStartAnchor();
		            e.Handled = true;
		            return;
		        }
		
		        if (e.Key == Key.E)
		        {
		            ArmEndAnchor();
		            e.Handled = true;
		            return;
		        }
		
		        if (e.Key == Key.F)
		        {
		            bool ok = SaveActiveLabel(
		                xPvaLabelType.BBT,
		                xPvaDirection.Up,
		                xPvaFractal.L0,
		                finalize: true);
		
		            if (ok)
		                ExportDataset();
		
		            e.Handled = true;
		            return;
		        }
				
				if (e.Key == Key.C)
				{
				    armStart = armEnd = false;

					pendingStartBar = pendingEndBar = -1;
					pendingStartCurrentBar = pendingEndCurrentBar = -1;
					
					pendingStartPrice = pendingEndPrice = 0;
					pendingStartTimeUtc = pendingEndTimeUtc = default(DateTime);
					pendingStartChartTime = pendingEndChartTime = default(DateTime);
					
					Print("xPva: capture cancelled");
					e.Handled = true;
					return;
				}

		    }
		    catch (Exception ex)
		    {
		        Print("xPva key exception: " + ex);
		    }
		}
		
		private void EnsureHooks()
		{
		    if (ChartControl == null)
		        return;
		
		    // If chart control changed, detach from the old one and reattach to the new one
		    if (hooksAttached && hookedChartControl != null && !ReferenceEquals(hookedChartControl, ChartControl))
		    {
		        RemoveHooks(); // detaches from old
		    }
		
		    if (hooksAttached)
		        return;
		
		    hookedChartControl = ChartControl;
		
		    hookedChartControl.PreviewKeyDown              -= OnChartPreviewKeyDown;
		    //hookedChartControl.PreviewMouseLeftButtonDown  -= OnChartPreviewMouseLeftButtonDown;
		    hookedChartControl.PreviewMouseRightButtonDown -= OnChartPreviewMouseRightButtonDown;
		    hookedChartControl.PreviewMouseRightButtonUp   -= OnChartPreviewMouseRightButtonUp;
		
		    hookedChartControl.PreviewKeyDown              += OnChartPreviewKeyDown;
		    //hookedChartControl.PreviewMouseLeftButtonDown  += OnChartPreviewMouseLeftButtonDown;
		    hookedChartControl.PreviewMouseRightButtonDown += OnChartPreviewMouseRightButtonDown;
		    hookedChartControl.PreviewMouseRightButtonUp   += OnChartPreviewMouseRightButtonUp;
		
		    hooksAttached = true;
		    Print("xPva: hooks attached");
		}

		private void RemoveHooks()
		{
		    if (!hooksAttached)
		        return;
		
		    if (hookedChartControl != null)
		    {
		        hookedChartControl.PreviewKeyDown              -= OnChartPreviewKeyDown;
		        //hookedChartControl.PreviewMouseLeftButtonDown  -= OnChartPreviewMouseLeftButtonDown;
		        hookedChartControl.PreviewMouseRightButtonDown -= OnChartPreviewMouseRightButtonDown;
		        hookedChartControl.PreviewMouseRightButtonUp   -= OnChartPreviewMouseRightButtonUp;
		    }
		
		    hooksAttached = false;
		    hookedChartControl = null;
		    Print("xPva: hooks removed");
		}

		public void ArmStartAnchor()
		{
		    if (lastChartControl == null || lastChartScale == null || ChartBars == null)
		    {
		        Print("xPva: cannot arm START (chart not ready yet). Click the chart once and try again.");
		        return;
		    }
		
		    armStart = true;
		    armEnd = false;
		    Print("xPva: Arm START anchor");
		}
		
		public void ArmEndAnchor()
		{
		    if (lastChartControl == null || lastChartScale == null || ChartBars == null)
		    {
		        Print("xPva: cannot arm END (chart not ready yet). Click the chart once and try again.");
		        return;
		    }
		
		    armEnd = true;
		    armStart = false;
		    Print("xPva: Arm END anchor");
		}
		
		public bool SaveActiveLabel(xPvaLabelType type, 
			xPvaDirection direction, xPvaFractal fractal,bool finalize)
		{
		    if (pendingStartBar < 0 || pendingEndBar < 0)
		    {
		        Print("xPva: Cannot save label – missing anchors");
		        return false;
		    }
			
			if (pendingStartCurrentBar < 0 || pendingEndCurrentBar < 0)
			{
			    Print("xPva: Cannot save label – capture currentBar not set");
			    return false;
			}

		
		    activeLabel = new xPvaLabel
		    {
		        Type      = type,
		        Direction = direction,
		        Fractal   = fractal,
		        Text      = "manual"
		    };
		
		    // CreatedAt
		    activeLabel.CreatedAt = new xPvaAt
			{
			    BarIndex = CurrentBar,
			    BarsAgo  = 0,
			    TimeUtc  = DateTime.UtcNow
			};
		
		    // Start anchor
		    activeLabel.Anchors.Add(new xPvaAnchor
		    {
		        Role     = xPvaAnchorRole.Start,
		        BarIndex = pendingStartBar,
		        BarsAgo  = pendingStartCurrentBar - pendingStartBar,
				ChartTime = pendingStartChartTime,
		        TimeUtc  = pendingStartTimeUtc,
		        Price    = pendingStartPrice
		    });
		
		    // End anchor
			activeLabel.Anchors.Add(new xPvaAnchor
			{
			    Role     = xPvaAnchorRole.End,
			    BarIndex = pendingEndBar,
			    BarsAgo  = pendingEndCurrentBar - pendingEndBar,
				ChartTime = pendingEndChartTime, 
			    TimeUtc  = pendingEndTimeUtc,
			    Price    = pendingEndPrice
			});
		
		    activeLabel.Tags.Add("online");
			
			if (direction == xPvaDirection.Up && pendingEndPrice < pendingStartPrice)
		        activeLabel.Tags.Add("dir_mismatch");
		    else if (direction == xPvaDirection.Down && pendingEndPrice > pendingStartPrice)
		        activeLabel.Tags.Add("dir_mismatch");
		
		    store.Add(activeLabel);
		
		    if (finalize)
    			store.Finalize(activeLabel.Id, CurrentBar, DateTime.UtcNow);
		
		    Print($"xPva label saved ({type})");
		
		    // reset
		    // reset all pending state
			pendingStartBar = pendingEndBar = -1;
			pendingStartPrice = pendingEndPrice = 0;
			
			pendingStartTimeUtc = default(DateTime);
			pendingEndTimeUtc   = default(DateTime);
			
			pendingStartChartTime = default;
			pendingEndChartTime   = default;
			
			pendingStartCurrentBar = -1;
			pendingEndCurrentBar   = -1;
			
			armStart = false;
			armEnd   = false;


			return true;
		}

		private static DateTime ToUtcFromChartTz(DateTime chartTime, string windowsTzId)
		{
		    var unspecified = DateTime.SpecifyKind(chartTime, DateTimeKind.Unspecified);
		    var tz = TimeZoneInfo.FindSystemTimeZoneById(windowsTzId);
		    return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
		}

        [Browsable(false)]
        public xPvaLabelStore LabelStore => store;

        public void ExportDataset()
		{
		    try
		    {
				Directory.CreateDirectory(ExportFolder);

				Print("xPva ExportDataset marker: 2025-12-29 A");
				
		        if (store == null || store.Labels == null || store.Labels.Count == 0)
		        {
		            Print("xPva export skipped: no labels");
		            return;
		        }
		
		        var ds = new xPvaDataset();
		
		        ds.Instrument.Name = Instrument != null ? xPvaSession.SafeInstrumentName(Instrument) : "Unknown";
		        ds.Instrument.Master = Instrument != null ? xPvaSession.SafeMasterInstrumentName(Instrument) : "Unknown";
		
		        ds.Bars.Type = BarsPeriod != null ? BarsPeriod.BarsPeriodType.ToString() : "Unknown";
		        ds.Bars.Value = BarsPeriod != null ? BarsPeriod.Value : 0;
		
		        ds.ChartTimeZone = "US Mountain Standard Time";
		
		        ds.Session.EthEnabled = true;
		        ds.Session.RthTemplate = "RTH";
		        ds.Session.EthTemplate = "ETH";
		
		        ds.Labels.AddRange(store.Labels);
		
		        var fileName =
		            $"{ds.Instrument.Master}_{ds.Bars.Type}_{ds.Bars.Value}_{lastExportTradingDay:yyyyMMdd}_UTC.json";
		
		        var fullPath = System.IO.Path.Combine(ExportFolder, fileName);
		
		        var json = xPvaJson.ToJson(ds);
		        xPvaJson.WriteUtf8File(fullPath, json);
		
		        Print($"xPva export OK: {fullPath}");
		    }
		    catch (Exception ex)
		    {
		        Print("xPva export error: " + ex);
		    }
		}

    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xPvaLabelRecorder[] cachexPvaLabelRecorder;
		public xPvaLabelRecorder xPvaLabelRecorder(string exportFolder, bool autoExportOnSessionEnd)
		{
			return xPvaLabelRecorder(Input, exportFolder, autoExportOnSessionEnd);
		}

		public xPvaLabelRecorder xPvaLabelRecorder(ISeries<double> input, string exportFolder, bool autoExportOnSessionEnd)
		{
			if (cachexPvaLabelRecorder != null)
				for (int idx = 0; idx < cachexPvaLabelRecorder.Length; idx++)
					if (cachexPvaLabelRecorder[idx] != null && cachexPvaLabelRecorder[idx].ExportFolder == exportFolder && cachexPvaLabelRecorder[idx].AutoExportOnSessionEnd == autoExportOnSessionEnd && cachexPvaLabelRecorder[idx].EqualsInput(input))
						return cachexPvaLabelRecorder[idx];
			return CacheIndicator<xPvaLabelRecorder>(new xPvaLabelRecorder(){ ExportFolder = exportFolder, AutoExportOnSessionEnd = autoExportOnSessionEnd }, input, ref cachexPvaLabelRecorder);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPvaLabelRecorder xPvaLabelRecorder(string exportFolder, bool autoExportOnSessionEnd)
		{
			return indicator.xPvaLabelRecorder(Input, exportFolder, autoExportOnSessionEnd);
		}

		public Indicators.xPvaLabelRecorder xPvaLabelRecorder(ISeries<double> input , string exportFolder, bool autoExportOnSessionEnd)
		{
			return indicator.xPvaLabelRecorder(input, exportFolder, autoExportOnSessionEnd);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPvaLabelRecorder xPvaLabelRecorder(string exportFolder, bool autoExportOnSessionEnd)
		{
			return indicator.xPvaLabelRecorder(Input, exportFolder, autoExportOnSessionEnd);
		}

		public Indicators.xPvaLabelRecorder xPvaLabelRecorder(ISeries<double> input , string exportFolder, bool autoExportOnSessionEnd)
		{
			return indicator.xPvaLabelRecorder(input, exportFolder, autoExportOnSessionEnd);
		}
	}
}

#endregion
