#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Collections;
using System.Xml.Serialization;
using System.Windows.Media;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
#endregion

// NT8: Indicators live in this namespace
namespace NinjaTrader.NinjaScript.Indicators
{
    /// <summary>
    /// Enter the description of your new custom indicator here
    /// </summary>
    [Description("Enter the description of your new custom indicator here")]
    public class xMACD : Indicator
    {
        #region Variables

        // Wizard generated variables
        private int firstMA   = 5;   // Default setting for FirstMA
        private int secondMA  = 13;  // Default setting for SecondMA
        private int movingAvg = 6;   // Default setting for MovingAvg

        // User defined variables
        private EMA fastEma;
        private EMA slowEma;
        private EMA avg;

        private bool m_UseVolume = false;

        private double awayTop    =  0.0001;
        private double awayBottom = -0.0001;

        private int m_AwayState       = NONE;
        private int m_AccelState      = NONE;
        private int m_CrossZeroState  = NONE;

        public  int ABOVE_INC_BLUE              = 0;
        public  int ABOVE_DEC_BLUE              = 1;
        public  int ABOVE_DEC_GREY              = 2;
        public  int ABOVE_INC_GREY              = 3;
        public  int BELOW_INC_GREY              = 4;
        public  int BELOW_DEC_GREY              = 5;
        public  int BELOW_INC_RED               = 6;
        public  int BELOW_DEC_RED               = 7;
        public  int ABOVE_CROSSOVER_GREY        = 8;
        public  int ABOVE_CROSSOVER_BLUE        = 9;
        public  int BELOW_CROSSOVER_GRAY        = 10;
        public  int BELOW_CROSSOVER_RED         = 11;
        public  int CROSS_BELOW_ZERO            = 12;
        public  int CROSS_ABOVE_ZERO            = 13;

        public  int ABOVE_1_AWAY                = 14;
        public  int ABOVE_2_AWAY                = 15;
        public  int ABOVE_3_AWAY                = 16;
        public  int BELOW_1_AWAY                = 17;
        public  int BELOW_2_AWAY                = 18;
        public  int BELOW_3_AWAY                = 19;
        public  int ABOVE_0_BELOW_AWAY          = 20;
        public  int BELOW_0_ABOVE_AWAY          = 21;
        public  const int NONE                  = 22;
        public  int ABOVE_4_AWAY                = 23;
        public  int BELOW_4_AWAY                = 24;
        public  int ABOVE_0_BELOW_HALF_AWAY     = 25;
        public  int BELOW_0_ABOVE_HALF_AWAY     = 26;

        private ArrayList array;

        private int m_CurrentBar;

        private bool m_PrintTable = false;

        // NT8: use Series<int> instead of IntSeries
        private Series<int> m_MacdAwayStateSeries;
        private Series<int> m_MacdAccelStateSeries;
        private Series<int> m_MacdCrossZeroStateSeries;

        private string m_FilDirectory = "C:\\Users\\rzahraie\\Desktop\\";
        private string m_FileName     = "Macd6E.txt";
        private System.IO.StreamWriter m_Sw;
        private bool m_CreatFile   = true;
        private bool m_WriteToFile = true;

        #endregion

        #region MacdData

        public class MacdData
        {
            public int m_AwayState;
            public int m_AccelState;
            public int m_CrossZeroState;

            public string m_SAwayState;
            public string m_SAccelState;
            public string m_SCrossZeroState;

            public MacdData(int away, int accel, int cross)
            {
                m_AwayState       = away;
                m_AccelState      = accel;
                m_CrossZeroState  = cross;

                m_SAwayState       = "";
                m_SAccelState      = "";
                m_SCrossZeroState  = "";
            }
        }

        public MacdData GetMacdData(int Bar)
        {
            MacdData md = new MacdData(-1, -1, -1);

            if (Bar < 0)
                return md;

            Update(); // keep original pattern

            md.m_AwayState      = m_MacdAwayStateSeries[CurrentBar - Bar];
            md.m_AccelState     = m_MacdAccelStateSeries[CurrentBar - Bar];
            md.m_CrossZeroState = m_MacdCrossZeroStateSeries[CurrentBar - Bar];

            md.m_SAwayState       = GetState(md.m_AwayState);
            md.m_SAccelState      = GetState(md.m_AccelState);
            md.m_SCrossZeroState  = GetState(md.m_CrossZeroState);

            return md;
        }

