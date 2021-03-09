using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;
using System;
using BeauUtil.Variants;
using Leaf.Runtime;

namespace Aqua.Scripting
{
    public class ScriptBlackboard : MonoBehaviour, IScriptComponent
    {
        [SerializeField] private SerializedHash32 m_BlackboardName = null;

        [NonSerialized] private VariantTable m_Table;

        [LeafMember]
        public void ClearBlackboard()
        {
            if (m_Table != null)
                m_Table.Clear();
        }

        #region IScriptComponent

        void IScriptComponent.OnDeregister(ScriptObject inObject)
        {
            Services.Data.UnbindTable(m_BlackboardName);
        }

        void IScriptComponent.OnRegister(ScriptObject inObject)
        {
            Services.Data.BindTable(m_BlackboardName, m_Table ?? new VariantTable(m_BlackboardName));
        }

        #endregion // IScriptComponent
    }
}