namespace NinjaTrader.NinjaScript.xPva
{
    public enum OoeLabel
    {
        None,
        P1,
        T1,
        P2,
        T2P,
        T2F
    }

    public class OoeEvent
    {
        public int BarIndex { get; set; }
        public double Volume { get; set; }
        public OoeLabel Label { get; set; }
    }

    public class xPvaVolumeOoe
    {
        private double? p1;
        private double? t1;
        private double? p2;
        private double? t2p;

        public OoeLabel CurrentState { get; private set; } = OoeLabel.None;

        public OoeEvent Step(VolPivotEvent pivot)
        {
            if (pivot == null)
                return null;

            if (pivot.IsPeak)
                return ProcessPeak(pivot);

            if (pivot.IsTrough)
                return ProcessTrough(pivot);

            return null;
        }

        private OoeEvent ProcessPeak(VolPivotEvent pivot)
        {
            double v = pivot.Volume;

            if (CurrentState == OoeLabel.None)
            {
                p1 = v;
                CurrentState = OoeLabel.P1;
                return Emit(pivot, OoeLabel.P1);
            }

            if (CurrentState == OoeLabel.T1)
            {
                if (p1.HasValue && v > p1.Value)
                {
                    p1 = v;
                    CurrentState = OoeLabel.P1;
                    return Emit(pivot, OoeLabel.P1);
                }
                else
                {
                    p2 = v;
                    CurrentState = OoeLabel.P2;
                    return Emit(pivot, OoeLabel.P2);
                }
            }

            if (CurrentState == OoeLabel.P2)
            {
                if (p2.HasValue && v > p2.Value)
                {
                    p2 = v;
                    return Emit(pivot, OoeLabel.P2);
                }
            }

            return null;
        }

        private OoeEvent ProcessTrough(VolPivotEvent pivot)
        {
            double v = pivot.Volume;

            if (CurrentState == OoeLabel.P1)
            {
                t1 = v;
                CurrentState = OoeLabel.T1;
                return Emit(pivot, OoeLabel.T1);
            }

            if (CurrentState == OoeLabel.P2)
            {
                if (t1.HasValue && v < t1.Value)
                {
                    t1 = v;
                    CurrentState = OoeLabel.T1;
                    return Emit(pivot, OoeLabel.T1);
                }
                else
                {
                    t2p = v;
                    CurrentState = OoeLabel.T2P;
                    return Emit(pivot, OoeLabel.T2P);
                }
            }

            if (CurrentState == OoeLabel.T2P)
            {
                if (t1.HasValue && v > t1.Value && v < t2p)
                {
                    CurrentState = OoeLabel.T2F;
                    return Emit(pivot, OoeLabel.T2F);
                }
            }

            return null;
        }

        private OoeEvent Emit(VolPivotEvent pivot, OoeLabel label)
        {
            return new OoeEvent
            {
                BarIndex = pivot.BarIndex,
                Volume = pivot.Volume,
                Label = label
            };
        }

        public void Reset()
        {
            p1 = null;
            t1 = null;
            p2 = null;
            t2p = null;
            CurrentState = OoeLabel.None;
        }
    }
}

