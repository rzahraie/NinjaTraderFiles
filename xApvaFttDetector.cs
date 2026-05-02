using System.Collections.Generic;

namespace APVA.Core
{
    public sealed class FttResult
    {
        public bool IsCandidate { get; set; }
        public bool IsConfirmed { get; set; }

        public int SegmentIndex { get; set; } = -1;
        public int BarIndex { get; set; } = -1;

        public string Reason { get; set; } = string.Empty;
    }

    public static class xApvaFttDetector
    {
        public static FttResult Detect(
            IReadOnlyList<VolumeSegment> segments,
            bool hasValidP3,
            bool expectedContinuationFailed)
        {
            var result = new FttResult();

            if (segments == null || segments.Count == 0)
                return result;

            for (int i = 0; i < segments.Count; i++)
            {
                VolumeSegment segment = segments[i];

                if (segment.Phase != VolumePhase.T2F)
                    continue;

                result.IsCandidate = true;
                result.SegmentIndex = i;
                result.BarIndex = segment.EndIndex;
                result.Reason = "T2F detected after dominance expectation.";

                if (hasValidP3 && expectedContinuationFailed)
                {
                    result.IsConfirmed = true;
                    result.Reason = "T2F plus valid P3 and failed structural continuation.";
                }

                return result;
            }

            return result;
        }
    }
}