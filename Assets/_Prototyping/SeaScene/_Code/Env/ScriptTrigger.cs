using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;

namespace ProtoAqua.Observation
{
    public class ScriptTrigger : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private string m_ScriptEntrypointId = null;
        [SerializeField] private Collider2D m_Collider = null;
        [SerializeField] private bool m_OnlyOnce = false;

        #endregion // Inspector

        [NonSerialized] private TriggerListener2D m_Listener;

        private void Awake()
        {
            m_Listener = m_Collider.EnsureComponent<TriggerListener2D>();
            m_Listener.TagFilter.Add("Player");
            m_Listener.onTriggerEnter.AddListener(OnEnter);
        }

        private void OnEnter(Collider2D inCollider)
        {
            Services.Script.StartNode(m_ScriptEntrypointId);
            if (m_OnlyOnce)
                gameObject.SetActive(false);
        }
    }
}