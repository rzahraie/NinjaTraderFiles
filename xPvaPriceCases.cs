namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaPriceCases
    {
        public static PriceCase Classify(in BarSnapshot cur, in BarSnapshot prev)
        {
            double H0 = cur.H, H1 = prev.H;
            double L0 = cur.L, L1 = prev.L;

            if (H0 > H1 && L0 > L1)
                return PriceCase.XB;

            if (H0 < H1 && L0 < L1)
                return PriceCase.XR;

            if (H0 == H1 && L0 == L1)
                return PriceCase.HITCH;

            if (H0 == H1 && L0 > L1)
                return PriceCase.FTP;

            if (H0 == H1 && L0 < L1)
                return PriceCase.STR;

            if (L0 == L1 && H0 < H1)
                return PriceCase.FBP;

            if (L0 == L1 && H0 > H1)
                return PriceCase.STB;

            if (L0 > L1 && H0 < H1)
                return PriceCase.SYM;

            if (L0 < L1 && H0 > H1)
            {
                if (cur.C > cur.O) return PriceCase.OUTB;
                if (cur.C < cur.O) return PriceCase.OUTR;
                return PriceCase.OUT_DOJI;
            }

            return PriceCase.Unknown;
        }

        public static bool IsTranslation(PriceCase pc) => pc == PriceCase.XB || pc == PriceCase.XR;
        public static bool IsInternal(PriceCase pc) =>
            pc == PriceCase.HITCH || pc == PriceCase.FTP || pc == PriceCase.FBP ||
            pc == PriceCase.SYM || pc == PriceCase.STB || pc == PriceCase.STR;
    }
}