        #endregion

        #region States helpers

        private void SetStates()
        {
            if (DifferenceLine[0] > 0)
            {
                if ((DifferenceLine[1] < DifferenceLine[0]) && (AverageLine[0] < DifferenceLine[0]))
                    m_AccelState = ABOVE_INC_BLUE;
                else if ((DifferenceLine[1] > DifferenceLine[0]) && (AverageLine[0] < DifferenceLine[0]))
                    m_AccelState = ABOVE_DEC_BLUE;
                else if ((DifferenceLine[1] > DifferenceLine[0]) && (AverageLine[0] > DifferenceLine[0]))
                    m_AccelState = ABOVE_DEC_GREY;
                else if ((DifferenceLine[1] < DifferenceLine[0]) && (AverageLine[0] > DifferenceLine[0]))
                    m_AccelState = ABOVE_INC_GREY;
            }
            else
            {
                if ((DifferenceLine[1] > DifferenceLine[0]) && (AverageLine[0] > DifferenceLine[0]))
                    m_AccelState = BELOW_INC_RED;
                else if ((DifferenceLine[1] < DifferenceLine[0]) && (AverageLine[0] > DifferenceLine[0]))
                    m_AccelState = BELOW_DEC_RED;
                else if ((DifferenceLine[1] < DifferenceLine[0]) && (AverageLine[0] < DifferenceLine[0]))
                    m_AccelState = BELOW_DEC_GREY;
                else if ((DifferenceLine[1] > DifferenceLine[0]) && (AverageLine[0] < DifferenceLine[0]))
                    m_AccelState = BELOW_INC_GREY;
            }
        }

        private void PrintStates(int Bar)
        {
            if ((Bar - 1) < 0)
                return;

            if (!m_PrintTable)
                return;

            if (!IsFirstTickOfBar)
                return;

            int awaystate  = m_MacdAwayStateSeries[CurrentBar - (Bar - 1)];
            int accelstate = m_MacdAccelStateSeries[CurrentBar - (Bar - 1)];
            int crossstate = m_MacdCrossZeroStateSeries[CurrentBar - (Bar - 1)];

            Print("Bar " + "\t\t\t" + "Accel state" + "\t\t\t" + "Away state" + "\t\t\t" + "Cross state" + "\t\t\t" + "MFI");
            Print("------------------------------------------------------------------------------------------------------------------------------"
                + "-------------------------------------------");
            Print((CurrentBar - 1) + "\t\t\t"
                  + array[accelstate] + "\t\t\t"
                  + array[awaystate] + "\t\t\t"
                  + array[crossstate] + "\t\t\t\t"
                  + this.xQMFI(14,xSeriesInputType.TypPriceXVol)[1].ToString("0.00"));
            Print("");
        }

        private void SetStatesInArray()
        {
            try
            {
                array = new ArrayList();

                array.Add("ABOVE_INC_BLUE");
                array.Add("ABOVE_DEC_BLUE");
                array.Add("ABOVE_DEC_GREY");
                array.Add("ABOVE_INC_GREY");
                array.Add("BELOW_INC_GREY");
                array.Add("BELOW_DEC_GREY");
                array.Add("BELOW_INC_RED");
                array.Add("BELOW_DEC_RED");
                array.Add("ABOVE_XER_GREY");
                array.Add("ABOVE_XER_BLUE");
                array.Add("BELOW_XER_GRAY");
                array.Add("BELOW_XER_RED");
                array.Add("CROSS_BELOW_ZERO");
                array.Add("CROSS_ABOVE_ZERO");

                array.Add("ABOVE_1_AWAY");
                array.Add("ABOVE_2_AWAY");
                array.Add("ABOVE_3_AWAY");
                array.Add("BELOW_1_AWAY");
                array.Add("BELOW_2_AWAY");
                array.Add("BELOW_3_AWAY");
                array.Add("ABOVE_0_BELOW_AWAY");
                array.Add("BELOW_0_ABOVE_AWAY");
                array.Add("NONE");
                array.Add("ABOVE_4_AWAY");
                array.Add("BELOW_4_AWAY");
                array.Add("ABOVE_0_<_1/2_AWAY");
                array.Add("BELOW_0_>_1/2_AWAY");
            }
            catch (Exception e)
            {
                Print(e.ToString());
            }
        }

