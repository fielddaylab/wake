using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoAqua.JobBoard
{
    public class Player : MonoBehaviour
    {
        List<string> activeJobs = new List<string>();
        List<string> availableJobs = new List<string>();
        List<string> completedJobs = new List<string>();
        List<string> lockedJobs = new List<string>();

        //function unlockJob(jobId);
  
        public void addAvailableJob(string jobId) {
            availableJobs.Add(jobId);
        }

        public void acceptAvailableJob(string jobId) {
            //This case shouldnt happen
            if(!availableJobs.Contains(jobId)) {
                Debug.Log("Error: accepted unavailable job");
                return;
            }

            availableJobs.Remove(jobId);
            activeJobs.Add(jobId);
        }

        public void completeJob(string jobId) {
            //Check some condition to make sure the job can be completed

            if(!activeJobs.Contains(jobId)){
                Debug.Log("Error: completing unaccepted job");
                return;
            }
            activeJobs.Remove(jobId);
            completedJobs.Add(jobId);
        }

        public List<string> getAvailableJobs() {
            return availableJobs;
        }
        public List<string> getActiveJobs() {
            return activeJobs;
        }
        public List<string> getCompletedJobs() {
            return completedJobs;
        }
    }
}
