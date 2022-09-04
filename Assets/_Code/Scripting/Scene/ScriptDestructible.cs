using BeauUtil;
using UnityEngine;
using System.Collections;
using Leaf.Runtime;
using BeauRoutine;
using UnityEngine.UI;
using UnityEngine.Scripting;
using BeauUtil.Variants;
using ScriptableBake;
using System;

namespace Aqua.Scripting
{
    [AddComponentMenu("Aqualab/Scripting/Script Destructible")]
    [DisallowMultipleComponent]
    public class ScriptDestructible : ScriptComponent, IBaked
    {
        static public readonly StringHash32 Trigger_Destroyed = "ScriptObjectDestroyed";

        public delegate IEnumerator DestructCallback(ScriptDestructible destructible);

        #region Inspector

        [SerializeField] private bool m_Persistent = true;
        [SerializeField] private ActiveGroup m_Reveal = null;
        [SerializeField, HideInInspector] private StringHash32 m_VarId;
        [SerializeField, HideInInspector] private ScriptGroupId m_GroupId;

        #endregion // Inspector

        [NonSerialized] private bool m_CachedDestroyed;
        private Routine m_Routine;

        public DestructCallback OnDestruct;

        private void Awake() {
            m_Reveal.ForceActive(false);
            Script.OnSceneLoad(LoadData);
        }

        private void LoadData() {
            if (!m_Persistent) {
                return;
            }

            m_CachedDestroyed = Script.ReadWorldVariable(m_VarId).AsBool();
            if (m_CachedDestroyed) {
                gameObject.SetActive(false);
                m_Reveal.SetActive(true);
            }
        }

        [LeafMember("IsDestroyed"), Preserve]
        public bool IsDestroyed() {
            return m_CachedDestroyed;
        }

        [LeafMember("Destroy"), Preserve]
        public void TriggerDestroy() {
            if (m_CachedDestroyed) {
                return;
            }

            m_CachedDestroyed = true;
            if (m_Persistent) {
                Script.WriteWorldVariable(m_VarId, true);
            }

            if (m_GroupId) {
                m_GroupId.enabled = false;
            }

            IEnumerator custom = OnDestruct?.Invoke(this);
            if (custom != null) {
                m_Routine.Replace(this, DestroyRoutine(custom)).Tick();
            } else {
                gameObject.SetActive(false);
                m_Reveal.SetActive(true);
            }

            using(var table = TempVarTable.Alloc()) {
                table.Set("objectId", Parent.Id());
                Trigger(Trigger_Destroyed, table);
            }
        }

        private IEnumerator DestroyRoutine(IEnumerator custom) {
            yield return custom;
            gameObject.SetActive(false);
            m_Reveal.SetActive(true);
        }

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags) {
            m_GroupId = GetComponent<ScriptGroupId>();
            if (m_Persistent) {
                return Ref.Replace(ref m_VarId, ScriptObject.MapPersistenceId(GetComponent<ScriptObject>(), "destroyed"));
            } else {
                return Ref.Replace(ref m_VarId, default);
            }
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}