using System;
using System.Collections.Generic;

namespace NinjaTrader.NinjaScript.APVA.V01
{
    public sealed class ApvaV01StateEngine
    {
		private const int VolumeWindowLength = 20;
		private readonly Queue<double> volumeWindow = new Queue<double>();
		private double volumeWindowSum;

        public ApvaStateSnapshot BuildSnapshot(
            ApvaBarFeatures features,
            ApvaSequenceState sequence,
            ApvaScores scores,
            ApvaStateSnapshot priorState,
			ApvaBarFeatures priorFeatures)
        {
            var snapshot = new ApvaStateSnapshot
            {
                BarIndex = features != null ? features.BarIndex : 0,
                Time = features != null ? features.Time : default(System.DateTime),

                Scores = scores ?? new ApvaScores(),

                SequencePhase = sequence != null
                    ? sequence.Phase
                    : ApvaSequencePhase.Unknown,

                SequenceAuthority = sequence != null
                    ? sequence.AuthorityScore
                    : 0.0,

                MaturityLevel = sequence != null
                    ? sequence.Maturity
                    : ApvaMaturityLevel.Unknown,

                ActiveDirection = sequence != null
                    ? sequence.Direction
                    : ApvaDirection.Unknown
            };
			
			ComputeVolumeExportFields(snapshot, features, priorFeatures);
			ComputeEnergyScores(snapshot, features, priorState);

            snapshot.MacroState = ClassifyMacroState(snapshot, priorState);
            snapshot.SFCStatus = ClassifySfcStatus(snapshot);
			
            NormalizeMacroState(snapshot);
			return snapshot;
        }

		public void Reset()
		{
			volumeWindow.Clear();
			volumeWindowSum = 0.0;
		}

		private void ComputeVolumeExportFields(
			ApvaStateSnapshot snapshot,
			ApvaBarFeatures features,
			ApvaBarFeatures priorFeatures)
		{
			if (snapshot == null || features == null)
				return;

			double volume = features.Volume;
			snapshot.Volume = volume;

			volumeWindow.Enqueue(volume);
			volumeWindowSum += volume;
			if (volumeWindow.Count > VolumeWindowLength)
				volumeWindowSum -= volumeWindow.Dequeue();

			snapshot.VolumeSMA = volumeWindow.Count > 0
				? volumeWindowSum / volumeWindow.Count
				: 0.0;
			snapshot.RelativeVolume = snapshot.VolumeSMA > 0.0
				? volume / snapshot.VolumeSMA
				: 0.0;

			double sampleVariance = 0.0;
			if (volumeWindow.Count > 1)
			{
				foreach (double observedVolume in volumeWindow)
				sampleVariance += Math.Pow(observedVolume - snapshot.VolumeSMA, 2.0);

				sampleVariance /= volumeWindow.Count - 1;
			}

			double sampleStdDev = sampleVariance > 0.0
				? Math.Sqrt(sampleVariance)
				: 0.0;
			snapshot.VolumeZScore = sampleStdDev > 0.0
				? (volume - snapshot.VolumeSMA) / sampleStdDev
				: 0.0;

			if (features.Close > features.Open)
			{
				snapshot.BarDirection = "Up";
				snapshot.SignedVolume = volume;
				snapshot.UpVolume = volume;
			}
			else if (features.Close < features.Open)
			{
				snapshot.BarDirection = "Down";
				snapshot.SignedVolume = -volume;
				snapshot.DownVolume = volume;
			}
			else
			{
				snapshot.BarDirection = "Flat";
				snapshot.FlatVolume = volume;
			}

			snapshot.UpDownVolumeDelta = snapshot.UpVolume - snapshot.DownVolume;
			ComputeSpyderPreviousCurrentHighLow(snapshot, features, priorFeatures);
		}

