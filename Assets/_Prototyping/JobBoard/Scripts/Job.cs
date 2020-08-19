using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProtoAqua.JobBoard
{
    

    public class Job : MonoBehaviour
    {
        

        public enum JobName {
            Job1,
            Job2,
            Job3,
            Job4
        }

        public static int getJobReward(JobName jobName) {
            switch(jobName) {
                default:
                case JobName.Job1: return 1000;
                case JobName.Job2: return 100;
                case JobName.Job3: return 5000;
                case JobName.Job4: return 1;
            }
        } 

        //Will need sprites
        //Difficulty
        //ID?
        //Description
        
    }

}