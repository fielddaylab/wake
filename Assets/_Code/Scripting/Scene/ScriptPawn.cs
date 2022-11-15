using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;
using System;
using BeauUtil.Variants;
using Leaf.Runtime;
using UnityEngine.Scripting;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua.Scripting
{
    [AddComponentMenu("Aqualab/Scripting/Script Pawn")]
    public class ScriptPawn : ScriptComponent
    {
        [SerializeField, ScriptCharacterId] private StringHash32 m_CharacterId = default;
        
        [Header("Locomotion")]
        [SerializeField] private ScriptObject m_DefaultNode = null;

        [Header("Optional")]
        [SerializeField] private ScriptInspectable m_Interaction = null;

        [NonSerialized] private StringHash32 m_LastNodeId;

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

            if (m_DefaultNode != null) {
                TeleportTo(m_DefaultNode);
            }
        }

        public override void PostRegister() {
            base.PostRegister();

            if (m_Interaction && m_Interaction.Hint) {
                // m_Interaction.Hint.Cursor = CursorImageType.Talk;
                if (!m_CharacterId.IsEmpty) {
                    Script.OnSceneLoad(() => {
                        m_Interaction.Hint.TooltipId = null;
                        m_Interaction.Hint.TooltipOverride = Loc.Format("ui.talkTo.tooltip", Assets.Character(m_CharacterId).ShortNameId());
                    });
                }
            }
        }

        #endregion // IScriptComponent

        public void TeleportTo(ScriptObject inObject) {
            transform.position = inObject.transform.position;
            transform.rotation = inObject.transform.rotation;
            m_LastNodeId = inObject.Id();
        }

        [LeafMember("TeleportTo"), Preserve]
        private void LeafTeleportTo(StringHash32 inObjectId)
        {
            Services.Script.TryGetScriptObjectById(inObjectId, out ScriptObject obj);
            transform.position = obj.transform.position;
            transform.rotation = obj.transform.rotation;
            m_LastNodeId = inObjectId;
        }

        [LeafMember("LastPawnNode"), Preserve]
        static private StringHash32 LeafLastNodeId(ScriptObject obj)
        {
            ScriptPawn pawn = obj != null ? obj.GetComponent<ScriptPawn>() : null;
            return pawn != null ? pawn.m_LastNodeId : null;
        }

        [LeafMember("PawnAtNode"), Preserve]
        static private bool LeafPawnAtNode(ScriptObject pawnGO, StringHash32 nodeId)
        {
            ScriptPawn pawn = pawnGO.GetComponent<ScriptPawn>();
            return pawn != null && pawn.m_LastNodeId == nodeId;
        }

        #if UNITY_EDITOR

        [ContextMenu("Create Node Here")]
        private void CreateNodeHere() {
            GameObject newNode = new GameObject(gameObject.name + " Node");
            newNode.transform.SetParent(transform.parent);
            newNode.transform.SetPositionAndRotation(transform.position, transform.rotation);
            newNode.AddComponent<ScriptObject>();
            Undo.RegisterCreatedObjectUndo(newNode, "Creating new node");
        }

        #endif // UNITY_EDITOR
    }
}