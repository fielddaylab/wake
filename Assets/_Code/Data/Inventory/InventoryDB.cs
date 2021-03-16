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
        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(InventoryDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR

        public bool TryGetArtifact(out InvItemArtifact artifact) {
            foreach(var obj in this.Objects) {
                if(obj.GetType().Equals(typeof(InvItemArtifact))) {
                    artifact = (InvItemArtifact)obj;
                    return true;
                }
            }
            artifact = null;
            return false;
        }
        
    }
    

}