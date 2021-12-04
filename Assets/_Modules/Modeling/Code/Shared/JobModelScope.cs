using BeauUtil;
using UnityEngine;

namespace Aqua.Modeling {

    [CreateAssetMenu(menuName = "Aqualab Content/Job Model Scope", fileName = "NewJobModelScope")]
    public class JobModelScope : ScriptableObject {
        [FilterBestiaryId(BestiaryDescCategory.Environment)] public StringHash32 EnvironmentId = default;

        [Header("Requirements")]
        [FilterBestiaryId(BestiaryDescCategory.Critter)] public StringHash32[] OrganismIds = null;
        [FactId(typeof(BFBehavior))] public StringHash32[] BehaviorIds = null;
        [Range(0, 100)] public int MinimumSyncAccuracy = 85;
        public bool IncludeWaterChemistryInAccuracy;
        
        [Header("Models")]
        [FactId(typeof(BFModel))] public StringHash32 ConceptualModelId = null;
        [FactId(typeof(BFModel))] public StringHash32 SyncModelId = null;
        [FactId(typeof(BFModel))] public StringHash32 PredictModelId = null;
        [FactId(typeof(BFModel))] public StringHash32 InterveneModelId = null;
    }
}