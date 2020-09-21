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
        [SerializeField] private string m_TriggerId = null;
        [Space]
        [SerializeField] private Collider2D m_Collider = null;
        [SerializeField] private bool m_OnlyOnce = false;
        [SerializeField] private float m_Delay = 0;

        #endregion // Inspector

        [NonSerialized] private TriggerListener2D m_Listener;
        [NonSerialized] private Routine m_WaitToTriggerRoutine;

        private void Awake()
        {
            m_Listener = m_Collider.EnsureComponent<TriggerListener2D>();
            m_Listener.TagFilter.Add("Player");
            m_Listener.onTriggerEnter.AddListener(OnEnter);
            m_Listener.onTriggerExit.AddListener(OnExit);
        }

        private void OnEnter(Collider2D inCollider)
        {
            m_WaitToTriggerRoutine = Routine.Start(this, WaitToTrigger());
            m_WaitToTriggerRoutine.TryManuallyUpdate();
        }

        private void OnExit(Collider2D inCollider)
        {
            m_WaitToTriggerRoutine.Stop();
        }

        private IEnumerator WaitToTrigger()
        {
            if (Services.UI.IsLetterboxed())
            {
                while(true)
                {
                    yield return null;
                    if (!Services.UI.IsLetterboxed())
                        break;
                }

                yield return Services.Tweaks.Get<ScriptingTweaks>().CutsceneEndNextTriggerDelay();
            }
            
            if (m_Delay > 0)
                yield return m_Delay;
            
            if (!string.IsNullOrEmpty(m_ScriptEntrypointId))
                Services.Script.StartNode(m_ScriptEntrypointId);
            else if (!string.IsNullOrEmpty(m_TriggerId))
                Services.Script.TriggerResponse(m_TriggerId);
            if (m_OnlyOnce)
                gameObject.SetActive(false);
        }
    }
}