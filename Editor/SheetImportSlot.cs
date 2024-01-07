using Services.Google.Convertion;
using System;
using System.Collections;
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
        public string docId;
        public string sheetName;
        public bool autoGenerateJsonValue = true;

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

            string url = $"{target_Head}{docId}{target_ExportFormat}{sheetName}";

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
                    string downloadData = webRequest.downloadHandler.text;

                    CreateFile(downloadData, target_CSVFolderPath, fileName, ".txt");

                    yield return new WaitForEndOfFrame();

                    CurrentStatus = Status.Successfully;

                    if (autoImport)
                    {
                        try
                        {
                            Import(setting);
                        }
                        catch(Exception e)
                        {
                            Debug.LogException(e);
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

        private void StopCoroutine()
        {
            if (coroutine == null)
                return;

            EditorCoroutineUtility.StopCoroutine(coroutine);

            coroutine = null;
        }

        public void Import(GoogleSheetDownloadSetting setting)
        {
            ImportFile(override_CSVFolderPath, setting.defaultCSVFolder, ".txt");

            if (!autoGenerateJsonValue)
            {
                return;
            }

            GenerateJson(setting);

            ImportFile(override_JsonFolderPath, setting.defaultJsonFolder, ".json");
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