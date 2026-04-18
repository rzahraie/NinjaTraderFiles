namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaPriceCases
    {
        public static PriceCase Classify(in BarSnapshot cur, in BarSnapshot prev, double tickSize)
        {
            double eps = tickSize > 0.0 ? tickSize * 0.5 : 1e-12;

            double H0 = cur.H, H1 = prev.H;
            double L0 = cur.L, L1 = prev.L;

            bool hGt = H0 > H1 + eps;
            bool hLt = H0 < H1 - eps;
            bool hEq = !hGt && !hLt;

            bool lGt = L0 > L1 + eps;
            bool lLt = L0 < L1 - eps;
            bool lEq = !lGt && !lLt;

            if (hGt && lGt)
                return PriceCase.XB;

            if (hLt && lLt)
                return PriceCase.XR;

            if (hEq && lEq)
                return PriceCase.HITCH;

            if (hEq && lGt)
                return PriceCase.FTP;

            if (hEq && lLt)
                return PriceCase.STR;

            if (lEq && hLt)
                return PriceCase.FBP;

            if (lEq && hGt)
                return PriceCase.STB;

            if (lGt && hLt)
                return PriceCase.SYM;

            if (lLt && hGt)
            {
                if (cur.C > cur.O + eps) return PriceCase.OUTB;
                if (cur.C < cur.O - eps) return PriceCase.OUTR;
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

