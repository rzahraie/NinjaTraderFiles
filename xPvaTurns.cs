namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public sealed class xPvaTurns
    {
        public sealed class State
        {
            public TurnType LastType = TurnType.Unknown;
            public EndEffectKind LastSourceKind = EndEffectKind.Unknown;
            public Band LastBand = Band.Unknown;
            public VolOoeName LastSource = VolOoeName.Unknown;
            public int LastBarIndex = -1;
        }

        public static TurnEvent? Step(State s, in EndEffectEvent ee)
        {
            TurnType type = Classify(ee);

            if (type == TurnType.Unknown)
                return null;

            if (s.LastBarIndex == ee.BarIndex &&
                s.LastType == type &&
                s.LastSourceKind == ee.Kind &&
                s.LastBand == ee.Band &&
                s.LastSource == ee.Source)
            {
                return null;
            }

            s.LastType = type;
            s.LastSourceKind = ee.Kind;
            s.LastBand = ee.Band;
            s.LastSource = ee.Source;
            s.LastBarIndex = ee.BarIndex;

            return new TurnEvent(
                ee.BarIndex,
                type,
                ee.Kind,
                ee.Band,
                ee.Source,
                ee.Value);
        }

        private static TurnType Classify(in EndEffectEvent ee)
        {
            // Minimal deterministic bridge only.
            // This is not the full Modrian table.

            switch (ee.Kind)
            {
                case EndEffectKind.Prelim:
                    return TurnType.A;

                case EndEffectKind.PeakContext:
                    return TurnType.B;

                case EndEffectKind.CandidateEnd:
                    return TurnType.C;

                case EndEffectKind.TroughContext:
                    return TurnType.C;
            }

            return TurnType.Unknown;
        }
    }
}