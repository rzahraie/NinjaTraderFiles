#region Using declarations
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace NinjaTrader.NinjaScript.xPva
{
    public sealed class xPvaLabelStore
    {
        private readonly List<xPvaLabel> labels = new List<xPvaLabel>();

        public IReadOnlyList<xPvaLabel> Labels => labels;

        public xPvaLabel Add(xPvaLabel label)
        {
            labels.Add(label);
            return label;
        }

        public bool Remove(string id)
        {
            var idx = labels.FindIndex(l => l.Id == id);
            if (idx < 0) return false;
            labels.RemoveAt(idx);
            return true;
        }

        public xPvaLabel Find(string id) => labels.FirstOrDefault(l => l.Id == id);

        public bool Finalize(string id, int barIndex, DateTime timeUtc)
        {
            var l = Find(id);
            if (l == null) return false;
            if (l.FinalizedAt != null) return false; // frozen
            l.FinalizedAt = new xPvaAt { BarIndex = barIndex, TimeUtc = timeUtc };
            return true;
        }

        public bool TryAddAnchor(string id, xPvaAnchor anchor)
        {
            var l = Find(id);
            if (l == null) return false;
            if (l.FinalizedAt != null) return false; // frozen
            l.Anchors.Add(anchor);
            return true;
        }
    }
}

