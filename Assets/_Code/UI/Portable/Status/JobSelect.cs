using UnityEngine;
using Aqua;
using BeauUtil;
using BeauData;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace Aqua.Portable
{
    public class JobSelect : MonoBehaviour
    {
        
        [SerializeField] Transform JobSelectTransform = null;
        [SerializeField] LocText JobName = null;
        [SerializeField] LocText Postee = null;
        [SerializeField] LocText JobDescr = null;
        [SerializeField] TextMeshProUGUI CashReward = null;
        [SerializeField] TextMeshProUGUI GearReward = null;

        [SerializeField] Transform DiffContainer = null;

        private JobDesc job = null;
        private DifficultyCategory[] DifficultyList = null;

        public PlayerJobStatus status { get; set; }

        public Transform GetTransform()
        {
            return JobSelectTransform;
        }

        public void SetupJobSelect(PlayerJob pJob)
        {
            if (pJob != null)
            {
                job = pJob.Job;
                status = pJob.Status();
            }
            else
            {
                throw new ArgumentNullException("both JobButton and PlayerJob parameters are null in JobSelect");
            }
            JobName.SetText(job.NameId());
            Postee.SetText(job.PosterId());
            CashReward.SetText(job.CashReward().ToString());
            GearReward.SetText(job.GearReward().ToString());
            JobDescr.SetText(job.DescId());

            DifficultyList = null;

            SetupDifficulties();

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