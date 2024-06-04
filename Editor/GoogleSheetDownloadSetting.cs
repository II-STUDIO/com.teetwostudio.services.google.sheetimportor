#if UNITY_EDITOR
namespace Services.Google.Sheetimportor
{
    [System.Serializable]
    public struct GoogleSheetDownloadSetting
    {
        public string defaultHead;
        public string defaultExportFormat;
        public string defaultCSVFolder;
        public string defaultJsonFolder;
        public string defaultName;

        public static GoogleSheetDownloadSetting Default
        {
            get
            {
                return new GoogleSheetDownloadSetting
                {
                    defaultHead = "https://docs.google.com/spreadsheets/d/",
                    defaultExportFormat = "/gviz/tq?tqx=out:csv&sheet=",
                    defaultCSVFolder = "Resources/GoogleSheets/CSVs/",
                    defaultJsonFolder = "Resources/GoogleSheets/Jsons/",
                    defaultName = "Untitled",
                };
            }
        }
    }
}
#endif