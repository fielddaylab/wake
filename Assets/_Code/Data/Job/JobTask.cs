using BeauUtil;
using System;

namespace Aqua
{
    [Serializable]
    public class JobTask
    {
        public SerializedHash32 Id;
        public string IdString;
        public ushort Index;
        
        public TextId LabelId;
        
        public JobStep[] Steps;
        public ushort[] PrerequisiteTaskIndices;
    }
}