        public System.Collections.Generic.List<string> GetAllStates()
        {
            var lst = new System.Collections.Generic.List<string>();

            lst.Add((string)array[m_AccelState]);
            lst.Add((string)array[m_AwayState]);
            lst.Add((string)array[m_CrossZeroState]);

            return lst;
        }

        public int GetAccelState()
        {
            return m_AccelState;
        }

        public int GetAwayState()
        {
            return m_AwayState;
        }

        public int GetCrossZoreState()
        {
            return m_CrossZeroState;
        }

        public string GetState(int state)
        {
            string s;

            if (state < 0)
                s = "NONE";
            else
                s = (string)array[state];

            return s;
        }

        #endregion

        #region Utility methods

        public string GetBarDateTime(int Bar)
        {
            string str = Time[m_CurrentBar - Bar].Date.ToString()
                       + " - "
                       + Time[m_CurrentBar - Bar].TimeOfDay.ToString();

            return str;
        }

        public bool IsMacdRisingBlue()
        {
            Update();

            if (DifferenceB[0] != 0)
                return true;
            else
                return false;
        }

        public bool IsMacdFallingRed()
        {
            Update();

            if (DifferenceR[0] != 0)
                return true;
            else
                return false;
        }

        public bool IsMacdRisingGrey()
        {
            Update();

            if (DifferenceG[0] == 0)
                return false;
            else if (DifferenceG[1] <= DifferenceG[0])
                return true;
            else
                return false;
        }

        public bool IsMacdFallingGrey()
        {
            Update();

            if (DifferenceG[0] == 0)
                return false;
            else if (DifferenceG[1] >= DifferenceG[0])
                return true;
            else
                return false;
        }

        public double GetMacdLineValue()
        {
            Update();
            return (DifferenceLine[0] / TickSize);
        }

        public double GetMacdLineValueNormalized()
        {
            Update();
            return Math.Round(DifferenceLine[0] / TickSize, 0);
        }

        public string GetPeakMagnitude()
        {
            double macdvalue = GetMacdLineValueNormalized();

            if ((macdvalue >= 2) && (macdvalue < 5))
                return "LOW_UP_PEAK";
            else if ((macdvalue >= 5) && (macdvalue < 10))
                return "MID_UP_PEAK";
            else if (macdvalue >= 10)
                return "MAX_UP_PEAK";
            else if ((macdvalue <= -2) && (macdvalue > -5))
                return "LOW_DN_PEAK";
            else if ((macdvalue <= -5) && (macdvalue > -10))
                return "MID_DN_PEAK";
            else if (macdvalue <= -10)
                return "MAX_DN_PEAK";
            else
                return string.Empty;
        }

        public string GetMacdPace(int Bar)
        {
            Update();

            string clr = "";

            double macd     = DifferenceLine[0];
            double prevmacd = DifferenceLine[1];
            double signal   = AverageLine[0];

            //Quadrant I - Left - BLUE
            if ((macd > 0) && (macd > signal) && (macd >= prevmacd))
                clr = "UP_TREND";
            //Quadrant I - Right - LIGHT BLUE
            else if ((macd > 0) && (macd > signal) && (macd <= prevmacd))
                clr = "UP_PULBK";
            //Quadrant II - Left - GRAY
            else if ((macd > 0) && (macd < signal) && (macd <= prevmacd))
                clr = "UP_PULBK";
            //Quadrant II - Right - GREEN
            else if ((macd > 0) && (macd < signal) && (macd >= prevmacd))
                clr = "UP_TREND";
            //Quadrant III - Left
            else if ((macd < 0) && (macd < signal) && (macd <= prevmacd))
                clr = "DN_TREND";
            //Quadrant III - Right
            else if ((macd < 0) && (macd < signal) && (macd >= prevmacd))
                clr = "DN_PULBK";
            //Quadrant IV - Left
            else if ((macd < 0) && (macd > signal) && (macd >= prevmacd))
                clr = "DN_PULBK";
            //Quadrant IV - Right
            else if ((macd < 0) && (macd > signal) && (macd <= prevmacd))
                clr = "DN_TREND";
            else
                return "NON";

            return clr;
        }

