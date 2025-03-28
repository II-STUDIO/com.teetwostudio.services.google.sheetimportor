#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Services.Google.Convertion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Services.Google.Sheetimportor
{
    [System.Serializable]
    public class SheetImportSlot
    {
        public enum Status
        {
            Normal,
            Downloading,
            Successfully,
            Error,
        }

        public string fileName;

        public void ClearTask()
        {
            Progess = 0f;
            CurrentStatus = Status.Normal;
        }

        public string docId;
        public string sheetName;
        public bool autoGenerateJsonValue = true;
        public bool importAsJsonValue = false;

        public string override_Head;
        public string override_ExportFormat;
        public string override_CSVFolderPath;
        public string override_JsonFolderPath;

        public Status CurrentStatus { get; private set; }

        public float Progess { get; private set; }

        public string ErrorMsg { get; private set; }

        private EditorCoroutine coroutine;

        public void SetCoroutine(EditorCoroutine coroutine)
        {
            this.coroutine = coroutine;
        }

        public IEnumerator Download(GoogleSheetDownloadSetting setting, bool autoImport = true, Action<SheetImportSlot> onEndProgess = null)
        {
            var target_Head = override_Head.IsNullOrEmpty() ? setting.defaultHead : override_Head;
            var target_ExportFormat = override_ExportFormat.IsNullOrEmpty() ? setting.defaultExportFormat : override_ExportFormat;
            var target_CSVFolderPath = override_CSVFolderPath.IsNullOrEmpty() ? setting.defaultCSVFolder : override_CSVFolderPath;
            var target_JsonFolderPath = override_JsonFolderPath.IsNullOrEmpty() ? setting.defaultJsonFolder : override_JsonFolderPath;

            string url_CSV = $"{target_Head}{docId}{target_ExportFormat}{sheetName}";
            string url_Json = $"{target_Head}{docId}{target_ExportFormat}";
            string url = importAsJsonValue ? url_Json : url_CSV;

            Debug.Log(url);

            CurrentStatus = Status.Downloading;

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

                yield return new EditorWaitForSeconds(0.6f);

                Progess = 1f;

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string targetPath = importAsJsonValue ? target_JsonFolderPath : target_CSVFolderPath;
                    string format = importAsJsonValue ? ".json" : ".txt";

                    string downloadData = webRequest.downloadHandler.text;
                    bool isBreak = false;
                    if (importAsJsonValue)
                    {
                        try
                        {
                            downloadData = GoogleJsonToJson(downloadData);

                        }
                        catch(Exception e)
                        {
                            Progess = 0f;
                            CurrentStatus = Status.Normal;

                            isBreak = true;
                            Debug.LogException(e);
                            StopCoroutine();
                        }
                    }

                    if (!isBreak)
                    {
                        CreateFile(downloadData, targetPath, fileName, format);

                        yield return new WaitForEndOfFrame();

                        CurrentStatus = Status.Successfully;

                        if (autoImport)
                        {
                            try
                            {
                                Import(setting);
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }
                        }
                    }
                }
                else
                {
                    CurrentStatus = Status.Error;

                    ErrorMsg = $"{webRequest.error}";

                    Debug.LogError(ErrorMsg);
                }
            }

            yield return new EditorWaitForSeconds(2.5f);

            ErrorMsg = string.Empty;

            Progess = 0f;
            CurrentStatus = Status.Normal;

            StopCoroutine();

            onEndProgess?.Invoke(this);
        }

        private string GoogleJsonToJson(string json)
        {
            json = json.Substring(47, json.Length - 49); // Trim extra characters
            JObject jsonObj = JObject.Parse(json);

            List<Dictionary<string, string>> sheetData = new List<Dictionary<string, string>>();

            // Extract column headers
            JArray columns = (JArray)jsonObj["table"]["cols"];
            List<string> columnNames = new List<string>();

            bool isSkipFirstRow = false;

            // Check if label is null and use the first row's value if needed
            foreach (var col in columns)
            {
                string colName = col["label"]?.ToString();
                if (string.IsNullOrEmpty(colName))
                {
                    // If the label is null, use the value from the first row
                    JArray rows = (JArray)jsonObj["table"]["rows"];
                    if (rows.Count > 0 && rows[0]["c"] != null && rows[0]["c"][columnNames.Count] != null)
                    {
                        var raw = rows[0]["c"][columnNames.Count];

                        if (raw != null && raw.HasValues)
                        {
                            colName = raw["v"].ToString();
                            isSkipFirstRow = true;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(colName))
                    columnNames.Add(colName);

            }

            if (columnNames.Count == 0)
            {
                Debug.LogWarning("No column names found!");
            }

            // Extract rows
            JArray rowsData = (JArray)jsonObj["table"]["rows"];
            foreach (var row in rowsData)
            {
                if (isSkipFirstRow)
                {
                    isSkipFirstRow = false;
                    continue;
                }

                JArray rowData = (JArray)row["c"];
                Dictionary<string, string> rowDict = new Dictionary<string, string>();

                for (int i = 0; i < columnNames.Count; i++)
                {
                    if (i < rowData.Count && rowData[i] != null && rowData[i]["v"] != null)
                        rowDict[columnNames[i]] = rowData[i]["v"].ToString();
                    else
                        rowDict[columnNames[i]] = ""; // Empty cell
                }

                sheetData.Add(rowDict);
            }

            // Convert to JSON and print result
            string formattedJson = Newtonsoft.Json.JsonConvert.SerializeObject(sheetData, Newtonsoft.Json.Formatting.Indented);

            return formattedJson;
        }

        private void StopCoroutine()
        {
            if (coroutine == null)
                return;

            EditorCoroutineUtility.StopCoroutine(coroutine);

            coroutine = null;
        }

        public void Import(GoogleSheetDownloadSetting setting)
        {
            if (importAsJsonValue)
            {
                ImportFile(override_JsonFolderPath, setting.defaultJsonFolder, ".json");
            }
            else
            {
                ImportFile(override_CSVFolderPath, setting.defaultCSVFolder, ".txt");

                if (!autoGenerateJsonValue)
                {
                    return;
                }

                GenerateJson(setting);

                ImportFile(override_JsonFolderPath, setting.defaultJsonFolder, ".json");
            }
        }

        public bool IsCSVExisted(GoogleSheetDownloadSetting setting)
        {
            var target_CSVFolderPath = override_CSVFolderPath.IsNullOrEmpty() ? setting.defaultCSVFolder : override_CSVFolderPath;

            string diractoryPath = $"{Application.dataPath}/{target_CSVFolderPath}";
            string filePath = $"{diractoryPath}{fileName}.txt";

            if (!Directory.Exists(diractoryPath))
                return false;

            if (!File.Exists(filePath))
                return false;

            return true;
        }

        public bool IsJsonExisted(GoogleSheetDownloadSetting setting)
        {
            var target_JsonFolderPath = override_JsonFolderPath.IsNullOrEmpty() ? setting.defaultJsonFolder : override_JsonFolderPath;

            string diractoryPath = $"{Application.dataPath}/{target_JsonFolderPath}";
            string filePath = $"{diractoryPath}{fileName}.json";

            if (!Directory.Exists(diractoryPath))
                return false;

            if (!File.Exists(filePath))
                return false;

            return true;
        }

        public TextAsset GetCSV(GoogleSheetDownloadSetting setting)
        {
            var target_CSVFolderPath = override_CSVFolderPath.IsNullOrEmpty() ? setting.defaultCSVFolder : override_CSVFolderPath;

            string diractoryPath = $"{Application.dataPath}/{target_CSVFolderPath}";
            string filePath = $"{diractoryPath}{fileName}.txt";

            if (!Directory.Exists(diractoryPath))
                return null;

            if (!File.Exists(filePath))
                return null;

            return (TextAsset)AssetDatabase.LoadAssetAtPath($"Assets/{target_CSVFolderPath}{fileName}.txt", typeof(TextAsset));
        }

        public TextAsset GEtJson(GoogleSheetDownloadSetting setting)
        {
            var target_JsonFolderPath = override_JsonFolderPath.IsNullOrEmpty() ? setting.defaultJsonFolder : override_JsonFolderPath;

            string diractoryPath = $"{Application.dataPath}/{target_JsonFolderPath}";
            string filePath = $"{diractoryPath}{fileName}.json";

            if (!Directory.Exists(diractoryPath))
                return null;

            if (!File.Exists(filePath))
                return null;

            return (TextAsset)AssetDatabase.LoadAssetAtPath($"Assets/{target_JsonFolderPath}{fileName}.json", typeof(TextAsset));
        }

        public void ReganerateOrImportJson(GoogleSheetDownloadSetting setting)
        {
            GenerateJson(setting);

            ImportFile(override_JsonFolderPath, setting.defaultJsonFolder, ".json");
        }

        private void GenerateJson(GoogleSheetDownloadSetting setting)
        {
            var target_CSVFolderPath = override_CSVFolderPath.IsNullOrEmpty() ? setting.defaultCSVFolder : override_CSVFolderPath;
            var target_JsonFolderPath = override_JsonFolderPath.IsNullOrEmpty() ? setting.defaultJsonFolder : override_JsonFolderPath;

            string diractoryPath = $"{Application.dataPath}/{target_CSVFolderPath}";
            string filePath = $"{diractoryPath}{fileName}.txt";

            if (!Directory.Exists(diractoryPath))
                throw new Exception($"Directory folder <{diractoryPath}> not exist or create yet");

            if (!File.Exists(filePath))
                throw new Exception($"File <{filePath}> not exist or import yet");

            var json = CSVJsonConvertor.ToJson(filePath);

            CreateFile(json, target_JsonFolderPath, fileName, ".json");
        }

        private void CreateFile(string data, string folderDirectory, string fileName, string fileFormat)
        {
            string diractoryPath = $"{Application.dataPath}/{folderDirectory}";
            string filePath = $"{diractoryPath}{fileName}{fileFormat}";

            if (!Directory.Exists(diractoryPath))
                Directory.CreateDirectory(diractoryPath);

            if (File.Exists(filePath))
                File.Delete(filePath);

            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.Write(data);
            }
        }

        private void ImportFile(string overrideValue, string defaultValue, string fileFormat)
        {
            var targetFolder = overrideValue.IsNullOrEmpty() ? defaultValue : overrideValue;

            string filePath = $"Assets/{targetFolder}{fileName}{fileFormat}";

            Debug.Log($"Import text asset : {filePath}");

            AssetDatabase.ImportAsset(filePath);
        }
    }
}
#endif