using System;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public class SimulationTest : MonoBehaviour, ISceneLoadHandler
    {
        [SerializeField] private ModelingScenarioData m_TestScenario = null;
        [SerializeField] private ModelingUI m_UI;
        [NonSerialized] private SimulationBuffer m_Buffer;

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            #if UNITY_EDITOR

            void LogThing<T>()
            {
                Debug.LogFormat("sizeof({0})={1}", typeof(T).Name, System.Runtime.InteropServices.Marshal.SizeOf<T>());
            }

            LogThing<WaterPropertyBlockF32>();
            LogThing<CritterData>();
            LogThing<CritterResult>();
            LogThing<SimulationResult>();

            #endif // UNITY_EDITOR

            m_Buffer = new SimulationBuffer();
            m_Buffer.SetScenario(m_TestScenario);
            m_UI.SetBuffer(m_Buffer);
            // m_Buffer.Flags = SimulatorFlags.Debug;

            m_Buffer.OnUpdate = OnBufferUpdated;
            OnBufferUpdated();
        }

        private void OnBufferUpdated()
        {
            m_UI.Refresh();
        }
    }
}