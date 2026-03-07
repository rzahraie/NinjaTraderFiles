#region Using declarations
using System;
using System.Collections.Generic;
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
	public class xSplitVolume : Indicator
	{
		private Series<int> DominantVolume;
		private Series<int> NonDominantVolume;
		private Series<SharpDX.Color> DominantVolumeColor;
		private Series<SharpDX.Color> NonDominantVolumeColor;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Split volume.";
				Name										= "xSplitVolume";
				Calculate									= Calculate.OnEachTick;
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
                MaximumBarsLookBack                         = MaximumBarsLookBack.Infinite;
                IsAutoScale                                 = true;
            }
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{				
				DominantVolume = new Series<int>(this);
				NonDominantVolume = new Series<int>(this);
				DominantVolumeColor = new Series<SharpDX.Color>(this);
				NonDominantVolumeColor = new Series<SharpDX.Color>(this);
			}
		}

        public override void OnCalculateMinMax()
        {
            int tmpMin = int.MaxValue;
            int tmpMax = int.MinValue;

            Print("Index\t\t\tMin Value\t\t\t\tMax Value\t\t\t\tV1\t\t\t\tV2");
            Print("-------------------------------------------------------------------------------------------");
            for (int idx = ChartBars.FromIndex; idx <= ChartBars.ToIndex; idx++)
            {
                int plotValueDom = DominantVolume.GetValueAt(idx);
                int plotValueNonDom = NonDominantVolume.GetValueAt(idx);

                int plotValue = Math.Max(plotValueDom, plotValueNonDom);

                // return min/max of volume value
                tmpMin = Math.Min(tmpMin, plotValue);
                tmpMax = Math.Max(tmpMax, plotValue);

                Print(idx + "\t\t\t\t" + tmpMin + "\t\t\t\t\t" + tmpMax + "\t\t\t\t\t" + plotValueDom + "\t\t\t\t" + plotValueNonDom);
                
            }

            // Finally, set the minimum and maximum Y-Axis values to +/- 50 ticks from the primary close value
            MinValue = tmpMin - 50;
            MaxValue = tmpMax + 50;
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            int barWidth = (int)ChartBars.Properties.ChartStyle.BarWidth;

            for (int idx = ChartBars.FromIndex; idx <= ChartBars.ToIndex; idx++)
            {
                if (idx < BarsRequiredToPlot) return;

                int x = chartControl.GetXByBarIndex(ChartBars, idx);
                List<float> lf = GetBarDistance(idx);

                float barDist = (lf[1] - lf[0]) / 2;

                CreateBar(NonDominantVolume.GetValueAt(idx), lf[0], lf[0] + barDist, NonDominantVolumeColor.GetValueAt(idx));
                CreateBar(DominantVolume.GetValueAt(idx), lf[0] + barDist, lf[1], DominantVolumeColor.GetValueAt(idx));

                //CreateBar(350, lf[0], lf[0] + barDist, NonDominantVolumeColor.GetValueAt(idx));
                //CreateBar(700, lf[0] + barDist, lf[1], DominantVolumeColor.GetValueAt(idx));
            }

            //base.OnRender(chartControl, chartScale);
        }

        private List<float> GetBarDistance(int indx)
        {
            ChartControlProperties props = ChartControl.Properties;

            float barSpace = props.BarDistance;
            float barWidth = (float)ChartControl.BarWidth;

            float barStartX = ChartControl.GetXByBarIndex(ChartBars, indx);
            barStartX = barStartX - barSpace / 2;
            float barEndx = ChartControl.GetXByBarIndex(ChartBars, indx) + barSpace / 2;

            return new List<float>() { barStartX, barEndx - (float)(0.1 * barSpace) };

        }

        private void CreateBar(float value, float x1, float x2, SharpDX.Color clr)
        {
            int barWidth = (int)ChartBars.Properties.ChartStyle.BarWidth;
            float y0 = ChartPanel.Y + ChartPanel.H;
            float y1 = y0 - value;
            SharpDX.Vector2 startPoint = new SharpDX.Vector2(x1, y0);
            SharpDX.Vector2 endPoint = new SharpDX.Vector2(x2, y1);

            // calculate the desired width and heigh of the rectangle
            float width = endPoint.X - startPoint.X;
            float height = endPoint.Y - startPoint.Y;

            // construct the rectangleF struct to describe the with position and size the drawing
            SharpDX.RectangleF rect = new SharpDX.RectangleF(startPoint.X, startPoint.Y, width, height);

            // define the brush used in the rectangle
            SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, clr);

            // execute the render target fill rectangle with desired values
            RenderTarget.FillRectangle(rect, customDXBrush);

            // always dispose of a brush when finished
            customDXBrush.Dispose();
        }

        protected override void OnBarUpdate()
		{
            //Add your custom indicator logic here.

            if (CurrentBar < BarsRequiredToPlot) return;

            xBarInformation barinfo = xPriceVolumeFunctions().GetBarInformation(CurrentBar - 1, CurrentBar);

            double nonDominantLegDistance = 0, dominantLegDistance;

            dominantLegDistance = Math.Abs(High[0] - Low[0]);

            switch (barinfo.BarType)
            {
                case xBarTypesEnum.HigherHighHigherLow:
                    nonDominantLegDistance = High[1] - Low[0];
                    NonDominantVolumeColor[0] = SharpDX.Color.Red;
                    break;

                case xBarTypesEnum.LowerLowLowerHigh:
                    nonDominantLegDistance = High[0] - Low[1];
                    NonDominantVolumeColor[0] = SharpDX.Color.Black;
                    break;

                case xBarTypesEnum.StitchLong:
                    nonDominantLegDistance = High[1] - Low[0];
                    NonDominantVolumeColor[0] = SharpDX.Color.Black;
                    break;

                case xBarTypesEnum.StitchShort:
                    nonDominantLegDistance = High[0] - Low[1];
                    NonDominantVolumeColor[0] = SharpDX.Color.Red;
                    break;

                case xBarTypesEnum.FlatTopPennant:
                    nonDominantLegDistance = High[1] - Low[0];
                    NonDominantVolumeColor[0] = SharpDX.Color.Red;
                    break;

                case xBarTypesEnum.FlatBottomPennant:
                    nonDominantLegDistance = High[0] - Low[1];
                    NonDominantVolumeColor[0] = SharpDX.Color.Black;
                    break;

                case xBarTypesEnum.Hitch:
                    NonDominantVolumeColor[0] = ((barinfo.BarBodyType == xBarBodyTypesEnums.Green) ||
                                (barinfo.BarBodyType == xBarBodyTypesEnums.Doji)) ? SharpDX.Color.Black : SharpDX.Color.Red;
                    break;

                case xBarTypesEnum.Inside:

                    if (barinfo.BarBodyType == xBarBodyTypesEnums.Green)
                    {
                        nonDominantLegDistance = High[1] - Low[0];
                        NonDominantVolumeColor[0] = SharpDX.Color.Red;
                    }
                    else
                    {
                        nonDominantLegDistance = High[0] - Low[1];
                        NonDominantVolumeColor[0] = SharpDX.Color.Black;
                    }
                    break;

                case xBarTypesEnum.OutSideBarLong:
                    nonDominantLegDistance = High[1] - Low[0];
                    NonDominantVolumeColor[0] = SharpDX.Color.Red;
                    break;

                case xBarTypesEnum.OutSideBarShort:
                    nonDominantLegDistance = High[0] - Low[1];
                    NonDominantVolumeColor[0] = SharpDX.Color.Black;
                    break;

                default:
                    NonDominantVolumeColor[0] = SharpDX.Color.Black;
                    break;
            }

            DominantVolumeColor[0] = (NonDominantVolumeColor[0] == SharpDX.Color.Red) ? SharpDX.Color.Black : SharpDX.Color.Red;

            NonDominantVolume[0] = (int)((nonDominantLegDistance / (nonDominantLegDistance + dominantLegDistance)) * Volume[0]);
            DominantVolume[0] = (int)((dominantLegDistance / (nonDominantLegDistance + dominantLegDistance)) * Volume[0]);
        }
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xSplitVolume[] cachexSplitVolume;
		public xSplitVolume xSplitVolume()
		{
			return xSplitVolume(Input);
		}

		public xSplitVolume xSplitVolume(ISeries<double> input)
		{
			if (cachexSplitVolume != null)
				for (int idx = 0; idx < cachexSplitVolume.Length; idx++)
					if (cachexSplitVolume[idx] != null &&  cachexSplitVolume[idx].EqualsInput(input))
						return cachexSplitVolume[idx];
			return CacheIndicator<xSplitVolume>(new xSplitVolume(), input, ref cachexSplitVolume);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xSplitVolume xSplitVolume()
		{
			return indicator.xSplitVolume(Input);
		}

		public Indicators.xSplitVolume xSplitVolume(ISeries<double> input )
		{
			return indicator.xSplitVolume(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xSplitVolume xSplitVolume()
		{
			return indicator.xSplitVolume(Input);
		}

		public Indicators.xSplitVolume xSplitVolume(ISeries<double> input )
		{
			return indicator.xSplitVolume(input);
		}
	}
}

#endregion
