using System.Collections;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Modeling {

    public class ModelingCtrl : MonoBehaviour, ISceneLoadHandler {
        public const ModelPhases ComputationalPhaseMask = ModelPhases.Sync | ModelPhases.Predict | ModelPhases.Intervene;

        #region Inspector

        [SerializeField] private ModelActivityHeader m_Header = null;
        [SerializeField] private ModelEcosystemHeader m_EcosystemHeader = null;
        [SerializeField] private BestiaryAddPanel m_EcosystemSelect = null;
        [SerializeField] private ModelWorldDisplay m_World = null;
        [SerializeField] private SimulationDataCtrl m_SimDataCtrl = null;
        [SerializeField] private ConceptualModelUI m_ConceptualUI = null;
        [SerializeField] private SimulationUI m_SimulationUI = null;

        #endregion // Inspector

        private readonly ModelState m_State = new ModelState();
        private readonly ModelProgressInfo m_ProgressionInfo = new ModelProgressInfo();

        #region Unity

        private void Awake() {
            m_Header.OnPhaseChanged = OnPhaseChanged;
            m_EcosystemSelect.OnAdded = OnEcosystemSelected;
            m_EcosystemSelect.OnRemoved = OnEcosystemRemoved;
            m_EcosystemSelect.OnCleared = OnEcosystemCleared;

            m_ConceptualUI.OnRequestImport = OnRequestConceptualImport;
            m_ConceptualUI.OnRequestExport = OnRequestConceptualExport;

            m_ConceptualUI.SetData(m_State, m_ProgressionInfo);
            m_SimulationUI.SetData(m_State, m_ProgressionInfo);
            m_SimDataCtrl.SetData(m_State, m_ProgressionInfo);
            
            Services.Events.Register(GameEvents.BestiaryUpdated, OnBestiaryUpdated, this);
            
            m_World.SetData(m_State);
        }

        private void OnDestroy() {
            Services.Events?.DeregisterAll(this);
        }

        #endregion // Unity

        #region Callbacks

        private void OnPhaseChanged(ModelPhases phase) {
            ModelPhases prevPhase = m_State.Phase;
            m_State.Phase = phase;

            // basic state
            if (phase == ModelPhases.Ecosystem) {
                m_EcosystemSelect.Show();
                if (prevPhase != ModelPhases.Ecosystem) {
                    m_EcosystemHeader.Hide();
                    m_World.Hide();
                }
            } else {
                m_EcosystemSelect.Hide();
                if (prevPhase == ModelPhases.Ecosystem) {
                    m_EcosystemHeader.Show(m_State.Environment);
                    m_World.Show();
                }
            }

            if (phase == ModelPhases.Concept) {
                m_ConceptualUI.Show();
            } else {
                m_ConceptualUI.Hide();
            }

            if (phase >= ModelPhases.Sync) {
                m_SimulationUI.Show();
            } else {
                m_SimulationUI.Hide();
            }
        }

        private void OnEcosystemSelected(BestiaryDesc selected) {
            if (!Ref.Replace(ref m_State.Environment, selected)) {
                return;
            }

            m_State.SiteData = Save.Science.GetSiteData(selected.Id());
            m_State.Conceptual.LoadFrom(m_State.SiteData);

            m_ProgressionInfo.Load(selected, Assets.Job(Save.Jobs.CurrentJobId));

            EvaluateConceptStatus();
            RefreshPhaseHeader();
        }

        private void OnEcosystemRemoved(BestiaryDesc selected) {
            if (!Ref.CompareExchange(ref m_State.Environment, selected, null)) {
                return;
            }

            Async.InvokeAsync(() => {
                if (m_State.Environment != null) {
                    return;
                }

                m_State.Conceptual.Reset();
                m_State.SiteData = null;

                m_ProgressionInfo.Reset(null);
                m_Header.SetSelected(ModelPhases.Ecosystem, false);

                RefreshPhaseHeader();
            });
        }

        private void OnEcosystemCleared() {
            if (!Ref.Replace(ref m_State.Environment, null)) {
                return;
            }

            m_State.Conceptual.Reset();
            m_State.SiteData = null;
            
            m_ProgressionInfo.Reset(null);
            m_Header.SetSelected(ModelPhases.Ecosystem, false);
            
            RefreshPhaseHeader();
        }

        private void OnBestiaryUpdated() {
            EvaluateConceptStatus();
            RefreshPhaseHeader();
        }

        private IEnumerator OnRequestConceptualImport() {
            return Routine.Start(this, ImportProcess()).Wait();
        }

        private void OnRequestConceptualExport() {

        }

        private IEnumerator ImportProcess() {
            yield return Async.Schedule(ImportProcess_Data(), AsyncFlags.HighPriority);
            yield return m_World.Reconstruct();
        }

        private IEnumerator ImportProcess_Data() {
            foreach(var entity in m_State.Conceptual.PendingEntities) {
                m_State.Conceptual.GraphedEntities.Add(entity);
                m_State.SiteData.GraphedCritters.Add(entity.Id());
                yield return null;
            }

            foreach(var fact in m_State.Conceptual.PendingFacts) {
                m_State.Conceptual.GraphedFacts.Add(fact);
                m_State.SiteData.GraphedFacts.Add(fact.Id);
                yield return null;
            }

            m_State.Conceptual.PendingEntities.Clear();
            m_State.Conceptual.PendingFacts.Clear();
            m_State.SiteData.OnChanged();

            Services.Events.QueueForDispatch(GameEvents.SiteDataUpdated, m_State.SiteData.MapId);
            yield return null;
            EvaluateConceptStatus();
            RefreshPhaseHeader();

            m_SimDataCtrl.TESTBuildProfile(); // TODO: Remove
        }

        #endregion // Callbacks

        #region Evaluation

        private void RefreshPhaseHeader() {
            EvaluatePhaseMask();
            EvaluateHighlightMask();
        }

        private void EvaluateConceptStatus() {
            ConceptualModelState.StatusId status = ConceptualModelState.StatusId.UpToDate;
            FactUtil.GatherPendingEntities(m_ProgressionInfo.ImportableEntities, Save.Bestiary, m_State.Conceptual.GraphedEntities, m_State.Conceptual.PendingEntities);
            FactUtil.GatherPendingFacts(m_ProgressionInfo.ImportableFacts, Save.Bestiary, m_State.Conceptual.GraphedFacts, m_State.Conceptual.PendingFacts);

            if (m_State.Conceptual.PendingEntities.Count > 0 || m_State.Conceptual.PendingFacts.Count > 0) {
                status = ConceptualModelState.StatusId.PendingImport;
            } else if (!HasRequiredEntities() || !HasRequiredBehaviors()) {
                status = ConceptualModelState.StatusId.MissingData;
            } else if (m_ProgressionInfo.Scope != null && !m_ProgressionInfo.Scope.ConceptualModelId.IsEmpty && !Save.Bestiary.HasFact(m_ProgressionInfo.Scope.ConceptualModelId)) {
                status = ConceptualModelState.StatusId.ExportReady;
            } else {
                status = ConceptualModelState.StatusId.UpToDate;
            }

            m_State.Conceptual.Status = status;
        }

        private void EvaluatePhaseMask() {
            ModelPhases mask = m_ProgressionInfo.Phases;
            if (m_ProgressionInfo.Scope != null) {
                if (ConceptualStatusWillBlockProgression(m_State.Conceptual.Status) || !HasRequiredModel(m_ProgressionInfo.Scope.ConceptualModelId)) {
                    mask &= ~ComputationalPhaseMask;
                } else if (!HasRequiredModel(m_ProgressionInfo.Scope.SyncModelId)) {
                    mask &= ~(ModelPhases.Predict | ModelPhases.Intervene);
                } else if (!HasRequiredModel(m_ProgressionInfo.Scope.PredictModelId)) {
                    mask &= ~ModelPhases.Intervene;
                }
            }

            m_Header.UpdateAllowedMask(mask);
        }

        private void EvaluateHighlightMask() {
            ModelPhases mask = 0;
            if (m_State.Conceptual.Status == ConceptualModelState.StatusId.ExportReady || m_State.Conceptual.Status == ConceptualModelState.StatusId.PendingImport) {
                mask |= ModelPhases.Concept;
            }

            m_Header.UpdateHighlightMask(mask);
        }

        static private bool HasRequiredModel(StringHash32 modelId) {
            return modelId.IsEmpty || Save.Bestiary.HasFact(modelId);
        }

        private bool HasRequiredEntities() {
            foreach(var requirement in m_ProgressionInfo.RequiredEntities) {
                if (!m_State.Conceptual.GraphedEntities.Contains(requirement)) {
                    return false;
                }
            }

            return true;
        }

        private bool HasRequiredBehaviors() {
            foreach(var requirement in m_ProgressionInfo.RequiredFacts) {
                if (!m_State.Conceptual.GraphedFacts.Contains(requirement)) {
                    return false;
                }
            }

            return true;
        }

        static private bool ConceptualStatusWillBlockProgression(ConceptualModelState.StatusId status) {
            switch(status) {
                case ConceptualModelState.StatusId.UpToDate: {
                    return false;
                }

                default: {
                    return true;
                }
            }
        }

        #endregion // Evaluation

        #region ISceneLoadHandler

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            m_Header.SetInputActive(true);
            m_Header.UpdateAllowedMask(ModelPhases.Ecosystem);
            m_Header.SetSelected(ModelPhases.Ecosystem, true);
        }

        #endregion // ISceneLoadHandler
    }
    
    public enum ModelPhases : byte {
        Ecosystem = 0x01,
        Concept = 0x02,
        Sync = 0x04,
        Predict = 0x08,
        Intervene = 0x10
    }
}