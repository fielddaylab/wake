using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public class ShowIfUpgrade : MonoBehaviour, ISceneLoadHandler
    {
        #region Inspector

        [SerializeField] private SerializedHash32 m_ItemName = null;
        [SerializeField] private GameObject[] m_Objects = null;
        
        #endregion // Inspector

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private bool m_LastState;

        private void Awake()
        {
            Services.Events.Register(GameEvents.InventoryUpdated, Refresh, this);
        }

        private void OnDestroy()
        {
            if (!Services.Valid)
                return;

            Services.Events?.DeregisterAll(this);
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            Refresh();
        }

        private void Refresh()
        {
            SetState(Services.Data.Profile.Inventory.HasUpgrade(m_ItemName));
        }

        private void SetState(bool inbState)
        {
            if (m_Initialized && inbState == m_LastState)
                return;

            m_Initialized = true;
            m_LastState = inbState;
            foreach(var obj in m_Objects)
                obj.SetActive(inbState);
        }
    }
}