        private void WriteDataToFile(string str)
        {
            if (!m_WriteToFile)
                return;

            if (m_CreatFile)
            {
                m_FilDirectory += m_FileName;
                m_Sw = System.IO.File.AppendText(m_FilDirectory);
                m_Sw.WriteLine(str);
                m_CreatFile = false;
            }
            else
                m_Sw.WriteLine(str);
        }

        private void WriteToFileEx(string str)
        {
            string filename = this.Instrument.FullName;

            filename = "C:\\Users\\rzahraie\\" + filename + "macd6E0908102" + ".txt";

            System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Append);
            System.IO.StreamWriter sw = new System.IO.StreamWriter(fs);

            sw.WriteLine(str);

            sw.Flush();
            sw.Close();
            fs.Close();
        }

        private void SetValues(double lb, double b, double r, double g, double t, double lg, double f)
        {
            DifferenceLB[0] = lb;
            DifferenceB[0]  = b;
            DifferenceR[0]  = r;
            DifferenceG[0]  = g;
            DifferenceT[0]  = t;
            DifferenceLG[0] = lg;
            DifferenceF[0]  = f;
        }

        #endregion

        #region OnStateChange / OnBarUpdate

        protected override void OnStateChange()
        {
            switch (State)
            {
                case State.SetDefaults:
                    Description                       = "Enter the description of your new custom indicator here";
                    Name                              = "xMACD";
                    Calculate                         = Calculate.OnBarClose;
                    IsOverlay                         = false;
                    DisplayInDataBox                  = true;
                    DrawOnPricePanel                  = false;
                    PaintPriceMarkers                 = true;
                    ScaleJustification                = ScaleJustification.Right;
                    IsSuspendedWhileInactive          = true;

                    FirstMA                           = 5;
                    SecondMA                          = 13;
                    MovingAvg                         = 6;
                    AwayTop                           = 0.0001;
                    AwayBottom                        = -0.0001;
                    PrintTable                        = false;
                    UseVolume                         = false;

                    // Plots: 7 bar-style histograms + 2 lines
                    AddPlot(Brushes.Blue,       "DifferenceB");   // Values[0]
                    AddPlot(Brushes.Red,        "DifferenceR");   // Values[1]
                    AddPlot(Brushes.Gray,       "DifferenceG");   // Values[2]
                    AddPlot(Brushes.LightBlue,  "DifferenceLB");  // Values[3]
                    AddPlot(Brushes.Tomato,     "DifferenceT");   // Values[4]
                    AddPlot(Brushes.LawnGreen,  "DifferenceLG");  // Values[5]
                    AddPlot(Brushes.Fuchsia,    "DifferenceF");   // Values[6]

                    AddPlot(Brushes.Blue,       "DifferenceLine"); // Values[7]
                    AddPlot(Brushes.Red,        "AverageLine");    // Values[8]

                    // "Away" lines (bottom & top)
                    AddLine(Brushes.Red,  2  * -0.0001, "One Away Bottom");
                    AddLine(Brushes.Red,  5  * -0.0001, "Two Away Bottom");
                    AddLine(Brushes.Red,  10 * -0.0001, "Three Away Bottom");

                    AddLine(Brushes.Blue, 2  * 0.0001, "One Away Top");
                    AddLine(Brushes.Blue, 5  * 0.0001, "Two Away Top");
                    AddLine(Brushes.Blue, 10 * 0.0001, "Three Away Top");

                    break;

                case State.Configure:
                    // Use bars required equivalent (approximate original behavior)
                    BarsRequiredToPlot = SecondMA + MovingAvg + 1;
                    break;

                case State.DataLoaded:
                    // Internal series
                    m_MacdAwayStateSeries      = new Series<int>(this);
                    m_MacdAccelStateSeries     = new Series<int>(this);
                    m_MacdCrossZeroStateSeries = new Series<int>(this);

                    SetStatesInArray();

                    // Styling: bar plots wider, line plots standard
                    for (int i = 0; i <= 6; i++)
                    {
                        Plots[i].PlotStyle = PlotStyle.Bar;
                        Plots[i].Width     = 5;
                    }
                    Plots[7].PlotStyle = PlotStyle.Line;
                    Plots[8].PlotStyle = PlotStyle.Line;

                    break;

                case State.Terminated:
                    // Dispose file resources (replacement for NT7 Dispose override)
                    if (m_Sw != null)
                    {
                        m_Sw.Dispose();
                        m_Sw = null;
                    }
                    break;
            }
        }

