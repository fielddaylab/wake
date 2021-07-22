using Aqua.Scripting;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    public class SpawnLocation : MonoBehaviour, ISceneOptimizable
    {
        [SerializeField, HideInInspector] private ScriptObject m_Parent;
        [SerializeField] private SerializedHash32 m_Id;

        public StringHash32 Id { get { return m_Id; } }

        #if UNITY_EDITOR

        private void Reset()
        {
            m_Parent = GetComponentInParent<ScriptObject>();
            if (m_Parent != null)
                m_Id = m_Parent.Id();
        }

        void ISceneOptimizable.Optimize()
        {
            m_Parent = GetComponentInParent<ScriptObject>();
            if (m_Parent != null)
                m_Id = m_Parent.Id();
        }

        #endif // UNITY_EDITOR
    }
}