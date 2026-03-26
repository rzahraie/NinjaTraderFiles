namespace NinjaTrader.NinjaScript.xPva.Engine
{
    public static class xPvaManualVolumeSequencer
    {
        public static ManualVolumeSequence Build(
            int containerId,
            ManualVolumeEvent[] events)
        {
            if (events == null || events.Length == 0)
            {
                return new ManualVolumeSequence(
                    containerId,
                    "NONE",
                    false,
                    false,
                    false,
					0);
            }

            var parts = new System.Collections.Generic.List<string>();

            bool sawDominant = false;
            bool sawNonDominantAfterDominant = false;
            bool sawDominantAfterNonDominant = false;
			int flipCount = 0;

            DominanceType? prevDom = null;

            for (int i = 0; i < events.Length; i++)
            {
                var e = events[i];
				

                string token = e.Label.ToString();

                if (e.Dominance == DominanceType.Dominant)
                    token += ":D";
                else if (e.Dominance == DominanceType.NonDominant)
                    token += ":N";
                else
                    token += ":U";

                parts.Add(token);

                if (e.Dominance == DominanceType.Dominant)
                    sawDominant = true;

                if (prevDom != e.Dominance)
				{
				    flipCount++;
				
				    if (prevDom == DominanceType.Dominant &&
				        e.Dominance == DominanceType.NonDominant)
				    {
				        sawNonDominantAfterDominant = true;
				    }
				
				    if (prevDom == DominanceType.NonDominant &&
				        e.Dominance == DominanceType.Dominant)
				    {
				        sawDominantAfterNonDominant = true;
				    }
				}
				
                prevDom = e.Dominance;
            }

            bool isMixed = sawNonDominantAfterDominant && sawDominantAfterNonDominant;

            return new ManualVolumeSequence(
                containerId,
                string.Join(" -> ", parts),
                sawDominant,
                sawNonDominantAfterDominant,
                isMixed,
				flipCount);
        }
    }
}





