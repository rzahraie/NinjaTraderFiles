#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using System.Windows.Media;

using NinjaTrader.Data;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xLogicPivotIndicator : Indicator
    {
        private enum RunDirection
        {
            None,
            Up,
            Down
        }

        // OUTPUT SERIES =======================================================
        private Series<int> pivotDirSeries;   // 1 = new UP run, -1 = new DOWN run, 0 = none
        private Series<int> runDirSeries;     // 1 = UP run, -1 = DOWN run, 0 = none
        private Series<int> pivotRunLenSeries; // length of run that ended at this pivot

        // INTERNAL STATE ======================================================
        private RunDirection runDir;
        private int          runStartIdx;
        private int          extremeIdx;      // highest-high bar (UP) or lowest-low bar (DOWN)
        private int          runLength;
        private bool         pendingOutside;
        private int          outsideIdx;      // last outside bar index

        private int          drawCounter;

        [Browsable(false)]
        [XmlIgnore]
        public Series<int> PivotDir
        {
            get { return pivotDirSeries; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<int> RunDir
        {
            get { return runDirSeries; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<int> PivotRunLen
        {
            get { return pivotRunLenSeries; }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name                     = "xLogicPivotIndicator";
                IsOverlay                = true;
                DrawOnPricePanel         = true;
                Calculate                = Calculate.OnBarClose;
                IsSuspendedWhileInactive = true;
            }
            else if (State == State.DataLoaded)
            {
                pivotDirSeries    = new Series<int>(this);
                runDirSeries      = new Series<int>(this);
                pivotRunLenSeries = new Series<int>(this);

                runDir         = RunDirection.None;
                runStartIdx    = -1;
                extremeIdx     = -1;
                runLength      = 0;
                pendingOutside = false;
                outsideIdx     = -1;
                drawCounter    = 0;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar == 0)
            {
                pivotDirSeries[0]    = 0;
                runDirSeries[0]      = 0;
                pivotRunLenSeries[0] = 0;
                return;
            }

            // Defaults for this bar
            pivotDirSeries[0]    = 0;
            pivotRunLenSeries[0] = 0;

            // Bar classification vs previous bar
            bool isInside  = High[0] <= High[1] && Low[0] >= Low[1];
            bool isOutside = High[0] >= High[1] && Low[0] <= Low[1];

            // Ignore inside bars for persistence
            if (isInside)
            {
                runDirSeries[0] = DirectionToInt(runDir);
                return;
            }

            // INITIAL RUN DETECTION ==========================================
            if (runDir == RunDirection.None)
            {
                if (High[0] > High[1] && Low[0] >= Low[1])
                {
                    runDir      = RunDirection.Up;
                    runStartIdx = CurrentBar - 1;
                    extremeIdx  = CurrentBar;
                    runLength   = 1;
                }
                else if (Low[0] < Low[1] && High[0] <= High[1])
                {
                    runDir      = RunDirection.Down;
                    runStartIdx = CurrentBar - 1;
                    extremeIdx  = CurrentBar;
                    runLength   = 1;
                }

                runDirSeries[0] = DirectionToInt(runDir);
                return;
            }

            // OUTSIDE BAR HANDLING ===========================================
            if (isOutside)
            {
                pendingOutside = true;
                outsideIdx     = CurrentBar;
                UpdateExtremeOnOutside();
                runDirSeries[0] = DirectionToInt(runDir);
                return;
            }

            if (pendingOutside)
            {
                bool reversalConfirmed = HandlePendingOutside();
                if (reversalConfirmed)
                {
                    runDirSeries[0] = DirectionToInt(runDir);
                    return;
                }
            }

            pendingOutside = false;
            outsideIdx     = -1;

            // NORMAL RUN / PIVOT LOGIC =======================================
            if (runDir == RunDirection.Up)
                HandleUpRun();
            else if (runDir == RunDirection.Down)
                HandleDownRun();

            runDirSeries[0] = DirectionToInt(runDir);
        }

        private int DirectionToInt(RunDirection dir)
        {
            switch (dir)
            {
                case RunDirection.Up:   return 1;
                case RunDirection.Down: return -1;
                default:                return 0;
            }
        }

        private void UpdateExtremeOnOutside()
        {
            if (extremeIdx < 0)
                return;

            int exBarsAgo = CurrentBar - extremeIdx;
            if (exBarsAgo < 0 || exBarsAgo > CurrentBar)
                return;

            if (runDir == RunDirection.Up)
            {
                if (High[0] > High[exBarsAgo])
                {
                    extremeIdx = CurrentBar;
                    runLength++;
                }
            }
            else if (runDir == RunDirection.Down)
            {
                if (Low[0] < Low[exBarsAgo])
                {
                    extremeIdx = CurrentBar;
                    runLength++;
                }
            }
        }

        private bool HandlePendingOutside()
        {
            if (outsideIdx < 0)
                return false;

            int outsideBarsAgo = CurrentBar - outsideIdx;
            if (outsideBarsAgo < 0 || outsideBarsAgo > CurrentBar)
                return false;

            double outsideHigh = High[outsideBarsAgo];
            double outsideLow  = Low[outsideBarsAgo];

            if (runDir == RunDirection.Up)
            {
                // Reversal only if price trades below low of outside bar
                if (Low[0] < outsideLow)
                {
                    ProcessReversal(RunDirection.Down);
                    return true;
                }
            }
            else if (runDir == RunDirection.Down)
            {
                // Reversal only if price trades above high of outside bar
                if (High[0] > outsideHigh)
                {
                    ProcessReversal(RunDirection.Up);
                    return true;
                }
            }

            return false;
        }

        private void HandleUpRun()
        {
            if (extremeIdx < 0)
                return;

            int exBarsAgo = CurrentBar - extremeIdx;
            if (exBarsAgo < 0 || exBarsAgo > CurrentBar)
                return;

            double extremeHigh = High[exBarsAgo];
            double extremeLow  = Low[exBarsAgo];

            // Extend up run (higher high, no lower low)
            if (High[0] > extremeHigh && Low[0] >= extremeLow)
            {
                extremeIdx = CurrentBar;
                runLength++;
                return;
            }

            // Reversal: trade below low of highest-high bar
            if (Low[0] < extremeLow)
            {
                ProcessReversal(RunDirection.Down);
                return;
            }
        }

        private void HandleDownRun()
        {
            if (extremeIdx < 0)
                return;

            int exBarsAgo = CurrentBar - extremeIdx;
            if (exBarsAgo < 0 || exBarsAgo > CurrentBar)
                return;

            double extremeLow  = Low[exBarsAgo];
            double extremeHigh = High[exBarsAgo];

            // Extend down run (lower low, no higher high)
            if (Low[0] < extremeLow && High[0] <= extremeHigh)
            {
                extremeIdx = CurrentBar;
                runLength++;
                return;
            }

            // Reversal: trade above high of lowest-low bar
            if (High[0] > extremeHigh)
            {
                ProcessReversal(RunDirection.Up);
                return;
            }
        }

        private void ProcessReversal(RunDirection newDirection)
        {
            int priorRunLength = runLength;

            // Always fire a pivot: we want EVERY structural reversal marked
            FirePivot(newDirection, priorRunLength);
        }

        private void FirePivot(RunDirection newDirection, int priorRunLength)
        {
            // Mark pivot in output series
            pivotDirSeries[0]    = DirectionToInt(newDirection);
            pivotRunLenSeries[0] = priorRunLength;

            // Draw on chart for visual confirmation
            drawCounter++;
            string tag = "LP_Pivot_" + drawCounter.ToString();

            if (newDirection == RunDirection.Up)
                Draw.TriangleUp(this, tag, false, 0, Low[0] - 2 * TickSize, Brushes.Lime);
            else if (newDirection == RunDirection.Down)
                Draw.TriangleDown(this, tag, false, 0, High[0] + 2 * TickSize, Brushes.Red);

            // Reset run to new direction starting here
            runDir      = newDirection;
            runStartIdx = CurrentBar;
            extremeIdx  = CurrentBar;
            runLength   = 1;
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xLogicPivotIndicator[] cachexLogicPivotIndicator;
		public xLogicPivotIndicator xLogicPivotIndicator()
		{
			return xLogicPivotIndicator(Input);
		}

		public xLogicPivotIndicator xLogicPivotIndicator(ISeries<double> input)
		{
			if (cachexLogicPivotIndicator != null)
				for (int idx = 0; idx < cachexLogicPivotIndicator.Length; idx++)
					if (cachexLogicPivotIndicator[idx] != null &&  cachexLogicPivotIndicator[idx].EqualsInput(input))
						return cachexLogicPivotIndicator[idx];
			return CacheIndicator<xLogicPivotIndicator>(new xLogicPivotIndicator(), input, ref cachexLogicPivotIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xLogicPivotIndicator xLogicPivotIndicator()
		{
			return indicator.xLogicPivotIndicator(Input);
		}

		public Indicators.xLogicPivotIndicator xLogicPivotIndicator(ISeries<double> input )
		{
			return indicator.xLogicPivotIndicator(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xLogicPivotIndicator xLogicPivotIndicator()
		{
			return indicator.xLogicPivotIndicator(Input);
		}

		public Indicators.xLogicPivotIndicator xLogicPivotIndicator(ISeries<double> input )
		{
			return indicator.xLogicPivotIndicator(input);
		}
	}
}

#endregion
