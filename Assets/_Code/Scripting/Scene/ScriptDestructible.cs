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
    [DisallowMultipleComponent]
    public class ScriptDestructible : ScriptComponent, IBaked
    {
        public delegate IEnumerator DestructCallback(ScriptDestructible destructible);

        #region Inspector

        [SerializeField, HideInInspector] private StringHash32 m_VarId;

        #endregion // Inspector

        [NonSerialized] private bool m_CachedDestroyed;
        private Routine m_Routine;

        public DestructCallback OnDestruct;

        private void Awake() {
            Script.OnSceneLoad(LoadData);
        }

        private void LoadData() {
            m_CachedDestroyed = Script.ReadWorldVariable(m_VarId).AsBool();
            if (m_CachedDestroyed) {
                gameObject.SetActive(false);
            }
        }

        [LeafMember("IsDestroyed")]
        public bool IsDestroyed() {
            return m_CachedDestroyed;
        }

        [LeafMember("Destroy")]
        public void TriggerDestroy() {
            if (m_CachedDestroyed) {
                return;
            }

            m_CachedDestroyed = true;
            Script.WriteWorldVariable(m_VarId, true);
            IEnumerator custom = OnDestruct?.Invoke(this);
            if (custom != null) {
                m_Routine.Replace(this, DestroyRoutine(custom)).Tick();
            } else {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator DestroyRoutine(IEnumerator custom) {
            yield return custom;
            gameObject.SetActive(false);
        }

        #region IBaked

        #if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags) {
            return Ref.Replace(ref m_VarId, ScriptObject.MapPersistenceId(GetComponent<ScriptObject>(), "destroyed"));
        }

        #endif // UNITY_EDITOR

        #endregion // IBaked
    }
}