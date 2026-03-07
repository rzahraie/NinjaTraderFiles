#region Using declarations
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#endregion

namespace NinjaTrader.NinjaScript.xPva
{
    public static class xPvaLabelFileReader
    {
        private static JsonSerializerSettings Settings => new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };

        public static xPvaDataset ReadDataset(string path)
        {
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<xPvaDataset>(json, Settings);
        }

        // Finds newest file like: 6E_Minute_5_*.json
        public static string FindLatestDatasetFile(string folder, string master, string barsType, int barsValue)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                return null;

            string prefix = $"{master}_{barsType}_{barsValue}_";

            var file = Directory
                .EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly)
                .Where(p => Path.GetFileName(p).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(p => new FileInfo(p))
                .OrderByDescending(fi => fi.LastWriteTimeUtc)
                .FirstOrDefault();

            return file?.FullName;
        }
    }
}
