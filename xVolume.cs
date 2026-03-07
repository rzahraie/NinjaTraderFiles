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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    public class xVolume : Indicator
    {
        #region Variables
        // Wizard generated variables
        private xVolumeTypesEnums Type = xVolumeTypesEnums.UpDownSide; // Default setting for Type
                                                                       // User defined variables (add any user defined variables below)
        private xVolumeColorStateEnums VolumeColorState;

        private Series<int> VolumeColorStateSeries;
        private Series<double> VolumeAccSeries;
        private Series<double> VolumeVelSeries;

        private string PrevVolumeColorstring;
        private string VolumeColorSequence;
        private Series<string> VolumeColorSequenceSeries;

        private double m_VolumeLevelMultiplier = 1;

        private Brush m_PrevBarColor = Brushes.White;

        private xBarTypesEnum m_PrevBarType = xBarTypesEnum.Undefined;

        private double m_PrevHLAvg;
        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                IsOverlay = false;
                Calculate = Calculate.OnBarClose;

                VolumeColorStateSeries = new Series<int>(this);
                VolumeColorSequenceSeries = new Series<string>(this);
                VolumeAccSeries = new Series<double>(this);
                VolumeVelSeries = new Series<double>(this);

                BarsRequiredToPlot = 2;

                AddPlot(new Stroke(Brushes.Black, 5), PlotStyle.Bar, "XV");

                AddLine(new Stroke(Brushes.Gray, DashStyleHelper.DashDotDot, 1), 125 * m_VolumeLevelMultiplier, "SlowLow");
                AddLine(new Stroke(Brushes.Red, DashStyleHelper.DashDotDot, 1), 250 * m_VolumeLevelMultiplier, "SlowMid");
                AddLine(new Stroke(Brushes.Black, DashStyleHelper.DashDotDot, 1), 500 * m_VolumeLevelMultiplier, "SlowHigh");
                AddLine(new Stroke(Brushes.Black, DashStyleHelper.DashDot, 1), 1000 * m_VolumeLevelMultiplier, "SlowExtreme");
                AddLine(new Stroke(Brushes.Blue, DashStyleHelper.DashDotDot, 1), 1500 * m_VolumeLevelMultiplier, "FastLow");
                AddLine(new Stroke(Brushes.Blue, DashStyleHelper.DashDot, 2), 2500 * m_VolumeLevelMultiplier, "FastMid");
                AddLine(new Stroke(Brushes.Blue, DashStyleHelper.DashDot, 2), 5000 * m_VolumeLevelMultiplier, "FastHigh");
                AddLine(new Stroke(Brushes.Blue, DashStyleHelper.DashDotDot, 2), 7500 * m_VolumeLevelMultiplier, "FastExtreme1");
                AddLine(new Stroke(Brushes.Blue, DashStyleHelper.Solid, 3), 10000 * m_VolumeLevelMultiplier, "FastExtreme2");



            }


        }

        private void SetColorState()
        {
            bool increase = (Volume[0] > Volume[1]) ? true : false;

            Brush clr = PlotBrushes[0][0];

            if (clr == Brushes.Black) VolumeColorState = (increase) ?
                xVolumeColorStateEnums.IncreasingBlack : xVolumeColorStateEnums.DecreasinBlack;
            else if (clr == Brushes.Red) VolumeColorState = (increase) ?
                xVolumeColorStateEnums.IncreasingRed : xVolumeColorStateEnums.DecreasingRed;

            else if (clr == Brushes.DarkSlateBlue) VolumeColorState = (increase) ?
                xVolumeColorStateEnums.IncreasingStichBlack : xVolumeColorStateEnums.DecreasingStichBlack;
            else if (clr == Brushes.LightSlateGray) VolumeColorState = (increase) ?
                xVolumeColorStateEnums.IncreasingReversalStichBlack : xVolumeColorStateEnums.DecreasingReversalStichBlack;

            else if (clr == Brushes.IndianRed) VolumeColorState = (increase) ?
                xVolumeColorStateEnums.IncreasingStitchRed : xVolumeColorStateEnums.DecreasingStitchRed;
            else if (clr == Brushes.OrangeRed) VolumeColorState = (increase) ?
                xVolumeColorStateEnums.IncreasingReversalStitchRed : xVolumeColorStateEnums.DecreasingReversalStitchRed;

            else if (clr == Brushes.Orange) VolumeColorState = (increase) ?
                xVolumeColorStateEnums.DecreasingReversalRed : xVolumeColorStateEnums.DecreasingReversalRed;
            else if (clr == Brushes.Gray) VolumeColorState = (increase) ?
                xVolumeColorStateEnums.IncreasingReversalBlack : xVolumeColorStateEnums.DecreasingReversalBlack;

            else if (clr == Brushes.Lime) VolumeColorState = (increase) ?
                xVolumeColorStateEnums.IncreasingOutSideBlack : xVolumeColorStateEnums.DecreasingOutSideBlack;
            else if (clr == Brushes.Fuchsia) VolumeColorState = (increase) ?
                xVolumeColorStateEnums.IncreasingOutSideRed : xVolumeColorStateEnums.IncreasingOutSideRed;

            else if (clr == Brushes.DarkGray) VolumeColorState = xVolumeColorStateEnums.InsideBlack;
            else if (clr == Brushes.LightGray) VolumeColorState = xVolumeColorStateEnums.InsideRed;



            else VolumeColorState = xVolumeColorStateEnums.Undefined;

            VolumeColorStateSeries[0] = (int)VolumeColorState;

            string vcolorstr = xEnumDescription.GetEnumDescription(VolumeColorState);

            if (string.IsNullOrEmpty(VolumeColorSequence)) VolumeColorSequence = vcolorstr;
            else
            {
                string a = PrevVolumeColorstring.ToLower();
                string b = vcolorstr.Replace("+", "").Replace("-", "").Replace("*", "").Replace("^", "").ToLower();

                //if (a.Contains(b)) VolumeColorSequence += vcolorstr;
                //else VolumeColorSequence = vcolorstr;

                VolumeColorSequenceSeries[0] = VolumeColorSequence;
            }

            PrevVolumeColorstring = VolumeColorSequence;
        }

        private void SetVolumeColorByUpDownSideMethod(xBarInformation bi, bool stitchuds, bool outsideuds,
                                                        bool highvol)
        {
            xBarInformation m_PrevBarType = xPriceVolumeFunctions().GetBarInformation(CurrentBar - 2, CurrentBar - 1);

            if ((highvol) && (Volume[0] < Volume[1]))
            {
                if ((bi.BarType == xBarTypesEnum.HigherHighHigherLow) ||
                    (bi.BarType == xBarTypesEnum.StitchLong)) PlotBrushes[0][0] = Brushes.RoyalBlue;
                else if ((bi.BarType == xBarTypesEnum.LowerLowLowerHigh) ||
                    (bi.BarType == xBarTypesEnum.StitchShort)) PlotBrushes[0][0] = Brushes.DarkRed;
                else PlotBrushes[0][0] = Brushes.LightGray;

                return;
            }

            bool lc = (bi.BarCloseType == xBarCloseTypesEnums.LowerClose) ? true : false;
            bool hc = (bi.BarCloseType == xBarCloseTypesEnums.HigherClose) ? true : false;
            bool eq = (bi.BarCloseType == xBarCloseTypesEnums.EqualClose) ? true : false;

            Brush[] stlclruds = { Brushes.DarkSlateBlue, Brushes.LightSlateGray };
            Brush[] stlclrhl = { Brushes.Black, Brushes.Gray };

            Brush[] stlclr = (stitchuds) ? stlclruds : stlclrhl;

            Brush[] stsclruds = { Brushes.IndianRed, Brushes.OrangeRed };
            Brush[] stsclrhl = { Brushes.Red, Brushes.Orange };

            Brush[] stsclr = (stitchuds) ? stsclruds : stsclrhl;

            Brush[] osclruds = { Brushes.Lime, Brushes.Fuchsia };
            Brush[] osclrhl = { Brushes.Black, Brushes.Red };

            Brush[] osclr = (outsideuds) ? osclruds : osclrhl;

            switch (bi.BarType)
            {
                case xBarTypesEnum.HigherHighHigherLow:
                    PlotBrushes[0][0] = ((hc) || (eq)) ? Brushes.Black : Brushes.Gray;
                    break;

                case xBarTypesEnum.StitchLong:
                    PlotBrushes[0][0] = ((hc) || (eq)) ? stlclr[0] : stlclr[1];
                    break;

                case xBarTypesEnum.LowerLowLowerHigh:
                    PlotBrushes[0][0] = ((lc) || (eq)) ? Brushes.Red : Brushes.Orange;
                    break;

                case xBarTypesEnum.StitchShort:
                    PlotBrushes[0][0] = ((lc) || (eq)) ? stsclr[0] : stsclr[1];
                    break;

                case xBarTypesEnum.FlatTopPennant:
                case xBarTypesEnum.FlatBottomPennant:
                case xBarTypesEnum.Hitch:
                case xBarTypesEnum.Inside:
                    PlotBrushes[0][0] = ((bi.BarBodyType == xBarBodyTypesEnums.Green) ||
                                (bi.BarBodyType == xBarBodyTypesEnums.Doji)) ? Brushes.DarkGray : Brushes.LightGray;

                    if (Volume[0] >= Volume[1])
                    {
                        if ((m_PrevBarType.BarType == xBarTypesEnum.HigherHighHigherLow) ||
                                (m_PrevBarType.BarType == xBarTypesEnum.StitchLong))
                            PlotBrushes[0][0] = Brushes.Black;
                        else if ((m_PrevBarType.BarType == xBarTypesEnum.LowerLowLowerHigh) ||
                                (m_PrevBarType.BarType == xBarTypesEnum.StitchShort))
                            PlotBrushes[0][0] = Brushes.Red;
                        else
                            PlotBrushes[0][0] = PlotBrushes[0][1];
                    }
                    break;

                case xBarTypesEnum.OutSideBarLong:
                    PlotBrushes[0][0] = osclr[0];
                    break;

                case xBarTypesEnum.OutSideBarShort:
                    PlotBrushes[0][0] = osclr[1];
                    break;

                default:
                    break;
            }
        }

        private void SetVolumeColorByHighLowMethod(xBarInformation bi, bool highVol)
        {
            if ((highVol) && (Volume[0] < Volume[1]))
            {
                PlotBrushes[0][0] = Brushes.LightGray;
                return;
            }

            switch (bi.BarType)
            {
                case xBarTypesEnum.HigherHighHigherLow:
                case xBarTypesEnum.StitchLong:
                    PlotBrushes[0][0] = Brushes.Black;
                    break;

                case xBarTypesEnum.LowerLowLowerHigh:
                case xBarTypesEnum.StitchShort:
                    PlotBrushes[0][0] = Brushes.Red;
                    break;

                case xBarTypesEnum.FlatTopPennant:
                case xBarTypesEnum.FlatBottomPennant:
                case xBarTypesEnum.Hitch:
                case xBarTypesEnum.Inside:
                    PlotBrushes[0][0] = ((bi.BarBodyType == xBarBodyTypesEnums.Green) ||
                                (bi.BarBodyType == xBarBodyTypesEnums.Doji)) ? Brushes.Black : Brushes.Red;
                    break;

                case xBarTypesEnum.OutSideBarLong:
                    PlotBrushes[0][0] = Brushes.Black;
                    break;

                case xBarTypesEnum.OutSideBarShort:
                    PlotBrushes[0][0] = Brushes.Red;
                    break;

                default:
                    PlotBrushes[0][0] = Brushes.Black;
                    break;
            }
        }

        private void SetVolumeColorByCloseToCloseMethod(xBarInformation bi)
        {
            switch (bi.BarCloseType)
            {
                case xBarCloseTypesEnums.HigherClose:
                    PlotBrushes[0][0] = Brushes.Black;
                    break;

                case xBarCloseTypesEnums.LowerClose:
                    PlotBrushes[0][0] = Brushes.Red;
                    break;

                default:
                    PlotBrushes[0][0] = Brushes.Black;
                    break;
            }
        }

        private void SetVolumeColorByBarHighLowVelocity(xBarInformation bi)
        {
            double hlavg = (High[0] + Low[0]) / 2;

            if (VolumeVelSeries.Count == 0) VolumeVelSeries[0] = (hlavg - TickSize);
            else VolumeVelSeries[0] = (hlavg - m_PrevHLAvg);

            if (VolumeVelSeries.Count > 1)
            {
                VolumeAccSeries[0] = VolumeVelSeries[0] - VolumeVelSeries[1];
                VolumeBarColor[0] = this.SMA(VolumeVelSeries, 5)[0];
            }

            if (VolumeBarColor[0] > 0)
            {
                PlotBrushes[0][0] = (VolumeBarColor[0] > VolumeBarColor[1]) ? Brushes.Black : Brushes.Gray;
            }
            else if (VolumeBarColor[0] < 0)
            {
                PlotBrushes[0][0] = (VolumeBarColor[0] < VolumeBarColor[1]) ? Brushes.Red : Brushes.Gray;
            }

            m_PrevHLAvg = hlavg;
        }

        private void SetVolatilityColorByOpenCloseMethod()
        {
            double diff = (Close[0] - Open[0]) / TickSize;
            double diffabs = Math.Abs(diff);

            VolumeBarColor[0] = diffabs;

            if (diff == 0) PlotBrushes[0][0] = Brushes.LightGray;
            else if (diffabs == VolumeBarColor[1]) PlotBrushes[0][0] = Brushes.LightGray;
            else if ((diffabs > VolumeBarColor[1]) && (diff > 0)) PlotBrushes[0][0] = Brushes.Black;
            else if ((diffabs > VolumeBarColor[1]) && (diff < 0)) PlotBrushes[0][0] = Brushes.Red;
            else PlotBrushes[0][0] = Brushes.LightGray;
        }

        private void SetVolatilityColorByHighLowUpMethod()
        {
            double diff = (High[0] - Low[0]) / TickSize;
            double diffclose = (Close[0] - Open[0]) / TickSize;
            bool diffcloseup = (Close[0] > Close[1]);
            bool diffclosedn = (Close[1] > Close[0]);

            VolumeBarColor[0] = Math.Abs(diff);

            if (diffclose == 0) PlotBrushes[0][0] = Brushes.LightGray;
            else if (Math.Abs(diff) == VolumeBarColor[1]) PlotBrushes[0][0] = Brushes.LightGray;
            else if ((diffclose > 0) && (VolumeBarColor[1] < Math.Abs(diff)) && (diffcloseup)) PlotBrushes[0][0] = Brushes.Black;
            else if ((diffclose > 0) && (VolumeBarColor[1] > Math.Abs(diff))) PlotBrushes[0][0] = Brushes.LightGray;
            else if ((diffclose < 0) && (VolumeBarColor[1] < Math.Abs(diff)) && (diffclosedn)) PlotBrushes[0][0] = Brushes.Red;
            else if ((diffclose < 0) && (VolumeBarColor[1] > Math.Abs(diff))) PlotBrushes[0][0] = Brushes.LightGray;

        }

        private void SetVolatilityColorByCloseToCloseMethod()
        {
            double diff = Close[0] - Close[1];

            double diffval = Math.Abs(diff / TickSize);

            VolumeBarColor[0] = diffval;

            if ((diff < 0) && (VolumeBarColor[1] == diffval) && (m_PrevBarColor == Brushes.Red)) PlotBrushes[0][0] = Brushes.Red;
            else if ((diff > 0) && (VolumeBarColor[1] == diffval) && (m_PrevBarColor == Brushes.Black)) PlotBrushes[0][0] = Brushes.Black;
            else if (diff == 0) PlotBrushes[0][0] = Brushes.LightGray;
            else if (diffval == VolumeBarColor[1]) PlotBrushes[0][0] = Brushes.LightGray;
            else if ((diff > 0) && (VolumeBarColor[1] < diffval)) PlotBrushes[0][0] = Brushes.Black;
            else if ((diff > 0) && (VolumeBarColor[1] > diffval)) PlotBrushes[0][0] = Brushes.LightGray;
            else if ((diff < 0) && (VolumeBarColor[1] < diffval)) PlotBrushes[0][0] = Brushes.Red;
            else if ((diff < 0) && (VolumeBarColor[1] > diffval)) PlotBrushes[0][0] = Brushes.LightGray;

            m_PrevBarColor = PlotBrushes[0][0];
        }

        private void SetVolumeColorByCloseToCloseStrictMethod(xBarInformation bi)
        {
            bool trueUp = (Close[0] > Close[1]) && (Close[0] > Open[1]);
            bool trueDown = (Close[0] < Close[1]) && (Close[0] < Open[1]);

            if (trueUp) PlotBrushes[0][0] = Brushes.Black;
            else if (trueDown) PlotBrushes[0][0] = Brushes.Red;
            else PlotBrushes[0][0] = Brushes.Gray;
        }

        private void SetVolumeColorByCloseToCloseStrictHighMethod(xBarInformation bi)
        {
            /*bool trueUp = (Close[0] > Close[1]) && (Close[0] > Open[1]) && (Close[0] > High[1]);
			bool trueDown = (Close[0] < Close[1]) && (Close[0] < Open[1]) && (Close[0] < Low[1]);
			bool volHigh = (Volume[0] > Volume[1]);
			
			if ((trueUp) && (volHigh)) PlotColors[0][0] = Color.Black;
			else if ((trueDown) && (volHigh)) PlotColors[0][0] = Color.Red;
			else PlotColors[0][0] = Color.LightGray;*/

            bool trueUp = (Close[0] > Close[1]) && (Close[0] > Open[1]);
            bool falseUp = (!trueUp) && (High[0] > High[1]);
            bool trueDown = (Close[0] < Close[1]) && (Close[0] < Open[1]);
            bool falseDown = (!trueDown) && (Low[0] < Low[1]);
            bool volHigh = (Volume[0] > Volume[1]);

            if ((volHigh) && (trueUp)) PlotBrushes[0][0] = Brushes.Black;
            else if ((volHigh) && (trueDown)) PlotBrushes[0][0] = Brushes.Red;
            else if ((!volHigh) && (trueUp)) PlotBrushes[0][0] = Brushes.LightGray;
            else if ((!volHigh) && (trueDown)) PlotBrushes[0][0] = Brushes.LightGray;
            else if ((volHigh) && (falseUp)) PlotBrushes[0][0] = Brushes.MediumBlue;
            else if ((volHigh) && (falseDown)) PlotBrushes[0][0] = Brushes.Fuchsia;
            else PlotBrushes[0][0] = Brushes.Gray;
        }

        private void SetVolumeColorByUpDownTrueMethod(xBarInformation bi)
        {

            xLateralStateEnums state = xLateral()[CurrentBar];

            switch (state)
            {
                case xLateralStateEnums.BROKEN_ABOVE:
                    PlotBrushes[0][0] = (Volume[0] > Volume[1]) ? Brushes.Black : Brushes.LightGray;
                    //Print(CurrentBar + "\t" + "Broken Above");
                    break;

                case xLateralStateEnums.BROKEN_BELOW:
                    PlotBrushes[0][0] = (Volume[0] > Volume[1]) ? Brushes.Red : Brushes.LightGray;
                    //Print(CurrentBar + "\t" + "Broken Below");
                    break;

                case xLateralStateEnums.INTACT:
                    PlotBrushes[0][0] = Brushes.LightGray;
                    //Print(CurrentBar + "\t" + "Intact");
                    break;

                case xLateralStateEnums.NO_STATE:
                    bool xblack = (((bi.BarType == xBarTypesEnum.HigherHighHigherLow) ||
                                    (bi.BarType == xBarTypesEnum.StitchLong)) &&
                                    (bi.BarCloseType == xBarCloseTypesEnums.HigherClose)) ? true : false;

                    bool xred = (((bi.BarType == xBarTypesEnum.LowerLowLowerHigh) ||
                                    (bi.BarType == xBarTypesEnum.StitchShort)) &&
                                    (bi.BarCloseType == xBarCloseTypesEnums.LowerClose)) ? true : false;

                    bool incvol = (Volume[0] > Volume[1]) ? true : false;

                    bool reverseRed = (Close[0] < Open[0]) ? true : false;
                    bool reverseBlack = (Close[0] > Open[0]) ? true : false;

                    bool black = (xblack && incvol && (!reverseRed)) ? true : false;
                    bool red = (xred && incvol && (!reverseBlack)) ? true : false;

                    Brush clr = (black) ? Brushes.Black : (red ? Brushes.Red : Brushes.Gray);

                    PlotBrushes[0][0] = clr;

                    //Print(CurrentBar + "\t" + "No State");
                    break;

                default:
                    break;

            }
        }

        private void SetVolumeColorByUpDownTrueMethodEx(xBarInformation bi)
        {

            xLateralStateEnums state = xLateralEx()[CurrentBar];

            switch (state)
            {
                case xLateralStateEnums.BROKEN_ABOVE:
                    PlotBrushes[0][0] = (Volume[0] > Volume[1]) ? Brushes.Black : Brushes.LightGray;
                    break;

                case xLateralStateEnums.BROKEN_BELOW:
                    PlotBrushes[0][0] = (Volume[0] > Volume[1]) ? Brushes.Red : Brushes.LightGray;
                    break;

                case xLateralStateEnums.INTACT:
                    PlotBrushes[0][0] = Brushes.LightGray;
                    break;

                case xLateralStateEnums.NO_STATE:
                    bool xblack = (((bi.BarType == xBarTypesEnum.HigherHighHigherLow) ||
                                    (bi.BarType == xBarTypesEnum.StitchLong)) &&
                                    (bi.BarCloseType == xBarCloseTypesEnums.HigherClose)) ? true : false;

                    bool xred = (((bi.BarType == xBarTypesEnum.LowerLowLowerHigh) ||
                                    (bi.BarType == xBarTypesEnum.StitchShort)) &&
                                    (bi.BarCloseType == xBarCloseTypesEnums.LowerClose)) ? true : false;

                    bool incvol = (Volume[0] > Volume[1]) ? true : false;

                    bool reverseRed = (Close[0] < Open[0]) ? true : false;
                    bool reverseBlack = (Close[0] > Open[0]) ? true : false;

                    bool black = (xblack && incvol && (!reverseRed)) ? true : false;
                    bool red = (xred && incvol && (!reverseBlack)) ? true : false;

                    Brush clr = (black) ? Brushes.Black : (red ? Brushes.Red : Brushes.Gray);

                    PlotBrushes[0][0] = clr;


                    break;

                default:
                    break;

            }
        }

        private void SetVolumeColorOpenToClose(xBarInformation bi)
        {
            if (Close[0] > Open[0]) PlotBrushes[0][0] = Brushes.Black;
            else if (Close[0] < Open[0]) PlotBrushes[0][0] = Brushes.Red;
            else if (Close[0] == Open[0])
            {
                if (High[0] > High[1]) PlotBrushes[0][0] = Brushes.Black;
                else if (Low[1] > Low[0]) PlotBrushes[0][0] = Brushes.Red;
                else PlotBrushes[0][0] = Brushes.Black;
            }
        }

        private void SetVolumeColorByUpDownTrueStrictMethod(xBarInformation bi)
        {

        }

        private void SetVolumeColorByUpDownTrueClose(xBarInformation bi)
        {
            if ((Close[0] > Close[1]) && (Close[0] > Open[1]) && (Volume[0] > Volume[1])) PlotBrushes[0][0] = Brushes.Black;
            else if ((Close[0] < Close[1]) && (Close[0] < Open[1]) && (Volume[0] > Volume[1])) PlotBrushes[0][0] = Brushes.Red;
            else PlotBrushes[0][0] = Brushes.Gray;

        }

        private void SetVolumeColorByUpDownSideStitchHighSeperateColor(xVolumeColorShowEnums clrshw)
        {
            if ((Close[0] > Close[1]) && (Volume[0] > Volume[1]) && (clrshw == xVolumeColorShowEnums.Black))
                PlotBrushes[0][0] = Brushes.Black;
            else if ((Close[0] < Close[1]) && (Volume[0] > Volume[1]) && (clrshw == xVolumeColorShowEnums.Red))
                PlotBrushes[0][0] = Brushes.Red;
            else PlotBrushes[0][0] = Brushes.White;
        }

        private void SetVolumeIntraBar(xBarInformation bi, xVolumeTypesEnums vt)
        {
            double nonDominantLegDistance = 0, dominantLegDistance, nonDominantVolume, dominantVolume;
            SolidColorBrush nonDominantColor = Brushes.Black;
            SolidColorBrush dominantColor = Brushes.Black;

            dominantLegDistance = Math.Abs(High[0] - Low[0]);

            switch (bi.BarType)
            {
				case xBarTypesEnum.HigherHighHigherLow:
                    nonDominantLegDistance = High[1] - Low[0];
                    nonDominantColor = Brushes.Red;
                break;

                case xBarTypesEnum.LowerLowLowerHigh:
                    nonDominantLegDistance = High[0] - Low[1];
                    nonDominantColor = Brushes.Black;
                break;

                case xBarTypesEnum.StitchLong:
                    nonDominantLegDistance = High[1] - Low[0];
                    nonDominantColor = Brushes.Black;
                break;
				
				case xBarTypesEnum.StitchShort:
                    nonDominantLegDistance = High[0] - Low[1];
                    nonDominantColor = Brushes.Red;
                break;
					
				case xBarTypesEnum.FlatTopPennant:
                    nonDominantLegDistance = High[1] - Low[0];
                    nonDominantColor = Brushes.Red;
                break;

                case xBarTypesEnum.FlatBottomPennant:
                    nonDominantLegDistance = High[0] - Low[1];
                    nonDominantColor = Brushes.Black;
                break;

				case xBarTypesEnum.Hitch:
                    nonDominantColor = ((bi.BarBodyType == xBarBodyTypesEnums.Green) ||
                                (bi.BarBodyType == xBarBodyTypesEnums.Doji)) ? Brushes.Black : Brushes.Red;
                    break;

				case xBarTypesEnum.Inside:
                    
                    if (bi.BarBodyType == xBarBodyTypesEnums.Green)
                    {
                        nonDominantLegDistance = High[1] - Low[0];
                        nonDominantColor = Brushes.Red;
                    }
                    else
                    {
                        nonDominantLegDistance = High[0] - Low[1];
                        nonDominantColor = Brushes.Black;
                    }
                break;
					
				case xBarTypesEnum.OutSideBarLong:
                    nonDominantLegDistance = High[1] - Low[0];
                    nonDominantColor = Brushes.Red;
                break;
					
				case xBarTypesEnum.OutSideBarShort:
                    nonDominantLegDistance = High[0] - Low[1];
                    nonDominantColor = Brushes.Black;
                break;

                default:
					nonDominantColor = Brushes.Black;
                break;
            }

            dominantColor = (nonDominantColor == Brushes.Red) ? Brushes.Black : Brushes.Red;

            nonDominantVolume = (nonDominantLegDistance / (nonDominantLegDistance + dominantLegDistance)) * Volume[0];
            dominantVolume = (dominantLegDistance / (nonDominantLegDistance + dominantLegDistance)) * Volume[0];

            if (vt == xVolumeTypesEnums.IntarBarNonDominant)
            {
                VolumeBarColor[0] = nonDominantVolume;
                PlotBrushes[0][0] = nonDominantColor;
            }
            else if (vt == xVolumeTypesEnums.IntraBarDominant)
            {
                VolumeBarColor[0] = dominantVolume;
                PlotBrushes[0][0] = dominantColor;
            }
        }
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
            // Use this method for calculating your indicator values. Assign a value to each
            // plot below by replacing 'Close[0]' with your own formula.
			
			if (CurrentBar < 1) return;
			
			try
			{
				VolumeBarColor[0] = Volume[0];
				
				xBarInformation barinfo = xPriceVolumeFunctions().GetBarInformation(CurrentBar-1,CurrentBar);
				
				switch(Type)
				{
					case xVolumeTypesEnums.UpDownSide:
						SetVolumeColorByUpDownSideMethod(barinfo, true, true, false);
					break;
						
					case xVolumeTypesEnums.UpDownSideStitch:
						SetVolumeColorByUpDownSideMethod(barinfo, false, true, false);
					break;
						
					case xVolumeTypesEnums.UpDownSideStitchHigh:
						SetVolumeColorByUpDownSideMethod(barinfo, false, true,true);
					break;
						
					case xVolumeTypesEnums.UpDownSideStitchOutSide:
						SetVolumeColorByUpDownSideMethod(barinfo, false, false, false);
					break;
						
					case xVolumeTypesEnums.HighLow:
						SetVolumeColorByHighLowMethod(barinfo, false);
					break;
						
					case xVolumeTypesEnums.HighLowHigh:
						SetVolumeColorByHighLowMethod(barinfo, true);
					break;
						
					case xVolumeTypesEnums.CloseToClose:
						SetVolumeColorByCloseToCloseMethod(barinfo);
					break;
						
					case xVolumeTypesEnums.CloseToCloseStrict:
						SetVolumeColorByCloseToCloseStrictMethod(barinfo);
					break;
						
					case xVolumeTypesEnums.CloseToCloseStrictHigh:
						SetVolumeColorByCloseToCloseStrictHighMethod(barinfo);
					break;
						
					case xVolumeTypesEnums.BarVolatilityCloseToClose:
						SetVolatilityColorByCloseToCloseMethod();
					break;
						
					case xVolumeTypesEnums.BarVolatilityHighLowUp:
						SetVolatilityColorByHighLowUpMethod();
					break;
						
					case xVolumeTypesEnums.BarVolatilityOpenCloseUp:
						SetVolatilityColorByOpenCloseMethod();
					break;
						
					case xVolumeTypesEnums.UpDownTrue:
						SetVolumeColorByUpDownTrueMethod(barinfo);
					break;
						
					case xVolumeTypesEnums.UpDownTrueClose:
						SetVolumeColorByUpDownTrueClose(barinfo);
					break;
						
					case xVolumeTypesEnums.UpDownTrueEx:
						SetVolumeColorByUpDownTrueMethodEx(barinfo);
					break;
						
					case xVolumeTypesEnums.UpDownSideStitchHighBlack:
						SetVolumeColorByUpDownSideStitchHighSeperateColor(xVolumeColorShowEnums.Black);
					break;
						
					case xVolumeTypesEnums.UpDownSideStitchHighRed:
						SetVolumeColorByUpDownSideStitchHighSeperateColor(xVolumeColorShowEnums.Red);
					break;
						
					case xVolumeTypesEnums.BarHighLowVelocity:
						SetVolumeColorByBarHighLowVelocity(barinfo);
					break;
						
					case xVolumeTypesEnums.OpenToClose:
						SetVolumeColorOpenToClose(barinfo);
					break;

                    case xVolumeTypesEnums.IntarBarNonDominant:
                        SetVolumeIntraBar(barinfo, xVolumeTypesEnums.IntarBarNonDominant);
                    break;

                    case xVolumeTypesEnums.IntraBarDominant:
                        SetVolumeIntraBar(barinfo, xVolumeTypesEnums.IntraBarDominant);
                    break;
						
					default:
					break;
				}
				
				SetColorState();
			}
			catch(System.Exception e)
			{
				Print("Exception : " + CurrentBar + " " + e.ToString());
			}

        }
		
		#region Properties
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
		public Brush PlotColor
		{
			get 
			{ 
				Update(); 
				return PlotBrushes[0][0]; 
			}
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> VolumeBarColor
		{
			get { return Values[0]; }
		}
		
        [Description("")]
        [NinjaScriptProperty]
		[Display(Name="Volume Type", Order=1, GroupName="Parameters")]
        public xVolumeTypesEnums VolumeType
        {
            get { return Type; }
            set { Type = value; }
        }
		
		[Range(1, int.MaxValue)]
		[NinjaScriptProperty]
		[Display(Name="VolumeMultiplier", Order=2, GroupName="Parameters")]
		public double VolumeMultiplier
		{ 
			get { return m_VolumeLevelMultiplier; } 
			set { m_VolumeLevelMultiplier = value; } 
		}
		
		public xVolumeBarInformation this[int BarsAgo]
		{
			get
			{
				Update();
				
				if (BarsAgo < 0) return (new xVolumeBarInformation(-1, xVolumeColorStateEnums.Undefined, "", Brushes.Gray));
				else return (new xVolumeBarInformation(CurrentBar, 
						(xVolumeColorStateEnums)VolumeColorStateSeries[BarsAgo],VolumeColorSequenceSeries[BarsAgo], PlotBrushes[0][BarsAgo]));
			}
		}
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xVolume[] cachexVolume;
		public xVolume xVolume(xVolumeTypesEnums volumeType, double volumeMultiplier)
		{
			return xVolume(Input, volumeType, volumeMultiplier);
		}

		public xVolume xVolume(ISeries<double> input, xVolumeTypesEnums volumeType, double volumeMultiplier)
		{
			if (cachexVolume != null)
				for (int idx = 0; idx < cachexVolume.Length; idx++)
					if (cachexVolume[idx] != null && cachexVolume[idx].VolumeType == volumeType && cachexVolume[idx].VolumeMultiplier == volumeMultiplier && cachexVolume[idx].EqualsInput(input))
						return cachexVolume[idx];
			return CacheIndicator<xVolume>(new xVolume(){ VolumeType = volumeType, VolumeMultiplier = volumeMultiplier }, input, ref cachexVolume);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xVolume xVolume(xVolumeTypesEnums volumeType, double volumeMultiplier)
		{
			return indicator.xVolume(Input, volumeType, volumeMultiplier);
		}

		public Indicators.xVolume xVolume(ISeries<double> input , xVolumeTypesEnums volumeType, double volumeMultiplier)
		{
			return indicator.xVolume(input, volumeType, volumeMultiplier);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xVolume xVolume(xVolumeTypesEnums volumeType, double volumeMultiplier)
		{
			return indicator.xVolume(Input, volumeType, volumeMultiplier);
		}

		public Indicators.xVolume xVolume(ISeries<double> input , xVolumeTypesEnums volumeType, double volumeMultiplier)
		{
			return indicator.xVolume(input, volumeType, volumeMultiplier);
		}
	}
}

#endregion
