using BeauUtil;
using UnityEngine;
using System;

namespace Aqua
{
    [Serializable]
    public class JobTask
    {
        public SerializedHash32 Id;
        public ushort Index;
        
        public TextId LabelId;
        public TextId DescriptionId;
        
        public JobStep[] Steps;
        public ushort[] PrerequisiteTaskIndices;
    }
}