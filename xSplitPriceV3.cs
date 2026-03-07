#region Using declarations
using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;

#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
    public class Price
    {
        public double Open;
        public double High;
        public double Low;
        public double Close;
    }
	public class xSplitPriceV3 : Indicator
	{
        public Series<Price>[] DynamicPlots;
        public Series<int> NonDomBarColor;

        private System.Windows.Media.Brush[] PlotRedBlackWMBrushes;
        private SharpDX.Direct2D1.Brush[] PlotRedBlackDXBrushes;
        private System.Windows.Media.Brush[] PlotBlackRedWMBrushes;
        private SharpDX.Direct2D1.Brush[] PlotBlackRedDXBrushes;
        private SharpDX.Direct2D1.SolidColorBrush customDXBrush;
        private bool[] PlotRedBlackBrushNeedsUpdate;
        private bool[] PlotBlackRedBrushNeedsUpdate;
        private const int COLOR_RED = 0;
        private const int COLOR_BLACK = 1;
        private const int NON_DOMINANT = 0;
        private const int DOMINANT = 1;
        private const int NDVOLUME = 0;
        private const int DVOLUME = 1;
        private const int NDVCOLOR = 2;

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Jack Hershey/Todd Billis split price indicator.";
				Name										= "xSplitPriceV3";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
                BarsRequiredToPlot                          = 2;

                UseBarWidth                                 = false;
                BarDistanceToUse                            = 0.9f;
                ShowTransparentPlotsInDataBox               = true;

                AddPlot(Brushes.Transparent, "DominantOpen");            //Plots[0]
                AddPlot(Brushes.Transparent, "DominantHigh");          //Plots[1]
                AddPlot(Brushes.Transparent, "DominantLow");          //Plots[2]
                AddPlot(Brushes.Transparent, "DominantClose");          //Plots[3]

                AddPlot(Brushes.Transparent, "NonDominantOpen");            //Plots[4]
                AddPlot(Brushes.Transparent, "NonDominantHigh");          //Plots[5]
                AddPlot(Brushes.Transparent, "NonDominantLow");          //Plots[6]
                AddPlot(Brushes.Transparent, "NonDominantClose");          //Plots[7]
            }
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
                int size = 2;

                DynamicPlots = new Series<Price>[size];
                PlotRedBlackWMBrushes = new System.Windows.Media.Brush[size];
                PlotRedBlackDXBrushes = new SharpDX.Direct2D1.Brush[size];
                PlotRedBlackBrushNeedsUpdate = new bool[size];
                PlotBlackRedWMBrushes = new System.Windows.Media.Brush[size];
                PlotBlackRedDXBrushes = new SharpDX.Direct2D1.Brush[size];
                PlotBlackRedBrushNeedsUpdate = new bool[size];
                NonDomBarColor = new Series<int>(this, MaximumBarsLookBack.Infinite);

                for (int i = 0; i < DynamicPlots.Length; i++)
                {
                    DynamicPlots[i] = new Series<Price>(this, MaximumBarsLookBack.Infinite);
                }

                for (int i = 0; i < PlotRedBlackDXBrushes.Length; i++)
                {
                    PlotRedBlackDXBrushes[i] = null;
                    PlotBlackRedDXBrushes[i] = null;
                }

                for (int i = 0; i < PlotRedBlackBrushNeedsUpdate.Length; i++)
                {
                    PlotRedBlackBrushNeedsUpdate[i] = true;
                    PlotBlackRedBrushNeedsUpdate[i] = true;
                }

                PlotRedBlackWMBrushes[0] = Brushes.Red;
                PlotRedBlackWMBrushes[1] = Brushes.Black;

                PlotBlackRedWMBrushes[0] = Brushes.Black;
                PlotBlackRedWMBrushes[1] = Brushes.Red;
            }
		}

        private void CalculateSplitPrice()
        {
            Price NonDominantPrice = new Price { Close = Close[0], High = High[0], Low = Low[0], Open = Open[0] };
            Price DominantPrice = new Price { Close = Close[0], High = High[0], Low = Low[0], Open = Open[0] };

            DynamicPlots[NON_DOMINANT][0] = NonDominantPrice;
            DynamicPlots[DOMINANT][0] = DominantPrice;
            NonDomBarColor[0] = (CurrentBar % 2 == 0) ? COLOR_BLACK : COLOR_RED;

            DominantHigh[0] = High[0];
            DominantLow[0] = Low[0];
            DominantOpen[0] = Open[0];
            DominantClose[0] = Open[0] + 2 * TickSize;

            NonDominantHigh[0] = High[0];
            NonDominantLow[0] = Low[0];
            NonDominantOpen[0] = Open[0];
            NonDominantClose[0] = Open[0] - 2 * TickSize;

            //Non-dominant Bar color
            PlotBrushes[0][0] = (NonDominantPrice.Close < NonDominantPrice.Open) ? Brushes.Red : Brushes.Black;

            //Dominant Bar Color
            PlotBrushes[0][0] = (DominantPrice.Close < DominantPrice.Open) ? Brushes.Red : Brushes.Black;
        }

        private void CalcuateSplitVolume()
        {
           

            //DynamicPlots[NON_DOMINANT][0] = 0;
            //DynamicPlots[DOMINANT][0] = 0;

           

            //NonDomBarColor[0] = COLOR_BLACK;

            ////Non-dominant Bar color
            //PlotBrushes[0][0] = (NonDomBarColor[0] == COLOR_BLACK) ? Brushes.Red : Brushes.Black;

            ////Dominant Bar Color
            //PlotBrushes[1][0] = (NonDomBarColor[0] == COLOR_RED) ? Brushes.Red : Brushes.Black;
        }

        protected override void OnBarUpdate()
		{
            //Add your custom indicator logic here.
            if (CurrentBar < BarsRequiredToPlot) return;

            CalculateSplitPrice();
        }

        public override void OnCalculateMinMax()
        {
            // make sure to always start fresh values to calculate new min/max values
            double tmpMin = double.MaxValue;
            double tmpMax = double.MinValue;

            // For performance optimization, only loop through what is viewable on the chart		
            for (int index = Math.Max(ChartBars.FromIndex - 1, 0); index <= ChartBars.ToIndex; index++)
            {
                // return min/max of our High/Low plots
                for (int i = 0; i < DynamicPlots.Length; i++)
                {
                    tmpMin = Math.Min(tmpMin, DynamicPlots[i].GetValueAt(index).Low);
                    tmpMax = Math.Max(tmpMax, DynamicPlots[i].GetValueAt(index).High);
                }
            }

            MinValue = tmpMin;
            MaxValue = tmpMax;
        }

        private void DisposeAndRecreateDXBrushes(SharpDX.Direct2D1.Brush[] brushes, Brush[] wmBrushes,
            bool[] brushUpdate)
        {
            try
            {
                // Dispose and recreate our DX Brushes
                if (brushes == null)
                    return;

                for (int i = 0; i < brushes.Length; i++)
                {
                    if (brushes[i] != null)
                        brushes[i].Dispose();
                    if (RenderTarget != null)
                        brushes[i] = wmBrushes[i].ToDxBrush(RenderTarget);
                    brushUpdate[i] = false;

                }
            }
            catch (Exception exception)
            {
                Log(exception.ToString(), LogLevel.Error); ;
            }
        }

        private void UpdateBrushes(SharpDX.Direct2D1.Brush[] brushes, Brush[] wmBrushes, bool[] brushUpdate)
        {
            for (int i = 0; i < brushUpdate.Length; i++)
            {
                if (brushUpdate[i] == true)
                {
                    if (brushes[i] != null)
                        brushes[i].Dispose();
                    if (RenderTarget != null)
                        brushes[i] = wmBrushes[i].ToDxBrush(RenderTarget);
                    brushUpdate[i] = false;
                }
            }
        }

        public override void OnRenderTargetChanged()
        {
            DisposeAndRecreateDXBrushes(PlotRedBlackDXBrushes, PlotRedBlackWMBrushes, PlotRedBlackBrushNeedsUpdate);
            DisposeAndRecreateDXBrushes(PlotBlackRedDXBrushes, PlotBlackRedWMBrushes, PlotBlackRedBrushNeedsUpdate);
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            // Store previous AA mode
            SharpDX.Direct2D1.AntialiasMode oldAntialiasMode = RenderTarget.AntialiasMode;
            RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;

            // make sure brushes are updated
            UpdateBrushes(PlotRedBlackDXBrushes, PlotRedBlackWMBrushes, PlotRedBlackBrushNeedsUpdate);
            UpdateBrushes(PlotBlackRedDXBrushes, PlotBlackRedWMBrushes, PlotBlackRedBrushNeedsUpdate);

            customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, SharpDX.Color.DodgerBlue);

            for (int barIdx = Math.Max(ChartBars.FromIndex - 1, 0); barIdx <= ChartBars.ToIndex; barIdx++)
            {
                float startx, pwidth;
                int idx = chartControl.GetXByBarIndex(ChartBars, barIdx);

                if (UseBarWidth)
                {
                    startx = chartControl.GetXByBarIndex(ChartBars, barIdx) - chartControl.GetBarPaintWidth(ChartBars) / 2;
                    pwidth = chartControl.GetBarPaintWidth(ChartBars) / DynamicPlots.Length;
                }
                else
                {
                    startx = chartControl.GetXByBarIndex(ChartBars, barIdx) - (chartControl.Properties.BarDistance * BarDistanceToUse) / 2;
                    pwidth = (chartControl.Properties.BarDistance * BarDistanceToUse) / DynamicPlots.Length;
                }

                for (int pltIdx = 0; pltIdx < DynamicPlots.Length; pltIdx++)
                {
                    float spaceY = ((DynamicPlots[pltIdx].GetValueAt(barIdx).High == DynamicPlots[pltIdx].GetValueAt(barIdx).Close) ||
                        (DynamicPlots[pltIdx].GetValueAt(barIdx).High == DynamicPlots[pltIdx].GetValueAt(barIdx).Open)) ? 0 : 0;

                    spaceY = ((DynamicPlots[pltIdx].GetValueAt(barIdx).Low == DynamicPlots[pltIdx].GetValueAt(barIdx).Close) ||
                        (DynamicPlots[pltIdx].GetValueAt(barIdx).Low == DynamicPlots[pltIdx].GetValueAt(barIdx).Open)) ? 0 : 0;

                    float lineX = (startx + pwidth * pltIdx) + pwidth / 2;
                    float startY = (float)DynamicPlots[pltIdx].GetValueAt(barIdx).High;
                    float endY =(float) DynamicPlots[pltIdx].GetValueAt(barIdx).Low;
                    float openStartY = (float)DynamicPlots[pltIdx].GetValueAt(barIdx).Open + spaceY;
                    float openEndY = openStartY + spaceY;
                    float closeStartY = (float)DynamicPlots[pltIdx].GetValueAt(barIdx).Close + spaceY;
                    float closeEndY = closeStartY + spaceY;

                    startY = chartScale.GetYByValue(startY);
                    endY = chartScale.GetYByValue(endY);
                    openStartY = chartScale.GetYByValue(openStartY);
                    closeStartY = chartScale.GetYByValue(closeStartY);
                    openEndY = chartScale.GetYByValue(openEndY);
                    closeEndY = chartScale.GetYByValue(closeEndY);

                    SharpDX.Vector2 verticalLineStart = new SharpDX.Vector2(lineX,startY);
                    SharpDX.Vector2 verticalLineEnd = new SharpDX.Vector2(lineX, endY);

                    SharpDX.Vector2 openStart = new SharpDX.Vector2(lineX - (float)ChartBars.Properties.ChartStyle.BarWidth / 2, openStartY);
                    SharpDX.Vector2 openEnd = new SharpDX.Vector2(lineX - (float)ChartBars.Properties.ChartStyle.BarWidth / 2 - pwidth /4, openStartY);

                    SharpDX.Vector2 closeStart = new SharpDX.Vector2(lineX + (float)ChartBars.Properties.ChartStyle.BarWidth / 2, closeStartY);
                    SharpDX.Vector2 closeEnd = new SharpDX.Vector2(lineX + (float)ChartBars.Properties.ChartStyle.BarWidth / 2 + pwidth / 4, closeStartY);

                    if (NonDomBarColor.GetValueAt(barIdx) == COLOR_RED)
                    {
                        DrawPriceLines(verticalLineStart, verticalLineEnd, PlotRedBlackDXBrushes[pltIdx]);
                        DrawPriceLines(openStart, openEnd, PlotRedBlackDXBrushes[pltIdx]);
                        DrawPriceLines(closeStart, closeEnd, PlotRedBlackDXBrushes[pltIdx]);
                    }
                    else
                    {
                        DrawPriceLines(verticalLineStart, verticalLineEnd, PlotBlackRedDXBrushes[pltIdx]);
                        DrawPriceLines(openStart, openEnd, PlotBlackRedDXBrushes[pltIdx]);
                        DrawPriceLines(closeStart, closeEnd, PlotBlackRedDXBrushes[pltIdx]);
                    }
                }
            }

            // Reset AA mode.
            RenderTarget.AntialiasMode = oldAntialiasMode;
        }

        #region SharpDX Helper Classes & Methods

        private void SetOpacity(int index, double opacity)
        {
            if (PlotRedBlackWMBrushes[index] == null)
                return;

            if (PlotRedBlackWMBrushes[index].IsFrozen)
                PlotRedBlackWMBrushes[index] = PlotRedBlackWMBrushes[index].Clone();

            PlotRedBlackWMBrushes[index].Opacity = opacity / 100.0;
            PlotRedBlackWMBrushes[index].Freeze();

            PlotRedBlackBrushNeedsUpdate[index] = true;
        }

        private void DrawPriceLines(SharpDX.Vector2 start, SharpDX.Vector2 end,  SharpDX.Direct2D1.Brush brush)
        {
            RenderTarget.DrawLine(start, end, brush,8);
        }
        #endregion

        #region Properties
        [NinjaScriptProperty]
        [Display(Name = "UseBarWidth", Description = "Use BarWidth or BarDistance", Order = 1, GroupName = "Parameters")]
        public bool UseBarWidth
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "BarDistanceToUse", Description = "Amount of BarDistance to use with Plot group", Order = 2, GroupName = "Parameters")]
        public float BarDistanceToUse
        { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DominantClose
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DominantHigh
        {
            get { return Values[1]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DominantLow
        {
            get { return Values[2]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DominantOpen
        {
            get { return Values[3]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> NonDominantClose
        {
            get { return Values[4]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> NonDominantHigh
        {
            get { return Values[5]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> NonDominantLow
        {
            get { return Values[6]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> NonDominantOpen
        {
            get { return Values[7]; }
        }
        #endregion

    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xSplitPriceV3[] cachexSplitPriceV3;
		public xSplitPriceV3 xSplitPriceV3(bool useBarWidth, float barDistanceToUse)
		{
			return xSplitPriceV3(Input, useBarWidth, barDistanceToUse);
		}

		public xSplitPriceV3 xSplitPriceV3(ISeries<double> input, bool useBarWidth, float barDistanceToUse)
		{
			if (cachexSplitPriceV3 != null)
				for (int idx = 0; idx < cachexSplitPriceV3.Length; idx++)
					if (cachexSplitPriceV3[idx] != null && cachexSplitPriceV3[idx].UseBarWidth == useBarWidth && cachexSplitPriceV3[idx].BarDistanceToUse == barDistanceToUse && cachexSplitPriceV3[idx].EqualsInput(input))
						return cachexSplitPriceV3[idx];
			return CacheIndicator<xSplitPriceV3>(new xSplitPriceV3(){ UseBarWidth = useBarWidth, BarDistanceToUse = barDistanceToUse }, input, ref cachexSplitPriceV3);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xSplitPriceV3 xSplitPriceV3(bool useBarWidth, float barDistanceToUse)
		{
			return indicator.xSplitPriceV3(Input, useBarWidth, barDistanceToUse);
		}

		public Indicators.xSplitPriceV3 xSplitPriceV3(ISeries<double> input , bool useBarWidth, float barDistanceToUse)
		{
			return indicator.xSplitPriceV3(input, useBarWidth, barDistanceToUse);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xSplitPriceV3 xSplitPriceV3(bool useBarWidth, float barDistanceToUse)
		{
			return indicator.xSplitPriceV3(Input, useBarWidth, barDistanceToUse);
		}

		public Indicators.xSplitPriceV3 xSplitPriceV3(ISeries<double> input , bool useBarWidth, float barDistanceToUse)
		{
			return indicator.xSplitPriceV3(input, useBarWidth, barDistanceToUse);
		}
	}
}

#endregion
