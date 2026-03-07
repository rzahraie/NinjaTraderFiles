// APVA v0.4 — Core Stubs (BarClassifier, LateralTracker)
// -----------------------------------------------------------------------------
// These are framework-agnostic C# stubs matching the APVA v0.4 spec you approved.
// They are intentionally dependency-light so we can plug into NinjaTrader 8 later.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace APVA.Core
{
    // ==========================
    // 0) Common types & helpers
    // ==========================

    /// <summary>
    /// Two-bar classification per APVA v0.4 (mutually exclusive).
    /// Check order: OB → IB → SHSL → SH/SL → FTP/FBP → HHHL/LHLL.
    /// </summary>
    public enum TwoBarType
    {
        None = 0,
        // Translational
        HHHL,
        LHLL, // (aka “LLLH” in some notes)
        SH,
        SL,
        OB,
        // Rotational
        IB,
        FTP,
        FBP,
        SHSL,
    }

    /// <summary>
    /// Volume paint (bar body color) per v0.4 rule.
    /// </summary>
    public enum VolumeColor { Black, Red, Neutral }

    /// <summary>
    /// Container orientation.
    /// </summary>
    public enum Orientation { Up, Down }

    /// <summary>
    /// Lateral lifecycle state.
    /// </summary>
    public enum LateralState { None, Seeding, Active }

    /// <summary>
    /// Minimal bar record used by core modules.
    /// </summary>
    public sealed class Bar
    {
        public int Index { get; set; }
        public DateTime Time { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low  { get; set; }
        public double Close { get; set; }
        public long Volume { get; set; }
    }

    public static class Num
    {
        public static bool Eq(double a, double b, double eps) => Math.Abs(a - b) <= eps;
        public static bool Gt(double a, double b, double eps) => a > b + eps;
        public static bool Lt(double a, double b, double eps) => a < b - eps;
        public static bool Ge(double a, double b, double eps) => a > b - eps; // ≥ with ε
        public static bool Le(double a, double b, double eps) => a < b + eps; // ≤ with ε
    }

    // =================
    // 1) BarClassifier
    // =================

    /// <summary>
    /// Deterministic two-bar classifier & bar paint per APVA v0.4.
    /// </summary>
    public static class BarClassifier
    {
        /// <summary>
        /// Classify current bar versus previous bar using ε comparisons.
        /// </summary>
        public static TwoBarType ClassifyTwoBar(Bar prev, Bar curr, double eps)
        {
            // Guard
            if (prev == null || curr == null) return TwoBarType.None;

            // Shorthands
            var Hn  = curr.High;  var Hm1 = prev.High;
            var Ln  = curr.Low;   var Lm1 = prev.Low;

            // Ordered exclusivity per spec: OB → IB → SHSL → SH/SL → FTP/FBP → HHHL/LHLL
            if (Num.Gt(Hn, Hm1, eps) && Num.Lt(Ln, Lm1, eps)) return TwoBarType.OB;
            if (Num.Lt(Hn, Hm1, eps) && Num.Gt(Ln, Lm1, eps)) return TwoBarType.IB;
            if (Num.Eq(Hn, Hm1, eps) && Num.Eq(Ln, Lm1, eps)) return TwoBarType.SHSL;
            if (Num.Eq(Ln, Lm1, eps) && Num.Gt(Hn, Hm1, eps)) return TwoBarType.SH;  // Stitch High
            if (Num.Eq(Hn, Hm1, eps) && Num.Lt(Ln, Lm1, eps)) return TwoBarType.SL;  // Stitch Low
            if (Num.Eq(Hn, Hm1, eps) && Num.Gt(Ln, Lm1, eps)) return TwoBarType.FTP;
            if (Num.Eq(Ln, Lm1, eps) && Num.Lt(Hn, Hm1, eps)) return TwoBarType.FBP;
            if (Num.Gt(Hn, Hm1, eps) && Num.Gt(Ln, Lm1, eps)) return TwoBarType.HHHL;
            if (Num.Lt(Hn, Hm1, eps) && Num.Lt(Ln, Lm1, eps)) return TwoBarType.LHLL;

            return TwoBarType.None;
        }

        /// <summary>
        /// Determine volume paint color. If doji (Close==Open), defer to two-bar type
        /// and if rotational, inherit previous color.
        /// </summary>
        public static VolumeColor GetVolumeColor(Bar prev, Bar curr, TwoBarType currentType, VolumeColor prevColor, double eps)
        {
            if (curr == null) return VolumeColor.Neutral;

            if (Num.Gt(curr.Close, curr.Open, eps)) return VolumeColor.Black;
            if (Num.Lt(curr.Close, curr.Open, eps)) return VolumeColor.Red;

            // Doji: use two-bar semantics; rotational → inherit; translational → pick by direction of extremes
            switch (currentType)
            {
                case TwoBarType.IB:
                case TwoBarType.FTP:
                case TwoBarType.FBP:
                case TwoBarType.SHSL:
                    return prevColor; // inherit

                case TwoBarType.HHHL:
                case TwoBarType.SH:
                case TwoBarType.OB: // OB is ambiguous; bias by prior color
                    return VolumeColor.Black;

                case TwoBarType.LHLL:
                case TwoBarType.SL:
                    return VolumeColor.Red;

                default:
                    return prevColor; // safest fallback
            }
        }

        public static bool IsTranslational(TwoBarType t)
            => t == TwoBarType.HHHL || t == TwoBarType.LHLL || t == TwoBarType.SH || t == TwoBarType.SL || t == TwoBarType.OB;

        public static bool IsRotational(TwoBarType t)
            => t == TwoBarType.IB || t == TwoBarType.FTP || t == TwoBarType.FBP || t == TwoBarType.SHSL;
    }

    // =================
    // 2) LateralTracker
    // =================

    #region Lateral events
    public sealed class LateralSeededEventArgs : EventArgs
    {
        public int SeedIndex { get; set; }
        public double LatHigh { get; set; }
        public double LatLow  { get; set; }
    }

    public sealed class LateralActivatedEventArgs : EventArgs
    {
        public int SeedIndex { get; set; }
        public int ActivateIndex { get; set; }
        public double LatHigh { get; set; }
        public double LatLow  { get; set; }
    }

    public sealed class LateralEndedEventArgs : EventArgs
    {
        public int SeedIndex { get; set; }
        public int EndIndex { get; set; }
        public bool BrokeUp { get; set; }
        public bool BrokeDown { get; set; }
        public double LatHigh { get; set; }
        public double LatLow  { get; set; }
    }
    #endregion

    /// <summary>
    /// Deterministic lateral detector (parent only). Nested laterals are managed by LateralStackManager.
    /// Rules per APVA v0.4: seed at bar i; activate after ≥2 fully-contained bars; end only on full-range exit.
    /// </summary>
    public sealed class LateralTracker
    {
        public LateralState State { get; private set; } = LateralState.None;
        public int SeedIndex { get; private set; } = -1;
        public double LatHigh { get; private set; }
        public double LatLow  { get; private set; }
        public int ContainedCount { get; private set; }
        public double Eps { get; }

        // Events
        public event EventHandler<LateralSeededEventArgs> LateralSeeded;
        public event EventHandler<LateralActivatedEventArgs> LateralActivated;
        public event EventHandler<LateralEndedEventArgs> LateralEnded;

        public LateralTracker(double epsilon)
        {
            Eps = Math.Max(0, epsilon);
        }

        /// <summary>
        /// Feed a new bar. Caller must supply sequential bars.
        /// </summary>
        public void OnBar(Bar bar)
        {
            if (bar == null) return;

            switch (State)
            {
                case LateralState.None:
                    SeedIndex = bar.Index;
                    LatHigh = bar.High;
                    LatLow = bar.Low;
                    ContainedCount = 1;
                    State = LateralState.Seeding;
                    LateralSeeded?.Invoke(this, new LateralSeededEventArgs{ SeedIndex = SeedIndex, LatHigh = LatHigh, LatLow = LatLow });
                    break;

                case LateralState.Seeding:
                    if (IsContained(bar))
                    {
                        ContainedCount++;
                        if (ContainedCount >= 3)
                        {
                            State = LateralState.Active;
                            LateralActivated?.Invoke(this, new LateralActivatedEventArgs
                            {
                                SeedIndex = SeedIndex,
                                ActivateIndex = bar.Index,
                                LatHigh = LatHigh,
                                LatLow = LatLow
                            });
                        }
                    }
                    else
                    {
                        // Restart seeding at this bar
                        SeedIndex = bar.Index;
                        LatHigh = bar.High;
                        LatLow = bar.Low;
                        ContainedCount = 1;
                        // No event re-fired to avoid spam; caller can reset if needed
                    }
                    break;

                case LateralState.Active:
                    // Check full-range exit first (ends the lateral)
                    if (BreaksUp(bar))
                    {
                        End(bar, brokeUp: true, brokeDown: false);
                        // Auto-seed new lateral starting at this bar
                        SeedIndex = bar.Index;
                        LatHigh = bar.High;
                        LatLow = bar.Low;
                        ContainedCount = 1;
                        State = LateralState.Seeding;
                        LateralSeeded?.Invoke(this, new LateralSeededEventArgs{ SeedIndex = SeedIndex, LatHigh = LatHigh, LatLow = LatLow });
                    }
                    else if (BreaksDown(bar))
                    {
                        End(bar, brokeUp: false, brokeDown: true);
                        SeedIndex = bar.Index;
                        LatHigh = bar.High;
                        LatLow = bar.Low;
                        ContainedCount = 1;
                        State = LateralState.Seeding;
                        LateralSeeded?.Invoke(this, new LateralSeededEventArgs{ SeedIndex = SeedIndex, LatHigh = LatHigh, LatLow = LatLow });
                    }
                    else
                    {
                        // Still inside → maintain; optional tightening OFF by default (pure first-bar bounds)
                    }
                    break;
            }
        }

        private bool IsContained(Bar b)
            => Num.Ge(b.Low, LatLow, Eps) && Num.Le(b.High, LatHigh, Eps);

        private bool BreaksUp(Bar b)
            => Num.Gt(b.Low, LatHigh, Eps); // entire range above top

        private bool BreaksDown(Bar b)
            => Num.Lt(b.High, LatLow, Eps); // entire range below bottom

        private void End(Bar bar, bool brokeUp, bool brokeDown)
        {
            var args = new LateralEndedEventArgs
            {
                SeedIndex = SeedIndex,
                EndIndex = bar.Index,
                BrokeUp = brokeUp,
                BrokeDown = brokeDown,
                LatHigh = LatHigh,
                LatLow = LatLow
            };
            State = LateralState.None;
            SeedIndex = -1;
            ContainedCount = 0;
            LateralEnded?.Invoke(this, args);
        }
    }

    /// <summary>
    /// Manages nested laterals using a stack. Feed the same bar stream to this manager
    /// if you want automatic detection of inner laterals while a parent is active.
    /// Implementation is a stub with clear extension points.
    /// </summary>
    public sealed class LateralStackManager
    {
        private readonly double eps;
        private readonly Stack<LateralTracker> stack = new Stack<LateralTracker>();

        public IEnumerable<LateralTracker> ActiveLaterals => stack;

        public event EventHandler<LateralSeededEventArgs> AnyLateralSeeded;
        public event EventHandler<LateralActivatedEventArgs> AnyLateralActivated;
        public event EventHandler<LateralEndedEventArgs> AnyLateralEnded;

        public LateralStackManager(double epsilon)
        {
            eps = Math.Max(0, epsilon);
        }

        public void OnBar(Bar bar)
        {
            if (stack.Count == 0)
            {
                var lt = NewTracker();
                Wire(lt);
                stack.Push(lt);
            }

            // Feed the top-most tracker
            var top = stack.Peek();
            top.OnBar(bar);

            // If the top tracker just activated, consider starting a child lateral.
            // NOTE: This is a placeholder policy; a stricter detection (e.g., inner 3-bar seed inside bounds)
            // can be added based on future screenshots/rules.
            if (top.State == LateralState.Active)
            {
                // Heuristic stub: if last 3 bars were inside the last bar's range, seed a nested lateral.
                // (Caller should maintain a small ring buffer of bars if you want this here.)
                // For now, we expose extension points only.
            }

            // Pop trackers that ended (None) unless they just re-seeded internally.
            // In this stub, we keep a single active tracker and let it auto re-seed on end.
        }

        private LateralTracker NewTracker() { return new LateralTracker(eps); }

        private void Wire(LateralTracker lt)
        {
            lt.LateralSeeded += (s, e) => AnyLateralSeeded?.Invoke(s, e);
            lt.LateralActivated += (s, e) => AnyLateralActivated?.Invoke(s, e);
            lt.LateralEnded += (s, e) => AnyLateralEnded?.Invoke(s, e);
        }
    }
    // =================
    // 3) VE (Volatility Expansion) — stubs
    // =================

    public enum VeSide { Upper, Lower }

    public sealed class VeSegment
    {
        public int StartIndex { get; set; } = -1;
        public int EndIndex   { get; set; } = -1; // -1 while active
        public VeSide Side    { get; set; }
        public double OriginPrice { get; set; }
    }

    public sealed class VeStartedEventArgs : EventArgs
    {
        public VeSegment Segment { get; set; }
    }
    public sealed class VeTerminatedEventArgs : EventArgs
    {
        public VeSegment Segment { get; set; }
    }

    /// <summary>
    /// Tracks VE lifecycle for a single container per APVA v0.4.
    /// Requires boundary providers for RTL/LTL so we can test expansion-side breaches.
    /// Styling is not handled here (renderer uses container style for VE lines).
    /// </summary>
    public sealed class VeTracker
    {
        private readonly Orientation orientation;
        private readonly double eps;
        private readonly Func<int, double> getRtlAt; // y-price of RTL at bar index
        private readonly Func<int, double> getLtlAt; // y-price of LTL at bar index

        public VeSegment ActiveSegment { get; private set; } // null if none

        public event EventHandler<VeStartedEventArgs> VeStarted;
        public event EventHandler<VeTerminatedEventArgs> VeTerminated;

        public VeTracker(Orientation orientation, Func<int, double> getRtlAt, Func<int, double> getLtlAt, double epsilon)
        {
            this.orientation = orientation;
            this.getRtlAt = getRtlAt;
            this.getLtlAt = getLtlAt;
            this.eps = Math.Max(0, epsilon);
        }

        public void Reset()
        {
            if (ActiveSegment != null)
            {
                var ended = ActiveSegment; ended.EndIndex = ended.EndIndex < 0 ? ended.StartIndex : ended.EndIndex;
                VeTerminated?.Invoke(this, new VeTerminatedEventArgs { Segment = ended });
            }
            ActiveSegment = null;
        }

        /// <summary>
        /// Feed a bar and detect expansion-side breaches.
        /// Up container: breach by High/Close above LTL.
        /// Down container: breach by Low/Close below LTL.
        /// </summary>
        public void OnBar(Bar b)
        {
            if (b == null) return;
            double ltl = getLtlAt != null ? getLtlAt(b.Index) : double.NaN;

            bool breach = false; VeSide side = VeSide.Lower; double origin = double.NaN;

            if (orientation == Orientation.Up)
            {
                // Expansion on the upper side (LTL)
                breach = Num.Gt(b.High, ltl, eps) || Num.Gt(b.Close, ltl, eps);
                side = VeSide.Upper; origin = b.High;
            }
            else // Down
            {
                // Expansion on the lower side (LTL)
                breach = Num.Lt(b.Low, ltl, eps) || Num.Lt(b.Close, ltl, eps);
                side = VeSide.Lower; origin = b.Low;
            }

            if (breach)
            {
                // Terminate prior segment if any
                if (ActiveSegment != null)
                {
                    ActiveSegment.EndIndex = b.Index - 1;
                    VeTerminated?.Invoke(this, new VeTerminatedEventArgs { Segment = ActiveSegment });
                }
                // Start a new one at breach extreme
                ActiveSegment = new VeSegment
                {
                    StartIndex = b.Index,
                    EndIndex = -1,
                    Side = side,
                    OriginPrice = origin
                };
                VeStarted?.Invoke(this, new VeStartedEventArgs { Segment = ActiveSegment });
            }
        }
    }

    // =================
    // 4) TapeBuilder — stubs
    // =================

    public enum TapeState { None, Candidate, Active, Broken }

    public sealed class TapeStartedEventArgs : EventArgs
    {
        public Orientation Orientation { get; set; }
        public int P1Index { get; set; }
        public double P1Price { get; set; }
        public int P2Index { get; set; }
        public double P2Price { get; set; }
    }

    public sealed class TapeModifiedEventArgs : EventArgs
    {
        public int P3Index { get; set; }
        public double P3Price { get; set; }
    }

    public sealed class TapeBrokenEventArgs : EventArgs
    {
        public int BreakIndex { get; set; }
    }

    /// <summary>
    /// Minimal tape builder scaffold. Full geometry (RTL/LTL recomputation, opposite-tape overlaps)
    /// is left as TODO; this class exposes state & events per v0.4 so we can wire tests first.
    /// </summary>
    public sealed class TapeBuilder
    {
        private readonly double eps;
        public TapeState State { get; private set; } = TapeState.None;
        public Orientation Orientation { get; private set; }

        public int P1Index { get; private set; } = -1;
        public double P1Price { get; private set; }
        public int P2Index { get; private set; } = -1;
        public double P2Price { get; private set; }
        public int P3Index { get; private set; } = -1;
        public double P3Price { get; private set; }

        // External dependencies (to be supplied by caller):
        private readonly Func<int, double> getRtlAt; // required for break/modify tests

        public event EventHandler<TapeStartedEventArgs> TapeStarted;
        public event EventHandler<TapeModifiedEventArgs> TapeModified;
        public event EventHandler<TapeBrokenEventArgs> TapeBroken;

        public TapeBuilder(double epsilon, Func<int, double> getRtlAt)
        {
            eps = Math.Max(0, epsilon);
            this.getRtlAt = getRtlAt;
        }

        public void Reset()
        {
            State = TapeState.None; P1Index = P2Index = P3Index = -1; P1Price = P2Price = P3Price = 0;
        }

        /// <summary>
        /// Feed sequential bars with their two-bar type and prior paint.
        /// This is a scaffold; detailed tape seeding/maintenance follows the spec but is elided here.
        /// </summary>
        public void OnBar(Bar prev, Bar curr, TwoBarType t, VolumeColor prevColor)
        {
            if (curr == null) return;

            switch (State)
            {
                case TapeState.None:
                    // Seed on two consecutive translational bars of same orientation
                    // TODO: The caller should invoke this twice with consecutive bars and pass us 't'.
                    if (BarClassifier.IsTranslational(t))
                    {
                        // Orientation from extremes
                        var up = t == TwoBarType.HHHL || t == TwoBarType.SH || (t == TwoBarType.OB && curr.High > prev.High);
                        Orientation = up ? Orientation.Up : Orientation.Down;
                        // P1 at origin extreme
                        P1Index = prev.Index;
                        P1Price = up ? prev.Low : prev.High;
                        // P2 at opposite extreme on current bar (provisional)
                        P2Index = curr.Index;
                        P2Price = up ? curr.High : curr.Low;
                        State = TapeState.Candidate;
                        TapeStarted?.Invoke(this, new TapeStartedEventArgs { Orientation = Orientation, P1Index = P1Index, P1Price = P1Price, P2Index = P2Index, P2Price = P2Price });
                    }
                    break;

                case TapeState.Candidate:
                case TapeState.Active:
                    // Break test: close through RTL
                    if (getRtlAt != null)
                    {
                        double rtl = getRtlAt(curr.Index);
                        bool breakCond = (Orientation == Orientation.Up) ? Num.Lt(curr.Close, rtl, eps)
                                                                         : Num.Gt(curr.Close, rtl, eps);
                        if (breakCond)
                        {
                            State = TapeState.Broken;
                            TapeBroken?.Invoke(this, new TapeBrokenEventArgs { BreakIndex = curr.Index });
                            return;
                        }
                    }

                    // Modify test: wick through RTL but close inside → re-point P3
                    if (getRtlAt != null)
                    {
                        double rtl = getRtlAt(curr.Index);
                        bool wickTouch = (Orientation == Orientation.Up)
                            ? (Num.Lt(curr.Low, rtl, eps) && Num.Ge(curr.Close, rtl, eps))
                            : (Num.Gt(curr.High, rtl, eps) && Num.Le(curr.Close, rtl, eps));
                        if (wickTouch)
                        {
                            P3Index = curr.Index;
                            P3Price = (Orientation == Orientation.Up) ? curr.Low : curr.High;
                            State = TapeState.Active;
                            TapeModified?.Invoke(this, new TapeModifiedEventArgs { P3Index = P3Index, P3Price = P3Price });
                            return;
                        }
                    }

                    // Else: maintain. In a full impl we’d extend P2 when making progress, etc.
                    break;
            }
        }
    }

    // =================
    // 5) TraverseAssembler — stubs
    // =================

    public enum TraverseState { None, PatternPending, JoinPending, ActiveAwaitP3, Active, Ftt }

    public sealed class TapeSummary
    {
        public Orientation Orientation { get; set; }
        public int StartIndex { get; set; } = -1;
        public int EndIndex   { get; set; } = -1;
        public double MaxHigh { get; set; }
        public double MinLow  { get; set; }
    }

    public sealed class TraverseStartedEventArgs : EventArgs
    {
        public Orientation Orientation { get; set; }
        public TapeSummary T1 { get; set; }
        public TapeSummary T2 { get; set; }
        public TapeSummary T3 { get; set; }
        public int P1Index { get; set; }
        public double P1Price { get; set; }
        public int P2Index { get; set; }
        public double P2Price { get; set; }
    }

    public sealed class TraverseJoinedEventArgs : EventArgs
    {
        public int JoinIndex { get; set; }
        public double PivotExtreme { get; set; }
    }

    public sealed class TraverseP3EventArgs : EventArgs
    {
        public int P3Index { get; set; }
        public double P3Price { get; set; }
    }

    public sealed class TraverseFttEventArgs : EventArgs
    {
        public int FttIndex { get; set; }
    }

    /// <summary>
    /// Assembles traverses from completed tapes.
    /// Pattern: U–D–U (Up) or D–U–D (Down). Join confirmation requires third tape to touch the pivot of the middle tape.
    /// P1 at T1 origin extreme; P2 at opposite extreme before T2 completes; P3 at first touch of traverse RTL during T3.
    /// </summary>
    public sealed class TraverseAssembler
    {
        private readonly double eps;
        private readonly Func<int, double> getRtlAt; // traverse RTL at bar index
        private readonly Func<int, double> getLtlAt; // traverse LTL at bar index

        private readonly List<TapeSummary> window = new List<TapeSummary>(3);

        public TraverseState State { get; private set; } = TraverseState.None;
        public Orientation Orientation { get; private set; }

        public int P1Index { get; private set; } = -1;
        public double P1Price { get; private set; }
        public int P2Index { get; private set; } = -1;
        public double P2Price { get; private set; }
        public int P3Index { get; private set; } = -1;
        public double P3Price { get; private set; }

        private bool touchedOppositeSinceP3 = false;

        public event EventHandler<TraverseStartedEventArgs> TraverseStarted;
        public event EventHandler<TraverseJoinedEventArgs> TraverseJoined;
        public event EventHandler<TraverseP3EventArgs> TraverseP3;
        public event EventHandler<TraverseFttEventArgs> TraverseFtt;

        public TraverseAssembler(double epsilon, Func<int, double> getRtlAt, Func<int, double> getLtlAt)
        {
            eps = Math.Max(0, epsilon);
            this.getRtlAt = getRtlAt;
            this.getLtlAt = getLtlAt;
        }

        public void Reset()
        {
            window.Clear();
            State = TraverseState.None;
            P1Index = P2Index = P3Index = -1;
            P1Price = P2Price = P3Price = 0;
            touchedOppositeSinceP3 = false;
        }

        /// <summary>
        /// Add a completed tape summary in chronological order.
        /// Caller should fill StartIndex/EndIndex and MinLow/MaxHigh.
        /// </summary>
        public void AddTape(TapeSummary t)
        {
            if (t == null) return;

            // Maintain rolling window of last 3 tapes
            if (window.Count == 3) window.RemoveAt(0);
            window.Add(t);

            if (window.Count < 3)
            {
                State = TraverseState.PatternPending;
                return;
            }

            var t1 = window[0];
            var t2 = window[1];
            var t3 = window[2];

            // Determine pattern
            bool upPattern = t1.Orientation == Orientation.Up && t2.Orientation == Orientation.Down && t3.Orientation == Orientation.Up;
            bool dnPattern = t1.Orientation == Orientation.Down && t2.Orientation == Orientation.Up && t3.Orientation == Orientation.Down;

            if (!(upPattern || dnPattern))
            {
                State = TraverseState.PatternPending;
                return;
            }

            Orientation = upPattern ? Orientation.Up : Orientation.Down;
            State = TraverseState.JoinPending;

            // Check join confirmation
            bool joined = false; double pivotExtreme = double.NaN; int joinIndex = t3.EndIndex >= 0 ? t3.EndIndex : t3.StartIndex;
            if (Orientation == Orientation.Down)
            {
                // third down tape must touch/undercut middle up tape's pivot low
                pivotExtreme = t2.MinLow;
                if (Num.Le(t3.MinLow, pivotExtreme, eps)) joined = true;
            }
            else
            {
                // third up tape must touch/exceed middle down tape's pivot high
                pivotExtreme = t2.MaxHigh;
                if (Num.Ge(t3.MaxHigh, pivotExtreme, eps)) joined = true;
            }

            if (!joined) return; // remain JoinPending until a later revised summary meets pivot, or caller re-adds a refined t3

            // Set P1 and P2
            if (Orientation == Orientation.Down)
            {
                P1Index = t1.StartIndex; P1Price = t1.MaxHigh; // origin extreme high
                P2Index = t1.EndIndex;   P2Price = t1.MinLow;  // lowest low before T2 completes
            }
            else
            {
                P1Index = t1.StartIndex; P1Price = t1.MinLow;  // origin extreme low
                P2Index = t1.EndIndex;   P2Price = t1.MaxHigh; // highest high before T2 completes
            }

            State = TraverseState.ActiveAwaitP3;
            TraverseStarted?.Invoke(this, new TraverseStartedEventArgs { Orientation = Orientation, T1 = t1, T2 = t2, T3 = t3, P1Index = P1Index, P1Price = P1Price, P2Index = P2Index, P2Price = P2Price });
            TraverseJoined?.Invoke(this, new TraverseJoinedEventArgs { JoinIndex = joinIndex, PivotExtreme = pivotExtreme });
        }

        /// <summary>
        /// Feed bars of the third tape to detect P3 touch and FTT per v0.4.
        /// Requires getRtlAt/getLtlAt to be wired to the current traverse geometry.
        /// </summary>
        public void OnBar(Bar b)
        {
            if (b == null || (State != TraverseState.ActiveAwaitP3 && State != TraverseState.Active)) return;

            double rtl = getRtlAt != null ? getRtlAt(b.Index) : double.NaN;
            double ltl = getLtlAt != null ? getLtlAt(b.Index) : double.NaN;

            if (State == TraverseState.ActiveAwaitP3)
            {
                bool touch = (Orientation == Orientation.Down)
                    ? Num.Ge(b.High, rtl, eps)
                    : Num.Le(b.Low, rtl, eps);

                if (touch)
                {
                    P3Index = b.Index; P3Price = (Orientation == Orientation.Down) ? b.High : b.Low;
                    State = TraverseState.Active;
                    TraverseP3?.Invoke(this, new TraverseP3EventArgs { P3Index = P3Index, P3Price = P3Price });
                    return;
                }
            }

            // Track opposite boundary touch after P3 (invalidates FTT eligibility)
            if (P3Index >= 0 && getLtlAt != null)
            {
                bool oppTouch = (Orientation == Orientation.Down)
                    ? Num.Le(b.Low, ltl, eps)
                    : Num.Ge(b.High, ltl, eps);
                if (oppTouch) touchedOppositeSinceP3 = true;
            }

            // FTT: close beyond RTL before any opposite touch
            if (!touchedOppositeSinceP3 && getRtlAt != null)
            {
                bool closeBeyond = (Orientation == Orientation.Down)
                    ? Num.Gt(b.Close, rtl, eps)
                    : Num.Lt(b.Close, rtl, eps);

                if (closeBeyond)
                {
                    State = TraverseState.Ftt;
                    TraverseFtt?.Invoke(this, new TraverseFttEventArgs { FttIndex = b.Index });
                }
            }
        }
    }

    // =================
    // 6) ChannelAssembler — stubs
    // =================

    public enum ChannelState { None, PatternPending, JoinPending, ActiveAwaitP3, Active, Ftt }

    public sealed class TraverseSummary
    {
        public Orientation Orientation { get; set; }
        public int StartIndex { get; set; } = -1;
        public int EndIndex   { get; set; } = -1;
        public double MaxHigh { get; set; }
        public double MinLow  { get; set; }
    }

    public sealed class ChannelStartedEventArgs : EventArgs
    {
        public Orientation Orientation { get; set; }
        public TraverseSummary T1 { get; set; }
        public TraverseSummary T2 { get; set; }
        public TraverseSummary T3 { get; set; }
        public int P1Index { get; set; }
        public double P1Price { get; set; }
        public int P2Index { get; set; }
        public double P2Price { get; set; }
    }

    public sealed class ChannelJoinedEventArgs : EventArgs
    {
        public int JoinIndex { get; set; }
        public double PivotExtreme { get; set; }
    }

    public sealed class ChannelP3EventArgs : EventArgs
    {
        public int P3Index { get; set; }
        public double P3Price { get; set; }
    }

    public sealed class ChannelFttEventArgs : EventArgs
    {
        public int FttIndex { get; set; }
    }

    /// <summary>
    /// Assembles channels from completed traverses.
    /// Pattern: Up traverse, Down traverse, Up traverse (Up channel), or symmetric for Down.
    /// Join confirmation requires third traverse to make progress beyond the first traverse's extreme by ε.
    /// P1 at T1 origin extreme; P2 at opposite extreme before T2 completes; P3 at first touch of channel RTL during T3.
    /// </summary>
    public sealed class ChannelAssembler
    {
        private readonly double eps;
        private readonly Func<int, double> getRtlAt; // channel RTL at bar index
        private readonly Func<int, double> getLtlAt; // channel LTL at bar index

        private readonly List<TraverseSummary> window = new List<TraverseSummary>(3);

        public ChannelState State { get; private set; } = ChannelState.None;
        public Orientation Orientation { get; private set; }

        public int P1Index { get; private set; } = -1;
        public double P1Price { get; private set; }
        public int P2Index { get; private set; } = -1;
        public double P2Price { get; private set; }
        public int P3Index { get; private set; } = -1;
        public double P3Price { get; private set; }

        private bool touchedOppositeSinceP3 = false;

        public event EventHandler<ChannelStartedEventArgs> ChannelStarted;
        public event EventHandler<ChannelJoinedEventArgs> ChannelJoined;
        public event EventHandler<ChannelP3EventArgs> ChannelP3;
        public event EventHandler<ChannelFttEventArgs> ChannelFtt;

        public ChannelAssembler(double epsilon, Func<int, double> getRtlAt, Func<int, double> getLtlAt)
        {
            eps = Math.Max(0, epsilon);
            this.getRtlAt = getRtlAt;
            this.getLtlAt = getLtlAt;
        }

        public void Reset()
        {
            window.Clear();
            State = ChannelState.None;
            P1Index = P2Index = P3Index = -1;
            P1Price = P2Price = P3Price = 0;
            touchedOppositeSinceP3 = false;
        }

        /// <summary>
        /// Add a completed traverse summary in chronological order.
        /// Caller should fill StartIndex/EndIndex and MinLow/MaxHigh.
        /// </summary>
        public void AddTraverse(TraverseSummary t)
        {
            if (t == null) return;

            if (window.Count == 3) window.RemoveAt(0);
            window.Add(t);

            if (window.Count < 3)
            {
                State = ChannelState.PatternPending;
                return;
            }

            var t1 = window[0];
            var t2 = window[1];
            var t3 = window[2];

            bool upPattern = t1.Orientation == Orientation.Up && t2.Orientation == Orientation.Down && t3.Orientation == Orientation.Up;
            bool dnPattern = t1.Orientation == Orientation.Down && t2.Orientation == Orientation.Up && t3.Orientation == Orientation.Down;

            if (!(upPattern || dnPattern))
            {
                State = ChannelState.PatternPending;
                return;
            }

            Orientation = upPattern ? Orientation.Up : Orientation.Down;
            State = ChannelState.JoinPending;

            bool joined = false; double pivotExtreme = double.NaN; int joinIndex = t3.EndIndex >= 0 ? t3.EndIndex : t3.StartIndex;
            if (Orientation == Orientation.Down)
            {
                // third down traverse must make progress beyond first traverse's extreme low
                var firstExtreme = t1.MinLow;
                pivotExtreme = firstExtreme;
                if (Num.Lt(t3.MinLow, firstExtreme, eps)) joined = true; // progress beyond
            }
            else
            {
                // third up traverse must make progress beyond first traverse's extreme high
                var firstExtreme = t1.MaxHigh;
                pivotExtreme = firstExtreme;
                if (Num.Gt(t3.MaxHigh, firstExtreme, eps)) joined = true; // progress beyond
            }

            if (!joined) return; // remain JoinPending until progress occurs

            // Set P1 and P2
            if (Orientation == Orientation.Down)
            {
                P1Index = t1.StartIndex; P1Price = t1.MaxHigh; // origin high
                P2Index = t1.EndIndex;   P2Price = t1.MinLow;  // opposite extreme before T2 completes
            }
            else
            {
                P1Index = t1.StartIndex; P1Price = t1.MinLow;  // origin low
                P2Index = t1.EndIndex;   P2Price = t1.MaxHigh; // opposite extreme before T2 completes
            }

            State = ChannelState.ActiveAwaitP3;
            ChannelStarted?.Invoke(this, new ChannelStartedEventArgs { Orientation = Orientation, T1 = t1, T2 = t2, T3 = t3, P1Index = P1Index, P1Price = P1Price, P2Index = P2Index, P2Price = P2Price });
            ChannelJoined?.Invoke(this, new ChannelJoinedEventArgs { JoinIndex = joinIndex, PivotExtreme = pivotExtreme });
        }

        /// <summary>
        /// Feed bars of the third traverse to detect P3 touch and FTT per v0.4.
        /// Requires getRtlAt/getLtlAt to be wired to the current channel geometry.
        /// </summary>
        public void OnBar(Bar b)
        {
            if (b == null || (State != ChannelState.ActiveAwaitP3 && State != ChannelState.Active)) return;

            double rtl = getRtlAt != null ? getRtlAt(b.Index) : double.NaN;
            double ltl = getLtlAt != null ? getLtlAt(b.Index) : double.NaN;

            if (State == ChannelState.ActiveAwaitP3)
            {
                bool touch = (Orientation == Orientation.Down)
                    ? Num.Ge(b.High, rtl, eps)
                    : Num.Le(b.Low, rtl, eps);

                if (touch)
                {
                    P3Index = b.Index; P3Price = (Orientation == Orientation.Down) ? b.High : b.Low;
                    State = ChannelState.Active;
                    ChannelP3?.Invoke(this, new ChannelP3EventArgs { P3Index = P3Index, P3Price = P3Price });
                    return;
                }
            }

            if (P3Index >= 0 && getLtlAt != null)
            {
                bool oppTouch = (Orientation == Orientation.Down)
                    ? Num.Le(b.Low, ltl, eps)
                    : Num.Ge(b.High, ltl, eps);
                if (oppTouch) touchedOppositeSinceP3 = true;
            }

            // FTT: close beyond RTL before opposite touch
            if (!touchedOppositeSinceP3 && getRtlAt != null)
            {
                bool closeBeyond = (Orientation == Orientation.Down)
                    ? Num.Gt(b.Close, rtl, eps)
                    : Num.Lt(b.Close, rtl, eps);

                if (closeBeyond)
                {
                    State = ChannelState.Ftt;
                    ChannelFtt?.Invoke(this, new ChannelFttEventArgs { FttIndex = b.Index });
                }
            }
        }
    }

    // =================
    // 6) ChannelAssembler — stubs
    // =================

  

    // =================
    // 7) VolumeCycleEngine — stubs (NT8-friendly)
    // =================

    public enum VcLeg { None, Dom1, NonDom, Dom2 }

    public sealed class VolumeCycleState
    {
        public Orientation Orientation { get; set; }
        public VcLeg CurrentLeg { get; set; } = VcLeg.None;
        public long Peak1 { get; set; } // dominant leg 1 peak
        public long Peak2 { get; set; } // dominant leg 2 peak
        public double ParityRatio { get; set; } // Peak2/Peak1 when available
        public bool Truncated { get; set; }
        public double VRC { get; set; }
        public double BVC { get; set; }
        public bool AbsorptionFlag { get; set; }
        public bool EfficientThrustFlag { get; set; }
    }

    public sealed class RollingMedian
    {
        private readonly int window;
        private readonly Queue<double> q = new Queue<double>();
        private readonly SortedDictionary<double,int> map = new SortedDictionary<double,int>();
        private int count = 0;

        public RollingMedian(int window)
        {
            this.window = Math.Max(1, window);
        }

        public void Add(double v)
        {
            q.Enqueue(v);
            int c; if (!map.TryGetValue(v, out c)) c = 0; map[v] = c + 1;
            count++;
            if (count > window)
            {
                var old = q.Dequeue();
                int co; if (map.TryGetValue(old, out co)) { if (co <= 1) map.Remove(old); else map[old] = co - 1; }
                count = window;
            }
        }

        public double Median()
        {
            if (count == 0) return double.NaN;
            int target = (count - 1) / 2; // lower median for even counts
            int acc = 0;
            foreach (var kv in map)
            {
                acc += kv.Value;
                if (acc > target) return kv.Key;
            }
            foreach (var kv in map) return kv.Key;
            return double.NaN;
        }
    }

    public sealed class RollingStats
    {
        private readonly int window;
        private readonly Queue<double> q = new Queue<double>();
        private double sum = 0.0;
        private double sumSq = 0.0;
        private double max = double.MinValue;

        public RollingStats(int window)
        {
            this.window = Math.Max(1, window);
        }

        public void Add(double v)
        {
            q.Enqueue(v);
            sum += v; sumSq += v * v;
            if (v > max) max = v;
            if (q.Count > window)
            {
                var old = q.Dequeue();
                sum -= old; sumSq -= old * old;
                if (old >= max) { // recompute max lazily
                    max = double.MinValue; foreach (var x in q) if (x > max) max = x;
                }
            }
        }
        public int Count { get { return q.Count; } }
        public double Mean { get { return Count == 0 ? double.NaN : sum / Count; } }
        public double StdDev
        {
            get
            {
                if (Count == 0) return double.NaN;
                double mean = sum / Count;
                double var = Math.Max(0.0, sumSq / Count - mean * mean);
                return Math.Sqrt(var);
            }
        }
        public double Max { get { return q.Count == 0 ? double.NaN : max; } }
    }

    public sealed class VolumeCycleEngine
    {
        private readonly int normWindow;
        private readonly double eps;
        private readonly RollingMedian medVol;
        private readonly RollingMedian medRange;
        private readonly RollingMedian medBody;
        private readonly RollingStats volStats;

        // simple monotonic run counters for phase switching
        private int incRun = 0;
        private int decRun = 0;
        private long lastVol = -1;

        public VolumeCycleState State { get; private set; } = new VolumeCycleState();

        public VolumeCycleEngine(int normalizationWindow, double epsilon)
        {
            normWindow = Math.Max(5, normalizationWindow);
            eps = Math.Max(0, epsilon);
            medVol = new RollingMedian(normWindow);
            medRange = new RollingMedian(normWindow);
            medBody = new RollingMedian(normWindow);
            volStats = new RollingStats(20);
        }

        public void Reset(Orientation orientation)
        {
            State = new VolumeCycleState { Orientation = orientation };
            incRun = decRun = 0; lastVol = -1;
        }

        /// <summary>
        /// Feed per-bar and update cycle phase + diagnostics. Range = High-Low; Body = |Close-Open|.
        /// </summary>
        public void OnBar(Orientation containerOrientation, Bar prev, Bar curr)
        {
            if (curr == null) return;
            if (State.Orientation != containerOrientation) State.Orientation = containerOrientation;

            double range = curr.High - curr.Low;
            double body = Math.Abs(curr.Close - curr.Open);
            double vol = curr.Volume;

            medVol.Add(vol); medRange.Add(range); medBody.Add(body);
            volStats.Add(vol);

            double vMed = medVol.Median();
            double rMed = medRange.Median();
            double bMed = medBody.Median();

            double Vn = (vMed > 0) ? vol / vMed : 1.0;
            double Rn = (rMed > 0) ? range / rMed : 1.0;
            double Bn = (bMed > 0) ? (body > 0 ? body / bMed : 0.000001) : 1.0;

            State.VRC = (Rn > 0) ? Vn / Rn : 1.0;
            State.BVC = (Bn > 0) ? Vn / Bn : 1.0;
            State.AbsorptionFlag = (State.VRC > 1.5) || (State.BVC > 2.0);
            State.EfficientThrustFlag = (State.VRC < 0.7);

            // Phase progression: oriented labels; bar-to-bar monotonic runs guide switching
            if (State.CurrentLeg == VcLeg.None) State.CurrentLeg = VcLeg.Dom1;

            // update runs
            if (lastVol < 0) { lastVol = curr.Volume; }
            else
            {
                if (curr.Volume > lastVol + eps) { incRun++; decRun = 0; }
                else if (curr.Volume < lastVol - eps) { decRun++; incRun = 0; }
                else { /* tie → no change */ }
                lastVol = curr.Volume;
            }

            if (State.CurrentLeg == VcLeg.Dom1)
            {
                if (curr.Volume > State.Peak1) State.Peak1 = curr.Volume;
                // switch to NonDom on 2 consecutive decreases
                if (decRun >= 2) { State.CurrentLeg = VcLeg.NonDom; incRun = decRun = 0; }
            }
            else if (State.CurrentLeg == VcLeg.NonDom)
            {
                // switch to Dom2 on 2 consecutive increases
                if (incRun >= 2) { State.CurrentLeg = VcLeg.Dom2; incRun = decRun = 0; }
            }
            else if (State.CurrentLeg == VcLeg.Dom2)
            {
                if (curr.Volume > State.Peak2) State.Peak2 = curr.Volume;
            }

            // update parity
            if (State.Peak1 > 0 && State.Peak2 > 0) State.ParityRatio = (double)State.Peak2 / (double)State.Peak1;
        }

        public void MarkTruncated()
        {
            State.Truncated = true;
        }
    }

    // =================
    // 8) DominanceEngine — stubs
    // =================

    public enum DominanceLabel { NonDom, Dom }

    public static class DominanceEngine
    {
        /// <summary>
        /// Resolve dominance for the current leg: geometry default can be overridden by VC phase and diagnostics.
        /// geometryDom: true if geometric leg is dominant (P1→P2 or P3→FTT), false if non-dominant (P2→P3).
        /// </summary>
        public static DominanceLabel Resolve(bool geometryDom, VolumeCycleState vc)
        {
            if (vc == null) return geometryDom ? DominanceLabel.Dom : DominanceLabel.NonDom;

            // If we are in NonDom VC leg, prefer NonDom unless Absorption suggests ineffective thrust.
            if (vc.CurrentLeg == VcLeg.NonDom) return DominanceLabel.NonDom;

            // If VC says Dom (Dom1 or Dom2) but absorption is high, we can downgrade to NonDom if desired.
            if ((vc.CurrentLeg == VcLeg.Dom1 || vc.CurrentLeg == VcLeg.Dom2) && vc.AbsorptionFlag)
            {
                // keep Dom but mark as weak elsewhere; for now we won't flip here.
                return geometryDom ? DominanceLabel.Dom : DominanceLabel.NonDom;
            }

            // Default to geometry
            return geometryDom ? DominanceLabel.Dom : DominanceLabel.NonDom;
        }
    }

    // =================
    // 9) FTTDetector — stubs
    // =================

    public enum FttState { None, Candidate, Confirmed, Invalidated }

    public sealed class FttEventArgs : EventArgs
    {
        public int Index { get; set; }
        public string Reason { get; set; }
    }

    public sealed class FttDetector
    {
        private readonly double eps;
        private readonly double closeNearExtremeThreshold; // e.g., 0.6
        private readonly RollingStats volStats = new RollingStats(20);

        public FttState State { get; private set; } = FttState.None;
        public int CandidateIndex { get; private set; } = -1;
        public double CandidateHigh { get; private set; }
        public double CandidateLow  { get; private set; }
        public Orientation ContainerOrientation { get; private set; }

        private readonly Func<int, double> getRtlAt; // container RTL provider

        public event EventHandler<FttEventArgs> FttCandidate;
        public event EventHandler<FttEventArgs> FttConfirmed;
        public event EventHandler<FttEventArgs> FttInvalidated;

        public FttDetector(double epsilon, double closeNearExtremeThreshold, Func<int, double> getRtlAt)
        {
            eps = Math.Max(0, epsilon);
            this.closeNearExtremeThreshold = Math.Max(0.0, Math.Min(1.0, closeNearExtremeThreshold));
            this.getRtlAt = getRtlAt;
        }

        public void Reset(Orientation orientation)
        {
            State = FttState.None; CandidateIndex = -1; ContainerOrientation = orientation;
        }

        /// <summary>
        /// Feed sequential bars. TwoBarType is used for translational/rotational checks.
        /// </summary>
        public void OnBar(Bar prev, Bar curr, TwoBarType t, Orientation containerOrientation, bool lateralSeededThisBar)
        {
            if (curr == null) return;
            if (ContainerOrientation != containerOrientation) ContainerOrientation = containerOrientation;

            // maintain rolling volume stats
            volStats.Add(curr.Volume);
            double mean = volStats.Mean; double sd = volStats.StdDev; double max = volStats.Max;

            // --- Candidate conditions ---
            bool candidate = false; string reason = null;

            // (1) Translational by extremes but closes opposite with close near extreme
            bool closesUp = Num.Gt(curr.Close, curr.Open, eps);
            bool closesDown = Num.Lt(curr.Close, curr.Open, eps);
            double posInRange = (curr.High > curr.Low + eps) ? (curr.Close - curr.Low) / (curr.High - curr.Low) : 0.5;

            if (BarClassifier.IsTranslational(t))
            {
                if (containerOrientation == Orientation.Down && closesUp && posInRange >= closeNearExtremeThreshold)
                {
                    candidate = true; reason = "Opposite close near high within down container";
                }
                else if (containerOrientation == Orientation.Up && closesDown && (1.0 - posInRange) >= closeNearExtremeThreshold)
                {
                    candidate = true; reason = "Opposite close near low within up container";
                }
            }

            // (2) Extreme opposite volume
            bool extremeOppVol = false;
            if (sd == sd) // not NaN
            {
                if (curr.Volume >= max || (sd > 0 && curr.Volume >= mean + 2.0 * sd)) extremeOppVol = true;
            }
            if (!candidate && extremeOppVol)
            {
                candidate = true; reason = "Extreme opposite volume";
            }

            // (3) Lateral seeding at reversal bar
            if (!candidate && lateralSeededThisBar)
            {
                candidate = true; reason = "Lateral seed at reversal";
            }

            if (candidate && State == FttState.None)
            {
                State = FttState.Candidate; CandidateIndex = curr.Index; CandidateHigh = curr.High; CandidateLow = curr.Low;
                FttCandidate?.Invoke(this, new FttEventArgs { Index = curr.Index, Reason = reason });
                return;
            }

            // --- Confirmation condition: close beyond RTL opposite to container ---
            if (State == FttState.Candidate && getRtlAt != null)
            {
                double rtl = getRtlAt(curr.Index);
                bool closeBeyond = (containerOrientation == Orientation.Down)
                    ? Num.Gt(curr.Close, rtl, eps)
                    : Num.Lt(curr.Close, rtl, eps);
                if (closeBeyond)
                {
                    State = FttState.Confirmed;
                    FttConfirmed?.Invoke(this, new FttEventArgs { Index = curr.Index, Reason = "Close beyond RTL" });
                    return;
                }
            }

            // --- Invalidation: new progress in original direction on expanding dominant volume ---
            if (State == FttState.Candidate && prev != null)
            {
                bool newProg = (containerOrientation == Orientation.Down) ? Num.Lt(curr.Low, prev.Low, eps)
                                                                          : Num.Gt(curr.High, prev.High, eps);
                bool volExp = curr.Volume > prev.Volume + eps;
                if (newProg && volExp)
                {
                    State = FttState.Invalidated;
                    FttInvalidated?.Invoke(this, new FttEventArgs { Index = curr.Index, Reason = "Continuation with expanding volume" });
                }
            }
        }
    }
	
	// =================
    // 11) GeometryService — container RTL/LTL utilities (NT8-safe)
    // =================

    // Simple immutable line in (index, price) space
    public sealed class Line2D
    {
        public int X1;
        public int X2;
        public double Y1;
        public double Y2;
        public double Slope;
        public double Intercept;

        public Line2D(int x1, double y1, int x2, double y2)
        {
            X1 = x1; Y1 = y1; X2 = x2; Y2 = y2;
            double dx = (double)(x2 - x1);
            Slope = Math.Abs(dx) > 1e-12 ? (y2 - y1) / dx : 0.0;
            Intercept = y1 - Slope * x1;
        }

        public double ValueAt(int x)
        {
            return Slope * x + Intercept;
        }
    }

    // Pair of RTL (trend-facing) and LTL for a container
    public sealed class ContainerGeometry
    {
        public Orientation Orientation;
        public Line2D RTL; // lower for Up, upper for Down
        public Line2D LTL;

        public ContainerGeometry(Orientation orientation, Line2D rtl, Line2D ltl)
        {
            Orientation = orientation; RTL = rtl; LTL = ltl;
        }

        public double RtlAt(int index) { return RTL != null ? RTL.ValueAt(index) : double.NaN; }
        public double LtlAt(int index) { return LTL != null ? LTL.ValueAt(index) : double.NaN; }

        // Rebuild RTL using P1 and P3 touch; LTL unchanged
        public ContainerGeometry WithP3(int p1Index, double p1Extreme, int p3Index, double p3Extreme)
        {
            Line2D newRtl = new Line2D(p1Index, p1Extreme, p3Index, p3Extreme);
            return new ContainerGeometry(this.Orientation, newRtl, this.LTL);
        }
    }

    public static class GeometryService
    {
        // Build tape geometry: Up → RTL=low-low, LTL=high-high; Down → RTL=high-high, LTL=low-low
        public static ContainerGeometry BuildTape(Orientation orientation, Bar p1, Bar p2)
        {
            if (p1 == null || p2 == null) return null;
            Line2D rtl, ltl;
            if (orientation == Orientation.Up)
            {
                rtl = new Line2D(p1.Index, p1.Low,  p2.Index, p2.Low);
                ltl = new Line2D(p1.Index, p1.High, p2.Index, p2.High);
            }
            else
            {
                rtl = new Line2D(p1.Index, p1.High, p2.Index, p2.High);
                ltl = new Line2D(p1.Index, p1.Low,  p2.Index, p2.Low);
            }
            return new ContainerGeometry(orientation, rtl, ltl);
        }

        // Build generic container from explicit anchors
        public static ContainerGeometry BuildGeneric(Orientation orientation,
                                                     int rtlX1, double rtlY1, int rtlX2, double rtlY2,
                                                     int ltlX1, double ltlY1, int ltlX2, double ltlY2)
        {
            Line2D rtl = new Line2D(rtlX1, rtlY1, rtlX2, rtlY2);
            Line2D ltl = new Line2D(ltlX1, ltlY1, ltlX2, ltlY2);
            return new ContainerGeometry(orientation, rtl, ltl);
        }

        // Delegates for builders (avoid fancy syntax)
        public static Func<int, double> MakeRtlDelegate(ContainerGeometry g)
        {
            if (g == null) return null;
            return delegate (int idx) { return g.RtlAt(idx); };
        }
        public static Func<int, double> MakeLtlDelegate(ContainerGeometry g)
        {
            if (g == null) return null;
            return delegate (int idx) { return g.LtlAt(idx); };
        }

        // Wick-touch test (ε-aware)
        public static bool WickTouchesRtl(Orientation orientation, Bar b, double rtl, double eps)
        {
            if (b == null) return false;
            if (orientation == Orientation.Up)
                return Num.Lt(b.Low, rtl, eps) && Num.Ge(b.Close, rtl, eps);
            else
                return Num.Gt(b.High, rtl, eps) && Num.Le(b.Close, rtl, eps);
        }

        // Close-beyond-RTL test (ε-aware)
        public static bool CloseBeyondRtl(Orientation orientation, Bar b, double rtl, double eps)
        {
            if (b == null) return false;
            if (orientation == Orientation.Up)
                return Num.Lt(b.Close, rtl, eps);
            else
                return Num.Gt(b.Close, rtl, eps);
        }
    }
	
	// =================
	// 12) TapeGeometryBridge — geometry-backed RTL for TapeBuilder
	// =================
	public sealed class TapeGeometryUpdatedEventArgs : System.EventArgs
    {
        public ContainerGeometry Geometry { get; set; }
    }

    /// <summary>
    /// Bridges TapeBuilder with GeometryService so RTL/LTL are real lines instead of constants.
    /// Requires a bar accessor so we can build P1–P2 tape lines at start.
    /// </summary>
    public sealed class TapeGeometryBridge
    {
        private readonly double eps;
        private readonly System.Func<int, Bar> getBarByIndex;

        private readonly TapeBuilder tape;
        private ContainerGeometry geom;

        // cache P1 so we can rebuild RTL at P3
        private int p1Index = -1;
        private double p1Extreme = double.NaN;
        private Orientation orientation;

        // Expose orientation & geometry for consumers
        public Orientation Orientation { get { return orientation; } }
        public ContainerGeometry Geometry { get { return geom; } }

        // Re-emit TapeBuilder events
        public event System.EventHandler<TapeStartedEventArgs> TapeStarted;
        public event System.EventHandler<TapeModifiedEventArgs> TapeModified;
        public event System.EventHandler<TapeBrokenEventArgs>  TapeBroken;

        // New: geometry updates
        public event System.EventHandler<TapeGeometryUpdatedEventArgs> GeometryUpdated;

        public TapeGeometryBridge(double epsilon, System.Func<int, Bar> getBarByIndex)
        {
            this.eps = System.Math.Max(0, epsilon);
            this.getBarByIndex = getBarByIndex;

            // Feed TapeBuilder an RTL delegate that always reflects current geometry
            this.tape = new TapeBuilder(this.eps, RtlAt);

            // Wire through events and maintain geometry
            this.tape.TapeStarted  += OnTapeStarted;
            this.tape.TapeModified += OnTapeModified;
            this.tape.TapeBroken   += OnTapeBroken;
        }

        /// <summary>Forwarding helper: call this instead of TapeBuilder.OnBar.</summary>
        public void OnBar(Bar prev, Bar curr, TwoBarType t, VolumeColor prevColor)
        {
            this.tape.OnBar(prev, curr, t, prevColor);
        }

        /// <summary>Current RTL value provider used by TapeBuilder.</summary>
        private double RtlAt(int index)
        {
            return (geom != null) ? geom.RtlAt(index) : double.NaN;
        }

        /// <summary>Optional helper if a consumer also wants LTL values.</summary>
        public double LtlAt(int index)
        {
            return (geom != null) ? geom.LtlAt(index) : double.NaN;
        }

        private void OnTapeStarted(object sender, TapeStartedEventArgs e)
        {
            orientation = e.Orientation;
            p1Index = e.P1Index;

            if (getBarByIndex == null)
            {
                // Without bar accessor we cannot compute high-high / low-low lines correctly.
                // Emit the event but skip geometry creation.
                RaiseTapeStarted(e);
                return;
            }

            Bar b1 = getBarByIndex(e.P1Index);
            Bar b2 = getBarByIndex(e.P2Index);
            if (b1 == null || b2 == null)
            {
                RaiseTapeStarted(e);
                return;
            }

            // Build canonical tape geometry
            geom = GeometryService.BuildTape(orientation, b1, b2);

            // Cache P1 extreme for later P3 re-pointing
            p1Extreme = (orientation == Orientation.Up) ? b1.Low : b1.High;

            RaiseTapeStarted(e);
            RaiseGeometryUpdated();
        }

        private void OnTapeModified(object sender, TapeModifiedEventArgs e)
        {
            // Wick touched RTL but closed inside → re-point RTL to P3 (P1 stays)
            if (geom != null && p1Index >= 0 && p1Extreme == p1Extreme) // nan-check
            {
                geom = geom.WithP3(p1Index, p1Extreme, e.P3Index, e.P3Price);
                RaiseGeometryUpdated();
            }
            if (TapeModified != null) TapeModified(sender, e);
        }

        private void OnTapeBroken(object sender, TapeBrokenEventArgs e)
        {
            if (TapeBroken != null) TapeBroken(sender, e);
        }

        private void RaiseTapeStarted(TapeStartedEventArgs e)
        {
            if (TapeStarted != null) TapeStarted(this, e);
        }

        private void RaiseGeometryUpdated()
        {
            if (GeometryUpdated != null) GeometryUpdated(this, new TapeGeometryUpdatedEventArgs { Geometry = geom });
        }
    }
	
	// =================
	// 13) TraverseGeometryBridge — geometry-backed RTL/LTL for TraverseAssembler
	// =================
	public sealed class TraverseGeometryUpdatedEventArgs : System.EventArgs
	{
	    public ContainerGeometry Geometry { get; set; }
	}
	
	/// <summary>
	/// Bridges TraverseAssembler with GeometryService so RTL/LTL are real lines.
	/// Needs a bar accessor to pull highs/lows at P1/P2 bars.
	/// </summary>
	public sealed class TraverseGeometryBridge
	{
	    private readonly double eps;
	    private readonly System.Func<int, Bar> getBarByIndex;
	
	    private readonly TraverseAssembler traverse;
	    private ContainerGeometry geom;
	
	    // cache P1 so we can rebuild RTL at P3
	    private int p1Index = -1;
	    private double p1Extreme = double.NaN;
	    private int p2Index = -1;
	    private Orientation orientation;
	
	    // Expose orientation & geometry for consumers
	    public Orientation Orientation { get { return orientation; } }
	    public ContainerGeometry Geometry { get { return geom; } }
	
	    // Re-emit TraverseAssembler events
	    public event System.EventHandler<TraverseStartedEventArgs> TraverseStarted;
	    public event System.EventHandler<TraverseJoinedEventArgs>  TraverseJoined;
	    public event System.EventHandler<TraverseP3EventArgs>      TraverseP3;
	    public event System.EventHandler<TraverseFttEventArgs>     TraverseFtt;
	
	    // New: geometry updates
	    public event System.EventHandler<TraverseGeometryUpdatedEventArgs> GeometryUpdated;
	
	    public TraverseGeometryBridge(double epsilon, System.Func<int, Bar> getBarByIndex)
	    {
	        this.eps = System.Math.Max(0, epsilon);
	        this.getBarByIndex = getBarByIndex;
	
	        // Feed TraverseAssembler delegates that reflect current geometry
	        this.traverse = new TraverseAssembler(this.eps, RtlAt, LtlAt);
	
	        // Wire through events and maintain geometry
	        this.traverse.TraverseStarted += OnTraverseStarted;
	        this.traverse.TraverseJoined  += OnTraverseJoined;
	        this.traverse.TraverseP3      += OnTraverseP3;
	        this.traverse.TraverseFtt     += OnTraverseFtt;
	    }
	
	    // Forwarders to underlying assembler
	    public void Reset()
	    {
	        if (geom != null) { geom = null; }
	        p1Index = -1; p2Index = -1; p1Extreme = double.NaN;
	        orientation = Orientation.Up;
	        traverse.Reset();
	    }
	
	    public void AddTape(TapeSummary t) { traverse.AddTape(t); }
	    public void OnBar(Bar b) { traverse.OnBar(b); }
	
	    /// <summary>Current RTL/LTL value providers used by TraverseAssembler.</summary>
	    private double RtlAt(int index)
	    {
	        return (geom != null) ? geom.RtlAt(index) : double.NaN;
	    }
	    private double LtlAt(int index)
	    {
	        return (geom != null) ? geom.LtlAt(index) : double.NaN;
	    }
	
	    private void OnTraverseJoined(object sender, TraverseJoinedEventArgs e)
	    {
	        if (TraverseJoined != null) TraverseJoined(sender, e);
	        // Geometry is actually built at Started (which fires immediately after Join in assembler).
	    }
	
	    private void OnTraverseStarted(object sender, TraverseStartedEventArgs e)
	    {
	        // Cache basic state
	        orientation = e.Orientation;
	        p1Index = e.P1Index;
	        p2Index = e.P2Index;
	        p1Extreme = e.P1Price;
	
	        // Build initial geometry if we can fetch P1/P2 bars
	        if (getBarByIndex != null)
	        {
	            Bar b1 = getBarByIndex(p1Index);
	            Bar b2 = getBarByIndex(p2Index);
	
	            if (b1 != null && b2 != null)
	            {
	                // Strategy:
	                //  - For Up traverse: build LTL from highs (b1.High → b2.High), then RTL parallel through P1 low (e.P1Price).
	                //  - For Down traverse: build RTL from highs (b1.High → b2.High), then LTL parallel through P1 low (e.P1Price).
	                if (orientation == Orientation.Up)
	                {
	                    Line2D ltl = new Line2D(p1Index, b1.High, p2Index, b2.High);
	                    Line2D rtl = ParallelThrough(ltl, p1Index, p1Extreme);
	                    geom = new ContainerGeometry(orientation, rtl, ltl);
	                }
	                else // Down
	                {
	                    Line2D rtl = new Line2D(p1Index, b1.High, p2Index, b2.High);
	                    Line2D ltl = ParallelThrough(rtl, p1Index, e.P1Price /* P1 low in down container? e.P1Price is origin extreme for down = high. Use bar low instead. */);
	                    // For down, P1 extreme in e.P1Price is a HIGH (origin extreme). We want the lower boundary through the P1's Low.
	                    // Use the bar's Low for the parallel LTL:
	                    ltl = ParallelThrough(rtl, p1Index, b1.Low);
	                    geom = new ContainerGeometry(orientation, rtl, ltl);
	                }
	                RaiseGeometryUpdated();
	            }
	        }
	
	        if (TraverseStarted != null) TraverseStarted(this, e);
	    }
	
	    private void OnTraverseP3(object sender, TraverseP3EventArgs e)
	    {
	        // Re-point RTL using P1 → P3 (P3 low for Up; P3 high for Down)
	        if (geom != null && p1Index >= 0 && p1Extreme == p1Extreme) // nan-check
	        {
	            geom = geom.WithP3(p1Index, p1Extreme, e.P3Index, e.P3Price);
	            RaiseGeometryUpdated();
	        }
	        if (TraverseP3 != null) TraverseP3(sender, e);
	    }
	
	    private void OnTraverseFtt(object sender, TraverseFttEventArgs e)
	    {
	        if (TraverseFtt != null) TraverseFtt(sender, e);
	    }
	
	    private void RaiseGeometryUpdated()
	    {
	        if (GeometryUpdated != null) GeometryUpdated(this, new TraverseGeometryUpdatedEventArgs { Geometry = geom });
	    }
	
	    // Build a line parallel to 'baseLine' that passes through (xThrough, yThrough)
	    private static Line2D ParallelThrough(Line2D baseLine, int xThrough, double yThrough)
	    {
	        double m = (baseLine != null) ? baseLine.Slope : 0.0;
	        // construct second point one index ahead with same slope
	        int x2 = xThrough + 1;
	        double y2 = yThrough + m * 1.0;
	        return new Line2D(xThrough, yThrough, x2, y2);
	    }
	}
	
	// =================
	// 14) ChannelGeometryBridge — geometry-backed RTL/LTL for ChannelAssembler
	// =================
	public sealed class ChannelGeometryUpdatedEventArgs : System.EventArgs
	{
	    public ContainerGeometry Geometry { get; set; }
	}
	
	/// <summary>
	/// Bridges ChannelAssembler with GeometryService so channel RTL/LTL are real lines.
	/// Uses traverse summaries (T1/T2) to form initial envelope; re-points RTL at P3.
	/// </summary>
	public sealed class ChannelGeometryBridge
	{
	    private readonly double eps;
	
	    private readonly ChannelAssembler channel;
	    private ContainerGeometry geom;
	
	    // cache P1 so we can rebuild RTL at P3
	    private int p1Index = -1;
	    private double p1Extreme = double.NaN;
	    private Orientation orientation;
	
	    // Expose orientation & geometry for consumers
	    public Orientation Orientation { get { return orientation; } }
	    public ContainerGeometry Geometry { get { return geom; } }
	
	    // Re-emit ChannelAssembler events
	    public event System.EventHandler<ChannelStartedEventArgs> ChannelStarted;
	    public event System.EventHandler<ChannelJoinedEventArgs>  ChannelJoined;
	    public event System.EventHandler<ChannelP3EventArgs>      ChannelP3;
	    public event System.EventHandler<ChannelFttEventArgs>     ChannelFtt;
	
	    // New: geometry updates
	    public event System.EventHandler<ChannelGeometryUpdatedEventArgs> GeometryUpdated;
	
	    public ChannelGeometryBridge(double epsilon)
	    {
	        this.eps = System.Math.Max(0, epsilon);
	
	        // Feed ChannelAssembler delegates that reflect current geometry
	        this.channel = new ChannelAssembler(this.eps, RtlAt, LtlAt);
	
	        // Wire through events and maintain geometry
	        this.channel.ChannelStarted += OnChannelStarted;
	        this.channel.ChannelJoined  += OnChannelJoined;
	        this.channel.ChannelP3      += OnChannelP3;
	        this.channel.ChannelFtt     += OnChannelFtt;
	    }
	
	    // Forwarders to underlying assembler
	    public void Reset()
	    {
	        geom = null;
	        p1Index = -1; p1Extreme = double.NaN;
	        orientation = Orientation.Up;
	        channel.Reset();
	    }
	
	    public void AddTraverse(TraverseSummary t) { channel.AddTraverse(t); }
	    public void OnBar(Bar b) { channel.OnBar(b); }
	
	    /// <summary>Current RTL/LTL value providers used by ChannelAssembler.</summary>
	    private double RtlAt(int index)
	    {
	        return (geom != null) ? geom.RtlAt(index) : double.NaN;
	    }
	    private double LtlAt(int index)
	    {
	        return (geom != null) ? geom.LtlAt(index) : double.NaN;
	    }
	
	    private void OnChannelJoined(object sender, ChannelJoinedEventArgs e)
	    {
	        if (ChannelJoined != null) ChannelJoined(sender, e);
	        // Geometry is built in Started (fired immediately after Join in assembler).
	    }
	
	    private void OnChannelStarted(object sender, ChannelStartedEventArgs e)
	    {
	        // Cache state
	        orientation = e.Orientation;
	        p1Index = e.P1Index;
	        p1Extreme = e.P1Price;
	
	        // Build initial geometry from traverse summaries:
	        // Up channel: LTL (upper) from highs across T1→T2; RTL parallel through P1 low (origin extreme).
	        // Down channel: RTL (upper) from highs across T1→T2; LTL parallel through T1.MinLow at P1 index.
	        var t1 = e.T1;
	        var t2 = e.T2;
	        if (t1 != null && t2 != null)
	        {
	            if (orientation == Orientation.Up)
	            {
	                Line2D ltl = new Line2D(t1.StartIndex, t1.MaxHigh, t2.EndIndex, t2.MaxHigh);
	                Line2D rtl = ParallelThrough(ltl, p1Index, p1Extreme); // pass through P1 low
	                geom = new ContainerGeometry(orientation, rtl, ltl);
	            }
	            else // Down
	            {
	                Line2D rtl = new Line2D(t1.StartIndex, t1.MaxHigh, t2.EndIndex, t2.MaxHigh); // upper bound
	                double p1Low = t1.MinLow; // best available lower anchor at P1 side
	                Line2D ltl = ParallelThrough(rtl, p1Index, p1Low); // lower bound parallel through low
	                geom = new ContainerGeometry(orientation, rtl, ltl);
	            }
	            RaiseGeometryUpdated();
	        }
	
	        if (ChannelStarted != null) ChannelStarted(this, e);
	    }
	
	    private void OnChannelP3(object sender, ChannelP3EventArgs e)
	    {
	        // Re-point RTL using P1 → P3 (P3 low for Up; P3 high for Down), preserve LTL
	        if (geom != null && p1Index >= 0 && p1Extreme == p1Extreme) // nan-check
	        {
	            geom = geom.WithP3(p1Index, p1Extreme, e.P3Index, e.P3Price);
	            RaiseGeometryUpdated();
	        }
	        if (ChannelP3 != null) ChannelP3(sender, e);
	    }
	
	    private void OnChannelFtt(object sender, ChannelFttEventArgs e)
	    {
	        if (ChannelFtt != null) ChannelFtt(sender, e);
	    }
	
	    private void RaiseGeometryUpdated()
	    {
	        if (GeometryUpdated != null) GeometryUpdated(this, new ChannelGeometryUpdatedEventArgs { Geometry = geom });
	    }
	
	    // Build a line parallel to 'baseLine' that passes through (xThrough, yThrough)
	    private static Line2D ParallelThrough(Line2D baseLine, int xThrough, double yThrough)
	    {
	        double m = (baseLine != null) ? baseLine.Slope : 0.0;
	        int x2 = xThrough + 1;
	        double y2 = yThrough + m * 1.0;
	        return new Line2D(xThrough, yThrough, x2, y2);
	    }
	}

	// =================
	// 15) APVAEngine — end-to-end orchestrator (skeleton)
	// =================
	public sealed class APVAEngine
	{
	    private readonly double eps;
	    private readonly System.Func<int, Bar> getBarByIndex;
	
	    // Bridges
	    private readonly TapeGeometryBridge tapeBridge;
	    private readonly TraverseGeometryBridge travBridge;
	    private readonly ChannelGeometryBridge chanBridge;
	
	    // Running state
	    private Bar prevBar = null;
	    private VolumeColor prevColor = VolumeColor.Neutral;
	
	    // Current two-bar type cache (optional to expose)
	    public TwoBarType LastTwoBarType { get; private set; } = TwoBarType.None;
	
	    // ===== Tape summary aggregator (from P1..break) =====
	    private sealed class TapeSummaryBuilder
	    {
	        public bool Active;
	        public Orientation Orientation;
	        public int StartIndex = -1;
	        public int EndIndex   = -1;
	        public double MaxHigh = double.NegativeInfinity;
	        public double MinLow  = double.PositiveInfinity;
	
	        public void Start(Orientation o, int p1Index)
	        {
	            Active = true; Orientation = o; StartIndex = p1Index;
	            EndIndex = -1; MaxHigh = double.NegativeInfinity; MinLow = double.PositiveInfinity;
	        }
	        public void Update(Bar b)
	        {
	            if (!Active || b == null) return;
	            if (b.High > MaxHigh) MaxHigh = b.High;
	            if (b.Low  < MinLow)  MinLow  = b.Low;
	            EndIndex = b.Index;
	        }
	        public TapeSummary Finish()
	        {
	            if (!Active) return null;
	            Active = false;
	            return new TapeSummary
	            {
	                Orientation = this.Orientation,
	                StartIndex  = this.StartIndex,
	                EndIndex    = this.EndIndex,
	                MaxHigh     = this.MaxHigh,
	                MinLow      = this.MinLow
	            };
	        }
	        public void Reset() { Active = false; StartIndex = EndIndex = -1; MaxHigh = double.NegativeInfinity; MinLow = double.PositiveInfinity; }
	    }
	    private readonly TapeSummaryBuilder tapeAgg = new TapeSummaryBuilder();
	
	    // ===== Traverse summary aggregator (very light) =====
	    private sealed class TraverseSummaryBuilder
	    {
	        public bool Active;
	        public Orientation Orientation;
	        public int StartIndex = -1;
	        public int EndIndex   = -1;
	        public double MaxHigh = double.NegativeInfinity;
	        public double MinLow  = double.PositiveInfinity;
	
	        public void Start(Orientation o, int p1Index)
	        {
	            Active = true; Orientation = o; StartIndex = p1Index;
	            EndIndex = -1; MaxHigh = double.NegativeInfinity; MinLow = double.PositiveInfinity;
	        }
	        public void Update(TapeSummary t)
	        {
	            if (!Active || t == null) return;
	            if (t.MaxHigh > MaxHigh) MaxHigh = t.MaxHigh;
	            if (t.MinLow  < MinLow)  MinLow  = t.MinLow;
	            EndIndex = (t.EndIndex >= 0 ? t.EndIndex : EndIndex);
	        }
	        public TraverseSummary Finish()
	        {
	            if (!Active) return null;
	            Active = false;
	            return new TraverseSummary
	            {
	                Orientation = this.Orientation,
	                StartIndex  = this.StartIndex,
	                EndIndex    = this.EndIndex,
	                MaxHigh     = this.MaxHigh,
	                MinLow      = this.MinLow
	            };
	        }
	        public void Reset() { Active = false; StartIndex = EndIndex = -1; MaxHigh = double.NegativeInfinity; MinLow = double.PositiveInfinity; }
	    }
	    private readonly TraverseSummaryBuilder travAgg = new TraverseSummaryBuilder();
	
	    // ===== Events (re-emit plus high-level hooks) =====
	    public event System.EventHandler<TapeStartedEventArgs>    TapeStarted;
	    public event System.EventHandler<TapeModifiedEventArgs>   TapeModified;
	    public event System.EventHandler<TapeBrokenEventArgs>     TapeBroken;
	    public event System.EventHandler<TapeGeometryUpdatedEventArgs> TapeGeometryUpdated;
	
	    public event System.EventHandler<TraverseStartedEventArgs>   TraverseStarted;
	    public event System.EventHandler<TraverseJoinedEventArgs>    TraverseJoined;
	    public event System.EventHandler<TraverseP3EventArgs>        TraverseP3;
	    public event System.EventHandler<TraverseFttEventArgs>       TraverseFtt;
	    public event System.EventHandler<TraverseGeometryUpdatedEventArgs> TraverseGeometryUpdated;
	
	    public event System.EventHandler<ChannelStartedEventArgs>   ChannelStarted;
	    public event System.EventHandler<ChannelJoinedEventArgs>    ChannelJoined;
	    public event System.EventHandler<ChannelP3EventArgs>        ChannelP3;
	    public event System.EventHandler<ChannelFttEventArgs>       ChannelFtt;
	    public event System.EventHandler<ChannelGeometryUpdatedEventArgs> ChannelGeometryUpdated;
	
	    public APVAEngine(double epsilon, System.Func<int, Bar> getBarByIndex)
	    {
	        eps = System.Math.Max(0, epsilon);
	        this.getBarByIndex = getBarByIndex;
	
	        // Bridges
	        tapeBridge = new TapeGeometryBridge(eps, getBarByIndex);
	        travBridge = new TraverseGeometryBridge(eps, getBarByIndex);
	        chanBridge = new ChannelGeometryBridge(eps);
	
	        // Wire: Tape
	        tapeBridge.TapeStarted        += (s,e) => { OnTapeStarted(e); };
	        tapeBridge.TapeModified       += (s,e) => { if (TapeModified != null) TapeModified(s,e); };
	        tapeBridge.TapeBroken         += (s,e) => { OnTapeBroken(e); };
	        tapeBridge.GeometryUpdated    += (s,e) => { if (TapeGeometryUpdated != null) TapeGeometryUpdated(s,e); };
	
	        // Wire: Traverse
	        travBridge.TraverseJoined     += (s,e) => { if (TraverseJoined != null) TraverseJoined(s,e); };
	        travBridge.TraverseStarted    += (s,e) => { OnTraverseStarted(e); };
	        travBridge.TraverseP3         += (s,e) => { if (TraverseP3 != null) TraverseP3(s,e); };
	        travBridge.TraverseFtt        += (s,e) => { OnTraverseFtt(e); };
	        travBridge.GeometryUpdated    += (s,e) => { if (TraverseGeometryUpdated != null) TraverseGeometryUpdated(s,e); };
	
	        // Wire: Channel (ready for future feed)
	        chanBridge.ChannelJoined      += (s,e) => { if (ChannelJoined != null) ChannelJoined(s,e); };
	        chanBridge.ChannelStarted     += (s,e) => { if (ChannelStarted != null) ChannelStarted(s,e); };
	        chanBridge.ChannelP3          += (s,e) => { if (ChannelP3 != null) ChannelP3(s,e); };
	        chanBridge.ChannelFtt         += (s,e) => { if (ChannelFtt != null) ChannelFtt(s,e); };
	        chanBridge.GeometryUpdated    += (s,e) => { if (ChannelGeometryUpdated != null) ChannelGeometryUpdated(s,e); };
	    }
	
	    // ===== Public feed =====
	    public void OnBar(Bar curr)
	    {
	        if (curr == null) return;
	
	        // Two-bar classification and paint
	        TwoBarType t = TwoBarType.None;
	        if (prevBar != null) t = BarClassifier.ClassifyTwoBar(prevBar, curr, eps);
	        LastTwoBarType = t;
	        var color = BarClassifier.GetVolumeColor(prevBar, curr, t, prevColor, eps);
	
	        // Drive the tape layer (bridge will build/maintain geometry)
	        tapeBridge.OnBar(prevBar, curr, t, prevColor);
	
	        // Update aggregators
	        if (tapeAgg.Active) tapeAgg.Update(curr);
	
	        // advance state
	        prevColor = color;
	        prevBar = curr;
	    }
	
	    // ===== Internal handlers =====
	    private void OnTapeStarted(TapeStartedEventArgs e)
	    {
	        // Begin aggregating current tape summary
	        tapeAgg.Reset();
	        tapeAgg.Start(e.Orientation, e.P1Index);
	
	        if (TapeStarted != null) TapeStarted(this, e);
	    }
	
	    private void OnTapeBroken(TapeBrokenEventArgs e)
	    {
	        // Finalize TapeSummary and feed TraverseAssembler
	        var summary = tapeAgg.Finish();
	        if (summary != null)
	            travBridge.AddTape(summary);
	
	        if (TapeBroken != null) TapeBroken(this, e);
	    }
	
		private void OnTraverseStarted(TraverseStartedEventArgs e)
		{
		    // If a traverse was already aggregating, finalize it and feed channel
		    if (travAgg.Active)
		    {
		        var finishedPrev = travAgg.Finish();
		        if (finishedPrev != null)
		        {
		            // Feed completed traverse to the channel layer
		            chanBridge.AddTraverse(finishedPrev);
		        }
		    }
		
		    travAgg.Reset();
		    travAgg.Start(e.Orientation, e.P1Index);
		
		    // Seed extremes from the 3 tapes that formed the traverse
		    if (e.T1 != null) travAgg.Update(e.T1);
		    if (e.T2 != null) travAgg.Update(e.T2);
		    if (e.T3 != null) travAgg.Update(e.T3);
		
		    if (TraverseStarted != null) TraverseStarted(this, e);
		}
		
		private void OnTraverseFtt(TraverseFttEventArgs e)
		{
		    // Finalize current traverse summary and feed it forward
		    var finished = travAgg.Finish();
		    if (finished != null)
		    {
		        chanBridge.AddTraverse(finished);
		    }
		
		    if (TraverseFtt != null) TraverseFtt(this, e);
		}
		
	    // Helper: convert TapeSummary (already correct type) — overload here for clarity
	    private static TapeSummary ToTapeSummary(TapeSummary t) { return t; }
	
	    // ===== Optional utilities =====
	    public ContainerGeometry CurrentTapeGeometry    { get { return tapeBridge.Geometry; } }
	    public ContainerGeometry CurrentTraverseGeometry{ get { return travBridge.Geometry; } }
	    public ContainerGeometry CurrentChannelGeometry { get { return chanBridge.Geometry; } }
	
	    public Orientation? CurrentTapeOrientation
	    {
	        get { return tapeBridge != null ? (Orientation?)tapeBridge.Orientation : null; }
	    }
	}
	
	// =================
	// 17) RenderSpec + Composer (drawing-friendly DTOs)
	// =================
	
	public enum StrokeKind { Solid, Dash, Dot, DotDash }
	public enum ContainerLevel { Tape, Traverse, Channel }
	public enum MarkerType { P1, P2, P3, FTT }
	
	public sealed class StrokeStyle
	{
	    public string ColorHex { get; set; } = "#808080"; // generic gray
	    public double Thickness { get; set; } = 1.0;
	    public StrokeKind Kind { get; set; } = StrokeKind.Solid;
	
	    public StrokeStyle Clone() => new StrokeStyle { ColorHex = ColorHex, Thickness = Thickness, Kind = Kind };
	}
	
	public sealed class LineSpec
	{
	    public int X1 { get; set; }
	    public double Y1 { get; set; }
	    public int X2 { get; set; }
	    public double Y2 { get; set; }
	    public StrokeStyle Style { get; set; } = new StrokeStyle();
	    public string Label { get; set; } // "RTL", "LTL", "VE"
	}
	
	public sealed class MarkerSpec
	{
	    public int Index { get; set; }
	    public double Price { get; set; }
	    public MarkerType Type { get; set; }
	    public string Label { get; set; } // optional text to display
	}
	
	public sealed class ContainerRenderSpec
	{
	    public ContainerLevel Level { get; set; }
	    public Orientation Orientation { get; set; }
	    public LineSpec RTL { get; set; }    // required when geometry available
	    public LineSpec LTL { get; set; }    // required when geometry available
	    public List<LineSpec> VeLines { get; set; } = new List<LineSpec>(); // same style as container per spec
	    public List<MarkerSpec> Markers { get; set; } = new List<MarkerSpec>();
	    public int StartIndex { get; set; }  // render window
	    public int EndIndex { get; set; }
	}
	
	public sealed class APVARenderSnapshot
	{
	    public ContainerRenderSpec Tape { get; set; }
	    public ContainerRenderSpec Traverse { get; set; }
	    public ContainerRenderSpec Channel { get; set; }
	}
	
	// ---- Default style helpers (feel free to change colors to your palette) ----
	public static class RenderStyles
	{
	    // Tape: dashed
	    public static StrokeStyle TapeUp()   => new StrokeStyle { ColorHex = "#2ECC71", Thickness = 1.5, Kind = StrokeKind.Dash };
	    public static StrokeStyle TapeDown() => new StrokeStyle { ColorHex = "#E74C3C", Thickness = 1.5, Kind = StrokeKind.Dash };
	
	    // Traverse: dot-dash (to distinguish from tape)
	    public static StrokeStyle TraverseUp()   => new StrokeStyle { ColorHex = "#3498DB", Thickness = 1.75, Kind = StrokeKind.DotDash };
	    public static StrokeStyle TraverseDown() => new StrokeStyle { ColorHex = "#9B59B6", Thickness = 1.75, Kind = StrokeKind.DotDash };
	
	    // Channel: solid and thicker
	    public static StrokeStyle ChannelUp()   => new StrokeStyle { ColorHex = "#1ABC9C", Thickness = 2.0, Kind = StrokeKind.Solid };
	    public static StrokeStyle ChannelDown() => new StrokeStyle { ColorHex = "#F39C12", Thickness = 2.0, Kind = StrokeKind.Solid };
	}
	
	/// <summary>
	/// Listens to APVAEngine events, keeps current windowed line specs for
	/// Tape / Traverse / Channel. VE lines (if provided separately) will
	/// inherit container style automatically, per your rule.
	/// </summary>
	public sealed class APVARenderComposer
	{
	    private readonly APVAEngine engine;
	    private int winStart = 0, winEnd = 0;
	
	    // Current snapshots
	    private ContainerRenderSpec tapeSpec;
	    private ContainerRenderSpec travSpec;
	    private ContainerRenderSpec chanSpec;
	
	    public APVARenderComposer(APVAEngine engine, int windowStartIndex, int windowEndIndex)
	    {
	        this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
	        SetWindow(windowStartIndex, windowEndIndex);
	
	        // Tape events
	        engine.TapeGeometryUpdated += (s, e) => { Rebuild(ContainerLevel.Tape, engine.CurrentTapeGeometry, engine.CurrentTapeOrientation); };
	        engine.TapeStarted += (s, e) => { EnsureSpec(ContainerLevel.Tape, e.Orientation); UpsertMarker(ContainerLevel.Tape, MarkerType.P1, e.P1Index, e.P1Price, "P1"); /* P2 is provisional at this point */ };
	        engine.TapeModified += (s, e) => { EnsureSpec(ContainerLevel.Tape, engine.CurrentTapeOrientation); UpsertMarker(ContainerLevel.Tape, MarkerType.P3, e.P3Index, e.P3Price, "P3"); };
	        engine.TapeBroken += (s, e) => { /* keep last geometry on chart; consumer may clear when desired */ };
	
	        // Traverse events
	        engine.TraverseGeometryUpdated += (s, e) => { Rebuild(ContainerLevel.Traverse, engine.CurrentTraverseGeometry, engine.CurrentTapeOrientation /* orientation doesn’t matter here, engine has it */); };
	        engine.TraverseStarted += (s, e) =>
	        {
	            EnsureSpec(ContainerLevel.Traverse, e.Orientation);
	            UpsertMarker(ContainerLevel.Traverse, MarkerType.P1, e.P1Index, e.P1Price, "P1");
	            UpsertMarker(ContainerLevel.Traverse, MarkerType.P2, e.P2Index, e.P2Price, "P2");
	        };
	        engine.TraverseP3 += (s, e) => { EnsureSpec(ContainerLevel.Traverse, engine.CurrentTraverseGeometry != null ? engine.CurrentTraverseGeometry.Orientation : Orientation.Up); UpsertMarker(ContainerLevel.Traverse, MarkerType.P3, e.P3Index, e.P3Price, "P3"); };
	        engine.TraverseFtt += (s, e) => { EnsureSpec(ContainerLevel.Traverse, engine.CurrentTraverseGeometry != null ? engine.CurrentTraverseGeometry.Orientation : Orientation.Up); UpsertMarker(ContainerLevel.Traverse, MarkerType.FTT, e.FttIndex, double.NaN, "FTT"); };
	
	        // Channel events
	        engine.ChannelGeometryUpdated += (s, e) => { Rebuild(ContainerLevel.Channel, engine.CurrentChannelGeometry, engine.CurrentChannelGeometry != null ? engine.CurrentChannelGeometry.Orientation : Orientation.Up); };
	        engine.ChannelStarted += (s, e) =>
	        {
	            EnsureSpec(ContainerLevel.Channel, e.Orientation);
	            UpsertMarker(ContainerLevel.Channel, MarkerType.P1, e.P1Index, e.P1Price, "P1");
	            UpsertMarker(ContainerLevel.Channel, MarkerType.P2, e.P2Index, e.P2Price, "P2");
	        };
	        engine.ChannelP3 += (s, e) => { EnsureSpec(ContainerLevel.Channel, engine.CurrentChannelGeometry != null ? engine.CurrentChannelGeometry.Orientation : Orientation.Up); UpsertMarker(ContainerLevel.Channel, MarkerType.P3, e.P3Index, e.P3Price, "P3"); };
	        engine.ChannelFtt += (s, e) => { EnsureSpec(ContainerLevel.Channel, engine.CurrentChannelGeometry != null ? engine.CurrentChannelGeometry.Orientation : Orientation.Up); UpsertMarker(ContainerLevel.Channel, MarkerType.FTT, e.FttIndex, double.NaN, "FTT"); };
	    }
	
	    public void SetWindow(int startIndexInclusive, int endIndexInclusive)
	    {
	        winStart = startIndexInclusive;
	        winEnd = endIndexInclusive >= startIndexInclusive ? endIndexInclusive : startIndexInclusive;
	        // Rebuild all if geometry exists
	        if (engine.CurrentTapeGeometry != null)      Rebuild(ContainerLevel.Tape, engine.CurrentTapeGeometry, engine.CurrentTapeOrientation);
	        if (engine.CurrentTraverseGeometry != null)  Rebuild(ContainerLevel.Traverse, engine.CurrentTraverseGeometry, engine.CurrentTraverseGeometry.Orientation);
	        if (engine.CurrentChannelGeometry != null)   Rebuild(ContainerLevel.Channel, engine.CurrentChannelGeometry, engine.CurrentChannelGeometry.Orientation);
	    }
	
	    public APVARenderSnapshot GetSnapshot()
	    {
	        return new APVARenderSnapshot
	        {
	            Tape = tapeSpec,
	            Traverse = travSpec,
	            Channel = chanSpec
	        };
	    }
	
	    /// <summary>
	    /// Allow VE segments to be painted with the same style as the owning container.
	    /// Call this from your VE trackers when they (re)start/terminate.
	    /// </summary>
	    public void ApplyVeSegments(ContainerLevel level, IEnumerable<VeSegment> segments)
	    {
	        var spec = GetSpec(level, createIfMissing:false);
	        if (spec == null || spec.RTL == null) return;
	
	        spec.VeLines.Clear();
	        if (segments == null) return;
	
	        // Same color & dash as container (use RTL style as the canonical container style)
	        foreach (var seg in segments)
	        {
	            if (seg == null || seg.StartIndex < winStart || (seg.EndIndex >= 0 && seg.EndIndex < winStart)) continue;
	            int x1 = System.Math.Max(seg.StartIndex, winStart);
	            int x2 = (seg.EndIndex >= 0) ? System.Math.Min(seg.EndIndex, winEnd) : winEnd;
	
	            // No price endpoint available except OriginPrice → we’ll render VE as horizontal until the next segment update
	            var ve = new LineSpec
	            {
	                X1 = x1,
	                Y1 = seg.OriginPrice,
	                X2 = x2,
	                Y2 = seg.OriginPrice,
	                Label = "VE",
	                Style = spec.RTL.Style.Clone() // inherit style exactly
	            };
	            spec.VeLines.Add(ve);
	        }
	    }
	
	    // -------- internals --------
	
	    private void Rebuild(ContainerLevel level, ContainerGeometry geom, Orientation? orientation)
	    {
	        if (geom == null) return;
	
	        var spec = GetSpec(level, createIfMissing:true);
	        spec.Level = level;
	        spec.Orientation = orientation ?? Orientation.Up;
	        spec.StartIndex = winStart; spec.EndIndex = winEnd;
	
	        // Styles per level + orientation
	        var styleRtl = PickStyle(level, spec.Orientation);
	        var styleLtl = styleRtl.Clone(); // keep same style family; label distinguishes
	
	        // Evaluate lines over current window
	        var rtl = new LineSpec { X1 = winStart, Y1 = geom.RtlAt(winStart), X2 = winEnd, Y2 = geom.RtlAt(winEnd), Style = styleRtl, Label = "RTL" };
	        var ltl = new LineSpec { X1 = winStart, Y1 = geom.LtlAt(winStart), X2 = winEnd, Y2 = geom.LtlAt(winEnd), Style = styleLtl, Label = "LTL" };
	        spec.RTL = rtl; spec.LTL = ltl;
	
	        // VE lines: unchanged here; caller may re-apply with ApplyVeSegments
	    }
	
	    private StrokeStyle PickStyle(ContainerLevel level, Orientation o)
	    {
	        switch (level)
	        {
	            case ContainerLevel.Tape:     return (o == Orientation.Up) ? RenderStyles.TapeUp()     : RenderStyles.TapeDown();
	            case ContainerLevel.Traverse: return (o == Orientation.Up) ? RenderStyles.TraverseUp() : RenderStyles.TraverseDown();
	            case ContainerLevel.Channel:  return (o == Orientation.Up) ? RenderStyles.ChannelUp()  : RenderStyles.ChannelDown();
	        }
	        return new StrokeStyle();
	    }
	
	    private ContainerRenderSpec GetSpec(ContainerLevel level, bool createIfMissing)
	    {
	        switch (level)
	        {
	            case ContainerLevel.Tape:
	                if (tapeSpec == null && createIfMissing) tapeSpec = new ContainerRenderSpec { Level = ContainerLevel.Tape, StartIndex = winStart, EndIndex = winEnd };
	                return tapeSpec;
	            case ContainerLevel.Traverse:
	                if (travSpec == null && createIfMissing) travSpec = new ContainerRenderSpec { Level = ContainerLevel.Traverse, StartIndex = winStart, EndIndex = winEnd };
	                return travSpec;
	            case ContainerLevel.Channel:
	                if (chanSpec == null && createIfMissing) chanSpec = new ContainerRenderSpec { Level = ContainerLevel.Channel, StartIndex = winStart, EndIndex = winEnd };
	                return chanSpec;
	        }
	        return null;
	    }
	
		// Accept nullable Orientation and coalesce to a default
		private void EnsureSpec(ContainerLevel level, Orientation? oNullable)
		{
		    EnsureSpec(level, oNullable ?? Orientation.Up);
		}

	    private void EnsureSpec(ContainerLevel level, Orientation o)
	    {
	        var spec = GetSpec(level, createIfMissing:true);
	        if (spec.Orientation != o) spec.Orientation = o;
	        spec.StartIndex = winStart; spec.EndIndex = winEnd;
	    }
	
	    private void UpsertMarker(ContainerLevel level, MarkerType type, int index, double price, string label)
	    {
	        var spec = GetSpec(level, createIfMissing:true);
	        if (spec.Markers == null) spec.Markers = new List<MarkerSpec>();
	        // replace existing same-type marker at same index if any
	        for (int i = 0; i < spec.Markers.Count; i++)
	        {
	            var m = spec.Markers[i];
	            if (m.Type == type && m.Index == index) { spec.Markers[i] = new MarkerSpec { Index = index, Price = price, Type = type, Label = label }; return; }
	        }
	        spec.Markers.Add(new MarkerSpec { Index = index, Price = price, Type = type, Label = label });
	    }
	}


    // =================
    // 18) Test Harness — NT8-friendly self-tests
    // =================

    public static class TestRunner
    {
        private static void Log(List<string> sink, string msg) { if (sink != null) sink.Add(msg); }
        private static Bar B(int idx, double o, double h, double l, double c, long v)
        {
            Bar b = new Bar(); b.Index = idx; b.Open = o; b.High = h; b.Low = l; b.Close = c; b.Volume = v; return b;
        }

        public static List<string> RunAll()
		{
		    List<string> outp = new List<string>();
		    try { Test_BarClassifier(outp); } catch (Exception ex) { Log(outp, "EX Test_BarClassifier: " + ex.Message); }
		    try { Test_Lateral(outp); } catch (Exception ex) { Log(outp, "EX Test_Lateral: " + ex.Message); }
		    try { Test_Tape_ModifyVsBreak(outp); } catch (Exception ex) { Log(outp, "EX Test_Tape_ModifyVsBreak: " + ex.Message); }
		    try { Test_VE_Down(outp); } catch (Exception ex) { Log(outp, "EX Test_VE_Down: " + ex.Message); }
		    try { Test_Traverse_Join_P3_FTT(outp); } catch (Exception ex) { Log(outp, "EX Test_Traverse: " + ex.Message); }
		    try { Test_Channel_Join_P3_FTT(outp); } catch (Exception ex) { Log(outp, "EX Test_Channel: " + ex.Message); }
		    try { Test_FTTDetector(outp); } catch (Exception ex) { Log(outp, "EX Test_FTTDetector: " + ex.Message); }
		    try { Test_VolumeCycle(outp); } catch (Exception ex) { Log(outp, "EX Test_VolumeCycle: " + ex.Message); }
		
		    // New geometry/bridge/engine tests
		    try { Test_Geometry_BuildTape_WithP3(outp); } catch (Exception ex) { Log(outp, "EX Test_Geometry: " + ex.Message); }
		    try { Test_TapeGeometryBridge_Lifecycle(outp); } catch (Exception ex) { Log(outp, "EX Test_TapeGeometryBridge: " + ex.Message); }
		    try { Test_TraverseGeometryBridge_Lifecycle(outp); } catch (Exception ex) { Log(outp, "EX Test_TraverseGeometryBridge: " + ex.Message); }
		    try { Test_ChannelGeometryBridge_Lifecycle(outp); } catch (Exception ex) { Log(outp, "EX Test_ChannelGeometryBridge: " + ex.Message); }
		    try { Test_APVAEngine_TapeLifecycle(outp); } catch (Exception ex) { Log(outp, "EX Test_APVAEngine: " + ex.Message); }
		
		    return outp;
		}


        private static void Pass(List<string> o, string name) { Log(o, "PASS: " + name); }
        private static void Fail(List<string> o, string name, string why) { Log(o, "FAIL: " + name + " → " + why); }

        private static void Test_BarClassifier(List<string> o)
        {
            double eps = 0.00001;
            Bar b0 = B(0, 10, 10, 9, 9.5, 1000);
            Bar b1 = B(1, 9.6, 10.6, 9.6, 10.4, 1100); // HHHL vs b0
            TwoBarType t1 = BarClassifier.ClassifyTwoBar(b0, b1, eps);
            if (t1 == TwoBarType.HHHL) Pass(o, "Classifier HHHL"); else Fail(o, "Classifier HHHL", t1.ToString());

            Bar b2 = B(2, 10.2, 10.4, 9.8, 10.0, 900); // inside b1
            TwoBarType t2 = BarClassifier.ClassifyTwoBar(b1, b2, eps);
            if (t2 == TwoBarType.IB) Pass(o, "Classifier IB"); else Fail(o, "Classifier IB", t2.ToString());

            Bar b3 = B(3, 10.0, 10.8, 9.5, 10.6, 1200); // outside b2
            TwoBarType t3 = BarClassifier.ClassifyTwoBar(b2, b3, eps);
            if (t3 == TwoBarType.OB) Pass(o, "Classifier OB"); else Fail(o, "Classifier OB", t3.ToString());
        }

        private static void Test_Lateral(List<string> o)
        {
            double eps = 0.00001;
            LateralTracker lt = new LateralTracker(eps);
            bool activated = false, ended = false;
            lt.LateralActivated += (s,e) => { activated = true; };
            lt.LateralEnded += (s,e) => { ended = true; };

            lt.OnBar(B(0, 10, 10, 9.5, 9.7, 1000)); // seed
            lt.OnBar(B(1, 9.8, 9.9, 9.6, 9.7, 900)); // inside
            lt.OnBar(B(2, 9.85, 9.95, 9.65, 9.7, 950)); // inside → activate
            lt.OnBar(B(3, 10.2, 10.3, 10.21, 10.25, 1100)); // full-range above → end

            if (activated) Pass(o, "Lateral activates after 2 contained bars"); else Fail(o, "Lateral activates", "no activation");
            if (ended) Pass(o, "Lateral ends only on full-range break"); else Fail(o, "Lateral ends", "no end detected");
        }

        private static void Test_Tape_ModifyVsBreak(List<string> o)
        {
            double eps = 0.00001; double rtlLevel = 100.0;
            TapeBuilder tb = new TapeBuilder(eps, (idx) => rtlLevel);
            bool modified = false, broken = false;
            tb.TapeModified += (s,e) => { modified = true; };
            tb.TapeBroken += (s,e) => { broken = true; };

            Bar p = B(0, 99.5, 101.0, 99.0, 100.5, 1000);
            Bar c1 = B(1, 100.2, 101.2, 100.1, 101.0, 1100); // translational up vs p → seed
            TwoBarType t1 = BarClassifier.ClassifyTwoBar(p, c1, eps);
            tb.OnBar(p, c1, t1, VolumeColor.Black);

            Bar c2 = B(2, 100.2, 100.3, 99.95, 100.05, 900); // Low < RTL, Close >= RTL → modify
            tb.OnBar(c1, c2, BarClassifier.ClassifyTwoBar(c1, c2, eps), VolumeColor.Black);

            Bar c3 = B(3, 100.0, 100.1, 99.6, 99.7, 1200); // Close < RTL → break
            tb.OnBar(c2, c3, BarClassifier.ClassifyTwoBar(c2, c3, eps), VolumeColor.Red);

            if (modified) Pass(o, "Tape modify on wick through RTL"); else Fail(o, "Tape modify", "not fired");
            if (broken) Pass(o, "Tape break on close through RTL"); else Fail(o, "Tape break", "not fired");
        }

        private static void Test_VE_Down(List<string> o)
        {
            double eps = 0.00001; double ltl = 90.0;
            VeTracker ve = new VeTracker(Orientation.Down, (i) => 110.0, (i) => ltl, eps);
            bool started1 = false, terminated = false, started2 = false;
            ve.VeStarted += (s,e) => { if (!started1) started1 = true; else started2 = true; };
            ve.VeTerminated += (s,e) => { terminated = true; };

            ve.OnBar(B(0, 95, 96, 89.9, 90.5, 1000)); // Low < LTL → start VE1
            ve.OnBar(B(1, 92, 93, 88.9, 89.5, 1100)); // Low < previous → terminate VE1, start VE2

            if (started1 && terminated && started2) Pass(o, "VE down: terminate & renew on expansion breaches"); else Fail(o, "VE down", "events missing");
        }

        private static void Test_Traverse_Join_P3_FTT(List<string> o)
        {
            double eps = 0.00001;
            // Summaries
            TapeSummary d1 = new TapeSummary(); d1.Orientation = Orientation.Down; d1.StartIndex = 10; d1.EndIndex = 12; d1.MaxHigh = 110; d1.MinLow = 100;
            TapeSummary u  = new TapeSummary(); u.Orientation = Orientation.Up;   u.StartIndex = 13; u.EndIndex = 15; u.MaxHigh = 115; u.MinLow = 101; // pivot low = 101
            TapeSummary d2 = new TapeSummary(); d2.Orientation = Orientation.Down; d2.StartIndex = 16; d2.EndIndex = 18; d2.MaxHigh = 114; d2.MinLow = 100.5; // <= 101 → join

            // RTL/LTL for traverse: choose constants for ease
            double rtl = 112.0; double ltl = 98.0;
            TraverseAssembler ta = new TraverseAssembler(eps, (i) => rtl, (i) => ltl);
            bool joined = false, p3 = false, ftt = false;
            ta.TraverseJoined += (s,e) => { joined = true; };
            ta.TraverseP3 += (s,e) => { p3 = true; };
            ta.TraverseFtt += (s,e) => { ftt = true; };

            ta.AddTape(d1); ta.AddTape(u); ta.AddTape(d2); // should join
            // simulate bars of third tape for P3 and FTT
            ta.OnBar(B(19, 111.5, 112.1, 110.0, 111.8, 1000)); // High >= RTL → P3
            ta.OnBar(B(20, 112.0, 113.0, 111.0, 112.2, 1100)); // Close > RTL and no LTL touch → FTT

            if (joined) Pass(o, "Traverse join D-U-D"); else Fail(o, "Traverse join", "not joined");
            if (p3) Pass(o, "Traverse P3 touch"); else Fail(o, "Traverse P3", "no P3");
            if (ftt) Pass(o, "Traverse FTT on close beyond RTL"); else Fail(o, "Traverse FTT", "no FTT");
        }

        private static void Test_Channel_Join_P3_FTT(List<string> o)
		{
		    double eps = 0.00001;
		    TraverseSummary t1 = new TraverseSummary { Orientation = Orientation.Up,   StartIndex = 100, EndIndex = 105, MaxHigh = 120, MinLow = 110 };
		    TraverseSummary t2 = new TraverseSummary { Orientation = Orientation.Down, StartIndex = 106, EndIndex = 111, MaxHigh = 119, MinLow = 109 };
		    TraverseSummary t3 = new TraverseSummary { Orientation = Orientation.Up,   StartIndex = 112, EndIndex = 117, MaxHigh = 121, MinLow = 113 };
		
		    // For an UP channel: FTT requires Close < RTL *before any LTL touch*.
		    // Set LTL well ABOVE price so High never reaches it post-P3.
		    double rtl = 114.0; 
		    double ltl = 120.0;   // (was 108.0 — that caused an immediate opposite-boundary touch)
		    ChannelAssembler ca = new ChannelAssembler(eps, (i) => rtl, (i) => ltl);
		
		    bool joined = false, p3 = false, ftt = false;
		    ca.ChannelJoined += (s,e) => { joined = true; };
		    ca.ChannelP3     += (s,e) => { p3     = true; };
		    ca.ChannelFtt    += (s,e) => { ftt    = true; };
		
		    ca.AddTraverse(t1); ca.AddTraverse(t2); ca.AddTraverse(t3);              // join
		    ca.OnBar(B(118, 113.5, 114.1, 113.0, 113.9, 1000)); // Low ≤ RTL → P3
		    ca.OnBar(B(119, 113.0, 113.5, 112.0, 112.9, 1100)); // Close < RTL → FTT (no LTL touch)
		
		    if (joined) Pass(o, "Channel join U-D-U"); else Fail(o, "Channel join", "not joined");
		    if (p3)     Pass(o, "Channel P3 touch");    else Fail(o, "Channel P3", "no P3");
		    if (ftt)    Pass(o, "Channel FTT on close beyond RTL"); else Fail(o, "Channel FTT", "no FTT");
		}

        private static void Test_FTTDetector(List<string> o)
		{
		    double eps = 0.00001; double rtl = 100.0;
		    FttDetector ftt = new FttDetector(eps, 0.6, (i) => rtl);
		    ftt.Reset(Orientation.Down);
		    bool cand = false, conf = false, inv = false;
		    ftt.FttCandidate  += (s,e) => { cand = true; };
		    ftt.FttConfirmed  += (s,e) => { conf = true; };
		    ftt.FttInvalidated+= (s,e) => { inv  = true; };
		
		    // --- Candidate + Confirm ---
		    Bar p = B(0, 100.5, 101, 99.5, 100.0, 800);
		    Bar c = B(1, 100.6, 100.6, 99.0, 100.55, 3000); // LHLL vs p with up close near high → candidate
		    TwoBarType t = BarClassifier.ClassifyTwoBar(p, c, eps);
		    ftt.OnBar(p, c, t, Orientation.Down, false); 
		    ftt.OnBar(c, B(2, 100.6, 101.0, 100.4, 100.8, 2000),
		              BarClassifier.ClassifyTwoBar(c, B(2,101,101,100.4,100.8,2000), eps),
		              Orientation.Down, false); // confirm close > RTL
		    if (cand && conf) Pass(o, "FTT candidate + confirm (down container)"); else Fail(o, "FTT candidate + confirm", $"cand={cand}, conf={conf}");
		
		    // --- Invalidation path: create candidate, then continue down on higher volume ---
		    ftt.Reset(Orientation.Down); cand = conf = inv = false;
		
		    Bar p2  = B(10, 100.5, 101.0, 99.5, 100.0, 900);
		    // Force candidate by opposite close near high (Open < Close), still LHLL vs p2
		    Bar c2a = B(11, 100.2, 100.6, 99.1, 100.55, 1500); // up close near high
		    TwoBarType t2a = BarClassifier.ClassifyTwoBar(p2, c2a, eps);
		    ftt.OnBar(p2, c2a, t2a, Orientation.Down, false); // candidate should fire
		
		    // Continuation down with expanding volume (no confirm): lower low + higher vol → invalidation
		    Bar c2b = B(12, 100.0, 100.2, 98.8, 99.1, 2200);
		    TwoBarType t2b = BarClassifier.ClassifyTwoBar(c2a, c2b, eps);
		    ftt.OnBar(c2a, c2b, t2b, Orientation.Down, false);
		
		    if (ftt.State == FttState.Invalidated || inv)
		        Pass(o, "FTT invalidation on continuation");
		    else
		        Fail(o, "FTT invalidation", "state="+ftt.State);
		}

        private static void Test_VolumeCycle(List<string> o)
        {
            double eps = 0.00001;
            VolumeCycleEngine vce = new VolumeCycleEngine(20, eps);
            vce.Reset(Orientation.Up);

            // Dom1: increasing vols
            Bar a = B(0, 10, 10.5, 9.5, 10.2, 1000);
            Bar b = B(1, 10.2, 10.7, 9.7, 10.4, 1200);
            Bar c = B(2, 10.4, 10.9, 9.9, 10.6, 1400);
            vce.OnBar(Orientation.Up, null, a); vce.OnBar(Orientation.Up, a, b); vce.OnBar(Orientation.Up, b, c);

            // NonDom: decreasing vols
            Bar d = B(3, 10.6, 10.8, 10.0, 10.3, 1300);
            Bar e = B(4, 10.3, 10.6, 10.0, 10.2, 1100);
            vce.OnBar(Orientation.Up, c, d); vce.OnBar(Orientation.Up, d, e);

            // Dom2: increasing vols again
            Bar f = B(5, 10.2, 10.9, 10.1, 10.8, 1150);
            Bar g = B(6, 10.8, 11.0, 10.6, 10.9, 1300);
            Bar h = B(7, 10.9, 11.2, 10.8, 11.1, 1500);
            vce.OnBar(Orientation.Up, e, f); vce.OnBar(Orientation.Up, f, g); vce.OnBar(Orientation.Up, g, h);

            if (vce.State.CurrentLeg == VcLeg.Dom2) Pass(o, "VC phase progression Dom1→NonDom→Dom2"); else Fail(o, "VC progression", vce.State.CurrentLeg.ToString());
            if (vce.State.Peak1 > 0 && vce.State.Peak2 > 0 && vce.State.ParityRatio > 0) Pass(o, "VC parity computed"); else Fail(o, "VC parity", "missing peaks");
        }
		
		private static void Test_Geometry_BuildTape_WithP3(List<string> o)
		{
		    // BuildTape (Up): RTL=low-low, LTL=high-high; WithP3 re-points RTL through P3
		    Bar b0 = B(0, 100.0, 101.0, 100.0, 100.5, 1000);
		    Bar b1 = B(1, 101.0, 102.0, 100.5, 101.4, 1100);
		
		    var geom = GeometryService.BuildTape(Orientation.Up, b0, b1);
		    bool ok1 = Math.Abs(geom.RtlAt(0) - b0.Low)  < 1e-9 && Math.Abs(geom.LtlAt(0) - b0.High) < 1e-9;
		    bool ok2 = Math.Abs(geom.RtlAt(1) - b1.Low)  < 1e-9 && Math.Abs(geom.LtlAt(1) - b1.High) < 1e-9;
		
		    // Re-point RTL with P3 at index 2
		    var geom2 = geom.WithP3(0, b0.Low, 2, 100.8);
		    bool ok3 = Math.Abs(geom2.RtlAt(2) - 100.8) < 1e-9;
		
		    if (ok1 && ok2 && ok3) Pass(o, "Geometry BuildTape + WithP3");
		    else Fail(o, "Geometry BuildTape + WithP3", "mismatch");
		}
		
		private static void Test_TapeGeometryBridge_Lifecycle(List<string> o)
		{
		    double eps = 1e-5;
		    var bars = new Dictionary<int, Bar>();
		
		    // Seed UP tape (HHHL)
		    Bar p0 = B(0, 100.2, 101.0, 100.0, 100.5, 1000); bars[p0.Index] = p0;
		    Bar c1 = B(1, 101.0, 101.6, 100.5, 101.2, 1100); bars[c1.Index] = c1; // HHHL vs p0
		
		    // After P1(0,100.0) & P2(1,100.5), RTL slope = +0.5 → RTL(2)=101.0
		    Bar c2 = B(2, 100.9, 101.2, 100.8, 101.05, 900);  bars[c2.Index] = c2; // wick below RTL, close above → modify (P3)
		    // WithP3 makes RTL from (0,100.0) to (2,100.8) → slope 0.4 → RTL(3)=101.2
		    Bar c3 = B(3, 101.0, 101.1, 100.6, 101.05, 1200); bars[c3.Index] = c3; // Close 101.05 < RTL(3)=101.2 → break
		
		    var bridge = new TapeGeometryBridge(eps, idx => bars.ContainsKey(idx) ? bars[idx] : null);
		
		    bool started=false, modified=false, broken=false; int geomUpdates=0;
		    bridge.TapeStarted     += (s,e) => started = true;
		    bridge.TapeModified    += (s,e) => modified = true;
		    bridge.TapeBroken      += (s,e) => broken  = true;
		    bridge.GeometryUpdated += (s,e) => geomUpdates++;
		
		    var t1 = BarClassifier.ClassifyTwoBar(p0, c1, eps);
		    bridge.OnBar(p0, c1, t1, VolumeColor.Black);
		
		    var t2 = BarClassifier.ClassifyTwoBar(c1, c2, eps);
		    bridge.OnBar(c1, c2, t2, VolumeColor.Black);
		
		    var t3 = BarClassifier.ClassifyTwoBar(c2, c3, eps);
		    bridge.OnBar(c2, c3, t3, VolumeColor.Red);
		
		    if (started && modified && broken && bridge.Geometry != null && geomUpdates >= 2)
		        Pass(o, "TapeGeometryBridge lifecycle (start→modify→break)");
		    else
		        Fail(o, "TapeGeometryBridge lifecycle", $"s={started} m={modified} b={broken} gu={geomUpdates}");
		}

		
		private static void Test_TraverseGeometryBridge_Lifecycle(List<string> o)
		{
		    double eps = 1e-5;
		    var bars = new Dictionary<int, Bar>();
		    // Provide P1/P2 bars for T1 (down traverse seed)
		    bars[10] = B(10, 105, 110, 100, 104, 1000);
		    bars[12] = B(12, 103, 109, 101, 102, 1000);
		
		    // Tape summaries for D-U-D to cause join
		    var d1 = new TapeSummary { Orientation = Orientation.Down, StartIndex = 10, EndIndex = 12, MaxHigh = 110, MinLow = 100 };
		    var u  = new TapeSummary { Orientation = Orientation.Up,   StartIndex = 13, EndIndex = 15, MaxHigh = 115, MinLow = 101 };
		    var d2 = new TapeSummary { Orientation = Orientation.Down, StartIndex = 16, EndIndex = 18, MaxHigh = 114, MinLow = 100.5 };
		
		    var bridge = new TraverseGeometryBridge(eps, idx => bars.ContainsKey(idx)? bars[idx] : null);
		
		    bool joined=false, started=false, p3=false, ftt=false, geomUpdated=false;
		    bridge.TraverseJoined   += (s,e) => joined = true;
		    bridge.TraverseStarted  += (s,e) => started = true;
		    bridge.TraverseP3       += (s,e) => p3 = true;
		    bridge.TraverseFtt      += (s,e) => ftt = true;
		    bridge.GeometryUpdated  += (s,e) => geomUpdated = true;
		
		    bridge.AddTape(d1);
		    bridge.AddTape(u);
		    bridge.AddTape(d2);  // join + started (+ geometry built)
		
		    // Synthesize P3 and FTT using current RTL
		    if (bridge.Geometry == null) { Fail(o, "TraverseGeometryBridge lifecycle", "no geometry"); return; }
		    double rtl = bridge.Geometry.RtlAt(19);
		
		    // Down traverse: P3 on High ≥ RTL; then FTT on Close > RTL (without LTL touch)
		    bridge.OnBar(B(19, rtl-0.1, rtl+0.05, rtl-0.5, rtl-0.05, 1000)); // P3
		    bridge.OnBar(B(20, rtl-0.1, rtl+0.5, rtl-0.05, rtl+0.2, 1100));  // FTT
		
		    if (joined && started && p3 && ftt && geomUpdated)
		        Pass(o, "TraverseGeometryBridge lifecycle (join→start→P3→FTT)");
		    else
		        Fail(o, "TraverseGeometryBridge lifecycle", $"j={joined} s={started} p3={p3} ftt={ftt} geo={geomUpdated}");
		}
		
		private static void Test_ChannelGeometryBridge_Lifecycle(List<string> o)
		{
		    double eps = 1e-5;
		    // Up channel: U-D-U traverses
		    var t1 = new TraverseSummary { Orientation = Orientation.Up,   StartIndex = 100, EndIndex = 105, MaxHigh = 120, MinLow = 110 };
		    var t2 = new TraverseSummary { Orientation = Orientation.Down, StartIndex = 106, EndIndex = 111, MaxHigh = 119, MinLow = 109 };
		    var t3 = new TraverseSummary { Orientation = Orientation.Up,   StartIndex = 112, EndIndex = 117, MaxHigh = 121, MinLow = 113 };
		
		    var bridge = new ChannelGeometryBridge(eps);
		
		    bool joined=false, started=false, p3=false, ftt=false, geomUpdated=false;
		    bridge.ChannelJoined   += (s,e) => joined = true;
		    bridge.ChannelStarted  += (s,e) => started = true;
		    bridge.ChannelP3       += (s,e) => p3 = true;
		    bridge.ChannelFtt      += (s,e) => ftt = true;
		    bridge.GeometryUpdated += (s,e) => geomUpdated = true;
		
		    bridge.AddTraverse(t1);
		    bridge.AddTraverse(t2);
		    bridge.AddTraverse(t3); // join + started (+ geometry built)
		
		    if (bridge.Geometry == null) { Fail(o, "ChannelGeometryBridge lifecycle", "no geometry"); return; }
		    double rtl = bridge.Geometry.RtlAt(118);
		
		    // Up channel: P3 on Low ≤ RTL; FTT on Close < RTL (before LTL touch)
		    bridge.OnBar(B(118, rtl, rtl+0.3, rtl-0.1, rtl+0.05, 1000));   // P3 (wick below RTL, close above)
		    bridge.OnBar(B(119, rtl-0.2, rtl-0.1, rtl-0.5, rtl-0.3, 1100)); // FTT (close < RTL)
		
		    if (joined && started && p3 && ftt && geomUpdated)
		        Pass(o, "ChannelGeometryBridge lifecycle (join→start→P3→FTT)");
		    else
		        Fail(o, "ChannelGeometryBridge lifecycle", $"j={joined} s={started} p3={p3} ftt={ftt} geo={geomUpdated}");
		}
		
		private static void Test_APVAEngine_TapeLifecycle(List<string> o)
		{
		    double eps = 1e-5;
		    var bars = new Dictionary<int, Bar>();
		
		    Bar p0 = B(0, 100.2, 101.0, 100.0, 100.5, 1000); bars[p0.Index] = p0;
		    Bar c1 = B(1, 101.0, 101.6, 100.5, 101.2, 1100); bars[c1.Index] = c1; // HHHL
		    Bar c2 = B(2, 100.9, 101.2, 100.8, 101.05, 900);  bars[c2.Index] = c2; // modify
		    // Break: same rationale as bridge test — close below RTL after P3
		    Bar c3 = B(3, 101.0, 101.1, 100.6, 101.05, 1200); bars[c3.Index] = c3;
		
		    var eng = new APVAEngine(eps, idx => bars.ContainsKey(idx) ? bars[idx] : null);
		
		    bool started=false, modified=false, broken=false;
		    eng.TapeStarted += (s,e) => started  = true;
		    eng.TapeModified+= (s,e) => modified = true;
		    eng.TapeBroken  += (s,e) => broken   = true;
		
		    eng.OnBar(p0); // prime
		    eng.OnBar(c1); // start
		    eng.OnBar(c2); // modify
		    eng.OnBar(c3); // break
		
		    if (started && modified && broken)
		        Pass(o, "APVAEngine tape lifecycle (start→modify→break)");
		    else
		        Fail(o, "APVAEngine tape lifecycle", $"s={started} m={modified} b={broken}");
		}
    }
}
















