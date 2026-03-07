using System;
using System.Windows;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;

namespace NinjaTrader.NinjaScript.Indicators
{
    #region chartCoord Structure
    [Serializable()]
    public struct ChartCoord
    {
        public DateTime X;
        public double Y;
        public bool Valid;
    }
    #endregion

    #region LineEx Class
    [Serializable()]
    public class LineEx
    {
        public SharpDX.Vector2 P1;
        public SharpDX.Vector2 P2;

        public LineEx(float x1, float y1, float x2, float y2)
        {
            P1.X = x1; P1.Y = y1; P2.X = x2; P2.Y = y2;
        }

        public LineEx(SharpDX.Vector2 p1, SharpDX.Vector2 p2)
        {
            P1 = p1; P2 = p2;
        }

        public void ExtendLine(int x)
        {
            double m;

            if (x < P1.X)
                return;

            if (P1.X != P2.X)
                m = (double)(P2.Y - P1.Y) / (double)(P2.X - P1.X);
            else
                m = 0;

            double b = P1.Y - (m * P1.X);
            double y = (m * x) + b;

            P2.X = x;
            P2.Y = Convert.ToInt32(y);
        }

        public LineEx CreateParallel(SharpDX.Vector2 P)
        {
            // Y = mX + b
            // X = (Y-b)/m

            if (P1.X == 0)
                return new LineEx(P.X, P.Y, P.X, P.Y);

            double m = 0;
            if (P1.X != P2.X)
                m = (P2.Y - P1.Y) / (P2.X - P1.X);

            double b = P.Y - (m * P.X);
            double y = m * P2.X + b;

            SharpDX.Vector2 PointX = new SharpDX.Vector2(P2.X, (float)y);

            return (new LineEx(P, PointX));
        }

        public double LineSlope()
        {   // m = (y2-y1)/(x2-x1) = (yn-y1)/(xn-x1)
            double m;

            if (P1.X != P2.X)
                m = (P2.Y - P1.Y) / (P2.X - P1.X);
            else
                m = 0;

            return m;
        }

        public bool IsPointOutOfLine(SharpDX.Vector2 P, bool highs)
        {
            bool ret = false;

            double m;
            if (P1.X != P2.X)
                m = (P2.Y - P1.Y) / (P2.X - P1.X);
            else
                m = 0;

            double b = P1.Y - (m * P1.X);
            double y = m * P.X + b;

            if (highs)
            {
                if (P.Y < y) ret = true;
            }
            else
            {
                if (P.Y > y) ret = true;
            }

            return ret;
        }

    }
    #endregion

    #region ChartHelper
    public class ChartHelper
    {
        Rect _chartBounds;
        double _chartMin;
        double _chartMax;
        double _TickSize;

        private string _debug;
        ChartControl chartControl;
        Bars _Bars;
        ChartBars chartBars;

        #region Constructor
        public ChartHelper(ChartControl chartCtrl, Bars bar, double tickSize, ChartBars ib)
        {
            chartControl = chartCtrl;
            _Bars = bar;
            _TickSize = tickSize;
            chartBars = ib;
        }
        #endregion

        #region Update(Rectangle)
        public void Update(Rect bounds, double min, double max)
        {
            _chartBounds = bounds; _chartMin = min; _chartMax = max;
        }
        #endregion

        #region GetDebugInfo()
        public string GetDebugInfo()
        {
            return _debug;
        }
        #endregion

        #region CalcTickHeight()
        public int CalcTickHeight()
        {
            int tickHeight = (int)Math.Round(Math.Round(((_chartBounds.Bottom - _chartBounds.Y) / (_chartMax - _chartMin)), 0) * _TickSize, 0);
            return tickHeight;
        }
        #endregion

        #region CalcChartCoordFromCanvasCoord()
        public ChartCoord CalcChartCoordFromCanvasCoord(double x, double y)
        {
            _debug = "";
            ChartCoord coord;

            coord.Y = ConvertYtoPrice(y);
            coord.X = ConvertXToTime(x);
            coord.Valid = true;

            if ((coord.X == DateTime.MinValue) || (coord.Y <= 0))
                coord.Valid = false;

            _debug = "X= " + x + " Y= " + y + " Coord.X=" + coord.X + " Coord.Y=" + coord.Y + "Coord.Valid=" + coord.Valid;
            return coord;
        }
        #endregion

