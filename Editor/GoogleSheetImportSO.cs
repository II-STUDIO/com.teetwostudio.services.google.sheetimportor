using UnityEngine;

namespace Services.Google.Sheetimportor
{
    public class GoogleSheetImportSO : ScriptableObject
    {
        public GoogleSheetDownloadSetting defaultSetting = GoogleSheetDownloadSetting.Default;

        public void UseDefaultDownloadSetting()
        {
            defaultSetting = GoogleSheetDownloadSetting.Default;
        }
    }
}