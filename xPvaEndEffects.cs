namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaEndEffects
    {
        public sealed class State
        {
            public EndEffectKind LastKind = EndEffectKind.Unknown;
            public Band LastBand = Band.Unknown;
            public VolOoeName LastSource = VolOoeName.Unknown;
            public int LastBarIndex = -1;
        }

        public static EndEffectEvent? Step(State s, in VolOoeEvent ooe)
        {
            EndEffectKind kind = Classify(ooe);

            if (kind == EndEffectKind.Unknown)
                return null;

            // prevent duplicate EE emission on same bar/source/kind
            if (s.LastBarIndex == ooe.BarIndex &&
                s.LastKind == kind &&
                s.LastBand == ooe.Band &&
                s.LastSource == ooe.Name)
            {
                return null;
            }

            s.LastKind = kind;
            s.LastBand = ooe.Band;
            s.LastSource = ooe.Name;
            s.LastBarIndex = ooe.BarIndex;

            return new EndEffectEvent(
                ooe.BarIndex,
                kind,
                ooe.Band,
                ooe.Name,
                ooe.Value);
        }

        private static EndEffectKind Classify(in VolOoeEvent ooe)
        {
            // Minimal deterministic mapping only.
            // This is NOT the full Hershey EE table.
            // It is just enough to create a stable structural layer above OOE.

            switch (ooe.Band)
            {
                case Band.PP:
                    if (ooe.Name == VolOoeName.P1 || ooe.Name == VolOoeName.T1)
                        return EndEffectKind.Prelim;
                    break;

                case Band.A:
                    if (ooe.Name == VolOoeName.P2)
                        return EndEffectKind.PeakContext;
                    break;

                case Band.B:
                    if (ooe.Name == VolOoeName.T2P)
                        return EndEffectKind.CandidateEnd;
                    break;

                case Band.C:
                    if (ooe.Name == VolOoeName.T2F)
                        return EndEffectKind.TroughContext;
                    break;
            }

            return EndEffectKind.Unknown;
        }
    }
}