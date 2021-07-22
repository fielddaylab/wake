using System;
using Aqua;
using BeauUtil.Debugger;

namespace ProtoAqua.ExperimentV2
{
    [Serializable]
    public class ObservationActorDefinition
    {
        public BestiaryDesc Type;
        public ActorStateTransitionSet Transitions;

        public void LoadFromBestiary(BestiaryDesc inBestiary)
        {
            Assert.NotNull(inBestiary);
            Assert.True(inBestiary.Category() == BestiaryDescCategory.Critter, "Provided entry is not for a critter");
            Assert.True(!inBestiary.HasFlags(BestiaryDescFlags.DoNotUseInExperimentation), "Provided entry is not usable in experimentation");
            
            Transitions = inBestiary.GetActorStateTransitions();
        }
    }
}