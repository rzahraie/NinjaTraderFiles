#region Using declarations
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    /// <summary>
    /// New Channel
    /// </summary>
    [Description("New Channel")]
    public class TChannelV6 : Indicator
    {
		#region Variables
		bool created = false;
		TRTL rtltest = null;
		TLTL ltltest = null;
		
		int m_Signal;
		
        // Wizard generated variables
        // User defined variables (add any user defined variables below)
		int m_CurrentBar;
		
		//All channels
		List<TChannel> m_Channels = new List<TChannel>();
		
		//Broken channels for each bar - cleared after processing
		List<TChannel> m_BrokenChannels = new List<TChannel>();
		
		//Remove unneeded join channels
		TChannel m_UnneededJoinChannel;
		
		TChannel m_DuplicateChannel;
		
		TChannel m_CurrentTape;
		
		
		
		TBar m_CurBar = new TBar();
		TBar m_PrevBar = new TBar();
		
		bool m_LogError = false;
		bool m_LogOutput = false;
		bool m_LogOutputTable = false;
		
		int m_StartBar;
		int m_EndBar;
		
		int m_BrokenDirection = -1;
		
		int m_TapeState = -1;
		
		int m_TickMultiplierHighLowDifference  = 2;
		
		int m_HighestLevelChannelToDraw = 4;
		
        #endregion
		
		#region LogOutPutFuncs
		public void Output(string s)
		{
			if (m_LogOutput) Print(s);	
		}
		
		public void Output(int i)
		{
			if (m_LogOutput) Print(i);	
		}
		
		public void Output(double d)
		{
			if (m_LogOutput) Print(d);	
		}
		
		public void Output(DateTime t)
		{
			if (m_LogOutput) Print(t);	
		}
		
		//
		public void OutputTable(string s)
		{
			if (m_LogOutputTable) Print(s);	
		}
		
		public void OutputTable(int i)
		{
			if (m_LogOutputTable) Print(i);	
		}
		
		public void OutputTable(double d)
		{
			if (m_LogOutputTable) Print(d);	
		}
		
		public void OutputTable(DateTime t)
		{
			if (m_LogOutputTable) Print(t);	
		}
		//
		
		public void LogError(string s)
		{
			if (m_LogError) Print("ERROR : " + s);	
		}
		
		public void LogError(int i)
		{
			if (m_LogError) Print("ERROR : " + i);	
		}
		
		public void LogError(double d)
		{
			if (m_LogError) Print("ERROR : " + d);	
		}
		
		public void LogError(DateTime t)
		{
			if (m_LogError) Print("ERROR : " + t);	
		}
		#endregion
		
		#region BarTypes
		/// <summary>
		/// 1. HHHL
		/// </summary>
		/// <param name="Bar1"></param>
		/// <param name="Bar2"></param>
		/// <returns></returns>
		private bool IsHigherHighHigherLow(int Bar2, int Bar1)
		{
			if ((GetHigh(Bar2) > GetHigh(Bar1)) && (GetLow(Bar2) > GetLow(Bar1))) return true;
			else return false;
		}
		
		/// <summary>
		/// 2. LLLH
		/// </summary>
		/// <param name="Bar1"></param>
		/// <param name="Bar2"></param>
		/// <returns></returns>
		private bool IsLowerHighLowerLow(int Bar2, int Bar1)
		{
			if ((GetHigh(Bar2) < GetHigh(Bar1)) && (GetLow(Bar2) < GetLow(Bar1))) return true;
			else return false;
		}
		
		/// <summary>
		/// 3. FTP
		/// </summary>
		/// <param name="Bar1"></param>
		/// <param name="Bar2"></param>
		/// <returns></returns>
		private bool IsFTP(int Bar2, int Bar1)
		{
			if ((GetHigh(Bar2) == GetHigh(Bar1)) && (GetLow(Bar2) > GetLow(Bar1))) return true;
			else return false;
		}
		
		/// <summary>
		/// 4. FBP
		/// </summary>
		/// <param name="Bar1"></param>
		/// <param name="Bar2"></param>
		/// <returns></returns>
		private bool IsFBP(int Bar2, int Bar1)
		{
			if ((GetHigh(Bar2) < GetHigh(Bar1)) && (GetLow(Bar2) == GetLow(Bar1))) return true;
			else return false;
		}
		
		/// <summary>
		/// 5. SLHH
		/// </summary>
		/// <param name="Bar1"></param>
		/// <param name="Bar2"></param>
		/// <returns></returns>
		private bool IsStitchLong(int Bar2, int Bar1)
		{
			if ((GetHigh(Bar1) < GetHigh(Bar2)) && (GetLow(Bar1) == GetLow(Bar2))) return true;
			else return false;
		}
		
		/// <summary>
		/// 6. SHLL
		/// </summary>
		/// <param name="Bar1"></param>
		/// <param name="Bar2"></param>
		/// <returns></returns>
		private bool IsStitchShort(int Bar2, int Bar1)
		{
			if ((GetLow(Bar1) > GetLow(Bar2)) && (GetHigh(Bar1) == GetHigh(Bar2))) return true;
			else return false;
		}
		
		/// <summary>
		/// 7. IB
		/// </summary>
		/// <param name="Bar1"></param>
		/// <param name="Bar2"></param>
		/// <returns></returns>
		private bool IsInsideBar(int Bar2, int Bar1)
		{
			if ((GetHigh(Bar2) < GetHigh(Bar1)) && (GetLow(Bar2) > GetLow(Bar1))) return true;
			else return false;
		}
		
		/// <summary>
		/// 8. OB
		/// </summary>
		/// <param name="Bar1"></param>
		/// <param name="Bar2"></param>
		/// <returns></returns>
		private bool IsOutSideBar(int Bar2, int Bar1)
		{
			if ((GetHigh(Bar1) < GetHigh(Bar2)) && (GetLow(Bar1) > GetLow(Bar2))) return true;
			else return false;
		}
		
		private bool IsOutSideBullishBar(int Bar2, int Bar1)
		{
			if (IsOutSideBar(Bar2,Bar1))
			{
				/*if (GetBodyRange(Bar1) < GetBodyRange(Bar2))
				{
					if (GetClose(Bar2) > GetClose(Bar1)) return true;
					else return false;
				}
				else return false;*/
				
				if (GetClose(Bar2) >= GetClose(Bar1)) return true;
				else return false;
			}
			else return false;
		}
		
		private bool IsOutSideBearishBar(int Bar2, int Bar1)
		{
			if (IsOutSideBar(Bar2,Bar1))
			{
				/*if (GetBodyRange(Bar1) < GetBodyRange(Bar2))
				{
					if (GetClose(Bar2) < GetClose(Bar1)) return true;
					else return false;
				}
				else return false;*/
				
				if (GetClose(Bar2) < GetClose(Bar1)) return true;
				else return false;
			}
			else return false;
		}
		
		/// <summary>
		/// 9. Same High/Same Low
		/// </summary>
		/// <param name="Bar"></param>
		/// <returns></returns>
		private bool IsEvenHarmonics(int Bar2, int Bar1)
		{
			if ((GetHigh(Bar1) == GetHigh(Bar2)) && (GetLow(Bar1) == GetLow(Bar2))) return true;
			else return false;
		}
	
		private bool IsSameClose(int Bar2, int Bar1)
		{
			if (GetClose(Bar1) == GetClose(Bar2)) return true;
			else return false;
		}
		
		private int DetermineBarType(int Bar1,int Bar2)
		{
			if (IsEvenHarmonics(Bar1,Bar2)) return TBar.SAME_HIGH_SAME_LOW;
			else if (IsHigherHighHigherLow(Bar1,Bar2)) return TBar.HIGHER_HIGH_HIGHER_LOW;
			else if (IsLowerHighLowerLow(Bar1,Bar2)) return TBar.LOWER_LOW_LOWER_HIGH; 
			else if (IsFTP(Bar1,Bar2)) return TBar.FLAT_TOP_PENNANT;
			else if (IsFBP(Bar1,Bar2)) return TBar.FLAT_BOTTOM_PENNANT;
			else if (IsOutSideBullishBar(Bar1,Bar2)) return TBar.OUTSIDE_BULLISH_ENGULFING_BAR;
			else if (IsOutSideBearishBar(Bar1,Bar2)) return TBar.OUTSIDE_BEARISH_ENGULFING_BAR;
			else if (IsInsideBar(Bar1,Bar2)) return TBar.INSIDE_BAR;
			else if (IsOutSideBar(Bar1,Bar2)) return TBar.OUTSIDE_BAR;
			else if (IsEvenHarmonics(Bar1,Bar2)) return TBar.SAME_HIGH_HIGHER_LOW;
			else if (IsEvenHarmonics(Bar1,Bar2)) return TBar.SAME_LOW_LOWER_HIGH;
			else if (IsStitchLong(Bar1, Bar2)) return TBar.HIGHER_HIGH_EQUAL_LOW;
			else if (IsStitchShort(Bar1, Bar2)) return TBar.LOWER_LOW_EQUAL_HIGH;
			else return NONE;
		}
		
		private int DetermineCloseType(int Bar1, int Bar2)
		{
			if (GetClose(Bar1) > GetClose(Bar2)) return TBar.HIGHER_CLOSE;
			else if (GetClose(Bar1) < GetClose(Bar2)) return TBar.LOWER_CLOSE;
			else return TBar.EQUAL_CLOSE;
		}
		
		private int DetermineBodyType(int Bar1, int Bar2)
		{
			if (GetClose(Bar1) > GetOpen(Bar2)) return TBar.GREEN;
			else if (GetClose(Bar1) < GetOpen(Bar2)) return TBar.RED;
			else return TBar.DOJI;
			
		}
		
		private TBar CreateBarObject(int Bar1, int Bar2)
		{
			
			int bartype = DetermineBarType(Bar1,Bar2);
			int closetype = DetermineCloseType(Bar1,Bar2);
			
			if ((bartype == TBar.HIGHER_HIGH_HIGHER_LOW) || (bartype == TBar.FLAT_TOP_PENNANT))
			{
				if (closetype == TBar.LOWER_CLOSE) bartype = TBar.HIGH_REVERSAL;
			}
			else if ((bartype == TBar.LOWER_LOW_LOWER_HIGH) || (bartype == TBar.FLAT_BOTTOM_PENNANT))
			{
				if (closetype == TBar.HIGHER_CLOSE) bartype = TBar.LOW_REVERSAL;
			} 
			
			TBar tbar = new TBar(Bar1, GetOpen(Bar1), GetHigh(Bar1), GetLow(Bar1), GetClose(Bar1), 
				bartype, closetype, DetermineBodyType(Bar1,Bar2));
			
			return tbar;
		}
		
		#endregion
		
		#region PriceVolFuncs
		public double GetHigh(int Bar)
		{
			return (High[m_CurrentBar-Bar]);
		}
		
		public double GetLow(int Bar)
		{
			return (Low[m_CurrentBar-Bar]);	
		}
		
		public double GetClose(int Bar)
		{
			return (Close[m_CurrentBar-Bar]);
		}
		
		public double GetOpen(int Bar)
		{
			return (Open[m_CurrentBar-Bar]);
		}
		
		public double GetVolume(int Bar)
		{
			return (Volume[m_CurrentBar-Bar]);	
		}
		
		#endregion
		
		#region RangeFuncs
		public double GetRange(int Bar)
		{
			return (High[m_CurrentBar-Bar] - Low[m_CurrentBar-Bar]);
		}
		
		public double GetBodyRange(int Bar)
		{
			return Math.Abs((Close[m_CurrentBar-Bar] - Open[m_CurrentBar-Bar]));
		}
		
		public int GetLowestLow(int BarStart, int BarsBack)
		{
			int start = BarStart - BarsBack;
			int end = BarStart;
			int ilow = 0;
			double low = GetLow(start);
			ilow = start;
			
			for (int i = start; i <= end; i++)
			{
				double ll = GetLow(i);
				
				if (low >= ll) 
				{
					low = ll;
					ilow = i;
				}
			}
			
			return ilow;
		}

		public int GetHighestHigh(int BarStart, int BarsBack)
		{
			int start = BarStart - BarsBack;
			int end = BarStart;
			int ihigh = 0;
			double high = GetHigh(start);
			ihigh = start;
			
			for (int i = start; i <= end; i++)
			{
				double hh = GetHigh(i);
				
				
				if (high <= hh) 
				{
					high = hh;
					ihigh = i;
				}
			}
			
			return ihigh;
		}
		#endregion
		
		#region CommonFuncs
		public string GetBarDateTime(int Bar)
		{
			string str = Time[m_CurrentBar - Bar].Date.ToString() + " - " + Time[m_CurrentBar - Bar].TimeOfDay.ToString();
			
			return str;
		}
		
		private int ReverseDirection(int direction)
		{
			if (direction == UP) return DOWN;
			else return UP;
		}
		#endregion
		
		#region CONSTS
		const int UP = 0;
		const int DOWN = 1;
		const int B2B = 2;
		const int R2R = 3;
		const int B2R = 4;
		const int R2B = 5;
		const int IB = 6;
		const int DB = 7;
		const int IR = 8;
		const int DR = 9;
		const int RIB = 10;
		const int BIR = 11;
		const int FLAT_TOP_PENNANT = 12;
		const int FLAT_BOTTOM_PENNANT = 13;
		const int LONG = 14;
		const int SHORT = 15;
		
		const int B_2_B = 912;
		const int R_2_R = 913;
		const int INC_B = 914;
		const int INC_R = 915;
		const int DEC_B = 916;
		const int DEC_R = 917;
		
		
		
		const int NONE = 26;
		
		const int BLACK_COLOR = NinjaTrader.Indicator.TBUpDnVol.BLACK_COLOR;
		const int RED_COLOR = NinjaTrader.Indicator.TBUpDnVol.RED_COLOR;
		
		#endregion
		
		#region POINT
		public class TPoint
		{
			public TPoint()
			{
				
			}
			
			public TPoint(int x, double y)
			{
				X = x;
				Y = y;
			}
			
			public int X;
			public double Y;
		};
		#endregion
		
		#region BAR
		public class TBar
		{
			public int m_BarNum;
			public double m_Open;
			public double m_High;
			public double m_Low;
			public double m_Close;
			
			public int m_BarType = -1;
			public int m_CloseType = -1;
			public int m_BodyType = -1;
			public int m_OutSideType = -1;
			
			/// <summary>
			/// Bar Type
			/// </summary>
			public const int FLAT_TOP_PENNANT = 0;
			public const int FLAT_BOTTOM_PENNANT = 1;
			public const int HIGHER_HIGH_HIGHER_LOW = 2;
			public const int HIGHER_HIGH_HIGHER_LOW_LOWER_CLOSE = 3;
			public const int HIGHER_HIGH_EQUAL_LOW = 30000;
			public const int LOWER_LOW_LOWER_HIGH = 4;
			public const int LOWER_LOW_LOWER_HIGH_HIGHER_CLOSE = 5;
			public const int LOWER_LOW_EQUAL_HIGH = 50000;
			public const int SAME_HIGH_HIGHER_LOW = 6;
			public const int SAME_LOW_LOWER_HIGH = 7;
			public const int INSIDE_BAR = 8;
			public const int OUTSIDE_BAR = 9;
			public const int OUTSIDE_BEARISH_ENGULFING_BAR = 10;
			public const int OUTSIDE_BULLISH_ENGULFING_BAR = 11;
			public const int LOWER_HIGH_EQUAL_CLOSE = 12;
			public const int HIGHER_LOW_EQUAL_CLOSE = 13;
			public const int SAME_HIGH_SAME_LOW = 14;
			public const int HIGH_REVERSAL = 15;
			public const int LOW_REVERSAL = 16;
			/// <summary>
			/// Close relative to prior bar
			/// </summary>
			public const int HIGHER_CLOSE = 17;
			public const int LOWER_CLOSE = 18;
			public const int EQUAL_CLOSE = 19;
			/// <summary>
			/// Bar body
			/// </summary>
			public const int GREEN = 20;
			public const int RED = 21;
			public const int DOJI = 22;
			
			System.Collections.Generic.Dictionary<int, string> m_ConstsToString = new System.Collections.Generic.Dictionary<int, string>();
			
			public TBar()
			{
				
			}
			
			public TBar(int num, double open, double high, double low, double close,
						 int bartype, int closetype, int bodytype)
			{
				
				m_BarNum = num;
				
				m_Open = open;
				m_High = high;
				m_Low = low;
				m_Close = close;
				
				m_BarType = bartype;
				m_CloseType = closetype;
				m_BodyType = bodytype;
				
				m_ConstsToString[FLAT_TOP_PENNANT] = "FLAT_TOP_PEN";
				m_ConstsToString[FLAT_BOTTOM_PENNANT] = "FLAT_BOTTOM_PEN";
				m_ConstsToString[HIGHER_HIGH_HIGHER_LOW] = "HIGHER_HIGH_HIGHER_LOW";
				m_ConstsToString[HIGHER_HIGH_HIGHER_LOW_LOWER_CLOSE] = "HIGHER_HIGH_HIGHER_LOW_LOWER_CLOSE";
				m_ConstsToString[LOWER_LOW_LOWER_HIGH] = "LOWER_LOW_LOWER_HIGH";
				m_ConstsToString[LOWER_LOW_LOWER_HIGH_HIGHER_CLOSE] = "LOWER_HIGH_LOWER_LOW_HIGHER_CLOSE";
				m_ConstsToString[SAME_HIGH_HIGHER_LOW] = "SAME_HIGH_HIGHER_LOW";
				m_ConstsToString[SAME_LOW_LOWER_HIGH] = "SAME_LOW_LOWER_HIGH";
				m_ConstsToString[INSIDE_BAR] = "INSIDE_BAR";
				m_ConstsToString[OUTSIDE_BAR] = "OUTSIDE_BAR";
				m_ConstsToString[OUTSIDE_BEARISH_ENGULFING_BAR] = "OUTSIDE_BEARISH_ENGULFING_BAR";
				m_ConstsToString[OUTSIDE_BULLISH_ENGULFING_BAR] = "OUTSIDE_BULLISH_ENGULFING_BAR";
				m_ConstsToString[LOWER_HIGH_EQUAL_CLOSE] = "LOWER_HIGH_EQUAL_CLOSE";
				m_ConstsToString[HIGHER_LOW_EQUAL_CLOSE] = "HIGHER_LOW_EQUAL_CLOSE";
				m_ConstsToString[SAME_HIGH_SAME_LOW] = "SAME_HIGH_SAME_LOW";
				m_ConstsToString[HIGH_REVERSAL] = "HIGH_REVERSAL_BAR";
				m_ConstsToString[LOW_REVERSAL] = "LOW_REVERSAL_BAR";
				
				m_ConstsToString[HIGHER_CLOSE] = "HIGHER_CLOSE";
				m_ConstsToString[LOWER_CLOSE] = "LOWER_CLOSE";
				m_ConstsToString[EQUAL_CLOSE] = "EQUAL_CLOSE";
				
				m_ConstsToString[GREEN] = "GREEN";
				m_ConstsToString[RED] = "RED";
				m_ConstsToString[DOJI] = "DOJI";
			}
			
			public string GetConstString(int cnst)
			{
				try
				{
					if ((cnst < 0 ) || (cnst > 22)) return "";
					else return m_ConstsToString[cnst];
				}
				catch(System.Exception e)
				{
					return "";	
				}
			}
			
		};
		#endregion
		
		#region TLINE
		public class TLine
		{
			public string m_Tag;
			public TPoint  m_BeginPoint;
			public TPoint	m_EndPoint;
			public TPoint m_ExtendedPoint;
			public double m_Slope;
			public int m_Direction;
			
			public bool m_Broken;
			
			
			private void Init()
			{
				m_BeginPoint = new TPoint();
				m_EndPoint = new TPoint();
				m_ExtendedPoint = new TPoint();
			}
			
			public TLine(int x1, double y1, int x2, double y2)
			{
					Init();
				
					m_BeginPoint.X = x1;
					m_BeginPoint.Y = y1;
				
					m_EndPoint.X = x2;
					m_EndPoint.Y = y2;
				
					m_ExtendedPoint = m_EndPoint;
				
					m_Broken = false;
				
					CalculateSlope();
			}
			
			public TLine(TPoint pt1, TPoint pt2)
			{
					Init();
				
					m_BeginPoint  = pt1;
					m_EndPoint = pt2;
				
					m_ExtendedPoint = m_EndPoint;
				
					m_Broken = false;
				
					CalculateSlope();
			}
			
			public TLine(double slope, TPoint pt1, TPoint endpoint, int direction)
			{
					Init();
				
					m_Slope = slope;
				
					m_BeginPoint = pt1;
					m_EndPoint = endpoint;
				
					m_ExtendedPoint = m_EndPoint;
				
					m_Direction = direction;
				
					
			}
			
			public TLine(double slope, int x1, double y1, int x2, double y2, int direction)
			{
					Init();
				
					m_Slope = slope;
				
					m_Direction = direction;
				
					m_BeginPoint.X = x1;
					m_BeginPoint.Y = y1;
				
					m_EndPoint.X = x1;
					m_EndPoint.Y = y1;
				
					m_ExtendedPoint = m_EndPoint;
				
					m_Tag = "LTL" + System.Convert.ToString(m_BeginPoint.X) + System.Convert.ToString(m_EndPoint.X) + System.Convert.ToString(x1);
			}
			
			//for outside bars RTL
			public TLine(double slope, int x, double y, int direction)
			{
					Init();
					
					m_Slope = slope;
					
					m_BeginPoint.X = x;
					m_BeginPoint.Y = y;
					
					m_EndPoint.X = x + 1;
					m_EndPoint.Y = LineValue(m_EndPoint.X);
					
					m_ExtendedPoint = m_EndPoint;
					
					m_Direction = direction;
				
			}
			
			protected void CalculateSlope()
			{
				m_Slope = (m_ExtendedPoint.Y - m_BeginPoint.Y)/(m_ExtendedPoint.X - m_BeginPoint.X);
				
				m_Direction = (m_Slope > 0) ? UP : DOWN;
			}
			
			public double LineValue(int x)
			{
				double y = m_Slope*(x - m_BeginPoint.X) + m_BeginPoint.Y;
				return y;
			}
			
		};
		#endregion
		
		#region TRTL
		public class TRTL : TLine
		{
			TPoint m_Point1;
			TPoint m_Point3;
			public bool m_FBO;
			public int m_FBOBarNum;
			public int m_BrokenBar;
			public bool m_Pennant;
			public bool m_Flarable = true;
			
			private void Init(int x1, double y1, int x2, double y2)
			{
				m_Point1 = new TPoint(x1, y1);
				m_Point3 = new TPoint(x2, y2);
				
				m_FBO = false;
				
			}
			
			public TRTL(int x1, double y1, int x2, double y2) : base(x1, y1, x2, y2)
			{
				Init(x1, y1, x2, y2);
				
				m_Tag = "RTL" + System.Convert.ToString(x1) + System.Convert.ToString(x2) + System.Convert.ToString(m_Direction);	
			}
			
			public TRTL(TPoint point1, TPoint point3) : base(point1, point3)
			{
				Init(point1.X, point1.Y, point3.X, point3.Y);
				
				
				
				m_Tag = "RTL" + System.Convert.ToString(point1.X) + 
							System.Convert.ToString(point3.X) + System.Convert.ToString(m_Direction);
			}
			
			public TRTL(double slope, int x, double y, int direction) : base(slope, x, y, direction)
			{
				Init(x, y, m_EndPoint.X, m_EndPoint.Y);
				
				m_Direction = direction;
				
				m_Tag = "RTL" + System.Convert.ToString(m_BeginPoint.X) + System.Convert.ToString(m_EndPoint.X);
			}
			
			
			private bool ReAdjustChannel(TBar curbar)
			{
				//@@1.CHANGE FROM V5
				
				double lineval = LineValue(curbar.m_BarNum);
				
				if (curbar.m_BarType == TBar.INSIDE_BAR) return true;
				else if (curbar.m_BarType == TBar.SAME_HIGH_HIGHER_LOW) return true;
				else if (curbar.m_BarType == TBar.SAME_HIGH_SAME_LOW) return true;
				else if (curbar.m_BarType == TBar.OUTSIDE_BEARISH_ENGULFING_BAR) return true;
				else if (curbar.m_BarType == TBar.OUTSIDE_BULLISH_ENGULFING_BAR) return true;
				
				if (m_Direction==UP) 
				{
					if (curbar.m_BarType == TBar.HIGHER_HIGH_HIGHER_LOW) return true;
					else if (curbar.m_BarType == TBar.HIGHER_HIGH_EQUAL_LOW) return true;
					else if (curbar.m_BarType == TBar.OUTSIDE_BULLISH_ENGULFING_BAR) return true;
					else if (curbar.m_BarType == TBar.HIGH_REVERSAL) return true;
					else if (curbar.m_BarType == TBar.FLAT_TOP_PENNANT) return true;
					else return false;
				}
				else if (m_Direction == DOWN) 
				{
					if (curbar.m_BarType == TBar.LOWER_LOW_LOWER_HIGH) return true;
					else if (curbar.m_BarType == TBar.LOWER_LOW_EQUAL_HIGH) return true;
					else if (curbar.m_BarType == TBar.OUTSIDE_BEARISH_ENGULFING_BAR) return true;
					else if (curbar.m_BarType == TBar.LOW_REVERSAL) return true;
					else if (curbar.m_BarType == TBar.FLAT_BOTTOM_PENNANT) return true;
					else return false;
				}
				else return false;
				
			}
			
			public bool Extend(TBar curbar, TBar prevbar, bool isbarinside)
			{
				//@@: REWORK
				
				if (m_Broken) return false;
				
				m_FBO = false;
				
				double lineval = LineValue(curbar.m_BarNum);
				
				m_ExtendedPoint.X = curbar.m_BarNum;
				m_ExtendedPoint.Y = lineval; 
				
				if ((((m_Direction==UP) && (lineval >= curbar.m_Low)) || ((m_Direction==DOWN) && (lineval <= curbar.m_High))))
				{
					if (ReAdjustChannel(curbar))
					{
						if (m_Direction==UP) 
						{
							if ((m_Pennant) && ((curbar.m_Low >= m_BeginPoint.Y)))
							{
								if (curbar.m_Low == m_BeginPoint.Y) m_ExtendedPoint.Y = lineval;
								else m_ExtendedPoint.Y = curbar.m_Low;
								
								m_FBO = true;
								m_FBOBarNum = curbar.m_BarNum;
							}
							else if ((curbar.m_Low > m_BeginPoint.Y))
							{
								m_ExtendedPoint.Y = curbar.m_Low;
								m_FBO = true;
								m_FBOBarNum = curbar.m_BarNum;
							}
						}
						else if (m_Direction==DOWN)
						{
							if ((m_Pennant) && ((curbar.m_High <= m_BeginPoint.Y)))
							{
								if (curbar.m_High == m_BeginPoint.Y) m_ExtendedPoint.Y = lineval;
								else m_ExtendedPoint.Y = curbar.m_High;
								
								m_FBO = true;
								m_FBOBarNum = curbar.m_BarNum;
							}
							else if ((curbar.m_High < m_BeginPoint.Y))
							{
								m_ExtendedPoint.Y = curbar.m_High;
								m_FBO = true;
								m_FBOBarNum = curbar.m_BarNum;
							}
							
						}
						
						if (m_FBO)
						{
							CalculateSlope();
						
							return false;
						}
						else
						{
							m_BrokenBar = curbar.m_BarNum;
							m_Broken = true;
							return true;
						}
					}
					else 
					{
						m_BrokenBar = curbar.m_BarNum;
						m_Broken = true;
						return true;
					}
				}
				
				return false;
			}
		
			public int Point1 
			{ 
				get { return m_Point1.X; }
			}
			
			public int Point3 
			{ 
				get { return m_Point3.X; }
			}
			
			public int FBOBarNum
			{
				get { return m_FBOBarNum; }	
			}
		};
		#endregion
		
		#region TLTL
		public class TLTL : TLine
		{
			public TPoint m_Point2;
			
			public bool m_Ve;
			
			public int m_VeBarNum;
			
			public List<TLine> m_VeLines;
			
			private void Init()
			{
				m_Point2 = new TPoint();
				
				m_VeLines = new List<TLine>();
				
			}
			
			public TLTL(double slope, TPoint pt1, TPoint pt2, TPoint endpoint, int direction) : base(slope, pt1, endpoint, direction)
			{
				Init();
				
				m_Tag = "LTL" + System.Convert.ToString(pt1.X) + System.Convert.ToString(endpoint.X) + System.Convert.ToString(m_Direction);
				m_Point2 = pt2;
				
				m_Ve = false;
				
			}
			
			public void Extend(TBar curbar, TBar prevbar)
			{
				double lineval;
				
				if (!m_Ve) lineval = LineValue(curbar.m_BarNum);
				else lineval = (m_VeLines[m_VeLines.Count - 1]).LineValue(curbar.m_BarNum);
				
				if (!m_Ve)
				{
						m_ExtendedPoint.X = curbar.m_BarNum;
						m_ExtendedPoint.Y = lineval;	
				}
				else
				{
					(m_VeLines[m_VeLines.Count - 1]).m_ExtendedPoint.X = curbar.m_BarNum;
					(m_VeLines[m_VeLines.Count - 1]).m_ExtendedPoint.Y = lineval;
				}
				
				if ((m_Direction==UP) && (lineval < curbar.m_High))
				{
						m_VeLines.Add(new TLine(m_Slope, curbar.m_BarNum, curbar.m_High, curbar.m_BarNum, curbar.m_High, m_Direction));
						
						m_Ve = true;
					
						m_VeBarNum = curbar.m_BarNum;
						
				}
				else if ((m_Direction==DOWN) && (lineval > curbar.m_Low))
				{
						m_VeLines.Add(new TLine(m_Slope, curbar.m_BarNum, curbar.m_Low, curbar.m_BarNum, curbar.m_Low, m_Direction));
						
						m_Ve = true;
						
						m_VeBarNum = curbar.m_BarNum;
				}
				else if (m_Ve)
				{
						(m_VeLines[m_VeLines.Count - 1]).m_ExtendedPoint.X = curbar.m_BarNum;
						(m_VeLines[m_VeLines.Count - 1]).m_ExtendedPoint.Y = lineval;
						
				}
				
			}
			
			
			
			public int Point2 
			{ 
				get { return m_Point2.X; }
			}
			
			public double Slope 
			{
				get { return m_Slope; }	
			}
			
			public void Clear()
			{
				m_VeLines.Clear();
			}
			
		};
		#endregion
		
		#region CHANNEL
		public class TChannel
		{
			/// <summary>
			/// Event consts
			/// </summary>
			/// 
			#region EventConsts
			public const int VE_NONE = -1;
			public const int VE_HIGH_EQ_OPEN_REJECT_OFF_OPEN = 0;
			public const int VE_HIGH_ABOVE_CLSOE_REJECT_OFF_OPEN = 1;
			public const int VE_HIGH_EQ_CLOSE_REJECT_OFF_CLOSE = 2;
			public const int VE_HIGH_ABOVE_CLOSE_REJECT_OFF_CLOSE = 3;
			public const int VE_HIGH_EQ_HIGH_CLOSE_REJECT_OFF_OPEN = 4;
			public const int VE_HIGH_ABOVE_HIGH_CLOSE_REJECT_OFF_OPEN = 5;
			public const int VE_HIGH_EQ_HIGH_CLOSE_REJECT_OFF_CLOSE = 6;
			public const int VE_HIGH_ABOVE_HIGH_CLOSE_REJECT_OFF_CLOSE = 7;
			public const int VE_HIGH_EQ_HIGH_CLOSE_REJECT_OFF_HIGH = 8;
			public const int VE_HIGH_ABOVE_HIGH_CLOSE_REJECT_OFF_HIGH = 9;
			public const int VE_LOW_EQ_OPEN_BOUNCE_OFF_OPEN = 10;
			public const int VE_LOW_BELOW_CLOSE_BOUNCE_OFF_OPEN = 11;
			public const int VE_LOW_EQ_CLOSE_BOUNCE_OFF_CLOSE = 12;
			public const int VE_LOW_BELOW_CLOSE_BOUNCE_OFF_CLOSE = 13;
			public const int VE_LOW_EQ_LOW_CLOSE_BOUNCE_OFF_OPEN = 14;
			public const int VE_LOW_BELOW_LOW_CLOSE_BOUNCE_OFF_OPEN = 15;
			public const int VE_LOW_EQ_LOW_CLOSE_BOUNCE_OFF_CLOSE = 16;
			public const int VE_LOW_BELOW_LOW_CLOSE_BOUNCE_OFF_CLOSE = 17;
			public const int VE_LOW_EQ_LOW_CLOSE_BOUNCE_OFF_LOW = 18;
			public const int VE_LOW_BELOW_LOW_CLOSE_BOUNCE_OFF_LOW = 19;
			
			System.Collections.Generic.Dictionary<int,string> m_EventStrings = 
				new System.Collections.Generic.Dictionary<int,string>();
			
			private int m_ChannelEvent = VE_NONE;
			
			public const int POINT_2_BOUNCE_OFF_LOW = 1201;
			public const int POINT_2_BOUNCE_OFF_CLOSE = 1202;
			public const int POINT_2_BOUNCE_OFF_OPEN = 1203;
			public const int POINT_2_PENETRATE_BOUNCE_OFF_LOW = 1204;
			public const int POINT_2_BREAK_DOWN = 1205;
			
			public const int POINT_2_REJECT_OFF_HIGH = 1301;
			public const int POINT_2_REJECT_OFF_CLOSE = 1302;
			public const int POINT_2_REJECT_OFF_OPEN = 1303;
			public const int POINT_2_PENETRATE_REJECT_OFF_HIGH = 1304;
			public const int POINT_2_BREAK_OUT = 1305;
			
			public const int FBO_BOUNCE = 1306;
			public const int FBO_REJECT = 1307;
			#endregion
			
			#region StateVariables
			public const int R_2_R = 100;
			public const int B_2_B = 101;
			public const int R_INC = 102;
			public const int B_INC = 103;
			public const int R_DEC = 104;
			public const int B_DEC = 105;
			
			private int m_State;
			
			public int State { get { return m_State; } set { m_State = value;  } }
			
			public string SState 
			{
				get
				{
					if (m_State == R_2_R) return "R_2_R";
					else if (m_State == B_2_B) return "B_2_B";
					else if (m_State == R_INC) return "R_INC";
					else if (m_State == B_INC) return "B_INC";
					else if (m_State == R_DEC) return "R_DEC";
					else if (m_State == B_DEC) return "B_DEC";
					else return "";
					
				}
			}
			#endregion
			
			#region LinePropertiesVariables
			private double m_TickSize;
			private int m_Level;
			private DashStyle m_LineStyle;
			private int m_LineWidth;
			private Color m_LineColor;
			
			private TRTL m_RTL;
			private TLTL m_LTL;
			#endregion
			
			#region SepcialBarsVariables
			private int m_UpgradedBar;
			private int m_JoinedBar;
			private int m_ChannelHL;
			private int m_PrevChannelHL;
			#endregion
			
			#region BooleanVariables
			private bool m_FBO;
			private bool m_Broken;
			private bool m_Joined = false;
			private bool m_Inside = false;
			private bool m_Removed = false;
			#endregion
			
			#region StringVariables
			private string m_Parent;
			
			private string m_Point1Text;
			private string m_Point2Text;
			private string m_Point3Text;
			
			private string m_VolumeSequence = "";
			
			private string m_ChannelType = "";
			#endregion
			
			#region Properties
			
			//@@
			public int ChannelConnectionPoint;
			
			public int ChannelEvent { get { return m_ChannelEvent; } set { m_ChannelEvent = value;  } }
			public string SChannelEvent 
			{ 
				get 
				{ 
					if (m_ChannelEvent == -1) return "";
					else return m_EventStrings[m_ChannelEvent]; 
				} 
			}
			
			public string ChannelType { get { return m_ChannelType; } set { m_ChannelType = value;  } }
			public string VolumeSequence { get { return m_VolumeSequence; } set { m_VolumeSequence = value;  } }
			public int VeBarNum { get { return m_LTL.m_VeBarNum; } }
			public int ChannelHL { get { return m_ChannelHL; } set { m_ChannelHL = value;  } }
			public int PrevChannelHL { get { return m_PrevChannelHL; } set { m_PrevChannelHL = value;  } }
			
			public bool Removed { get { return m_Removed; } set { m_Removed = value;  } }
			
			
			public int UpgradedBar { get { return m_UpgradedBar; } set {m_UpgradedBar = value; } }
			public DashStyle LineStyle{ get { return m_LineStyle; } }
			public int LineWidth { get { return m_LineWidth; } }
			public Color LineColor { get { return m_LineColor; } set { m_LineColor = value;} }
			
			public TRTL RTL { get { return m_RTL; } }
			public TLTL LTL { get { return m_LTL; } set { m_LTL = value; } }
			
			public string RTLTag { get { return m_RTL.m_Tag; } set {  m_RTL.m_Tag = value; }}
			public string LTLTag { get { return m_LTL.m_Tag; } set {  m_LTL.m_Tag = value; }}
			public string Parent {get  {return m_Parent; } set { m_Parent = value; } }
		
			
			public int RTLX1 { get { return m_RTL.m_BeginPoint.X; } }
			public int RTLX2 { get { return m_RTL.m_ExtendedPoint.X; } }
			
			public double RTLY1 { get { return m_RTL.m_BeginPoint.Y; } }
			public double RTLY2 { get { return m_RTL.m_ExtendedPoint.Y; } }
			
			public int LTLX1 { get { return m_LTL.m_BeginPoint.X; } }
			public int LTLX2 { get { return m_LTL.m_ExtendedPoint.X; } }
			
			public double LTLY1 { get { return m_LTL.m_BeginPoint.Y; } }
			public double LTLY2 { get { return m_LTL.m_ExtendedPoint.Y; } }
			public int Length { get { return (RTLX2 - RTLX1); } }
			
			public int Direction { get { return m_RTL.m_Direction; } }
			
			public string Label { get { return (m_RTL.m_BeginPoint.X.ToString() + "," + m_RTL.m_ExtendedPoint.X.ToString()); } }
			
			public int Angle
			{
				get
				{
					double pricediff = (RTLY2 - RTLY1)/m_TickSize;
					int timediff = RTLX2 - RTLX1;
					
					return(System.Convert.ToInt16(Math.Atan(pricediff/timediff)*(180/Math.PI)));
				}
			}
			
			public string GannAngle 
			{
				get 
				{ 
					int ang = Math.Abs(Angle);
					
					string s = "";
					
					if ((ang >= 7) && (ang < 11)) s = "8x1";
					else if ((ang >= 11) && (ang < 17)) s = "4x1";
					else if ((ang >= 17) && (ang < 23)) s = "3x1";
					else if ((ang >= 23) && (ang < 35)) s = "2x1";
					else if ((ang >= 35) && (ang < 54)) s = "1x1";
					else if ((ang >= 54) && (ang < 67)) s = "1x2";
					else if ((ang >= 67) && (ang < 73)) s = "1x3";
					else if ((ang >= 73) && (ang < 78)) s = "1x4";
					else if ((ang >= 78) && (ang < 90)) s = "1x8";
					
					return s;
				} 
			}
			
			public int JoinedBar 
			{  
				get 
				{ 
					return m_JoinedBar; 
				} 
				
				set
				{
					m_JoinedBar = value;
				}
			}
			
			public string SDirection 
			{ 
					get 
					{ 
						string s = (m_RTL.m_Direction==UP) ? "UP" : "DOWN"; 
						return s;
					}
			}
			
			public double Slope { get { return m_RTL.m_Slope; } }
			
			public int Level 
			{ 
				get { return m_Level; }
				set 
				{ 
					m_Level = value;
					SetLineProps();
					
				}
			}
			
			public bool Broken { get { return m_Broken; } set { m_Broken = value; }}
			public int BrokenBar { get { return RTL.m_BrokenBar; } }

			public bool FBO 
			{ 
				get { return m_FBO; } 
				set
				{
					m_FBO = value;
					m_RTL.m_FBO = value;
				}
			}
			
			public bool Joined { get { return m_Joined; } set { m_Joined = value; }}
			
			public bool Inside { get { return m_Inside; } set { m_Inside = value; }}
			
			public bool Pennant { get { return m_RTL.m_Pennant; } set {  m_RTL.m_Pennant = value; }}
			
			public int Point1 
			{ 
				get { return m_RTL.Point1; }
			}
			
			public int Point2 
			{ 
				get { return m_LTL.Point2; }
			}
			 
			public int Point3 
			{ 
				get { return m_RTL.Point3; }
			}
			
			public string Point1Tag
			{
				get { return m_Point1Text; }
				set { m_Point1Text = value; }
			}
			
			public string Point2Tag
			{
				get { return m_Point2Text; }
				set { m_Point2Text = value; }
			}
			
			public string Point3Tag
			{
				get { return m_Point3Text; }
				set { m_Point3Text = value; }
			}
			
			public int FBOBarNum
			{
				get { return RTL.FBOBarNum; }	
			}
			
			public bool Upgraded
			{
				get { return Upgraded; }
				set { Upgraded = value; } 	
			}
			#endregion
			
			public TChannel(TRTL rtl, TLTL ltl, int level, double ticksize)
			{
				m_TickSize = ticksize;
				
				m_RTL = rtl;
				m_LTL = ltl;
				
				m_Broken = false;
				m_FBO = false;
				
				m_Joined = false;
				
				m_Level = level;
				
				m_PrevChannelHL = 0;
				
				SetLineProps();
				
				m_EventStrings[0] = "VE_HIGH_EQ_OPEN_REJECT_OFF_OPEN";  
				m_EventStrings[1] = "VE_HIGH_ABOVE_CLSOE_REJECT_OFF_OPEN";  
				m_EventStrings[2] = "VE_HIGH_EQ_CLOSE_REJECT_OFF_CLOSE";  
				m_EventStrings[3] = "VE_HIGH_ABOVE_CLOSE_REJECT_OFF_CLOSE";  
				m_EventStrings[4] = "VE_HIGH_EQ_HIGH_CLOSE_REJECT_OFF_OPEN";  
				m_EventStrings[5] = "VE_HIGH_ABOVE_HIGH_CLOSE_REJECT_OFF_OPEN";  
				m_EventStrings[6] = "VE_HIGH_EQ_HIGH_CLOSE_REJECT_OFF_CLOSE";  
				m_EventStrings[7] = "VE_HIGH_ABOVE_HIGH_CLOSE_REJECT_OFF_CLOSE";  
				m_EventStrings[8] = "VE_HIGH_EQ_HIGH_CLOSE_REJECT_OFF_HIGH";  
				m_EventStrings[9] = "VE_HIGH_ABOVE_HIGH_CLOSE_REJECT_OFF_HIGH";  
				m_EventStrings[10] = "VE_LOW_EQ_OPEN_BOUNCE_OFF_OPEN";  
				m_EventStrings[11] = "VE_LOW_BELOW_CLOSE_BOUNCE_OFF_OPEN";  
				m_EventStrings[12] = "VE_LOW_EQ_CLOSE_BOUNCE_OFF_CLOSE";  
				m_EventStrings[13] = "VE_LOW_BELOW_CLOSE_BOUNCE_OFF_CLOSE";  
				m_EventStrings[14] = "VE_LOW_EQ_LOW_CLOSE_BOUNCE_OFF_OPEN";  
				m_EventStrings[15] = "VE_LOW_BELOW_LOW_CLOSE_BOUNCE_OFF_OPEN";  
				m_EventStrings[16] = "VE_LOW_EQ_LOW_CLOSE_BOUNCE_OFF_CLOSE";  
				m_EventStrings[17] = "VE_LOW_BELOW_LOW_CLOSE_BOUNCE_OFF_CLOSE";  
				m_EventStrings[18] = "VE_LOW_EQ_LOW_CLOSE_BOUNCE_OFF_LOW";  
				m_EventStrings[19] = "VE_LOW_BELOW_LOW_CLOSE_BOUNCE_OFF_LOW";  
			}
			
			public void Extend(TBar curbar, TBar prevbar, bool isbarinside)
			{	
				if ((GannAngle == "3x1") || (GannAngle == "4x1") || (GannAngle == "8x1")) m_RTL.m_Flarable = false;
				
				if (!m_RTL.m_Broken) 
				{
					if (!m_RTL.Extend(curbar, prevbar, isbarinside)) 
					{
						if (m_RTL.m_FBO) m_FBO = true;
						
					}
					else m_Broken = true;
						
					m_LTL.Extend(curbar, prevbar);
				}
			}
			
			private void SetLineThickness()
			{
				switch(m_Level)
				{
					case 0:
						m_LineWidth = 2;
					break;
						
					case 1:
						m_LineWidth = 1;
					break;
						
					default:
						m_LineWidth = 1;
					break;
				}
			}
			
			private void SetLineType()
			{
				switch(m_Level)
				{
					case 0:
						m_LineStyle = DashStyle.Solid;
					break;
						
					case 1:
						m_LineStyle = DashStyle.Dash;
					break;
						
					case 2:
						m_LineStyle = DashStyle.DashDot;
					break;
						
					case 3:
						m_LineStyle = DashStyle.DashDotDot;
					break;
						
					case 4:
						m_LineStyle = DashStyle.Dot;
					break;
						
					default:
						m_LineStyle = DashStyle.Dash;
					break;
				}
			}
			
			private void SetLineColor()
			{
				if (m_RTL.m_Direction == UP)
				{
					switch(m_Level)
					{
						case 0:
							m_LineColor = Color.Blue;
						break;
							
						case 1:
							m_LineColor = Color.DarkTurquoise;
						break;
							
						case 2:
							m_LineColor = Color.DarkViolet;
						break;
							
						case 3:
							m_LineColor = Color.DarkGreen;
						break;
							
						case 4:
							m_LineColor = Color.LightGreen;
						break;
							
						default:
							m_LineColor = Color.DarkTurquoise;
						break;
					}
				}
				else
				{
					switch(m_Level)
					{
						case 0:
							m_LineColor = Color.Red;
						break;
							
						case 1:
							m_LineColor = Color.Fuchsia;
						break;
							
						case 2:
							m_LineColor = Color.Coral;
						break;
							
						case 3:
							m_LineColor = Color.Chocolate;
						break;
							
						case 4:
							m_LineColor = Color.Brown;
						break;
							
						default:
							m_LineColor = Color.Fuchsia;
						break;
					}
				}
			}
			
			private void SetLineColorScheme2()
			{
				if (m_RTL.m_Direction == UP)
				{
					switch(m_Level)
					{
						case 0:
							m_LineColor = Color.Blue;
						break;
							
						case 1:
							m_LineColor = Color.Blue;
						break;
							
						case 2:
							m_LineColor = Color.Blue;
						break;
							
						case 3:
							m_LineColor = Color.DarkTurquoise;
						break;
							
						case 4:
							m_LineColor = Color.LightBlue;
						break;
							
						default:
							m_LineColor = Color.LightSkyBlue;
						break;
					}
				}
				else
				{
					switch(m_Level)
					{
						case 0:
							m_LineColor = Color.Red;
						break;
							
						case 1:
							m_LineColor = Color.Red;
						break;
							
						case 2:
							m_LineColor = Color.Red;
						break;
							
						case 3:
							m_LineColor = Color.Fuchsia;
						break;
							
						case 4:
							m_LineColor = Color.LightPink;
						break;
							
						default:
							m_LineColor = Color.Tomato;
						break;
					}
				}
			}
			
			private void SetLineProps()
			{
				SetLineThickness();
				SetLineColorScheme2();
				SetLineType();
				//SetLineColor();
			}
			
			
			
			
			
			public void ClearLTLLines()
			{
				m_LTL.Clear();
			}
			
		};
		
		public class ILengthComparerDesc:IComparer<TChannel>
 		{
  			public int Compare(TChannel x, TChannel y)
  			{
   				return y.Length - x.Length; //descending sort
   
  			}
		};
		
		public class ILevelComparer:IComparer<TChannel>
 		{
  			public int Compare(TChannel x, TChannel y)
  			{
   				return y.Level - x.Level; //descending sort
   
  			}
		};
		
		public class ILevelComparerAsc:IComparer<TChannel>
 		{
  			public int Compare(TChannel x, TChannel y)
  			{
   				return x.Level - y.Level; //descending sort
   
  			}
		};
		
		
		
		#endregion
		
		#region LINE_FUNCS
		
		private bool EqualPt1HighsLows(int pt1, int direction)
		{
			bool ret = false;
			
			if (direction == UP) 
			{
				if  (GetLow(pt1) == GetLow(pt1+1)) ret = true;
			}
			else if (direction == DOWN) 
			{
				if (GetHigh(pt1) == GetHigh(pt1+1)) ret = true;
			}
			
			return ret;
			
		}
		
		private TRTL CreateRTL(int pt1x, double pt1y, int pt2, int direction)
		{
			TRTL line;
			
			line = LineChecks(pt1x, pt1y, pt2, direction); 
			LogError("CreateRTL - LineChecks " + " pt1 " + pt1x + "," + pt1y + " pt2 " + pt2 + " direction : " + direction);
			
			if (line != null) return line; 
			
			line = LineChecks(pt1x, pt1y, pt2-1, direction); 
			LogError("CreateRTL - LineChecks " + " pt1 " + pt1x + "," + pt1y + " pt2 - 1 " + (pt2-1) + " direction : " + direction);
			
			if (line != null) return line;
			
			line = LineChecks(pt1x, pt1y, pt2-2, direction); 
			LogError("CreateRTL - LineChecks " + " pt1 " + pt1x + "," + pt1y + " pt2 - 2 " + (pt2-2) + " direction : " + direction);
			
			if (line != null) return line;
			
			return null;
		}
		
		public TRTL LineChecks(int pt1x, double pt1y, int pt2, int direction)
		{
			TPoint point1, point2;
			TRTL line;
			
			if (direction==UP)
			{
				if (GetLow(pt2) <= pt1y) 
				{
					LogError("direction : " + direction + "pt2 : " +  
								pt2 + " Low " + pt2 + " <= " + "pt1 : " +  
								pt1x + " Low " + pt1y); 
					return null;
				}
				
				point1 = new TPoint(pt1x, pt1y);
				point2 = new TPoint(pt2, GetLow(pt2));
				
			}
			else
			{
				if (GetHigh(pt2) >= pt1y) 
				{
					LogError("direction : " + direction + "pt2 : " +  pt2 + 
					" High " + GetHigh(pt2) + " >= " + "pt1 : " +  pt1x + 
					" High " + pt1y);
					return null;
				}
				
				point1 = new TPoint(pt1x, pt1y);
				point2 = new TPoint(pt2, GetHigh(pt2));
				
			}
			
			line = new TRTL(point1, point2);
			
			if (CheckLineIntegrity(line)) return line;
			else return null;
			
		}
		
		private TRTL CreateRTL(int pt1, int pt2, int direction)
		{
			TRTL line;
			
			line = LineChecks(pt1, pt2, direction); 
			LogError("CreateRTL - LineChecks " + " pt1 " + pt1 + " pt2 " + pt2 + " direction : " + direction);
			
			if (line != null) return line; 
			
			if (EqualPt1HighsLows(pt1, direction))
			{
				
				line = LineChecks(pt1+1, pt2, direction); 
				LogError("CreateRTL - LineChecks " + " pt1 +1 " + pt1 + " pt2 " + pt2 + " direction : " + direction);
			
				if (line != null) return line; 
			}
			
			line = LineChecks(pt1, pt2-1, direction); 
			LogError("CreateRTL - LineChecks " + " pt1 " + pt1 + " pt2 - 1 " + (pt2-1) + " direction : " + direction);
			
			if (line != null) return line;
			
			line = LineChecks(pt1, pt2-2, direction); 
			LogError("CreateRTL - LineChecks " + " pt1 " + pt1 + " pt2 - 2 " + (pt2-2) + " direction : " + direction);
			
			if (line != null) return line;
			
			return null;
		}
		
		
		
		
		
		public TRTL LineChecks(int pt1, int pt2, int direction)
		{
			TPoint point1, point2;
			TRTL line;
			
			if ((pt2 - pt1) < 1) 
			{
				LogError("LineChecks : " + " pt2 " + pt2 + " pt1 " + pt1 + " < 1" );
				return null;
			}
			
			if (direction==UP)
			{
				if (GetLow(pt2) <= GetLow(pt1)) 
				{
					LogError("direction : " + direction + "pt2 : " +  pt2 + " Low " + GetLow(pt2) + " <= " + "pt1 : " +  pt1 + " Low " + GetLow(pt1)); 
					return null;
				}
				
				point1 = new TPoint(pt1, GetLow(pt1));
				point2 = new TPoint(pt2, GetLow(pt2));
				
				
			}
			else
			{
				if (GetHigh(pt2) >= GetHigh(pt1)) 
				{
					LogError("direction : " + direction + "pt2 : " +  pt2 + " High " + GetHigh(pt2) + " >= " + "pt1 : " +  pt1 + " High " + GetHigh(pt1));
					return null;
				}
				
				point1 = new TPoint(pt1, GetHigh(pt1));
				point2 = new TPoint(pt2, GetHigh(pt2));
			}
			
			
			line = new TRTL(point1, point2);
			
			if (CheckLineIntegrity(line)) return line;
			else return null;
		}
		
		private TLTL CreateLTL(TRTL line)
		{	
			int x1 = line.m_BeginPoint.X;
			int x2 = line.m_ExtendedPoint.X;
			
			double y1 = (line.m_Direction==UP) ? GetHigh(x1) : GetLow(x1);
			
			double y;
			double maxdiff = 0;
			int bar = 0;
			
			for (int i = x1; i <= x2; i++)
			{
				y = line.m_Slope*(i-x1) + y1;
				
				double yhl = (line.m_Direction==UP) ? GetHigh(i) : GetLow(i);
				
				double diff = (line.m_Direction==UP) ? (yhl-y) : (y-yhl);
				
				if (((line.m_Direction==UP) && (diff >= maxdiff)) || (((line.m_Direction==DOWN) && (diff >= maxdiff))))
				{
					maxdiff = diff;
					bar = i;
				}
			}
			
			int xpt2 = bar;
			
			double ypt2 = (line.m_Direction==UP) ? GetHigh(xpt2) : GetLow(xpt2);
			
			y1 = ypt2 - (xpt2-x1)*line.m_Slope;
			
			double y2 = ypt2 + (x2-xpt2)*line.m_Slope;
			
			TPoint point1 = new TPoint(x1, y1);
			TPoint point2 = new TPoint(xpt2, xpt2);
			TPoint endpoint = new TPoint(x2, y2);
			
			TLTL ltl = new TLTL(line.m_Slope, point1, point2, endpoint, line.m_Direction);
			
			return ltl;
			
		}
		
		private TLTL CreateLTL(int x1, int x2, int direction)
		{
			double y1 = (direction==UP) ? GetHigh(x1) : GetLow(x1);
			double y2 = (direction==UP) ? GetHigh(x2) : GetLow(x2);
			
			double slope = y2 - y1;
			
			TPoint point1 = new TPoint(x1,y1);
			TPoint point2 = new TPoint(x2, y2);
			TPoint endpoint = new TPoint(x2, y2);
			
			TLTL ltl = new TLTL(slope, point1, point2, endpoint, direction);
			
			return ltl;
			
		}
		
		private TLTL CreateLTL(int x1, int x2, int direction, double slope)
		{
			double y1 = (direction==UP) ? GetHigh(x1) : GetLow(x1);
			double y2 = (direction==UP) ? GetHigh(x2) : GetLow(x2);
			
			TPoint point1 = new TPoint(x1,y1);
			TPoint point2 = new TPoint(x2, y2);
			TPoint endpoint = new TPoint(x2, y2);
			
			TLTL ltl = new TLTL(slope, point1, point2, endpoint, direction);
			
			return ltl;
			
		}
		
		private TRTL CreateRTL(double slope, int x, double y, int direction)
		{
			TRTL rtl = new TRTL(slope, x, y, direction);
			
			return rtl;
		}
		
		public bool CheckLineIntegrity(TLine line, ref int barnum, ref double price)
		{
			for (int i = line.m_BeginPoint.X; i <= line.m_ExtendedPoint.X; i++)
			{
					double y = line.LineValue(i);
				
					if (line.m_Direction==UP) 
					{
						price = GetLow(i);
						
						if (price < y) 
						{
							barnum = i;
				
							LogError("CheckLineIntegrity1: Price : " + price + " < " + " y : " + y);
							
							return false;
						}
					}
					else
					{
						price = GetHigh(i);
						
						if (price > y) 
						{
							barnum = i;
							
							LogError("CheckLineIntegrity1: Price : " + price + " > " + " y : " + y);
							
							return false;
						}
					}
			}
			
			return true;	
		}
		
		public bool CheckLineIntegrity(TLine line)
		{
			for (int i = line.m_BeginPoint.X; i <= line.m_ExtendedPoint.X; i++)
			{
					double y = line.LineValue(i);
				
					if (line.m_Direction==UP) 
					{
						if (GetLow(i) < y) 
						{
							LogError("CheckLineIntegrity2: Low : " + "(" + i + ")" + " = " + + GetLow(i) + " < " + " y : " + y);
							return false;
						}
					}
					else 
					{
						if (GetHigh(i) > y) 
						{
							LogError("CheckLineIntegrity2: High : " + "(" + i + ")" + " = " + + GetHigh(i) + " > " + " y : " + y);
							return false;
						}
					}
			}
			
			return true;	
		}
		#endregion
		
		#region DRAW_FUNCS
		private void DrawChannel(TChannel channel)
		{
			if (channel.Level > 4) channel.LineColor = Color.White;
			
			RemoveDrawObject(channel.RTLTag);
			
			DrawLine(channel.RTLTag, false, m_CurrentBar - channel.RTLX1, channel.RTLY1, 
					m_CurrentBar - channel.RTLX2, channel.RTLY2, channel.LineColor,
			channel.LineStyle, channel.LineWidth);
			
			RemoveDrawObject(channel.LTLTag);
			
			DrawLine(channel.LTLTag, false, m_CurrentBar - channel.LTLX1, channel.LTLY1, 
					m_CurrentBar - channel.LTLX2, channel.LTLY2, channel.LineColor,
			channel.LineStyle, channel.LineWidth);
			
			
		}
		
		private void Draw(TChannel channel)
		{
			if (m_HighestLevelChannelToDraw < channel.Level) return;
			
			DrawTrendLine(channel.RTL, channel.LineColor, channel.LineStyle, channel.LineWidth);
			
			DrawTrendLine(channel.LTL, channel.LineColor, channel.LineStyle, channel.LineWidth);
			
			TLTL ltlx = channel.LTL;
			
			if (ltlx.m_Ve)
			{
				foreach(TLine line in ltlx.m_VeLines) 
				{
					if (line.m_BeginPoint.X == line.m_ExtendedPoint.X) continue;
					
					DrawTrendLine(line, channel.LineColor, channel.LineStyle, channel.LineWidth);
				}
			}
			
			//if (channel.Level == 0) AnnotateChannel(channel);
		}
		
		private void DrawTrendLine(TLine line, Color clr, DashStyle style, int width)
		{
			RemoveDrawObject(line.m_Tag);
			
			DrawLine(line.m_Tag, false, m_CurrentBar - line.m_BeginPoint.X,line.m_BeginPoint.Y, 
					m_CurrentBar - line.m_ExtendedPoint.X, line.m_ExtendedPoint.Y, clr,
			style, width);
		}
		
		private void EraseChannel(TChannel channel)
		{
			RemoveDrawObject(channel.RTLTag);
			RemoveDrawObject(channel.LTLTag);
			
			/*RemoveDrawObject(channel.Point1Tag);
			RemoveDrawObject(channel.Point2Tag);
			RemoveDrawObject(channel.Point3Tag);*/
		}
		
		public void AnnotateChannel(TChannel ch)
		{
	
			bool bpt13, bpt2;
			Color clr;
		
			if (ch.Direction == UP) 	
			{
				bpt13 = false;
				bpt2 = true;
				clr = ch.LineColor;
			}
			else if (ch.Direction == DOWN)
			{
				bpt13 = true;
				bpt2 = false;
				clr = ch.LineColor;
			}
			else return;
		
			//@@		
			ch.Point1Tag = "Point 1" + System.Convert.ToString(ch.Point1);
			ch.Point2Tag = "Point 2" +System.Convert.ToString(ch.Point2);
			ch.Point3Tag = "Point 3" +System.Convert.ToString(ch.Point3);
			
			AnnotateBar("pt1", ch.Point1Tag, ch.Point1, bpt13,clr);
			AnnotateBar("pt2", ch.Point2Tag, ch.Point2, bpt2,clr);
			AnnotateBar("pt3", ch.Point3Tag, ch.Point3, bpt13,clr);
		}
		
		private void AnnotateBar(string text, string tag, int Bar, bool abovebar, Color clr)
		{
			double y;
			
			if (abovebar) y = GetHigh(Bar) + (TickSize*3.3);
			else y = GetLow(Bar) - (TickSize*3.3);
			
			DrawText(tag, text, CurrentBar- Bar, y, clr);
		}
		#endregion
		
		#region CHANNEL_HIGH_LOW
		private int GetChannelHigh(TChannel ch)
		{
			int hh = GetHighestHigh(ch.RTLX2, (ch.RTLX2 - ch.RTLX1));
			
			return hh;
		}
		
		private int GetChannelLow(TChannel ch)
		{
			int ll = GetLowestLow(ch.RTLX2, (ch.RTLX2 - ch.RTLX1));
			
			return ll;
		}	
		
		#endregion
		
		#region CHANNEL_CREATION
		
		private bool ChannelExists(TChannel ch)
		{
			bool b = false;
			
			foreach(TChannel chx in m_Channels)
			{
				if (chx.Broken) continue;
				
				if (chx.RTLTag == ch.RTLTag) b = true;
				else if ((chx.Length == ch.Length) && (chx.Slope == ch.Slope)) b = true;
				//else if ((!chx.Broken) && (chx.Direction == ch.Direction) && (chx.Slope == ch.Slope)) b = true;
				
				if (b)
				{
					if (ch.Length > chx.Length) return false;
					else return true;
				}
			}
			
			return false;
		}
		
		private void AddChannelToListAndDraw(TChannel channel)
		{
			if (channel == null) return;
			
			Draw(channel);
			
			m_Channels.Add(channel);
			
			
		}
		
		private TChannel CreateFTPOrFBPTape(int pt1, int pt2, int direction)
		{
			TChannel channel = null;
			TRTL rtl = null;
			TLTL ltl = null;
			int level = -1;
			
			LogError("CreateFTPOrFBPTape " + " pt1 " + pt1 + " pt2 " + pt2 + 
					" direction " + direction);
			
			ltl = CreateLTL(pt1, pt2, direction);
			
			if (ltl != null) 
			{
				double y = (direction == UP) ? (GetLow(pt2) - ltl.Slope) : (GetHigh(pt2) - ltl.Slope);
				
				rtl = CreateRTL(ltl.Slope, pt1, y, direction);
			}
			else 
			{
				LogError("CreateFTPOrFBPTape : UNABLE TO CREATE RTL : pt1 " + pt1 + " pt2 " + pt2 + " direction " + direction);
				return channel;
			}
			
			TChannel chx = GetHighestLevelActiveChannel();
			
			if (chx != null) 
				LogError("CreateFTPOrFBPTape : Highest active level channel : " + chx.Level + " tag " + chx.RTLTag);
			
			level = (level == -1) ? ((chx != null) ? chx.Level + 1 : 0) : level;
			
			channel = new TChannel(rtl, ltl, level, TickSize);
			
			channel.Pennant = true;
			
			if (ChannelExists(channel))
			{
				LogError("CreateFTPOrFBPTape : Channel not be created. It already exists");
				return null;
			}
			
			Output("FTP/FBP channel  " + channel.RTLTag + " at level " + channel.Level +  " direction " + channel.SDirection + " is created.");
			
			return channel;
			
		}
		
		private TChannel CreateOutsideTapeEx(int pt1, int pt2, int direction)
		{
			TChannel channel = null;
			TRTL rtl = null;
			TLTL ltl = null;
			int level = -1;
			
			LogError("CreateOutsideTape " + " pt1 " + pt1 + " pt2 " + pt2 + 
					" direction " + direction);
			
			ltl = CreateLTL(pt1, pt2, direction);
			
			if (ltl != null) 
			{
				double y = (direction == UP) ? (GetLow(pt2) - ltl.Slope) : (GetHigh(pt2) - ltl.Slope);
				
				rtl = CreateRTL(ltl.Slope, pt1, y, direction);
			}
			else 
			{
				LogError("CreateOutsideTape : UNABLE TO CREATE RTL : pt1 " + pt1 + " pt2 " + pt2 + " direction " + direction);
				return channel;
			}
			
			TChannel chx = GetHighestLevelActiveChannel();
			
			if (chx != null) 
				LogError("CreateOutsideTape : Highest active level channel : " + chx.Level + " tag " + chx.RTLTag);
			
			level = (level == -1) ? ((chx != null) ? chx.Level + 1 : 0) : level;
			
			channel = new TChannel(rtl, ltl, level, TickSize);
			
			channel.Pennant = true;
			
			if (ChannelExists(channel))
			{
				LogError("CreateOutsideTape : Channel not be created. It already exists");
				return null;
			}
			
			Output("Outside channel  " + channel.RTLTag + " at level " + channel.Level +  " direction " + channel.SDirection + " is created.");
			
			return channel;
			
		}
		
		private TChannel CreateOutsideTape(int pt1, int pt2, int direction)
		{
			TChannel channel = null;
			TRTL rtl = null;
			TLTL ltl = null;
			int level = -1;
			
			LogError("CreateOutsideTape " + " pt1 " + pt1 + " pt2 " + pt2 + 
					" direction " + direction);
			
			ltl = CreateLTL(pt1, pt2, direction);
			
			if (ltl != null) rtl = CreateRTL(ltl.Slope, ltl.m_BeginPoint.X,ltl.m_BeginPoint.Y, direction);
			else 
			{
				LogError("CreateOutsideTape : UNABLE TO CREATE RTL : pt1 " + pt1 + " pt2 " + pt2 + " direction " + direction);
				return channel;
			}
			
			TChannel chx = GetHighestLevelActiveChannel();
			
			if (chx != null) 
				LogError("CreateTape : Highest active level channel : " + chx.Level + " tag " + chx.RTLTag);
			
			level = (level == -1) ? ((chx != null) ? chx.Level + 1 : 0) : level;
			
			channel = new TChannel(rtl, ltl, level, TickSize);
			
			if (ChannelExists(channel))
			{
				LogError("CreateTape : Channel not created. It already exists");
				return null;
			}
			
			Output("Outside channel  " + channel.RTLTag + " at level " + channel.Level +  " direction " + channel.SDirection + " is created.");
			
			return channel;
			
		}
		
		private TChannel CreateTape(int pt1x, double pt1y, int pt2, int direction, int level, bool inside, bool joined)
		{
			TChannel channel = null;
			
			if (pt1x == -1) return channel;
			
			LogError(CurrentBar + " CreateTape " + " pt1 " + pt1x + "," + pt1y + 
					 " pt2 " + pt2 + " direction " + direction +  " level : " + 
					 level + " inside " + inside);
			
			TLTL ltl = null;
			TRTL rtl = CreateRTL(pt1x, pt1y, pt2, direction);
			
			if (rtl != null) ltl = CreateLTL(rtl);
			else 
			{
				LogError(CurrentBar + " CreateTape : UNABLE TO CREATE RTL : pt1 " + pt1x + "," + 
						pt1y  + " pt2 " + pt2 + " direction " + direction + " level " + level +
				     " inside " + inside);
				return channel;
			}
			
			LogError(CurrentBar + " CreateTape : Get Highsest Level channel");
			
			TChannel chx = GetHighestLevelActiveChannel();
			
			if (chx != null) 
				LogError(CurrentBar + " CreateTape : Highest active level channel : " + chx.Level + " tag " + chx.RTLTag);
			
			level = (level == -1) ? ((chx != null) ? chx.Level + 1 : 0) : level;
			
			channel = new TChannel(rtl, ltl, level, TickSize);
			
			channel.Inside = inside;
			
			channel.Joined = joined;
			
			if (ChannelExists(channel))
			{
				LogError(CurrentBar + " CreateTape : Channel not created. It already exists");
				return null;
			}
			
			//@@change
			if (Math.Abs(channel.Angle) < 4)
			{
				LogError(CurrentBar + " CreateTape : Channel not created: Angle is less than 4 degrees.");
				return null;
			}
			
			Output(channel.RTLTag + " at level " + channel.Level +  " direction " + channel.SDirection + " is created.");
			
			return channel;
			
		}
		
		
		private TChannel CreateTape(int pt1, int pt2, int direction, int level, bool inside, bool joined)
		{
			TChannel channel = null;
			
			if (pt1 == -1) return channel;
			
			LogError(CurrentBar + " CreateTape " + " pt1 " + pt1 + " pt2 " + pt2 + " direction " + direction +  " level : " + level + " inside " + inside);
			
			TLTL ltl = null;
			TRTL rtl = CreateRTL(pt1, pt2, direction);
			
			if (rtl != null) ltl = CreateLTL(rtl);
			else 
			{
				LogError(CurrentBar + " CreateTape : UNABLE TO CREATE RTL : pt1 " + pt1 + " pt2 " + pt2 + " direction " + direction + " level " + level +
				     " inside " + inside);
				return channel;
			}
			
			LogError(CurrentBar + " CreateTape : Get Highsest Level channel");
			
			TChannel chx = GetHighestLevelActiveChannel();
			
			if (chx != null) 
				LogError(CurrentBar + " CreateTape : Highest active level channel : " + chx.Level + " tag " + chx.RTLTag);
			
			level = (level == -1) ? ((chx != null) ? chx.Level + 1 : 0) : level;
			
			channel = new TChannel(rtl, ltl, level, TickSize);
			
			channel.Inside = inside;
			
			channel.Joined = joined;
			
			if (ChannelExists(channel))
			{
				LogError(CurrentBar + " CreateTape : Channel not created. It already exists");
				return null;
			}
			
			//@@change
			if (Math.Abs(channel.Angle) < 4)
			{
				LogError(CurrentBar + " CreateTape : Channel not created: Angle is less than 4 degrees.");
				return null;
			}
			
			Output(channel.RTLTag + " at level " + channel.Level +  " direction " + channel.SDirection + " is created.");
			
			return channel;
			
		}
		#endregion
		
		#region CHANNEL_EXTENSION
		
		private void ExtendChannelsEx(TBar curbar, TBar prevbar)
		{
			for(int i = 0; i < m_Channels.Count; i++)
			{
				TChannel ch = m_Channels[i];
				
				if (ch.Broken) continue;
				
				if (ch.RTLX2 == curbar.m_BarNum) continue;
				
				ch.Extend(curbar, prevbar, IsInsideBar(curbar.m_BarNum, prevbar.m_BarNum));
				
				Output("Channel " + ch.RTLTag + " level " + ch.Level + " direction " + ch.SDirection + " extended.");
				
				if (ch.FBO)
				{
					TLTL ltlve = CreateLTL(ch.RTLX1, ch.RTLX1 + 1, ch.Direction, ch.Slope);
					
					EraseChannel(ch);
					
					ch.LTL = ltlve;
					
					
					
					for (int j = ch.RTLX1 + 1; j <= curbar.m_BarNum; j++)
					{
							TBar pbar = CreateBarObject(j-1,j-2);
							TBar cbar = CreateBarObject(j,j-1);
						
							ch.Extend(cbar, pbar, IsInsideBar(j,j-1));
						
							if (ch.Broken)
							{
								if (ch.Point3 == j) ch.Broken = false;	
							}
					}
					
					ch.FBO = false;
				}
				
				Draw(ch);
				
				if (ch.Broken) 
				{
					m_BrokenDirection = ch.Direction;
					
					LogError("Channel " + ch.RTLTag +  " level " + ch.Level + " has been added to list of broken channels.");
					m_BrokenChannels.Add(ch);
					
					if (m_CurrentTape.RTLTag == ch.RTLTag) m_TapeState = -1;
				}
				
				FlagDuplicateChannel(ch);
			}	
		}
	
		private void HandleChannelBreaks(int Bar)
		{
			m_BrokenChannels.Sort(new ILevelComparer());
			
			int count = 0;
			
			foreach(TChannel bch in m_BrokenChannels)
			{
				LogError("HandleChannelBreaks: " + bch.RTLTag  + 
				"  Break same direction lower level channels - level : " + bch.Level);
				
				count = BreakLowerChannels(bch);
			}
			
			m_BrokenChannels.Clear();
			
		}
		
		private int BreakLowerChannels(TChannel ch)
		{
			int count = 1;
			
			for(int i = 0; i < m_Channels.Count; i++)
			{
				if ((ch.Direction == m_Channels[i].Direction) && (!m_Channels[i].Broken) && 
					(m_Channels[i].Level > ch.Level)) 
				{
					LogError("Channel " + m_Channels[i].RTLTag + " direction :  " + m_Channels[i].Direction + 
					" level : " + m_Channels[i].Level + " is broken ");
					m_Channels[i].Broken = true;
					m_Channels[i].Joined = true;
					
					count++;
				}
			}
			
			return count;
		}
		
		private void BrokenChannels()
		{
			foreach(TChannel bch in m_BrokenChannels)
			{
				Output("Channel : " + bch.RTLTag+ " level : " 
					+ bch.Level +  " direction : " + bch.SDirection + " is broken.");
				
				
			}
			
		}
		
		
		
		
		
		#endregion
		
		#region CHANNEL_REMOVAL
		private void RemoveChannel(TChannel ch)
		{
			int index  = -1;
			
			for (int i = 0; i < m_Channels.Count; i++)
			{
				if (m_Channels[i].RTLTag == ch.RTLTag) index = i;
			}
			
			if (index != -1) m_Channels.RemoveAt(index);
			
			index = -1;
			
			for (int i = 0; i < m_BrokenChannels.Count; i++)
			{
				if (m_BrokenChannels[i].RTLTag == ch.RTLTag) index = i;
			}
			
			if (index != -1) m_BrokenChannels.RemoveAt(index);
		}
		
		private void Remove2BarInsideChannel()
		{
			TChannel rch = null;
			int index = -1;
			
			for (int i = 0; i < m_Channels.Count; i++)
			{	
				if ((m_Channels[i].Inside) && (m_Channels[i].Length == 2) && (m_Channels[i].Broken)) 
				{
					rch = 	m_Channels[i];
					index = i;
					
					Output("Channel " + rch.RTLTag + " will be removed. " + " broken " + 
								rch.Broken + " length " + rch.Length + " index " + index);
					
					break;	
				}
			}
			
			if (index != -1) 
			{
				UpgradeActiveLowerLevelChannels(rch.Level, rch.RTLTag);
				RemoveChannel(rch);
				EraseChannel(rch);
			}
		}		
		
		private void FlagUnneededJoinedChannel(TChannel ch, int Bar)
		{
			TChannel erasech = null;
			
			foreach(TChannel jch in m_Channels)
			{
				if ((jch.Broken) && (jch.Point3 > ch.RTLX1) && (jch.Point3 < ch.RTLX2)  && 
					(jch.Direction != ch.Direction) && (jch.RTLTag != ch.RTLTag) && (jch.Joined))
				{
					LogError("Channel " + jch.RTLTag + " is flagged for removal because of channel " + ch.RTLTag);
					
					m_UnneededJoinChannel = jch;
					
					break;
				}
			}
		}
		
		private void RemoveUnneededJoinedChannel()
		{
			if (m_UnneededJoinChannel != null)
			{
				LogError("Channel " + m_UnneededJoinChannel.RTLTag + " is removed because of it is  not needed!");
				
				RemoveChannel(m_UnneededJoinChannel);
				EraseChannel(m_UnneededJoinChannel);
				
				m_UnneededJoinChannel.Removed = true;
				
				UpgradeBrokenLowerChannels(m_UnneededJoinChannel);
				
				m_UnneededJoinChannel = null;
				
				
				
				
			}
			
			if (m_DuplicateChannel != null)
			{
				LogError("Duplicate Channel " + m_DuplicateChannel.RTLTag + " is removed!");
				
				RemoveChannel(m_DuplicateChannel);
				EraseChannel(m_DuplicateChannel);
				
				UpgradeBrokenLowerChannels(m_DuplicateChannel);
				
				m_DuplicateChannel = null;
			}
		}
		
		private void FlagDuplicateChannel(TChannel chx)
		{
			TChannel rch = null;
			
			foreach(TChannel ch in m_Channels)
			{
				int anglediff = Math.Abs(Math.Abs(ch.Angle) - Math.Abs(chx.Angle));
				
				if ((!ch.Broken) && (!chx.Broken) && (ch.Direction == chx.Direction) && (anglediff <= 2) && 
					(ch.Length > chx.Length)) rch = chx;
				else if (ch.Label == chx.Label) rch = chx;
				
			}
			
			if (rch != null) 
			{
				LogError("Channel " + rch.RTLTag + " is duplicate and should be removed!");
				
				m_DuplicateChannel = rch;
			}
		}
		#endregion
		
		#region CHANNEL_SEARCH
		
		private List<TChannel> FindChannelsAtBar(int Bar)
		{
			List<TChannel> lst = new List<TChannel>();
			
			foreach(TChannel ch in m_Channels)
			{
				if ((ch.RTLX2 == Bar) && (!ch.Broken)) lst.Add(ch);
			}
			
			return lst;
			
			
		}
		
		private bool ChannelAtBar(int Bar)
		{
			bool b = false;
			
			foreach(TChannel ch in m_Channels)
			{
				if ((ch.RTLX2 == Bar) && (!ch.Broken)) b= true;
			}
			
			return b;
			
		}
		
		private TChannel GetCurrentActiveChannel(int level)
		{
			TChannel pch = null;
			
			foreach(TChannel ch in m_Channels)
			{
				if ((ch.Level == level) && (!ch.Broken)) 
				{
					pch = ch;
					break;
				}
			}
			
			return pch;
		}
		
		private bool CheckMerge(TChannel mchannel, int level)	
		{
			TChannel ch = GetCurrentActiveChannel(level);
			
			if (ch == null) return true;
			
			return(IsInChannel(mchannel, ch));
		}
		
		private int GetChannelTapes(TChannel ch, int level)
		{
			int count = 0;
			
			foreach(TChannel chx in m_Channels)
			{
				if (IsInChannel(chx, ch))
				{
						if (chx.Level == level) count++;
				}
			}
			
			return count;
		}
		
		private bool IsInChannel(TChannel child, TChannel parent)
		{
			if ((child.RTLX1 >= parent.RTLX1) && (child.RTLX2 <= parent.RTLX2)) return true;
			else return false;	
		}
		
		private bool ContainsChannel(TChannel child, TChannel parent)
		{	
			if ((child.RTLX1 >= parent.RTLX1) && (child.RTLX1 <= parent.RTLX2)) return true;
			else return false;
		}
		
		private TChannel GetParentChannel(int Bar, int level, int direction)
		{
			TChannel pch = null;
			
			foreach(TChannel ch in m_Channels)
			{
				if (((ch.RTLX1 <= Bar) && (ch.RTLX2 >= Bar)) && 
					(ch.Level == level) && (ch.Direction == direction) && (ch.Broken))  
				{
					pch = ch;
					break;
				}
			}
			
			return pch;
		}
		
		private TChannel GetParentChannel(TChannel cch)
		{
			TChannel pch = null;
			
			pch = GetChannel(cch.Parent);
			
			if ((pch != null) && (pch.Level == cch.Level)) return pch;
			
			foreach(TChannel ch in m_Channels)
			{
				if (((ch.RTLX1 <= cch.RTLX1) && (ch.RTLX2 >= cch.RTLX1)) && 
					(ch.Level == cch.Level) && (ch.Direction != cch.Direction) && (ch.Broken))  
				{
					pch = ch;
					break;
				}
			}
			
			return pch;
		}
		
		private TChannel GetChannel(string tag)
		{	
			foreach(TChannel ch in m_Channels)
			{
				if (ch.RTL.m_Tag == tag) return ch;
			}
			
			return null;
		}
		
		private TChannel GetActiveChannel(int level)
		{	
			foreach(TChannel ch in m_Channels)
			{
				if ((ch.Level == level) && (!ch.Broken)) return ch;
			}
			
			return null;
		}
		
		private TChannel GetHighestLevelBrokenChannel()
		{
			int level = -1;
			TChannel ch = null;
	
			foreach(TChannel bch in m_BrokenChannels)
			{
				bool inside = false;
				
				LogError("GetHighestLevelBrokenChannel " + bch.RTLTag + " level " + bch.Level + 
				" direction " + bch.Direction + " broken " + bch.Broken + " LEVEL " + level + " inside " + bch.Inside);
				
				 
				if ((bch.Inside) && (bch.Length == 2)) inside = true;
				
				if ((bch.Broken) && (bch.Level > level) && (!inside))
				{
					LogError("GetHighestLevelBrokenChannel CH =  " + bch.RTLTag + " level " + bch.Level + 
				" direction " + bch.Direction);
					level = bch.Level;
					ch = bch;
				}
			}
			
			return ch;
			
		}
		
		private TChannel GetLowestLevelBrokenChannel()
		{
			int level = 100;
			TChannel ch = null;;
			
			foreach(TChannel bch in m_BrokenChannels)
			{
				bool inside = false;
				
				LogError("GetLowestLevelBrokenChannel " + bch.RTLTag + " level " + bch.Level + 
				" direction " + bch.Direction + " broken " + bch.Broken + " LEVEL " + level + " inside " + bch.Inside);
				
				 
				if ((bch.Inside) && (bch.Length == 2)) inside = true;
				
				if ((bch.Broken) && (bch.Level < level) && (!inside))
				{
					LogError("GetLowestLevelBrokenChannel CH =  " + bch.RTLTag + " level " + bch.Level + 
				" direction " + bch.Direction);
					
					level = bch.Level;
					ch = bch;
				}
			}
			
			return ch;
			
		}
		
		private TChannel GetHighestLevelActiveChannel()
		{
			int level = -1;
			TChannel ch = null;;
		
			foreach(TChannel ich in m_Channels)
			{	
				if ((!ich.Broken) && (ich.Level > level))
				{	
					level = ich.Level;
					ch = ich;
				}
			}
			
			return ch;
		}
		
		private TChannel GetLowestLevelActiveChannel()
		{
			int level = -1;
			TChannel ch = null;;
			
			foreach(TChannel ich in m_Channels)
			{
				if ((!ich.Broken) && (ich.Level > level))
				{
					level = ich.Level;
					ch = ich;
				}
			}
			
			return ch;
		}
		
		#endregion
		
		#region CHANNEL_MODIFICATIONS
		private void AssignChannelLevels()
		{
			int level = 0;
			
			m_Channels.Sort(new ILengthComparerDesc());
			
			List<TChannel> lst = new List<TChannel>();
			
			for(int i = 0; i < m_Channels.Count; i++)
			{
					if (!m_Channels[i].Broken) 
					{ 
						if (m_Channels[i].Level > level) 
						{
							LogError("Channel " + m_Channels[i].RTLTag + " has been upgraded from level " + m_Channels[i].Level + " to " + level);
							
							UpgradeBrokenLowerChannels(m_Channels[i]);
						}
						
						TChannel jch = GetAJoinChannel( m_Channels[i]);
						
						if (jch != null) 
						{
							//@@1.CHANGE FROM V5
							m_Channels[i].Joined = true;
							
							lst.Add(jch);
						}
						
						int vr =  m_Channels[i].Level + 1;
						
						//LogError("NEW" + CurrentBar + "  " + (level + 1)  + " to " +  vr);
						
						for (int j = level + 1; j < vr; j++)
						{
							LogError(CurrentBar + "************NEW**************" + m_Channels[i].Label +   " j " + j + " Changed level from " + 
																	m_Channels[i].Level  + " to " +  (m_Channels[i].Level - 1));
						    m_Channels[i].Level = m_Channels[i].Level - 1;
							
							jch = GetAJoinChannel( m_Channels[i]);
							
							lst.Add(jch);
			
			
						}
						
						level++;
						
						Draw(m_Channels[i]);
					}
					
			}
			
			
			foreach(TChannel jch in lst) 
			{
			   
				if (jch != null) ProcessJoinedChannel(jch, jch.RTLX2);
			}
		}
		
		private void UpgradeBrokenLowerChannels(TChannel ch)
		{
			for(int i = 0; i <m_Channels.Count; i++)
			{
				if ((m_Channels[i].RTLX1 >= ch.RTLX1) && (m_Channels[i].RTLX2 <= ch.RTLX2) && (m_Channels[i].Broken))
				{
					int level = m_Channels[i].Level;
					
					if (ch.Removed)
					{
						m_Channels[i].Level--;
					}
					else
					{
						if (m_Channels[i].Level > ch.Level) m_Channels[i].Level--; 
					}
					
					Draw(m_Channels[i]);
					
					LogError("Channel " + m_Channels[i].RTLTag + " has been upgraded from " + level + " to " + m_Channels[i].Level +
					 " due to breakage of " + ch.RTLTag);
				}
			}
			
		}
		
		private void UpgradeActiveLowerLevelChannels(int level, string tag)
		{
			int index = 0;
			
			m_Channels.Sort(new ILevelComparer());
			
			LogError("UpgradeActiveLowerLevelChannels: " + tag + " is broken.");
			
			for(int i = 0; i <m_Channels.Count; i++)
			{
				
				if ((m_Channels[i].Level > level) && (!m_Channels[i].Broken)) 
				{
					m_Channels[i].Level = m_Channels[i].Level - 1;
					
					Output("UpgradeActiveLowerLevelChannels: Channel " + m_Channels[i].RTLTag +  " upgraded  " +  " to level " +  m_Channels[i].Level + 
					" due to breakage of " + tag);
					
					Draw(m_Channels[i]);
				}
			}
		}
		
		private void DownGradeChannels(TChannel ch)
		{
			for(int i = m_Channels.Count - 1; i >= 0; i--)
			{	
				if ((ch.RTLX1 <= m_Channels[i].RTLX1) && 
								(ch.RTLX2 >= m_Channels[i].RTLX2))
				{	
					if ((m_Channels[i].RTLX1 == ch.RTLX1) && (m_Channels[i].RTLX2 == ch.RTLX2)) continue;
					
					//if ((m_Channels[i].Level < ch.Level) && (!m_Channels[i].Broken)) continue;
					
					int templevel = m_Channels[i].Level;
					
					if (m_Channels[i].Level < ch.Level) m_Channels[i].Level =ch.Level + 1;
					else m_Channels[i].Level++;
					
					string active = (m_Channels[i].Broken) ? "Broken" : "Active";
					
					LogError("Downgrade channels within " + ch.RTLTag);
					if (!m_Channels[i].Broken) Output("Channel " + m_Channels[i].RTLTag + " " + 
							active + " is downgraded from level " + templevel + " to level " + m_Channels[i].Level);
					
					//m_Channels[i].Joined = true;
					
					Draw(m_Channels[i]);
				}
			}
		}
	
		#endregion
		
		#region CHANNEL_STATS
		
		private void XYZ(int Bar)
		{
			string header = "Bar\tDirection\t\tType\t\tMove #\t\tVe\t\tVe Event\t\tVolume Type\t\tVolume Cycle\t\tVolume Sig\t\tPeak Volume";
			
			OutputTable(" ");
			OutputTable(header);
			OutputTable("================================================================================================" +
						"=================================================================================================================");
			
			System.Collections.Generic.IEnumerable<TChannel> query = 
				from TChannel in m_Channels where TChannel.Broken == false select TChannel;
			
			foreach (TChannel ch1 in query)
			{
				TChannel ch = ch1;
				
				string ve =  "";
				string veevent = "";
				int components = 0;
				
				GetVolSeq(ref ch, components, Bar);
			
				SetChannelState(ref ch);
				
				if (Bar == ch.VeBarNum) 
				{
					ve = "YES";
					
					veevent = (ch.Direction == UP) ? (((GetClose(Bar) >= ch.LTL.m_ExtendedPoint.Y) ? "ABOVE" : "BELOW")) : 
								(((GetClose(Bar) <= ch.LTL.m_ExtendedPoint.Y) ? "BELOW" : "ABOVE"));	
					
					
				}
				
				string row = Bar + "\t" + ch.SDirection + "\t\t" + ch.ChannelType + " " + "\t" + components + 
						"\t\t" + ve + "\t\t" + veevent + "\t\t" + ch.SState + "\t\t\t" + ch.VolumeSequence + "\t\t" + " " + "\t\t" + " ";
				
				OutputTable(row);
			}
		}
		
		private void PrintChannelBreakInfoHeader(TChannel ch)
		{
			string header = "Channel\t\tBreak Bar\t\tBar Type";
		}
		
		private void PrintChannelBreakInfo()
		{
			
		}
		
		
		private void PrintHeaderInfo()
		{
			string s1 = "Channel\t\tPt 2\tVe\tPt 3\tFBO\tLevel\tDirection\t\tLength\t\tAngle\t" +
				"Gann Angle\tComponents\tHL\tPHL\tMFI\t\tChannel Type\tVolSeq";
			
			OutputTable(s1);
			OutputTable("===============================================================================================================" +
			"====================================================================================================");
			
		}
		
		private string PrintChannelInfo(TChannel ch, int components, int Bar)
		{
			string signal = "";
			this.G_CurrentBar = Bar;
			
			string smfi = GetMFIString(Bar);
			
			string sbartype = GetBarType(Bar);
			
			GetVolSeq(ref ch, components, Bar);
			
			SetChannelState(ref ch);
			
			string chevent = GetChannelEvent(ch, Bar);
			
			string s1 = ch.RTLTag +"\t" + Convert.ToString(ch.Point2) + "\t" + Convert.ToString(ch.VeBarNum) + 
				"\t" +Convert.ToString(ch.Point3) + "\t" + Convert.ToString(ch.FBOBarNum) + 
				"\t" + Convert.ToString(ch.Level) + "\t" +ch.SDirection + 
				"\t\t" + Convert.ToString(ch.Length) + "\t" + 
				"\t" + Convert.ToString(ch.Angle) + "\t" + ch.GannAngle + "\t\t" + Convert.ToString(components) + 
				"\t\t" + Convert.ToString(ch.ChannelHL) + "\t" + Convert.ToString(ch.PrevChannelHL) + 
				"\t" + smfi +
				"\t\t" + ch.ChannelType + "\t" + ch.VolumeSequence;
			
			
			
			OutputTable(s1);
			
			return signal;
		}
		
		private void PrintBarInfoHeader()
		{
				string s = "Bar\t\t\tBar Type\t\t\t\t\t\t\tBar Body Type\t\t\tBar Close Type";
			
				OutputTable(s);
				OutputTable("=====================================================================================" +
			 "============================================");
			
				
		}
		
		private void PrintBarInfo(int Bar)
		{
			TBar bartype = CreateBarObject(Bar,Bar-1);
			
			string s = Convert.ToString(Bar) + "\t\t\t" + 
				       bartype.GetConstString(bartype.m_BarType) + "\t\t\t\t" + 
				       bartype.GetConstString(bartype.m_BodyType) + "\t\t\t\t" +
					   bartype.GetConstString(bartype.m_CloseType);	
			
			OutputTable(s);
			
			OutputTable("");
			
			
		}
		
		private void PrintStateTableHeaderInfo()
		{
			string header = "Channel\t\t\tDirection\t\tLevel\t\tState\t\tChannel Event\t\t\tChannel Parent";
			
			OutputTable(" ");
			OutputTable(header);
			OutputTable("============================================================================================");
			
		}
		
		private void PrintStateTable(List<TChannel> chlist, int Bar)
		{
			string row;
			
			foreach(TChannel ch in chlist)
			{
				DetermineChannelEvent(ch, Bar);
				
				row = ch.RTLTag + "\t\t" + ch.SDirection + "\t\t" + ch.Level + "\t\t" + ch.SState + "\t\t" + ch.SChannelEvent + "\t\t\t" +
						ch.Parent;	
				
				OutputTable(row);
			}
		}
		
		private void PrintStats(int Bar)
		{
			Output("@@@@@@@@@@@@@@@@@@@@@");
			
			List<TChannel> chinfo = new List<TChannel>();
			
			TChannel prevch = null;
			
			for (int i = 0; i < m_Channels.Count; i++)
			{
				TChannel ch = m_Channels[i];
				
				if (!ch.Broken)
				{
					SetChannelHL(ref ch);
				
					m_Channels[i] = ch;
					
					chinfo.Add(ch);	
				}
			}
			
			chinfo.Sort(new ILevelComparerAsc());
			
			PrintHeaderInfo();
			
			string psignal = "";
			string signal = "";
			
			
			foreach(TChannel ch in chinfo)
			{
				if (!ch.Broken) 
				{
					int levels = -1;
					
					
					levels = GetChannelTapes(ch, ch.Level + 1);
					
					psignal = PrintChannelInfo(ch, levels, Bar);
					
					if (psignal.Length != 0) signal = psignal;
				}	
			}
			
			Output("@@@@@@@@@@@@@@@@@@@@@");	
			//Output(signal + " @ " + GetClose(Bar));
			
			PrintStateTableHeaderInfo();
			PrintStateTable(chinfo, Bar);
			
			Output("@@@@@@@@@@@@@@@@@@@@@");
			
			signal = "";
		}
		#endregion
		
		#region TAPE_FUNCTIONS
		private TChannel GetTape(int Bar)
		{
			TChannel ch = null;
			
			if (IsHigherHighHigherLow(Bar,Bar-1)) ch = CreateTape(Bar-1, Bar, UP, -1, false, false);
			else if (IsLowerHighLowerLow(Bar,Bar-1)) ch = CreateTape(Bar-1, Bar, DOWN, -1, false, false);
			else if ((GetHigh(Bar) < GetHigh(Bar-1)) && (GetLow(Bar) == GetLow(Bar-1))) ch = CreateTape(Bar-1, Bar, DOWN, -1, false, false);
			else if ((GetLow(Bar) > GetLow(Bar-1)) && (GetHigh(Bar) == GetHigh(Bar-1))) ch = CreateTape(Bar-1, Bar, UP, -1, false, false);
			else if (IsStitchLong(Bar, Bar -1)) ch = CreateFTPOrFBPTape(Bar-1, Bar, UP);
			else if (IsStitchShort(Bar, Bar -1)) ch = CreateFTPOrFBPTape(Bar-1, Bar, DOWN);
			//else if ((IsFTP(Bar,Bar-1))) ch = CreateFTPOrFBPTape(Bar-1,Bar, DOWN);
			//else if ((IsFBP(Bar,Bar-1))) ch = CreateFTPOrFBPTape(Bar-1,Bar, UP);
			
			return ch;
		}
		
		
		
		private void Find2BarTape(int Bar)
		{
			TChannel chx = GetHighestLevelActiveChannel();
			
			TChannel tape = GetTape(Bar);
			
			int direction = (chx != null) ? chx.Direction : -1;
			
			if (tape != null)
			{
				if (chx != null) tape.Parent = chx.RTLTag;
				
				if (tape.Direction != direction) AddChannelToListAndDraw(tape);
				else if (m_BrokenDirection == direction)
				{
					//AddChannelToListAndDraw(tape);
					m_BrokenDirection = -1;
				}
			}
			
			
		}
		
		private void Find2BarOutSideOrInsideBar(int Bar)
		{
			TChannel tapeup = null;
			TChannel tapedn = null;
			
			if (IsInsideBar(Bar,Bar-1))
			{
				tapeup = CreateTape(Bar-1, Bar, UP, -1, true, false);
				tapedn = CreateTape(Bar-1, Bar, DOWN, -1, true, false);
			}
			else if (IsOutSideBar(Bar,Bar-1))
			{
				//tapeup = CreateOutsideTapeEx(Bar-1,Bar,UP);
				//tapedn = CreateOutsideTapeEx(Bar-1,Bar,DOWN);
				
			}
			
			if (GetClose(Bar) > GetClose(Bar-1))
			{
				if (tapeup != null) 
				{
					m_CurrentTape = tapeup;
				
					m_TapeState = UP;
				
					AddChannelToListAndDraw(tapeup);
					AddChannelToListAndDraw(tapedn);
				}
			}
			else
			{
				if (tapedn != null) 
				{
					m_CurrentTape= tapedn;
				
					m_TapeState = DOWN;
				
					AddChannelToListAndDraw(tapedn);
					AddChannelToListAndDraw(tapeup);
				}
			}
			
			
		}
		
		#endregion
		
		#region VOLUME
		
		private bool IsVolumeInc(int direction, int start, int end)
		{
			bool b = false;
			
			
			
			int volcolor = (direction == UP) ? BLACK_COLOR : RED_COLOR;
			
			for (int i  = start; i <= end; i++)
			{
				if (i == 0) continue;
				
				if ((this.TBUpDnVol()[i] == volcolor) && (GetVolume(i) > GetVolume(i-1))) 
				{ 
					b = true;	
					break;
				}
				
			}
			
			return b;
		}
	
		
		#endregion
		
		#region TEST
        private void TestLine(int Bar, int start, int end, int direction)
		{
			try
			{
				
					rtltest = CreateRTL(0, 1, direction);
				
					if (rtltest != null)  
					{
					
						DrawTrendLine(rtltest, Color.Blue, DashStyle.Solid, 1);	
					
						ltltest = CreateLTL(rtltest);
					
						DrawTrendLine(ltltest, Color.Blue, DashStyle.Solid, 1);	
						
						created = true;
					}
					else Print("NULL");
					
					if (created)
					{
						TBar curbar = new TBar(Bar, Open[0], High[0], Low[0], Close[0], -1,-1,-1);
						TBar prevbar = new TBar(Bar-1, Open[1], High[1], Low[1], Close[1],  -1,-1,-1);
						
						rtltest.Extend(curbar, prevbar, IsInsideBar(Bar,Bar-1));
						
						if (rtltest.m_FBO)
						{
							TLTL ltlve = CreateLTL(rtltest);
							ltlve.m_Tag = ltltest.m_Tag;
							
							DrawTrendLine(rtltest, Color.Blue, DashStyle.Solid, 1);
							DrawTrendLine(ltlve, Color.Blue, DashStyle.Solid, 1);	
							
						}
					}
				
			}
			catch (System.Exception e)
			{
				Output(e.ToString());	
			}
		}
		
		private void TestLTL(int bar, int start, int end, int direction)
		{
			if (bar != end) return;
			
			TRTL rtltest = CreateRTL(start, end, direction);
			
			if (rtltest == null)
			{
				Output("No RTL found.");
				return;
			}
			
			DrawTrendLine(rtltest, Color.Blue, DashStyle.Solid, 1);	
			
			TLTL ltltest = CreateLTL(rtltest);
			
			DrawTrendLine(ltltest, Color.Blue, DashStyle.Solid, 1);
			
			
			
		}
		
		private void PrintInsideChannelStats()
		{
				/*if ((prevch != null) && (prevch.Level == ch.Level) && (prevch.Direction == ch.Direction))
				{
					if (prevch.Inside) LogError("Channel " + prevch.RTLTag + " is inside and duplicate of " + ch.RTLTag);	
					else if (ch.Inside) LogError("Channel " + ch.RTLTag + " is inside and duplicate of " + prevch.RTLTag);	
				}
				
				prevch = ch;*/	
		}
		#endregion
		
		#region SIGNALS
		
		private void DetermineChannelEvent(TChannel ch, int Bar)
		{
			if (ch.VeBarNum != 0)
			{
				TBar curbar = CreateBarObject(Bar, ch.VeBarNum);
				
				switch(ch.Direction)
				{
					case UP:
						//
						if ((GetHigh(Bar) == GetOpen(ch.VeBarNum)) && 
							(GetClose(Bar) <= GetOpen(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_HIGH_EQ_OPEN_REJECT_OFF_OPEN;
						//
						else if ((GetHigh(Bar) > GetOpen(ch.VeBarNum)) && 
							(GetClose(Bar) <= GetOpen(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_HIGH_ABOVE_CLSOE_REJECT_OFF_OPEN;
						//
						else if ((GetHigh(Bar) == GetClose(ch.VeBarNum)) && 
							(GetClose(Bar) <= GetClose(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_HIGH_EQ_CLOSE_REJECT_OFF_CLOSE;
						//
						else if ((GetHigh(Bar) > GetClose(ch.VeBarNum)) && 
							(GetClose(Bar) <= GetClose(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_HIGH_ABOVE_CLOSE_REJECT_OFF_CLOSE;
						//
						else if ((GetHigh(Bar) == GetHigh(ch.VeBarNum)) && 
							(GetClose(Bar) <= GetOpen(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_HIGH_EQ_HIGH_CLOSE_REJECT_OFF_OPEN;
						//
						else if ((GetHigh(Bar) > GetHigh(ch.VeBarNum)) && 
							(GetClose(Bar) <= GetOpen(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_HIGH_ABOVE_HIGH_CLOSE_REJECT_OFF_OPEN;
						//
						else if ((GetHigh(Bar) == GetHigh(ch.VeBarNum)) && 
							(GetClose(Bar) <= GetClose(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_HIGH_EQ_HIGH_CLOSE_REJECT_OFF_CLOSE;
						//
						else if ((GetHigh(Bar) > GetHigh(ch.VeBarNum)) && 
							(GetClose(Bar) <= GetClose(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_HIGH_ABOVE_HIGH_CLOSE_REJECT_OFF_CLOSE;
						//
						else if ((GetHigh(Bar) == GetHigh(ch.VeBarNum)) && 
							(GetClose(Bar) <= GetHigh(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_HIGH_EQ_HIGH_CLOSE_REJECT_OFF_HIGH;
						//
						else if ((GetHigh(Bar) > GetHigh(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetHigh(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_HIGH_ABOVE_HIGH_CLOSE_REJECT_OFF_HIGH;
						else ch.ChannelEvent = TChannel.VE_NONE;
					break;
						
					case DOWN:
						//
						if ((GetLow(Bar) == GetOpen(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetOpen(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_LOW_EQ_OPEN_BOUNCE_OFF_OPEN;
						//
						else if ((GetLow(Bar) < GetOpen(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetOpen(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_LOW_BELOW_CLOSE_BOUNCE_OFF_OPEN;
						//
						else if ((GetLow(Bar) == GetClose(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetClose(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_LOW_EQ_CLOSE_BOUNCE_OFF_CLOSE;
						//
						else if ((GetLow(Bar) < GetClose(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetClose(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_LOW_BELOW_CLOSE_BOUNCE_OFF_CLOSE;
						//
						else if ((GetLow(Bar) == GetLow(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetOpen(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_LOW_EQ_LOW_CLOSE_BOUNCE_OFF_OPEN;
						//
						else if ((GetLow(Bar) < GetLow(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetOpen(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_LOW_BELOW_LOW_CLOSE_BOUNCE_OFF_OPEN;
						//
						else if ((GetLow(Bar) == GetLow(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetClose(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_LOW_EQ_LOW_CLOSE_BOUNCE_OFF_CLOSE;
						//
						else if ((GetLow(Bar) < GetLow(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetClose(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_LOW_BELOW_LOW_CLOSE_BOUNCE_OFF_CLOSE;
						//
						else if ((GetLow(Bar) == GetLow(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetLow(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_LOW_EQ_LOW_CLOSE_BOUNCE_OFF_LOW;
						//
						else if ((GetLow(Bar) < GetLow(ch.VeBarNum)) && 
							(GetClose(Bar) >= GetLow(ch.VeBarNum))) ch.ChannelEvent = TChannel.VE_LOW_BELOW_LOW_CLOSE_BOUNCE_OFF_LOW;
						else ch.ChannelEvent = TChannel.VE_NONE;
					break;
						
					default:
					break;
					
				}
					
			}
		}
		
		private string Signals2(TChannel ch, string sbartype, string chevent, int Bar)
		{
			string signal = "";
			
			if (((ch.Direction == UP) && (sbartype == "DOUBLE TOP") || (sbartype == "REVERSAL") || (sbartype == "CLOSE_TOP")) && 
				(chevent == "VeReject"))  this.GDrawDown(Bar, (signal = "SHORT : " + (sbartype + " " + chevent)));
			else if (((ch.Direction == DOWN) && (sbartype == "DOUBLE BOTTOM") || (sbartype == "REVERSAL") || (sbartype == "CLOSE_BOTTOM")) && 
				(chevent == "VeBounce"))  this.GDrawUp(Bar, (signal = "LONG : " + (sbartype + " " + chevent)));
			//
			else if ((ch.Direction == UP) && (sbartype == "CLOSE_TOP") && 
				(ch.VeBarNum == (Bar -1)))  this.GDrawDown(Bar, (signal = "SHORT : " + sbartype + " VeUp"));
			else if ((ch.Direction == DOWN) &&(sbartype == "CLOSE_BOTTOM") && 
				(ch.VeBarNum == (Bar -1)))  this.GDrawUp(Bar, (signal = "LONG : " + sbartype + " VeDown"));
			//
			else if ((ch.Direction == UP) && (ch.VeBarNum == (Bar - 1)) && (GetVolumeSeq(Bar-1) == "B+") && 
				(GetVolumeSeq(Bar) == "R1/2-")) this.GDrawDown(Bar, (signal = "SHORT : " + "B+R1/2-" + " Ve"));
			else if ((ch.Direction == DOWN) && (ch.VeBarNum == (Bar - 1)) && (GetVolumeSeq(Bar-1) == "R+") && 
				(GetVolumeSeq(Bar) == "B1/2-")) this.GDrawUp(Bar, (signal = "LONG : " + "R+B1/2-" + " Ve"));
			//
			else if ((ch.VeBarNum == (Bar-1)) && (GetLow(Bar) > GetLow(ch.VeBarNum)) && 
				(GetClose(Bar) > GetClose(ch.VeBarNum)) && 
				(GetHigh(Bar) < GetHigh(ch.VeBarNum))) this.GDrawUp(Bar, (signal = "LONG : " + " Ve  - Low > Ve * Close > Ve * High < Ve "));
			else if ((ch.VeBarNum == (Bar-1)) && (GetHigh(Bar) < GetHigh(ch.VeBarNum)) && 
				(GetClose(Bar) < GetClose(ch.VeBarNum)) && 
				(GetLow(Bar) > GetLow(ch.VeBarNum))) this.GDrawDown(Bar, (signal = "SHORT : " + " Ve  - High > Ve  * Close < Ve * Low > Ve "));
			//
			else if ((ch.Level == 0) && (ch.Direction == DOWN) && (ch.FBOBarNum == Bar) && 
				(GetClose(Bar) <= GetOpen(Bar))) this.GDrawDown(Bar, (signal = "SHORT : FBO!"));
			else if ((ch.Level == 0) && (ch.Direction == UP) && (ch.FBOBarNum == Bar) && 
				(GetClose(Bar) >= GetOpen(Bar))) this.GDrawUp(Bar, (signal = "LONG : FBO!"));
			/*else if ((ch.Direction == UP) && (ch.VeBarNum < ch.ChannelHL) && 
				((sbartype == "DOUBLE TOP") || (sbartype == "REVERSAL") || (sbartype == "CLOSE_TOP")))
				this.GDrawDown(Bar, "SHORT");
			else if ((ch.Direction == DOWN) && (ch.VeBarNum < ch.ChannelHL) && 
				((sbartype == "DOUBLE BOTTOM") || (sbartype == "REVERSAL") || (sbartype == "CLOSE_BOTTOM")))
				this.GDrawUp(Bar, "LONG");*/	
			
			return signal;
		}
		
		private string HLSignal(TChannel ch, int Bar)
		{
			string signal = "";
			
			if (ch.Level > 1) return signal;
			
			if ((ch.Direction == UP) && (ch.ChannelHL != ch.PrevChannelHL) && (ch.ChannelHL == Bar) &&
				(GetHigh(ch.ChannelHL) > GetHigh(ch.PrevChannelHL)) && 
				(GetClose(Bar) < GetHigh(ch.PrevChannelHL)) &&
				((Bar - ch.PrevChannelHL) > 2)) signal = "SHORT";
			else if ((ch.Direction == DOWN) && (ch.ChannelHL != ch.PrevChannelHL) && (ch.ChannelHL == Bar) &&
				(GetLow(ch.ChannelHL) < GetLow(ch.PrevChannelHL)) && 
				(GetClose(Bar) > GetLow(ch.PrevChannelHL)) &&
				((Bar - ch.PrevChannelHL) > 2)) signal = "LONG";
			
			return signal;
			
		}
		
		private string MonitorChannelSignals(TChannel ch, int Bar, double mfi)
		{
			bool go = false;
			
			if ((ch.VeBarNum == Bar) && (ch.Point3 != Bar) && (ch.FBOBarNum != Bar) && (mfi >= 60)) go = true;
			
			if ((ch.Direction == UP) && (go)) 
			{
				m_Signal = SHORT;
				return "SHORT";
			}
			else if ((ch.Direction == DOWN) && (go)) 
			{
				m_Signal = LONG;
				return "LONG";
			}
			else return "";
		}
		
		
		
		private void SetChannelHL(ref TChannel ch)
		{
			int hl = (ch.Direction == UP) ? GetChannelHigh(ch) : GetChannelLow(ch);
			double dhl = (ch.Direction == UP) ? GetHigh(hl) : GetLow(hl);
			
			
			if (ch.PrevChannelHL == 0) 
			{
				ch.ChannelHL = hl;
				ch.PrevChannelHL = hl;
			}
			else if (ch.ChannelHL != hl) 
			{
				if ((ch.RTLX2 == 869) && (ch.Direction == DOWN))
				{
					Output("SetChannelHL Low : Bar "  + hl + " " + GetLow(hl) + " plow : Bar " + ch.ChannelHL + 
						" " + GetLow(ch.ChannelHL));
				}
				
				bool hler = (ch.Direction == UP) ? (GetHigh(hl) != GetHigh(ch.ChannelHL) ? true : false) :
					(GetLow(hl) != GetLow(ch.ChannelHL) ? true : false);
				
				if (hler)
				{
					ch.PrevChannelHL = ch.ChannelHL;
					ch.ChannelHL = hl;
				}
			}
			
		}
		
		#endregion
		
		#region ANALYTICS
		private int GetChannelHighLow(TChannel ch)
		{
			int hl = (ch.Direction == UP) ? GetChannelHigh(ch) : GetChannelLow(ch);
			double dhl = (ch.Direction == UP) ? GetHigh(hl) : GetLow(hl);
			
			return hl;
		}
		
		private string GetMFIString(int Bar)
		{
			double qmfi = QMFI(14).Plot0.Get(Bar);
			string smfi = "";
			
			if ( qmfi < 49) smfi = "40";
			else if ((qmfi > 50) && (qmfi < 55)) smfi = "55";
			else if ((qmfi > 55) && (qmfi < 60)) smfi = "55";
			else if ((qmfi > 60) && (qmfi < 70)) smfi = "60";
			else if ((qmfi > 70) && (qmfi < 80)) smfi = "70";
			else if ((qmfi > 80) && (qmfi < 90)) smfi = "80";
			else if (qmfi > 90) smfi = "90";
			
			return smfi;
		}
		
		private void GetVolSeq(ref TChannel ch, int components, int Bar)
		{
			if (components == 0) ch.ChannelType = "TAPE_TYPE";
			else  ch.ChannelType = "CHANL_TYPE";
			
			if (ch.ChannelType ==  "TAPE_TYPE") 
			{
				string str;
				
				if (ch.RTLX1 == (Bar-1))
				{
					str = GetVolumeSeq(Bar-1);
					str = str + GetVolumeSeq(Bar);
				}
				else str = GetVolumeSeq(Bar);
					
				ch.VolumeSequence =  ch.VolumeSequence + str;
			}
		}
		
		private string GetBarType(int Bar)
		{
			TBar bartype = CreateBarObject(Bar,Bar-1);
			
			string sbartype = "";
			
			if ((GetClose(Bar-1) == GetOpen(Bar)) && (GetClose(Bar) > GetOpen(Bar))) sbartype = "CLOSE_BOTTOM";
			else if ((GetOpen(Bar-1) == GetClose(Bar))  && (GetClose(Bar) < GetOpen(Bar))) sbartype = "CLOSE_TOP";
			else if (bartype.m_BarType == TBar.HIGH_REVERSAL) sbartype = "HIGH_REVERSAL";
			else if (bartype.m_BarType == TBar.LOW_REVERSAL) sbartype = "LOW_REVERSAL";
			else if (GetHigh(Bar-1) == GetHigh(Bar)) sbartype = "DOUBLE TOP";
			else if (GetLow(Bar-1) == GetLow(Bar)) sbartype = "DOUBLE BOTTOM";
			else if (bartype.m_BarType == TBar.INSIDE_BAR) sbartype = "INSIDE_BAR";
			else if (bartype.m_BarType == TBar.OUTSIDE_BAR) sbartype = "OUTSIDE_BAR";
			else sbartype = bartype.GetConstString(bartype.m_BarType);
			
			
			
			return ( bartype.GetConstString(bartype.m_BarType) + "/" + 
					bartype.GetConstString(bartype.m_BodyType) + "/" + bartype.GetConstString(bartype.m_CloseType));
		}
		
		private string GetChannelEvent(TChannel ch, int Bar)
		{
			double low = GetLow(Bar);
			double high = GetHigh(Bar);
			double close = GetClose(Bar);
			double open = GetOpen(Bar);
			
			string chevent = "";
			
			switch(ch.Direction)
			{
				case UP:
					double vehigh = GetHigh(ch.VeBarNum);
					
					if ((vehigh <= high) &&  (close < vehigh) && ( close <= open)) chevent= "VeReject";
				break;
					
				case DOWN:
					double velow = GetLow(ch.VeBarNum);
					
					if ((velow >= low) &&  (close > velow) && ( close >= open)) chevent= "VeBounce";
				break;
					
				default:
					
				break;
				
			}
			
			return chevent;
		}
		
		
		
		#endregion
		
		#region TapeVolumeScan
		
		private void SetVolumeSeq(TChannel line)
		{
			if (line == null) return ;
			
			line.VolumeSequence = "";
			
			for (int i = line.RTLX1; i <= line.RTLX2; i++) line.VolumeSequence += GetVolumeSeq(i);
		}
		
		private string GetVolumeSeqEx(int Bar)
		{
			if (Bar < 3) return "";
			
			if ((TBUpDnVol()[Bar] == RED_COLOR) &&
				(GetVolume(Bar) > GetVolume(Bar-1))) 
			{
				if (GetClose(Bar-1) < GetClose(Bar)) return "R*+";
				else return "R+";
			}
			else if ((TBUpDnVol()[Bar] == RED_COLOR) &&
				(GetVolume(Bar) < GetVolume(Bar-1))) 
			{
				if (GetVolume(Bar) <= (0.5*GetVolume(Bar-1))) return "R1/2-";
				else if (GetClose(Bar-1) < GetClose(Bar)) return "R*-";
				return "R-";
			}
			else if ((TBUpDnVol()[Bar] == BLACK_COLOR) &&
				(GetVolume(Bar) > GetVolume(Bar-1))) 
			{
				if (GetClose(Bar-1) > GetClose(Bar)) return "B*+";
				else return "B+";
			}
			else if ((TBUpDnVol()[Bar] == BLACK_COLOR) &&
				(GetVolume(Bar) < GetVolume(Bar-1))) 
			{
				if (GetVolume(Bar) <= (0.5*GetVolume(Bar-1))) return "B1/2-";
				else if (GetClose(Bar-1) > GetClose(Bar)) return "B*-";
				else return "B-";
			}
			else return "";
			
		}
		
		private string GetVolumeSeq(int Bar)
		{
			string s = "";
			
			if (Bar < 3) return "";
			
			if ((TBUpDnVol()[Bar] == RED_COLOR)) s = "R";
			else if ((TBUpDnVol()[Bar] == BLACK_COLOR)) s = "B";
			else return "";
			
			if (GetVolume(Bar) <= (0.5*GetVolume(Bar-1))) s= s + "1/2-";
			else if ((GetVolume(Bar) > GetVolume(Bar-1))) s = s + "+";
			else if ((GetVolume(Bar) < GetVolume(Bar-1))) s = s + "-";
			
			if ((IsHigherHighHigherLow(Bar,Bar-1)) && (GetClose(Bar) < GetClose(Bar-1))) s = s + "*";
			else if ((IsLowerHighLowerLow(Bar,Bar-1)) && (GetClose(Bar) > GetClose(Bar-1))) s = s + "*";
			else if ((IsFTP(Bar,Bar-1)) && (GetClose(Bar) < GetClose(Bar-1))) s = s + "*";
			else if ((IsFBP(Bar,Bar-1)) && (GetClose(Bar) > GetClose(Bar-1))) s = s + "*";
			
			return s;
			
		}
		
		private bool ChannelContainsRed(TChannel ch)
		{
			if (ch.VolumeSequence.Contains("R*-")) return true;
			else if (ch.VolumeSequence.Contains("R*+")) return true;
			else if ((ch.VolumeSequence.Contains("R+"))) return true;
			else if (ch.VolumeSequence.Contains("R-")) return true;
			else return false;
		}
		
		private bool ChannelContainsBlack(TChannel ch)
		{
			if (ch.VolumeSequence.Contains("B*-")) return true;
			else if (ch.VolumeSequence.Contains("B*+")) return true;
			else if ((ch.VolumeSequence.Contains("B+"))) return true;
			else if (ch.VolumeSequence.Contains("B-")) return true;
			else return false;
		}
		
		private bool ChannelContainsB2B(TChannel ch)
		{
			if (ch.Direction != UP) return false;
			
			if (ch.VolumeSequence.Contains("B-B+")) return true;
			else if (ch.VolumeSequence.Contains("B1/2-B+")) return true;
			else if (ch.VolumeSequence.Contains("B1/2-*B+")) return true;
			else if (ch.VolumeSequence.Contains("B-B*+")) return true;
			else if (ch.VolumeSequence.Contains("B1/2-B*+")) return true;
			else if (ch.VolumeSequence.Contains("R1/2*-B+")) return true;
			else return false;
		}
		
		private bool ChannelContainsR2R(TChannel ch)
		{
			if (ch.Direction != DOWN) return false;
			
			if (ch.VolumeSequence.Contains("R-R+")) return true;
			else if (ch.VolumeSequence.Contains("R1/2-R+")) return true;
			else if (ch.VolumeSequence.Contains("R1/2-*R+*")) return true;
			else if (ch.VolumeSequence.Contains("R-R*+")) return true;
			else if (ch.VolumeSequence.Contains("R1/2-R+")) return true;
			else if (ch.VolumeSequence.Contains("B1/2*-R+")) return true;
			else return false;
		}
		
		private bool ChannelContainsDecRed(TChannel ch)
		{
			if (ch.Direction != DOWN) return false;
			
			if (ch.VolumeSequence.Contains("R-")) return true;
			else if (ch.VolumeSequence.Contains("R*-")) return true;
			else if (ch.VolumeSequence.Contains("R1/2*-")) return true;
			else if (ch.VolumeSequence.Contains("R1/2-")) return true;
			else return false;
		}
		
		private bool ChannelContainsDecBlack(TChannel ch)
		{
			if (ch.Direction != UP) return false;
			
			if (ch.VolumeSequence.Contains("B-")) return true;
			else if (ch.VolumeSequence.Contains("B*-")) return true;
			else if (ch.VolumeSequence.Contains("B1/2*-")) return true;
			else if (ch.VolumeSequence.Contains("B1/2-")) return true;
			else return false;
		}
		
		private bool ChannelContainsIncRed(TChannel ch)
		{
			if (ch.Direction != DOWN) return false;
			
			if (ch.VolumeSequence.Contains("R+")) return true;
			else if (ch.VolumeSequence.Contains("R*+")) return true;
			else return false;
		}
		
		private bool ChannelContainsIncBlack(TChannel ch)
		{
			if (ch.Direction != UP) return false;
			
			if (ch.VolumeSequence.Contains("B+")) return true;
			else if (ch.VolumeSequence.Contains("B*+")) return true;
			else return false;
		}
		
		private void SetChannelState(ref TChannel ch)
		{
			if (ch == null) return;
			
			if (ChannelContainsRed(ch) && (ChannelContainsB2B(ch))) ch.State = TChannel.B_2_B;
			else if (ChannelContainsBlack(ch) && (ChannelContainsR2R(ch))) ch.State = TChannel.R_2_R;
			else if ((ChannelContainsB2B(ch))) ch.State = TChannel.B_2_B;
			else if (ChannelContainsR2R(ch)) ch.State = TChannel.R_2_R;
			else if (ChannelContainsIncRed(ch)) ch.State = TChannel.R_INC;
			else if (ChannelContainsIncBlack(ch)) ch.State = TChannel.B_INC;
			else if (ChannelContainsDecRed(ch)) ch.State = TChannel.R_DEC;
			else if (ChannelContainsDecBlack(ch)) ch.State = TChannel.B_DEC;
			else if ((!ChannelContainsIncRed(ch)) && (ChannelContainsDecRed(ch))) ch.State = TChannel.R_DEC;
			else if ((!ChannelContainsIncBlack(ch)) && (ChannelContainsDecBlack(ch))) ch.State = TChannel.B_DEC;
			
		}
		#endregion
		
		#region JoinTheTriads
		
		private bool ExtendJoinChannel(int Bar, ref TChannel jch)
		{
			bool b = false;
			
			if (jch.Point3 == Bar) return true;
				
			for (int i = jch.Point3; i < Bar; i++)
			{
				TBar cb = CreateBarObject(i+1,i);
				TBar pb = CreateBarObject(i,i-1);
				
				LogError("Extending channel Point 3 : " + i);
				LogError("Extending channel " + jch.RTLTag + " at " + i + " from "  + pb.m_BarNum  + " to " + cb.m_BarNum);
				
				
				jch.Extend(cb,pb,IsInsideBar(cb.m_BarNum,pb.m_BarNum));
				
				if (jch.FBO) 
				{
					LogError("Join channel : " +  jch.RTLTag + " FBO during extension at " + jch.FBOBarNum);
					
					b = false;
					
					break;
				}
				else if (jch.Broken) 
				{
					LogError("Join Channel : Extension of channel " + jch.RTLTag + 
							" failed because channel is broken.");
					
					b= false;
					
					break;
				}
				else 
				{
					LogError("Join channel : " + jch.RTLTag + " successfully extended. ");
					b = true;
				}
			}
			
			return b;
		}
		
		private TChannel CreateJoinChannels(int Bar, int pt1x, double pt1y, 
										int pt3, int direction, int level, bool pennant)
		{
			TChannel jch = null;
			bool b = false;
			
			LogError("<<<CreateJoinChannels>>> " + " pt1 " + pt1x + ","  + pt1y + " Bar " + Bar);
			
			for (int i = pt3; i <= Bar; i++)
			{
				LogError(Bar + " ^^^Attempting to create a new join channel^^^  - pt1 : " +  pt1x + ","  + pt1y + " pt3 : " + i); 
				
				
				jch = (pennant) ? CreateTape(pt1x, pt1y, i, direction, level, false, false)
									: CreateTape(pt1x, i, direction, level, false, false);
				
				if (jch == null) 
				{
					LogError(Bar + " Join: Failed to join channel for " +  pt1x + ","  + pt1y
					+ "," + i + "," + " direction : " + direction +
						" level : " + level);
					
					
					continue;
				}
				
				jch.Pennant = pennant;
				
				if (jch.Broken)
				{
					LogError(Bar + " Join: Failed to join Channel BROKEN " +  pt1x + ","  + pt1y + 
					"," + i + "," + " direction : " + direction +
						" level : " + level);
					
					jch = null;
					
					continue;
				}
				
				if (ExtendJoinChannel(Bar, ref jch)) 
				{
					LogError(Bar +  " Join: Created and extended a channel for " +  pt1x + ","  + pt1y + "," + i + "," + " direction : " + direction +
						" level : " + level);
					break;
					
				}	
				else
				{
					LogError(Bar + " Join : unable to extend channel " + jch.RTLTag + " at " + jch.FBOBarNum);
					jch = null;
					continue;
				}
				
				if (CheckLineIntegrity(jch.RTL))
				{
					LogError(Bar + " Join: Line integrity check for channel " + jch.RTLTag + " passed.");
					break;
				}
				else 
				{
					jch = null;
					
					LogError(Bar +  " Line integrity check for channel " + jch.RTLTag + " failed!");
				}
			}
			
			return jch;
		}
		
		private void JoinOperation()
		{
			for (int i = 0; i < m_Channels.Count; i++)
			{
				TChannel ch = m_Channels[i];
				
				if ((!ch.Joined) && (!ch.Broken))
				{
					if (JoinChannel(ch) != null)
					{
						ch.Joined = true;
						
						m_Channels[i] = ch;
					}
				}
			}
		}
		
		private TChannel GetLongestJoiningChannel(TChannel ch)
		{
			int length = 0;
			TChannel pch = null;
			
			foreach(TChannel tch in m_Channels)
			{
				if (((tch.RTLX1 <= ch.RTLX1) && (tch.RTLX2 >= ch.RTLX1)) && 
					(tch.Length >= length) && (tch.Direction != ch.Direction) && (tch.Broken))  
				{
					pch = tch;
					length = tch.Length;
					
				}
			}
			
			return pch;
		}
		
		private TChannel GetAJoinChannel(TChannel ch3)
		{
			TChannel jch = null;
			
			if (ch3.Joined == true) return jch;
			
			TChannel ch2 = GetParentChannel(ch3);
			
			if (ch2 == null) return jch;
			
			TChannel ch1 = GetParentChannel(ch2);
			
			if (ch1 == null) return jch;
			
			if ((ch3.Direction == DOWN) && (ch2.Direction == UP) && (ch1.Direction == DOWN))
			{
				int low3 = GetChannelLow(ch3);
				
				int high2 = GetChannelHigh(ch2);
				
				int low1 = GetChannelLow(ch1);
				
				int high1 = GetChannelHigh(ch1);
				
				double diff = Math.Round(GetLow(low3) - GetLow(low1), 4);
				
				//if ((GetLow(low3) <= GetLow(low1)) && (GetHigh(high2) < GetHigh(high1)))
				if ((diff <= m_TickMultiplierHighLowDifference*TickSize) && (GetHigh(high2) < GetHigh(high1)))
				{
					
					jch = (ch1.Pennant) ? CreateJoinChannels(ch3.RTLX2, ch1.RTLX1, ch1.RTLY1, high2, DOWN, ch3.Level, true): 
								CreateJoinChannels(ch3.RTLX2, high1, 0, high2, DOWN, ch3.Level, false);
					
					
				}
			}
			else if ((ch3.Direction == UP) && (ch2.Direction == DOWN) && (ch1.Direction == UP))
			{
				int high3 = GetChannelHigh(ch3);
				
				int low2 = GetChannelLow(ch2);
				
				int high1 = GetChannelHigh(ch1);
				
				int low1 = GetChannelLow(ch1);
				
				if (GetLow(ch1.RTLX1) <= GetLow(low1)) low1 = ch1.RTLX1;
				
				double diff = Math.Round(GetHigh(high1) - GetHigh(high3), 4);
				
				//if ((GetHigh(high3) >= GetHigh(high1)) && (GetLow(low2) > GetLow(low1)))
				if ((diff <= m_TickMultiplierHighLowDifference*TickSize) && (GetLow(low2) > GetLow(low1)))
				{
					jch = (ch1.Pennant) ? CreateJoinChannels(ch3.RTLX2, ch1.RTLX1, ch1.RTLY1, low2, UP, ch3.Level, true) :
							CreateJoinChannels(ch3.RTLX2, low1, 0, low2, UP, ch3.Level, false);	
				}
				
			}
			
			if (jch != null)
			{
				LogError(CurrentBar + " Join : creating a channel from " + ch1.RTLX1 + " pt3 " + ch2.RTLX2);
				
				jch.JoinedBar = ch3.RTLX2;	
			}
			else LogError(CurrentBar +  " Join : Unable to join channels " + ch1.RTLX1 + " pt 3" + ch2.RTLX2);	
			
			return jch;
			
		}
		
		private void ProcessJoinedChannel(TChannel jch, int rtlx2)
		{
			if (jch != null)
			{
				AddChannelToListAndDraw(jch);
				
				Output(CurrentBar + " ***Join : " + jch.RTLTag + " level " + jch.Level + " created as a result of a join operation.***");
				
				DownGradeChannels(jch);
				
				//@@Change from V5
				//FlagUnneededJoinedChannel(jch, rtlx2);
			}
			
		}
		
		private TChannel JoinChannel(TChannel ch3)
		{
			
			TChannel jch = GetAJoinChannel(ch3);
			
			ProcessJoinedChannel(jch, ch3.RTLX2);
			
			return jch;
		}
		
		#endregion
		
		private void RemoveStitchChannel(int Bar)
		{
			TChannel tape = GetTape(Bar);
			
			if (m_CurrentTape.Pennant)
			{
				if ((m_CurrentTape.RTLX2 == Bar-1) && (m_CurrentTape.Length == 2) && (tape.Direction == m_CurrentTape.Direction))
				{
					EraseChannel(m_CurrentTape);
					
					m_CurrentTape = null;
				}
			}
		}
		
		private void FSM(int Bar)
		{
			TChannel tape = GetTape(Bar);
			
			if (tape == null) return;
				
			if (m_TapeState != tape.Direction)
			{
				m_TapeState = tape.Direction;
				
				m_CurrentTape = tape;
				
				AddChannelToListAndDraw(tape);
			}
			
			
		}
        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose	= true;
            Overlay				= true;
            PriceTypeSupported	= false;
			
			DrawOnPricePanel = true;
			
			BarsRequired = 2;
        }
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
		/// 
		
		private void RunTape(int bar)
		{
			m_CurBar = CreateBarObject(bar,bar-1);
			
			Output("//////Find 2 Bar Tape//////");
			FSM(bar);
			Output("//////Find 2 Bar Tape//////");
			
			Output("/////Create 2 Bar Inside or Outside bar//////");
			Find2BarOutSideOrInsideBar(bar);
			Output("/////Create 2 Bar Inside or Outside bar//////");
			
			Output("////////Extend channels/////////");
			ExtendChannelsEx(m_CurBar,m_PrevBar);	
			Output("////////Extend channels/////////");
			
			Output("///////Remove 2 Bar inside channels///////");
			Remove2BarInsideChannel();
			Output("///////Remove 2 Bar inside channels///////");
			
			Output("///////Broken channels///////");
			BrokenChannels();
			Output("///////Broken channels///////");
				
			Output("///////Upgrade active lower channels///////");
			HandleChannelBreaks(bar);
			Output("///////Upgrade active lower channels///////");
			
			Output("/////Assign levels to these channels//////");
			AssignChannelLevels();
			Output("/////Assign levels to these channels//////");
			
			JoinOperation();
			
			Output("/////Assign levels to these channels//////");
			AssignChannelLevels();
			Output("/////Assign levels to these channels//////");
			
			
			XYZ(bar);
			
			/*PrintStats(bar);
			
			OutputTable("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
			
			OutputTable("");
			
			PrintBarInfoHeader();
			PrintBarInfo(bar);
			
			OutputTable("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");*/
			
		}
		
		
		private void TestPrint(int Bar)
		{
			Print("*****" + CurrentBar + "*****");	
			
			List<TChannel> active = new List<TChannel>();
			List<TChannel> broken = new List<TChannel>();
			
			foreach(TChannel ch in m_Channels)
			{
				if (ch.RTLX2 != Bar) continue;
				
				if (ch.Broken) broken.Add(ch);
				else active.Add(ch);
				
				
			}
			
			foreach(TChannel ch in broken)
			{
				Print(ch.RTLTag + "\t\t" + ch.Label + "\t\t" + ch.Broken + "\t\t" + ch.SDirection + "\t\t" + ch.Level);		
			}
			
			Print("---------------------------------------------------------------------------------------------------------");
			foreach(TChannel ch in active)
			{
				Print(ch.RTLTag + "\t\t" + ch.Label + "\t\t" + ch.Broken + "\t\t" + ch.SDirection + "\t\t" + ch.Level);		
			}
		}
		protected override void OnBarUpdate()
        {
            // Use this method for calculating your indicator values. Assign a value to each
            // plot below by replacing 'Close[0]' with your own formula.
			
			m_CurrentBar = CurrentBar;
			
			this.G_CurrentBar = CurrentBar;
			
			if (m_EndBar == 0)
			{
				if (CurrentBar < m_StartBar) return;
			}
			else if ((CurrentBar < m_StartBar) || (CurrentBar > m_EndBar)) return;
			
			try
			{
				LogError("====================");
				LogError("Bar : " + CurrentBar);
				LogError("====================");
				
				RunTape(m_CurrentBar);
				
				//TestPrint(m_CurrentBar);
			}
			catch (System.Exception e)
			{
				Print(CurrentBar + " : " + e.ToString());
				
			}
        }

        #region Properties
		[Description("Start drawing trendlines from this bar")]
        [Category("Start/Stop Bars")]
        public int StartBar
        {
            get { return m_StartBar; }
            set { m_StartBar = Math.Max(1, value); }
        }
		
		//@@change
		[Description("Stop drawing trendlines at this bar(inactive)")]
        [Category("Start/Stop Bars")]
        public int StopBar
        {
            get { return m_EndBar; }
            set { m_EndBar = Math.Max(0, value); }
        }
		
		[Description("Information")]
        [Category("Messages")]
        public bool InfoMessages
        {
            get { return m_LogOutput; }
            set { m_LogOutput = value; }
        }
		
		[Description("Error Messages")]
        [Category("Messages")]
        public bool ErrorMessages
        {
            get { return m_LogError; }
            set { m_LogError = value; }
        }
		
		[Description("Tables")]
        [Category("Messages")]
        public bool InfoTables
        {
            get { return m_LogOutputTable; }
            set { m_LogOutputTable = value; }
        }
		
		[Description("Join channels at number of ticks away from High/Low.")]
        [Category("Parameters")]
        public int TickMultiplier
        {
            get { return m_TickMultiplierHighLowDifference; }
            set { m_TickMultiplierHighLowDifference = Math.Max(0, value); }
        }
		
		[Description("Join channels at number of ticks away from High/Low.")]
        [Category("Parameters")]
        public int HighestLevelChannelToDraw
        {
            get { return m_HighestLevelChannelToDraw; }
            set { m_HighestLevelChannelToDraw = Math.Max(0, value); }
        }
		
		
        #endregion
    }
}

//@@


#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private TChannelV6[] cacheTChannelV6 = null;

        private static TChannelV6 checkTChannelV6 = new TChannelV6();

        /// <summary>
        /// New Channel
        /// </summary>
        /// <returns></returns>
        public TChannelV6 TChannelV6(int highestLevelChannelToDraw, int tickMultiplier)
        {
            return TChannelV6(Input, highestLevelChannelToDraw, tickMultiplier);
        }

        /// <summary>
        /// New Channel
        /// </summary>
        /// <returns></returns>
        public TChannelV6 TChannelV6(Data.IDataSeries input, int highestLevelChannelToDraw, int tickMultiplier)
        {
            if (cacheTChannelV6 != null)
                for (int idx = 0; idx < cacheTChannelV6.Length; idx++)
                    if (cacheTChannelV6[idx].HighestLevelChannelToDraw == highestLevelChannelToDraw && cacheTChannelV6[idx].TickMultiplier == tickMultiplier && cacheTChannelV6[idx].EqualsInput(input))
                        return cacheTChannelV6[idx];

            lock (checkTChannelV6)
            {
                checkTChannelV6.HighestLevelChannelToDraw = highestLevelChannelToDraw;
                highestLevelChannelToDraw = checkTChannelV6.HighestLevelChannelToDraw;
                checkTChannelV6.TickMultiplier = tickMultiplier;
                tickMultiplier = checkTChannelV6.TickMultiplier;

                if (cacheTChannelV6 != null)
                    for (int idx = 0; idx < cacheTChannelV6.Length; idx++)
                        if (cacheTChannelV6[idx].HighestLevelChannelToDraw == highestLevelChannelToDraw && cacheTChannelV6[idx].TickMultiplier == tickMultiplier && cacheTChannelV6[idx].EqualsInput(input))
                            return cacheTChannelV6[idx];

                TChannelV6 indicator = new TChannelV6();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.HighestLevelChannelToDraw = highestLevelChannelToDraw;
                indicator.TickMultiplier = tickMultiplier;
                Indicators.Add(indicator);
                indicator.SetUp();

                TChannelV6[] tmp = new TChannelV6[cacheTChannelV6 == null ? 1 : cacheTChannelV6.Length + 1];
                if (cacheTChannelV6 != null)
                    cacheTChannelV6.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheTChannelV6 = tmp;
                return indicator;
            }
        }
    }
}

// This namespace holds all market analyzer column definitions and is required. Do not change it.
namespace NinjaTrader.MarketAnalyzer
{
    public partial class Column : ColumnBase
    {
        /// <summary>
        /// New Channel
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.TChannelV6 TChannelV6(int highestLevelChannelToDraw, int tickMultiplier)
        {
            return _indicator.TChannelV6(Input, highestLevelChannelToDraw, tickMultiplier);
        }

        /// <summary>
        /// New Channel
        /// </summary>
        /// <returns></returns>
        public Indicator.TChannelV6 TChannelV6(Data.IDataSeries input, int highestLevelChannelToDraw, int tickMultiplier)
        {
            return _indicator.TChannelV6(input, highestLevelChannelToDraw, tickMultiplier);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// New Channel
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.TChannelV6 TChannelV6(int highestLevelChannelToDraw, int tickMultiplier)
        {
            return _indicator.TChannelV6(Input, highestLevelChannelToDraw, tickMultiplier);
        }

        /// <summary>
        /// New Channel
        /// </summary>
        /// <returns></returns>
        public Indicator.TChannelV6 TChannelV6(Data.IDataSeries input, int highestLevelChannelToDraw, int tickMultiplier)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.TChannelV6(input, highestLevelChannelToDraw, tickMultiplier);
        }
    }
}
#endregion
