using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using ScriptableBake;
using UnityEngine;

namespace Aqua.Modeling {

    [CreateAssetMenu(menuName = "Aqualab Content/Job Model Scope", fileName = "NewJobModelScope")]
    public class JobModelScope : ScriptableObject, IBaked {
        [FilterBestiaryId(BestiaryDescCategory.Environment)] public StringHash32 EnvironmentId = default;

        [Header("Requirements")]
        [FilterBestiaryId(BestiaryDescCategory.Critter)] public StringHash32[] OrganismIds = null;
        [FactId(typeof(BFBehavior))] public StringHash32[] BehaviorIds = null;
        [Range(0, 100)] public int MinimumSyncAccuracy = 85;
        public bool IncludeWaterChemistryInAccuracy;
        public ActorCountRange[] InterventionTargets;
        
        [Header("Models")]
        [FactId(typeof(BFModel))] public StringHash32 ConceptualModelId = null;
        [FactId(typeof(BFModel))] public StringHash32 SyncModelId = null;
        [FactId(typeof(BFModel))] public StringHash32 PredictModelId = null;
        [FactId(typeof(BFModel))] public StringHash32 InterveneModelId = null;

        #if UNITY_EDITOR

        int IBaked.Order { get { return 99; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            List<BFBase> validFacts = new List<BFBase>();

            BestiaryDesc env = Assets.Bestiary(EnvironmentId);
            if (!env) {
                Log.Error("[JobModelScope] Environment id '{0}' not found for JobModelScope '{1}'", EnvironmentId, name);
            } else {
                validFacts.AddRange(env.Facts);
            }

            foreach(var organismId in OrganismIds) {
                BestiaryDesc organism = Assets.Bestiary(organismId);
                if (!organism) {
                    Log.Error("[JobModelScope] Organism id '{0}' not found for JobModelScope '{1}'", organismId, name);
                } else if (env != null && !env.HasOrganism(organismId)) {
                    Log.Error("[JobModelScope] Organism '{0}' is not present in environment '{1]' for JobModelScope '{2}'", organismId, EnvironmentId, name);
                }

                if (organism) {
                    validFacts.AddRange(organism.Facts);
                }
            }

            foreach(var behaviorId in BehaviorIds) {
                BFBase fact = Assets.Fact(behaviorId);
                if (!fact) {
                    Log.Error("[JobModelScope] Behavior id '{0}' not found for JobModelScope '{1}'", behaviorId, name);
                } else if (!validFacts.Contains(fact)) {
                    Log.Error("[JobModelScope] Behavior id '{0}' not within the list of discovered facts for JobModelScope '{1}'", behaviorId, name);
                }
            }

            if (!ConceptualModelId.IsEmpty && !Assets.Fact(ConceptualModelId)) {
                Log.Error("[JobModelScope] Unrecognized model '{0}' for JobModelScope '{1}'", ConceptualModelId, name);
            }

            if (!SyncModelId.IsEmpty && !Assets.Fact(SyncModelId)) {
                Log.Error("[JobModelScope] Unrecognized model '{0}' for JobModelScope '{1}'", SyncModelId, name);
            }

            if (!PredictModelId.IsEmpty && !Assets.Fact(PredictModelId)) {
                Log.Error("[JobModelScope] Unrecognized model '{0}' for JobModelScope '{1}'", PredictModelId, name);
            }

            if (!InterveneModelId.IsEmpty && !Assets.Fact(InterveneModelId)) {
                Log.Error("[JobModelScope] Unrecognized model '{0}' for JobModelScope '{1}'", InterveneModelId, name);
            }

            return false;
        }

        #endif // UNITY_EDITOR
    }
}