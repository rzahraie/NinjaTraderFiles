using System.Windows.Media;
using System.Collections.Generic;
using System.ComponentModel;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators
{
	public class ILengthComparerDesc:IComparer<xArrowLine>
	{
		public int Compare(xArrowLine x, xArrowLine y)
		{
			return y.Length - x.Length; //descending sort

		}
	};
	
	public class ILengthComparerAsc:IComparer<xArrowLine>
	{
		public int Compare(xArrowLine x, xArrowLine y)
		{
			return x.Length - y.Length; //ascending sort

		}
	};	
		
    public class xArrowLine
    {
        #region Point
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

        const int UP = 10;
        const int DOWN = 11;

        public string m_ParentTag;
        public string m_Tag;

        public bool m_RemoveParent = false;

        public TPoint m_BeginPoint;
        public TPoint m_EndPoint;
        public TPoint m_ExtendedPoint;

        public double m_Slope;
        public int m_Direction;
        public int m_Level;

        public int m_Id;

        public bool m_Joined = false;
        public bool m_Ended = false;
        public bool m_Composite = false;
        public bool m_Extended = false;

        public Brush m_LineColor;
        public DashStyleHelper m_LineStyle;
        public int m_LineWidth;

        public double m_PeakVolume;
        public double m_TickSize = 0.0001;

        private int m_Angle;
        private bool m_Sealed;

        #region CONSTS
        //public const int REGULAR = 1;
        //public const int OUTSIDE = 2;
        //public const int COMPOSITE = 3;

        public const int B_2_B = 100;
        public const int INC_B_2_DEC_R = 101;
        public const int DEC_R_2_INC_B = 102;
        public const int R_2_R = 103;
        public const int INC_R_2_DEC_B = 104;
        public const int DEC_B_2_INC_R = 105;
        public const int B_2_BSPECIAL = 106;
        public const int R_2_RSPECIAL = 107;

        public const int R_DEC = 110;
        public const int B_DEC = 111;
        public const int R_INC = 112;
        public const int B_INC = 113;

        public const int TAPE = 200;
        public const int TRAVERSE = 201;
        public const int CHANNEL = 202;
        public const int GCHANNEL = 203;

        public const string sTAPE = "TAPE";
        public const string sTRAVERSE = "TRAV";
        public const string sCHANNEL = "CHAN";
        public const string sGCHANNEL = "GRND";
        #endregion

        #region Properties

        public bool Sealed { get { return m_Sealed; } set { m_Sealed = value; } }
        public int Length { get { return (m_ExtendedPoint.X - m_BeginPoint.X); } }
        public string VolumeSequence { get { return m_VolumeSequence; } set { m_VolumeSequence = value; } }
        public string VolumeSymbolSequence { get { return m_VolumeSymbolSequence; } set { m_VolumeSymbolSequence = value; } }
        public int Type { get { return m_Type; } set { m_Type = value; } }
        public string SType
        {
            get
            {
                //if (m_Type == REGULAR) return "REG"; 
                //else if (m_Type == OUTSIDE) return "OUT"; 
                //if (m_Type == COMPOSITE) return "CMP";
                if (m_Type == TAPE) return sTAPE;
                else if (m_Type == TRAVERSE) return sTRAVERSE;
                else if (m_Type == CHANNEL) return sCHANNEL;
                else if (m_Type == GCHANNEL) return sGCHANNEL;
                else return "";
            }
        }
        public int State { get { return m_State; } set { m_State = value; } }

        public string SState
        {
            get
            {
                if (m_State == B_2_B) return "B_2_B";
                else if (m_State == INC_B_2_DEC_R) return "INC_B_2_DEC_R";
                else if (m_State == DEC_R_2_INC_B) return "DEC_R_2_INC_B";
                else if (m_State == R_2_R) return "R_2_R";
                else if (m_State == INC_R_2_DEC_B) return "INC_R_2_DEC_B";
                else if (m_State == DEC_B_2_INC_R) return "DEC_B_2_INC_R";
                /*else if (m_State == R_DEC) return "R_DEC";
                else if (m_State == B_DEC) return "B_DEC";
                else if (m_State == R_INC) return "R_INC";
                else if (m_State == B_INC) return "B_INC";*/
                else if (m_State == R_DEC) return "2_R";
                else if (m_State == B_DEC) return "2_B";
                else if (m_State == R_INC) return "2_R";
                else if (m_State == B_INC) return "2_B";
                else if (m_State == B_2_BSPECIAL) return "B_2_B*";
                else if (m_State == R_2_RSPECIAL) return "R_2_R*";
                else return "";

            }
        }

        public string SDirection
        {
            get
            {
                string s = (m_Direction == UP) ? "UP" : "DOWN";
                return s;
            }
        }

        public int Id { get { return m_Id; } }

        public string SId
        {
            get
            {
                return (System.Convert.ToString(m_Id));
            }
        }

        public xArrowLine Left1 { set { m_Left1 = value; } get { return m_Left1; } }
        public xArrowLine Right { set { m_Right = value; } get { return m_Right; } }
        public xArrowLine Left2 { set { m_Left2 = value; } get { return m_Left2; } }

        public int Angle
        {
            get
            {
                //double pricediff = (m_EndPoint.Y - m_BeginPoint.Y)/m_TickSize;
                //int timediff = m_EndPoint.X - m_BeginPoint.X;

                //return(System.Convert.ToInt16(Math.Atan(pricediff/timediff)*(180/Math.PI)));

                return m_Angle;
            }
        }

        public int Point3
        {
            get
            {
                if (m_Composite)
                {
                    return (m_Right.m_ExtendedPoint.X);

                }
                else
                {
                    return m_EndPoint.X;
                }
            }

        }

        public int Point3Angle
        {
            get
            {
                if (!m_Composite) return 0;

                double pricediff = (m_Right.m_ExtendedPoint.Y - m_Left1.m_BeginPoint.Y) / m_TickSize;
                int timediff = m_Right.m_ExtendedPoint.X - m_Left1.m_BeginPoint.X;

                return (System.Convert.ToInt16(System.Math.Atan(pricediff / timediff) * (180 / System.Math.PI)));
            }

        }

        public string GannAngle
        {
            get
            {
                int ang = System.Math.Abs(Angle);

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

        public string Label
        {

            get
            {
                string label = m_BeginPoint.X.ToString() + "," +
                                m_ExtendedPoint.X.ToString();

                return label;

            }
        }


        #endregion

        private string m_VolumeSequence = "";
        private string m_VolumeSymbolSequence = "";
        private int m_State;
        private int m_Type;

        public int m_Point3;
        public bool m_Broken;

        private xArrowLine m_Left1;
        private xArrowLine m_Right;
        private xArrowLine m_Left2;

        private void Init()
        {
            m_BeginPoint = new TPoint();
            m_EndPoint = new TPoint();
            m_ExtendedPoint = new TPoint();
        }

        public xArrowLine(int x1, double y1, int x2, double y2, int direction, int level, int id)
        {
            Init();

            m_Direction = direction;

            m_BeginPoint.X = x1;
            m_BeginPoint.Y = y1;

            m_EndPoint.X = x2;
            m_EndPoint.Y = y2;

            m_ExtendedPoint = m_EndPoint;

            m_Broken = false;

            m_Level = level;

            CalculateSlope();

            m_Tag = "RTL" + System.Convert.ToString(x1) +
                    System.Convert.ToString(x2) + System.Convert.ToString(m_Direction) +
                    System.Convert.ToString(m_Level);

            m_Id = id;

            SetLineProperties();

            m_Angle = CalculateAngle();
        }

        public void UpdateEndPoint(int x2, double y2)
        {
            m_ExtendedPoint.X = x2;
            m_ExtendedPoint.Y = y2;

        }

        protected void CalculateSlope()
        {
            m_Slope = (m_ExtendedPoint.Y - m_BeginPoint.Y) / (m_ExtendedPoint.X - m_BeginPoint.X);

            m_Direction = (m_Slope > 0) ? UP : DOWN;
        }

        private int CalculateAngle()
        {
            double pricediff = (m_EndPoint.Y - m_BeginPoint.Y) / m_TickSize;
            int timediff = m_EndPoint.X - m_BeginPoint.X;

            return (System.Convert.ToInt16(System.Math.Atan(pricediff / timediff) * (180 / System.Math.PI)));

        }

        public double LineValue(int x)
        {
            double y = m_Slope * (x - m_BeginPoint.X) + m_BeginPoint.Y;
            return y;
        }

        public void SetLineLevel(int level)
        {
            m_Level = level;

            SetLineProperties();
        }

        public void SetLineProperties()
        {
            SetLineThickness();
            SetLineType();
            SetLineColorScheme();
        }

        private void SetLineThickness()
        {
            switch (m_Level)
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
            switch (m_Level)
            {
                case 0:
                    m_LineStyle = DashStyleHelper.Solid;
                    break;

                case 1:
                    m_LineStyle = DashStyleHelper.Dash;
                    break;

                case 2:
                    m_LineStyle = DashStyleHelper.DashDot;
                    break;

                case 3:
                    m_LineStyle = DashStyleHelper.Dot;
                    break;

                case 4:
                    m_LineStyle = DashStyleHelper.DashDotDot;
                    break;

                case 5:
                    m_LineStyle = DashStyleHelper.Solid;
                    break;

                case 6:
                    m_LineStyle = DashStyleHelper.Dash;
                    break;

                case 7:
                    m_LineStyle = DashStyleHelper.DashDot;
                    break;

                case 8:
                    m_LineStyle = DashStyleHelper.Dot;
                    break;

                case 9:
                    m_LineStyle = DashStyleHelper.DashDotDot;
                    break;

                default:
                    m_LineStyle = DashStyleHelper.Dash;
                    break;
            }
        }

        private void SetLineColorScheme()
        {
            if (m_Direction == UP)
            {
                switch (m_Level)
                {
                    case 0:
                        m_LineColor = Brushes.Blue;
                        break;

                    case 1:
                        m_LineColor = Brushes.Blue;
                        break;

                    case 2:
                        m_LineColor = Brushes.Blue;
                        break;

                    case 3:
                        m_LineColor = Brushes.DarkTurquoise;
                        break;

                    case 4:
                        m_LineColor = Brushes.LightBlue;
                        break;

                    default:
                        m_LineColor = Brushes.LightSkyBlue;
                        break;
                }
            }
            else
            {
                switch (m_Level)
                {
                    case 0:
                        m_LineColor = Brushes.Red;
                        break;

                    case 1:
                        m_LineColor = Brushes.Red;
                        break;

                    case 2:
                        m_LineColor = Brushes.Red;
                        break;

                    case 3:
                        m_LineColor = Brushes.Fuchsia;
                        break;

                    case 4:
                        m_LineColor = Brushes.LightPink;
                        break;

                    default:
                        m_LineColor = Brushes.Tomato;
                        break;
                }
            }
        }

    }
}

    

   





