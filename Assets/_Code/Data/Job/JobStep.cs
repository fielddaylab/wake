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

        #if UNITY_EDITOR

        private bool ShowCondition()
        {
            return Type == JobStepType.EvaluateCondition;
        }

        private bool ShowAmount()
        {
            return Type == JobStepType.GetItem;
        }

        #endif // UNITY_EDITOR
    }
}