        #region CalcCanvasCoordFromChartCoords()
        public SharpDX.Vector2 CalcCanvasCoordFromChartCoords(ChartCoord coord)
        {
            _debug = "";
            SharpDX.Vector2 canvasCoord = new SharpDX.Vector2(0, 0);
            if (chartControl == null)
                return canvasCoord;

            int barIdx;
            DateTime lastTime = _Bars.GetTime(_Bars.Count - 1);

            if (coord.X <= lastTime)
            {
                barIdx = _Bars.GetBar(coord.X);
                if (barIdx <= 0) return canvasCoord;

                canvasCoord.X = chartControl.GetXByBarIndex(chartBars, barIdx);
                canvasCoord.Y = ConvertPriceToY(coord.Y);
            }
            else
            {
                TimeSpan rest = coord.X.Subtract(lastTime);
                int numBars = (int)Math.Round((rest.TotalMinutes / (double)(_Bars.BarsPeriod.Value)), 0);
                barIdx = _Bars.GetBar(lastTime);
                if (barIdx <= 0) return canvasCoord;

                canvasCoord.X = chartControl.GetXByBarIndex(chartBars, barIdx) + (numBars * (int)chartControl.Properties.BarDistance);
                canvasCoord.Y = ConvertPriceToY(coord.Y);
            }

            return canvasCoord;
        }
        #endregion

        #region GetChartCoordBarIndex()
        public ChartCoord GetChartCoordBarIndex(int barIndex, bool high)
        {
            _debug = "";
            ChartCoord coord;
            coord.Valid = false;
            coord.X = DateTime.MinValue;
            coord.Y = 0;

            if (barIndex < 0 || barIndex > _Bars.Count)
                return coord;

            coord.X = ConvertIdxToTime(barIndex);
            if (high)
                coord.Y = _Bars.GetHigh(barIndex);
            else
                coord.Y = _Bars.GetLow(barIndex);

            coord.Valid = true;
            return coord;
        }
        #endregion

        #region GetCanvasCoordBarIndex()
        public SharpDX.Vector2 GetCanvasCoordBarIndex(int barIndex, bool high)
        {
            _debug = "";
            SharpDX.Vector2 coord = new SharpDX.Vector2(0, 0);
            if (chartControl == null)
                return coord;

            double price;

            if (barIndex < 0 || barIndex > _Bars.Count)
                return coord;

            if (high)
                price = _Bars.GetHigh(barIndex);
            else
                price = _Bars.GetLow(barIndex);

            coord.X = ConvertBarIdxToX(barIndex);
            coord.Y = ConvertPriceToY(price);

            return coord;
        }
        #endregion

        #region GetBarIdxFromChartCoord()
        public int GetBarIdxFromChartCoord(ChartCoord coord)
        {
            return _Bars.GetBar(coord.X);
        }
        #endregion

        #region GetBarIdxFromCanvasCoord()
        public int GetBarIdxFromCanvasCoord(int x)
        {
            return ConvertXtoBarIdx(x);
        }
        #endregion

        #region ConvertBarIdxToX()
        public int ConvertBarIdxToX(int barIndex)
        {
            return chartControl.GetXByBarIndex(chartBars, barIndex);
        }
        #endregion

        #region ConvertPriceToY
        public int ConvertPriceToY(double price)
        {
            _debug = "";
            double ratio = (_chartMax - _chartMin) / (_chartBounds.Bottom - _chartBounds.Y);
            double chartY = _chartBounds.Bottom - ((price - _chartMin) / ratio);

            if (double.IsInfinity(chartY) || double.IsNaN(chartY))
                return 0;

            return Convert.ToInt32(chartY);
        }
        #endregion

        #region ConvertYtoPrice
        public double ConvertYtoPrice(double y)
        {
            _debug = "";
            int _tickLength = 0;

            if (_TickSize < 1)
                _tickLength = _TickSize.ToString().Length - 2;

            double chartscale = Math.Abs(_chartMax - _chartMin);
            double boundAreaScale = _chartBounds.Bottom - _chartBounds.Y;

            double ratio = (double)(chartscale) / boundAreaScale;
            double chartPrice = Math.Round(_chartMin + ((_chartBounds.Bottom - y) * ratio), _tickLength);

            return RoundPriceToTick(chartPrice);
        }
        #endregion

        #region ConvertIdxToTime()
        public DateTime ConvertIdxToTime(int barIdx)
        {
            _debug = "";
            DateTime time = DateTime.MinValue;

            if (barIdx <= 0)
                return time;

            if (barIdx >= _Bars.Count)
                time = CalcTimeOutofBounds(barIdx);
            else
                time = _Bars.GetTime(barIdx);

            return time;
        }
        #endregion

