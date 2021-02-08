using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class ScriptObject : MonoBehaviour, IPoolAllocHandler, IKeyValuePair<StringHash32, ScriptObject>
    {
        #region Inspector

        [SerializeField] private SerializedHash32 m_ClassName = "";
        [SerializeField] private SerializedHash32 m_Id = "";

        #endregion // Inspector

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, ScriptObject>.Key { get { return m_Id; } }
        ScriptObject IKeyValuePair<StringHash32, ScriptObject>.Value { get { return this; } }

        #endregion // KeyValue

        public StringHash32 Id() { return m_Id; }
        public StringHash32 ClassName() { return m_ClassName; }

        protected virtual void Awake()
        {
            RegisterScriptObject();
        }

        protected virtual void OnDestroy()
        {
            DeregisterScriptObject();
        }

        protected virtual void OnAlloc()
        {
            RegisterScriptObject();
        }

        protected virtual void OnFree()
        {
            DeregisterScriptObject();
        }

        protected void RegisterScriptObject()
        {
            Services.Script.TryRegister(this);
        }

        protected void DeregisterScriptObject()
        {
            Services.Script?.TryDeregister(this);
        }

        void IPoolAllocHandler.OnAlloc()
        {
            OnAlloc();
        }

        void IPoolAllocHandler.OnFree()
        {
            OnFree();
        }
    }
}