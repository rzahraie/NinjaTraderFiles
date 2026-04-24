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
            if (active == null || active.State == xPvaContainerState.Completed)
            {
                TryStartContainer(cur, dir, sig);
                return active;
            }

            active.LastBar = cur.Index;

            switch (active.Direction)
            {
                case xPvaContainerDirection.Up:
                    StepUp(cur, sig, tickSize);
                    break;

                case xPvaContainerDirection.Down:
                    StepDown(cur, sig, tickSize);
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
                active = new xPvaContainer
                {
                    Id = nextId++,
                    Direction = xPvaContainerDirection.Up,
                    State = xPvaContainerState.SeekingP2,
                    StartBar = cur.Index,
                    LastBar = cur.Index,
                    P1Bar = cur.Index,
                    P1Price = cur.L
                };
            }
            else if (sig.Phase == SignalPhase.ShortValid || sig.Phase == SignalPhase.ShortCandidate)
            {
                active = new xPvaContainer
                {
                    Id = nextId++,
                    Direction = xPvaContainerDirection.Down,
                    State = xPvaContainerState.SeekingP2,
                    StartBar = cur.Index,
                    LastBar = cur.Index,
                    P1Bar = cur.Index,
                    P1Price = cur.H
                };
            }
        }

        private void StepUp(
            in BarSnapshot cur,
            in xPvaSignalResult sig,
            double tickSize)
        {
            switch (active.State)
            {
                case xPvaContainerState.SeekingP2:
				    if (!active.HasP2 || cur.H > active.P2Price + tickSize * 0.5)
				    {
				        active.P2Bar = cur.Index;
				        active.P2Price = cur.H;
				    }
				
				    if (active.HasP2 &&
				        cur.Index > active.P2Bar &&
				        cur.L < active.P2Price - tickSize &&
				        cur.L > active.P1Price + tickSize * 0.5)
				    {
				        active.P3Bar = cur.Index;
				        active.P3Price = cur.L;
				        active.State = xPvaContainerState.PostP3;
				    }
				
				    if (cur.L < active.P1Price - tickSize * 0.5)
				        active.State = xPvaContainerState.Completed;
				
				    break;
                case xPvaContainerState.PostP3:
                    if (cur.H > active.P2Price + tickSize * 0.5)
                    {
                        active.State = xPvaContainerState.Completed;
                    }
                    else if (cur.Index > active.P3Bar &&
			         (sig.Phase == SignalPhase.ShortCandidate ||
			          sig.Phase == SignalPhase.ShortValid ||
			          cur.L < active.P3Price - tickSize * 0.5))
                    {
                        active.FttBar = cur.Index;
                        active.FttPrice = cur.H;
                        active.State = xPvaContainerState.FttDetected;
                    }

                    break;

                case xPvaContainerState.FttDetected:
                    active.State = xPvaContainerState.Completed;
                    break;
            }
        }

        private void StepDown(
            in BarSnapshot cur,
            in xPvaSignalResult sig,
            double tickSize)
        {
            switch (active.State)
            {
                case xPvaContainerState.SeekingP2:
                    if (!active.HasP2 || cur.L < active.P2Price - tickSize * 0.5)
                    {
                        active.P2Bar = cur.Index;
                        active.P2Price = cur.L;
                    }

                    if (active.HasP2 &&
					    cur.Index > active.P2Bar &&
					    cur.H > active.P2Price + tickSize &&
					    cur.H < active.P1Price - tickSize * 0.5)
                    {
                        active.P3Bar = cur.Index;
                        active.P3Price = cur.H;
                        active.State = xPvaContainerState.PostP3;
                    }

                    if (cur.H > active.P1Price + tickSize * 0.5)
                        active.State = xPvaContainerState.Completed;

                    break;

                case xPvaContainerState.PostP3:
                    if (cur.L < active.P2Price - tickSize * 0.5)
                    {
                        active.State = xPvaContainerState.Completed;
                    }
                    else if (cur.Index > active.P3Bar &&
			         (sig.Phase == SignalPhase.LongCandidate ||
			          sig.Phase == SignalPhase.LongValid ||
			          cur.H > active.P3Price + tickSize * 0.5))
                    {
                        active.FttBar = cur.Index;
                        active.FttPrice = cur.L;
                        active.State = xPvaContainerState.FttDetected;
                    }

                    break;

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
                $"FTT={Fmt(c.FttBar, c.FttPrice)}";
        }

        private static string Fmt(int bar, double price)
        {
            return bar >= 0 ? $"{bar}@{price:F2}" : "NA";
        }
    }
}


