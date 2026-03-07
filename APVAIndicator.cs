#region Using declarations
using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
    /// Visual adapter over APVA.Core: feeds bars into APVAEngine and draws
    /// Tape / Traverse / Channel RTL/LTL + markers using Draw.Line / Draw.Text.
    /// </summary>
	public class APVAIndicator : Indicator
	{
		// ---- Inputs ----
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Render Window (bars)", GroupName = "APVA", Order = 0)]
        public int RenderWindow { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Epsilon", GroupName = "APVA", Order = 1)]
        public double Epsilon { get; set; }

        // ---- Core ----
        private APVA.Core.APVAEngine engine;
        private APVA.Core.APVARenderComposer painter;

        // Simple bar cache so engine can query by index
        private readonly Dictionary<int, APVA.Core.Bar> barCache = new Dictionary<int, APVA.Core.Bar>();

        // Reusable brushes cache by hex
        private readonly Dictionary<string, Brush> brushCache = new Dictionary<string, Brush>(StringComparer.OrdinalIgnoreCase);

        // Tag prefixes to keep Draw.* ids stable
        private const string TagPrefixTape     = "APVA_TAPE_";
        private const string TagPrefixTraverse = "APVA_TRAV_";
        private const string TagPrefixChannel  = "APVA_CHAN_";
        private const string TagPrefixMk       = "APVA_MK_";
		
		private SimpleFont labelFont;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name                    = "APVA_Indicator";
                Calculate               = Calculate.OnBarClose; // change to OnEachTick if desired
                IsOverlay               = true;
                IsSuspendedWhileInactive= true;

                RenderWindow = 300;
                Epsilon      = 0.00001;
            }
            else if (State == State.Configure)
            {
                // nothing
            }
            else if (State == State.DataLoaded)
            {
                // Accessor for engine to fetch bars by absolute index
                Func<int, APVA.Core.Bar> getBar = idx =>
                {
                    APVA.Core.Bar b;
                    return barCache.TryGetValue(idx, out b) ? b : null;
                };

                engine  = new APVA.Core.APVAEngine(Epsilon, getBar);
                painter = new APVA.Core.APVARenderComposer(engine, 0, 0);
				labelFont = new SimpleFont("Segoe UI", 11); // pick any family/size you like
            }
        }

        protected override void OnBarUpdate()
        {
            // Build APVA.Core.Bar from Ninja data
            var b = new APVA.Core.Bar
            {
                Index  = CurrentBar,
                Time   = Time[0],
                Open   = Open[0],
                High   = High[0],
                Low    = Low[0],
                Close  = Close[0],
                Volume = (long) Volume[0]
            };
            barCache[CurrentBar] = b;

            // Feed engine (sequential)
            engine.OnBar(b);

            // Update painter window to [CurrentBar-RenderWindow+1 .. CurrentBar]
            int start = Math.Max(0, CurrentBar - RenderWindow + 1);
            painter.SetWindow(start, CurrentBar);

            // Fetch snapshot and draw
            var snap = painter.GetSnapshot();
            DrawContainerSpec(snap.Tape,     TagPrefixTape);
            DrawContainerSpec(snap.Traverse, TagPrefixTraverse);
            DrawContainerSpec(snap.Channel,  TagPrefixChannel);
        }

        // --------- Drawing helpers ---------

        private void DrawContainerSpec(APVA.Core.ContainerRenderSpec spec, string tagPrefix)
        {
            if (spec == null) return;

            // Lines
            if (spec.RTL != null) DrawLineSpec(spec.RTL, tagPrefix + "RTL_");
            if (spec.LTL != null) DrawLineSpec(spec.LTL, tagPrefix + "LTL_");

            // VE lines (same style as container, already handled by composer)
            if (spec.VeLines != null)
            {
                int i = 0;
                foreach (var ve in spec.VeLines)
                {
                    DrawLineSpec(ve, tagPrefix + "VE_" + i.ToString(CultureInfo.InvariantCulture) + "_");
                    i++;
                }
            }

            // Markers
            if (spec.Markers != null)
            {
                foreach (var mk in spec.Markers)
                    DrawMarkerSpec(mk, tagPrefix + TagPrefixMk);
            }
        }

        private void DrawLineSpec(APVA.Core.LineSpec line, string tagPrefix)
        {
            if (line == null) return;

            // Convert absolute indices to BarsAgo
            int startBarsAgo = CurrentBar - line.X1;
            int endBarsAgo   = CurrentBar - line.X2;

            if (startBarsAgo < 0 && endBarsAgo < 0) return; // fully future; skip
            // clamp to visible history
            startBarsAgo = Math.Max(0, startBarsAgo);
            endBarsAgo   = Math.Max(0, endBarsAgo);

            // NinjaTrader requires unique tag per line; reuse same tag to update
            string tag = tagPrefix + line.Label;

            var brush = BrushFromHex(line.Style?.ColorHex ?? "#808080");
            var dash  = ToDash(line.Style?.Kind ?? APVA.Core.StrokeKind.Solid);
            int width = Math.Max(1, (int)Math.Round(line.Style?.Thickness ?? 1.0));

            Draw.Line(this, tag, false,
                      startBarsAgo, line.Y1,
                      endBarsAgo,   line.Y2,
                      brush, dash, width);
        }

        private void DrawMarkerSpec(APVA.Core.MarkerSpec mk, string tagPrefix)
		{
		    if (mk == null) return;
		    int barsAgo = CurrentBar - mk.Index;
		    if (barsAgo < 0) return;
		
		    string text = mk.Label ?? mk.Type.ToString();
		    string tag  = $"{tagPrefix}{mk.Type}_{mk.Index}";
		
		    double y = double.IsNaN(mk.Price) ? (High[barsAgo] + Low[barsAgo]) * 0.5 : mk.Price;
		    double yOffset = Instrument.MasterInstrument.TickSize * 4;
		
		    var brush = Brushes.White;
		
		    // 6-arg overload: (owner, tag, text, barsAgo, y, textBrush)
		    Draw.Text(this, tag, text, barsAgo, y + yOffset, brush);
		}


        private Brush BrushFromHex(string hex)
        {
            Brush b;
            if (brushCache.TryGetValue(hex, out b)) return b;
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var solid = new SolidColorBrush(color);
                solid.Freeze();
                brushCache[hex] = solid;
                return solid;
            }
            catch
            {
                return Brushes.Gray;
            }
        }

        private DashStyleHelper ToDash(APVA.Core.StrokeKind kind)
        {
            switch (kind)
            {
                case APVA.Core.StrokeKind.Solid:   return DashStyleHelper.Solid;
                case APVA.Core.StrokeKind.Dash:    return DashStyleHelper.Dash;
                case APVA.Core.StrokeKind.Dot:     return DashStyleHelper.Dot;
                case APVA.Core.StrokeKind.DotDash: return DashStyleHelper.DashDot;
            }
            return DashStyleHelper.Solid;
        }
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private APVAIndicator[] cacheAPVAIndicator;
		public APVAIndicator APVAIndicator(int renderWindow, double epsilon)
		{
			return APVAIndicator(Input, renderWindow, epsilon);
		}

		public APVAIndicator APVAIndicator(ISeries<double> input, int renderWindow, double epsilon)
		{
			if (cacheAPVAIndicator != null)
				for (int idx = 0; idx < cacheAPVAIndicator.Length; idx++)
					if (cacheAPVAIndicator[idx] != null && cacheAPVAIndicator[idx].RenderWindow == renderWindow && cacheAPVAIndicator[idx].Epsilon == epsilon && cacheAPVAIndicator[idx].EqualsInput(input))
						return cacheAPVAIndicator[idx];
			return CacheIndicator<APVAIndicator>(new APVAIndicator(){ RenderWindow = renderWindow, Epsilon = epsilon }, input, ref cacheAPVAIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.APVAIndicator APVAIndicator(int renderWindow, double epsilon)
		{
			return indicator.APVAIndicator(Input, renderWindow, epsilon);
		}

		public Indicators.APVAIndicator APVAIndicator(ISeries<double> input , int renderWindow, double epsilon)
		{
			return indicator.APVAIndicator(input, renderWindow, epsilon);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.APVAIndicator APVAIndicator(int renderWindow, double epsilon)
		{
			return indicator.APVAIndicator(Input, renderWindow, epsilon);
		}

		public Indicators.APVAIndicator APVAIndicator(ISeries<double> input , int renderWindow, double epsilon)
		{
			return indicator.APVAIndicator(input, renderWindow, epsilon);
		}
	}
}

#endregion
