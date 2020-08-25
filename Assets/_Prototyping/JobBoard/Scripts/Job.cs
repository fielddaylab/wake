using BeauData;

namespace ProtoAqua.JobBoard
{
    

    public class Job : ISerializedObject
    {


        //TODO make private and add getters
        public string jobId;
        public int jobIndex;
        public string jobName;
        public string jobDescription;
        public string jobCompletedDescription;
        public string jobPostee;
        public string[] requiredJobs;
        //Equipment array?
        public bool required;
        public int jobReward;
        public bool experimentation;
        public int experimentationDifficulty;
        public bool modeling;
        public int modelingDifficulty;
        public bool argument;
        public int argumentDifficulty;
        public string spritePath;

        
        
        public void Serialize(Serializer ioSerializer) {
            ioSerializer.Serialize("jobId", ref jobId);
            ioSerializer.Serialize("jobIndex", ref jobIndex); //TODO Could just increment this? 
            ioSerializer.Serialize("jobName", ref jobName);
            ioSerializer.Serialize("jobDescription", ref jobDescription);
            ioSerializer.Serialize("jobCompletedDescription", ref jobCompletedDescription);
            ioSerializer.Serialize("jobPostee", ref jobPostee);
            ioSerializer.Array("requiredJobs", ref requiredJobs);
            //Equipment Array
            ioSerializer.Serialize("required", ref required);
            ioSerializer.Serialize("jobReward", ref jobReward);
            ioSerializer.Serialize("experimentation", ref experimentation, false); //default false
            ioSerializer.Serialize("experimentationDifficulty", ref experimentationDifficulty, (int)0);
            ioSerializer.Serialize("modeling", ref modeling, false);
            ioSerializer.Serialize("modelingDifficulty", ref modelingDifficulty, (int)0);
            ioSerializer.Serialize("argument", ref argument, false);
            ioSerializer.Serialize("argumentDifficulty", ref argumentDifficulty, (int)0);
            ioSerializer.Serialize("spritePath", ref spritePath);
        }


        // public static string getJobName(string jobId) {
        //     switch(jobId) {
        //         default:
        //         case "job1": return "The first Job";
        //         case "job2": return "The second Job";
        //         case "job3": return "The third Job";
        //         case "job4": return "The fourth Job";
        //     }
        // }

        // public static int getJobIndex(string jobId) {
        //     switch(jobId) {
        //         default:
        //         case "job1": return 1;
        //         case "job2": return 2;
        //         case "job3": return 3;
        //         case "job4": return 4;
        //     }
        // }

        // public static int getJobReward(string jobId) {
        //     switch(jobId) {
        //         default:
        //         case "job1": return 1000;
        //         case "job2": return 100;
        //         case "job3": return 5000;
        //         case "job4": return 1;
        //     }
        // } 

        // public static string getJobDescription(string jobId) {
        //     switch(jobId) {
        //         default:
        //         case "job1": return "The first Job The first JobThe first Job The first Job The first Job The first Job The first Job The first Job The first Job The first Job";
        //         case "job2": return "The second Job The second Job The second Job The second Job The second Job The second Job The second Job The second Job The second Job";
        //         case "job3": return "The third Job The third Job The third Job The third Job The third Job The third Job The third Job The third Job The third Job The third Job";
        //         case "job4": return "The fourth Job The fourth Job The fourth Job The fourth Job The fourth Job The fourth Job The fourth Job The fourth Job The fourth Job";
        //     }
        // }

        // public static int getJobDifficulty(string jobId) {
        //     switch(jobId) {
        //         default:
        //         case "job1": return 1;
        //         case "job2": return 2;
        //         case "job3": return 3;
        //         case "job4": return 4;
        //     }
        // } 

        // public static string getJobPostee(string jobId) {
        //     switch(jobId) {
        //         default:
        //         case "job1": return "Matt";
        //         case "job2": return "Autumn";
        //         case "job3": return "Nicholas";
        //         case "job4": return "Jenn";
        //     }
        // }

      


        //Will need sprites

    }

}