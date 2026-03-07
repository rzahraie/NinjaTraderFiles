#region Using declarations
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#endregion

namespace NinjaTrader.NinjaScript.xPva
{
    internal static class xPvaJson
    {
        public static string ToJson<T>(T obj)
        {
            var settings = new JsonSerializerSettings
		    {
		        Formatting = Formatting.Indented,
		        Converters = { new StringEnumConverter() }
		    };
    		
			return JsonConvert.SerializeObject(obj, settings);
        }

        public static void WriteUtf8File(string path, string json)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, json, Encoding.UTF8);
        }
    }
}

