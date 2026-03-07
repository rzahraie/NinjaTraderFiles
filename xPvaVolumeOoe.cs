namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaVolumeOoe
    {
        public sealed class State
        {
            public VolOoeName LastName = VolOoeName.Unknown;

            public long LastP1 = 0;
            public long LastT1 = 0;
            public long LastP2 = 0;
            public long LastT2P = 0;
            public long LastT2F = 0;

            public Band Band = Band.PP;

            public bool HasP1 => LastP1 > 0;
            public bool HasT1 => LastT1 > 0;
            public bool HasP2 => LastP2 > 0;
            public bool HasT2P => LastT2P > 0;
            public bool HasT2F => LastT2F > 0;
        }

        public static VolOoeEvent? Step(State s, in VolPivotEvent pivot, in PermissionEvent perm)
        {
            // Keep current Phase-1 behavior: only advance when permission is granted.
            if (perm.Permission == Permission.Denied)
                return null;

            // 1) First peak -> P1
            if (!s.HasP1)
            {
                if (pivot.Kind == VolPivotKind.Peak)
                {
                    s.LastP1 = pivot.Value;
                    s.LastName = VolOoeName.P1;
                    s.Band = Band.PP;
                    return new VolOoeEvent(pivot.BarIndex, VolOoeName.P1, s.Band, pivot.Value);
                }

                return null;
            }

            // 2) First trough after P1 -> T1
            if (!s.HasT1)
            {
                if (pivot.Kind == VolPivotKind.Trough)
                {
                    s.LastT1 = pivot.Value;
                    s.LastName = VolOoeName.T1;
                    s.Band = Band.PP;
                    return new VolOoeEvent(pivot.BarIndex, VolOoeName.T1, s.Band, pivot.Value);
                }

                return null;
            }

            // 3) First peak after T1:
            //    - if > P1, reset to new P1
            //    - else P2, enter A band
            if (!s.HasP2)
            {
                if (pivot.Kind == VolPivotKind.Peak)
                {
                    if (pivot.Value > s.LastP1)
                    {
                        s.LastP1 = pivot.Value;
                        s.LastT1 = 0;
                        s.LastP2 = 0;
                        s.LastT2P = 0;
                        s.LastT2F = 0;
                        s.LastName = VolOoeName.P1;
                        s.Band = Band.PP;
                        return new VolOoeEvent(pivot.BarIndex, VolOoeName.P1, s.Band, pivot.Value);
                    }

                    s.LastP2 = pivot.Value;
                    s.LastName = VolOoeName.P2;
                    s.Band = Band.A;
                    return new VolOoeEvent(pivot.BarIndex, VolOoeName.P2, s.Band, pivot.Value);
                }

                return null;
            }

            // 4) After P2:
            //    - higher Peak updates P2 and stays in A band
            //    - lower Trough than T1 resets to T1 (new structure)
            //    - otherwise T2P and enter B band
            if (!s.HasT2P)
            {
                if (pivot.Kind == VolPivotKind.Peak)
                {
                    if (pivot.Value > s.LastP2)
                    {
                        s.LastP2 = pivot.Value;
                        s.LastName = VolOoeName.P2;
                        s.Band = Band.A;
                        return new VolOoeEvent(pivot.BarIndex, VolOoeName.P2, s.Band, pivot.Value);
                    }

                    return null;
                }

                if (pivot.Kind == VolPivotKind.Trough)
                {
                    if (pivot.Value < s.LastT1)
                    {
                        s.LastT1 = pivot.Value;
                        s.LastP2 = 0;
                        s.LastT2P = 0;
                        s.LastT2F = 0;
                        s.LastName = VolOoeName.T1;
                        s.Band = Band.PP;
                        return new VolOoeEvent(pivot.BarIndex, VolOoeName.T1, s.Band, pivot.Value);
                    }

                    s.LastT2P = pivot.Value;
                    s.LastName = VolOoeName.T2P;
                    s.Band = Band.B;
                    return new VolOoeEvent(pivot.BarIndex, VolOoeName.T2P, s.Band, pivot.Value);
                }

                return null;
            }

            // 5) After T2P:
            //    - trough lower than T2P but higher than T1 -> T2F, enter C band
            //    - trough below T1 resets to T1
            //    - higher peak than P2 refreshes P2 and goes back to A band
            if (!s.HasT2F)
            {
                if (pivot.Kind == VolPivotKind.Trough)
                {
                    if (pivot.Value < s.LastT1)
                    {
                        s.LastT1 = pivot.Value;
                        s.LastP2 = 0;
                        s.LastT2P = 0;
                        s.LastT2F = 0;
                        s.LastName = VolOoeName.T1;
                        s.Band = Band.PP;
                        return new VolOoeEvent(pivot.BarIndex, VolOoeName.T1, s.Band, pivot.Value);
                    }

                    if (pivot.Value < s.LastT2P && pivot.Value > s.LastT1)
                    {
                        s.LastT2F = pivot.Value;
                        s.LastName = VolOoeName.T2F;
                        s.Band = Band.C;
                        return new VolOoeEvent(pivot.BarIndex, VolOoeName.T2F, s.Band, pivot.Value);
                    }

                    return null;
                }

                if (pivot.Kind == VolPivotKind.Peak)
                {
                    if (pivot.Value > s.LastP2)
                    {
                        s.LastP2 = pivot.Value;
                        s.LastT2P = 0;
                        s.LastT2F = 0;
                        s.LastName = VolOoeName.P2;
                        s.Band = Band.A;
                        return new VolOoeEvent(pivot.BarIndex, VolOoeName.P2, s.Band, pivot.Value);
                    }

                    return null;
                }
            }

            // 6) After T2F:
            //    For now, keep it stable until later B..K logic is added.
            //    We do allow a stronger P2 to restart A-band progression,
            //    or a lower T1 reset.
            if (pivot.Kind == VolPivotKind.Peak && pivot.Value > s.LastP2)
            {
                s.LastP2 = pivot.Value;
                s.LastT2P = 0;
                s.LastT2F = 0;
                s.LastName = VolOoeName.P2;
                s.Band = Band.A;
                return new VolOoeEvent(pivot.BarIndex, VolOoeName.P2, s.Band, pivot.Value);
            }

            if (pivot.Kind == VolPivotKind.Trough && pivot.Value < s.LastT1)
            {
                s.LastT1 = pivot.Value;
                s.LastP2 = 0;
                s.LastT2P = 0;
                s.LastT2F = 0;
                s.LastName = VolOoeName.T1;
                s.Band = Band.PP;
                return new VolOoeEvent(pivot.BarIndex, VolOoeName.T1, s.Band, pivot.Value);
            }

            return null;
        }
    }
}
