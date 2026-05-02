namespace APVA.Core
{
    public sealed class xApvaContainerCandidate
    {
        public ContainerDirection Direction { get; set; } = ContainerDirection.Unknown;

        public xApvaPoint P1 { get; set; }
        public xApvaPoint P2 { get; set; }
        public xApvaPoint P3 { get; set; }

        public xApvaLine RTL { get; set; }
        public xApvaLine LTL { get; set; }

        public bool HasValidP3
        {
            get { return P1 != null && P2 != null && P3 != null && RTL != null; }
        }

        public bool IsBroken { get; set; }
        public int BrokenBarIndex { get; set; } = -1;

        public bool IsFboCandidate { get; set; }
        public int FboBarIndex { get; set; } = -1;

        public double WidthAt(int index)
        {
            if (RTL == null || LTL == null)
                return 0.0;

            return System.Math.Abs(LTL.ValueAt(index) - RTL.ValueAt(index));
        }

        public bool IsInside(Bar bar, double tickTolerance)
        {
            if (RTL == null || LTL == null)
                return false;

            double rtl = RTL.ValueAt(bar.Index);
            double ltl = LTL.ValueAt(bar.Index);

            double lower = System.Math.Min(rtl, ltl);
            double upper = System.Math.Max(rtl, ltl);

            return bar.High <= upper + tickTolerance &&
                   bar.Low >= lower - tickTolerance;
        }

        public bool BreaksBoundary(Bar bar, double tickTolerance)
        {
            if (RTL == null || LTL == null)
                return false;

            return !IsInside(bar, tickTolerance);
        }
		
		public bool ExpectedContinuationFailed(Bar bar, double tickTolerance)
		{
		    if (!HasValidP3 || bar == null)
		        return false;
		
		    if (Direction == ContainerDirection.Up)
		    {
		        double ltl = LTL.ValueAt(bar.Index);
		
		        return bar.High < ltl - tickTolerance &&
		               bar.Close < LTL.ValueAt(bar.Index) - tickTolerance;
		    }
		
		    if (Direction == ContainerDirection.Down)
		    {
		        double ltl = LTL.ValueAt(bar.Index);
		
		        return bar.Low > ltl + tickTolerance &&
		               bar.Close > LTL.ValueAt(bar.Index) + tickTolerance;
		    }
		
		    return false;
		}
    }
	
	
}
