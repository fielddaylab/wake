using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using BeauPools;
using System;
using Aqua.Profile;

namespace Aqua.Portable
{
    public class StatusApp : PortableMenuApp
    {
        #region Types

        public enum PageId
        {
            Job,
            Item,
            Tech
        }

        public class OpenToPageRequest : IPortableRequest
        {
            public PageId Page;

            public OpenToPageRequest(PageId inPage)
            {
                Page = inPage;
            }

            public StringHash32 AppId()
            {
                return "status";
            }

            public bool CanClose()
            {
                return true;
            }

            public bool CanNavigateApps()
            {
                return true;
            }

            public bool ForceInputEnabled()
            {
                return false;
            }

            public void Dispose()
            {
            }
        }

        #endregion // Types

        #region Inspector

        [Header("Tabs")]

        [SerializeField, Required] private Toggle m_JobToggle = null;
        [SerializeField, Required] private Toggle m_ItemToggle = null;
        [SerializeField, Required] private Toggle m_TechToggle = null;

        [Header("Job")]

        [SerializeField, Required] private Transform m_JobTab = null;
        [SerializeField, Required] private JobInfoDisplay m_JobDisplay = null;
        [SerializeField, Required] private PortableJobTaskList m_JobTaskList = null;
        [SerializeField, Required] private Transform m_NoJobDisplay = null;

        [Header("Item")]

        [SerializeField, Required] private Transform m_ItemTab = null;
        [SerializeField, Required] private InvItemDisplay m_CoinDisplay = null;
        [SerializeField, Required] private InvItemDisplay m_GearDisplay = null;

        [Header("Tech")]

        [SerializeField, Required] private Transform m_TechTab = null;
        [SerializeField, Required] private PortableUpgradeSection m_TechSubmarineSection = null;
        [SerializeField, Required] private PortableUpgradeSection m_TechExperimentSection = null;
        [SerializeField, Required] private PortableUpgradeSection m_TechTabletSection = null;

        #endregion

        [NonSerialized] private PageId m_CurrentPage;

        protected override void Awake()
        {
            base.Awake();

            m_JobToggle.onValueChanged.AddListener(OnJobToggle);
            m_ItemToggle.onValueChanged.AddListener(OnItemToggle);
            m_TechToggle.onValueChanged.AddListener(OnTechToggle);
        }

        public override bool TryHandle(IPortableRequest inRequest)
        {
            OpenToPageRequest pageRequest = inRequest as OpenToPageRequest;
            if (pageRequest != null)
            {
                Show();
                LoadPage(pageRequest.Page, true);
                return true;
            }

            return false;
        }

        #region Panel

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            m_JobToggle.interactable = true;
            m_ItemToggle.interactable = true;
            m_TechToggle.interactable = true;

            LoadPage(PageId.Job, true);
        }

        protected override void OnHide(bool inbInstant)
        {
            base.OnHide(inbInstant);
        }

        #endregion // Panel

        #region Callbacks

        private void OnJobToggle(bool inbOn)
        {
            if (!inbOn || !IsShowing())
                return;

            LoadPage(PageId.Job, false);
        }

        private void OnItemToggle(bool inbOn)
        {
            if (!inbOn || !IsShowing())
                return;

            LoadPage(PageId.Item, false);
        }

        private void OnTechToggle(bool inbOn)
        {
            if (!inbOn || !IsShowing())
                return;

            LoadPage(PageId.Tech, false);
        }

        #endregion // Callbacks

        #region Page Display

        private void LoadPage(PageId inPage, bool inbForce)
        {
            if (!inbForce && m_CurrentPage == inPage)
                return;
            
            m_CurrentPage = inPage;

            m_JobToggle.SetIsOnWithoutNotify(inPage == PageId.Job);
            m_ItemToggle.SetIsOnWithoutNotify(inPage == PageId.Item);
            m_TechToggle.SetIsOnWithoutNotify(inPage == PageId.Tech);

            switch(inPage)
            {
                case PageId.Job:
                    {
                        LoadJobPage();
                        break;
                    }

                case PageId.Item:
                    {
                        LoadItemPage();
                        break;
                    }
                
                case PageId.Tech:
                    {
                        LoadTechPage();
                        break;
                    }
            }

            Services.Events.Dispatch(GameEvents.PortableStatusTabSelected, inPage);
        }

        private void LoadJobPage()
        {
            m_JobTab.gameObject.SetActive(true);
            m_ItemTab.gameObject.SetActive(false);
            m_TechTab.gameObject.SetActive(false);

            JobsData jobsData = Services.Data.Profile.Jobs;
            PlayerJob currentJob = jobsData.CurrentJob;
            if (currentJob == null)
            {
                m_JobDisplay.gameObject.SetActive(false);
                m_NoJobDisplay.gameObject.SetActive(true);
            }
            else
            {
                m_NoJobDisplay.gameObject.SetActive(false);
                m_JobDisplay.gameObject.SetActive(true);
                m_JobDisplay.Populate(currentJob.Job, currentJob.Status());
                m_JobTaskList.LoadTasks(currentJob.Job, jobsData);
            }
        }

        private void LoadItemPage()
        {
            m_JobTab.gameObject.SetActive(false);
            m_ItemTab.gameObject.SetActive(true);
            m_TechTab.gameObject.SetActive(false);

            InventoryData invData = Services.Data.Profile.Inventory;
            m_CoinDisplay.Populate(invData.GetItem(ItemIds.Cash));
            m_GearDisplay.Populate(invData.GetItem(ItemIds.Gear));
        }

        private void LoadTechPage()
        {
            m_JobTab.gameObject.SetActive(false);
            m_ItemTab.gameObject.SetActive(false);
            m_TechTab.gameObject.SetActive(true);
            
            using(PooledList<InvItem> upgrades = PooledList<InvItem>.Create())
            {
                foreach(var upgrade in Services.Data.Profile.Inventory.GetItems(InvItemCategory.Upgrade))
                {
                    upgrades.Add(Services.Assets.Inventory.Get(upgrade.ItemId));
                }

                upgrades.Sort(InvItem.SortByCategoryAndOrder);

                m_TechSubmarineSection.Clear();
                m_TechExperimentSection.Clear();
                m_TechTabletSection.Clear();

                InvItemSubCategory currentCategory = InvItemSubCategory.None;
                int startIdx = 0;
                InvItem currentItem;

                for(int i = 0; i < upgrades.Count; i++)
                {
                    currentItem = upgrades[i];
                    if (currentItem.SubCategory() != currentCategory)
                    {
                        PopulateTechPage(currentCategory, new ListSlice<InvItem>(upgrades, startIdx, i - startIdx));
                        startIdx = i;
                        currentCategory = currentItem.SubCategory();
                    }
                }

                PopulateTechPage(currentCategory, new ListSlice<InvItem>(upgrades, startIdx, upgrades.Count - startIdx));
            }
        }

        private void PopulateTechPage(InvItemSubCategory inCategory, ListSlice<InvItem> inItems)
        {
            switch(inCategory)
            {
                case InvItemSubCategory.Experimentation:
                    m_TechExperimentSection.Load(inItems);
                    break;

                case InvItemSubCategory.Portable:
                    m_TechTabletSection.Load(inItems);
                    break;

                case InvItemSubCategory.Submarine:
                    m_TechSubmarineSection.Load(inItems);
                    break;
            }
        }

        #endregion // Page Display

        #region Statics

        static public void OpenToPage(PageId inPage)
        {
            var request = new OpenToPageRequest(inPage);
            Services.UI.FindPanel<PortableMenu>().Open(request);
        }

        #endregion // Statics
    }
}