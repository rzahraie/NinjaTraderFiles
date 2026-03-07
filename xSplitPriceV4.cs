#region Using declarations
using System;
using System.Collections.Generic;
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
	public class xSplitPriceV4 : Indicator
	{
        public Series<Price>[] DynamicPlots;
        public Series<int> BarColors;

        private const int size = 3;
        private const int numberOfBrushes = 7;

        private System.Windows.Media.Brush[][] PlotWMBrushes;
        private SharpDX.Direct2D1.Brush[][] PlotDXBrushes;
        private bool[][] PlotBrushesNeedsUpdate;


        private System.Windows.Media.Brush[] PlotRedBlackWMBrushes;
        private System.Windows.Media.Brush[] PlotBlackRedWMBrushes;

        private SharpDX.Direct2D1.Brush[] PlotRedBlackDXBrushes;
        private SharpDX.Direct2D1.Brush[] PlotBlackRedDXBrushes;

        private SharpDX.Direct2D1.SolidColorBrush customDXBrush;

        private bool[] PlotRedBlackBrushNeedsUpdate;
        private bool[] PlotBlackRedBrushNeedsUpdate;

        private const int COLOR_RED = 0;
        private const int COLOR_BLACK = 1;

        /// <summary>
        /// ///////////////
        /// </summary>
        //private const int NON_DOMINANT = 0;
        //private const int DOMINANT = 1;
        //private const int NDVOLUME = 0;
        //private const int DVOLUME = 1;
        //private const int NDVCOLOR = 2;

        /// <summary>
        /// ///////
        /// </summary>
        private const int BAR_1 = 0;
        private const int BAR_2 = 1;
        private const int BAR_3 = 2;

        private const int BBR = 0;
        private const int RBB = 1;
        private const int BBB = 2;
        private const int RBR = 3;
        private const int RRB = 4;
        private const int RRR = 5;
        private const int BRB = 6;

        private void IntializeBrushes(System.Windows.Media.Brush[] plotWMBrush, SolidColorBrush color1,
            SolidColorBrush color2, SolidColorBrush color3, SharpDX.Direct2D1.Brush[] plotDxBrush,bool[] plotBrushNeedsUpdate)
        {
            plotWMBrush[0] = color1;
            plotWMBrush[1] = color2;
            plotWMBrush[2] = color3;

            for (int i = 0; i < plotDxBrush.Length; i++)
            {
                plotDxBrush[i] = null;
                plotBrushNeedsUpdate[i] = true;
            }

        }
        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Jack Hershey/Todd Billis split price indicator.";
				Name										= "xSplitPriceV4";
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

                AddPlot(Brushes.Transparent, "Bar1Open");            //Plots[0]
                AddPlot(Brushes.Transparent, "Bar1High");          //Plots[1]
                AddPlot(Brushes.Transparent, "Bar1Low");          //Plots[2]
                AddPlot(Brushes.Transparent, "Bar1Close");          //Plots[3]

                AddPlot(Brushes.Transparent, "Bar2Open");            //Plots[4]
                AddPlot(Brushes.Transparent, "Bar2High");          //Plots[5]
                AddPlot(Brushes.Transparent, "Bar2Low");          //Plots[6]
                AddPlot(Brushes.Transparent, "Bar2Close");          //Plots[7]

                AddPlot(Brushes.Transparent, "Bar3Open");            //Plots[8]
                AddPlot(Brushes.Transparent, "Bar3High");          //Plots[9]
                AddPlot(Brushes.Transparent, "Bar3Low");          //Plots[10]
                AddPlot(Brushes.Transparent, "Bar3Close");          //Plots[11]
            }
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
                int size = 3;

                DynamicPlots = new Series<Price>[size];

                PlotRedBlackWMBrushes = new System.Windows.Media.Brush[size];
                PlotBlackRedWMBrushes = new System.Windows.Media.Brush[size];

                PlotRedBlackDXBrushes = new SharpDX.Direct2D1.Brush[size];
                PlotBlackRedDXBrushes = new SharpDX.Direct2D1.Brush[size];

                PlotRedBlackBrushNeedsUpdate = new bool[size];
                PlotBlackRedBrushNeedsUpdate = new bool[size];

                BarColors = new Series<int>(this, MaximumBarsLookBack.Infinite);

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
                PlotRedBlackWMBrushes[2] = Brushes.Red;


                PlotBlackRedWMBrushes[0] = Brushes.Black;
                PlotBlackRedWMBrushes[1] = Brushes.Red;
                PlotBlackRedWMBrushes[2] = Brushes.Black;

                IntializeBrushes(PlotWMBrushes[0], Brushes.Black, Brushes.Black, Brushes.Red, PlotDXBrushes[0], PlotBrushesNeedsUpdate[0]);
                IntializeBrushes(PlotWMBrushes[1], Brushes.Red, Brushes.Black, Brushes.Black, PlotDXBrushes[1], PlotBrushesNeedsUpdate[1]);
                IntializeBrushes(PlotWMBrushes[2], Brushes.Black, Brushes.Black, Brushes.Black, PlotDXBrushes[2], PlotBrushesNeedsUpdate[2]);
                IntializeBrushes(PlotWMBrushes[3], Brushes.Red, Brushes.Black, Brushes.Red, PlotDXBrushes[3], PlotBrushesNeedsUpdate[3]);
                IntializeBrushes(PlotWMBrushes[4], Brushes.Red, Brushes.Red, Brushes.Black, PlotDXBrushes[4], PlotBrushesNeedsUpdate[4]);
                IntializeBrushes(PlotWMBrushes[5], Brushes.Red, Brushes.Red, Brushes.Red, PlotDXBrushes[5], PlotBrushesNeedsUpdate[5]);
                IntializeBrushes(PlotWMBrushes[6], Brushes.Black, Brushes.Red, Brushes.Black, PlotDXBrushes[6], PlotBrushesNeedsUpdate[6]);

            }
		}

        private void CalculateSplitPriceEx()
        {
            Price Bar1 = new Price { Close = Close[0], High = High[0], Low = Low[0], Open = Open[0] };
            Price Bar2 = new Price { Close = Close[0], High = High[0], Low = Low[0], Open = Open[0] };
            Price Bar3 = new Price { Close = Close[0], High = High[0], Low = Low[0], Open = Open[0] };

            DynamicPlots[BAR_1][0] = Bar1;
            DynamicPlots[BAR_2][0] = Bar2;
            DynamicPlots[BAR_3][0] = Bar3;

            xBarInformation m_PrevBarType = xPriceVolumeFunctions().GetBarInformation(CurrentBar - 2, CurrentBar - 1);

            xBarInformation currBarInfo = xPriceVolumeFunctions().GetBarInformation(CurrentBar - 1, CurrentBar);
            xBarInformation prevBarInfo = xPriceVolumeFunctions().GetBarInformation(CurrentBar - 2, CurrentBar - 1);

            //A. current bar higher high lower close - prev bar lower close
            if ((currBarInfo.BarType == xBarTypesEnum.HigherHighHigherLow) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose) && 
                (prevBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Open[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;

                BarColors[0] = BBR;
            }
            //B. current bar higher high higher close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.HigherHighHigherLow) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Low[0];

                Bar3.High = Low[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = RBB;

            }
            //C.current bar higher high higher close - prev bar higher close
            else if ((currBarInfo.BarType == xBarTypesEnum.HigherHighHigherLow) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Low[0];

                Bar3.High = Low[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = BBB;

            }
            //D. current bar higher high lower close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.HigherHighHigherLow) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Open[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;

                BarColors[0] = RBR;

            }
            //E. current bar lower high lower close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.LowerLowLowerHigh) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Low[0];

                Bar3.High = Low[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = BRB;

            }
            //F. current bar lower high higher close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.LowerLowLowerHigh) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = Open[0];
                Bar2.Low = Low[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = RRB;

            }
            //G. current bar lower high higher close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.LowerLowLowerHigh) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = Open[0];
                Bar2.Low = Low[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = BRB;

            }
            //H. current bar lower high lower close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.LowerLowLowerHigh) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = Open[0];
                Bar2.Low = Low[0];

                Bar3.High = Low[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Red;

                BarColors[0] = RRR;

            }
            //I. current bar outside lower close - prev bar higer close
            else if ((currBarInfo.BarType == xBarTypesEnum.OutSideBarShort) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Open[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;

                BarColors[0] = BBR;

            }
            //J. current bar outside higer close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.OutSideBarLong) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = Open[0];
                Bar2.Low = Low[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = RRB;

            }
            //K. current bar outside higer close - prev bar higher close
            else if ((currBarInfo.BarType == xBarTypesEnum.OutSideBarLong) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = Open[0];
                Bar2.Low = Low[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = BRB;

            }
            //L. current bar outside lower close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.OutSideBarShort) &&
               (prevBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Open[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;

                BarColors[0] = RBR;

            }
            //M. current bar stitch long lower close - prev bar higher close
            else if ((currBarInfo.BarType == xBarTypesEnum.StitchLong) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose)
               && (prevBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Open[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;

                BarColors[0] = BBR;

            }
            //N. current bar stitch long higher close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.StitchLong) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose)
               && (prevBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Low[0];

                Bar3.High = Low[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = RBB;

            }
            //O. current bar stitch long higher close - prev bar higher close
            else if ((currBarInfo.BarType == xBarTypesEnum.StitchLong) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose)
               && (prevBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Low[0];

                Bar3.High = Low[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = BBB;

            }
            //P. current bar stitch long lower close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.StitchLong) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose)
               && (prevBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Open[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;

                BarColors[0] = RBR;

            }
            //Q. current bar stitch short lower close - prev bar higher close
            else if ((currBarInfo.BarType == xBarTypesEnum.StitchShort) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose)
               && (prevBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Low[0];

                Bar3.High = Low[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;

                BarColors[0] = BBR;

            }
            //R. current bar stitch short higher close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.StitchShort) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose)
               && (prevBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = Open[0];
                Bar2.Low = Low[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = RRB;

            }
            //S. current bar stitch short higher close - prev bar higher close
            else if ((currBarInfo.BarType == xBarTypesEnum.StitchShort) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose)
               && (prevBarInfo.BarCloseType == xBarCloseTypesEnums.HigherClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = Open[0];
                Bar2.Low = Low[0];

                Bar3.High = High[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Black;
                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Black;

                BarColors[0] = BRB;

            }
            //T. current bar stitch short lower close - prev bar lower close
            else if ((currBarInfo.BarType == xBarTypesEnum.StitchShort) && (currBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose)
               && (prevBarInfo.BarCloseType == xBarCloseTypesEnums.LowerClose))
            {
                Bar1.High = High[1];
                Bar1.Low = Low[1];

                Bar2.High = High[0];
                Bar2.Low = Low[0];

                Bar3.High = Low[0];
                Bar3.Low = Low[0];

                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Red;
                PlotBrushes[0][0] = Brushes.Red;

                BarColors[0] = RRR;

            }
        }
        private void CalculateSplitPrice()
        {
            Price Bar1 = new Price { Close = Close[0], High = High[0], Low = Low[0], Open = Open[0] };
            Price Bar2 = new Price { Close = Close[0], High = High[0], Low = Low[0], Open = Open[0] };
            Price Bar3 = new Price { Close = Close[0], High = High[0], Low = Low[0], Open = Open[0] };

            DynamicPlots[BAR_1][0] = Bar1;
            DynamicPlots[BAR_2][0] = Bar2;
            DynamicPlots[BAR_3][0] = Bar3;

            BarColors[0] = (CurrentBar % 2 == 0) ? COLOR_BLACK : COLOR_RED;

            Bar1High[0] = High[0];
            Bar1Low[0] = Low[0];
            Bar1Open[0] = Open[0];
            Bar1Close[0] = Open[0] + 2 * TickSize;

            Bar2High[0] = High[0];
            Bar2Low[0] = Low[0];
            Bar2Open[0] = Open[0];
            Bar2Close[0] = Open[0] - 2 * TickSize;

            Bar3High[0] = High[0];
            Bar3Low[0] = Low[0];
            Bar3Open[0] = Open[0];
            Bar3Close[0] = Open[0] + 2 * TickSize;

            //Non-dominant Bar color
            PlotBrushes[0][0] = (Bar1.Close < Bar1.Open) ? Brushes.Red : Brushes.Black;

            //Dominant Bar Color
            PlotBrushes[0][0] = (Bar2.Close < Bar2.Open) ? Brushes.Red : Brushes.Black;

            PlotBrushes[0][0] = (Bar3.Close < Bar3.Open) ? Brushes.Red : Brushes.Black;
        }

        protected override void OnBarUpdate()
		{
            //Add your custom indicator logic here.
            if (CurrentBar < BarsRequiredToPlot) return;

            CalculateSplitPriceEx();
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
                if ((brushes == null) || (wmBrushes == null) || (brushUpdate == null))
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
            // Dispose and recreate our DX Brushes
            if ((brushes == null) || (wmBrushes == null) || (brushUpdate == null))
                return;

            for (int i = 0; i < brushUpdate.Length; i++)
            {
                if (brushUpdate[i] == true)
                {
                    if (brushes[i] != null)
                        brushes[i].Dispose();
                    if ((RenderTarget != null) && (wmBrushes[i] != null))
                        brushes[i] = wmBrushes[i].ToDxBrush(RenderTarget);
                    brushUpdate[i] = false;
                }
            }
        }

        public override void OnRenderTargetChanged()
        {
            DisposeAndRecreateDXBrushes(PlotRedBlackDXBrushes, PlotRedBlackWMBrushes, PlotRedBlackBrushNeedsUpdate);
            DisposeAndRecreateDXBrushes(PlotBlackRedDXBrushes, PlotBlackRedWMBrushes, PlotBlackRedBrushNeedsUpdate);

            for (int i = 0; i < numberOfBrushes; i++)
            {
                DisposeAndRecreateDXBrushes(PlotDXBrushes[i], PlotWMBrushes[i], PlotBrushesNeedsUpdate[i]);
            }
        }

        private void SetWMBrushColors(int idx)
        {
            switch(BarColors.GetValueAt(idx))
            {
                case BBR:
                    PlotRedBlackWMBrushes[0] = Brushes.Black;
                    PlotRedBlackWMBrushes[1] = Brushes.Black;
                    PlotRedBlackWMBrushes[2] = Brushes.Red;
                 break;

                case RBB:
                    PlotRedBlackWMBrushes[0] = Brushes.Red;
                    PlotRedBlackWMBrushes[1] = Brushes.Black;
                    PlotRedBlackWMBrushes[2] = Brushes.Black;
                    break;

                case BBB:
                    PlotRedBlackWMBrushes[0] = Brushes.Black;
                    PlotRedBlackWMBrushes[1] = Brushes.Black;
                    PlotRedBlackWMBrushes[2] = Brushes.Black;
                    break;

                case RBR:
                    PlotRedBlackWMBrushes[0] = Brushes.Red;
                    PlotRedBlackWMBrushes[1] = Brushes.Black;
                    PlotRedBlackWMBrushes[2] = Brushes.Red;
                    break;

                case RRB:
                    PlotRedBlackWMBrushes[0] = Brushes.Red;
                    PlotRedBlackWMBrushes[1] = Brushes.Red;
                    PlotRedBlackWMBrushes[2] = Brushes.Black;
                    break;

                case RRR:
                    PlotRedBlackWMBrushes[0] = Brushes.Red;
                    PlotRedBlackWMBrushes[1] = Brushes.Red;
                    PlotRedBlackWMBrushes[2] = Brushes.Red;
                    break;

                case BRB:
                    PlotRedBlackWMBrushes[0] = Brushes.Black;
                    PlotRedBlackWMBrushes[1] = Brushes.Red;
                    PlotRedBlackWMBrushes[2] = Brushes.Black;
                    break;

                default:
                    PlotRedBlackWMBrushes[0] = Brushes.Black;
                    PlotRedBlackWMBrushes[1] = Brushes.Black;
                    PlotRedBlackWMBrushes[2] = Brushes.Black;
                    break;
            }

            UpdateBrushes(PlotRedBlackDXBrushes, PlotRedBlackWMBrushes, PlotRedBlackBrushNeedsUpdate);
            //UpdateBrushes(PlotBlackRedDXBrushes, PlotBlackRedWMBrushes, PlotBlackRedBrushNeedsUpdate);
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

                    SetWMBrushColors(pltIdx);



                    DrawPriceLines(verticalLineStart, verticalLineEnd, PlotRedBlackDXBrushes[pltIdx]);
                    //DrawPriceLines(openStart, openEnd, PlotRedBlackDXBrushes[pltIdx]);
                    //DrawPriceLines(closeStart, closeEnd, PlotRedBlackDXBrushes[pltIdx]);
                   
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
            if (brush == null) return;

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
        public Series<double> Bar1Close
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar1High
        {
            get { return Values[1]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar1Low
        {
            get { return Values[2]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar1Open
        {
            get { return Values[3]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar2Close
        {
            get { return Values[4]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar2High
        {
            get { return Values[5]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar2Low
        {
            get { return Values[6]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar2Open
        {
            get { return Values[7]; }
        }
        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar3Close
        {
            get { return Values[8]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar3High
        {
            get { return Values[9]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar3Low
        {
            get { return Values[10]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> Bar3Open
        {
            get { return Values[11]; }
        }
        #endregion

    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xSplitPriceV4[] cachexSplitPriceV4;
		public xSplitPriceV4 xSplitPriceV4(bool useBarWidth, float barDistanceToUse)
		{
			return xSplitPriceV4(Input, useBarWidth, barDistanceToUse);
		}

		public xSplitPriceV4 xSplitPriceV4(ISeries<double> input, bool useBarWidth, float barDistanceToUse)
		{
			if (cachexSplitPriceV4 != null)
				for (int idx = 0; idx < cachexSplitPriceV4.Length; idx++)
					if (cachexSplitPriceV4[idx] != null && cachexSplitPriceV4[idx].UseBarWidth == useBarWidth && cachexSplitPriceV4[idx].BarDistanceToUse == barDistanceToUse && cachexSplitPriceV4[idx].EqualsInput(input))
						return cachexSplitPriceV4[idx];
			return CacheIndicator<xSplitPriceV4>(new xSplitPriceV4(){ UseBarWidth = useBarWidth, BarDistanceToUse = barDistanceToUse }, input, ref cachexSplitPriceV4);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xSplitPriceV4 xSplitPriceV4(bool useBarWidth, float barDistanceToUse)
		{
			return indicator.xSplitPriceV4(Input, useBarWidth, barDistanceToUse);
		}

		public Indicators.xSplitPriceV4 xSplitPriceV4(ISeries<double> input , bool useBarWidth, float barDistanceToUse)
		{
			return indicator.xSplitPriceV4(input, useBarWidth, barDistanceToUse);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xSplitPriceV4 xSplitPriceV4(bool useBarWidth, float barDistanceToUse)
		{
			return indicator.xSplitPriceV4(Input, useBarWidth, barDistanceToUse);
		}

		public Indicators.xSplitPriceV4 xSplitPriceV4(ISeries<double> input , bool useBarWidth, float barDistanceToUse)
		{
			return indicator.xSplitPriceV4(input, useBarWidth, barDistanceToUse);
		}
	}
}

#endregion
