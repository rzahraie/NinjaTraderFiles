using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public enum xPvaContainerDirection
    {
        Unknown = 0,
        Up = 1,
        Down = -1
    }

    public enum xPvaContainerState
    {
        None = 0,
        SeekingP1,
        SeekingP2,
        SeekingP3,
        PostP3,
        FttDetected,
        Completed
    }

    public sealed class xPvaContainer
    {
        public int Id;
        public xPvaContainerDirection Direction;
        public xPvaContainerState State;

        public int StartBar;
        public int LastBar;

        public int P1Bar = -1;
        public double P1Price;

        public int P2Bar = -1;
        public double P2Price;

        public int P3Bar = -1;
        public double P3Price;

        public int FttBar = -1;
        public double FttPrice;

        public bool HasP1 => P1Bar >= 0;
        public bool HasP2 => P2Bar >= 0;
        public bool HasP3 => P3Bar >= 0;
        public bool HasFtt => FttBar >= 0;
		
		public int DominantLegStartBar = -1;
		public int DominantLegEndBar = -1;
		
		public int PullbackStartBar = -1;
		public int PullbackEndBar = -1;
		
		public int PostP3AttemptStartBar = -1;
		public int PostP3AttemptEndBar = -1;
		
		public int DominanceRunAtP2;
		public int DominanceRunAtP3;
		public double ImbalanceAtP2;
		public double ImbalanceAtP3;
    }

    public sealed class xPvaContainerEngine
    {
        private int nextId = 1;
        private xPvaContainer active;
		private const int StartLookbackBars = 5;
		private readonly Queue<BarSnapshot> recentBars = new Queue<BarSnapshot>();

        public xPvaContainer Active => active;

        public xPvaContainerEngine()
        {
            active = null;
        }

       public xPvaContainer Step(
		    in BarSnapshot cur,
		    in xPvaDirectionResult dir,
		    in xPvaDominanceResult dom,
		    in xPvaSequenceStats seq,
		    in xPvaImbalanceResult imb,
		    in xPvaSignalResult sig,
		    double tickSize)
        {
			recentBars.Enqueue(cur);

			while (recentBars.Count > StartLookbackBars)
			    recentBars.Dequeue();

            if (active != null && active.State == xPvaContainerState.Completed)
			    active = null;
			
			if (active == null)
			{
			    TryStartContainer(cur, dir, sig);
			    return active;
			}

            active.LastBar = cur.Index;

            switch (active.Direction)
			{
			    case xPvaContainerDirection.Up:
			        StepUp(cur, dom, seq, imb, sig, tickSize);
			        break;
			
			    case xPvaContainerDirection.Down:
			        StepDown(cur, dom, seq, imb, sig, tickSize);
			        break;
			}

            return active;
        }

        private void TryStartContainer(
		    in BarSnapshot cur,
		    in xPvaDirectionResult dir,
		    in xPvaSignalResult sig)
		{
		    if (sig.Phase == SignalPhase.LongValid || sig.Phase == SignalPhase.LongCandidate)
		    {
		        int p1Bar = cur.Index;
		        double p1Price = cur.L;
		
		        foreach (BarSnapshot b in recentBars)
		        {
		            if (b.L < p1Price)
		            {
		                p1Price = b.L;
		                p1Bar = b.Index;
		            }
		        }
		
		        active = new xPvaContainer
		        {
		            Id = nextId++,
		            Direction = xPvaContainerDirection.Up,
		            State = xPvaContainerState.SeekingP2,
		            StartBar = p1Bar,
		            LastBar = cur.Index,
		            P1Bar = p1Bar,
		            P1Price = p1Price,
		            P2Bar = cur.Index,
		            P2Price = cur.H,
		            DominantLegStartBar = p1Bar,
		            DominantLegEndBar = cur.Index
		        };
		    }
		    else if (sig.Phase == SignalPhase.ShortValid || sig.Phase == SignalPhase.ShortCandidate)
		    {
		        int p1Bar = cur.Index;
		        double p1Price = cur.H;
		
		        foreach (BarSnapshot b in recentBars)
		        {
		            if (b.H > p1Price)
		            {
		                p1Price = b.H;
		                p1Bar = b.Index;
		            }
		        }
		
		        active = new xPvaContainer
		        {
		            Id = nextId++,
		            Direction = xPvaContainerDirection.Down,
		            State = xPvaContainerState.SeekingP2,
		            StartBar = p1Bar,
		            LastBar = cur.Index,
		            P1Bar = p1Bar,
		            P1Price = p1Price,
		            P2Bar = cur.Index,
		            P2Price = cur.L,
		            DominantLegStartBar = p1Bar,
		            DominantLegEndBar = cur.Index
		        };
		    }
		}

        private void StepUp(
	    in BarSnapshot cur,
	    in xPvaDominanceResult dom,
	    in xPvaSequenceStats seq,
	    in xPvaImbalanceResult imb,
	    in xPvaSignalResult sig,
	    double tickSize)
		{
		    bool dominant = dom.State == DominanceState.Dominant;
		    bool nonDominant = dom.State == DominanceState.NonDominant;
		
		    switch (active.State)
		    {
		        case xPvaContainerState.SeekingP2:
		        {
		            bool extendsP2 =
					    active.Direction == xPvaContainerDirection.Up
					        ? cur.H > active.P2Price + tickSize * 0.5
					        : cur.L < active.P2Price - tickSize * 0.5;
					
					bool domOrContinuation =
					    dominant || extendsP2;
					
					if (domOrContinuation)
		            {
		                if (active.DominantLegStartBar < 0)
		                    active.DominantLegStartBar = cur.Index;
		
		                active.DominantLegEndBar = cur.Index;
		
		                if (!active.HasP2 || cur.H > active.P2Price + tickSize * 0.5)
		                {
		                    active.P2Bar = cur.Index;
		                    active.P2Price = cur.H;
		                    active.DominanceRunAtP2 = seq.DominanceRunLength;
		                    active.ImbalanceAtP2 = imb.Imbalance;
		                }
		            }
		            else if (active.HasP2 &&
				         nonDominant &&
				         cur.Index > active.P2Bar &&
				         !extendsP2)
		            {
		                if (active.PullbackStartBar < 0)
		                    active.PullbackStartBar = cur.Index;
		
		                active.PullbackEndBar = cur.Index;
		
		                if (!active.HasP3 || cur.L < active.P3Price - tickSize * 0.5)
		                {
		                    active.P3Bar = cur.Index;
		                    active.P3Price = cur.L;
		                    active.DominanceRunAtP3 = seq.DominanceRunLength;
		                    active.ImbalanceAtP3 = imb.Imbalance;
		                }
		
		                active.State = xPvaContainerState.SeekingP3;
		            }
		
		            if (cur.L < active.P1Price - tickSize * 2 &&
					    dom.State == DominanceState.Dominant)
					{
					    active.State = xPvaContainerState.Completed;
					}
		
		            break;
		        }
		
		        case xPvaContainerState.SeekingP3:
		        {
		            if (nonDominant)
		            {
		                active.PullbackEndBar = cur.Index;
		
		                if (!active.HasP3 || cur.L < active.P3Price - tickSize * 0.5)
		                {
		                    active.P3Bar = cur.Index;
		                    active.P3Price = cur.L;
		                    active.DominanceRunAtP3 = seq.DominanceRunLength;
		                    active.ImbalanceAtP3 = imb.Imbalance;
		                }
		            }
		            else if (active.HasP3 && dominant && cur.Index > active.P3Bar)
		            {
		                active.PostP3AttemptStartBar = cur.Index;
		                active.PostP3AttemptEndBar = cur.Index;
		                active.State = xPvaContainerState.PostP3;
		            }
		
		            if (cur.L < active.P1Price - tickSize * 2 &&
					    dom.State == DominanceState.Dominant)
					{
					    active.State = xPvaContainerState.Completed;
					}
		
		            break;
		        }
		
		        case xPvaContainerState.PostP3:
		        {
		            if (dominant)
		            {
		                active.PostP3AttemptEndBar = cur.Index;
		
		                if (cur.H > active.P2Price + tickSize * 0.5)
		                {
		                    active.State = xPvaContainerState.Completed;
		                }
		            }
		            else if (nonDominant && cur.Index > active.PostP3AttemptStartBar)
					{
					    if (active.PostP3AttemptEndBar >= active.PostP3AttemptStartBar &&
					        cur.H < active.P2Price + tickSize * 0.5)
					    {
					        active.FttBar = cur.Index;
					        active.FttPrice = cur.H;
					        active.State = xPvaContainerState.FttDetected;
					    }
					    else
					    {
					        active.State = xPvaContainerState.Completed;
					    }
					}
		
		            if (cur.L < active.P3Price - tickSize * 0.5)
		            {
		                active.FttBar = cur.Index;
		                active.FttPrice = cur.H;
		                active.State = xPvaContainerState.FttDetected;
		            }
		
		            break;
		        }
		
		        case xPvaContainerState.FttDetected:
		            active.State = xPvaContainerState.Completed;
		            break;
		    }
		}

        private void StepDown(
		    in BarSnapshot cur,
		    in xPvaDominanceResult dom,
		    in xPvaSequenceStats seq,
		    in xPvaImbalanceResult imb,
		    in xPvaSignalResult sig,
		    double tickSize)
		{
		    bool dominant = dom.State == DominanceState.Dominant;
		    bool nonDominant = dom.State == DominanceState.NonDominant;
		
		    switch (active.State)
		    {
		        case xPvaContainerState.SeekingP2:
		        {
		            bool extendsP2 =
					    active.Direction == xPvaContainerDirection.Up
					        ? cur.H > active.P2Price + tickSize * 0.5
					        : cur.L < active.P2Price - tickSize * 0.5;
					
					bool domOrContinuation =
					    dominant || extendsP2;
					
					if (domOrContinuation)
		            {
		                if (active.DominantLegStartBar < 0)
		                    active.DominantLegStartBar = cur.Index;
		
		                active.DominantLegEndBar = cur.Index;
		
		                if (!active.HasP2 || cur.L < active.P2Price - tickSize * 0.5)
		                {
		                    active.P2Bar = cur.Index;
		                    active.P2Price = cur.L;
		                    active.DominanceRunAtP2 = seq.DominanceRunLength;
		                    active.ImbalanceAtP2 = imb.Imbalance;
		                }
		            }
		            else if (active.HasP2 &&
				         nonDominant &&
				         cur.Index > active.P2Bar &&
				         !extendsP2)
		            {
		                if (active.PullbackStartBar < 0)
		                    active.PullbackStartBar = cur.Index;
		
		                active.PullbackEndBar = cur.Index;
		
		                if (!active.HasP3 || cur.H > active.P3Price + tickSize * 0.5)
		                {
		                    active.P3Bar = cur.Index;
		                    active.P3Price = cur.H;
		                    active.DominanceRunAtP3 = seq.DominanceRunLength;
		                    active.ImbalanceAtP3 = imb.Imbalance;
		                }
		
		                active.State = xPvaContainerState.SeekingP3;
		            }
		
		            if (cur.H > active.P1Price + tickSize * 2 &&
					    dom.State == DominanceState.Dominant)
					{
					    active.State = xPvaContainerState.Completed;
					}
		
		            break;
		        }
		
		        case xPvaContainerState.SeekingP3:
		        {
		            if (nonDominant)
		            {
		                active.PullbackEndBar = cur.Index;
		
		                if (!active.HasP3 || cur.H > active.P3Price + tickSize * 0.5)
		                {
		                    active.P3Bar = cur.Index;
		                    active.P3Price = cur.H;
		                    active.DominanceRunAtP3 = seq.DominanceRunLength;
		                    active.ImbalanceAtP3 = imb.Imbalance;
		                }
		            }
		            else if (active.HasP3 && dominant && cur.Index > active.P3Bar)
		            {
		                active.PostP3AttemptStartBar = cur.Index;
		                active.PostP3AttemptEndBar = cur.Index;
		                active.State = xPvaContainerState.PostP3;
		            }
		
		            if (cur.H > active.P1Price + tickSize * 2 &&
					    dom.State == DominanceState.Dominant)
					{
					    active.State = xPvaContainerState.Completed;
					}
		
		            break;
		        }
		
		        case xPvaContainerState.PostP3:
		        {
		            if (dominant)
		            {
		                active.PostP3AttemptEndBar = cur.Index;
		
		                if (cur.L < active.P2Price - tickSize * 0.5)
		                {
		                    active.State = xPvaContainerState.Completed;
		                }
		            }
		            else if (nonDominant && cur.Index > active.PostP3AttemptStartBar)
					{
					    if (active.PostP3AttemptEndBar >= active.PostP3AttemptStartBar &&
					        cur.L > active.P2Price - tickSize * 0.5)
					    {
					        active.FttBar = cur.Index;
					        active.FttPrice = cur.L;
					        active.State = xPvaContainerState.FttDetected;
					    }
					    else
					    {
					        active.State = xPvaContainerState.Completed;
					    }
					}
		
		            if (cur.H > active.P3Price + tickSize * 0.5)
		            {
		                active.FttBar = cur.Index;
		                active.FttPrice = cur.L;
		                active.State = xPvaContainerState.FttDetected;
		            }
		
		            break;
		        }
		
		        case xPvaContainerState.FttDetected:
		            active.State = xPvaContainerState.Completed;
		            break;
		    }
		}

        public static string Format(xPvaContainer c)
		{
		    if (c == null)
		        return "CNT none";
		
		    return
		        $"CNT id={c.Id} dir={c.Direction} state={c.State} " +
		        $"P1={Fmt(c.P1Bar, c.P1Price)} " +
		        $"P2={Fmt(c.P2Bar, c.P2Price)} " +
		        $"P3={Fmt(c.P3Bar, c.P3Price)} " +
		        $"FTT={Fmt(c.FttBar, c.FttPrice)} " +
		        $"domLeg={Range(c.DominantLegStartBar, c.DominantLegEndBar)} " +
		        $"pb={Range(c.PullbackStartBar, c.PullbackEndBar)} " +
		        $"post={Range(c.PostP3AttemptStartBar, c.PostP3AttemptEndBar)} " +
		        $"imbP2={c.ImbalanceAtP2:F2} imbP3={c.ImbalanceAtP3:F2}";
		}

        private static string Fmt(int bar, double price)
        {
            return bar >= 0 ? $"{bar}@{price:F2}" : "NA";
        }
		
		private static string Range(int start, int end)
		{
		    return start >= 0 && end >= 0 ? $"{start}-{end}" : "NA";
		}
    }
}








