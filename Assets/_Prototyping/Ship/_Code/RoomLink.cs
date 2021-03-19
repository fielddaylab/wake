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
        [SerializeField] private Room m_Room = null;
        [SerializeField] private string m_Scene = null;

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
                    break;

                case LinkType.Room:
                    roomMgr.LoadRoom(m_Room);
                    break;

                case LinkType.Scene:
                    roomMgr.LoadScene(m_Scene);
                    break;
            }
        }
    }
}

