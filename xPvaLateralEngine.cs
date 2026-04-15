using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public sealed class xPvaLateralEngine
    {
        private readonly xPvaEngineParameters p;

        public xPvaLateralEngine(xPvaEngineParameters parameters)
        {
            p = parameters;
        }

        public xPvaLateralResult Compute(
            xPvaRuntimeState state,
            IReadOnlyList<xPvaBarFeatures> window,
            xPvaImbalanceResult imbalance,
            double tickSize)
        {
            if (window == null || window.Count < p.MinLateralBars)
                return new xPvaLateralResult(LateralStateKind.None, LateralBias.Unknown, double.NaN, double.NaN, -1, 0);

            double eps = Math.Max(tickSize * p.EpsilonTicks, 1e-12);

            int k = window.Count - p.MinLateralBars;
            double seedHigh = window[k].High;
            double seedLow = window[k].Low;

            bool contained = true;
            for (int i = k; i < window.Count; i++)
            {
                if (window[i].High > seedHigh + eps || window[i].Low < seedLow - eps)
                {
                    contained = false;
                    break;
                }
            }

            if (contained)
            {
                LateralBias bias =
                    imbalance.Imbalance >= p.LateralBiasThreshold ? LateralBias.Up :
                    imbalance.Imbalance <= -p.LateralBiasThreshold ? LateralBias.Down :
                    LateralBias.Neutral;

                return new xPvaLateralResult(
                    LateralStateKind.Active,
                    bias,
                    seedHigh,
                    seedLow,
                    window[k].BarIndex,
                    p.MinLateralBars);
            }

            xPvaBarFeatures last = window[window.Count - 1];
            if (!double.IsNaN(state.ActiveLateralHigh))
            {
                if (last.Close > state.ActiveLateralHigh + eps)
                {
                    return new xPvaLateralResult(
                        LateralStateKind.BrokenUp,
                        LateralBias.Up,
                        state.ActiveLateralHigh,
                        state.ActiveLateralLow,
                        state.ActiveLateralStartBar,
                        window.Count);
                }

                if (last.Close < state.ActiveLateralLow - eps)
                {
                    return new xPvaLateralResult(
                        LateralStateKind.BrokenDown,
                        LateralBias.Down,
                        state.ActiveLateralHigh,
                        state.ActiveLateralLow,
                        state.ActiveLateralStartBar,
                        window.Count);
                }
            }

            return new xPvaLateralResult(LateralStateKind.None, LateralBias.Unknown, double.NaN, double.NaN, -1, 0);
        }
    }
}