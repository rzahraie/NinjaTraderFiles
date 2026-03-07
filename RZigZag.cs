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
	public class RZigZag : Indicator
	{
		#region CONSTS
		const int UP = 10;
		const int DOWN = 11;
		const int HOLD = 12;
		const int OUTSIDE_UP = 13;
		const int OUTSIDE_DOWN = 14;
		
		public const int NO_STATE = -1;
		public const int BROKEN_ABOVE = 0;
		public const int BROKEN_BELOW = 1;
		public const int CLOSED_ABOVE_SUPPORT = 2;
		public const int CLOSED_BELOW_RESISTANCE = 3;
		public const int INTACT = 4;
		
		public const int LAT = 5;
		public const int NO_LAT = 6;
		
		public const int DOWN_TAPE = 100;
		public const int UP_TAPE = 101;
		public const int DOWN_TRAVERSE = 102;
		public const int UP_TRAVERSE = 103;
		#endregion
		
		#region LogVariables
		bool m_LogError = false;
		bool m_LogOutput = true;
		bool m_LogOutputTable = true;
		bool m_LogInfoTable = true;
		bool m_LogStats = false;
		bool m_LogVolumeCycle = false;
		bool m_LogVolumeHeaderFirst = false;
		bool m_ReWorkMajorJoinsLog = false;
		bool m_ReArrangePrint = false;
		bool m_PrintPriceVolumeCycle = false;
		#endregion
		
		#region LogOutPutFunctions
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
		
		#region BarVariables
		int m_StartBar;
		int m_StopBar = 0;
		int m_PrevBar = 0;
		public int m_CurrentBar;
		#endregion
		
		#region StateVariables
		private int m_SuperState = NO_STATE;
		private int m_SubState = NO_STATE;
		private int m_PrevSubState = NO_STATE;
		#endregion
		
		#region LevelVariables
		int m_Level = -1;
		int m_HighestLevelToDraw = 1;
		bool m_Draw = true;
        #endregion
		
		#region LineVariables
		private xArrowLine m_TapeLine;
		private xArrowLine m_PrevLine = null;
		private int m_LineCount = 0;
		System.Collections.Generic.List<xArrowLine> m_ArrowLines = 
						new System.Collections.Generic.List<xArrowLine>();
		private double m_PrevPeakVolume;
		private double m_PrevLinePeakVolume;
		private int m_PrevDirection;
		private double m_PrevStartpoint;
		private double m_PrevPrevStartpoint;
		private double m_StarPoint;
		
		private string m_MajorTrend = "XX";
		private string m_PrevMajorTrend = "XX";
		private string m_Cycle = "COMPLETE";
		private string m_Phase = "DOMINANT";
		
		private int m_ReRunJoinNum = 0;
		private bool m_SpecialJoin = false;
		#endregion

		#region LineCreation
		
		private int GetLevel(int x1, int x2)
		{
			xArrowLine ln = GetLowesttLevelLine(x1);
			
			if (ln == null) m_Level = 0;
			else m_Level = ln.m_Level;
			
			return m_Level;
		}
		
		private bool IsLineDuplicate(xArrowLine line)
		{
			foreach(xArrowLine ln in m_ArrowLines)
			{
				if (ln.m_Tag == line.m_Tag) return true;
			}
			
			return false;
		}
		
		private void AddLine(xArrowLine line)
		{
			if (!IsLineDuplicate(line)) m_ArrowLines.Add(line);
			
		}
		
		private xArrowLine CreateLine(int x1, double y1, int x2, double y2, int direction, int level)
		{
			
			xArrowLine line = new xArrowLine(x1, y1, x2, y2, direction, level,m_LineCount++);
			
			return line;
		}
		
		private void CreateNewArrowTape(int x1, int x2, int direction)
		{
			try
			{
			double y1 = (direction == UP) ? Low[CurrentBar- x1] : High[CurrentBar- x1];
			double y2 = (direction == UP) ? High[CurrentBar - x2] : Low[CurrentBar - x2];
			
			if (m_TapeLine != null)
			{
				//SetVolumeSeq(m_TapeLine);
				//SetChannelState(ref m_TapeLine);
				//SetVolumeSeqSymbol(m_TapeLine);
				
				//m_TapeLine.m_PeakVolume = GetMaxVolume(m_TapeLine);
				
				m_TapeLine.m_Ended = true;
			
				m_TapeLine.m_Broken = true;
				
				m_PrevLine = m_TapeLine;
			}
			
			xArrowLine line = CreateLine(x1, y1, x2, y2, direction, GetLevel(x1, x2));
			
			if (line != null)
			{
				//SetVolumeSeq(line);
				//SetChannelState(ref line);
				//SetVolumeSeqSymbol(line);
			
				//m_TapeLine.m_PeakVolume = GetMaxVolume(line);
					
				m_TapeLine = line;
				
				line.Type = xArrowLine.TAPE;
				
				DrawLine(m_TapeLine);	
			}
			
			if (x1 == x2)
			{
				//if (m_TapeLine.m_Direction == UP) m_TapeLine.VolumeSequence = GetVolumeSeqOutUp(x2);
				//else m_TapeLine.VolumeSequence = GetVolumeSeqOutDn(x2);
				
				//SetChannelState(ref m_TapeLine);
			}
			
			if (m_PrevLine != null) AddLine(m_PrevLine);
			
			m_PrevLine = null;
			}
			catch(System.Exception e)
			{
				Print("CreateNewArrowTape : " + e.ToString());
			}
			
		}
		#endregion
		
		#region LineExtension
		private void ExtendAllNonTapeLines()
		{
			for (int i = 0; i < m_ArrowLines.Count; i++)
			{
				xArrowLine line = m_ArrowLines[i];
				
				if (line.m_Tag == m_TapeLine.m_Tag) continue;
				
				if (!line.m_Ended)
				{
					bool lh = false;
					
					if (m_TapeLine.m_Direction == UP)
					{
						if (m_TapeLine.m_ExtendedPoint.Y >= line.m_ExtendedPoint.Y) lh = true;
					}
					else
					{
						if (m_TapeLine.m_ExtendedPoint.Y <= line.m_ExtendedPoint.Y) lh = true;
					}
					
					if (line.m_Direction == m_TapeLine.m_Direction)
					{
						if (lh) 
						{
							line.UpdateEndPoint(m_TapeLine.m_ExtendedPoint.X, m_TapeLine.m_ExtendedPoint.Y);
							DrawLine(line);
						}
					}
					else
					{
						line.m_Ended = true;
					}
					
					if (line.Left1.m_PeakVolume > line.Left2.m_PeakVolume) line.m_PeakVolume = line.Left1.m_PeakVolume;
					else line.m_PeakVolume = line.Left2.m_PeakVolume;
				}
			}
		}
		
		private void BreakLines(int Bar, xArrowLine currentline)
		{
			xArrowLine farrow = null;
			int length = 0;
			bool broken = false;
			
			for(int i = 0; i < m_ArrowLines.Count; i++)
			{		
				xArrowLine ln = m_ArrowLines[i];
				
				if (ln.m_Broken) continue;
				
				if ((ln.m_ExtendedPoint.X == currentline.m_BeginPoint.X) && 
					(ln.m_Direction != currentline.m_Direction))
				{
					if (currentline.m_Direction == UP)
					{
						if (currentline.m_EndPoint.Y >= ln.m_BeginPoint.Y) 	m_ArrowLines[i].m_Broken = true;
						
						broken = true;
					}
					else
					{
						if (currentline.m_EndPoint.Y <= ln.m_BeginPoint.Y) 	m_ArrowLines[i].m_Broken = true;
						
						broken = true;
					}
				}
			}
			
			if (broken)
			{
				
			}
				
		}
		
		private void UpDowngradeLines(int Bar, xArrowLine line, int upordown)
		{
			for(int i = 0; i < m_ArrowLines.Count; i++)
			{
				xArrowLine ln = m_ArrowLines[i];
				
				if (ln.m_Tag == line.m_Tag) continue;
				
				if ((line.m_BeginPoint.X <= ln.m_BeginPoint.X) && 
								(line.m_EndPoint.X >= ln.m_EndPoint.X))
				{
					int newlevel = ln.m_Level + upordown;
					
					if (newlevel < 0) newlevel = 0;
					
					m_ArrowLines[i].SetLineLevel(newlevel);
					
					DrawLine(m_ArrowLines[i]);
				}
			}
			
			if ((line.m_BeginPoint.X <= m_TapeLine.m_BeginPoint.X) && 
								(line.m_EndPoint.X >= m_TapeLine.m_EndPoint.X))
			{
					m_TapeLine.SetLineLevel(m_TapeLine.m_Level + upordown);
					
					DrawLine(m_TapeLine);
			}
			
		}
		
		private void ExtendArrowTape(int Bar)
		{
			int x = Bar;
			
			double y = (m_TapeLine.m_Direction == UP) ? High[CurrentBar - Bar] : Low[CurrentBar - Bar];
			
			m_TapeLine.UpdateEndPoint(x, y);
				
			DrawLine(m_TapeLine);
			
			//SetVolumeSeq(m_TapeLine);
			
			//SetChannelState(ref m_TapeLine);
			
			//SetVolumeSeqSymbol(m_TapeLine);
			
			//m_TapeLine.m_PeakVolume = GetMaxVolume(m_TapeLine);
			
		}
		#endregion
		
		#region LineRemoval
		private bool  RemoveArrowLine(xArrowLine line)
		{
			
			RemoveDrawObject(line.m_Tag);
			
			bool b = m_ArrowLines.Remove(line);
			
			return b;
		}
		#endregion
		
        public int GetHighestHigh(int BarStart, int BarsBack)
        {
            int start = BarStart - BarsBack;
            int end = BarStart;
            int ihigh = 0;
            double high = High[CurrentBar - start];
            ihigh = start;

            for (int i = start; i <= end; i++)
            {
                double hh = High[CurrentBar - i];


                if (high <= hh)
                {
                    high = hh;
                    ihigh = i;
                }
            }

            return ihigh;
        }

        public int GetLowestLow(int BarStart, int BarsBack)
        {
            int start = BarStart - BarsBack;
            int end = BarStart;
            int ilow = 0;
            double low = Low[CurrentBar - ilow];
            ilow = start;

            for (int i = start; i <= end; i++)
            {
                double ll = Low[CurrentBar - i];

                if (low >= ll)
                {
                    low = ll;
                    ilow = i;
                }
            }

            return ilow;
        }

        private int GetChannelHigh(xArrowLine line)
        {
            int hh = GetHighestHigh(line.m_EndPoint.X, (line.m_EndPoint.X - line.m_BeginPoint.X));

            return hh;
        }

        private int GetChannelLow(xArrowLine line)
        {
            int ll = GetLowestLow(line.m_EndPoint.X, (line.m_EndPoint.X - line.m_BeginPoint.X));

            return ll;
        }

        #region DrawFuncs

        private void DrawLine(xArrowLine line)
        {
            if (!m_Draw) return;

            RemoveDrawObject(line.m_Tag);

            if (m_HighestLevelToDraw < line.m_Level) return;

            Draw.ArrowLine(this, line.m_Tag, CurrentBar - line.m_BeginPoint.X, line.m_BeginPoint.Y,
                    CurrentBar - line.m_ExtendedPoint.X, line.m_ExtendedPoint.Y, line.m_LineColor,
            line.m_LineStyle, line.m_LineWidth);
        }

        #endregion

		#region LineSearch
		
		private int GetHighestLevelLine(xArrowLine line)
		{
			int level = -1;
			
			foreach(xArrowLine ln in m_ArrowLines)
			{
				if ((ln.m_BeginPoint.X >= line.m_BeginPoint.X) && (ln.m_ExtendedPoint.X <= line.m_ExtendedPoint.X)) 
				{
				 if (level < ln.m_Level) level = ln.m_Level;	
				}
			}
			
			return level;
		}
		
		private xArrowLine GetHighestLevelLine(int Bar)
		{
			xArrowLine line = null;
			int level = -1;
			
			foreach(xArrowLine ln in m_ArrowLines)
			{
				if ((ln.m_ExtendedPoint.X == Bar) && (ln.m_Level > level))
				{
					level = ln.m_Level;
					line = ln;
				}
			}
			
			if ((m_TapeLine != null) && (m_TapeLine.m_ExtendedPoint.X == Bar) && (m_TapeLine.m_Level > level)) 
			{
				level = m_TapeLine.m_Level;
				line = m_TapeLine;
			}
			
			return line;
			
		}
		
		private xArrowLine GetLowesttLevelLine(int Bar)
		{
			xArrowLine line = null;
			int level = 100;
			
			foreach(xArrowLine ln in m_ArrowLines)
			{
				if ((ln.m_ExtendedPoint.X == Bar) && (ln.m_Level < level))
				{
					level = ln.m_Level;
					line = ln;
				}
			}
			
			if ((line == null) && (m_TapeLine != null) && (m_TapeLine.m_ExtendedPoint.X == Bar) && (m_TapeLine.m_Level > level)) 
			{
				level = m_TapeLine.m_Level;
				line = m_TapeLine;
			}
			
			return line;
			
		}
	
		private xArrowLine FindPreviousConnectingArrow(xArrowLine line)
		{
			xArrowLine farrow = null;
			int length = 0;
			
			foreach(xArrowLine ln in m_ArrowLines)
			{				
				if ((ln.m_ExtendedPoint.X == line.m_BeginPoint.X) && ( ln.m_Direction != line.m_Direction))
				{
					if (ln.Length >= length) 
					{
						farrow = ln;
						
						length = ln.Length;
					}
				}
			}
			
			return farrow;
			
		}
		
		private xArrowLine FindPreviousConnectingArrowL(xArrowLine line)
		{
			xArrowLine farrow = null;
			int length = 100;
			
			foreach(xArrowLine ln in m_ArrowLines)
			{				
				if ((ln.m_ExtendedPoint.X == line.m_BeginPoint.X) && ( ln.m_Direction != line.m_Direction))
				{
					if (ln.Length <= length) 
					{
						farrow = ln;
						
						length = ln.Length;
					}
				}
			}
			
			return farrow;
			
		}
		
		private List<xArrowLine> FindAllConnectingArrows(xArrowLine line)
		{
			List<xArrowLine> lines = new List<xArrowLine>();
	
			foreach(xArrowLine ln in m_ArrowLines)
			{				
				if ((ln.m_ExtendedPoint.X == line.m_BeginPoint.X) && ( ln.m_Direction != line.m_Direction))
				{
						lines.Add(ln);
				}
			}
			
			lines.Sort(new ILengthComparerDesc());
			
			return lines;
			
		}
		
		private xArrowLine FindChannelAtBar(int Bar)
		{
			foreach(xArrowLine ln in m_ArrowLines)
			{
				if ((ln.m_ExtendedPoint.X == Bar) && (ln.m_Composite) && (ln.m_Level == 0)) return ln;
			}
			
			return null;
			
		}
		
		private xArrowLine FindCompositeLineStartingAtBar(xArrowLine ch)
		{
			foreach(xArrowLine ln in m_ArrowLines)
			{
				if (ln.m_Tag == ch.m_Tag) continue;
				
				if ((ln.m_BeginPoint.X == ch.m_BeginPoint.X) && (ln.m_Composite)) return ln;
			}
			
			return null;
			
		}
		
		private List<xArrowLine> FindAllxArrowLines(int Bar)
		{
			List<xArrowLine> lines = new List<xArrowLine>();
			
			foreach(xArrowLine line in m_ArrowLines)
			{
				if (line.m_ExtendedPoint.X == Bar) lines.Add(line);
			}
			
			return lines;
			
		}
		
		#endregion
        


        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Price-Volume Arrow display - subs for channels";
				Name										= "RZigZag";
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
				StartBar					= 1;
				StopBar					= 1;
				InfoTables					= true;
				Statistics					= false;
				VolumeCycle					= false;
				PrintPriceVolumeCycle					= false;
				HighestLevelToDraw					= 1;
				HighestDraw					= false;
				ReWorkJoins					= true;
				ReWorkMajorJoinsLog					= true;
				SignalDUVolume					= false;
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StartBar", Description="Start drawing trendlines from this bar", Order=1, GroupName="Parameters")]
		public int StartBar
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StopBar", Description="Stop drawing trendlines at this bar", Order=2, GroupName="Parameters")]
		public int StopBar
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="InfoTables", Description="Messages", Order=3, GroupName="Parameters")]
		public bool InfoTables
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Statistics", Description="Statistics", Order=4, GroupName="Parameters")]
		public bool Statistics
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="VolumeCycle", Description="Volume Cycle", Order=5, GroupName="Parameters")]
		public bool VolumeCycle
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="PrintPriceVolumeCycle", Description="Price/Volume Cycle", Order=6, GroupName="Parameters")]
		public bool PrintPriceVolumeCycle
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="HighestLevelToDraw", Description="Highest level to draw", Order=7, GroupName="Parameters")]
		public int HighestLevelToDraw
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="HighestDraw", Description="Highest level to draw", Order=8, GroupName="Parameters")]
		public bool HighestDraw
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="ReWorkJoins", Description="Re-Arrange Joins", Order=9, GroupName="Parameters")]
		public bool ReWorkJoins
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="ReWorkMajorJoinsLog", Description="Re-Arrange Joins Log", Order=10, GroupName="Parameters")]
		public bool ReWorkMajorJoinsLog
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="SignalDUVolume", Description="DU Volume", Order=11, GroupName="Parameters")]
		public bool SignalDUVolume
		{ get; set; }
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RZigZag[] cacheRZigZag;
		public RZigZag RZigZag(int startBar, int stopBar, bool infoTables, bool statistics, bool volumeCycle, bool printPriceVolumeCycle, int highestLevelToDraw, bool highestDraw, bool reWorkJoins, bool reWorkMajorJoinsLog, bool signalDUVolume)
		{
			return RZigZag(Input, startBar, stopBar, infoTables, statistics, volumeCycle, printPriceVolumeCycle, highestLevelToDraw, highestDraw, reWorkJoins, reWorkMajorJoinsLog, signalDUVolume);
		}

		public RZigZag RZigZag(ISeries<double> input, int startBar, int stopBar, bool infoTables, bool statistics, bool volumeCycle, bool printPriceVolumeCycle, int highestLevelToDraw, bool highestDraw, bool reWorkJoins, bool reWorkMajorJoinsLog, bool signalDUVolume)
		{
			if (cacheRZigZag != null)
				for (int idx = 0; idx < cacheRZigZag.Length; idx++)
					if (cacheRZigZag[idx] != null && cacheRZigZag[idx].StartBar == startBar && cacheRZigZag[idx].StopBar == stopBar && cacheRZigZag[idx].InfoTables == infoTables && cacheRZigZag[idx].Statistics == statistics && cacheRZigZag[idx].VolumeCycle == volumeCycle && cacheRZigZag[idx].PrintPriceVolumeCycle == printPriceVolumeCycle && cacheRZigZag[idx].HighestLevelToDraw == highestLevelToDraw && cacheRZigZag[idx].HighestDraw == highestDraw && cacheRZigZag[idx].ReWorkJoins == reWorkJoins && cacheRZigZag[idx].ReWorkMajorJoinsLog == reWorkMajorJoinsLog && cacheRZigZag[idx].SignalDUVolume == signalDUVolume && cacheRZigZag[idx].EqualsInput(input))
						return cacheRZigZag[idx];
			return CacheIndicator<RZigZag>(new RZigZag(){ StartBar = startBar, StopBar = stopBar, InfoTables = infoTables, Statistics = statistics, VolumeCycle = volumeCycle, PrintPriceVolumeCycle = printPriceVolumeCycle, HighestLevelToDraw = highestLevelToDraw, HighestDraw = highestDraw, ReWorkJoins = reWorkJoins, ReWorkMajorJoinsLog = reWorkMajorJoinsLog, SignalDUVolume = signalDUVolume }, input, ref cacheRZigZag);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RZigZag RZigZag(int startBar, int stopBar, bool infoTables, bool statistics, bool volumeCycle, bool printPriceVolumeCycle, int highestLevelToDraw, bool highestDraw, bool reWorkJoins, bool reWorkMajorJoinsLog, bool signalDUVolume)
		{
			return indicator.RZigZag(Input, startBar, stopBar, infoTables, statistics, volumeCycle, printPriceVolumeCycle, highestLevelToDraw, highestDraw, reWorkJoins, reWorkMajorJoinsLog, signalDUVolume);
		}

		public Indicators.RZigZag RZigZag(ISeries<double> input , int startBar, int stopBar, bool infoTables, bool statistics, bool volumeCycle, bool printPriceVolumeCycle, int highestLevelToDraw, bool highestDraw, bool reWorkJoins, bool reWorkMajorJoinsLog, bool signalDUVolume)
		{
			return indicator.RZigZag(input, startBar, stopBar, infoTables, statistics, volumeCycle, printPriceVolumeCycle, highestLevelToDraw, highestDraw, reWorkJoins, reWorkMajorJoinsLog, signalDUVolume);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RZigZag RZigZag(int startBar, int stopBar, bool infoTables, bool statistics, bool volumeCycle, bool printPriceVolumeCycle, int highestLevelToDraw, bool highestDraw, bool reWorkJoins, bool reWorkMajorJoinsLog, bool signalDUVolume)
		{
			return indicator.RZigZag(Input, startBar, stopBar, infoTables, statistics, volumeCycle, printPriceVolumeCycle, highestLevelToDraw, highestDraw, reWorkJoins, reWorkMajorJoinsLog, signalDUVolume);
		}

		public Indicators.RZigZag RZigZag(ISeries<double> input , int startBar, int stopBar, bool infoTables, bool statistics, bool volumeCycle, bool printPriceVolumeCycle, int highestLevelToDraw, bool highestDraw, bool reWorkJoins, bool reWorkMajorJoinsLog, bool signalDUVolume)
		{
			return indicator.RZigZag(input, startBar, stopBar, infoTables, statistics, volumeCycle, printPriceVolumeCycle, highestLevelToDraw, highestDraw, reWorkJoins, reWorkMajorJoinsLog, signalDUVolume);
		}
	}
}

#endregion
