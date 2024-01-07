using Services.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Services.Google.Sheetimportor
{
    public class GoogleSheetImportorEditorWindow : EditorWindow
    {
        private const string SODirectory = "Resources/GoogleSheets/";
        private const string SOPath = SODirectory + "GoogleSheetImportorSO.asset";

        private GoogleSheetImportSO so;
        private AnimBoolGroupController<SheetImportSlot> slotAnimBools = new();
        private AnimBoolGroupController<SheetImportSlot> slotAnimBoolOverrides = new();

        private GUIStyle middleLabelStyle;

        private Vector3 slotListScrollviewPos;

        private int targetSlotProgessCount = 0;
        private int slotEndProgessCount = 0;

        private bool isDownloadAll = false;

        private List<SheetImportSlot> endProgessedList = new();

        [MenuItem("IIStudio/Google/SheetImportor")]
        public static void Open()
        {
            var window = GetWindow<GoogleSheetImportorEditorWindow>("Google Sheet Importor");

            var guids = AssetDatabase.FindAssets("t:GoogleSheetImportSO");

            GoogleSheetImportSO exitedSO = null;

            if (guids.Length > 0)
                exitedSO = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), typeof(GoogleSheetImportSO)) as GoogleSheetImportSO;

            if (exitedSO)
                window.so = exitedSO;
            else
            {
                window.so = CreateInstance<GoogleSheetImportSO>();

                string targetDirectory = $"{Application.dataPath}/{SODirectory}/";
                string resultPath = $"Assets/{SOPath}";

                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                AssetDatabase.Refresh();

                AssetDatabase.CreateAsset(window.so, resultPath);
                AssetDatabase.SaveAssets();
            }

            window.SetupAnimBool();
        }

        private void OnDisable()
        {
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
        }

        void OnGUI()
        {
            if(middleLabelStyle == null)
            {
                middleLabelStyle = new GUIStyle(GUI.skin.label);
                middleLabelStyle.alignment = TextAnchor.MiddleCenter;
            }    

            using (var verticalScope = new GUILayout.VerticalScope("GroupBox"))
            {
                var targetSO = EditorGUILayout.ObjectField("SO Database", so, typeof(GoogleSheetImportSO), true) as GoogleSheetImportSO;
                if(targetSO != so)
                {
                    Undo.RecordObject(this, "GoogleSheetEditorWindow_Change_SO");

                    so = targetSO;
                }

                GUILayout.Space(5f);

                so.defaultSetting.defaultHead = DetectTextField("Default Head", so.defaultSetting.defaultHead, "GoogleSheetEditorWindow_Change_SO_DefaultSetting_Head");
                so.defaultSetting.defaultExportFormat = DetectTextField("Default Export Format", so.defaultSetting.defaultExportFormat, "GoogleSheetEditorWindow_Change_SO_DefaultSetting_ExportFormat");
                so.defaultSetting.defaultCSVFolder = DetectTextField("Default CSV Folder", so.defaultSetting.defaultCSVFolder, "GoogleSheetEditorWindow_Change_SO_DefaultSetting_CSVFolder");
                so.defaultSetting.defaultJsonFolder = DetectTextField("Default Json Folder", so.defaultSetting.defaultJsonFolder, "GoogleSheetEditorWindow_Change_SO_DefaultSetting_JsonFolder");
                so.defaultSetting.defaultName = DetectTextField("Default Name", so.defaultSetting.defaultName, "GoogleSheetEditorWindow_Change_SO_DefaultSetting_Name");
            }

            using (var scrollViewScope = new GUILayout.ScrollViewScope(slotListScrollviewPos, "GroupBox"))
            {
                slotListScrollviewPos = scrollViewScope.scrollPosition;

                if (so.importSlots == null)
                {
                    Close();
                    return;
                }

                int slotCount = so.importSlots.Count;

                bool isValide = true;

                if (slotCount > 0)
                {
                    isValide = DrawSlotListGUI(slotCount);
                }
                else
                {
                    DrawSlotEmptyLabelGUI();
                }

                GUILayout.FlexibleSpace();

                if (isValide)
                {
                    if (GUILayout.Button("Add"))
                    {
                        AddSlot();
                    }

                    if (!isDownloadAll)
                    {
                        if (GUILayout.Button("Download All"))
                        {
                            DownloadAll();
                        }
                    }
                }
            }

            Repaint();
        }

        private string DetectTextField(string label, string value, string undoAction)
        {
            var targetValue = EditorGUILayout.TextField(label, value);

            if(targetValue != value)
            {
                Undo.RecordObject(so, undoAction);

                EditorUtility.SetDirty(so);

                return targetValue;
            }

            return value;
        }

        private bool DetextToggleField(string label, bool value, string undoAction, params GUILayoutOption[] options)
        {
            var targetValue = EditorGUILayout.Toggle(label, value, options);

            if (targetValue != value)
            {
                Undo.RecordObject(so, undoAction);

                EditorUtility.SetDirty(so);

                return targetValue;
            }

            return value;
        }

        public void SetupAnimBool()
        {
            for (int i = 0; i < so.importSlots.Count; i++)
            {
                var slot = so.importSlots[i];

                slotAnimBools.Add(slot, startValue: false);
                slotAnimBoolOverrides.Add(slot, startValue: false);
            }
        }

        private void AddSlot()
        {
            Undo.RecordObject(so, "Add_GooleSheet_ImportSlot");

            var slot = so.AddSlot();

            slotAnimBools.Add(slot, startValue: true);
            slotAnimBoolOverrides.Add(slot, startValue: false);

            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
        }

        private void RemoveSlot(int index)
        {
            if (!so.IsValideIndex(index))
                return;

            Undo.RecordObject(so, "Remove_GooleSheet_ImportSlot");

            var slot = so.importSlots[index];

            so.RemoveSlot(index);

            slotAnimBools.Remove(slot);
            slotAnimBoolOverrides.Remove(slot);  

            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
        }

        private void DrawSlotEmptyLabelGUI()
        {
            EditorGUILayout.LabelField("<Slot is empty create your new slot>", middleLabelStyle);
        }

        private bool DrawSlotListGUI(int slotCount)
        {
            for (int i = 0; i < slotCount; i++)
            {
                if (!DrawSlotGUI(i))
                {
                    return false;
                }
            }

            return true;
        }

        private bool DrawSlotGUI(int index)
        {
            using (var frame = new GUILayout.VerticalScope("Box"))
            {
                var slot = so.importSlots[index];

                var animBool = slotAnimBools.Get(slot, startValue: false);

                using (var header = new GUILayout.HorizontalScope())
                {
                    animBool.target = EditorGUILayout.BeginFoldoutHeaderGroup(animBool.target, slot.fileName);

                    if(!animBool.target)
                    {
                        switch (slot.CurrentStatus)
                        {
                            case SheetImportSlot.Status.Normal:
                                var downloadIcon = EditorGUIUtility.IconContent("Download-Available");
                                if (GUILayout.Button(downloadIcon, GUILayout.Width(50f)))
                                    Download(index, autoImport: true, OnEndSlotProgess);
                                break;
                            case SheetImportSlot.Status.Downloading:
                                var downloadingicon = EditorGUIUtility.IconContent("d_WaitSpin07");
                                GUILayout.Label(downloadingicon, GUI.skin.button, GUILayout.Width(50f));
                                break;
                            case SheetImportSlot.Status.Successfully:
                                var successfullyIcon = EditorGUIUtility.IconContent("d_Progress");
                                GUILayout.Label(successfullyIcon, GUI.skin.button, GUILayout.Width(50f));
                                break;
                            case SheetImportSlot.Status.Error:
                                var errorIcon = EditorGUIUtility.IconContent("Error");
                                GUILayout.Label(errorIcon, GUI.skin.button, GUILayout.Width(50f));
                                break;
                        }

                    }

                    if (GUILayout.Button("x", GUILayout.Width(25f)))
                    {
                        if (slot.CurrentStatus != SheetImportSlot.Status.Downloading)
                        {
                            RemoveSlot(index);
                            return false;
                        }
                    }
                }

                using (var group = new EditorGUILayout.FadeGroupScope(animBool.faded))
                {
                    if (group.visible)
                    {
                        using (var window = new GUILayout.VerticalScope(slot.fileName, "Window", GUILayout.Height(90)))
                        {
                            EditorGUI.indentLevel++;

                            slot.fileName = DetectTextField("File Name", slot.fileName, "Change_GoogleSheet_Import_Slot_FileName");
                            slot.docId = DetectTextField("Doccument ID", slot.docId, "Change_GoogleSheet_Import_Slot_DocID");
                            slot.sheetName = DetectTextField("Sheet Name", slot.sheetName, "Change_GoogleSheet_Import_Slot_SheetName");

                            using (var jsonScope = new GUILayout.HorizontalScope())
                            {
                                slot.autoGenerateJsonValue = DetextToggleField("Auto Generate Json", slot.autoGenerateJsonValue, "Change_GoogleSheet_Import_Slot_AutoGenerateJson", GUILayout.Width(180f));

                                if (slot.IsCSVExisted(so.defaultSetting))
                                {
                                    if (GUILayout.Button("Regenerate Jon"))
                                    {
                                        slot.ReganerateOrImportJson(so.defaultSetting);
                                    }
                                }
                            }

                            DrawSlotPropertOverride(index);

                            GUILayout.Space(5f);

                            DrawSlotCommand(index);

                            EditorGUI.indentLevel--;
                        }
                    }
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            return true;
        }

        private void DrawSlotPropertOverride(int index)
        {
            var slot = so.importSlots[index];

            var animBool = slotAnimBoolOverrides.Get(slot, startValue: false);

            animBool.target = EditorGUILayout.Toggle("Override Option", animBool.target);

            using (var group = new EditorGUILayout.FadeGroupScope(animBool.faded))
            {
                if (group.visible)
                {
                    EditorGUI.indentLevel++;

                    slot.override_Head = DetectTextField("Head", slot.override_Head, "Change_GoogleSheet_Import_Slot_OverrideHead");
                    slot.override_ExportFormat = DetectTextField("Export Format", slot.override_ExportFormat, "Change_GoogleSheet_Import_Slot_OverrideExportFormat");
                    slot.override_CSVFolderPath = DetectTextField("CSV Folder Path", slot.override_CSVFolderPath, "Change_GoogleSheet_Import_Slot_OverrideCSVFolderPath");
                    slot.override_JsonFolderPath = DetectTextField("Json Folder Path", slot.override_JsonFolderPath, "Change_GoogleSheet_Import_Slot_OverrideJsonFolderPath");

                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawSlotCommand(int index)
        {
            var slot = so.importSlots[index];

            switch (slot.CurrentStatus)
            {
                case SheetImportSlot.Status.Normal:
                    var downloadIcon = EditorGUIUtility.IconContent("Download-Available");
                    if (GUILayout.Button(downloadIcon))
                        Download(index, autoImport: true, OnEndSlotProgess);
                    break;
                case SheetImportSlot.Status.Downloading:
                    using (var group = new EditorGUILayout.VerticalScope())
                    {
                        string current = (slot.Progess * 100f).ToString("F1");
                        EditorGUI.ProgressBar(group.rect, slot.Progess, $"Downloading {current}%");
                        EditorGUILayout.LabelField(string.Empty, middleLabelStyle);
                    }
                    break;
                case SheetImportSlot.Status.Successfully:
                    EditorGUILayout.HelpBox("Import Successfully", MessageType.Info);
                    break;
                case SheetImportSlot.Status.Error:
                    EditorGUILayout.HelpBox($"Import failed\n{slot.ErrorMsg}", MessageType.Error);
                    break;
            }
        }

        private void Download(int index, bool autoImport = true, Action<SheetImportSlot> onEndProgess = null)
        {
            var slot = so.importSlots[index];

            if (slot.CurrentStatus != SheetImportSlot.Status.Normal)
                return;

            if (slot.docId.IsNullOrEmpty())
            {
                Debug.LogWarning($"Document id of slot index <{index}> is empty");
                return;
            }

            if (slot.sheetName.IsNullOrEmpty())
            {
                Debug.LogWarning($"Sheet name of slot index <{index}> is empty");
                return;
            }

            if (slot.fileName.IsNullOrEmpty())
            {
                Debug.LogWarning($"file name of slot index <{index}> is empty");
                return;
            }

            var coroutine = EditorCoroutineUtility.StartCoroutine(slot.Download(so.defaultSetting, autoImport, onEndProgess), slot);
            slot.SetCoroutine(coroutine);
        }

        private void DownloadAll()
        {
            isDownloadAll = true;

            endProgessedList.Clear();
            slotEndProgessCount = 0;
            targetSlotProgessCount = so.importSlots.Count;

            for (int i = 0; i < targetSlotProgessCount; i++)
            {
                Download(i, autoImport: false, OnSlotEndProgessList);
            }
        }

        private void OnEndSlotProgess(SheetImportSlot slot)
        {
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
        }

        private void OnSlotEndProgessList(SheetImportSlot slot)
        {
            slotEndProgessCount++;

            endProgessedList.Add(slot);

            if (slotEndProgessCount < targetSlotProgessCount)
            {
                return;
            }

            ImportAllEndProgessSlot();

            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            endProgessedList.Clear();
            slotEndProgessCount = 0;

            isDownloadAll = false;
        }

        private void ImportAllEndProgessSlot()
        {
            for(int i = 0; i < endProgessedList.Count; i++)
            {
                var slot = endProgessedList[i];

                try
                {
                    slot.Import(so.defaultSetting);
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}