using UnityEngine;
using Aqua;
using ProtoAqua;
using BeauUtil;
using BeauData;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace ProtoAqua.JobBoard
{
    public class JobSelect : MonoBehaviour
    {
        
        [SerializeField] Transform JobSelectTransform = null;
        [SerializeField] LocText JobName = null;
        [SerializeField] LocText Postee = null;
        [SerializeField] LocText JobDescr = null;
        [SerializeField] TextMeshProUGUI CashReward = null;
        [SerializeField] TextMeshProUGUI GearReward = null;

        [SerializeField] Transform StatusJobButton = null;

        [SerializeField] TextMeshProUGUI StatusJobButtonText = null;

        [SerializeField] Transform DiffContainer = null;

        private JobDesc job = null;

        private JobButton jobButton = null;
        private DifficultyCategory[] DifficultyList = null;

        public PlayerJobStatus status { get; set; }

        public Transform GetTransform()
        {
            return JobSelectTransform;
        }

        public Transform GetStatusButton()
        {
            return StatusJobButton;
        }

        public JobButton GetJobButton()
        {
            return jobButton;
        }

        public void SetupJobSelect(JobButton button)
        {
            job = button.Job;
            jobButton = button;
            status = button.Status;
            JobName.SetText(job.NameId());
            Postee.SetText(job.PosterId());
            CashReward.SetText(job.CashReward().ToString());
            GearReward.SetText(job.GearReward().ToString());
            JobDescr.SetText(job.DescId());

            DifficultyList = null;

            SetupDifficulties();

            SetupStatusButton();
        }

        public void SetupStatusButton()
        {
            if (jobButton == null)
            {
                StatusJobButton.gameObject.SetActive(false);
                return;
            }

            StatusJobButton.gameObject.SetActive(true);

            if (jobButton.Status.Equals(PlayerJobStatus.NotStarted))
            {
                StatusJobButtonText.SetText("Accept");
            }
            else if (jobButton.Status.Equals(PlayerJobStatus.InProgress))
            {
                StatusJobButtonText.SetText("Set As Active");
            }
            else
            {
                StatusJobButton.gameObject.SetActive(false);
            }
        }

        public void SetupDifficulties()
        {
            // TODO : check if number of category objects are aligned with number of diff types
            DifficultyList = DiffContainer.GetComponentsInChildren<DifficultyCategory>(true);
            if (DifficultyList.Length < 3)
            {
                throw new Exception("Not enough diffcontainers");
            }
            int i = 0;
            foreach (DifficultyType dtype in (DifficultyType[])Enum.GetValues(typeof(DifficultyType)))
            {
                DifficultyList[i++].DifficultySetup(job, dtype);
            }
        }










    }    
}