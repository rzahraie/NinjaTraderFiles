#region Using declarations
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	 #region Channel Logic
     public class ChannelLogic
        {
            string _debug = "";

            private ChartHelper chartHelper;
            private bool _enableBinarySerialization;
            ChartBars chartBars;
            
            #region Initialization
            public ChannelLogic(ChartHelper charthelper, bool enableBinarySerialization,
                ChartBars cb)
            {

                chartHelper = charthelper;
                chartBars = cb;
            }

            public string GetDebugInfo()
            {
                return _debug;
            }
            #endregion

            #region Serialization
            public void SerializeChannels(List<Channel> channels, string file)
            {
                lock (channels)
                {
                    if (_enableBinarySerialization)
                        SaveChannelList(channels, file);
                    else
                        SaveChannelListXML(channels, file);
                }
            }
            public List<Channel> DeserializeChannels(string file, DateTime startTime)
            {
                if (_enableBinarySerialization)
                    return ReadChannelList(file, startTime);

                return ReadChannelListXML(file, startTime);
            }

            protected void SaveChannelListXML(List<Channel> channels, string file)
            {
                _debug = "";



                FileStream stream = new FileStream(file, FileMode.Create);
                _debug += "File created OK\r\n";

                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Channel>));
                    serializer.Serialize(stream, channels);
                    _debug += "Serialization OK\r\n";
                }
                catch (Exception e)
                {

                    _debug += "error: " + e.Message + "\r\n";
                }
                finally
                {
                    stream.Close();
                    _debug += "Close OK\r\n";
                }
            }

            protected List<Channel> ReadChannelListXML(string file, DateTime startTime)
            {
                _debug = "";

                List<Channel> channels = new List<Channel>();
                Channel channel;
                FileStream stream;

                if (File.Exists(file))
                    stream = new FileStream(file, FileMode.Open);
                else
                    return channels;

                _debug = "Open File Ok\r\n";
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<Channel>));
                    _debug += "Try to deserialize\r\n";
                    channels = (List<Channel>)serializer.Deserialize(stream);
                    _debug += "Channel Count=" + channels.Count + "\r\n";
                }
                catch (Exception e)
                {
                    _debug += "Exception: " + e.Message + "\r\n";
                }
                finally
                {
                    stream.Close();
                    _debug += "Close OK\r\n";
                }

                VerifyVisibility(channels, startTime);

                return channels;
            }

            protected void SaveChannelList(List<Channel> channels, string file)
            {
                _debug = "";

                FileStream stream = new FileStream(file, FileMode.Create);
                _debug += "File created OK\r\n";
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();

                    formatter.Serialize(stream, channels);
                    _debug += "Serialization OK\r\n";
                }
                catch (Exception e)
                {
                    _debug += "error: " + e.Message + "\r\n";
                }
                finally
                {
                    stream.Close();
                    _debug += "Close OK\r\n";
                }
            }
            protected List<Channel> ReadChannelList(string file, DateTime startTime)
            {
                _debug = "";

                List<Channel> channels = new List<Channel>();
                Channel channel;
                FileStream stream;

                if (File.Exists(file))
                    stream = new FileStream(file, FileMode.Open);
                else
                    return channels;

                _debug = "Open File Ok\r\n";
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    _debug += "Try to deserialize\r\n";
                    channels = (List<Channel>)formatter.Deserialize(stream);
                }
                catch (Exception e)
                {
                    _debug += "Exception: " + e.Message + "\r\n";
                }
                finally
                {
                    stream.Close();
                    _debug += "Close OK\r\n";
                }

                VerifyVisibility(channels, startTime);

                return channels;
            }


            public void VerifyVisibility(List<Channel> channels, DateTime startTime)
            {
                for (int i = 0; i < channels.Count; i++)
                {
                    if (channels[i].ChartP1.X <= startTime)
                        channels[i].ReadyToDraw = false;
                    else
                        channels[i].ReadyToDraw = true;
                }
            }
            #endregion

            #region Snap
            public ChartCoord Snap(ChartCoord coord, SharpDX.Color color, SharpDX.Direct2D1.RenderTarget renderTarget)
            {
                SharpDX.Vector2 pntTest;
                SharpDX.RectangleF rectPoint;
                ChartCoord retCoord = coord;

                int size = 8;

                SharpDX.Vector2 pnt = chartHelper.CalcCanvasCoordFromChartCoords(coord);
                rectPoint = new SharpDX.RectangleF((float)pnt.X - (size / 2), (float)pnt.Y - (size / 2), size, size);

                int barIdx = chartHelper.GetBarIdxFromChartCoord(coord);
                if ((barIdx < 0) && (barIdx >= chartBars.Count))
                    return retCoord;

                for (int idx = barIdx - 1; idx < barIdx + 1; idx++)
                {
                    ChartCoord coordHigh;
                    coordHigh.X = chartBars.Bars.GetTime(idx);
                    coordHigh.Y = chartBars.Bars.GetHigh(idx);
                    coordHigh.Valid = true;

                    ChartCoord coordLow;
                    coordLow.X = chartBars.Bars.GetTime(idx);
                    coordLow.Y = chartBars.Bars.GetLow(idx);
                    coordLow.Valid = true;

                    pntTest = chartHelper.CalcCanvasCoordFromChartCoords(coordHigh);
                    rectPoint = new SharpDX.RectangleF((float)pntTest.X - (size / 2), (float)pntTest.Y - (size / 2), size, size);

                    if (rectPoint.Contains(pnt))
                    {
                        retCoord = coordHigh;
                        DrawSnap(rectPoint, color, renderTarget);
                        break;
                    }
                    else
                    {
                        pntTest = chartHelper.CalcCanvasCoordFromChartCoords(coordLow);
                        rectPoint = new SharpDX.RectangleF(pntTest.X - (size / 2), pntTest.Y - (size / 2), size, size);
                        if (rectPoint.Contains(pnt))
                        {
                            retCoord = coordLow;
                            DrawSnap(rectPoint, color, renderTarget);
                            break;
                        }
                    }
                }

                return retCoord;
            }

            public void DrawSnap(SharpDX.RectangleF rect, SharpDX.Color color, SharpDX.Direct2D1.RenderTarget renderTarget)
            {
                //SmoothingMode oldSmoothingMode = graphics.SmoothingMode;
                //graphics.SmoothingMode = SmoothingMode.HighQuality;

                //graphics.FillRectangle(new SolidBrush(color), rect);

                //graphics.SmoothingMode = oldSmoothingMode;
                using (SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, color))
                {
                    renderTarget.FillRectangle(rect, customDXBrush);
                }
            }
            #endregion

            #region Trend Lines
            public bool UpTrend(Channel channel)
            {
                if (channel.ChartP3.Y > channel.ChartP1.Y)
                    return true;

                else return false;
            }

            public double Slope(Channel channel)
            {
                SharpDX.Vector2 P1 = chartHelper.CalcCanvasCoordFromChartCoords(channel.ChartP1);
                SharpDX.Vector2 P3 = chartHelper.CalcCanvasCoordFromChartCoords(channel.ChartP3);
                LineEx RTL = new LineEx(P1, P3);
                return RTL.LineSlope();
            }

            public int CalcPt2BarIdx(Channel channel)
            {
                int barIdx = 0;

                if ((channel.ChartP4.X <= channel.ChartP1.X))   // why would P4 be to the left of P1 ??
                    return chartHelper.GetBarIdxFromChartCoord(channel.ChartP3);

                //Get bar index of most extreme bar between pt1 and pt3
                if (UpTrend(channel))
                    barIdx = GetBarIdxExtreme(channel.ChartP1, channel.ChartP3, true);
                else
                    barIdx = GetBarIdxExtreme(channel.ChartP1, channel.ChartP3, false);

                return barIdx;
            }

            public int GetBarIdxExtreme(ChartCoord startCoord, ChartCoord endCoord, bool highs)
            {
                ChartCoord coord;
                ChartCoord coordTest;
                SharpDX.Vector2 pnt;
                LineEx paralell;

                int startBar = chartHelper.GetBarIdxFromChartCoord(startCoord);
                int endBar = chartHelper.GetBarIdxFromChartCoord(endCoord);

                SharpDX.Vector2 P1 = chartHelper.CalcCanvasCoordFromChartCoords(startCoord);
                SharpDX.Vector2 P3 = chartHelper.CalcCanvasCoordFromChartCoords(endCoord);
                LineEx RTL = new LineEx(P1, P3);

                if (P1.X == 0)
                    return 0;

                int idx = startBar;
                do
                {
                    pnt = chartHelper.GetCanvasCoordBarIndex(idx, highs);
                    paralell = RTL.CreateParallel(pnt);

                    int i;
                    for (i = (idx + 1); i <= endBar; i++)
                    {
                        pnt = chartHelper.GetCanvasCoordBarIndex(i, highs);
                        if (paralell.IsPointOutOfLine(pnt, highs))
                            break;
                    }

                    if (i >= endBar)
                        break;

                    idx++;
                } while (idx <= (endBar));

                return idx;
            }

            public List<LineEx> CalcVEs(ChartCoord startCoord, ChartCoord point2, ChartCoord point3, ChartCoord endCoord, bool highs)
            {
                List<LineEx> ltls = new List<LineEx>();
                _debug = "";

                ChartCoord coord;
                ChartCoord coordTest;
                SharpDX.Vector2 pnt1;
                SharpDX.Vector2 pnt2;
                LineEx paralell;

                int startBar = chartHelper.GetBarIdxFromChartCoord(startCoord);
                int endBar = chartHelper.GetBarIdxFromChartCoord(endCoord);
                int Pt2Bar = chartHelper.GetBarIdxFromChartCoord(point2);

                SharpDX.Vector2 P1 = chartHelper.CalcCanvasCoordFromChartCoords(startCoord);
                SharpDX.Vector2 P2 = chartHelper.CalcCanvasCoordFromChartCoords(point2);
                SharpDX.Vector2 P3 = chartHelper.CalcCanvasCoordFromChartCoords(point3);
                SharpDX.Vector2 P4 = chartHelper.CalcCanvasCoordFromChartCoords(endCoord);

                if (P2.X == 0)
                    return ltls;

                LineEx RTL = new LineEx(P1, P3);
                RTL.ExtendLine((int)P4.X);

                paralell = RTL.CreateParallel(P2);

                int idx = Pt2Bar;
                do
                {
                    int i;
                    for (i = (idx + 1); i <= endBar; i++)
                    {
                        pnt1 = chartHelper.GetCanvasCoordBarIndex(i, highs);
                        //	_debug +=  "Pnt1=" + pnt1.ToString () + " \r\n";
                        if (paralell.IsPointOutOfLine(pnt1, highs))
                            break;
                    }

                    if (i > endBar)
                    {
                        ltls.Add(paralell);
                        break;
                    }
                    else
                    {
                        pnt1 = chartHelper.GetCanvasCoordBarIndex(i, highs);
                        paralell.ExtendLine((int)pnt1.X);
                        ltls.Add(paralell);
                        paralell = RTL.CreateParallel(pnt1);
                    }

                    idx++;
                } while (idx <= endBar);

                return ltls;
            }

            public List<LineEx> CreateTrendlines(Channel channel)
            {
                string ss = "";

                SharpDX.Vector2 P1 = chartHelper.CalcCanvasCoordFromChartCoords(channel.ChartP1);
                SharpDX.Vector2 P2 = chartHelper.CalcCanvasCoordFromChartCoords(channel.ChartP2);
                SharpDX.Vector2 P3 = chartHelper.CalcCanvasCoordFromChartCoords(channel.ChartP3);
                SharpDX.Vector2 P4 = chartHelper.CalcCanvasCoordFromChartCoords(channel.ChartP4);

                List<LineEx> TrendLines = new List<LineEx>();

                if (P1.X == 0)
                    return TrendLines;

                if (P1 == P3)
                    return TrendLines;

                // RTL
                LineEx RTL = new LineEx(P1, P3);
                RTL.ExtendLine((int)P4.X);
                TrendLines.Insert(0, RTL);

                if (channel.PointsCount > 2)
                {
                    List<LineEx> ltls = CalcVEs(channel.ChartP1, channel.ChartP2, channel.ChartP3, channel.ChartP4, UpTrend(channel));
                    foreach (LineEx line in ltls)
                        TrendLines.Add(line);
                }
                else
                {
                    LineEx LTL = RTL.CreateParallel(P2);
                    TrendLines.Add(LTL);
                }

                //				//Calc and update channel extension point
                //				int max = TrendLines.Count-1 ;
                //				Point extensionPnt = new Point (TrendLines[max].P2.X, TrendLines[max].P2.Y); 
                //				chartCoord extPntChart = chartHelper.CalcChartCoordFromCanvasCoord (extensionPnt.X, extensionPnt.Y);
                //				channel.SetPoint(4, extPntChart, true);

                return TrendLines;
            }
            #endregion

            #region Drawing Channels
            public void DrawChannel(Channel channel, SharpDX.Direct2D1.RenderTarget renderTarget)
            {
                List<LineEx> TrendLines = CreateTrendlines(channel);
                Draw(TrendLines, channel.ChannelColor, channel.ChannelWidth, renderTarget);
            }
            public void DrawChannel(Channel channel, SharpDX.Color color, int width, SharpDX.Direct2D1.RenderTarget renderTarget)
            {
                List<LineEx> TrendLines = CreateTrendlines(channel);
                Draw(TrendLines, color, width, renderTarget);
                //if (channel.Numbers) DrawNumbers(graphics,channel);
            }

            protected void Draw(List<LineEx> TrendLines, SharpDX.Color color, int width, SharpDX.Direct2D1.RenderTarget renderTarget)
            {
                //***@@todo-check***
                //RenderTarget.DrawLine()

                //SmoothingMode oldSmoothingMode = graphics.SmoothingMode;
                //graphics.SmoothingMode = SmoothingMode.HighQuality;
                //Pen pen = new Pen(color, width);
                //for (int i = 0; i < TrendLines.Count; i++)
                //    graphics.DrawLine(pen, TrendLines[i].P1, TrendLines[i].P2);

                //graphics.SmoothingMode = oldSmoothingMode;

                for (int i = 0; i < TrendLines.Count; i++)
                {
                    using (SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, color))
                    {
                        renderTarget.DrawLine(TrendLines[i].P1, TrendLines[i].P2, customDXBrush, width);
                    }
                }
            }
            #endregion

            #region Selection
            //***@@todo***
            public bool IsInLine(Channel channel, SharpDX.Vector2 pnt, SharpDX.Direct2D1.RenderTarget renderTarget)
            {
                List<LineEx> TrendLines = CreateTrendlines(channel);

                //Pen p = new Pen(Color.Black, 8);
                //GraphicsPath pth = new GraphicsPath();
                SharpDX.Direct2D1.PathGeometry pth = new SharpDX.Direct2D1.PathGeometry(Core.Globals.D2DFactory);
                SharpDX.Direct2D1.GeometrySink geometrySink = pth.Open();

                for (int i = 0; i < TrendLines.Count; i++)
                {
                    //pth.Reset();

                    if (TrendLines[i].P1 != TrendLines[i].P2)
                    {
                        //pth.AddLine(TrendLines[i].P1, TrendLines[i].P2);
                        //pth.Widen(p);

                        SharpDX.Vector2[] vector2 = new SharpDX.Vector2[2];

                        vector2[0] = TrendLines[i].P1;
                        vector2[1] = TrendLines[i].P2;

                        geometrySink.AddLines(vector2);

                        //@@todo isvisible | widen
                        if (pth.FillContainsPoint(pnt))
                        {
                            pth.Dispose();
                            return true;
                        }

                        //if (pth.IsVisible(pnt))
                        //{
                        //    p.Dispose();
                        //    return true;
                        //}
                    }
                }

                geometrySink.EndFigure(SharpDX.Direct2D1.FigureEnd.Closed);
                geometrySink.Close();

                SharpDX.Direct2D1.SolidColorBrush customDXBrush =
                    new SharpDX.Direct2D1.SolidColorBrush(renderTarget, SharpDX.Color.Black);

                renderTarget.FillGeometry(pth, customDXBrush);

                //p.Dispose();
                pth.Dispose();
                customDXBrush.Dispose();

                return false;
            }

            public SharpDX.RectangleF[] CalcAnchorPoints(Channel channel)
            {
                SharpDX.Vector2 pnt;
                Size sz = new Size(6, 6);
                SharpDX.RectangleF[] rectAnchor = new SharpDX.RectangleF[4];

                List<LineEx> TrendLines = CreateTrendlines(channel);

                //P1
                pnt = new SharpDX.Vector2(TrendLines[0].P1.X - 3, TrendLines[0].P1.Y - 3);
                rectAnchor[0] = new SharpDX.RectangleF(pnt.X, pnt.Y, 6, 6);
                //P3
                pnt = new SharpDX.Vector2(TrendLines[0].P2.X - 3, TrendLines[0].P2.Y - 3);
                rectAnchor[2] = new SharpDX.RectangleF(pnt.X, pnt.Y, 6, 6);
                //P2
                pnt = new SharpDX.Vector2(TrendLines[1].P1.X - 3, TrendLines[1].P1.Y - 3);
                rectAnchor[1] = new SharpDX.RectangleF(pnt.X, pnt.Y, 6, 6);

                //Extension Point
                int max = TrendLines.Count - 1;
                pnt = new SharpDX.Vector2(TrendLines[max].P2.X - 4, TrendLines[max].P2.Y - 4);
                rectAnchor[3] = new SharpDX.RectangleF(pnt.X, pnt.Y, 8, 8);

                return rectAnchor;
            }

            public int IsInAnchorPoint(SharpDX.RectangleF[] rectAnchor, SharpDX.Vector2 pnt)
            {
                int AnchorPointSelected = 0;

                for (int i = 0; i < 4; i++)
                {
                    SharpDX.RectangleF rect = new SharpDX.RectangleF(rectAnchor[i].X, rectAnchor[i].Y, rectAnchor[i].Width,
                        rectAnchor[i].Height);
                    rect.Inflate(4, 4);
                    if (rect.Contains(pnt))
                    {
                        AnchorPointSelected = (i + 1);
                        return AnchorPointSelected;
                    }
                }
                return AnchorPointSelected;
            }

            public int IsInAnchorPoint(Channel channel, SharpDX.Vector2 pnt)
            {
                int AnchorPointSelected = 0;

                if (!channel.IsSelected)
                    return AnchorPointSelected;

                SharpDX.RectangleF[] rectAnchor = CalcAnchorPoints(channel);

                for (int i = 0; i < 4; i++)
                {
                    SharpDX.RectangleF rect = new SharpDX.RectangleF(rectAnchor[i].X, rectAnchor[i].Y, rectAnchor[i].Width,
                        rectAnchor[i].Height);
                    rect.Inflate(4, 4);
                    if (rect.Contains(pnt))
                    {
                        AnchorPointSelected = (i + 1);
                        return AnchorPointSelected;
                    }
                }

                return AnchorPointSelected;
            }

            public void DrawAnchor(int point, SharpDX.Color color, bool inflate, SharpDX.RectangleF[] rectAnchor, 
                SharpDX.Direct2D1.RenderTarget renderTarget)
            {
                SharpDX.RectangleF rect;

                if (inflate)
                {
                    rect = new SharpDX.RectangleF(rectAnchor[point - 1].X, rectAnchor[point - 1].Y, rectAnchor[point - 1].Width,
                        rectAnchor[point - 1].Height);
                    rect.Inflate(1, 1);
                }

                else
                    rect = rectAnchor[point - 1];

                //***@@todo***
                SharpDX.Direct2D1.Ellipse ellipse = new SharpDX.Direct2D1.Ellipse();
                if (point < 4)
                    FillRect(rect, color, renderTarget);
                else
                    FillEllipse(ellipse, color, renderTarget);
            }

            private void FillRect(SharpDX.RectangleF rect, SharpDX.Color color, SharpDX.Direct2D1.RenderTarget renderTarget)
            {
                using (SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, color))
                {
                    renderTarget.FillRectangle(rect, customDXBrush);
                }
            }

            private void FillEllipse(SharpDX.Direct2D1.Ellipse ellipse, SharpDX.Color color, SharpDX.Direct2D1.RenderTarget renderTarget)
            {
                using (SharpDX.Direct2D1.SolidColorBrush customDXBrush = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, color))
                {
                    renderTarget.FillEllipse(ellipse, customDXBrush);
                }
            }

            public SharpDX.RectangleF[] Select(Channel channel, SharpDX.Color anchorColor, SharpDX.Color extensionColor,
                SharpDX.Direct2D1.RenderTarget renderTarget)
            {
                SharpDX.RectangleF[] anchors = CalcAnchorPoints(channel);

                DrawAnchor(1, anchorColor, false, anchors, renderTarget);
                DrawAnchor(2, anchorColor, false, anchors, renderTarget);
                DrawAnchor(3, anchorColor, false, anchors, renderTarget);

                DrawAnchor(4, extensionColor, false, anchors, renderTarget);
                channel.IsSelected = true;
                return anchors;
            }

            public void DeSelect(Channel channel)
            {
                channel.IsSelected = false;
                channel.AnchorPointSelected = 0;
            }

            #endregion

            #region Cancel Points
            public void SetCancelPoint(Channel channel, int point)
            {
                if (point == 1) { channel.prevPointCoord = channel.ChartP1; }
                if (point == 2) { channel.prevPointCoord = channel.ChartP2; }
                if (point == 3) { channel.prevPointCoord = channel.ChartP3; }

                channel.CancelPoint = point;
            }
            public void CancelPoint(Channel channel)
            {
                if (channel.CancelPoint == 0)
                    return;

                if (channel.CancelPoint == 1) { channel.ChartP1 = channel.prevPointCoord; }
                if (channel.CancelPoint == 2) { channel.ChartP2 = channel.prevPointCoord; }
                if (channel.CancelPoint == 3) { channel.ChartP3 = channel.prevPointCoord; }
            }
            #endregion
        }
        #endregion

	#region Channel
        [Serializable()]
        public class Channel : ISerializable
        {
            // Private properties:
            private SharpDX.Color _channelColor;

            //Public properties:

            // Serialization properties
            public bool Numbers;   //** palinuro's additions
            public bool Alarm;

            public int PointsCount;
            public bool ReadyToDraw;
            public ChartCoord ChartP1;
            public ChartCoord ChartP2;
            public ChartCoord ChartP3;
            public ChartCoord ChartP4;
            public int ChannelWidth;
            public int ChannelColorSerialize
            {
                get { return _channelColor.ToAbgr(); }
                set { _channelColor = SharpDX.Color.FromAbgr(value); }
            }
            // End

            [XmlIgnore()]
            public SharpDX.Color ChannelColor
            {
                get { return _channelColor; }
                set { _channelColor = value; }
            }

            [XmlIgnore()]
            [NonSerialized()]
            public bool IsSelected = false;
            [XmlIgnore()]
            [NonSerialized()]
            public int AnchorPointSelected = 0;
            [XmlIgnore()]
            [NonSerialized()]
            public int CancelPoint = 0;
            [XmlIgnore()]
            [NonSerialized()]
            public ChartCoord prevPointCoord;

            public Channel()
            {
                PointsCount = 0;
                ReadyToDraw = false;
                Numbers = false;                //** Palinuro
                Alarm = false;
            }

            public void ResetPoints()
            {
                PointsCount = 0;
            }

            public void SetPoint(int id, ChartCoord coord, bool noCount)
            {
                if (id == 1) ChartP1 = coord;
                if (id == 2) ChartP2 = coord;
                if (id == 3) { ChartP3 = coord; ChartP4 = coord; }
                if (id == 4) ChartP4 = coord;

                if (!noCount)
                    PointsCount++;
            }

            public void SetPoint(int id, DateTime barX, double price, bool noCount)
            {
                if (id == 1) { ChartP1.X = barX; ChartP1.Y = price; }
                if (id == 2) { ChartP2.X = barX; ChartP2.Y = price; }
                if (id == 3) { ChartP3.X = barX; ChartP3.Y = price; ChartP4.X = barX; ChartP4.Y = price; }
                if (id == 4) { ChartP4.X = barX; ChartP4.Y = price; }

                if (!noCount)
                    PointsCount++;
            }

            #region "Binary Serialization"
            // Used only for Binary Serialization
            public Channel(SerializationInfo info, StreamingContext ctxt)
            {
                //Get the values from info and assign them to the appropriate properties
                Numbers = (bool)info.GetValue("Numbers", typeof(bool)); //**
                Alarm = (bool)info.GetValue("Alarm", typeof(bool)); //**

                PointsCount = (int)info.GetValue("PointsCount", typeof(int));
                ReadyToDraw = (bool)info.GetValue("ReadyToDraw", typeof(bool));
                ChartP1.X = (DateTime)info.GetValue("ChartP1.X", typeof(DateTime));
                ChartP1.Y = (double)info.GetValue("ChartP1.Y", typeof(double));
                ChartP1.Valid = (bool)info.GetValue("ChartP1.Valid", typeof(bool));
                ChartP2.X = (DateTime)info.GetValue("ChartP2.X", typeof(DateTime));
                ChartP2.Y = (double)info.GetValue("ChartP2.Y", typeof(double));
                ChartP2.Valid = (bool)info.GetValue("ChartP2.Valid", typeof(bool));
                ChartP3.X = (DateTime)info.GetValue("ChartP3.X", typeof(DateTime));
                ChartP3.Y = (double)info.GetValue("ChartP3.Y", typeof(double));
                ChartP3.Valid = (bool)info.GetValue("ChartP3.Valid", typeof(bool));
                ChartP4.X = (DateTime)info.GetValue("ChartP4.X", typeof(DateTime));
                ChartP4.Y = (double)info.GetValue("ChartP4.Y", typeof(double));
                ChartP4.Valid = (bool)info.GetValue("ChartP4.Valid", typeof(bool));
                ChannelWidth = (int)info.GetValue("ChannelWidth", typeof(int));
                ChannelColor = (SharpDX.Color)info.GetValue("ChannelColor", typeof(SharpDX.Color));
            }

            //Serialization function.
            public void GetObjectData(SerializationInfo info, StreamingContext ctxt)
            {
                //You can use any custom name for your name-value pair. But make sure you
                // read the values with the same name. For ex:- If you write EmpId as "EmployeeId"
                // then you should read the same with "EmployeeId"
                info.AddValue("Numbers", Numbers);  //**
                info.AddValue("Alarm", Alarm);    //**
                info.AddValue("PointsCount", PointsCount);
                info.AddValue("ReadyToDraw", ReadyToDraw);
                info.AddValue("ChartP1.X", ChartP1.X);
                info.AddValue("ChartP1.Y", ChartP1.Y);
                info.AddValue("ChartP1.Valid", ChartP1.Valid);
                info.AddValue("ChartP2.X", ChartP2.X);
                info.AddValue("ChartP2.Y", ChartP2.Y);
                info.AddValue("ChartP2.Valid", ChartP2.Valid);
                info.AddValue("ChartP3.X", ChartP3.X);
                info.AddValue("ChartP3.Y", ChartP3.Y);
                info.AddValue("ChartP3.Valid", ChartP3.Valid);
                info.AddValue("ChartP4.X", ChartP4.X);
                info.AddValue("ChartP4.Y", ChartP4.Y);
                info.AddValue("ChartP4.Valid", ChartP4.Valid);
                info.AddValue("ChannelWidth", ChannelWidth);
                info.AddValue("ChannelColor", ChannelColor);
            }
            #endregion
        }
        #endregion

    public class xHersheyChannelx : Indicator
	{
        #region Variables
        private int fontSize = 12;

        private string _fileName;
        private SharpDX.Vector2 _mouseCurrentPos;

        private string _msgID = "msgWindow";
        private bool _initMouseEvents = false;
        private ToolMode _currentMode = ToolMode.Select;
        private Channel _currentChannel = null;

        int _defaultWidth = 1;
        SharpDX.Color _defaultColor = SharpDX.Color.Blue;
        SharpDX.Color _drawingColor = SharpDX.Color.DarkRed;
        SharpDX.Color _anchorColor = SharpDX.Color.Black;
        SharpDX.Color _snapColor = SharpDX.Color.Blue;
        SharpDX.Color _extensionColor = SharpDX.Color.Blue;
        SharpDX.Color _upChannelColor = SharpDX.Color.Blue;
        SharpDX.Color _dnChannelColor = SharpDX.Color.Red;
        bool _binarySerial = false;

        private bool autocolor = false;
        private bool changeDefault = false;
        private bool _initialAlarm = false;
        private string alarm_sound = "Alert1.wav";
        private double prevTick = 0;
        private double prevTrigger = 0;

        private List<Channel> ChannelList;
        private ChartHelper chartHelper;
        private ChannelLogic channelLogic;
        #endregion

        #region Help Structs
        public enum ToolMode
        {
            Select,
            Selected,
            Create,
            Edit
        };
        #endregion

       
       
        // END OF CLASSES -----------------------------------------------------------------------------------------------------

        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "xHersheyChannelx";
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

        protected override void OnBarUpdate()
        {
            //Add your custom indicator logic here.
            if (CurrentBar == 0)
            {
                chartHelper = new ChartHelper(ChartControl, Bars, TickSize, ChartBars);
                channelLogic = new ChannelLogic(chartHelper, _binarySerial, ChartBars);
            }

            if (Calculate != Calculate.OnBarClose) Calculate = Calculate.OnBarClose;

            if ((ChartControl != null) && (!_initMouseEvents))
            {
                ChartPanel.MouseDown += XHersheyChannelx_MouseDown;
                ChartPanel.MouseMove += XHersheyChannelx_MouseMove;
                ChartPanel.KeyDown += XHersheyChannelx_KeyDown;

                string _dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\NinjaTrader 8\\dat\\";

                if (!Directory.Exists(_dir))
                    Directory.CreateDirectory(_dir);

                _fileName = _dir + Instrument.FullName + "_" + Bars.BarsPeriod.Value + ".dat";

                //Print ("Filename=" + _fileName);

                ChannelList = channelLogic.DeserializeChannels(_fileName, Bars.GetTime(0));

                _initMouseEvents = true;

            }
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            base.OnRender(chartControl, chartScale);

            Rect rect = new Rect(ChartPanel.X, ChartPanel.Y, ChartPanel.Width, ChartPanel.Height);
            chartHelper.Update(rect, ChartPanel.MinValue, ChartPanel.MaxValue);
            int cc = 0;

            lock (ChannelList)
            {
                for (int i = 0; i < ChannelList.Count; i++)
                    if (ChannelList[i].ReadyToDraw)
                    {
                        cc++;
                        channelLogic.DrawChannel(ChannelList[i], ChannelList[i].ChannelColor, ChannelList[i].ChannelWidth, RenderTarget);

                        //@@todo point 1,2,3
                        //@todo alarms
                    }
            }

            ChartPaint();
        }

        //***@todo***
        /// <summary>
		/// EVENTS  ---------------------------------------------------------------------------------------------------
		/// </summary>		
        private void ChartPaint()
        {
            try
            {
                #region Create Channel
                if (_currentMode == ToolMode.Create)
                {
                    if ((_mouseCurrentPos != SharpDX.Point.Zero) || (_currentChannel != null))
                    {
                        ChartCoord coord = chartHelper.CalcChartCoordFromCanvasCoord(_mouseCurrentPos.X, _mouseCurrentPos.Y);
                        if (!coord.Valid) return;

                        coord = channelLogic.Snap(coord, _snapColor, RenderTarget);

                        if (!_currentChannel.ChartP1.Valid)
                            return;

                        _currentChannel.SetPoint(3, coord, true);
                        int barpt2idx = channelLogic.CalcPt2BarIdx(_currentChannel);

                        bool upTrend = channelLogic.UpTrend(_currentChannel);
                        ChartCoord chartCoord = chartHelper.GetChartCoordBarIndex(barpt2idx, upTrend);
                        if (!chartCoord.Valid) return;
                        _currentChannel.SetPoint(2, chartCoord, true);
                        //BigMoose Added	This automatically sets the channel color to green for an up channel, red for down channel				
                        if (upTrend == true) _currentChannel.ChannelColor = _upChannelColor;
                        if (upTrend == false) _currentChannel.ChannelColor = _dnChannelColor;
                        //BigMoose Added						
                        channelLogic.DrawChannel(_currentChannel, _drawingColor, 1, RenderTarget);
                    }
                }
                #endregion

                #region Select Channel
                else if ((_currentMode == ToolMode.Selected))
                {
                    SharpDX.RectangleF[] anchors = channelLogic.Select(_currentChannel, _anchorColor, _extensionColor, RenderTarget);

                    _currentChannel.AnchorPointSelected = channelLogic.IsInAnchorPoint(anchors, new SharpDX.Vector2((float)_mouseCurrentPos.X, 
                        (float)_mouseCurrentPos.Y));
                    if (_currentChannel.AnchorPointSelected > 0 && _currentChannel.AnchorPointSelected < 4)
                        channelLogic.DrawAnchor(_currentChannel.AnchorPointSelected, _anchorColor, true, anchors, RenderTarget);
                    else if (_currentChannel.AnchorPointSelected == 4)
                        channelLogic.DrawAnchor(_currentChannel.AnchorPointSelected, _extensionColor, true, anchors, RenderTarget);
                }
                #endregion

                #region Edit Channel
                else if (_currentMode == ToolMode.Edit)
                {
                    if ((_mouseCurrentPos != SharpDX.Point.Zero) || (_currentChannel != null))
                    {
                        _currentChannel.ReadyToDraw = false;

                        ChartCoord coord = chartHelper.CalcChartCoordFromCanvasCoord(_mouseCurrentPos.X, _mouseCurrentPos.Y);
                        if (!coord.Valid) return;

                        coord = channelLogic.Snap(coord, _snapColor, RenderTarget);

                        if (_currentChannel.AnchorPointSelected > 0)
                        {
                            _currentChannel.SetPoint(_currentChannel.AnchorPointSelected, coord, true);
                            // verify if P2 is > P3 or P2 < P1. P2 must be between P1 and P3
                            if ((_currentChannel.ChartP2.X > _currentChannel.ChartP3.X) ||
                                 (_currentChannel.ChartP2.X < _currentChannel.ChartP1.X))
                            {
                                int barpt2idx = channelLogic.CalcPt2BarIdx(_currentChannel);

                                bool upTrend = channelLogic.UpTrend(_currentChannel);
                                ChartCoord chartCoord = chartHelper.GetChartCoordBarIndex(barpt2idx, upTrend);
                                if (!chartCoord.Valid) return;
                                _currentChannel.SetPoint(2, chartCoord, true);
                            }
                        }

                        channelLogic.DrawChannel(_currentChannel, _drawingColor, 1, RenderTarget);
                    }
                }
                #endregion
            }
            catch (Exception) { }
        }

        private void XHersheyChannelx_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (_currentMode == ToolMode.Create)
                    {
                        if (_currentChannel.PointsCount == 0)
                        {
                            ChartCoord coord = chartHelper.CalcChartCoordFromCanvasCoord((int)e.GetPosition(ChartControl).X,
                                (int)e.GetPosition(ChartControl).Y);
                            if (!coord.Valid) return;

                            coord = channelLogic.Snap(coord, _snapColor, RenderTarget);
                            _currentChannel.SetPoint(1, coord, false); // Set Point 1

                            _mouseCurrentPos = new SharpDX.Point((int)e.GetPosition(ChartControl).X, (int)e.GetPosition(ChartControl).Y);
                            DrawMessage("New Channel\r\nClick Point 3:");
                        }
                        else if (_currentChannel.PointsCount == 1)
                        {
                            ChartCoord coord = chartHelper.CalcChartCoordFromCanvasCoord((int)e.GetPosition(ChartControl).X,
                                (int)e.GetPosition(ChartControl).Y);
                            if (!coord.Valid) return;

                            coord = channelLogic.Snap(coord, _snapColor, RenderTarget);
                            _currentChannel.SetPoint(3, coord, false); // Set Point 3

                            int barpt2idx = channelLogic.CalcPt2BarIdx(_currentChannel);
                            bool upTrend = channelLogic.UpTrend(_currentChannel);
                            //** altered:
                            SetAutoColor(_currentChannel);
                            ChartCoord chartCoord = chartHelper.GetChartCoordBarIndex(barpt2idx, upTrend);
                            if (!chartCoord.Valid) return;
                            _currentChannel.SetPoint(2, chartCoord, false);

                            _currentChannel.ReadyToDraw = true;
                            ChannelList.Add(_currentChannel);
                            _currentMode = ToolMode.Select;
                            _mouseCurrentPos = SharpDX.Point.Zero;
                            channelLogic.SerializeChannels(ChannelList, _fileName);
                            //Print (DateTime.Now.ToShortTimeString() + "=> " + channelLogic.GetDebugInfo () + "  CN=" + ChannelList.Count );
                            RemoveDrawObject(_msgID);
                        }
                    }
                    else if (_currentMode == ToolMode.Select || _currentMode == ToolMode.Selected)
                    {
                        if (_currentMode == ToolMode.Selected)
                        {
                            if (_currentChannel.AnchorPointSelected > 0 && _currentChannel.AnchorPointSelected < 4)
                            {
                                DrawMessage("Channel Selected\r\nMove Point and Click:");
                                channelLogic.SetCancelPoint(_currentChannel, _currentChannel.AnchorPointSelected);
                                _currentMode = ToolMode.Edit;
                                return;
                            }
                            else if (_currentChannel.AnchorPointSelected == 4)
                            {
                                DrawMessage("Channel Selected\r\nMove Extension Point and Click:");
                                channelLogic.SetCancelPoint(_currentChannel, _currentChannel.AnchorPointSelected);
                                _currentMode = ToolMode.Edit;
                                return;
                            }
                        }

                        bool noSelection = true;
                        SharpDX.Point testPnt = new SharpDX.Point((int)e.GetPosition(ChartControl).X, (int)e.GetPosition(ChartControl).Y);
                        for (int i = 0; i < ChannelList.Count; i++)
                        {
                            if (channelLogic.IsInLine(ChannelList[i], testPnt, RenderTarget))
                            {
                                noSelection = false;
                                DrawMessage("Channel Selected:\r\n\r\n/    = Delete Channel\r\n+-  = Change Width\r\n*    = Toggle Color\r\nCtrl+W = Toggle Numbers\r\nCtrl+A = Toggle Alert");
                                _currentChannel = ChannelList[i];
                                _currentMode = ToolMode.Selected;
                            }
                        }

                        if ((noSelection) && (_currentMode == ToolMode.Selected))
                        {
                            RemoveDrawObject(_msgID);
                            _currentMode = ToolMode.Select;
                        }

                    }
                    else if (_currentMode == ToolMode.Edit)
                    {
                        ChartCoord coord = chartHelper.CalcChartCoordFromCanvasCoord((int)e.GetPosition(ChartControl).X, (int)e.GetPosition(ChartControl).Y);
                        if (!coord.Valid) return;

                        coord = channelLogic.Snap(coord, _snapColor, RenderTarget);

                        _currentChannel.SetPoint(_currentChannel.AnchorPointSelected, coord, false);
                        _currentChannel.ReadyToDraw = true;
                        channelLogic.DeSelect(_currentChannel);
                        _currentMode = ToolMode.Select;
                        channelLogic.SerializeChannels(ChannelList, _fileName);
                        //Print (DateTime.Now.ToShortTimeString() + "=> " + channelLogic.GetDebugInfo () + "  CN=" + ChannelList.Count );
                        RemoveDrawObject(_msgID);
                    }
                }

            }
            catch (Exception) { }
        }

        private void XHersheyChannelx_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentMode == ToolMode.Select)
                return;

            if (((_currentMode == ToolMode.Create) && (_currentChannel.PointsCount >= 0)) ||
                (_currentMode == ToolMode.Selected) || (_currentMode == ToolMode.Edit))
            {
                _mouseCurrentPos.X = (int)e.GetPosition(ChartControl).X;
                _mouseCurrentPos.Y = (int)e.GetPosition(ChartControl).Y;

                if (ChartControl != null)
                    Invalidate();
            }
        }

        private void XHersheyChannelx_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key ==Key.Insert)       // Create NEW Channel: first step initialize Channel data
            {
                _currentChannel = new Channel();
                _currentChannel.ChannelColor = _defaultColor;
                _currentChannel.ChannelWidth = _defaultWidth;
                _currentChannel.Alarm = _initialAlarm;
                _currentMode = ToolMode.Create;
                _mouseCurrentPos = SharpDX.Point.Zero;
                DrawMessage("New Channel\r\nClick Point 1:");
            }
            if (e.Key == Key.Escape)       //  Abort selection or new channel creation
            {
                RemoveDrawObject(_msgID);

                if (_currentMode == ToolMode.Edit)
                {
                    channelLogic.CancelPoint(_currentChannel);
                    _currentChannel.ReadyToDraw = true;
                }
                _currentChannel = null;
                _currentMode = ToolMode.Select;

                if (ChartControl != null)
                    Invalidate();
            }
            // remove channel from chart (one or all)
            //	if ((e.KeyCode == Keys.Delete) && (e.Control == true) )	
            if (e.Key == Key.Divide)
            {
                // Remove selected channel
                //		if (e.Control == true)
                //		{

                if (_currentMode == ToolMode.Selected)
                {
                    int idx = ChannelList.IndexOf(_currentChannel);
                    ChannelList.RemoveAt(idx);
                    channelLogic.SerializeChannels(ChannelList, _fileName);
                    //Print (DateTime.Now.ToShortTimeString() + "=> " + channelLogic.GetDebugInfo () + "  CN=" + ChannelList.Count );
                    _currentMode = ToolMode.Select;
                    RemoveDrawObject(_msgID);
                }
                //		}
                /*	
                        else		 //Control key active means Delete ALL channels from chart
                    {
                        DialogResult res = System.Windows.Forms.MessageBox.Show ("Are you sure you want to DELETE all Channels ?", "WARNING !!!!", MessageBoxButtons.YesNo, MessageBoxIcon.Question , MessageBoxDefaultButton.Button2);    
                        if (res == DialogResult.No)
                            return;

                        ChannelList.Clear ();
                        channelLogic.SerializeChannels ( ChannelList, _fileName );
                        //Print (DateTime.Now.ToShortTimeString() + "=> " + channelLogic.GetDebugInfo () + "  CN=" + ChannelList.Count );

                        _currentMode = ToolMode.Select ;
                        if (_currentMode == ToolMode.Selected )
                            RemoveDrawObject(_msgID);				

                        if (ChartControl != null)
                            ChartControl.ChartPanel.Invalidate ();				
                    }
                        */
            }
            //  increase width of channel lines
            if ((e.Key == Key.Add) || (e.Key == Key.OemPlus))
            {
                if (_currentMode == ToolMode.Selected)
                {
                    if (_currentChannel != null)
                    {
                        _currentChannel.ChannelWidth = Math.Min(5, _currentChannel.ChannelWidth + 1);
                        //** altered:
                        SetAutoColor(_currentChannel);


                        if (changeDefault)
                        {
                            _defaultWidth = _currentChannel.ChannelWidth;
                            changeDefault = false;
                        }
                    }

                    channelLogic.SerializeChannels(ChannelList, _fileName);
                    //Print (DateTime.Now.ToShortTimeString() + "=> " + channelLogic.GetDebugInfo () + "  CN=" + ChannelList.Count );
                }
            }
            //  decrease width of channel lines
            if ((e.Key == Key.Subtract) || (e.Key == Key.OemMinus))
            {
                if (_currentMode == ToolMode.Selected)
                {
                    if (_currentChannel != null)
                    {
                        _currentChannel.ChannelWidth = Math.Max(1, _currentChannel.ChannelWidth - 1);
                        //** altered:
                        SetAutoColor(_currentChannel);

                        if (changeDefault)
                        {
                            _defaultWidth = _currentChannel.ChannelWidth;
                            changeDefault = false;
                        }
                    }

                    channelLogic.SerializeChannels(ChannelList, _fileName);
                    //Print (DateTime.Now.ToShortTimeString() + "=> " + channelLogic.GetDebugInfo () + "  CN=" + ChannelList.Count );
                }
            }
            // Toggle Channel Color Key
            if (e.Key == Key.Multiply)   //BigMoose Added
                                              //		if ( ((e.KeyCode == Keys.Oem6)) && (e.Control == true))  // ] key
            {
                if (_currentMode == ToolMode.Selected)
                {
                    if (_currentChannel != null)
                    {
                        Brush backColor = Brushes.Transparent;

                        if (ChartControl != null)
                            backColor = ChartControl.Properties.ChartBackground;

                        try

                        {
                            //BigMoose Added	
                            if (_currentChannel.ChannelColor == _dnChannelColor)
                                _currentChannel.ChannelColor = _upChannelColor;
                            else
                                _currentChannel.ChannelColor = _dnChannelColor;
                            //BigMoose Added	
                            /*  BigMoose Removed
                                                        System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
                                                        colorDialog.AnyColor = true;
                                                        colorDialog.AllowFullOpen = true;
                                                        colorDialog.ShowDialog(); 

                                                        lock (_currentChannel)
                                                        {
                                                        if (colorDialog.Color != backColor)
                                                            _currentChannel.ChannelColor =  colorDialog.Color ;
                                                        if (changeDefault) 
                                                        {
                                                            _defaultColor = colorDialog.Color;
                                                            changeDefault = false;

                                                        }

                                                        colorDialog.Dispose ();

                                                        channelLogic.SerializeChannels ( ChannelList, _fileName );
                                                        //Print (DateTime.Now.ToShortTimeString() + "=> " + channelLogic.GetDebugInfo () + "  CN=" + ChannelList.Count );

                                                        if (ChartControl != null)
                                                            ChartControl.ChartPanel.Invalidate ();		
                                                        }*/
                        }

                        catch (StackOverflowException ex)
                        { }

                    }
                }

            }
            // toggle point 123 labels on the selected channel
            if ((e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control))
            {
                if (_currentMode == ToolMode.Selected)
                {
                    if (_currentChannel != null)
                    {
                        _currentChannel.Numbers = !_currentChannel.Numbers;
                        channelLogic.SerializeChannels(ChannelList, _fileName);
                    }
                }
            }
            if (e.Key == Key.Oem5)   //  \\ character
            {

                if (_currentMode == ToolMode.Selected)
                {
                    if (_currentChannel != null)
                    {
                        changeDefault = !changeDefault;
                    }
                }
            }
            // toggle individual alarm enable on the selected channel
            if ((e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control))
            {
                if (_currentMode == ToolMode.Selected)
                {
                    if (_currentChannel != null)
                    {
                        _currentChannel.Alarm = !_currentChannel.Alarm;
                        channelLogic.SerializeChannels(ChannelList, _fileName);
                    }
                }
            }

            e.Handled = true;
        }

        public void SetAutoColor(Channel _currentChannel)
        {
            if (_currentChannel != null)
            {
                if (autocolor)
                {
                    bool upTrend = channelLogic.UpTrend(_currentChannel);
                    if (upTrend)
                        switch (_currentChannel.ChannelWidth)
                        {
                            case 1:
                                _currentChannel.ChannelColor = SharpDX.Color.Gray;
                                break;
                            case 2:
                                _currentChannel.ChannelColor = SharpDX.Color.LightBlue;
                                break;
                            case 3:
                                _currentChannel.ChannelColor = SharpDX.Color.Blue;
                                break;
                            case 4:
                                _currentChannel.ChannelColor = SharpDX.Color.ForestGreen;
                                break;
                            case 5:
                                _currentChannel.ChannelColor = SharpDX.Color.DarkBlue;
                                break;
                        }
                    else switch (_currentChannel.ChannelWidth)
                        {
                            case 1:
                                _currentChannel.ChannelColor = SharpDX.Color.IndianRed;
                                break;
                            case 2:
                                _currentChannel.ChannelColor = SharpDX.Color.MediumOrchid;
                                break;
                            case 3:
                                _currentChannel.ChannelColor = SharpDX.Color.Red;
                                break;
                            case 4:
                                _currentChannel.ChannelColor = SharpDX.Color.Purple;
                                break;
                            case 5:
                                _currentChannel.ChannelColor = SharpDX.Color.DarkRed;
                                break;
                        }
                }
                //  else _currentChannel.ChannelColor = whatever it was
            }
        }

        public void DrawBarNumbers(string text)
        {
            //@@todo
        }

        public void DrawMessage(string msg)
        {
            Draw.TextFixed(this, _msgID, msg, TextPosition.TopLeft, Brushes.BlueViolet, 
                new SimpleFont("Arial",10) , Brushes.Transparent, Brushes.Transparent, 255);
        }

        private void Invalidate()
        {
            //Draw.Dot(this, "dummy", false, 0, Low[0], Brushes.Red);
            //RemoveDrawObject("dummy");
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xHersheyChannelx[] cachexHersheyChannelx;
		public xHersheyChannelx xHersheyChannelx()
		{
			return xHersheyChannelx(Input);
		}

		public xHersheyChannelx xHersheyChannelx(ISeries<double> input)
		{
			if (cachexHersheyChannelx != null)
				for (int idx = 0; idx < cachexHersheyChannelx.Length; idx++)
					if (cachexHersheyChannelx[idx] != null &&  cachexHersheyChannelx[idx].EqualsInput(input))
						return cachexHersheyChannelx[idx];
			return CacheIndicator<xHersheyChannelx>(new xHersheyChannelx(), input, ref cachexHersheyChannelx);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xHersheyChannelx xHersheyChannelx()
		{
			return indicator.xHersheyChannelx(Input);
		}

		public Indicators.xHersheyChannelx xHersheyChannelx(ISeries<double> input )
		{
			return indicator.xHersheyChannelx(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xHersheyChannelx xHersheyChannelx()
		{
			return indicator.xHersheyChannelx(Input);
		}

		public Indicators.xHersheyChannelx xHersheyChannelx(ISeries<double> input )
		{
			return indicator.xHersheyChannelx(input);
		}
	}
}

#endregion
