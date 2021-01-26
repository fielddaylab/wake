using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Inventory/Inventory Database", fileName = "InventoryDB")]
    public class InventoryDB : DBObjectCollection<InvItem>
    {

        public IEnumerable<InvItem> AllItems()
        {

            foreach (var item in Objects)
            {
                yield return item;
            }
        }

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(InventoryDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}