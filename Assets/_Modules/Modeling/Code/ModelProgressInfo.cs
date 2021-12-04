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

        public void LoadFromJob(BestiaryDesc currentEnvironment, JobDesc desc) {
            LoadFromScope(currentEnvironment, desc?.FindAsset<JobModelScope>());
        }

        public void LoadFromScope(BestiaryDesc currentEnvironment, JobModelScope scope) {
            if (!currentEnvironment || !scope || scope.EnvironmentId != currentEnvironment.Id()) {
                Reset(currentEnvironment);
                return;
            }

            Sim = currentEnvironment.FactOfType<BFSim>();
            Scope = scope;

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

            ImportableEntities.Clear();
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

            if (Sim) {
                Phases |= ModelPhases.Sync;
            }

            RequiredEntities.Clear();
            RequiredFacts.Clear();
        }
    }
}