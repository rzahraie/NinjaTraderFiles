using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaVolumePivots
    {
        public sealed class State
        {
            public int Window;              // 1 => 3-bar pivot, 2 => 5-bar pivot...
            public long[] Ring;             // store last (2*Window + 1) volumes
            public int RingCount;
            public int RingHead;            // next write index
            public int LastCenterBarIndex;  // bar index of current center candidate (confirmed when ring full)

            public State(int window)
            {
                Window = Math.Max(1, window);
                Ring = new long[2 * Window + 1];
                RingCount = 0;
                RingHead = 0;
                LastCenterBarIndex = -1;
            }
        }

        public static VolPivotEvent? Step(State s, in BarSnapshot bar)
        {
            // Push volume into ring
            s.Ring[s.RingHead] = bar.V;
            s.RingHead = (s.RingHead + 1) % s.Ring.Length;
            if (s.RingCount < s.Ring.Length) s.RingCount++;

            // We can only confirm a pivot once ring is full.
            if (s.RingCount < s.Ring.Length)
                return null;

            // The "center" element is Window bars behind the newest bar.
            // Newest bar is at RingHead-1. Center is at (RingHead-1 - Window).
            int newest = (s.RingHead - 1 + s.Ring.Length) % s.Ring.Length;
            int center = (newest - s.Window + s.Ring.Length) % s.Ring.Length;
            long centerV = s.Ring[center];

            bool isPeak = true;
            bool isTrough = true;

            for (int k = 1; k <= s.Window; k++)
            {
                int left = (center - k + s.Ring.Length) % s.Ring.Length;
                int right = (center + k) % s.Ring.Length;

                long lv = s.Ring[left];
                long rv = s.Ring[right];

                // Strict peak/trough: center must be > neighbors for peak, < for trough.
                if (centerV <= lv || centerV <= rv) isPeak = false;
                if (centerV >= lv || centerV >= rv) isTrough = false;
            }

            // Center bar index is bar.Index - Window
            int centerBarIndex = bar.Index - s.Window;
            if (centerBarIndex == s.LastCenterBarIndex)
                return null; // don't emit twice per bar

            s.LastCenterBarIndex = centerBarIndex;

            if (isPeak) return new VolPivotEvent(centerBarIndex, VolPivotKind.Peak, centerV);
            if (isTrough) return new VolPivotEvent(centerBarIndex, VolPivotKind.Trough, centerV);
            return null;
        }
    }
}
