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
	public class xSplitVolumeV3 : Indicator
	{
        public Series<double>[] DynamicPlots;
        public Series<int> NonDomBarColor;

        private System.Windows.Media.Brush[] PlotRedBlackWMBrushes;
        private SharpDX.Direct2D1.Brush[] PlotRedBlackDXBrushes;
        private System.Windows.Media.Brush[] PlotBlackRedWMBrushes;
        private SharpDX.Direct2D1.Brush[] PlotBlackRedDXBrushes;
        private bool[] PlotRedBlackBrushNeedsUpdate;
        private bool[] PlotBlackRedBrushNeedsUpdate;
        private const int COLOR_RED = 0;
        private const int COLOR_BLACK = 1;
        private const int NON_DOMINANT = 0;
        private const int DOMINANT = 1;
        private const int NDVOLUME = 0;
        private const int DVOLUME = 1;
        private const int NDVCOLOR = 2;
        private xSplitVolumeTypesEnums splitVolumeType = xSplitVolumeTypesEnums.PreviousCurrentHighLow;

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Jack Hershey/Todd Billis split volume indicator.";
				Name										= "xSplitVolumeV3";
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

                AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Bar, "DominantVolumeBar");            //Plots[0]
				AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Bar, "NonDominantVolumeBar");         //Plots[1]
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
                int size = 2;

                DynamicPlots = new Series<double>[size];
                PlotRedBlackWMBrushes = new System.Windows.Media.Brush[size];
                PlotRedBlackDXBrushes = new SharpDX.Direct2D1.Brush[size];
                PlotRedBlackBrushNeedsUpdate = new bool[size];
                PlotBlackRedWMBrushes = new System.Windows.Media.Brush[size];
                PlotBlackRedDXBrushes = new SharpDX.Direct2D1.Brush[size];
                PlotBlackRedBrushNeedsUpdate = new bool[size];
                NonDomBarColor = new Series<int>(this, MaximumBarsLookBack.Infinite);

                for (int i = 0; i < DynamicPlots.Length; i++)
                {
                    DynamicPlots[i] = new Series<double>(this, MaximumBarsLookBack.Infinite);
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

        private ArrayList CalculateSplitVolumeUsePreviousCurrentHighLow()
        {
            xBarInformation barinfo = xPriceVolumeFunctions().GetBarInformation(CurrentBar - 1, CurrentBar);

            ArrayList retList = new ArrayList();

            double nonDominantLegDistance = 0, dominantLegDistance;

            dominantLegDistance = Math.Abs(High[0] - Low[0]);

            int nonDomClr = COLOR_BLACK;

            switch (barinfo.BarType)
            {
                case xBarTypesEnum.HigherHighHigherLow:
                    nonDominantLegDistance = High[1] - Low[0];
                    nonDomClr = COLOR_RED;
                    break;

                case xBarTypesEnum.LowerLowLowerHigh:
                    nonDominantLegDistance = High[0] - Low[1];
                    nonDomClr = COLOR_BLACK;
                    break;

                case xBarTypesEnum.StitchLong:
                    nonDominantLegDistance = High[1] - Low[0];
                    nonDomClr = COLOR_BLACK;
                    break;

                case xBarTypesEnum.StitchShort:
                    nonDominantLegDistance = High[0] - Low[1];
                    nonDomClr = COLOR_RED;
                    break;

                case xBarTypesEnum.FlatTopPennant:
                    nonDominantLegDistance = High[1] - Low[0];
                    nonDomClr = COLOR_RED;
                    break;

                case xBarTypesEnum.FlatBottomPennant:
                    nonDominantLegDistance = High[0] - Low[1];
                    nonDomClr = COLOR_BLACK;
                    break;

                case xBarTypesEnum.Hitch:
                    nonDomClr = ((barinfo.BarBodyType == xBarBodyTypesEnums.Green) ||
                                (barinfo.BarBodyType == xBarBodyTypesEnums.Doji)) ? COLOR_BLACK : COLOR_RED;
                    break;

                case xBarTypesEnum.Inside:

                    if (barinfo.BarBodyType == xBarBodyTypesEnums.Green)
                    {
                        nonDominantLegDistance = High[1] - Low[0];
                        nonDomClr = COLOR_RED;
                    }
                    else
                    {
                        nonDominantLegDistance = High[0] - Low[1];
                        nonDomClr = COLOR_BLACK;
                    }
                    break;

                case xBarTypesEnum.OutSideBarLong:
                    nonDominantLegDistance = High[1] - Low[0];
                    nonDomClr = COLOR_RED;
                    break;

                case xBarTypesEnum.OutSideBarShort:
                    nonDominantLegDistance = High[0] - Low[1];
                    nonDomClr = COLOR_BLACK;
                    break;

                default:
                    nonDomClr = COLOR_BLACK;
                    break;
            }


            retList.Insert(NDVOLUME, nonDominantLegDistance);
            retList.Insert(DVOLUME, dominantLegDistance);
            retList.Insert(NDVCOLOR, nonDomClr);

            return retList;
        }

        private ArrayList CalculateSplitVolumeUseIntraBarPath()
        {
            ArrayList retList = new ArrayList();

            double nonDominantLegDistance = 0, dominantLegDistance = 0;
            int nonDomClr = COLOR_BLACK;

            if (Close[0] < Open[0])
            {
                nonDominantLegDistance = (High[0] - Open[0]) + (Close[0] - Low[0]);
                dominantLegDistance = High[0] - Low[0];
                nonDomClr = COLOR_BLACK;
            }
            else if (Close[0] >= Open[0])
            {
                nonDominantLegDistance = (Open[0] - Low[0]) + (High[0] - Close[0]);
                dominantLegDistance = High[0] - Low[0];
                nonDomClr = COLOR_RED;
            }

            retList.Insert(NDVOLUME, nonDominantLegDistance);
            retList.Insert(DVOLUME, dominantLegDistance);
            retList.Insert(NDVCOLOR, nonDomClr);

            return retList;
        }

        private ArrayList CalculateSplitVolumeUseBarPartsPercentage()
        {
            ArrayList retList = new ArrayList();

            double nonDominantLegDistance = 0, dominantLegDistance = 0;
            int nonDomClr = COLOR_BLACK;
            double value1 = 0, value2 = 0;

            if (Close[0] < Open[0])
            {
                value1 = (High[0] - Open[0]) + (Close[0] - Low[0]);
                value2 = Open[0] - Close[0];
                nonDomClr = (value1 > value2) ? COLOR_RED : COLOR_BLACK;
            }
            else if (Close[0] >= Open[0])
            {
                value1 = (Open[0] - Low[0]) + (High[0] - Close[0]);
                value2 = Close[0] - Open[0];
                nonDomClr = (value1 > value2) ? COLOR_BLACK : COLOR_RED;
            }

            nonDominantLegDistance = Math.Min(value1, value2);
            dominantLegDistance = Math.Max(value1, value2);

            retList.Insert(NDVOLUME, nonDominantLegDistance);
            retList.Insert(DVOLUME, dominantLegDistance);
            retList.Insert(NDVCOLOR, nonDomClr);

            return retList;
        }

        private void CalcuateSplitVolume()
        {
            ArrayList retList = null;

            switch (splitVolumeType)
            {
                case xSplitVolumeTypesEnums.PreviousCurrentHighLow:
                    retList = CalculateSplitVolumeUsePreviousCurrentHighLow();
                    break;

                case xSplitVolumeTypesEnums.IntraPartPath:
                    retList = CalculateSplitVolumeUseIntraBarPath();
                    break;

                case xSplitVolumeTypesEnums.BarPartsPercentage:
                    retList = CalculateSplitVolumeUseBarPartsPercentage();
                    break;

                default:
                    break;
            }

            if (retList == null) return;

            DynamicPlots[NON_DOMINANT][0] = (int)(((double)retList[NDVOLUME] / ((double)retList[NDVOLUME] + (double)retList[DVOLUME])) * Volume[0]);
            DynamicPlots[DOMINANT][0] = (int)(((double)retList[DVOLUME] / ((double)retList[NDVOLUME] + (double)retList[DVOLUME])) * Volume[0]);

            DominantVolumeBar[0] = DynamicPlots[DOMINANT][0];
            NonDominantVolumeBar[0] = DynamicPlots[NON_DOMINANT][0];

            NonDomBarColor[0] = (int)retList[NDVCOLOR];

            //Non-dominant Bar color
            PlotBrushes[0][0] = (NonDomBarColor[0] == COLOR_BLACK) ? Brushes.Red : Brushes.Black;

            //Dominant Bar Color
            PlotBrushes[1][0] = (NonDomBarColor[0] == COLOR_RED) ? Brushes.Red : Brushes.Black;
        }

        protected override void OnBarUpdate()
		{
            //Add your custom indicator logic here.
            if (CurrentBar < BarsRequiredToPlot) return;

            CalcuateSplitVolume();
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
                    tmpMin = Math.Min(tmpMin, DynamicPlots[i].GetValueAt(index));
                    tmpMax = Math.Max(tmpMax, DynamicPlots[i].GetValueAt(index));
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

            for (int barIdx = Math.Max(ChartBars.FromIndex - 1, 0); barIdx <= ChartBars.ToIndex; barIdx++)
            {
                float startx, pwidth;

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
                    SharpDX.RectangleF thisRect = new SharpDX.RectangleF()
                    {
                        X = startx + pwidth * pltIdx,
                        Y = chartScale.GetYByValue(0),
                        Width = pwidth,
                        Height = -chartScale.GetYByValue(0) + chartScale.GetYByValue(DynamicPlots[pltIdx].GetValueAt(barIdx))
                    };

                    if (NonDomBarColor.GetValueAt(barIdx) == COLOR_RED)
                    {
                        FillRectangle(thisRect, PlotRedBlackDXBrushes[pltIdx]);
                    }
                    else
                    {
                        FillRectangle(thisRect, PlotBlackRedDXBrushes[pltIdx]);
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

        public void FillRectangle(SharpDX.RectangleF rect, SharpDX.Direct2D1.Brush brush)
        {
            RenderTarget.FillRectangle(rect, brush);
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

        [Description("")]
        [NinjaScriptProperty]
        [Display(Name = "CalculationType", Order = 1, GroupName = "Parameters")]
        public xSplitVolumeTypesEnums CalculationType
        {
            get { return splitVolumeType; }
            set { splitVolumeType = value; }
        }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> DominantVolumeBar
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> NonDominantVolumeBar
		{
			get { return Values[1]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xSplitVolumeV3[] cachexSplitVolumeV3;
		public xSplitVolumeV3 xSplitVolumeV3(bool useBarWidth, float barDistanceToUse, xSplitVolumeTypesEnums calculationType)
		{
			return xSplitVolumeV3(Input, useBarWidth, barDistanceToUse, calculationType);
		}

		public xSplitVolumeV3 xSplitVolumeV3(ISeries<double> input, bool useBarWidth, float barDistanceToUse, xSplitVolumeTypesEnums calculationType)
		{
			if (cachexSplitVolumeV3 != null)
				for (int idx = 0; idx < cachexSplitVolumeV3.Length; idx++)
					if (cachexSplitVolumeV3[idx] != null && cachexSplitVolumeV3[idx].UseBarWidth == useBarWidth && cachexSplitVolumeV3[idx].BarDistanceToUse == barDistanceToUse && cachexSplitVolumeV3[idx].CalculationType == calculationType && cachexSplitVolumeV3[idx].EqualsInput(input))
						return cachexSplitVolumeV3[idx];
			return CacheIndicator<xSplitVolumeV3>(new xSplitVolumeV3(){ UseBarWidth = useBarWidth, BarDistanceToUse = barDistanceToUse, CalculationType = calculationType }, input, ref cachexSplitVolumeV3);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xSplitVolumeV3 xSplitVolumeV3(bool useBarWidth, float barDistanceToUse, xSplitVolumeTypesEnums calculationType)
		{
			return indicator.xSplitVolumeV3(Input, useBarWidth, barDistanceToUse, calculationType);
		}

		public Indicators.xSplitVolumeV3 xSplitVolumeV3(ISeries<double> input , bool useBarWidth, float barDistanceToUse, xSplitVolumeTypesEnums calculationType)
		{
			return indicator.xSplitVolumeV3(input, useBarWidth, barDistanceToUse, calculationType);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xSplitVolumeV3 xSplitVolumeV3(bool useBarWidth, float barDistanceToUse, xSplitVolumeTypesEnums calculationType)
		{
			return indicator.xSplitVolumeV3(Input, useBarWidth, barDistanceToUse, calculationType);
		}

		public Indicators.xSplitVolumeV3 xSplitVolumeV3(ISeries<double> input , bool useBarWidth, float barDistanceToUse, xSplitVolumeTypesEnums calculationType)
		{
			return indicator.xSplitVolumeV3(input, useBarWidth, barDistanceToUse, calculationType);
		}
	}
}

#endregion
