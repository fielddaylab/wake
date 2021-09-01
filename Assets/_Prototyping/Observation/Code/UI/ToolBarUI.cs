using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System.Collections;
using Aqua;

namespace ProtoAqua.Observation
{
    public class ToolBarUI : SharedPanel, ISceneLoadHandler
    {
        [SerializeField] private ToolButton m_ScannerButton = null;
        [SerializeField] private ToolButton m_TaggerButton = null;

        protected override void Awake()
        {
            m_ScannerButton.Toggle.onValueChanged.AddListener((b) => { if (b) OnToolSelected(PlayerROV.ToolId.Scanner); });
            m_TaggerButton.Toggle.onValueChanged.AddListener((b) => { if (b) OnToolSelected(PlayerROV.ToolId.Tagger); });

            Services.Events.Register<PlayerROV.ToolId>(PlayerROV.Event_ToolSwitched, OnToolSwitched, this);
            Services.Events.Register(GameEvents.InventoryUpdated, RefreshItemList, this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Services.Events?.DeregisterAll(this);
        }

        private void OnToolSelected(PlayerROV.ToolId inToolId)
        {
            Services.Events.Dispatch(PlayerROV.Event_RequestToolSwitch, inToolId);
        }

        private void OnToolSwitched(PlayerROV.ToolId inToolId)
        {
            switch(inToolId)
            {
                case PlayerROV.ToolId.Scanner:
                    m_ScannerButton.Toggle.SetIsOnWithoutNotify(true);
                    break;

                case PlayerROV.ToolId.Tagger:
                    m_TaggerButton.Toggle.SetIsOnWithoutNotify(true);
                    break;
            }
        }

        private void RefreshItemList()
        {
            bool bHasScanner = Services.Data.Profile.Inventory.HasUpgrade(ItemIds.ROVScanner);
            bool bHasTagger = Services.Data.Profile.Inventory.HasUpgrade(ItemIds.ROVTagger);

            int itemCount = 0;
            if (bHasScanner)
                itemCount++;
            if (bHasTagger)
                itemCount++;

            if (itemCount < 2)
            {
                Hide();
                return;
            }

            Show();
            m_ScannerButton.gameObject.SetActive(bHasScanner);
            m_TaggerButton.gameObject.SetActive(bHasTagger);
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            RefreshItemList();
        }
    }
}