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
using System.Collections;

namespace Aqua.Portable
{
    public class JobApp : PortableMenuApp
    {
        #region Inspector

        [Header("Active Job")]
        [SerializeField, Required] private JobInfoDisplay m_JobDisplay = null;
        [SerializeField, Required] private PortableJobTaskList m_JobTaskList = null;
        [SerializeField, Required] private AppearAnimSet m_JobAppearAnim = null;
        [SerializeField, Required] private LayoutGroup m_LayoutRebuilder = null;
        
        [Header("No Job")]
        [SerializeField, Required] private Transform m_NoJobDisplay = null;

        #endregion

        #region Panel

        protected override void OnShowComplete(bool inbInstant)
        {
            base.OnShowComplete(inbInstant);
            LoadData();
        }

        protected override void OnHide(bool inbInstant)
        {
            base.OnHide(inbInstant);
        }

        #endregion // Panel

        #region Page Display

        protected override IEnumerator LoadData()
        {
            JobsData jobsData = Save.Jobs;
            PlayerJob currentJob = jobsData.CurrentJob;
            
            if (!currentJob.IsValid)
            {
                m_JobDisplay.gameObject.SetActive(false);
                m_NoJobDisplay.gameObject.SetActive(true);
            }
            else
            {
                m_NoJobDisplay.gameObject.SetActive(false);
                m_JobDisplay.gameObject.SetActive(true);
                m_JobDisplay.Populate(currentJob.Job, currentJob.Status);
                yield return Routine.Amortize(m_JobTaskList.LoadTasks(currentJob.Job, jobsData), 5);
                m_LayoutRebuilder.ForceRebuild();
                yield return null;

                float delay = m_JobAppearAnim.Play();
                m_JobTaskList.AnimateTasks(delay);
            }
        }

        #endregion // Page Display
    }
}