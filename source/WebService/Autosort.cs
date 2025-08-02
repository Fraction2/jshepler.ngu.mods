using System;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;

namespace jshepler.ngu.mods.WebService
{
    public class Autosort
    {
        #region Properties
        public static Character Character => Plugin.Character;
        public static InventoryController InvController => Character.inventoryController;
        public static Inventory Inventory => Character.inventory;

        #endregion

        #region Methods

        public static void DoAutosort()
        {
            try {
                if (Input.GetMouseButton(0)) {
                    FileLog.Log($"Autosort aborted: item may be selected");
                    return;
                }
                 
                var numberOfInventorySlots = InvController.curSpaces();
                var equipmentList = Inventory.inventory;

                for (int i = InvController.totalInvMergeSlots(); i < numberOfInventorySlots; i++) {

                    for (int j = i + 1; j < numberOfInventorySlots; j++) {
                        var item1 = equipmentList[i];
                        var item2 = equipmentList[j];
                        var id1 = (item1?.id ?? 0);
                        var id2 = (item2?.id ?? 0);

                        if (id1 == 0) id1 = 10000;
                        if (id2 == 0) id2 = 10000;
                                                  
                        if (item1.isMacGuffin()) {
                            id1 += 10000;
                        }

                        if (item2.isMacGuffin()) {
                            id2 += 10000;
                        }

                        if (Compare(id1, id2)) {
                            var slot1 = i;
                            var slot2 = j;
                            Inventory.markLoadoutIDSwap(slot1, slot2);
                            var temp = equipmentList[slot1];
                            equipmentList[slot1] = equipmentList[slot2];
                            equipmentList[slot2] = temp;
                        }
                    }
                }
                InvController.updateInventory();
                //Plugin.ShowTooltip("Autosort completed successfully!");
            } catch (Exception ex) {
                FileLog.Log($"Autosort failed: {ex.Message}");
            }
        }

        private static bool Compare(int a, int b)
        {
            return a == 0 || b < a;
        }


        #endregion
    }
}
