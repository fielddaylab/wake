#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Aqua.Option;
using AquaAudio;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using Leaf;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Aqua {
    [ServiceDependency(typeof(DataService), typeof(EventService), typeof(AudioMgr))]
    [DefaultExecutionOrder(-1000000000)]
    internal partial class PerformanceWatcher : ServiceBehaviour, IDebuggable {
        public float LowResHeight = 660f;

        private PerformanceTracker m_PerfTracker;

        protected override void Initialize() {
            base.Initialize();

            m_PerfTracker = new PerformanceTracker(256);
        }

        protected override void Shutdown() {
            base.Shutdown();
        }

        private void LateUpdate() {
            if (Application.targetFrameRate != -1) {
                Application.targetFrameRate = -1;
            }

            Perf.DesiredWorldRenderScale = GetWorldRenderScale(Save.Options.Performance.Resolution, LowResHeight);
        }

        #if DEVELOPMENT
        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus() {
            DMInfo menu = new DMInfo("Performance");

            menu.AddButton("Log System Info", () => Perf.LogSystemInfo());
            menu.AddDivider();
            menu.AddButton("Reset LowRes Resolution", () => {
                LowResHeight = 660;
            });
            menu.AddButton("Reduce LowRes Resolution", () => {
                LowResHeight = LowResHeight * 0.75f;
            }, () => LowResHeight >= 300);
            yield return menu;
        }
        #endif // DEVELOPMENT

        static private float GetWorldRenderScale(OptionsPerformance.QualityMode inResolutionMode, float inLowResCameraHeight) {
            float scale = Mathf.Min(1, inLowResCameraHeight / Screen.height);
            switch (inResolutionMode) {
                case OptionsPerformance.QualityMode.Medium: {
                        scale = (1 + scale) * 0.5f;
                        break;
                    }
                case OptionsPerformance.QualityMode.High: {
                        scale = 1;
                        break;
                    }
            }

            return scale;
        }
    }

    static public class Perf {
        static private float s_DesiredWorldRenderScale = 1;

        static public float DesiredWorldRenderScale {
            get { return s_DesiredWorldRenderScale; }
            internal set {
                if (value != s_DesiredWorldRenderScale) {
                    Log.Msg("[Perf] World render scale updated {0} -> {1}", s_DesiredWorldRenderScale, value);
                    s_DesiredWorldRenderScale = value;
                }
            }
        }

        static public OptionsPerformance GenerateDefaultPerformanceSettings() {
            OptionsPerformance perf = new OptionsPerformance() {
                Resolution = OptionsPerformance.QualityMode.High
            };

            int graphicsMem = SystemInfo.graphicsMemorySize;
            int shaderLevel = SystemInfo.graphicsShaderLevel;
            if (graphicsMem <= 96 || shaderLevel < 20) {
                perf.Resolution = OptionsPerformance.QualityMode.Low;
            } else if (graphicsMem <= 256 || shaderLevel < 35) {
                perf.Resolution = OptionsPerformance.QualityMode.Medium;
            }
            return perf;
        }

        static public void LogSystemInfo() {
            StringBuilder sb = new StringBuilder();
            // StringBuilder generated = new StringBuilder();
            // foreach (var prop in typeof(SystemInfo).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
            //     if (prop.IsDefined(typeof(ObsoleteAttribute), true)) {
            //         continue;
            //     }

            //     sb.Append("...").Append(prop.Name).Append(": ").Append(prop.GetValue(null)).Append('\n');
            //     generated.Append("AppendProperty(\"").Append(prop.Name).Append("\", SystemInfo.").Append(prop.Name).Append(");\n");
            // 
            {
                AppendProperty("batteryLevel", SystemInfo.batteryLevel);
                AppendProperty("batteryStatus", SystemInfo.batteryStatus);
                AppendProperty("operatingSystem", SystemInfo.operatingSystem);
                AppendProperty("operatingSystemFamily", SystemInfo.operatingSystemFamily);
                AppendProperty("processorType", SystemInfo.processorType);
                AppendProperty("processorFrequency", SystemInfo.processorFrequency);
                AppendProperty("processorCount", SystemInfo.processorCount);
                AppendProperty("systemMemorySize", SystemInfo.systemMemorySize);
                AppendProperty("deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier);
                AppendProperty("deviceName", SystemInfo.deviceName);
                AppendProperty("deviceModel", SystemInfo.deviceModel);
                AppendProperty("supportsAccelerometer", SystemInfo.supportsAccelerometer);
                AppendProperty("supportsGyroscope", SystemInfo.supportsGyroscope);
                AppendProperty("supportsLocationService", SystemInfo.supportsLocationService);
                AppendProperty("supportsVibration", SystemInfo.supportsVibration);
                AppendProperty("supportsAudio", SystemInfo.supportsAudio);
                AppendProperty("deviceType", SystemInfo.deviceType);
                AppendProperty("graphicsMemorySize", SystemInfo.graphicsMemorySize);
                AppendProperty("graphicsDeviceName", SystemInfo.graphicsDeviceName);
                AppendProperty("graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
                AppendProperty("graphicsDeviceID", SystemInfo.graphicsDeviceID);
                AppendProperty("graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID);
                AppendProperty("graphicsDeviceType", SystemInfo.graphicsDeviceType);
                AppendProperty("graphicsUVStartsAtTop", SystemInfo.graphicsUVStartsAtTop);
                AppendProperty("graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);
                AppendProperty("graphicsShaderLevel", SystemInfo.graphicsShaderLevel);
                AppendProperty("graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded);
                AppendProperty("renderingThreadingMode", SystemInfo.renderingThreadingMode);
                AppendProperty("hasHiddenSurfaceRemovalOnGPU", SystemInfo.hasHiddenSurfaceRemovalOnGPU);
                AppendProperty("hasDynamicUniformArrayIndexingInFragmentShaders", SystemInfo.hasDynamicUniformArrayIndexingInFragmentShaders);
                AppendProperty("supportsShadows", SystemInfo.supportsShadows);
                AppendProperty("supportsRawShadowDepthSampling", SystemInfo.supportsRawShadowDepthSampling);
                AppendProperty("supportsMotionVectors", SystemInfo.supportsMotionVectors);
                AppendProperty("supports3DTextures", SystemInfo.supports3DTextures);
                AppendProperty("supports2DArrayTextures", SystemInfo.supports2DArrayTextures);
                AppendProperty("supports3DRenderTextures", SystemInfo.supports3DRenderTextures);
                AppendProperty("supportsCubemapArrayTextures", SystemInfo.supportsCubemapArrayTextures);
                AppendProperty("copyTextureSupport", SystemInfo.copyTextureSupport);
                AppendProperty("supportsComputeShaders", SystemInfo.supportsComputeShaders);
                AppendProperty("supportsGeometryShaders", SystemInfo.supportsGeometryShaders);
                AppendProperty("supportsTessellationShaders", SystemInfo.supportsTessellationShaders);
                AppendProperty("supportsInstancing", SystemInfo.supportsInstancing);
                AppendProperty("supportsHardwareQuadTopology", SystemInfo.supportsHardwareQuadTopology);
                AppendProperty("supports32bitsIndexBuffer", SystemInfo.supports32bitsIndexBuffer);
                AppendProperty("supportsSparseTextures", SystemInfo.supportsSparseTextures);
                AppendProperty("supportedRenderTargetCount", SystemInfo.supportedRenderTargetCount);
                AppendProperty("supportsSeparatedRenderTargetsBlend", SystemInfo.supportsSeparatedRenderTargetsBlend);
                AppendProperty("supportedRandomWriteTargetCount", SystemInfo.supportedRandomWriteTargetCount);
                AppendProperty("supportsMultisampledTextures", SystemInfo.supportsMultisampledTextures);
                AppendProperty("supportsMultisampleAutoResolve", SystemInfo.supportsMultisampleAutoResolve);
                AppendProperty("supportsTextureWrapMirrorOnce", SystemInfo.supportsTextureWrapMirrorOnce);
                AppendProperty("usesReversedZBuffer", SystemInfo.usesReversedZBuffer);
                AppendProperty("npotSupport", SystemInfo.npotSupport);
                AppendProperty("maxTextureSize", SystemInfo.maxTextureSize);
                AppendProperty("maxCubemapSize", SystemInfo.maxCubemapSize);
                AppendProperty("maxComputeBufferInputsVertex", SystemInfo.maxComputeBufferInputsVertex);
                AppendProperty("maxComputeBufferInputsFragment", SystemInfo.maxComputeBufferInputsFragment);
                AppendProperty("maxComputeBufferInputsGeometry", SystemInfo.maxComputeBufferInputsGeometry);
                AppendProperty("maxComputeBufferInputsDomain", SystemInfo.maxComputeBufferInputsDomain);
                AppendProperty("maxComputeBufferInputsHull", SystemInfo.maxComputeBufferInputsHull);
                AppendProperty("maxComputeBufferInputsCompute", SystemInfo.maxComputeBufferInputsCompute);
                AppendProperty("maxComputeWorkGroupSize", SystemInfo.maxComputeWorkGroupSize);
                AppendProperty("maxComputeWorkGroupSizeX", SystemInfo.maxComputeWorkGroupSizeX);
                AppendProperty("maxComputeWorkGroupSizeY", SystemInfo.maxComputeWorkGroupSizeY);
                AppendProperty("maxComputeWorkGroupSizeZ", SystemInfo.maxComputeWorkGroupSizeZ);
                AppendProperty("supportsAsyncCompute", SystemInfo.supportsAsyncCompute);
                AppendProperty("supportsGraphicsFence", SystemInfo.supportsGraphicsFence);
                AppendProperty("supportsAsyncGPUReadback", SystemInfo.supportsAsyncGPUReadback);
                AppendProperty("supportsRayTracing", SystemInfo.supportsRayTracing);
                AppendProperty("supportsSetConstantBuffer", SystemInfo.supportsSetConstantBuffer);
                AppendProperty("minConstantBufferOffsetAlignment", SystemInfo.minConstantBufferOffsetAlignment);
                AppendProperty("hasMipMaxLevel", SystemInfo.hasMipMaxLevel);
                AppendProperty("supportsMipStreaming", SystemInfo.supportsMipStreaming);
                AppendProperty("usesLoadStoreActions", SystemInfo.usesLoadStoreActions);
                AppendProperty("supportsStoreAndResolveAction", SystemInfo.supportsStoreAndResolveAction);
            }
            sb.TrimEnd(StringUtils.DefaultNewLineChars);
            Debug.LogFormat("[Perf] System Info:\n{0}", sb.ToString());
            // Log.Msg("Copypaste:\n{0}", generated.ToString());

            void AppendProperty(string name, object value) {
                sb.Append("...").Append(name).Append(": ").Append(value).Append('\n');
            }
        }
    }
}