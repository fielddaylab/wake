using System.Collections.Generic;
using BeauRoutine.Extensions;
using BeauUtil;
using ScriptableBake;
using UnityEngine;

namespace Aqua.View {
    public sealed class RoomLinkUpdater : MonoBehaviour, ISceneLoadHandler, IBaked {
        [SerializeField, HideInInspector] private RoomLink[] m_AllLinks = null;

        private void RefreshLinks(bool force) {
            StringHash32 currentMapId = MapDB.LookupCurrentMap();
            foreach(var link in m_AllLinks) {
                RoomLink.UpdateStatus(link, currentMapId, force);
            }
        }

        private void Awake() {
            Services.Events.Register(GameEvents.ViewLockChanged, () => RefreshLinks(false), this);
        }

        private void OnDestroy() {
            Services.Events?.DeregisterAll(this);
        }

        #region ISceneLoadHandler

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            RefreshLinks(true);
        }

        #endregion // ISceneLoadHandler

        #if UNITY_EDITOR

        #region IBaked

        int IBaked.Order => 3;

        bool IBaked.Bake(BakeFlags flags) {
            List<RoomLink> allLinks = new List<RoomLink>();
            SceneHelper.ActiveScene().Scene.GetAllComponents<RoomLink>(true, allLinks);
            m_AllLinks = allLinks.ToArray();
            return true;
        }

        #endregion // IBaked

        #endif // UNITY_EDITOR
    }
}