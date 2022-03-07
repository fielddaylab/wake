using UnityEngine;
using BeauRoutine;
using BeauUtil;
using System;
using Aqua.Cameras;

namespace Aqua.Ship
{
    public class Room : MonoBehaviour, IKeyValuePair<StringHash32, Room>, IBakedComponent
    {
        #region Inspector

        [SerializeField] private SerializedHash32 m_Id = null;
        
        [Header("Camera Settings")]
        [SerializeField, Required(ComponentLookupDirection.Self)] private CameraPose m_CameraTarget = null;

        [Header("Objects")]
        [SerializeField] private ColorGroup m_RootRenderingGroup = null;
        [SerializeField] private GameObject m_ScriptingGroup = null;
        [SerializeField, HideInInspector] private RoomLink[] m_Links;

        #endregion // Inspector

        [NonSerialized] private ParticleSystem[] m_ParticleSystems;

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, Room>.Key { get { return Id(); } }
        Room IKeyValuePair<StringHash32, Room>.Value { get { return this; } }

        #endregion // KeyValue

        public StringHash32 Id() { return m_Id; }

        public void Initialize()
        {
            m_ParticleSystems = GetComponentsInChildren<ParticleSystem>(false);
            Hide();
        }

        public void Enter()
        {
            Services.Camera.SnapToPose(m_CameraTarget);
            Services.Events.Dispatch(GameEvents.RoomChanged, m_Id.Source());
            Save.Map.RecordVisitedLocation(m_Id);

            Show();
        }

        public void Exit()
        {
            Hide();
        }

        private void Show()
        {
            if (m_RootRenderingGroup)
            {
                m_RootRenderingGroup.Visible = true;
            }

            if (m_ScriptingGroup)
            {
                m_ScriptingGroup.gameObject.SetActive(true);
            }

            foreach(var particleSystem in m_ParticleSystems)
            {
                if (particleSystem.main.prewarm)
                {
                    particleSystem.Simulate(particleSystem.main.duration, true, true, false);
                    particleSystem.Play();
                }
                else
                {
                    particleSystem.Play();
                }
            }
        }

        private void Hide()
        {
            if (m_RootRenderingGroup)
            {
                m_RootRenderingGroup.Visible = false;
            }

            if (m_ScriptingGroup)
            {
                m_ScriptingGroup.gameObject.SetActive(false);
            }

            foreach(var particleSystem in m_ParticleSystems)
            {
                particleSystem.Clear();
                particleSystem.Stop();
            }
        }

        #if UNITY_EDITOR

        void IBakedComponent.Bake()
        {
            m_Links = GetComponentsInChildren<RoomLink>(true);
        }

        #endif // UNITY_EDITOR
    }
}

