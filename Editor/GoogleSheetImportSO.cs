#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Services.Google.Sheetimportor
{
    [CreateAssetMenu(fileName = "SheetImportoSO",menuName = "IIStudio/Google/SheetImportoSO")]
    public class GoogleSheetImportSO : ScriptableObject
    {
        public GoogleSheetDownloadSetting defaultSetting = GoogleSheetDownloadSetting.Default;

        public List<SheetImportSlot> importSlots = new();

        public void ClearTask()
        {
            foreach(var slot in importSlots)
            {
                slot.ClearTask();
            }
        }

        public void UseDefaultDownloadSetting()
        {
            defaultSetting = GoogleSheetDownloadSetting.Default;
        }

        public SheetImportSlot AddSlot()
        {
            var slot = CreateSlot();

            importSlots.Add(slot);

            return slot;
        }

        public bool IsValideIndex(int index)
        {
            return index >= 0 || index <= importSlots.Count - 1;
        }

        public void RemoveSlot(int index)
        {
            if (!IsValideIndex(index))
                return;

            importSlots.RemoveAt(index);
        }

        private SheetImportSlot CreateSlot()
        {
            var slot = new SheetImportSlot();

            int slotCount = importSlots.Count;

            if (slotCount > 0)
            {
                var stepSlot = importSlots[slotCount - 1];
                slot.docId = stepSlot.docId;
                slot.sheetName = stepSlot.sheetName;
            }

            slot.fileName = $"{defaultSetting.defaultName}_{slotCount}";

            return slot;
        }
    }
}
#endif