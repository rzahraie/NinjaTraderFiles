#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// Type of bar
    /// </summary>
    [Description("Type of bar")]
    public class xPriceVolumeFunctions : Indicator
    {
        #region Variables
        // Wizard generated variables
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void OnStateChange()
        {
            if (State == State.SetDefaults) 
   			{ 
				IsOverlay							= false;
				BarsRequiredToPlot  = 2; 
			}
        }
		
		public double GetPriceVolume(int Bar, xPriceVolumeTypeEnum pvtype)
		{	
			Update();
			
			if (Bar  > CurrentBar) 
			{
				Print("Bar is greater than CurrentBar");
				return 0;
			}
			
			switch(pvtype)
			{
				case xPriceVolumeTypeEnum.Open :
					return (Open[CurrentBar-Bar]);
				break;
					
				case xPriceVolumeTypeEnum.High :
					return (High[CurrentBar-Bar]);
				break;
					
				case xPriceVolumeTypeEnum.Low :
					return (Low[CurrentBar-Bar]);
				break;
					
				case xPriceVolumeTypeEnum.Close :
					return (Close[CurrentBar-Bar]);
				break;
					
				case xPriceVolumeTypeEnum.Typical :
					return (Typical[CurrentBar-Bar]);
				break;
					
				case xPriceVolumeTypeEnum.Mid :
					return (Median[CurrentBar-Bar]);
				break;
					
				case xPriceVolumeTypeEnum.HighLow :
					return (High[CurrentBar-Bar] - Low[CurrentBar-Bar]);
				break;
					
				case xPriceVolumeTypeEnum.Volume :
					return (Volume[CurrentBar-Bar]);
				break;
					
				default:
					return 0;
				break;
				
			}
		}
		
		private double GetOpenPrice(int Bar)
		{
			return (GetPriceVolume(Bar, xPriceVolumeTypeEnum.Open));
		}
		
		private double GetHighPrice(int Bar)
		{
			return (GetPriceVolume(Bar, xPriceVolumeTypeEnum.High));
		}
		
		private double GetLowPrice(int Bar)
		{
			return (GetPriceVolume(Bar, xPriceVolumeTypeEnum.Low));
		}
		
		private double GetClosePrice(int Bar)
		{
			return (GetPriceVolume(Bar, xPriceVolumeTypeEnum.Close));
		}
		
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="LeftBar">Bar-1</param>
		/// <param name="RightBar">Bar</param>
		/// <returns></returns>
		private xBarTypesEnum GetTypeOfBar(int LeftBar, int RightBar)
		{
			if ((GetHighPrice(RightBar) > GetHighPrice(LeftBar)) && 
				(GetLowPrice(RightBar) > GetLowPrice(LeftBar))) return xBarTypesEnum.HigherHighHigherLow;
			else if ((GetHighPrice(RightBar) < GetHighPrice(LeftBar)) && 
				(GetLowPrice(RightBar) < GetLowPrice(LeftBar))) return xBarTypesEnum.LowerLowLowerHigh;
			else if ((GetHighPrice(RightBar) == GetHighPrice(LeftBar)) && 
				(GetLowPrice(RightBar) > GetLowPrice(LeftBar))) return xBarTypesEnum.FlatTopPennant;
			else if ((GetHighPrice(RightBar) < GetHighPrice(LeftBar)) && 
				(GetLowPrice(RightBar) == GetLowPrice(LeftBar))) return xBarTypesEnum.FlatBottomPennant;
			else if ((GetHighPrice(LeftBar) < GetHighPrice(RightBar)) && 
				(GetLowPrice(LeftBar) == GetLowPrice(RightBar))) return xBarTypesEnum.StitchLong;
			else if ((GetLowPrice(LeftBar) > GetLowPrice(RightBar)) && 
				(GetHighPrice(LeftBar) == GetHighPrice(RightBar))) return xBarTypesEnum.StitchShort;
			else if ((GetHighPrice(RightBar) < GetHighPrice(LeftBar)) && 
				(GetLowPrice(RightBar) > GetLowPrice(LeftBar))) return xBarTypesEnum.Inside;
			else if ((GetHighPrice(RightBar) == GetHighPrice(LeftBar)) && 
				(GetLowPrice(RightBar) == GetLowPrice(LeftBar))) return xBarTypesEnum.Hitch;
			else if ((GetHighPrice(LeftBar) < GetHighPrice(RightBar)) && 
				(GetLowPrice(LeftBar) > GetLowPrice(RightBar)) && 
				((GetClosePrice(RightBar) >= GetClosePrice(LeftBar)))) return xBarTypesEnum.OutSideBarLong;
			else if ((GetHighPrice(LeftBar) < GetHighPrice(RightBar)) && 
				(GetLowPrice(LeftBar) > GetLowPrice(RightBar)) && 
				((GetClosePrice(RightBar) < GetClosePrice(LeftBar)))) return xBarTypesEnum.OutSideBarShort;
			else return xBarTypesEnum.Undefined;
			
		}
		
		private xBarCloseTypesEnums GetCloseTypeOfBar(int LeftBar, int RightBar)
		{
			if (GetClosePrice(LeftBar) > GetClosePrice(RightBar)) return xBarCloseTypesEnums.LowerClose;
			else if (GetClosePrice(LeftBar) < GetClosePrice(RightBar)) return xBarCloseTypesEnums.HigherClose;
			else if (GetClosePrice(LeftBar) == GetClosePrice(RightBar)) return xBarCloseTypesEnums.EqualClose;
			else return xBarCloseTypesEnums.Undefined;
		}
		
		private xBarBodyTypesEnums GetBodyTypeOfBar(int RightBar)
		{
			if (GetClosePrice(RightBar) > GetOpenPrice(RightBar)) return xBarBodyTypesEnums.Green;
			else if (GetClosePrice(RightBar) < GetOpenPrice(RightBar)) return xBarBodyTypesEnums.Red;
			else if (GetClosePrice(RightBar) == GetOpenPrice(RightBar)) return xBarBodyTypesEnums.Doji;
			else return xBarBodyTypesEnums.Undefined;	
		}
		
		private xBarPartTypesEnums GetBarPartType(int RightBar)
		{
			bool OCDoji = ((Close[0] == High[0]) && (High[0] == Open[0])); //TwoPartDojiHigh
			bool OCDownFull = ((Close[0] == Low[0]) && (High[0] == Open[0])); //OnePartFullLow
			bool OC2H = (Open[0] == High[0]); //TwoPartOpenHigh
			bool OC2L = (Close[0] == Low[0]); //TwoPartCloseLow
			
			bool CODoji = ((Open[0] == Low[0]) && (Low[0] == Close[0]));  //TwoPartDojiLow
			bool COUpFull = ((Open[0] == Low[0]) && (High[0] == Close[0]));  //OnePartFullHIgh
			bool CO2L = (Open[0] == Low[0]);  //TwoPartOpenLow
			bool CO2H = (Close[0] == High[0]); //TwoPartCloseHigh
			
			if (OCDoji) return xBarPartTypesEnums.TwoPartDojiHigh;
			else if (OCDownFull) return xBarPartTypesEnums.OnePartFullLow;
			else if (OC2H) return xBarPartTypesEnums.TwoPartOpenHigh;
			else if (OC2L) return xBarPartTypesEnums.TwoPartCloseLow;
			else if (CODoji) return xBarPartTypesEnums.TwoPartDojiLow;
			else if (COUpFull) return xBarPartTypesEnums.OnePartFullHigh;
			else if (CO2L) return xBarPartTypesEnums.TwoPartOpenLow;
			else if (CO2H) return xBarPartTypesEnums.TwoPartCloseHigh;
			else if (Open[0] == Close[0]) return xBarPartTypesEnums.ThreePartDoji;
			else if (Close[0] > Open[0]) return xBarPartTypesEnums.ThreePartHigh;
			else if (Open[0] > Close[0]) return xBarPartTypesEnums.ThreePartLow;
			else return xBarPartTypesEnums.BarPartUnknown;
		}
		
		public xBarInformation GetBarInformation(int LeftBar, int RightBar)
		{
			xBarTypesEnum bartype = GetTypeOfBar(LeftBar, RightBar);
			
			xBarCloseTypesEnums closetype = GetCloseTypeOfBar(LeftBar, RightBar);
			
			xBarBodyTypesEnums bodytype = GetBodyTypeOfBar(RightBar);
			
			xBarPartTypesEnums parttype = GetBarPartType(RightBar);
			
			return (new xBarInformation(RightBar, bartype , closetype, bodytype, parttype));
		}
		
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
            // Use this method for calculating your indicator values. Assign a value to each
            // plot below by replacing 'Close[0]' with your own formula.
			
			
			
        }

        #region Properties
		public xBarInformation this[int Bar]
		{
			get
			{
				Update();
				
				return (GetBarInformation(Bar-1, Bar));
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
		private xPriceVolumeFunctions[] cachexPriceVolumeFunctions;
		public xPriceVolumeFunctions xPriceVolumeFunctions()
		{
			return xPriceVolumeFunctions(Input);
		}

		public xPriceVolumeFunctions xPriceVolumeFunctions(ISeries<double> input)
		{
			if (cachexPriceVolumeFunctions != null)
				for (int idx = 0; idx < cachexPriceVolumeFunctions.Length; idx++)
					if (cachexPriceVolumeFunctions[idx] != null &&  cachexPriceVolumeFunctions[idx].EqualsInput(input))
						return cachexPriceVolumeFunctions[idx];
			return CacheIndicator<xPriceVolumeFunctions>(new xPriceVolumeFunctions(), input, ref cachexPriceVolumeFunctions);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xPriceVolumeFunctions xPriceVolumeFunctions()
		{
			return indicator.xPriceVolumeFunctions(Input);
		}

		public Indicators.xPriceVolumeFunctions xPriceVolumeFunctions(ISeries<double> input )
		{
			return indicator.xPriceVolumeFunctions(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xPriceVolumeFunctions xPriceVolumeFunctions()
		{
			return indicator.xPriceVolumeFunctions(Input);
		}

		public Indicators.xPriceVolumeFunctions xPriceVolumeFunctions(ISeries<double> input )
		{
			return indicator.xPriceVolumeFunctions(input);
		}
	}
}

#endregion
