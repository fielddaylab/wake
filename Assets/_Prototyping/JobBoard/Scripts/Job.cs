using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProtoAqua.JobBoard
{
    

    public class Job : MonoBehaviour
    {
        


        public static string getJobName(string jobId) {
            switch(jobId) {
                default:
                case "job1": return "The first Job";
                case "job2": return "The second Job";
                case "job3": return "The third Job";
                case "job4": return "The fourth Job";
            }
        }

        public static int getJobReward(string jobId) {
            switch(jobId) {
                default:
                case "job1": return 1000;
                case "job2": return 100;
                case "job3": return 5000;
                case "job4": return 1;
            }
        } 

        public static string getJobDescription(string jobId) {
            switch(jobId) {
                default:
                case "job1": return "The first Job The first JobThe first Job The first Job The first Job The first Job The first Job The first Job The first Job The first Job";
                case "job2": return "The second Job The second Job The second Job The second Job The second Job The second Job The second Job The second Job The second Job";
                case "job3": return "The third Job The third Job The third Job The third Job The third Job The third Job The third Job The third Job The third Job The third Job";
                case "job4": return "The fourth Job The fourth Job The fourth Job The fourth Job The fourth Job The fourth Job The fourth Job The fourth Job The fourth Job";
            }
        }

        public static int getJobDifficulty(string jobId) {
            switch(jobId) {
                default:
                case "job1": return 1;
                case "job2": return 2;
                case "job3": return 3;
                case "job4": return 4;
            }
        } 

        public static string getJobPostee(string jobId) {
            switch(jobId) {
                default:
                case "job1": return "Matt";
                case "job2": return "Autumn";
                case "job3": return "Nicholas";
                case "job4": return "Jenn";
            }
        }
        

        //Will need sprites
        
    }

}