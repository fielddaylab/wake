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
    [AddComponentMenu("Aqualab/Scripting/Script Blackboard")]
    public class ScriptBlackboard : ScriptComponent
    {
        [SerializeField] private SerializedHash32 m_BlackboardName = null;

        [NonSerialized] private VariantTable m_Table;

        [LeafMember, Preserve]
        public void ClearBlackboard()
        {
            if (m_Table != null)
                m_Table.Clear();
        }

        #region IScriptComponent

        public override void OnDeregister(ScriptObject inObject)
        {
            Services.Data.UnbindTable(m_BlackboardName);
            base.OnDeregister(inObject);
        }

        public override void OnRegister(ScriptObject inObject)
        {
            base.OnRegister(inObject);
            Services.Data.BindTable(m_BlackboardName, m_Table ?? (m_Table = new VariantTable(m_BlackboardName)));
        }

        #endregion // IScriptComponent
    }
}