using System.Collections;
using BeauRoutine;
using BeauUtil;
using UnityEngine;
using Aqua.Cameras;
using BeauUtil.Debugger;
using Leaf.Runtime;
using UnityEngine.Scripting;
using System;

namespace Aqua.Modeling {

    public class ModelingCtrl : MonoBehaviour, ISceneLoadHandler {
        public const ModelPhases ComputationalPhaseMask = ModelPhases.Sync | ModelPhases.Predict | ModelPhases.Intervene;

        #region Inspector

        [SerializeField] private ModelActivityHeader m_Header = null;
        [SerializeField] private ModelEcosystemHeader m_EcosystemHeader = null;
        [SerializeField] private InlinePopupPanel m_InlinePopup = null;
        [SerializeField] private BestiaryAddPanel m_EcosystemSelect = null;
        [SerializeField] private ModelWorldDisplay m_World = null;
        [SerializeField] private SimulationDataCtrl m_SimDataCtrl = null;
        [SerializeField] private ConceptualModelUI m_ConceptualUI = null;
        [SerializeField] private SimulationUI m_SimulationUI = null;
        [SerializeField] private CameraPose m_ConceptualCamera = null;
        [SerializeField] private CameraPose m_SimulationCamera = null;

        [Header("-- DEBUG --")]

        [SerializeField] private JobModelScope m_DEBUGModelScope = null;

        #endregion // Inspector

        private readonly ModelState m_State = new ModelState();
        private readonly ModelProgressInfo m_ProgressInfo = new ModelProgressInfo();

        static private ModelingCtrl s_Instance;

        #region Unity

        private void Start() {
            m_Header.OnPhaseChanged = OnPhaseChanged;

            m_EcosystemSelect.OnAdded = OnEcosystemSelected;
            m_EcosystemSelect.OnRemoved = OnEcosystemRemoved;
            m_EcosystemSelect.OnCleared = OnEcosystemCleared;
            m_EcosystemSelect.HighlightFilter = EcosystemHighlightPredicate;

            m_ConceptualUI.OnRequestImport = OnRequestConceptualImport;
            m_ConceptualUI.OnRequestExport = OnRequestConceptualExport;

            m_SimulationUI.OnSyncUnsuccessful = OnSyncFailed;
            m_SimulationUI.OnSyncAchieved = OnSyncAchieved;
            m_SimulationUI.OnPredictCompleted = OnPredictCompleted;
            m_SimulationUI.OnInterventionReset = OnInterventionReset;
            m_SimulationUI.OnAnimationStart = OnAnimationStart;
            m_SimulationUI.OnInterventionSuccessful = OnInterventionCompleted;
            m_SimulationUI.OnInterventionUnsuccessful = OnInterventionUnsuccessful;

            m_SimDataCtrl.OnInterventionUpdated += OnInterventionUpdated;

            m_State.Simulation = m_SimDataCtrl;

            m_State.Display.Status = m_EcosystemHeader.SetStatusText;
            m_State.Display.TextPopup = (t, c) => {
                PopupContent content = default;
                content.Text = Loc.Find(t);
                content.TextColorOverride = c;
                m_InlinePopup.Present(ref content, 0);
            };
            m_State.Display.FactsPopup = (f) => {
                Array.Sort(f, BFType.SortByVisualOrder);
                BFDiscoveredFlags[] flags = new BFDiscoveredFlags[f.Length];
                for(int i = 0; i < flags.Length; i++) {
                    flags[i] = Save.Bestiary.GetDiscoveredFlags(f[i].Id);
                }

                PopupFacts facts = default;
                facts.Facts = f;
                facts.Flags = flags;

                PopupContent content = default;
                content.Facts = facts;

                m_InlinePopup.Present(ref content, 0);
            };
            m_State.Display.ClearPopup = () => m_InlinePopup.Hide();
            m_State.Display.FilterNodes = (any, all, none, force) => {
                m_World.SetFilters(any, all, none, force);
            };

            m_ConceptualUI.SetData(m_State, m_ProgressInfo);
            m_SimulationUI.SetData(m_State, m_ProgressInfo);
            m_SimDataCtrl.SetData(m_State, m_ProgressInfo);
            
            Services.Events.Register(GameEvents.BestiaryUpdated, OnBestiaryUpdated, this);
            
            m_World.SetData(m_State);

            s_Instance = this;
        }

