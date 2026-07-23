using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UEBS2Stereo
{
    internal sealed class AcceptanceLedger
    {
        internal sealed class Entry { internal string surface, classification, detail; internal int frame; }
        private readonly List<Entry> entries=new List<Entry>();
        internal IEnumerable<Entry> Entries => entries;
        internal void Add(string surface,string classification,string detail)
        {
            for(int i=0;i<entries.Count;i++) if(entries[i].surface==surface && entries[i].classification==classification && entries[i].detail==detail) return;
            entries.Add(new Entry { surface=surface, classification=classification, detail=detail, frame=Time.frameCount });
        }
        internal string ToJson()
        {
            StringBuilder sb=new StringBuilder("{\"surfaces\":[");
            for(int i=0;i<entries.Count;i++)
            {
                if(i>0)sb.Append(',');
                Entry e=entries[i]; sb.Append("{\"surface\":\"").Append(E(e.surface)).Append("\",\"classification\":\"").Append(E(e.classification)).Append("\",\"detail\":\"").Append(E(e.detail)).Append("\"}");
            }
            return sb.Append("]}").ToString();
        }
        private static string E(string value) { return (value??"").Replace("\\","\\\\").Replace("\"","\\\"").Replace("\r","\\r").Replace("\n","\\n"); }
    }
}
