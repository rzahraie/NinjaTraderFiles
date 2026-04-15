using System;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public static class xPvaMath
    {
        public static bool Eq(double a, double b, double eps) => Math.Abs(a - b) <= eps;
        public static bool Gt(double a, double b, double eps) => a > b + eps;
        public static bool Lt(double a, double b, double eps) => a < b - eps;
        public static bool Ge(double a, double b, double eps) => a > b - eps;
        public static bool Le(double a, double b, double eps) => a < b + eps;

        public static double SafeDiv(double num, double den, double fallback = 0.0)
        {
            return Math.Abs(den) > 1e-12 ? num / den : fallback;
        }

        public static double Clamp01(double x)
        {
            if (x < 0.0) return 0.0;
            if (x > 1.0) return 1.0;
            return x;
        }

        public static int SignEps(double x, double eps)
        {
            if (x > eps) return 1;
            if (x < -eps) return -1;
            return 0;
        }
    }
}