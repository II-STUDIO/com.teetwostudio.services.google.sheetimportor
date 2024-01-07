using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Services.Google.Convertion
{
    public static class CSVJsonConvertor
    {
        public static string ToJson(string filePath)
        {
            if (filePath.IsNullOrEmpty())
            {
                throw new Exception("Canot covert CSV to json 'file path' is empty");
            }

            var csv = new List<List<string>>();
            var lines = File.ReadAllLines(filePath);

            for(int i = 0; i < lines.Length; i++)
            {
                var filterLine = lines[i].Replace('"', '/');
                lines[i] = filterLine.Replace("/", string.Empty);
            }

            foreach (string line in lines)
            {
                csv.Add(SplitFilter(line));
            }

            var properties = SplitFilter(lines[0]);

            var listObjResult = new List<Dictionary<string, object>>();

            for (int i = 1; i < lines.Length; i++)
            {
                var objResult = new Dictionary<string, object>();
                for (int j = 0; j < properties.Count; j++)
                    objResult.Add(properties[j], csv[i][j]);

                listObjResult.Add(objResult);
            }

            return JsonConvert.SerializeObject(listObjResult);
        }

        private static List<string> SplitFilter(string line)
        {
            List<string> worlds = new();
            var raws = line.Split(',');
            for(int i = 0; i < raws.Length; i++)
            {
                var value = raws[i];
                if (value.IsNullOrEmpty())
                    continue;

                worlds.Add(value);
            }

            return worlds;
        }
    }
}