        #region ConvertTimeToIdx()
        public int ConvertTimeToIdx(DateTime time)   // in process
        {   //  like Pepe's other netohds, this only works on MINUTE charts
            _debug = "";
            int barIdx = 0;
            DateTime last = _Bars.GetTime(_Bars.Count - 1);  // time of last bar on chart

            if (time <= last)
                return _Bars.GetBar(time);

            TimeSpan rest = time.Subtract(last);   // diffference from now to future time
            int numBars = (int)Math.Round((rest.TotalMinutes / _Bars.BarsPeriod.Value), 0);
            barIdx = (_Bars.Count - 1) + numBars;
            /*
			Print ("Converting: FutureBarIdx =" + (Bars.Count -1).ToString() + " + " + numBars.ToString() + " = "
					+ barIdx.ToString() );
			Print(" & CurrentBar=" + CurrentBar.ToString());
			
			TimeSpan sessionEndTime = Bars.SessionEnd.TimeOfDay;
			TimeSpan lastTime = Bars.Get(lastBarIndex).Time.TimeOfDay;
			TimeSpan rest = sessionEndTime.Subtract(lastTime);
			int numBarsToCloseSession = (int)Math.Round((rest.TotalMinutes / Bars.Period.Value), 0);
			DateTime retTime = Bars.Get(Bars.Count - 1).Time.Add(new TimeSpan(0, numBars * Bars.Period.Value, 0));
			*/

            return barIdx;
        }
        #endregion

        #region ConvertXToTime()
        public DateTime ConvertXToTime(double x)
        {
            _debug = "";
            int barIdx = ConvertXtoBarIdx(x);
            return ConvertIdxToTime(barIdx);
        }
        #endregion

        #region ConvertBarIdxToX()
        public int ConvertXtoBarIdx(double x)
        {
            _debug = "";
            if (chartControl == null)
                return 0;

            int numBarsOnCanvas = 0;
            int idxFirstBar = 0;
            int idxLastBar = 0;

            if (chartBars.ToIndex < _Bars.Count)
            {
                numBarsOnCanvas = chartBars.ToIndex - chartBars.FromIndex;
                idxFirstBar = chartBars.FromIndex;
                idxLastBar = chartBars.ToIndex;
            }
            else
            {
                numBarsOnCanvas = _Bars.Count - chartBars.FromIndex;
                idxFirstBar = chartBars.FromIndex;
                idxLastBar = _Bars.Count - 1;
            }

            int firstBarX = chartControl.GetXByBarIndex(chartBars, idxFirstBar);
            int halfBarWidth = (int)Math.Round(((double)(chartControl.Properties.BarDistance / (double)2)), 0, MidpointRounding.AwayFromZero);
            int margin = firstBarX + halfBarWidth;
            double ratio = 1 + ((x - margin) / (double)(chartControl.Properties.BarDistance));
            int numberPeriods = (int)Math.Truncate(ratio);

            int barIndex = idxFirstBar + numberPeriods;

            if (barIndex < 0)
                return 0;

            _debug = "BarIndex = " + barIndex + " NumBarsCanvas= " + numBarsOnCanvas + " FirstBar=" + idxFirstBar + " LastBar=" + idxLastBar + " Ratio = " + ratio + " NumPeriods =" + numberPeriods;
            return barIndex;
        }
        #endregion

        #region RoundPriceToTick()
        public double RoundPriceToTick(double price)
        {
            _debug = "";
            int numTotalTicks = (int)Math.Round(price / (double)_TickSize, 0);
            return (numTotalTicks * _TickSize);
        }
        #endregion

        #region private CalcTimeOutofBounds()
        private DateTime CalcTimeOutofBounds(int barIdx)
        {
            _debug = "";

            DateTime retTime = DateTime.MinValue;
            int lastBarIndex = _Bars.Count - 1;
            TimeSpan sessionEndTime = _Bars.GetSessionEndTime(0).TimeOfDay;
            TimeSpan lastTime = _Bars.GetTime(lastBarIndex).TimeOfDay;

            TimeSpan rest = sessionEndTime.Subtract(lastTime);
            int numBarsToCloseSession = (int)Math.Round((rest.TotalMinutes / _Bars.BarsPeriod.Value), 0);

            int numBars = barIdx - lastBarIndex;
            retTime = _Bars.GetTime(lastBarIndex).Add(new TimeSpan(0, numBars * _Bars.BarsPeriod.Value, 0));
            _debug = "RetTime= " + retTime.ToString() + " lastBarIndex=" + lastBarIndex +
                " numBarsToCloseSession=" + numBarsToCloseSession + " barIdx=" + barIdx;
            return retTime;
        }
        #endregion

    }
    #endregion

}
