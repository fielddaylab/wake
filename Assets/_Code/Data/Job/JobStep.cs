using System;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [Serializable]
    public struct JobStep
    {
        [AutoEnum] public JobStepType Type;
        public SerializedHash32 Target;

        [ShowIfField("ShowCondition")] public string ConditionString;
        [ShowIfField("ShowAmount")] public int Amount;

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