        protected override void OnBarUpdate()
        {
            try
            {
                if (CurrentBar == 0)
                    return;

                m_CurrentBar = CurrentBar;

                // NOTE: We keep the original conditional-input pattern.
                // If Volume doesn't work directly in NT8, you can replace with VOL() or
                // project Volume into a Series<double>.
                fastEma = EMA((m_UseVolume ? Volume : Input), FirstMA);
                slowEma = EMA((m_UseVolume ? Volume : Input), SecondMA);

                double diff = fastEma[0] - slowEma[0];

                DifferenceLine[0] = diff;

                avg = EMA(DifferenceLine, MovingAvg);

                if (diff > 0.0)
                {
                    if (diff > avg[0])
                    {
                        if (DifferenceLine[0] < DifferenceLine[1])
                        {
                            DifferenceLB[0] = diff;
                            DifferenceB[0]  = 0;
                            DifferenceR[0]  = 0;
                            DifferenceG[0]  = 0;
                            DifferenceT[0]  = 0;
                            DifferenceLG[0] = 0;
                            DifferenceF[0]  = 0;
                        }
                        else
                        {
                            DifferenceB[0]  = diff;
                            DifferenceR[0]  = 0;
                            DifferenceG[0]  = 0;
                            DifferenceLB[0] = 0;
                            DifferenceT[0]  = 0;
                            DifferenceLG[0] = 0;
                            DifferenceF[0]  = 0;
                        }
                    }
                    else
                    {
                        if (DifferenceLine[0] > DifferenceLine[1])
                        {
                            DifferenceLG[0] = diff;
                            DifferenceB[0]  = 0;
                            DifferenceR[0]  = 0;
                            DifferenceG[0]  = 0;
                            DifferenceLB[0] = 0;
                            DifferenceT[0]  = 0;
                            DifferenceF[0]  = 0;
                        }
                        else
                        {
                            DifferenceG[0]  = diff;
                            DifferenceB[0]  = 0;
                            DifferenceLG[0] = 0;
                            DifferenceR[0]  = 0;
                            DifferenceLB[0] = 0;
                            DifferenceT[0]  = 0;
                            DifferenceF[0]  = 0;
                        }
                    }
                }
                else
                {
                    if (diff < avg[0])
                    {
                        if (DifferenceLine[0] > DifferenceLine[1])
                        {
                            DifferenceT[0]  = diff;
                            DifferenceLB[0] = 0;
                            DifferenceB[0]  = 0;
                            DifferenceR[0]  = 0;
                            DifferenceG[0]  = 0;
                        }
                        else
                        {
                            DifferenceR[0]  = diff;
                            DifferenceB[0]  = 0;
                            DifferenceG[0]  = 0;
                            DifferenceT[0]  = 0;
                            DifferenceLB[0] = 0;
                        }
                    }
                    else
                    {
                        if (DifferenceLine[0] < DifferenceLine[1])
                        {
                            DifferenceF[0]  = diff;
                            DifferenceLG[0] = 0;
                            DifferenceR[0]  = 0;
                            DifferenceG[0]  = 0;
                            DifferenceLB[0] = 0;
                            DifferenceT[0]  = 0;
                            DifferenceB[0]  = 0;
                        }
                        else
                        {
                            DifferenceG[0]  = diff;
                            DifferenceF[0]  = diff;
                            DifferenceLG[0] = 0;
                            DifferenceR[0]  = 0;
                            DifferenceLB[0] = 0;
                            DifferenceT[0]  = 0;
                            DifferenceB[0]  = 0;
                        }
                    }
                }

                AverageLine[0]   = avg[0];
                DifferenceLine[0] = diff;

                SetStates();

                if ((DifferenceLine[1] > 0) && (DifferenceLine[0] < 0))
                    m_CrossZeroState = CROSS_BELOW_ZERO;
                else if ((DifferenceLine[1] < 0) && (DifferenceLine[0] > 0))
                    m_CrossZeroState = CROSS_ABOVE_ZERO;
                else
                    m_CrossZeroState = NONE;

                if ((diff < (0.5 * awayTop)) && (diff > 0))
                    m_AwayState = ABOVE_0_BELOW_HALF_AWAY;
                else if ((diff < awayTop) && (diff > (0.5 * awayTop)))
                    m_AwayState = ABOVE_0_BELOW_AWAY;
                else if ((diff > awayTop) && (diff < (2 * awayTop)))
                    m_AwayState = ABOVE_1_AWAY;
                else if ((diff > (2 * awayTop)) && (diff < (3 * awayTop)))
                    m_AwayState = ABOVE_2_AWAY;
                else if ((diff > (3 * awayTop)) && (diff < (4 * awayTop)))
                    m_AwayState = ABOVE_3_AWAY;
                else if (diff > (4 * awayTop))
                    m_AwayState = ABOVE_4_AWAY;
                else if ((diff > (0.5 * awayBottom)) && (diff < 0))
                    m_AwayState = BELOW_0_ABOVE_HALF_AWAY;
                else if ((diff > awayBottom) && (diff < (0.5 * awayBottom)))
                    m_AwayState = BELOW_0_ABOVE_AWAY;
                else if ((diff < awayBottom) && (diff > (2 * awayBottom)))
                    m_AwayState = BELOW_1_AWAY;
                else if ((diff < (2 * awayBottom)) && (diff > (3 * awayBottom)))
                    m_AwayState = BELOW_2_AWAY;
                else if ((diff < (3 * awayBottom)) && (diff > (4 * awayBottom)))
                    m_AwayState = BELOW_3_AWAY;
                else if (diff < (4 * awayBottom))
                    m_AwayState = BELOW_4_AWAY;

                m_MacdAwayStateSeries[0]      = m_AwayState;
                m_MacdAccelStateSeries[0]     = m_AccelState;
                m_MacdCrossZeroStateSeries[0] = m_CrossZeroState;

                PrintStates(CurrentBar);
            }
            catch (Exception e)
            {
                Print("Bar " + CurrentBar);
                Print(e.ToString());
            }
        }