        private void OnDestroy() {
            Services.Events?.DeregisterAll(this);

            s_Instance = null;
        }

        #endregion // Unity

        #region Callbacks

        private void OnPhaseChanged(ModelPhases phase) {
            ModelPhases prevPhase = m_State.Phase;
            m_State.Phase = phase;

            UpdatePhaseVariable(phase);
            m_InlinePopup.Hide();

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
                    m_EcosystemHeader.Show(m_State.Environment, m_ProgressInfo.Scope, !Save.CurrentJobId.IsEmpty);
                    m_World.Show();
                }
            }

            if (prevPhase == ModelPhases.Intervene && phase < ModelPhases.Intervene) {
                m_SimDataCtrl.ClearIntervention();
            }

            if (phase == ModelPhases.Concept) {
                m_ConceptualUI.Show();
            } else {
                m_ConceptualUI.Hide();
            }

            if (phase >= ModelPhases.Sync) {
                if (prevPhase < ModelPhases.Sync) {
                    m_SimDataCtrl.LoadConceptualModel();
                }
                m_SimulationUI.Show();
                m_SimulationUI.SetPhase(phase);
                if (prevPhase < ModelPhases.Sync && m_SimulationUI.IsShowing()) {
                    Services.Camera.MoveToPose(m_SimulationCamera, 0.3f, Curve.CubeOut);
                }
            } else {
                if (prevPhase >= ModelPhases.Sync) {
                    m_SimDataCtrl.ClearSimulatedData();
                    Services.Camera.MoveToPose(m_ConceptualCamera, 0.3f, Curve.CubeOut);
                }
                m_SimulationUI.Hide();
            }

