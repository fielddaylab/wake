using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoAqua.JobBoard
{
    public class Player : MonoBehaviour
    {
        ArrayList acceptedJobs = new ArrayList();
        ArrayList availableJobs = new ArrayList();
        ArrayList completedJobs = new ArrayList();
  
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
            acceptedJobs.Add(jobId);
        }

        public void completeJob(string jobId) {
            //Check some condition to make sure the job can be completed

            if(!acceptedJobs.Contains(jobId)){
                Debug.Log("Error: completing unaccepted job");
                return;
            }
            acceptedJobs.Remove(jobId);
            completedJobs.Add(jobId);
        }

        public ArrayList getAvailableJobs() {
            return availableJobs;
        }
        public ArrayList getAcceptedJobs() {
            return acceptedJobs;
        }
        public ArrayList getCompletedJobs() {
            return completedJobs;
        }
    }
}
