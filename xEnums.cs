using System;
using System.ComponentModel;

namespace NinjaTrader
{
//
		public enum xPeakVolumePlotTypesEnums
		{
			PriceHighLow,
			PeakRange,
			Volume,
			VolumeIncreaseOnly,
			QMFIVolumeIncrease
		};
		
		public enum xDerivativeCalcTypesEnums
		{
			Distance,
			Velocity
		};
	
		public enum xDerivativeDataInputTypesEnums
		{
			Volume,
			BarVolatility,
			QMFI,
			QMFIBarVolatility
		};
		
		public enum xBarTypesEnum
		{
			HigherHighHigherLow,
			LowerLowLowerHigh,
			StitchLong,
			StitchShort,
			FlatTopPennant,
			FlatBottomPennant,
			Inside,
			Hitch,
			OutSideBarLong,
			OutSideBarShort,
			Undefined
		};
		
		public enum xBarCloseTypesEnums
		{
			HigherClose,
			LowerClose,
			EqualClose,
			Undefined
		};
		
		public enum xBarBodyTypesEnums
		{
			Green,
			Red,
			Doji,
			Undefined
		};
		
		public enum xBarPartTypesEnums
		{
			OnePartFullHigh,
			OnePartFullLow,
			TwoPartDojiHigh,
			TwoPartDojiLow,
			TwoPartOpenHigh,
			TwoPartOpenLow,
			TwoPartCloseHigh,
			TwoPartCloseLow,
			ThreePartHigh,
			ThreePartLow,
			ThreePartDoji,
			BarPartUnknown
		};
	
		public enum xPriceVolumeTypeEnum
		{
			Open,
			High,
			Low,
			Close,
			Typical,
			Mid,
			HighLow,
			Volume,
			Undefined
		};
		
		public enum xVolumeColorEnums
		{
			Up,
			Down,
			Inside,
			OutSideUp,
			OutSideDown,
			ReversalUp,
			ReversalDown,
			Undefined
		};
		
		public enum xVolumeColorShowEnums
		{
			Black,
			Red,
			Both
		};
		
		public enum xVolumeTypesEnums
		{
			HighLow,
			HighLowHigh,
			CloseToClose,
			CloseToCloseStrict,
			CloseToCloseStrictHigh,
			BarVolatilityCloseToClose,
			BarVolatilityHighLow,
			BarVolatilityHighLowUp,
			UpDownSide,
			UpDownSideStitch,
			UpDownSideStitchHigh,
			UpDownSideStitchOutSide,
			UpDownTrue,
			UpDownTrueClose,
			UpDownTrueStrict,
			UpDownSideStitchHighBlack,
			UpDownSideStitchHighRed,
			HighLowHighBlack,
			HighLowHighRed,
			BarHighLowVelocity,
			BarVolatilityOpenCloseUp,
			UpDownTrueEx,
			OpenToClose,
			OpenToCloseHigh,
			IntarBarNonDominant,
			IntraBarDominant
		};

        public enum xSplitVolumeTypesEnums
        {
            PreviousCurrentHighLow,
            IntraPartPath,
            BarPartsPercentage
        };
		
		
		public enum xVolumeColorStateEnums 
		{ 
			[Description("B+")]
			IncreasingBlack,
			[Description("B-")]
			DecreasinBlack,
			[Description("B*+")]
			IncreasingReversalBlack,
			[Description("B*-")]
			DecreasingReversalBlack,
			[Description("B^+")]
			IncreasingOutSideBlack,
			[Description("B`+")]
			IncreasingStichBlack,
			[Description("B`-")]
			DecreasingStichBlack,
			[Description("B'+")]
			IncreasingReversalStichBlack,
			[Description("B'-")]
			DecreasingReversalStichBlack,
			[Description("B^-")]
			DecreasingOutSideBlack,
			[Description("b")]
			InsideBlack,
			[Description("R+")]
			IncreasingRed, 
			[Description("R-")]
			DecreasingRed,
			[Description("R*+")]
			IncreasingReversalRed,
			[Description("R*-")]
			DecreasingReversalRed,
			[Description("R`+")]
			IncreasingStitchRed,
			[Description("R`-")]
			DecreasingStitchRed,
			[Description("R'+")]
			IncreasingReversalStitchRed,
			[Description("R'-")]
			DecreasingReversalStitchRed,
			[Description("R^+")]
			IncreasingOutSideRed,
			[Description("R^-")]
			DecreasingOutSideRed,
			[Description("r")]
			InsideRed,
			[Description("i")]
			Inside,
			[Description("Undefined")]
			Undefined
		};
		
		public enum xBarDifference
		{
			HighToLow,
			LowToHigh,
			CloseToClose
		};
		
		public enum xLateralMovementSimpleCloseEnums
		{
			NO_STATE = -1,
			BROKEN_ABOVE = 0,
			BROKEN_BELOW = 1,
			INTACT = 2	
		};
		
		public enum xLateralStateEnums
		{
			NO_STATE = -1,
			BROKEN_ABOVE = 0,
			BROKEN_BELOW = 1,
			INTACT = 2
		};
		
		public enum xLateralPiercedStateEnums
		{
			NO_STATE = -1,
			PIERCED_ABOVE = 2,
			PIERCED_BELOW = 3
		};
		
		public enum xSignalTypesEnums
		{
			TWO_BAR_SIGNALS,
			TWO_BAR_SIMPLE_SIGNALS,
			HIGH_VOLUME_HIGH_VOLATILITY_SIGNALS,
			VOLATILITY_CYCLE_ONLY_SIGNALS,
			THREE_BAR_SIGNALS,
			COMPLETE_VOLUME_CYCLE,
			VOLUME_CLOSE_HL_VOLAT_SIGNALS,
			VOLUME_CLOSE_HI_VOLAT_SIGNALS_MFI,
			DOJI,
			VOLUME_LEVEL_SIGNAL,
			BAR_PART_SYSTEM,
			VOLUME_ACCEL
		};
		
		public enum xSeriesInputType
		{
			Volume,
			BarVolatility,
			BarVolatilityCloseToClose,
			TypPriceXVol
		};
		
		
}

