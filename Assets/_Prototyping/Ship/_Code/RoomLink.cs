using UnityEngine;
using UnityEngine.EventSystems;
using Aqua;
using BeauUtil;

namespace ProtoAqua.Ship
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
        
        [Header("Room Mode")]
        [SerializeField] private Room m_Room = null;
        
        [Header("Scene Mode")]
        [SerializeField] private string m_Scene = null;
        [SerializeField] private bool m_StopMusic = true;

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
                    break;
            }
        }
    }
}

