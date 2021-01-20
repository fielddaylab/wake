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
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using BeauPools;
using System;
using Aqua;
using BeauUtil.Debugger;

namespace Aqua.Portable
{
    public class StatusApp : PortableMenuApp
    {

        #region Inspector

        [Header("Types")]
        [SerializeField, Required] private Toggle m_JobToggle = null;

        [SerializeField, Required] private Toggle m_ResourceToggle = null;

        [Header("Job")]
        [SerializeField, Required] private Transform JobSelect = null;
        [SerializeField, Required] private Transform DefaultJob = null;

        #endregion

        [NonSerialized] private PortableMenu m_ParentMenu = null;

        [NonSerialized] private PortableTweaks m_Tweaks = null;

        [NonSerialized] private PlayerJob currentJob = null;

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
            PlayerJob job = null;
            if (currentJob == null || !currentJob.IsInProgress())
            {
                job = Services.Data.Profile.Jobs.CurrentJob;

                if (job == null)
                {
                    Debug.Log("No current jobs found.");
                    JobSelect.gameObject.SetActive(false);
                    DefaultJob.gameObject.SetActive(true);

                }
                else
                {
                    SetupJobStatus(job);
                    JobSelect.gameObject.SetActive(true);
                    DefaultJob.gameObject.SetActive(false);
                }

            }
        }

        private void SetupJobStatus(PlayerJob job)
        {
            JobSelect JobDisplay = JobSelect.GetComponent<JobSelect>();
            JobDisplay.SetupJobSelect(job);

        }

        private void LoadResource()
        {
            return;
        }



    }
    #endregion
}