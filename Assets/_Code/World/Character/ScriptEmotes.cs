using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;
using Aqua.Scripting;
using BeauUtil.Variants;
using Leaf.Runtime;
using Leaf;
using BeauUtil.Services;
using Aqua.Debugging;
using BeauUtil.Debugger;
using UnityEngine.Scripting;
using BeauUtil.Blocks;

namespace Aqua.Character
{
    public class ScriptEmotes : ScriptComponent {
        
        [SerializeField, ScriptCharacterId] private StringHash32 m_CharacterId;

        private void OnEnable() {
            DialogPanel.UpdatePortrait += UpdateFace;
        }

        private void OnDisable() {
            DialogPanel.UpdatePortrait -= UpdateFace;
        }

        private void UpdateFace(StringHash32 characterId, StringHash32 portraitId){
            if (characterId != m_CharacterId) {
                return;
            }
        }
    }
}
