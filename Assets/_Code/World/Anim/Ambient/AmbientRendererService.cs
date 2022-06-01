using System;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using Random = System.Random;

namespace Aqua.Animation
{
    public class AmbientRendererService : ServiceBehaviour
    {
        #region Consts

        private const byte ChangeFlag_Color = 1;

        #endregion // Consts

        private readonly RingBuffer<AmbientRenderer> m_Renderers = new RingBuffer<AmbientRenderer>(64, RingBufferMode.Expand);
        private readonly Random m_Random = new Random(Environment.TickCount ^ 52507827);

        private float m_LastTimestamp;

        #region Register/Deregister

        public void Register(AmbientRenderer inRenderer)
        {
            Assert.False(m_Renderers.Contains(inRenderer), "AmbientRenderer was registered twice");
            m_Renderers.PushBack(inRenderer);
            SetupRenderer(inRenderer);
        }

        public void Deregister(AmbientRenderer inRenderer)
        {
            Assert.True(m_Renderers.Contains(inRenderer), "AmbientRenderer was deregistered multiple times, or never registered");
            m_Renderers.FastRemove(inRenderer);
            ResetRenderer(inRenderer);
        }

        private void SetupRenderer(AmbientRenderer inRenderer)
        {
            AmbientUtils.InitColorWave(ref inRenderer.ColorState, ref inRenderer.ColorAnimation, inRenderer.Group.Color, m_LastTimestamp, m_Random);
        }

        private void ResetRenderer(AmbientRenderer inRenderer)
        {
            inRenderer.Group.Color = inRenderer.ColorAnimation.Initial;
        }

        #endregion // Register/Deregister

        private void ResetTimestamp()
        {
            m_LastTimestamp = 0;
        }

        private void LateUpdate()
        {
            m_LastTimestamp += Time.deltaTime;

            if (m_Renderers.Count > 0)
            {
                Process(m_LastTimestamp, m_Renderers, m_Random);
            }
        }

        #region Internal

        static private unsafe void Process(float inTimestamp, ListSlice<AmbientRenderer> inRenderers, Random inRandom)
        {
            // TODO: Batch these in groups of 32 or something
            // otherwise we risk using too much of the stack

            int objectCount = inRenderers.Length;
            int objectIdx;

            // buffers
            Color* colors = Frame.AllocArray<Color>(objectCount);

            AmbientColorState* stateBuffer = Frame.AllocArray<AmbientColorState>(objectCount);
            AmbientColorPropertyConfig* configBuffer = Frame.AllocArray<AmbientColorPropertyConfig>(objectCount);
            byte* changeBuffer = Frame.AllocArray<byte>(objectCount);

            for(int i = 0; i < objectCount; ++i)
            {
                changeBuffer[i] = 0;
            }
            
            // process position

            LoadColorState(colors, stateBuffer, configBuffer, inRenderers);
            AmbientUtils.ProcessColorValues(colors, stateBuffer, configBuffer, changeBuffer, objectCount, inTimestamp, ChangeFlag_Color);
            AmbientUtils.ProcessColorWaves(stateBuffer, configBuffer, objectCount, inTimestamp, inRandom);
            StoreColorState(stateBuffer, inRenderers);

            // store results back in objects

            objectIdx = 0;
            foreach(var obj in inRenderers)
            {
                byte changeFlags = changeBuffer[objectIdx];
                if (changeFlags != 0)
                {
                    Color targetColor = colors[objectIdx];
                    ColorGroup targetGroup = obj.Group;
                    targetGroup.SetColor(obj.Channel, targetColor);
                }
                ++objectIdx;
            }
        }

        #region Load

        static private unsafe void LoadColorState(Color* ioColors, AmbientColorState* ioStateBuffer, AmbientColorPropertyConfig* ioPropertyBuffer, ListSlice<AmbientRenderer> inRenderers)
        {
            int objectIndex = 0;
            foreach(var obj in inRenderers)
            {
                ioStateBuffer[objectIndex] = obj.ColorState;
                ioPropertyBuffer[objectIndex] = obj.ColorAnimation;
                ioColors[objectIndex] = obj.ColorAnimation.Initial;
                ++objectIndex;
            }
        }

        #endregion // Load

        #region Store

        static private unsafe void StoreColorState(AmbientColorState* inStateBuffer, ListSlice<AmbientRenderer> inRenderers)
        {
            int objectIdx = 0;
            foreach(var obj in inRenderers)
            {
                obj.ColorState = inStateBuffer[objectIdx];
                objectIdx++;
            }
        }

        #endregion // Store

        #endregion // Internal

        #region Service

        private void OnSceneLoaded(SceneBinding inBinding, object inContext)
        {
            m_LastTimestamp = 0;
        }

        protected override void Initialize()
        {
            base.Initialize();

            SceneHelper.OnSceneUnload += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            SceneHelper.OnSceneLoaded -= OnSceneLoaded;
        }

        #endregion // Service
    }
}