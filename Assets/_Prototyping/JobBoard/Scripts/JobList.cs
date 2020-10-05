
using BeauData;
using UnityEngine.Scripting;

namespace ProtoAqua.JobBoard{
    public class JobList : ISerializedObject
    {

        private Job[] jobList;

        [Preserve]
        public JobList() { }
        
        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.ObjectArray("jobList", ref jobList);
        }
        

        //Returns the array of jobs
        public Job[] getJobList() {
            return jobList;
        }

        //Returns a job based off a jobId. 
        //TODO optimize?
        public Job findJob(string jobId) {
                for(int i = 0; i < jobList.Length; i++) {
                    if(jobList[i].jobId.Equals(jobId)) {
                        return jobList[i];
                    }
                }
                //Should throw error?
               return new Job();
        }



    }
}
