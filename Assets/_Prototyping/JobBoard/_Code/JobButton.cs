using UnityEngine;
using Aqua;
using BeauUtil;
using BeauData;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace ProtoAqua.JobBoard
{
    public class JobButton : MonoBehaviour
    {
        [SerializeField] private LocText JobName = null;
        [SerializeField] private TextMeshProUGUI JobReward = null;

        [SerializeField] public Image Icon = null;

        [SerializeField] public Transform ButtonTransform = null;

        public JobDesc Job { get; set; }

        public StringHash32 JobId { get; set; }

        public PlayerJobStatus Status { get; set; }

        public void SetupJob(JobDesc job, PlayerJob player)
        {
            Job = job;
            JobId = job.Id();
            if (player == null)
            {
                Status = PlayerJobStatus.NotStarted;
            }
            else
            {
                Status = player.Status();
            }

            SetJobName(job.NameId());
            SetReward(job.CashReward());
            SetIcon(job.Icon());
            return;
        }

        public void SetJobName(StringHash32 jobName) {
            if(!jobName.IsEmpty) {
                JobName.SetText(jobName);
            }
            else {
                JobName.SetText(StringHash32.Null);
            }
        }

        public void SetReward(int reward) {
            JobReward.SetText(reward.ToString());
        }

        public void SetIcon(Sprite sprite) {
            if(sprite != null) {
                Icon.sprite = sprite;
            }
        }

        public Transform GetTransform()
        {
            return ButtonTransform;
        }
    }
}