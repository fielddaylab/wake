using System;
using BeauData;
using UnityEditor;
using UnityEngine;

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

        public Sprite sprite;
        
        
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
        

            ioSerializer.AssetRef("SpriteName", ref sprite);
            
        }

    }


}