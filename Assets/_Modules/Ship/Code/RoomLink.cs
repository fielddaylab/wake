using UnityEngine;
using UnityEngine.EventSystems;
using BeauUtil;
using Aqua.Scripting;

namespace Aqua.Ship
{
    [RequireComponent(typeof(CursorInteractionHint))]
    public class RoomLink : MonoBehaviour, IPointerClickHandler, IBakedComponent
    {
        private enum LinkType
        {
            Room,
            Scene,
            Nav
        }

        #region Inspector

        [SerializeField] private LinkType m_LinkType = LinkType.Room;
        [SerializeField, ShowIfField("ShowRoom")] private Room m_Room = null;
        [SerializeField, ShowIfField("ShowScene")] private string m_Scene = null;
        [SerializeField, ShowIfField("ShowScene")] private string m_Entrance = default;
        [SerializeField, ShowIfField("ShowScene")] private bool m_StopMusic = true;
        [SerializeField, ShowIfField("ShowScene")] private bool m_SuppressAutosave = false;
        [SerializeField] private bool m_AlwaysAvailable = false;
        [Space]
        [Inline(InlineAttribute.DisplayType.HeaderLabel), SerializeField] private EnableDisableGroup m_Objects = null;

        #endregion // Inspector

        [SerializeField, HideInInspector] public StringHash32 LinkId;

        public bool IsAlwaysAvailable() { return m_AlwaysAvailable; }

        private void Awake()
        {
            this.EnsureComponent<CursorInteractionHint>();
        }
        
        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            var roomMgr = RoomManager.Find<RoomManager>();

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
                    roomMgr.LoadScene(m_Scene, m_Entrance);
                    if (m_StopMusic)
                        Services.Audio.StopMusic();
                    if (m_SuppressAutosave)
                        AutoSave.Suppress();
                    break;
            }
        }

        public void Show()
        {
            if (EnableDisableGroup.SetEnabled(m_Objects, true))
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (EnableDisableGroup.SetEnabled(m_Objects, false))
            {
                gameObject.SetActive(false);
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

        void IBakedComponent.Bake()
        {
            switch(m_LinkType)
            {
                case LinkType.Room:
                    LinkId = m_Room.Id();
                    break;

                case LinkType.Scene:
                    {
                        LinkId = StringHash32.Null;
                        foreach(var map in ValidationUtils.FindAllAssets<MapDesc>())
                        {
                            if (map.SceneName() == m_Scene)
                            {
                                LinkId = map.Id();
                                break;
                            }
                        }

                        if (LinkId.IsEmpty)
                            LinkId = m_Scene;

                        break;
                    }
                
                case LinkType.Nav:
                    {
                        LinkId = "nav";
                        break;
                    }
            }
        }

        #endif // UNITY_EDITOR
    }
}

