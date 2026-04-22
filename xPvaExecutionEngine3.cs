using System;

namespace NinjaTrader.NinjaScript.xPva.Engine2
{
    public enum xPvaExecutionReason
    {
        None = 0,

        EnterLongValid,

        ExitLongDecay,
        ExitShortDecay,

        ReverseToShortValid,
        ReverseToShortShock,

        ReverseToLongValid,
        ReverseToLongCandidateEarly,

        HoldLong,
        HoldShort,
        StandAside
    }

    public sealed class xPvaExecutionParameters3
    {
        // Flat-entry policy
        public double LongEntryScoreMin { get; set; } = 0.55;
        public bool EnableFlatShortEntry { get; set; } = false;

        // Long reversal policy while currently short
        public bool EnableLongEarlyReversal { get; set; } = true;
        public double LongEarlyReverseScoreMin { get; set; } = 0.40;
        public int LongEarlyReverseMinDegradingBars { get; set; } = 1;

        // Short reversal policy while currently long
        public bool EnableShortShockReversal { get; set; } = true;
        public bool EnableShortCandidateEarly { get; set; } = false;
        public bool EnableShortOppositePressureOverride { get; set; } = false;

        public int ExitOnDecayBars { get; set; } = 2;
    }

    public sealed class xPvaExecContext
    {
        // Position: -1 short, 0 flat, +1 long
        public int Position { get; set; }

        // Existing signal outputs
        public SignalPhase Phase { get; set; }
        public double Score { get; set; }

        // Existing runtime state
        public int DegradingBars { get; set; }
        public int OppositePressureBars { get; set; }
        public bool OppositePressureArmed { get; set; }
        public bool ShockReversalArmed { get; set; }

        // Optional context fields for future use
        //public xPvaDirectionContext DirectionContext { get; set; }
        public double RelativeVolume { get; set; }
        public double DeltaCloseOpen { get; set; }
        public double DeltaHighLow { get; set; }
    }

    public sealed class xPvaExecutionResult3
    {
        public ExecutionIntent Intent { get; }
        public xPvaExecutionReason Reason { get; }

        public xPvaExecutionResult3(ExecutionIntent intent, xPvaExecutionReason reason)
        {
            Intent = intent;
            Reason = reason;
        }

        public override string ToString()
        {
            return $"{Intent} / {Reason}";
        }
    }

    public sealed class xPvaExecutionEngine3
    {
        private readonly xPvaExecutionParameters3 p;

        public xPvaExecutionEngine3(xPvaExecutionParameters3 parameters)
        {
            p = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public xPvaExecutionResult3 Compute(xPvaExecContext ctx)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));

            switch (ctx.Position)
            {
                case 0:
                    return ComputeFlat(ctx);

                case 1:
                    return ComputeLong(ctx);

                case -1:
                    return ComputeShort(ctx);

                default:
                    return new xPvaExecutionResult3(
                        ExecutionIntent.None,
                        xPvaExecutionReason.None);
            }
        }

        private xPvaExecutionResult3 ComputeFlat(xPvaExecContext ctx)
        {
            // Flat short entry intentionally disabled by policy.
            if (ctx.Phase == SignalPhase.LongValid &&
                ctx.Score >= p.LongEntryScoreMin)
            {
                return new xPvaExecutionResult3(
                    ExecutionIntent.EnterLong,
                    xPvaExecutionReason.EnterLongValid);
            }

            if (p.EnableFlatShortEntry &&
                ctx.Phase == SignalPhase.ShortValid &&
                ctx.Score >= p.LongEntryScoreMin)
            {
                return new xPvaExecutionResult3(
                    ExecutionIntent.EnterShort,
                    xPvaExecutionReason.None);
            }

            return new xPvaExecutionResult3(
                ExecutionIntent.StandAside,
                xPvaExecutionReason.StandAside);
        }

        private xPvaExecutionResult3 ComputeLong(xPvaExecContext ctx)
        {
            // 1) Strong confirmed bearish reversal
            if (ctx.Phase == SignalPhase.ShortValid)
            {
                return new xPvaExecutionResult3(
                    ExecutionIntent.ReverseToShort,
                    xPvaExecutionReason.ReverseToShortValid);
            }

            // 2) Bearish shock reversal
            if (p.EnableShortShockReversal &&
                ctx.ShockReversalArmed)
            {
                return new xPvaExecutionResult3(
                    ExecutionIntent.ReverseToShort,
                    xPvaExecutionReason.ReverseToShortShock);
            }

            // 3) Intentionally disabled paths unless later reintroduced
            if (p.EnableShortCandidateEarly &&
                ctx.Phase == SignalPhase.ShortCandidate)
            {
                return new xPvaExecutionResult3(
                    ExecutionIntent.ReverseToShort,
                    xPvaExecutionReason.None);
            }

            if (p.EnableShortOppositePressureOverride &&
                ctx.OppositePressureArmed &&
                ctx.OppositePressureBars >= 4 &&
                ctx.DegradingBars >= 1)
            {
                return new xPvaExecutionResult3(
                    ExecutionIntent.ReverseToShort,
                    xPvaExecutionReason.None);
            }

            // 4) Damage-control exit
            if (ctx.DegradingBars >= p.ExitOnDecayBars)
            {
                return new xPvaExecutionResult3(
                    ExecutionIntent.ExitLong,
                    xPvaExecutionReason.ExitLongDecay);
            }

            return new xPvaExecutionResult3(
                ExecutionIntent.HoldLong,
                xPvaExecutionReason.HoldLong);
        }

        private xPvaExecutionResult3 ComputeShort(xPvaExecContext ctx)
        {
            // 1) Strong confirmed bullish reversal
            if (ctx.Phase == SignalPhase.LongValid)
            {
                return new xPvaExecutionResult3(
                    ExecutionIntent.ReverseToLong,
                    xPvaExecutionReason.ReverseToLongValid);
            }

            // 2) Bullish early reversal is explicitly allowed
            if (p.EnableLongEarlyReversal &&
                ctx.Phase == SignalPhase.LongCandidate &&
                ctx.Score >= p.LongEarlyReverseScoreMin &&
                ctx.DegradingBars >= p.LongEarlyReverseMinDegradingBars)
            {
                return new xPvaExecutionResult3(
                    ExecutionIntent.ReverseToLong,
                    xPvaExecutionReason.ReverseToLongCandidateEarly);
            }

            // 3) Damage-control exit
            if (ctx.DegradingBars >= p.ExitOnDecayBars)
            {
                return new xPvaExecutionResult3(
                    ExecutionIntent.ExitShort,
                    xPvaExecutionReason.ExitShortDecay);
            }

            return new xPvaExecutionResult3(
                ExecutionIntent.HoldShort,
                xPvaExecutionReason.HoldShort);
        }
    }
}

