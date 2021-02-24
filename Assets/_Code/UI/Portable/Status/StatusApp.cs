// Portable - [Bestiary, Journal, Status] 
// Bestiary
// Journal
// Status - show current resources(coins, gears), current job

// task - create status
// Assets.Code.UI.Portable
// Look at BestiaryApp
// making a StatusApp
// onShow, onHide - BestiaryApp
// Services.Data.CurrentJob seperate state (no job selected)

// InventoryData - coins, gears
// Active - 1, InProgress - 3

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using BeauPools;
using System;

namespace Aqua.Portable
{
    public class StatusApp : PortableMenuApp
    {

        #region Inspector

        [Header("Types")]

        [SerializeField, Required] private ToggleGroup m_Group = null;
        [SerializeField, Required] private Toggle m_JobToggle = null;
        [SerializeField, Required] private Toggle m_ResourceToggle = null;

        [Header("Default")]

        [SerializeField, Required] private TextMeshProUGUI Title = null;
        [SerializeField, Required] private TextMeshProUGUI Background = null;
        [SerializeField, Required] private TextMeshProUGUI Type = null;
        [SerializeField, Required] private Transform Default = null;

        [Header("Job")]

        [SerializeField, Required] private Transform JobView = null;
        [SerializeField, Required] private JobInfoDisplay m_JobDisplay = null;

        [Header("Resource")]

        [SerializeField, Required] private Transform InventoryView = null;

        [SerializeField, Required] private Transform m_ResourceGroup = null;

        #endregion

        [NonSerialized] private PortableMenu m_ParentMenu = null;

        [NonSerialized] private PortableTweaks m_Tweaks = null;

        [NonSerialized] private PlayerJob currentJob = null;

        [NonSerialized] private List<Transform> itemDisplays = new List<Transform>();

        protected override void Awake()
        {
            base.Awake();

            LoadCurrentJob();

            m_JobToggle.onValueChanged.AddListener(OnJobToggled);
            m_ResourceToggle.onValueChanged.AddListener(OnResourceToggled);

            m_ParentMenu = GetComponentInParent<PortableMenu>();
        }

        #region Callbacks

        private void OnJobToggled(bool inbOn)
        {
            if (!IsShowing()) return;

            if (inbOn)
            {
                LoadCurrentJob();
            }
        }

        private void OnResourceToggled(bool inbOn)
        {
            if (!IsShowing()) return;

            if (inbOn)
            {
                LoadResource();
            }
        }

        protected override void OnShow(bool inbInstant)
        {
            base.OnShow(inbInstant);

            m_JobToggle.SetIsOnWithoutNotify(true);
            m_ResourceToggle.SetIsOnWithoutNotify(false);
            LoadCurrentJob();

            m_JobToggle.interactable = true;
            m_ResourceToggle.interactable = true;

        }

        protected override void OnHide(bool inbInstant)
        {
            base.OnHide(inbInstant);
        }

        private void LoadCurrentJob()
        {
            InventoryView.gameObject.SetActive(false);

            PlayerJob job = null;
            if (currentJob == null || !currentJob.IsInProgress())
            {
                job = Services.Data.Profile.Jobs.CurrentJob;

                if (job == null)
                {
                    Debug.Log("No current jobs found.");

                    JobView.gameObject.SetActive(false);
                    SetupDefault("job", true);

                }
                else
                {
                    SetupJobStatus(job);
                    JobView.gameObject.SetActive(true);
                    SetupDefault("job", false);
                }

            }
        }

        private void SetupDefault(string toggleType, bool Value)
        {
            if (toggleType.Equals("job"))
            {
                Title.SetText("Current Job");
                Background.SetText("Job Portal");
                Type.SetText("jobs");
            }
            else
            {
                Title.SetText("Inventory");
                Background.SetText("Inventory");
                Type.SetText("inventory items");
            }
            
            Default.gameObject.SetActive(Value);

            // foreach (var toggle in m_Group.ActiveToggles())
            // {
            //     if (toggle.Equals(m_JobToggle))
            //     {

            //     }
            // }

        }

        private void SetupJobStatus(PlayerJob job)
        {
            m_JobDisplay.Populate(job.Job);
        }

        private void LoadResource()
        {
            JobView.gameObject.SetActive(false);

            int itemCount = Services.Assets.Inventory.Objects.Count;

            if (itemCount == 0)
            {
                SetupDefault("inventory", true);
            }
            else
            {
                InventoryView.gameObject.SetActive(true);

                m_ResourceGroup.gameObject.GetImmediateComponentsInChildren<Transform>(false, true, itemDisplays);

                if (itemDisplays.Count < itemCount)
                {
                    throw new IndexOutOfRangeException("number of InvItemDisplays is less than items in DB.");
                }

                int i = 0;
                foreach (PlayerInv playItem in Services.Data.Profile.Inventory.Items())
                {
                    itemDisplays[i].GetComponent<InvItemDisplay>().SetupItem(playItem);
                    itemDisplays[i++].gameObject.SetActive(true);
                }

                SetupDefault("", false);
            }


        }

    }

    #endregion
}