[System.Serializable]
public struct GoogleSheetDownloadSetting
{
    public string defaultHead;
    public string defaultExportFormat;
    public string defaultFolder;

    public static GoogleSheetDownloadSetting Default
    {
        get
        {
            return new GoogleSheetDownloadSetting
            {
                defaultHead = "https://docs.google.com/spreadsheets/d/",
                defaultExportFormat = "/export?format=csv",
                defaultFolder = "Resources/"
            };
        }
    }
}
