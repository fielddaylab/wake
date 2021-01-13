using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Aqua;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using BeauData;


namespace ProtoAqua.JobBoard
{
    public class Player : MonoBehaviour
    {
        List<JobDesc> activeJobs = new List<JobDesc>();
        List<JobDesc> availableJobs = new List<JobDesc>();
        List<JobDesc> completedJobs = new List<JobDesc>();
        List<JobDesc> lockedJobs = new List<JobDesc>();
        

        //function unlockJob(jobId);

        public void addAvailableJob(JobDesc job) 
        {
            availableJobs.Add(job);
        }

        public void acceptAvailableJob(StringHash32 jobId) 
        {
            JobDesc currJob = null;

            foreach (JobDesc job in availableJobs)
            {
                if (job.Id() == jobId)
                {
                    currJob = job;
                }
            }

            if(currJob == null)
            {
                Debug.Log("Error: accepted unavailable job");
                return;                
            }
            else
            {
                availableJobs.Remove(currJob);
                activeJobs.Add(currJob);
                
            }


        }

        public void completeJob(StringHash32 jobId) 
        {
            //Check some condition to make sure the job can be completed
            JobDesc currJob = null;

            foreach (JobDesc job in activeJobs)
            {
                if (job.Id() == jobId)
                {
                    currJob = job;
                }
            }

            if(currJob == null)
            {
                Debug.Log("Error: completing unaccepted job");
                return;                
            }
            else
            {
                availableJobs.Remove(currJob);
                activeJobs.Add(currJob);
                
            }
        }

        public List<JobDesc> getAvailableJobs() {
            return availableJobs;
        }
        public List<JobDesc> getActiveJobs() {
            return activeJobs;
        }
        public List<JobDesc> getCompletedJobs() {
            return completedJobs;
        }
    }
}