        #endregion

        #region Properties (plots / parameters)

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DifferenceB
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DifferenceR
        {
            get { return Values[1]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DifferenceG
        {
            get { return Values[2]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DifferenceLB
        {
            get { return Values[3]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DifferenceT
        {
            get { return Values[4]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DifferenceLG
        {
            get { return Values[5]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DifferenceF
        {
            get { return Values[6]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DifferenceLine
        {
            get { return Values[7]; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> AverageLine
        {
            get { return Values[8]; }
        }

        [Description("First MA")]
        [Category("Parameters")]
        public int FirstMA
        {
            get { return firstMA; }
            set { firstMA = Math.Max(1, value); }
        }

        [Description("Second MA")]
        [Category("Parameters")]
        public int SecondMA
        {
            get { return secondMA; }
            set { secondMA = Math.Max(1, value); }
        }

        [Description("Moving Average")]
        [Category("Parameters")]
        public int MovingAvg
        {
            get { return movingAvg; }
            set { movingAvg = Math.Max(1, value); }
        }

        [Description("Away top")]
        [Category("Parameters")]
        public double AwayTop
        {
            get { return awayTop; }
            set { awayTop = value; }
        }

        [Description("Away bottom")]
        [Category("Parameters")]
        public double AwayBottom
        {
            get { return awayBottom; }
            set { awayBottom = value; }
        }

        [Description("Print Table")]
        [Category("Output")]
        public bool PrintTable
        {
            get { return m_PrintTable; }
            set { m_PrintTable = value; }
        }

        [Description("Use Volume")]
        [Category("InputType")]
        public bool UseVolume
        {
            get { return m_UseVolume; }
            set { m_UseVolume = value; }
        }

        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xMACD[] cachexMACD;
		public xMACD xMACD()
		{
			return xMACD(Input);
		}

		public xMACD xMACD(ISeries<double> input)
		{
			if (cachexMACD != null)
				for (int idx = 0; idx < cachexMACD.Length; idx++)
					if (cachexMACD[idx] != null &&  cachexMACD[idx].EqualsInput(input))
						return cachexMACD[idx];
			return CacheIndicator<xMACD>(new xMACD(), input, ref cachexMACD);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xMACD xMACD()
		{
			return indicator.xMACD(Input);
		}

		public Indicators.xMACD xMACD(ISeries<double> input )
		{
			return indicator.xMACD(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xMACD xMACD()
		{
			return indicator.xMACD(Input);
		}

		public Indicators.xMACD xMACD(ISeries<double> input )
		{
			return indicator.xMACD(input);
		}
	}
}

#endregion
