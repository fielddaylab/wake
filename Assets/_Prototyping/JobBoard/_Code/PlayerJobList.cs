// using Aqua;
// using System;
// using UnityEngine;
// using BeauData;
// using BeauUtil;
// using UnityEngine.Scripting;
// using System.Collections.Generic;

// namespace ProtoAqua.JobBoard{
//     public class PlayerJobList : MonoBehaviour
//     {


//         private List<JobDesc> AllJobs = new List<JobDesc>();
//         private List<JobDesc> ActiveJobs = new List<JobDesc>();

//         private List<JobDesc> InProgressJobs = new List<JobDesc>();
//         private List<JobDesc> AvailableJobs = new List<JobDesc>();
//         private List<JobDesc> CompletedJobs = new List<JobDesc>();

//         private List<PlayerJob> PlayerList = new List<PlayerJob>();

//         [Preserve]
//         public JobList()
//         {

//         }
        
//         // public void Serialize(Serializer ioSerializer)
//         // {
//         //     ioSerializer.ObjectArray("jobList", ref JobList);
//         // }
        

//         //Returns the array of jobs
//         public List<JobDesc> GetJobList() {
//             return jobList;
//         }

//         public void Add(JobDesc job)
//         {
//             jobList.Add(job);
//         }

//         //Returns a job based off a jobId. 
//         //TODO optimize?
//         public JobDesc findJob(StringHash32 JobId)
//         {
//             foreach (JobDesc job in Services.Assets.Jobs.Objects)
//             {
//                 if (job.Id() == JobId)
//                 {
//                     return job;
//                 }
//             }

//             return null;
//             //Should throw error?
//             //return new Job();
//         }



//     }
// }
