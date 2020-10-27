using System.Collections.Generic;
using BeauUtil;

namespace ProtoAqua.Experiment
{
    public class ExperimentResultData
    {
        public ExperimentSetupData Setup;
        public float Duration;
        public readonly HashSet<StringHash32> ObservedBehaviorIds = new HashSet<StringHash32>();

        public void Reset()
        {
            Setup = null;
            Duration = 0;
            ObservedBehaviorIds.Clear();
        }
    }
}