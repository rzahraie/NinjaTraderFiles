using System;

namespace APVA.Core
{
    public sealed class xApvaPoint
    {
        public int Index { get; set; }
        public double Price { get; set; }

        public xApvaPoint()
        {
        }

        public xApvaPoint(int index, double price)
        {
            Index = index;
            Price = price;
        }
    }

    public sealed class xApvaLine
    {
        public xApvaPoint Start { get; private set; }
        public xApvaPoint End { get; private set; }

        public double Slope { get; private set; }

        public xApvaLine(xApvaPoint start, xApvaPoint end)
        {
            if (start == null)
                throw new ArgumentNullException("start");

            if (end == null)
                throw new ArgumentNullException("end");

            if (end.Index == start.Index)
                throw new ArgumentException("Line start and end index cannot be the same.");

            Start = start;
            End = end;

            Slope = (End.Price - Start.Price) / (End.Index - Start.Index);
        }

        public double ValueAt(int index)
        {
            return Start.Price + Slope * (index - Start.Index);
        }

        public bool IsAbove(Bar bar, double tickTolerance)
        {
            double lineValue = ValueAt(bar.Index);
            return bar.Low > lineValue + tickTolerance;
        }

        public bool IsBelow(Bar bar, double tickTolerance)
        {
            double lineValue = ValueAt(bar.Index);
            return bar.High < lineValue - tickTolerance;
        }

        public bool BreaksLowerBoundary(Bar bar, double tickTolerance)
        {
            double lineValue = ValueAt(bar.Index);
            return bar.Low < lineValue - tickTolerance;
        }

        public bool BreaksUpperBoundary(Bar bar, double tickTolerance)
        {
            double lineValue = ValueAt(bar.Index);
            return bar.High > lineValue + tickTolerance;
        }
    }
}