            m_State.OnPhaseChanged?.Invoke(prevPhase, phase);
        }

        private void UpdatePhaseVariable(ModelPhases phase) {
            switch(phase) {
                case ModelPhases.Ecosystem: {
                    Services.Data.SetVariable(ModelingConsts.Var_ModelPhase, ModelingConsts.ModelPhase_Ecosystem);
                    Services.Events.Dispatch(ModelingConsts.Event_Phase_Changed, ModelPhases.Ecosystem);
                    break;
                }
                case ModelPhases.Concept: {
                    Services.Data.SetVariable(ModelingConsts.Var_ModelPhase, ModelingConsts.ModelPhase_Visual);
                    Services.Events.Dispatch(ModelingConsts.Event_Phase_Changed, ModelPhases.Concept);
                    break;
                }
                case ModelPhases.Sync: {
                    Services.Data.SetVariable(ModelingConsts.Var_ModelPhase, ModelingConsts.ModelPhase_Describe);
                    Services.Events.Dispatch(ModelingConsts.Event_Phase_Changed, ModelPhases.Sync);
                    break;
                }
                case ModelPhases.Predict: {
                    Services.Data.SetVariable(ModelingConsts.Var_ModelPhase, ModelingConsts.ModelPhase_Predict);
                    Services.Events.Dispatch(ModelingConsts.Event_Phase_Changed, ModelPhases.Predict);
                    break;
                }
                case ModelPhases.Intervene: {
                    Services.Data.SetVariable(ModelingConsts.Var_ModelPhase, ModelingConsts.ModelPhase_Intervene);
                    Services.Events.Dispatch(ModelingConsts.Event_Phase_Changed, ModelPhases.Intervene);
                    break;
                }
            }
        }

        private void OnEcosystemSelected(BestiaryDesc selected) {
            if (!Ref.Replace(ref m_State.Environment, selected)) {
                return;
            }

            m_State.SiteData = Save.Science.GetSiteData(selected.Id());
            m_State.Conceptual.LoadFrom(m_State.SiteData);

            #if UNITY_EDITOR
            if (BootParams.BootedFromCurrentScene && m_DEBUGModelScope != null) {
                m_ProgressInfo.LoadFromScope(selected, m_DEBUGModelScope);
            } else {
                m_ProgressInfo.LoadFromJob(selected, Assets.Job(Save.Jobs.CurrentJobId));
            }
            #else
            m_ProgressInfo.LoadFromJob(selected, Assets.Job(Save.Jobs.CurrentJobId));
            #endif // UNITY_EDITOR
            m_SimDataCtrl.LoadSite();

            Services.Data.SetVariable(ModelingConsts.Var_EcosystemSelected, selected.Id());
            Services.Data.SetVariable(ModelingConsts.Var_HasJob, m_ProgressInfo.Scope != null);

            Services.Events.Dispatch(ModelingConsts.Event_Ecosystem_Selected, selected.name);

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

                m_ProgressInfo.Reset(null);
                m_SimDataCtrl.ClearSite();

                m_Header.SetSelected(ModelPhases.Ecosystem, false);

                Services.Data.SetVariable(ModelingConsts.Var_EcosystemSelected, null);
                Services.Data.SetVariable(ModelingConsts.Var_HasJob, false);

                RefreshPhaseHeader();
            });
        }

        private void OnEcosystemCleared() {
            if (!Ref.Replace(ref m_State.Environment, null)) {
                return;
            }

            m_State.Conceptual.Reset();
            m_State.SiteData = null;
            
            m_SimDataCtrl.ClearSite();
            m_ProgressInfo.Reset(null);
            m_Header.SetSelected(ModelPhases.Ecosystem, false);

            Services.Data.SetVariable(ModelingConsts.Var_EcosystemSelected, null);
            Services.Data.SetVariable(ModelingConsts.Var_HasJob, false);
            
            RefreshPhaseHeader();
        }

        private bool EcosystemHighlightPredicate(BestiaryDesc ecosystem) {
            return Save.CurrentJob.Job?.FindAsset<JobModelScope>()?.EnvironmentId == ecosystem.Id();
        }

        private void OnBestiaryUpdated() {
            EvaluateConceptStatus();
            RefreshPhaseHeader();
        }

        private IEnumerator OnRequestConceptualImport() {
            m_InlinePopup.Hide();
            return Routine.Start(this, ImportProcess()).Wait();
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

            m_SimDataCtrl.GeneratePlayerProfile();

            Services.Events.Queue(GameEvents.SiteDataUpdated, m_State.SiteData.EnvOrMapId);
            yield return null;
            EvaluateConceptStatus();
            RefreshPhaseHeader();
        }

        private void OnRequestConceptualExport() {
            if (m_ProgressInfo.Scope != null && !m_ProgressInfo.Scope.ConceptualModelId.IsEmpty && Save.Bestiary.RegisterFact(m_ProgressInfo.Scope.ConceptualModelId)) {
                BFBase fact = Assets.Fact(m_ProgressInfo.Scope.ConceptualModelId);
                Services.UI.Popup.PresentFact(Loc.Find("modeling.newConceptualModel.header"), null, null, fact, BFType.DefaultDiscoveredFlags(fact)).OnComplete((_) => {
                    Services.Script.TriggerResponse(ModelingConsts.Trigger_ConceptExported);
                    Services.Events.Dispatch(ModelingConsts.Event_Concept_Exported);
                });
                m_InlinePopup.Hide();
                EvaluateConceptStatus();
                RefreshPhaseHeader();
                Services.Audio.PostEvent("modelSynced");
            }
        }

        private void OnSyncFailed() {
            Services.UI.Popup.Display(
                Loc.Find("modeling.noSyncPopup.header"), Loc.Find("modeling.noSyncPopup.description")
            ).OnComplete((_) => {
                Services.Script.TriggerResponse(ModelingConsts.Trigger_SyncError);
                Services.Events.Dispatch(ModelingConsts.Event_Sync_Error, m_State.LastKnownAccuracy);
            });
            Services.Audio.PostEvent("syncDenied");
        }

        private void OnSyncAchieved() {
            if (m_ProgressInfo.Scope != null && !m_ProgressInfo.Scope.SyncModelId.IsEmpty && Save.Bestiary.RegisterFact(m_ProgressInfo.Scope.SyncModelId)) {
                BFBase fact = Assets.Fact(m_ProgressInfo.Scope.SyncModelId);
                Services.UI.Popup.PresentFact(Loc.Find("modeling.newSyncModel.header"), null, null, fact, BFType.DefaultDiscoveredFlags(fact)).OnComplete((_) => {
                    Services.Script.TriggerResponse(ModelingConsts.Trigger_SyncCompleted);
                    Services.Events.Dispatch(ModelingConsts.Event_Simulation_Complete, m_State.LastKnownAccuracy);
                });
                RefreshPhaseHeader();
                Services.Audio.PostEvent("modelSynced");
            }
        }

        private void OnPredictCompleted() {
            if (m_ProgressInfo.Scope != null && !m_ProgressInfo.Scope.PredictModelId.IsEmpty && Save.Bestiary.RegisterFact(m_ProgressInfo.Scope.PredictModelId)) {
                BFBase fact = Assets.Fact(m_ProgressInfo.Scope.PredictModelId);
                Services.UI.Popup.PresentFact(Loc.Find("modeling.newPredictModel.header"), null, null, fact, BFType.DefaultDiscoveredFlags(fact)).OnComplete((_) => {
                    Services.Script.TriggerResponse(ModelingConsts.Trigger_PredictCompleted);
                    Services.Events.Dispatch(ModelingConsts.Event_Predict_Complete);
                });
                RefreshPhaseHeader();
                Services.Audio.PostEvent("modelSynced");
            }
        }

        private void OnInterventionUpdated() {
            m_World.ReconstructForIntervention();
        }

        private void OnInterventionUnsuccessful() {
            Services.Events.Dispatch(ModelingConsts.Event_Intervene_Error);
            Services.Script.TriggerResponse(ModelingConsts.Trigger_InterveneError);
        }

        private void OnAnimationStart() {
            if (m_State.Phase == ModelPhases.Intervene) {
                m_World.DisableIntervention();
            }
        }

        private void OnInterventionReset() {
            m_World.EnableIntervention();
        }

        private void OnInterventionCompleted() {
            if (m_ProgressInfo.Scope != null && !m_ProgressInfo.Scope.InterveneModelId.IsEmpty && Save.Bestiary.RegisterFact(m_ProgressInfo.Scope.InterveneModelId)) {
                BFBase fact = Assets.Fact(m_ProgressInfo.Scope.InterveneModelId);
                Services.UI.Popup.PresentFact(Loc.Find("modeling.newInterveneModel.header"), null, null, fact, BFType.DefaultDiscoveredFlags(fact)).OnComplete((_) => {
                    Services.Script.TriggerResponse(ModelingConsts.Trigger_InterveneCompleted);
                    Services.Events.Dispatch(ModelingConsts.Event_Intervene_Complete);
                });;
                RefreshPhaseHeader();
                Services.Audio.PostEvent("predictionSynced");
            }
        }

        #endregion // Callbacks

        #region Evaluation

        private void RefreshPhaseHeader() {
            ModelPhases saveMask = GetUnlockedPhases();
            EvaluatePhaseMask(saveMask);
            EvaluateHighlightMask(saveMask);
        }

        private void EvaluateConceptStatus() {
            ConceptualModelState.StatusId status = ConceptualModelState.StatusId.UpToDate;
            FactUtil.GatherPendingEntities(m_ProgressInfo.ImportableEntities, Save.Bestiary, m_State.Conceptual.GraphedEntities, m_State.Conceptual.PendingEntities);
            FactUtil.GatherPendingFacts(m_ProgressInfo.ImportableFacts, Save.Bestiary, m_State.Conceptual.GraphedEntities, m_State.Conceptual.PendingEntities, m_State.Conceptual.GraphedFacts, m_State.Conceptual.PendingFacts);

            bool missingEntities = !HasRequiredEntities();
            bool missingBehaviors = !HasRequiredBehaviors();
            ModelMissingReasons missingReason = 0;

            if (m_State.Conceptual.PendingEntities.Count > 0 || m_State.Conceptual.PendingFacts.Count > 0) {
                status = ConceptualModelState.StatusId.PendingImport;
            } else if (missingEntities || missingBehaviors) {
                status = ConceptualModelState.StatusId.MissingData;
                if (missingEntities) {
                    missingReason |= ModelMissingReasons.Organisms;
                }
                if (missingBehaviors) {
                    missingReason |= ModelMissingReasons.Behaviors;
                }
            } else if (m_ProgressInfo.Scope != null && !m_ProgressInfo.Scope.ConceptualModelId.IsEmpty && !Save.Bestiary.HasFact(m_ProgressInfo.Scope.ConceptualModelId)) {
                status = ConceptualModelState.StatusId.ExportReady;
            } else {
                status = ConceptualModelState.StatusId.UpToDate;
            }

            m_State.Conceptual.Status = status;
            m_State.Conceptual.MissingReasons = missingReason;

            Services.Data.SetVariable(ModelingConsts.Var_HasMissingFacts, status == ConceptualModelState.StatusId.MissingData);
            Services.Data.SetVariable(ModelingConsts.Var_HasPendingFacts, status == ConceptualModelState.StatusId.PendingImport);
            Services.Data.SetVariable(ModelingConsts.Var_HasPendingExport, status == ConceptualModelState.StatusId.ExportReady);
        }

        private void EvaluatePhaseMask(ModelPhases saveMask) {
            ModelPhases mask = m_ProgressInfo.Phases;
            ModelPhases completed = m_ProgressInfo.Phases;
            if (m_ProgressInfo.Scope != null) {
                if (ConceptualStatusWillBlockProgression(m_State.Conceptual.Status) || !HasRequiredModel(m_ProgressInfo.Scope.ConceptualModelId)) {
                    mask &= ~ComputationalPhaseMask;
                    completed &= ~(ComputationalPhaseMask | ModelPhases.Concept);
                } else if (!HasRequiredModel(m_ProgressInfo.Scope.SyncModelId)) {
                    mask &= ~(ModelPhases.Predict | ModelPhases.Intervene);
                    completed &= ~ComputationalPhaseMask;
                } else if (!HasRequiredModel(m_ProgressInfo.Scope.PredictModelId)) {
                    mask &= ~ModelPhases.Intervene;
                    completed &= ~(ModelPhases.Predict | ModelPhases.Intervene);
                } else if (!HasRequiredModel(m_ProgressInfo.Scope.InterveneModelId)) {
                    completed &= ~ModelPhases.Intervene;
                }
            } else {
                completed &= ~ComputationalPhaseMask;
                if (ConceptualStatusWillBlockProgression(m_State.Conceptual.Status)) {
                    mask &= ~ComputationalPhaseMask;
                    completed &= ~ModelPhases.Concept;
                }
            }

            mask &= saveMask;
            completed &= saveMask;

            m_State.AllowedPhases = mask;
            m_State.CompletedPhases = completed;
            m_Header.UpdateAllowedMask(mask);
        }

        static private ModelPhases GetUnlockedPhases()
        {
            ModelPhases mask = ModelPhases.Ecosystem;

            if (Save.Inventory.HasUpgrade(ItemIds.VisualModel))
                mask |= ModelPhases.Concept;
            if (Save.Inventory.HasUpgrade(ItemIds.SyncModel))
                mask |= ModelPhases.Sync;
            if (Save.Inventory.HasUpgrade(ItemIds.PredictionModel))
                mask |= ModelPhases.Predict;
            if (Save.Inventory.HasUpgrade(ItemIds.InterveneModel))
                mask |= ModelPhases.Intervene;

            return mask;
        }

        private void EvaluateHighlightMask(ModelPhases saveMask) {
            ModelPhases mask = 0;
            if (m_State.Conceptual.Status == ConceptualModelState.StatusId.ExportReady || m_State.Conceptual.Status == ConceptualModelState.StatusId.PendingImport) {
                mask |= ModelPhases.Concept;
            } else if (m_ProgressInfo.Scope != null && m_State.Conceptual.Status != ConceptualModelState.StatusId.MissingData) {
                if (!HasRequiredModel(m_ProgressInfo.Scope.SyncModelId)) {
                    mask |= ModelPhases.Sync;
                } else if (!HasRequiredModel(m_ProgressInfo.Scope.PredictModelId)) {
                    mask |= ModelPhases.Predict;
                } else if (!HasRequiredModel(m_ProgressInfo.Scope.InterveneModelId)) {
                    mask |= ModelPhases.Intervene;
                }
            }

            mask &= saveMask;

            m_Header.UpdateHighlightMask(mask);
        }

        static private bool HasRequiredModel(StringHash32 modelId) {
            return modelId.IsEmpty || Save.Bestiary.HasFact(modelId);
        }

        private bool HasRequiredEntities() {
            foreach(var requirement in m_ProgressInfo.RequiredEntities) {
                if (!m_State.Conceptual.GraphedEntities.Contains(requirement)) {
                    return false;
                }
            }

            return true;
        }

        private bool HasRequiredBehaviors() {
            foreach(var requirement in m_ProgressInfo.RequiredFacts) {
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

            Services.Camera.SnapToPose(m_ConceptualCamera);
        }

        #endregion // ISceneLoadHandler

        #region Leaf

        [LeafMember("ModelingSetPhase"), Preserve]
        static private void LeafSetPhase(ModelPhases phase) {
            Assert.NotNull(s_Instance, "Cannot call modeling leaf methods when outside of modeling room");
            s_Instance.m_Header.SetSelected(phase, true);
        }

        // [LeafMember("ModelingCanRunPhase")]
        // static private bool LeafCheckCanRunPhase(ModelPhases phase) {
        //     JobModelScope scope = Save.CurrentJob.Job?.FindAsset<JobModelScope>();
        // }

        #endregion // Leaf
    }
    
    public enum ModelPhases : byte {
        Ecosystem = 0x01,
        Concept = 0x02,
        Sync = 0x04,
        Predict = 0x08,
        Intervene = 0x10
    }

    public enum ModelMissingReasons : byte {
        Organisms = 0x01,
        Behaviors = 0x02,
        HistoricalPopulations = 0x04,
        HistoricalWaterChem = 0x08,
    }

    public struct MissingFactRecord {
        public StringHash32 OrganismId;
        public WaterPropertyId PropertyId;
        public MissingFactTypes FactTypes;
    }

    public enum MissingFactTypes : ushort {
        Repro = 0x001,
        Repro_Stressed = 0x002,
        Eat = 0x004,
        Eat_Stressed = 0x008,
        WaterChem = 0x010,
        WaterChem_Stressed = 0x020,
        Parasite = 0x040,
        PopulationHistory = 0x080,
        WaterChemHistory = 0x100,
        StressRange = 0x200
    }
}