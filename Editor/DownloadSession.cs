using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Services.Google.Sheetimportor
{
    [System.Serializable]
    public class DownloadSession
    {
        public string head;
        public string sheetId;
        public string exportFormat;
        public string folderPath;
        public string fileName;

        public float Progess { get; private set; }

        public IEnumerator Download(GoogleSheetDownloadSetting setting)
        {
            if (head.IsNullOrEmpty())
                head = setting.defaultHead;

            if (exportFormat.IsNullOrEmpty())
                exportFormat = setting.defaultExportFormat;

            if (folderPath.IsNullOrEmpty())
                folderPath = setting.defaultFolder;

            string url = $"{head}{sheetId}{exportFormat}";

            Progess = 0f;

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                var operation = webRequest.SendWebRequest();

                while (!operation.isDone)
                {
                    Progess = operation.progress;
                    yield return null;
                }

                Progess = 1f;

                yield return new WaitForSeconds(0.6f);

                Progess = 1f;

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string downloadData = webRequest.downloadHandler.text;
                    string diractoryPath = $"{Application.dataPath}/{folderPath}";
                    string filePath = $"{diractoryPath}{fileName}.txt";

                    if (!Directory.Exists(diractoryPath))
                        Directory.CreateDirectory(diractoryPath);

                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    using (StreamWriter sw = new StreamWriter(filePath, true))
                    {
                        sw.Write(downloadData);
                    }

                    yield return new WaitForEndOfFrame();

                    AssetDatabase.Refresh();
                }
                else
                {
                    throw new Exception($"{webRequest.error}");
                }
            }
        }
        //private static void OnComplete(string fileName)
        //{
        //    var csv = new List<string[]>();
        //    var lines = File.ReadAllLines(fileName);
        //    Debug.Log(lines.Length);
        //    foreach (string line in lines)
        //        csv.Add(line.Split(','));

        //    var properties = lines[0].Split(',');

        //    var listObjResult = new List<Dictionary<string, object>>();

        //    for (int i = 1; i < lines.Length; i++)
        //    {
        //        var objResult = new Dictionary<string, object>();
        //        for (int j = 0; j < properties.Length; j++)
        //            objResult.Add(properties[j], csv[i][j]);

        //        listObjResult.Add(objResult);
        //    }

        //    var json = JsonConvert.SerializeObject(listObjResult);
        //    Debug.Log(json);
        //}
    }
}