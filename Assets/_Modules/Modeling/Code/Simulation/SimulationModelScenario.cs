using BeauUtil;
using UnityEngine;

namespace Aqua.Modeling {

    [CreateAssetMenu(menuName = "Aqualab Content/Simulation Model Scenario", fileName = "NewSimulationModelScenario")]
    public class SimulationModelScenario : ScriptableObject {
        #region Inspector

        [SerializeField, FilterBestiaryId(BestiaryDescCategory.Environment)] private StringHash32 m_EnvironmentId = default;
        [SerializeField, FactId(typeof(BFModel))] private StringHash32 m_PrerequisiteModelId = default;

        [Header("Sync")]

        [SerializeField] private uint m_SyncTickCount = 10;
        [SerializeField, KeyValuePair("Id", "Population")] private ActorCountI32[] m_InitialActors = null;
        [SerializeField, FactId(typeof(BFModel))] private StringHash32 m_SyncModelId = default;

        [Header("Predict")]

        [SerializeField] private uint m_PredictTickCount = 10;
        [SerializeField, FactId(typeof(BFModel))] private StringHash32 m_PredictModelId = default;

        #endregion // Inspector

        public StringHash32 EnvironmentId() { return m_EnvironmentId; }
        public StringHash32 PrerequisiteModelId() { return m_PrerequisiteModelId; }
    }
}