		private static void ComputeSpyderPreviousCurrentHighLow(
			ApvaStateSnapshot snapshot,
			ApvaBarFeatures features,
			ApvaBarFeatures priorFeatures)
		{
			if (priorFeatures == null)
			{
				snapshot.SpyderDominantVolume = snapshot.Volume;
				snapshot.SpyderNonDominantVolume = 0.0;
				snapshot.SpyderDominantVolumeShare = 1.0;
				snapshot.SpyderNonDominantVolumeShare = 0.0;
				snapshot.SpyderNonDominantColor = "Unknown";
				snapshot.SpyderSplitMethod = "PreviousCurrentHighLow_NoPrior";
				return;
			}

			double dominantLegDistance = Math.Max(0.0, Math.Abs(features.High - features.Low));
			double nonDominantLegDistance = 0.0;
			string nonDominantColor;

			if (features.High > priorFeatures.High && features.Low > priorFeatures.Low)
			{
				nonDominantLegDistance = priorFeatures.High - features.Low;
				nonDominantColor = "Red";
			}
			else if (features.High < priorFeatures.High && features.Low < priorFeatures.Low)
			{
				nonDominantLegDistance = features.High - priorFeatures.Low;
				nonDominantColor = "Black";
			}
			else if (features.High > priorFeatures.High && features.Low == priorFeatures.Low)
			{
				nonDominantLegDistance = priorFeatures.High - features.Low;
				nonDominantColor = "Black";
			}
			else if (features.High == priorFeatures.High && features.Low < priorFeatures.Low)
			{
				nonDominantLegDistance = features.High - priorFeatures.Low;
				nonDominantColor = "Red";
			}
			else if (features.High == priorFeatures.High && features.Low > priorFeatures.Low)
			{
				nonDominantLegDistance = priorFeatures.High - features.Low;
				nonDominantColor = "Red";
			}
			else if (features.High < priorFeatures.High && features.Low == priorFeatures.Low)
			{
				nonDominantLegDistance = features.High - priorFeatures.Low;
				nonDominantColor = "Black";
			}
			else if (features.High < priorFeatures.High && features.Low > priorFeatures.Low)
			{
				if (features.Close > features.Open)
				{
					nonDominantLegDistance = priorFeatures.High - features.Low;
					nonDominantColor = "Red";
				}
				else
				{
					nonDominantLegDistance = features.High - priorFeatures.Low;
					nonDominantColor = "Black";
				}
			}
			else if (features.High > priorFeatures.High && features.Low < priorFeatures.Low &&
				features.Close >= priorFeatures.Close)
			{
				nonDominantLegDistance = priorFeatures.High - features.Low;
				nonDominantColor = "Red";
			}
			else if (features.High > priorFeatures.High && features.Low < priorFeatures.Low)
			{
				nonDominantLegDistance = features.High - priorFeatures.Low;
				nonDominantColor = "Black";
			}
			else
			{
				nonDominantColor = features.Close < features.Open ? "Red" : "Black";
			}

			nonDominantLegDistance = Math.Max(0.0, nonDominantLegDistance);
			double totalDistance = dominantLegDistance + nonDominantLegDistance;

			if (totalDistance <= 0.0)
			{
				snapshot.SpyderDominantVolume = snapshot.Volume;
				snapshot.SpyderNonDominantVolume = 0.0;
			}
			else
			{
				snapshot.SpyderNonDominantVolume =
					snapshot.Volume * nonDominantLegDistance / totalDistance;
				snapshot.SpyderDominantVolume =
					snapshot.Volume * dominantLegDistance / totalDistance;
			}

			snapshot.SpyderDominantVolumeShare = snapshot.Volume > 0.0
				? snapshot.SpyderDominantVolume / snapshot.Volume
				: 0.0;
			snapshot.SpyderNonDominantVolumeShare = snapshot.Volume > 0.0
				? snapshot.SpyderNonDominantVolume / snapshot.Volume
				: 0.0;
			snapshot.SpyderNonDominantColor = nonDominantColor;
			snapshot.SpyderSplitMethod = "PreviousCurrentHighLow";
		}

		private static void ComputeEnergyScores(
		    ApvaStateSnapshot snapshot,
		    ApvaBarFeatures features,
		    ApvaStateSnapshot priorState)
		{
		    if (snapshot == null || snapshot.Scores == null || features == null)
		        return;
		
		    double overlap = Clamp01(features.OverlapRatio);
		    double narrowBody = Clamp01(1.0 - features.BodyToRangeRatio);
		    double ambiguity = Clamp01(snapshot.Scores.AmbiguityScore);
		    double degradation = Clamp01(snapshot.Scores.DegradationScore);
		    double dominance = Clamp01(snapshot.Scores.DominanceScore);
		    double balance = Clamp01(snapshot.Scores.BalanceScore);
		
		    double priorCompression =
		        priorState != null && priorState.Scores != null
		            ? priorState.Scores.CompressionScore
		            : 0.0;
		
		    double rawCompression =
		        0.35 * overlap +
		        0.25 * narrowBody +
		        0.20 * ambiguity +
		        0.20 * balance;
		
		    // Persistent unresolved/balance should allow compression to accumulate.
		    if (priorState != null &&
		        (priorState.MacroState == ApvaMacroState.Unresolved ||
		         priorState.MacroState == ApvaMacroState.Balance))
		    {
		        rawCompression =
		            0.70 * rawCompression +
		            0.30 * priorCompression;
		    }
		
		    // Heavy degradation means this is not clean compression.
		    rawCompression *= (1.0 - 0.35 * degradation);
		
		    double rawExpansion =
		        0.45 * dominance +
		        0.25 * snapshot.SequenceAuthority +
		        0.20 * features.BodyToRangeRatio +
		        0.10 * (1.0 - overlap);
		
		    // Compression can fuel expansion, but only if degradation is not dominant.
		    if (priorCompression >= 0.45 && degradation < 0.60)
		        rawExpansion += 0.15 * priorCompression;
		
		    snapshot.Scores.CompressionScore = Clamp01(rawCompression);
		    snapshot.Scores.ExpansionPressure = Clamp01(rawExpansion);
			
			double structuralCompression =
			    0.40 * balance +
			    0.30 * (1.0 - degradation) +
			    0.20 * snapshot.SequenceAuthority +
			    0.10 * overlap;
			
			double entropicCompression =
			    0.40 * ambiguity +
			    0.30 * degradation +
			    0.20 * overlap +
			    0.10 * (1.0 - snapshot.SequenceAuthority);
			
			snapshot.Scores.StructuralCompression =
			    Clamp01(structuralCompression);
			
			snapshot.Scores.EntropicCompression =
			    Clamp01(entropicCompression);
			
			double incubationQuality =
			    snapshot.Scores.StructuralCompression
			    - snapshot.Scores.EntropicCompression
			    + snapshot.Scores.ExpansionPressure;
			
			snapshot.Scores.IncubationQuality =
			    Clamp01(incubationQuality);
		}
		
