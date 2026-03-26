namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualContainerAnalyzer
    {
        public static ManualContainerAnalysis Analyze(
		    ManualContainerSnapshot snapshot,
		    System.Func<int, double> getOpen,
		    System.Func<int, double> getHigh,
		    System.Func<int, double> getLow,
		    System.Func<int, double> getClose,
		    System.Func<int, long> getVolume,
		    double tickSize)
        {
            var volumeEvents =
			    xPvaManualVolumeAnalyzer.Analyze(
			        snapshot.P1.BarIndex,
			        snapshot.P3.BarIndex,
			        getVolume,
			        getOpen,
			        getClose,
			        snapshot.IsUpContainer);
			
			var volumeSequence = 
				xPvaManualVolumeSequencer.Build(
					snapshot.ContainerId,
				 	volumeEvents);
			
			var volumeState =
			    xPvaManualVolumeStateClassifier.Classify(
			        volumeSequence);

            int? candidateBar = null;
            int? confirmedBar = null;
            StructureState? structureState = null;
            ActionType? actionType = null;
            TradeIntent? tradeIntent = null;

            if (snapshot.P3.BarIndex > snapshot.P1.BarIndex)
            {
                double p1Price = snapshot.P1.Price;
                double p2Price = snapshot.P2.Price;
                double p3Price = snapshot.P3.Price;

                int p1Idx = snapshot.P1.BarIndex;
                int p2Idx = snapshot.P2.BarIndex;
                int p3Idx = snapshot.P3.BarIndex;

                if (p3Idx > p1Idx)
                {
                    var gp1 = new GeometryPoint(p1Idx, p1Price);
                    var gp2 = new GeometryPoint(p2Idx, p2Price);
                    var gp3 = new GeometryPoint(p3Idx, p3Price);

                    var rtl = new LineDef(gp1, gp3);

                    var ltlB = new GeometryPoint(
                        gp2.BarIndex + 1,
                        gp2.Price + snapshot.LtlSlope);

                    var ltl = new LineDef(gp2, ltlB);

                    for (int idx = p3Idx + 1; idx <= p3Idx + 5000; idx++)
                    {
                        double high;
                        double low;
                        double close;

                        try
                        {
                            high = getHigh(idx);
                            low = getLow(idx);
                            close = getClose(idx);
                        }
                        catch
                        {
                            break;
                        }

                        double ltlNow = ltl.ValueAt(idx);
                        double tolerance = snapshot.BreakToleranceTicks * tickSize;

                        bool broke;

                        if (snapshot.BreakMode == ManualContainerBreakMode.CloseCross)
                        {
                            broke =
                                snapshot.IsUpContainer
                                    ? close < (ltlNow - tolerance)
                                    : close > (ltlNow + tolerance);
                        }
                        else
                        {
                            broke =
                                snapshot.IsUpContainer
                                    ? low < (ltlNow - tolerance)
                                    : high > (ltlNow + tolerance);
                        }

                        if (!broke)
                            continue;

                        candidateBar = idx;

                        int confirmIdx = idx + 1;
                        double confirmHigh;
                        double confirmLow;
                        double confirmClose;

                        try
                        {
                            confirmHigh = getHigh(confirmIdx);
                            confirmLow = getLow(confirmIdx);
                            confirmClose = getClose(confirmIdx);
                        }
                        catch
                        {
                            break;
                        }

                        double confirmLtl = ltl.ValueAt(confirmIdx);
                        double confirmTolerance = snapshot.BreakToleranceTicks * tickSize;

                        bool confirmBroke;

                        if (snapshot.BreakMode == ManualContainerBreakMode.CloseCross)
                        {
                            confirmBroke =
                                snapshot.IsUpContainer
                                    ? confirmClose < (confirmLtl - confirmTolerance)
                                    : confirmClose > (confirmLtl + confirmTolerance);
                        }
                        else
                        {
                            confirmBroke =
                                snapshot.IsUpContainer
                                    ? confirmLow < (confirmLtl - confirmTolerance)
                                    : confirmHigh > (confirmLtl + confirmTolerance);
                        }

                        if (confirmBroke)
                        {
                            confirmedBar = confirmIdx;
                            structureState = Engine.StructureState.Broken;
                            actionType = Engine.ActionType.Sideline;
                            tradeIntent = Engine.TradeIntent.Sideline;
                        }

                        break;
                    }
                }
            }

            return new ManualContainerAnalysis(
                snapshot,
                volumeEvents,
				volumeSequence,
				volumeState,
                candidateBar,
                confirmedBar,
                structureState,
                actionType,
                tradeIntent);
        }
    }
}





