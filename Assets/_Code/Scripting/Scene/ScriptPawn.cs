using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;
using System;
using BeauUtil.Variants;
using Leaf.Runtime;
using UnityEngine.Scripting;

namespace Aqua.Scripting
{
    [AddComponentMenu("Aqualab/Scripting/Script Pawn")]
    public class ScriptPawn : ScriptComponent
    {
        [SerializeField, ScriptCharacterId] private StringHash32 m_CharacterId = default;
        [SerializeField] private ScriptInspectable m_Interaction = null;

        #region IScriptComponent

        public override void OnDeregister(ScriptObject inObject)
        {
            base.OnDeregister(inObject);
        }

        public override void OnRegister(ScriptObject inObject)
        {
            base.OnRegister(inObject);

            if (m_Interaction) {
                m_Interaction.Config.Action = ScriptInteractAction.Talk;
                m_Interaction.Config.TargetId = m_CharacterId;
            }
        }

        public override void PostRegister() {
            base.PostRegister();

            if (m_Interaction && m_Interaction.Hint) {
                // m_Interaction.Hint.Cursor = CursorImageType.Talk;
                if (!m_CharacterId.IsEmpty) {
                    m_Interaction.Hint.TooltipId = null;
                    m_Interaction.Hint.TooltipOverride = Loc.Format("ui.talkTo.tooltip", Assets.Character(m_CharacterId).ShortNameId());
                }
            }
        }

        #endregion // IScriptComponent
    }
}