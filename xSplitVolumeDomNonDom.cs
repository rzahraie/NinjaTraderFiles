#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class xSplitVolumeDomNonDom : Indicator
	{
        Series<double> Pbu;
        Series<double> Pbd;
        Series<double> Ovu;
        Series<double> Ovd;
        Series<double> Vou;
        Series<double> Vod;
        Series<double> Dominant;
        Series<double> NonDominant;
        Series<bool> Sync;
        Series<bool> NonSync;
        Series<double> NDA;
        Series<double> DOMA;
        Series<double> NDB;
        Series<double> DOMB;
        Series<double> PAR;
        Series<double> NDVoA;
        Series<double> NDVoB;
        Series<double> DOMVoA;
        Series<double> DOMVoB;



        protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Split volume for dominant and non-dominant";
				Name										= "xSplitVolumeDomNonDom";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
                BarsRequiredToPlot                          = 5;

                Pbu = new Series<double>(this);
                Pbd = new Series<double>(this);
                Ovu = new Series<double>(this);
                Ovd = new Series<double>(this);
                Vou = new Series<double>(this);
                Vod = new Series<double>(this);
                Dominant = new Series<double>(this);
                NonDominant = new Series<double>(this);
                Sync = new Series<bool>(this);
                NonSync = new Series<bool>(this);
                NDA = new Series<double>(this);
                DOMA = new Series<double>(this);
                NDB = new Series<double>(this);
                DOMB = new Series<double>(this);
                PAR = new Series<double>(this);
                NDVoA = new Series<double>(this);
                NDVoB = new Series<double>(this);
                DOMVoA = new Series<double>(this);
                DOMVoB = new Series<double>(this);

                AddPlot(new Stroke(Brushes.Blue, 2), PlotStyle.Bar, "TradeSignal");
                
            }
			else if (State == State.Configure)
			{
			}
		}

        private void CalculateDominantAndNonDominantVolume()
        {
            Pbu[0] = ((High[0] - Close[0])/((High[0] - Close[0]) + (Open[0] - Low[0]) + (Close[0] - Low[0])))*Volume[0];
            Pbd[0] = ((Close[0] - Low[0])/((Close[0] - Low[0]) + (High[0] - Open[0]) + (High[0] - Close[0]))) * Volume[0];

            Ovu[0] = ((Open[0] - Low[0])/((High[0] - Close[0]) + (Open[0] - Low[0]) + (Close[0] - Low[0]))) * Volume[0];
            Ovd[0] = ((High[0] - Open[0])/((High[0] - Close[0]) + (Open[0] - Low[0]) + (Close[0] - Low[0]))) * Volume[0];

            Vou[0] = ((Close[0] - Low[0])/((High[0] - Close[0]) + (Open[0] - Low[0]) + (Close[0] - Low[0]))) * Volume[0];
            Vod[0] = ((High[0] - Close[0])/((High[0] - Close[0]) + (Open[0] - Low[0]) + (Close[0] - Low[0]))) * Volume[0];

            bool upTrend = (High[0] > High[1]) && (Low[0] > Low[1])  ? true : false;
            bool downTrend = (Low[0] < Low[1]) && (High[0] < High[1])   ? true : false;

            bool outsideBar = (upTrend & downTrend) ? true : false;

            bool closeInsidePrevBarUp = (upTrend && (Close[0] <= High[1]));
            bool closeInsidePrevBarDown = (downTrend && (Close[0] >= Low[1]));

            if (upTrend & !outsideBar)
            {
                Dominant[0] = Vou[0];
                NonDominant[0] = Pbu[1] + Ovu[0];
            }
            else if (downTrend & !outsideBar)
            {
                Dominant[0] = Vod[0];
                NonDominant[0] = Pbd[1] + Ovd[0];
            }

            if (closeInsidePrevBarUp)
            {
                NDVoA[0] = High[1] - Open[0];
                DOMVoA[0] = High[0] - Open[0];

                NDVoB[0] = DOMVoA[0];
                DOMVoB[0] = High[0] - Low[0];
            }
            else if (closeInsidePrevBarDown)
            {
                NDVoA[0] = Open[0] - Low[1];
                DOMVoA[0] = Open[0] - Low[0];

                NDVoB[0] = DOMVoA[0];
                DOMVoB[0] = High[0] - Low[0];
            }

            PAR[0] = High[1] - Low[1];

            NDA[0] = (NDVoA[0] / (NDVoA[0] + DOMVoA[0])) * (PAR[0] / DOMVoA[0]) * (DOMVoA[0] / (DOMVoA[0] + DOMVoB[0])) * (PAR[0] / DOMVoB[0]) * Volume[0];
            DOMA[0] = (DOMVoA[0] / (NDVoA[0] + DOMVoA[0])) * (PAR[0] / DOMVoA[0]) * (DOMVoA[0] / (DOMVoA[0] + DOMVoB[0])) * (PAR[0] / DOMVoB[0]) * Volume[0];

            if ((closeInsidePrevBarUp) || (closeInsidePrevBarDown))
            {
                Dominant[0] = DOMA[0];
                NonDominant[0] = NDA[0];
            }
        }

        private void GenerateSignals()
        {
            CalculateDominantAndNonDominantVolume();

            bool syncPart1Inc = (NonDominant[0] > NonDominant[1]) && (Dominant[0] > Dominant[1]);
            bool syncPart2Dec = (NonDominant[0] < NonDominant[1]) && (Dominant[0] < Dominant[1]);

            bool notSyncPart1Inc = (NonDominant[0] < NonDominant[1]) && (Dominant[0] > Dominant[1]);
            bool notSyncPart2Dec = (NonDominant[0] > NonDominant[1]) && (Dominant[0] < Dominant[1]);

            Sync[0] = syncPart1Inc || syncPart2Dec;
            NonSync[0] = notSyncPart1Inc || notSyncPart2Dec;

            bool signal = ((NonSync[2] && NonSync[1]  && Sync[0]) || (Sync[2]  && Sync[1] && NonSync[0]));

            bool Highhigh5 = (High[4] > High[3]) && (High[3] > High[2]) && (High[2] > High[1]) && (High[1] > High[0]);
            bool HighLow5 = (Low[4] > Low[3]) && (Low[3] > Low[2]) && (Low[2] > Low[1]) && (Low[1] > Low[0]);
            bool HigherHighHigherLowBar = Highhigh5 && HighLow5;

            bool Lowhhigh5 = (High[4] < High[3]) && (High[3] < High[2]) && (High[2] < High[1]) && (High[1] < High[0]);
            bool LowLow5 = (Low[4] < Low[3]) && (Low[3] < Low[2]) && (Low[2] < Low[1]) && (Low[1] < Low[0]);
            bool LowerLowLowerHighBar = Lowhhigh5 && LowLow5;

            TradeSignal[0] = (HigherHighHigherLowBar || LowerLowLowerHighBar) && signal ? 1 : 0;
        }

		protected override void OnBarUpdate()
		{
            //Add your custom indicator logic here.
            if (CurrentBar < 4) return;

            GenerateSignals();
        }

		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> PBU
		{
			get { return Pbu; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> PBD
		{
			get { return Pbd; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> OVU
		{
			get { return Ovu; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> OVD
		{
			get { return Ovd; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> VOU
		{
			get { return Vou; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> VOD
		{
			get { return Vod; }
		}

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> DOMINANT
        {
            get { return Dominant; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> NON_DOMINANT
        {
            get { return NonDominant; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<bool> SYNC
        {
            get { return Sync; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<bool> NON_SYNC
        {
            get { return NonSync; }
        }

        [Browsable(false)]
        [XmlIgnore]
        public Series<double> TradeSignal
        {
            get { return Values[0]; }
        }

        #endregion

    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private xSplitVolumeDomNonDom[] cachexSplitVolumeDomNonDom;
		public xSplitVolumeDomNonDom xSplitVolumeDomNonDom()
		{
			return xSplitVolumeDomNonDom(Input);
		}

		public xSplitVolumeDomNonDom xSplitVolumeDomNonDom(ISeries<double> input)
		{
			if (cachexSplitVolumeDomNonDom != null)
				for (int idx = 0; idx < cachexSplitVolumeDomNonDom.Length; idx++)
					if (cachexSplitVolumeDomNonDom[idx] != null &&  cachexSplitVolumeDomNonDom[idx].EqualsInput(input))
						return cachexSplitVolumeDomNonDom[idx];
			return CacheIndicator<xSplitVolumeDomNonDom>(new xSplitVolumeDomNonDom(), input, ref cachexSplitVolumeDomNonDom);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.xSplitVolumeDomNonDom xSplitVolumeDomNonDom()
		{
			return indicator.xSplitVolumeDomNonDom(Input);
		}

		public Indicators.xSplitVolumeDomNonDom xSplitVolumeDomNonDom(ISeries<double> input )
		{
			return indicator.xSplitVolumeDomNonDom(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.xSplitVolumeDomNonDom xSplitVolumeDomNonDom()
		{
			return indicator.xSplitVolumeDomNonDom(Input);
		}

		public Indicators.xSplitVolumeDomNonDom xSplitVolumeDomNonDom(ISeries<double> input )
		{
			return indicator.xSplitVolumeDomNonDom(input);
		}
	}
}

#endregion
