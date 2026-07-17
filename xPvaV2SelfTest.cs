#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class xPvaV2SelfTest : Indicator
    {
        private bool hasRun;

        [NinjaScriptProperty]
        [Display(Name = "Run On Load", GroupName = "Self Test", Order = 1)]
        public bool RunOnLoad { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "xPvaV2SelfTest";
                Description = "Runs APVA V2 model self-tests and prints results.";
                Calculate = Calculate.OnBarClose;
                IsOverlay = false;
                PaintPriceMarkers = false;
                IsSuspendedWhileInactive = true;
                PrintTo = PrintTo.OutputTab2;
                RunOnLoad = true;
            }
        }

        protected override void OnBarUpdate()
        {
            if (!RunOnLoad || hasRun || CurrentBar < 1)
                return;

            hasRun = true;
            IList<string> failures = xPvaV2ModelSelfTest.Run();
            if (failures.Count == 0)
            {
                Print("[APVA V2] self-test passed: " + xPvaV2ModelSelfTest.TestCount + " checks");
                return;
            }

            Print("[APVA V2] self-test failed: " + failures.Count + " failure(s) across " + xPvaV2ModelSelfTest.TestCount + " checks");
            foreach (string failure in failures)
                Print("[APVA V2] " + failure);
        }
    }
}