		private static double Clamp01(double value)
		{
		    if (value < 0.0)
		        return 0.0;
		
		    if (value > 1.0)
		        return 1.0;
		
		    return value;
		}

        private static ApvaMacroState ClassifyMacroState(
            ApvaStateSnapshot current,
            ApvaStateSnapshot prior)
        {
            var s = current.Scores;

            if (s.AmbiguityScore >= 0.65)
                return ApvaMacroState.Unresolved;

            if (s.TransitionScore >= 0.65 &&
                s.DegradationScore >= 0.45)
                return ApvaMacroState.TransitionAttempt;

            if (s.BalanceScore >= 0.60)
                return ApvaMacroState.Balance;

            if (s.DegradationScore >= 0.55 &&
                s.DominanceScore >= 0.30)
                return ApvaMacroState.Degrading;

            if (s.DominanceScore >= 0.45)
                return ApvaMacroState.Directional;

            if (prior != null &&
                prior.MacroState == ApvaMacroState.Directional &&
                s.DominanceScore >= 0.35)
                return ApvaMacroState.Directional;
			
			if (s.DegradationScore >= 0.45 ||
			    s.BalanceScore >= 0.40 ||
			    s.AmbiguityScore >= 0.40)
			    return ApvaMacroState.Unresolved;
			
			if (prior != null &&
			    prior.SponsorState == ApvaSponsorState.Reasserting &&
			    current.ActiveDirection == prior.ActiveDirection)
			{
			    return ApvaMacroState.Unresolved;
			}
			
			if (current.Events != null)
			{
			    foreach (var e in current.Events)
			    {
			        if (e.EventType == ApvaEventType.ReclaimAttempt ||
			            e.EventType == ApvaEventType.RejectedReclaim ||
			            e.EventType == ApvaEventType.AcceptedReclaim)
			            return ApvaMacroState.Unresolved;
			    }
			}

            return ApvaMacroState.Unknown;
        }

        private static string ClassifySfcStatus(ApvaStateSnapshot current)
        {
            var s = current.Scores;

            if (s.DegradationScore >= 0.65 &&
                s.TransitionScore >= 0.45)
                return "Confirmed Structural";

            if (s.DegradationScore >= 0.50)
                return "Candidate";

            return "None";
        }
		
		private static void NormalizeMacroState(ApvaStateSnapshot snapshot)
		{
		    if (snapshot == null)
		        return;
		
		    if (snapshot.MacroState != ApvaMacroState.Unknown)
		        return;
		
		    if (snapshot.Scores == null)
		        return;
		
		    bool enoughAuctionEvidence =
		        snapshot.Scores.DominanceScore >= 0.20 ||
		        snapshot.Scores.DegradationScore >= 0.20 ||
		        snapshot.Scores.BalanceScore >= 0.20 ||
		        snapshot.Scores.TransitionScore >= 0.10 ||
		        snapshot.Scores.AmbiguityScore >= 0.20;
		
		    if (enoughAuctionEvidence)
		        snapshot.MacroState = ApvaMacroState.Unresolved;
			
			if (snapshot.MacroState == ApvaMacroState.Unresolved &&
			    snapshot.Scores.DominanceScore >= 0.40 &&
			    snapshot.Scores.DegradationScore < 0.45 &&
			    snapshot.Scores.AmbiguityScore < 0.25 &&
			    snapshot.SequenceAuthority >= 0.67)
			{
			    snapshot.MacroState = ApvaMacroState.Directional;
			    return;
			}
		}
    }
}









