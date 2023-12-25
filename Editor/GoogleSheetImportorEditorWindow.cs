using System.IO;
using UnityEditor;
using UnityEngine;

namespace Services.Google.Sheetimportor
{
    public class GoogleSheetImportorEditorWindow : EditorWindow
    {
        private const string SODirectory = "Resources/GoogleSheetImportorData";
        private const string SOPath = SODirectory + "/GoogleSheetImportorSO.asset";

        public GoogleSheetImportSO so;

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
        }

        void OnGUI()
        {
            using (var verticalScope = new GUILayout.VerticalScope("GroupBox"))
            {
                so = EditorGUILayout.ObjectField("SO Database", so, typeof(GoogleSheetImportSO), true) as GoogleSheetImportSO;
            }
        }
    }
}