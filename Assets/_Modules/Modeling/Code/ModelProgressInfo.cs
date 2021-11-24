using BeauUtil;

namespace Aqua.Modeling
{
    public class ModelProgressInfo {
        public JobModelScope Scope;
        public BFSim Sim;
        public ModelPhases Phases = ModelPhases.Ecosystem;

        public readonly RingBuffer<BestiaryDesc> ImportableEntities = new RingBuffer<BestiaryDesc>();
        public readonly RingBuffer<BFBase> ImportableFacts = new RingBuffer<BFBase>();

        public readonly RingBuffer<BestiaryDesc> RequiredEntities = new RingBuffer<BestiaryDesc>();
        public readonly RingBuffer<BFBase> RequiredFacts = new RingBuffer<BFBase>();

        public void Load(BestiaryDesc currentEnvironment, JobDesc desc) {
            if (!currentEnvironment || desc == null || (Scope = desc.FindAsset<JobModelScope>()) == null || Scope.EnvironmentId != currentEnvironment.Id()) {
                Reset(currentEnvironment);
                return;
            }

            Sim = currentEnvironment.FactOfType<BFSim>();

            Phases = ModelPhases.Ecosystem | ModelPhases.Concept;
            if (!Scope.SyncModelId.IsEmpty) {
                Phases |= ModelPhases.Sync;
            }
            if (!Scope.PredictModelId.IsEmpty) {
                Phases |= ModelPhases.Predict;
            }
            if (!Scope.InterveneModelId.IsEmpty) {
                Phases |= ModelPhases.Intervene;
            }

            ImportableFacts.Clear();
            FactUtil.GatherImportableFacts(currentEnvironment, ImportableEntities, ImportableFacts);

            RequiredEntities.Clear();
            foreach(var id in Scope.OrganismIds) {
                RequiredEntities.PushBack(Assets.Bestiary(id));
            }

            RequiredFacts.Clear();
            foreach(var id in Scope.BehaviorIds) {
                RequiredFacts.PushBack(Assets.Fact(id));
            }
        }

        public void Reset(BestiaryDesc currentEnvironment) {
            Scope = null;
            Phases = ModelPhases.Ecosystem;

            ImportableEntities.Clear();
            ImportableFacts.Clear();
            if (currentEnvironment) {
                Sim = currentEnvironment.FactOfType<BFSim>();
                Phases |= ModelPhases.Concept;
                FactUtil.GatherImportableFacts(currentEnvironment, ImportableEntities, ImportableFacts);
            } else {
                Sim = null;
            }

            RequiredEntities.Clear();
            RequiredFacts.Clear();
        }
    }
}