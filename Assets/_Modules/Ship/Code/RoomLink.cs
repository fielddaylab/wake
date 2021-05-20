using UnityEngine;
using UnityEngine.EventSystems;
using BeauUtil;
using Aqua.Scripting;

namespace Aqua.Ship
{
    [RequireComponent(typeof(CursorInteractionHint))]
    public class RoomLink : MonoBehaviour, IPointerClickHandler
    {
        private enum LinkType
        {
            Room,
            Scene,
            Nav
        }

        [SerializeField] private LinkType m_LinkType = LinkType.Room;
        [SerializeField, ShowIfField("ShowRoom")] private Room m_Room = null;
        [SerializeField, ShowIfField("ShowScene")] private string m_Scene = null;
        [SerializeField, ShowIfField("ShowScene")] private bool m_StopMusic = true;
        [SerializeField, ShowIfField("ShowScene")] private bool m_SuppressAutosave = false;

        private void Awake()
        {
            this.EnsureComponent<CursorInteractionHint>();
        }
        
        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            var roomMgr = Services.State.FindManager<RoomManager>();

            switch(m_LinkType)
            {
                case LinkType.Nav:
                    roomMgr.LoadNavRoom();
                    Services.Audio.StopMusic();
                    break;

                case LinkType.Room:
                    roomMgr.LoadRoom(m_Room);
                    break;

                case LinkType.Scene:
                    roomMgr.LoadScene(m_Scene);
                    if (m_StopMusic)
                        Services.Audio.StopMusic();
                    if (m_SuppressAutosave)
                        AutoSave.Suppress();
                    break;
            }
        }
    
        #if UNITY_EDITOR

        private bool ShowRoom()
        {
            return m_LinkType == LinkType.Room;
        }

        private bool ShowScene()
        {
            return m_LinkType == LinkType.Scene;
        }

        #endif // UNITY_EDITOR
    }
}

