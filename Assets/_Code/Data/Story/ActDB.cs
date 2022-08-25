using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab System/Act Database", fileName = "ActDB")]
    public class ActDB : DBObjectCollection<ActDesc>
    {
        public ActDesc Act(uint inActIndex)
        {
            if (inActIndex >= Count())
                return null;
            return Objects[(int) inActIndex];
        }

        protected override void ConstructLookupForItem(ActDesc inItem, int inIndex) {
            base.ConstructLookupForItem(inItem, inIndex);

            #if UNITY_EDITOR
            inItem.EditorInit();
            #endif // UNITY_EDITOR
        }

        #if UNITY_EDITOR

        [UnityEditor.CustomEditor(typeof(ActDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}