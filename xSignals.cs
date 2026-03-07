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
	public class xSignals : Indicator
	{
		#region
		private xVolumeTypesEnums m_xVolumeTypesEnums = xVolumeTypesEnums.UpDownSide;
        private List<string> m_redSeqList = new List<string>();
        private List<string> m_blackSeqList = new List<string>();
        private Brush m_PrevBarColor = Brushes.Gray;
        #endregion

        private void IsPeakVolume()
        {
            xVolumeBarInformation xvb0 = xVolume(m_xVolumeTypesEnums, 1)[0];
            xVolumeBarInformation xvb1 = xVolume(m_xVolumeTypesEnums, 1)[1];
            xVolumeBarInformation xvb2 = xVolume(m_xVolumeTypesEnums, 1)[2];

            bool increasingVolume = false;
            bool redColor = false;
            bool blackColor = false;
            bool peakExtreme = false;

            if ((Volume[1] > Volume[2]) && (Volume[0] > Volume[2])) increasingVolume = true;

            if (Volume[0] >= 2*Volume[1]) peakExtreme = true;

            if ((xvb2.VolumeBarColor == Brushes.Black) && (xvb1.VolumeBarColor == Brushes.Black) && (xvb0.VolumeBarColor == Brushes.Black)) blackColor = true;

            if ((xvb2.VolumeBarColor == Brushes.Red) && (xvb1.VolumeBarColor == Brushes.Red) && (xvb0.VolumeBarColor == Brushes.Red)) redColor = true;

            if ((increasingVolume) && (blackColor))
            {
                if (peakExtreme) { }
                else { }
            }

            if ((increasingVolume) && (redColor))
            {
                if (peakExtreme) { }
                else { }
            }
        }

        private void IsGoldenSequence()
        {
            xVolumeBarInformation xvb0 = xVolume(m_xVolumeTypesEnums, 1)[0];
            xVolumeBarInformation xvb1 = xVolume(m_xVolumeTypesEnums, 1)[1];
			xVolumeBarInformation xvb2 = xVolume(m_xVolumeTypesEnums, 1)[2];
			xVolumeBarInformation xvb3 = xVolume(m_xVolumeTypesEnums, 1)[3];
			xVolumeBarInformation xvb4 = xVolume(m_xVolumeTypesEnums, 1)[4];
			
		

            //if ((xvb0.VolumeBarColor == Brushes.Black) && (xvb1.VolumeBarColor == Brushes.Black)) m_blackSeqList.Add()
			
			

            
        }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Volume based signals";
				Name										= "xSignals";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
			}
			else if (State == State.Configure)
			{
			}
		}

        #region Properties
        [Description("")]
        [NinjaScriptProperty]
        [Display(Name = "Volume Type", Order = 1, GroupName = "Parameters")]
        public xVolumeTypesEnums VolumeType
        {
            get { return m_xVolumeTypesEnums; }
            set { m_xVolumeTypesEnums = value; }
        }
        #endregion
		
		enum BarTypes {
			HIGHER_HIGH_HIGHER_LOW,
			LOWER_HIGH_LOWER_LOW,
			TRUE_HIGHER_HIGH_HIGHER_LOW,
			TRUE_LOWER_HIGH_LOWER_LOW,
			OUTSIDE_LONG,
			OUTSIDE_SHORT,
			UNDEFINED
		};
		
		enum BarBodyTypes {
			DOJI,
			HIGHER_CLOSE_THAN_OPEN,
			LOWER_CLOSE_THAN_OPEN
		};
		
		private BarTypes GetBarType(int barsAgo) {
			int rightBar = barsAgo;
			int leftBar = rightBar + 1; 
			
			if ((High[rightBar] > High[leftBar]) && 
				(Low[rightBar] > Low[leftBar]) && 
				(Close[rightBar] > Close[leftBar]) &&
				(Close[rightBar] > Open[rightBar]) &&
				(Close[rightBar] > Open[leftBar]) &&
				(Close[rightBar] > High[leftBar])) return BarTypes.TRUE_HIGHER_HIGH_HIGHER_LOW;
			else if ((High[rightBar] > High[leftBar]) && 
				(Low[rightBar] > Low[leftBar])) return BarTypes.HIGHER_HIGH_HIGHER_LOW;
			else if ((High[rightBar] < High[leftBar]) && 
				(Low[rightBar] < Low[leftBar]) &&
				(Close[rightBar] < Close[leftBar]) &&
				(Close[rightBar] < Open[rightBar]) &&
				(Close[rightBar] < Open[leftBar]) &&
				(Close[rightBar] < Low[leftBar])) return BarTypes.TRUE_LOWER_HIGH_LOWER_LOW;
			else if ((High[rightBar] < High[leftBar]) && 
				(Low[rightBar] < Low[leftBar])) return BarTypes.LOWER_HIGH_LOWER_LOW;
			else if ((High[leftBar] < High[rightBar]) && 
				(Low[leftBar] > Low[rightBar]) && 
				((Close[rightBar] >= Close[leftBar]))) return BarTypes.OUTSIDE_LONG;
			else if ((High[leftBar] < High[rightBar]) && 
				(Low[leftBar] > Low[rightBar]) && 
				((Close[rightBar] < Close[leftBar]))) return BarTypes.OUTSIDE_SHORT;
			else return BarTypes.UNDEFINED;
		}

		public bool IsTrueXferBlackBar(int barsAgo) {
			if ((GetBarType(barsAgo) == BarTypes.TRUE_HIGHER_HIGH_HIGHER_LOW || 
				GetBarType(barsAgo) == BarTypes.OUTSIDE_LONG)) {
					return true;
				}
				
			return false;
 		}
		
		public bool IsTrueXferRedBar(int barsAgo) {
			if ((GetBarType(barsAgo) == BarTypes.TRUE_LOWER_HIGH_LOWER_LOW || 
				GetBarType(barsAgo) == BarTypes.OUTSIDE_SHORT)) {
					return true;
				}
				
			return false;
 		}
		
		public bool IsTwoConsecutiveHighs() {
			if (IsTrueXferBlackBar(0) && IsTrueXferBlackBar(1)) {
				return true;
			}
			
			return false;
		}
		
		public bool IsTwoConsecutiveLows() {
			if (IsTrueXferRedBar(0) && IsTrueXferRedBar(1)) {
				return true;
			}
			
			return false;
		}
		
		public bool IsThreeConsecutiveHighs() {
			if (IsTrueXferBlackBar(0) && IsTrueXferBlackBar(1) && IsTrueXferBlackBar(2)) {
				return true;
			}
			
			return false;
		}
		
		public bool IsThreeConsecutiveLows() {
			if (IsTrueXferRedBar(0) && IsTrueXferRedBar(1) && IsTrueXferRedBar(2)) {
				return true;
			}
			
			return false;
		}
		
        protected override void OnBarUpdate()
		{
			PrintTo  = PrintTo.OutputTab2;
			
			if (CurrentBar < 4) return;
			int textOffset = 3;
			
			bool peakVolume = (Volume[0] > Volume[1] && Volume[1] > Volume[2]) ? true : false;
			bool incVolume = Volume[0] >= 1.9*Volume[1];
			bool cycleVolume = ((Volume[0] > Volume[1]) && (Volume[2] > Volume[1]) && (Volume[2] > Volume[3])) ? true : false;	
			
			
			//Print("CurrentBar : " + CurrentBar + " Type : " +  GetBarType(0) + "   True Volume: " +  cycleVolume);
			
			if ((IsTwoConsecutiveHighs()) && (peakVolume)) {
				Draw.Text(this, CurrentBar.ToString(), "Pa",0,High[0] + textOffset, Brushes.Black);
			} 
			else if ((IsTwoConsecutiveLows()) && (peakVolume)) {
				Draw.Text(this, CurrentBar.ToString(), "Pa",0,Low[0] - textOffset, Brushes.Red);
			}
			
			if (IsThreeConsecutiveHighs() && (cycleVolume)) {
				string volumeCyleText = (Volume[0] > Volume[2] ? "Vc+" : "Vc-");
				Draw.Text(this, CurrentBar.ToString(), volumeCyleText, 0,High[0] + textOffset, Brushes.Black);
			}
			else if (IsThreeConsecutiveLows() && (cycleVolume)) {
				string volumeCyleText = (Volume[0] > Volume[2] ? "Vc+" : "Vc-");
				Draw.Text(this, CurrentBar.ToString(),volumeCyleText, 0,Low[0] - textOffset, Brushes.Red);
			}
			else if (IsTrueXferBlackBar(0) && (incVolume)) {
				Draw.Text(this, CurrentBar.ToString(), "P",0,High[0] + textOffset, Brushes.Black);
			}
			else if (IsTrueXferRedBar(0) && (incVolume)) {
				Draw.Text(this, CurrentBar.ToString(), "P",0,Low[0] - textOffset, Brushes.Red);
			}
			
			//BarTypes bt = GetBarType(0);
			
			//
			//if ( bt == BarTypes.TRUE_HIGHER_HIGH_HIGHER_LOW) {
			//	Draw.Text(this, CurrentBar.ToString(), "Th",0,High[0] + textOffset, Brushes.Black);
			//}
			//Add your custom indicator logic here.
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xSignals[] cachexSignals;
		public xSignals xSignals(xVolumeTypesEnums volumeType)
		{
			return xSignals(Input, volumeType);
		}

		public xSignals xSignals(ISeries<double> input, xVolumeTypesEnums volumeType)
		{
			if (cachexSignals != null)
				for (int idx = 0; idx < cachexSignals.Length; idx++)
					if (cachexSignals[idx] != null && cachexSignals[idx].VolumeType == volumeType && cachexSignals[idx].EqualsInput(input))
						return cachexSignals[idx];
			return CacheIndicator<xSignals>(new xSignals(){ VolumeType = volumeType }, input, ref cachexSignals);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xSignals xSignals(xVolumeTypesEnums volumeType)
		{
			return indicator.xSignals(Input, volumeType);
		}

		public Indicators.xSignals xSignals(ISeries<double> input , xVolumeTypesEnums volumeType)
		{
			return indicator.xSignals(input, volumeType);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xSignals xSignals(xVolumeTypesEnums volumeType)
		{
			return indicator.xSignals(Input, volumeType);
		}

		public Indicators.xSignals xSignals(ISeries<double> input , xVolumeTypesEnums volumeType)
		{
			return indicator.xSignals(input, volumeType);
		}
	}
}

#endregion
