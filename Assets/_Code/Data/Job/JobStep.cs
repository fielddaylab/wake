using System;
using BeauUtil;

namespace Aqua
{
    [Serializable]
    public struct JobStep
    {
        [AutoEnum] public JobStepType Type;
        public SerializedHash32 Target;

        public string ConditionString;
        public int Amount;
    }
}