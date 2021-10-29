using System;
using Aqua.Scripting;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Scripting;

namespace Aqua
{
    public sealed class ScriptObject : MonoBehaviour, IPoolAllocHandler, IPoolConstructHandler, IKeyValuePair<StringHash32, ScriptObject>, ILeafActor
    {
        #region Inspector

        [SerializeField] private SerializedHash32 m_Id = "";

        #endregion // Inspector

        [NonSerialized] private IScriptComponent[] m_ScriptComponents;
        [NonSerialized] private VariantTable m_Locals;
        // [NonSerialized] private bool m_Pooled;

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, ScriptObject>.Key { get { return m_Id; } }
        ScriptObject IKeyValuePair<StringHash32, ScriptObject>.Value { get { return this; } }

        #endregion // KeyValue

        public StringHash32 Id() { return m_Id; }

        #region ILeafActor

        StringHash32 ILeafActor.Id { get { return m_Id; } }
        public VariantTable Locals { get { return m_Locals ?? (m_Locals = new VariantTable()); } }

        #endregion // ILeafActor

        #region Leaf

        [LeafMember("Activate"), Preserve]
        private void Activate()
        {
            gameObject.SetActive(true);
        }

        [LeafMember("Deactivate"), Preserve]
        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        [LeafMember("ToggleActive"), Preserve]
        private void ToggleActive()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        #endregion // Leaf

        #region Events

        private void Awake()
        {
            RegisterScriptObject();
        }

        private void OnDestroy()
        {
            DeregisterScriptObject();
        }

        #endregion // Events

        #region Register/Deregister

        private void RegisterScriptObject()
        {
            if (Services.Script.TryRegisterObject(this))
            {
                if (m_ScriptComponents == null)
                    m_ScriptComponents = GetComponents<IScriptComponent>();

                for(int i = m_ScriptComponents.Length - 1; i >= 0; i--)
                    m_ScriptComponents[i].OnRegister(this);
            }
        }

        private void DeregisterScriptObject()
        {
            if (Services.Script && Services.Script.TryDeregisterObject(this))
            {
                for(int i = m_ScriptComponents.Length - 1; i >= 0; i--)
                    m_ScriptComponents[i].OnDeregister(this);
            }
        }

        #endregion // Register/Deregister

        #region IPoolAllocHandler

        void IPoolAllocHandler.OnAlloc()
        {
            RegisterScriptObject();
        }

        void IPoolAllocHandler.OnFree()
        {
            DeregisterScriptObject();
            m_Locals?.Clear();
        }

        void IPoolConstructHandler.OnConstruct()
        {
            // m_Pooled = true;
        }

        void IPoolConstructHandler.OnDestruct()
        {
        }

        #endregion // IPoolAllocHandler

        static public ScriptThreadHandle Inspect(ScriptObject inObject)
        {
            Assert.NotNull(inObject);
            using(var table = TempVarTable.Alloc())
            {
                table.Set("objectId", inObject.m_Id.Hash());
                return Services.Script.TriggerResponse(GameTriggers.InspectObject, table);
            }
        }

        static public StringHash32 FindId(GameObject inObject, StringHash32 inDefault = default(StringHash32))
        {
            Assert.NotNull(inObject);
            ScriptObject obj = inObject.GetComponent<ScriptObject>();
            if (!obj.IsReferenceNull())
                return obj.m_Id;

            return inDefault;
        }

        static public StringHash32 FindId(Component inObject, StringHash32 inDefault = default(StringHash32))
        {
            Assert.NotNull(inObject);
            ScriptObject obj = inObject.GetComponent<ScriptObject>();
            if (!obj.IsReferenceNull())
                return obj.m_Id;

            return inDefault;
        }
    }
}