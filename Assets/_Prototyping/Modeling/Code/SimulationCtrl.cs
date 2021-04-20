using System;
using Aqua;
using Aqua.Debugging;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class SimulationCtrl : MonoBehaviour, ISceneLoadHandler, IInputHandler
    {
        #region Inspector

        [SerializeField, Required] private ModelingUI m_ModelingUI = null;
        [SerializeField, Required] private SimulationUI m_SimulationUI = null;
        [SerializeField, Required] private BaseInputLayer m_Input = null;

        #pragma warning disable CS0414

        [Header("-- DEBUG -- ")]
        [SerializeField] private ModelingScenarioData m_TestScenario = null;

        #pragma warning restore CS0414
        
        #endregion // Inspector

        [NonSerialized] private SimulationBuffer m_Buffer;
        [NonSerialized] private ModelingState m_State;

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            ModelingScenarioData scenario = Services.Data.CurrentJob()?.Job.FindAsset<ModelingScenarioData>();
            
            #if UNITY_EDITOR
            if (!scenario && BootParams.BootedFromCurrentScene)
                scenario = m_TestScenario;
            #endif // UNITY_DEITOR

            m_ModelingUI.SetScenario(scenario, scenario && BootParams.BootedFromCurrentScene);

            m_Buffer = new SimulationBuffer();

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DebugService.IsLogging(LogMask.Modeling))
                m_Buffer.Flags = SimulatorFlags.Debug;
            #endif // UNITY_EDITOR || DEVELOPMENT_BUILD

            m_Buffer.SetScenario(scenario);
            m_ModelingUI.Populate(Services.Data.Profile.Bestiary);
            // m_ModelingUI.OnSimulateClick = OnAdvanceClicked;

            Services.Data.SetVariable(SimulationConsts.Var_HasScenario, m_Buffer.Scenario() != null);
            SyncPhaseScriptVar();

            m_Buffer.OnUpdate = OnBufferUpdated;
            OnBufferUpdated();

            m_Input.Device.RegisterHandler(this);
        }

        private void OnBufferUpdated()
        {
            var updateFlags = m_Buffer.Refresh();

            if (updateFlags == 0)
                return;

            float error = m_Buffer.CalculateModelError();
            int sync = 100 - Mathf.CeilToInt(error * 100);
            
            if (m_State.Phase == ModelingPhase.Model && m_State.ModelSync != 100 && sync == 100)
            {
                Services.Audio.PostEvent("modelSync");
            }

            m_State.ModelSync = sync;
            
            error = m_Buffer.CalculatePredictionError();
            sync = 100 - Mathf.CeilToInt(error * 100);
            
            if (m_State.Phase == ModelingPhase.Predict && m_State.PredictSync != 100 && sync == 100)
            {
                Services.Audio.PostEvent("modelSync");
            }

            m_State.PredictSync = sync;

            Services.Data.SetVariable(SimulationConsts.Var_ModelSync, m_State.ModelSync);
            Services.Data.SetVariable(SimulationConsts.Var_PredictSync, m_State.PredictSync);
            
            // m_ModelingUI.Refresh(m_State, updateFlags);
        }

        private void OnAdvanceClicked()
        {
            switch(m_State.Phase)
            {
                case ModelingPhase.Model:
                    {
                        if (m_State.ModelSync < 100)
                        {
                            Services.Audio.PostEvent("syncDenied");
                            break;
                        }

                        AdvanceToPredict();
                        break;
                    }

                case ModelingPhase.Predict:
                    {
                        if (m_State.PredictSync < 100)
                        {
                            Services.Audio.PostEvent("syncDenied");
                            break;
                        }

                        CompleteActivity();
                        break;
                    }
            }
        }

        private void AdvanceToPredict()
        {
            // if no prediction is necessary, just complete it
            if (m_Buffer.Scenario().PredictionTicks() <= 0)
            {
                CompleteActivity();
                return;
            }

            m_State.Phase = ModelingPhase.Predict;
            SyncPhaseScriptVar();
            // m_ModelingUI.SwitchToPredict();

            Services.Audio.PostEvent("modelSynced");
            Services.Script.TriggerResponse(SimulationConsts.Trigger_Synced);
        }

        private void CompleteActivity()
        {
            m_State.Phase = ModelingPhase.Completed;
            SyncPhaseScriptVar();

            StringHash32 fact = m_Buffer.Scenario().BestiaryModelId();
            if (!fact.IsEmpty)
                Services.Data.Profile.Bestiary.RegisterFact(fact);
            // m_ModelingUI.Complete();

            Services.Audio.PostEvent("predictionSynced");
            Services.Script.TriggerResponse(SimulationConsts.Trigger_Completed);

            m_Input.Device.DeregisterHandler(this);
        }

        private void SyncPhaseScriptVar()
        {
            switch(m_State.Phase)
            {
                case ModelingPhase.Model:
                    Services.Data.SetVariable(SimulationConsts.Var_ModelPhase, SimulationConsts.ModelPhase_Model);
                    break;

                case ModelingPhase.Predict:
                    Services.Data.SetVariable(SimulationConsts.Var_ModelPhase, SimulationConsts.ModelPhase_Predict);
                    break;

                case ModelingPhase.Completed:
                    Services.Data.SetVariable(SimulationConsts.Var_ModelPhase, SimulationConsts.ModelPhase_Completed);
                    break;
            }
        }

        void IInputHandler.HandleInput(DeviceInput inInput)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            
            if (inInput.KeyPressed(KeyCode.F8) || inInput.KeyPressed(KeyCode.R))
            {
                m_Buffer.Flags |= SimulatorFlags.Debug;
                m_Buffer.ReloadScenario();
            }

            #endif // UNITY_EDITOR || DEVELOPMENT_BUILD
        }
    }
}