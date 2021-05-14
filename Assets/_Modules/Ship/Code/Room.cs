using UnityEngine;
using BeauRoutine;
using BeauUtil;
using System;

namespace Aqua.Ship
{
    public class Room : MonoBehaviour, IKeyValuePair<StringHash32, Room>
    {
        #region Inspector

        [SerializeField] private SerializedHash32 m_Id = null;
        
        [Header("Camera Settings")]
        [SerializeField, Required(ComponentLookupDirection.Self)] private Transform m_CameraTarget = null;
        [SerializeField] private float m_CameraHeight = 10;

        [Header("Objects")]
        [SerializeField] private ColorGroup m_RootRenderingGroup = null;
        [SerializeField] private GameObject m_ScriptingGroup = null;

        #endregion // Inspector

        [NonSerialized] private RoomLink[] m_Links;

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, Room>.Key { get { return Id(); } }
        Room IKeyValuePair<StringHash32, Room>.Value { get { return this; } }

        #endregion // KeyValue

        public StringHash32 Id() { return m_Id; }

        public void Initialize()
        {
            m_Links = GetComponentsInChildren<RoomLink>(true);

            Hide();
        }

        public void Enter(Camera inCamera)
        {
            inCamera.transform.SetPosition(m_CameraTarget.position, Axis.XY);
            
            var fovPlane = inCamera.GetComponent<CameraFOVPlane>();
            if (fovPlane != null)
            {
                fovPlane.Target = m_CameraTarget;
                fovPlane.Height = m_CameraHeight;
            }

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
        }
    }
}

