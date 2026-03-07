#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations; // [Display]
using System.Windows;
using System.Windows.Input;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
// DX namespaces
using SharpDX;                       // RectangleF
using SharpDX.Direct2D1;            // SolidColorBrush
using SharpDX.DirectWrite;          // TextFormat
#endregion

#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;           // [Display]
using SW = System.Windows;                            // SW.Point
using SWI = System.Windows.Input;                     // SWI.Mouse
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;

// SharpDX aliases to avoid ambiguity with System.Windows + multiple Factory types
using DX  = SharpDX;                  // DX.RectangleF
using D2D = SharpDX.Direct2D1;        // D2D.SolidColorBrush
using DW  = SharpDX.DirectWrite;      // DW.Factory, DW.TextFormat, DW.TextAlignment
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// Pre-click Hi/Low hover box for the price panel.
    /// Draws a small box at the bar high/low when the mouse is close.
    /// Also draws a tiny crosshair at the mouse to prove OnRender updates.
    /// </summary>
    public class PreClickHiLoHover : Indicator
    {
        // DX resources
        private D2D.SolidColorBrush fillBrush;
        private D2D.SolidColorBrush borderBrush;
        private D2D.SolidColorBrush crossBrush;
        private D2D.SolidColorBrush warnBrush;
        private DW.TextFormat       warnTextFormat;

        // mouse tracking via WPF
        private SW.Point _lastMousePanelPt;
        private bool     _hasMouseMoveHook;

        [NinjaScriptProperty, Display(Name = "Box Size (px)", Order = 0, GroupName = "Parameters")]
        public float BoxSize { get; set; }

        [NinjaScriptProperty, Display(Name = "Y Tolerance (px)", Order = 1, GroupName = "Parameters")]
        public float YTolerance { get; set; }

        [NinjaScriptProperty, Display(Name = "X Tolerance (px add-on)", Order = 2, GroupName = "Parameters")]
        public float XToleranceAddon { get; set; }

        [NinjaScriptProperty, Display(Name = "Show Debug Prints", Order = 3, GroupName = "Parameters")]
        public bool DebugPrints { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "PreClickHiLoHover";
                IsOverlay = true;                  // draw over price panel
                Calculate = Calculate.OnEachTick;  // we also invalidate on mouse
                IsSuspendedWhileInactive = true;

                BoxSize = 12f;
                YTolerance = 8f;
                XToleranceAddon = 2f;
                DebugPrints = false;
            }
            else if (State == State.Terminated)
            {
                UnhookMouse();
                DisposeDx();
            }
        }

        private void HookMouse()
        {
            if (_hasMouseMoveHook || ChartPanel == null) return;
            try
            {
                ChartPanel.MouseMove += ChartPanel_MouseMove;
                _hasMouseMoveHook = true;
                if (DebugPrints) Print("PreClickHiLoHover: hooked ChartPanel.MouseMove");
            }
            catch { /* ignore */ }
        }

        private void UnhookMouse()
        {
            if (!_hasMouseMoveHook) return;
            try
            {
                if (ChartPanel != null)
                    ChartPanel.MouseMove -= ChartPanel_MouseMove;
            }
            catch { /* ignore */ }
            _hasMouseMoveHook = false;
        }

        private void ChartPanel_MouseMove(object sender, SWI.MouseEventArgs e)
        {
            if (ChartPanel == null || ChartControl == null) return;
            _lastMousePanelPt = e.GetPosition(ChartPanel);
            ChartControl.InvalidateVisual(); // refresh on mouse move (no ticks needed)
        }

        public override void OnRenderTargetChanged()
        {
            DisposeDx();
            if (RenderTarget != null)
            {
                fillBrush   = new D2D.SolidColorBrush(RenderTarget, new SharpDX.Color(0, 120, 215, 180)); // blue semi
                borderBrush = new D2D.SolidColorBrush(RenderTarget, new SharpDX.Color(0,  80, 180, 255)); // blue border
                crossBrush  = new D2D.SolidColorBrush(RenderTarget, new SharpDX.Color(255,255,255,160));  // white crosshair
                warnBrush   = new D2D.SolidColorBrush(RenderTarget, new SharpDX.Color(255, 80,  80, 240)); // red text

                using (var dwFactory = new DW.Factory())
                {
                    warnTextFormat = new DW.TextFormat(dwFactory, "Segoe UI", 14f)
                    {
                        TextAlignment      = DW.TextAlignment.Leading,      // disambiguated
                        ParagraphAlignment = DW.ParagraphAlignment.Near
                    };
                }
            }
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (chartControl == null || chartScale == null || ChartPanel == null || RenderTarget == null)
                return;

            // Ensure mouse hook is installed (lazy hook here is safe)
            if (!_hasMouseMoveHook) HookMouse();

            // Mouse point (panel-relative). If we haven’t seen a move yet, query on demand.
            SW.Point mousePtPanel = (_lastMousePanelPt.X != 0 || _lastMousePanelPt.Y != 0)
                                    ? _lastMousePanelPt
                                    : SWI.Mouse.GetPosition(ChartPanel);
            int      mouseAbsX = (int)(mousePtPanel.X + ChartPanel.X);

            // Draw a tiny crosshair at the mouse so we know this renders
            DrawCrosshair(mouseAbsX, (float)mousePtPanel.Y, 5f);

            // Map current render panel -> Bars in that panel
            var bars = GetBarsForThisPanel(chartControl, chartScale.PanelIndex);
            if (bars == null || bars.Count == 0)
            {
                DrawWarning("Add PreClickHiLoHover to the PRICE panel.", 8f, 8f);
                if (DebugPrints) Print($"No Bars found for panel {chartScale.PanelIndex} – nothing to hover.");
                return;
            }

            int idx = bars.GetBarIdxByX(chartControl, mouseAbsX);
            if (idx < 0 || idx >= bars.Count)
                return;

            double barAbsX = chartControl.GetXByBarIndex(bars, idx); // absolute X
            double hi      = bars.Bars.GetHigh(idx);
            double lo      = bars.Bars.GetLow(idx);
            double hiY     = chartScale.GetYByValue(hi);             // Y in THIS panel
            double loY     = chartScale.GetYByValue(lo);

            double tolX = Math.Max(6, chartControl.BarWidth + XToleranceAddon);
            SW.Point? hit = null;

            if (Math.Abs(mouseAbsX - barAbsX) <= tolX)
            {
                if (Math.Abs(mousePtPanel.Y - hiY) <= YTolerance)
                    hit = new SW.Point(barAbsX, hiY);
                else if (Math.Abs(mousePtPanel.Y - loY) <= YTolerance)
                    hit = new SW.Point(barAbsX, loY);
            }

            if (!hit.HasValue)
                return;

            var rect = new DX.RectangleF(
                (float)(hit.Value.X - BoxSize / 2f),
                (float)(hit.Value.Y - BoxSize / 2f),
                BoxSize, BoxSize);

            RenderTarget.FillRectangle(rect, fillBrush);
            RenderTarget.DrawRectangle(rect, borderBrush, 1f);

            if (DebugPrints)
                Print($"Hover hit at idx={idx} X={rect.X:F1} Y={rect.Y:F1} (hiY={hiY:F1} loY={loY:F1})");
        }

        // Find a ChartBars actually attached to THIS panel
        private ChartBars GetBarsForThisPanel(ChartControl cc, int panelIndex)
        {
            if (cc?.BarsArray == null) return null;

            for (int i = 0; i < cc.BarsArray.Count; i++)
            {
                var b = cc.BarsArray[i];
                if (b?.ChartPanel != null && b.ChartPanel.PanelIndex == panelIndex)
                    return b;
            }
            // No series in this panel
            return null;
        }

        private void DrawCrosshair(double absX, float y, float half)
        {
            if (crossBrush == null) return;
            // Vertical line
            var vRect = new DX.RectangleF((float)absX - 0.5f, y - half, 1f, half * 2);
            RenderTarget.FillRectangle(vRect, crossBrush);
            // Horizontal line
            var hRect = new DX.RectangleF((float)absX - half, y - 0.5f, half * 2, 1f);
            RenderTarget.FillRectangle(hRect, crossBrush);
        }

        private void DrawWarning(string text, float x, float y)
        {
            if (warnBrush == null || warnTextFormat == null) return;
            var layout = new DX.RectangleF(x, y, x + 5000f, y + 100f);
            RenderTarget.DrawText(text, warnTextFormat, layout, warnBrush);
        }

        private void DisposeDx()
        {
            if (fillBrush      != null) { fillBrush.Dispose();      fillBrush = null; }
            if (borderBrush    != null) { borderBrush.Dispose();    borderBrush = null; }
            if (crossBrush     != null) { crossBrush.Dispose();     crossBrush = null; }
            if (warnBrush      != null) { warnBrush.Dispose();      warnBrush = null; }
            if (warnTextFormat != null) { warnTextFormat.Dispose(); warnTextFormat = null; }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PreClickHiLoHover[] cachePreClickHiLoHover;
		public PreClickHiLoHover PreClickHiLoHover(float boxSize, float yTolerance, float xToleranceAddon, bool debugPrints)
		{
			return PreClickHiLoHover(Input, boxSize, yTolerance, xToleranceAddon, debugPrints);
		}

		public PreClickHiLoHover PreClickHiLoHover(ISeries<double> input, float boxSize, float yTolerance, float xToleranceAddon, bool debugPrints)
		{
			if (cachePreClickHiLoHover != null)
				for (int idx = 0; idx < cachePreClickHiLoHover.Length; idx++)
					if (cachePreClickHiLoHover[idx] != null && cachePreClickHiLoHover[idx].BoxSize == boxSize && cachePreClickHiLoHover[idx].YTolerance == yTolerance && cachePreClickHiLoHover[idx].XToleranceAddon == xToleranceAddon && cachePreClickHiLoHover[idx].DebugPrints == debugPrints && cachePreClickHiLoHover[idx].EqualsInput(input))
						return cachePreClickHiLoHover[idx];
			return CacheIndicator<PreClickHiLoHover>(new PreClickHiLoHover(){ BoxSize = boxSize, YTolerance = yTolerance, XToleranceAddon = xToleranceAddon, DebugPrints = debugPrints }, input, ref cachePreClickHiLoHover);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PreClickHiLoHover PreClickHiLoHover(float boxSize, float yTolerance, float xToleranceAddon, bool debugPrints)
		{
			return indicator.PreClickHiLoHover(Input, boxSize, yTolerance, xToleranceAddon, debugPrints);
		}

		public Indicators.PreClickHiLoHover PreClickHiLoHover(ISeries<double> input , float boxSize, float yTolerance, float xToleranceAddon, bool debugPrints)
		{
			return indicator.PreClickHiLoHover(input, boxSize, yTolerance, xToleranceAddon, debugPrints);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PreClickHiLoHover PreClickHiLoHover(float boxSize, float yTolerance, float xToleranceAddon, bool debugPrints)
		{
			return indicator.PreClickHiLoHover(Input, boxSize, yTolerance, xToleranceAddon, debugPrints);
		}

		public Indicators.PreClickHiLoHover PreClickHiLoHover(ISeries<double> input , float boxSize, float yTolerance, float xToleranceAddon, bool debugPrints)
		{
			return indicator.PreClickHiLoHover(input, boxSize, yTolerance, xToleranceAddon, debugPrints);
		}
	}
}

#endregion
