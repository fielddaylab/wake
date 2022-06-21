/*
 * Copyright (C) 2022. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    22 May 2022
 * 
 * File:    SystemInfoExt.cs
 * Purpose: Class for exporting SystemInfo properties to a string.
 */

using System.IO;
using System.Text;

namespace UnityEngine {
    /// <summary>
    /// API for writing all SystemInfo properties to a string or stream.
    /// </summary>
    static public class SystemInfoExt {
        static public void Report(StringBuilder writer, bool includeSecureInfo = false) {
            // os
            Property(writer, "runtimePlatform", Application.platform);
            Property(writer, "operatingSystem", SystemInfo.operatingSystem);
            Property(writer, "operatingSystemFamily", SystemInfo.operatingSystemFamily);

            // device
            Property(writer, "deviceModel", SystemInfo.deviceModel);
            Property(writer, "deviceType", SystemInfo.deviceType);
            if (includeSecureInfo) {
                Property(writer, "deviceName", SystemInfo.deviceName);
                Property(writer, "deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier);
            }

            // processor
            Property(writer, "processorCount", SystemInfo.processorCount);
            Property(writer, "processorFrequency", SystemInfo.processorFrequency);
            Property(writer, "processorType", SystemInfo.processorType);

            // battery
            Property(writer, "batteryLevel", SystemInfo.batteryLevel);
            Property(writer, "batteryStatus", SystemInfo.batteryStatus);

            // memory
            Property(writer, "systemMemorySize", SystemInfo.systemMemorySize);
            Property(writer, "graphicsMemorySize", SystemInfo.graphicsMemorySize);

            // features
            Property(writer, "supportsAudio", SystemInfo.supportsAudio);
            Property(writer, "supportsAccelerometer", SystemInfo.supportsAccelerometer);
            Property(writer, "supportsGyroscope", SystemInfo.supportsGyroscope);
            Property(writer, "supportsLocationService", SystemInfo.supportsLocationService);
            Property(writer, "supportsVibration", SystemInfo.supportsVibration);

            // graphics
            Property(writer, "graphicsDeviceID", SystemInfo.graphicsDeviceID);
            Property(writer, "graphicsDeviceName", SystemInfo.graphicsDeviceName);
            Property(writer, "graphicsDeviceType", SystemInfo.graphicsDeviceType);
            Property(writer, "graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
            Property(writer, "graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID);
            Property(writer, "graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);
            Property(writer, "graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded);
            Property(writer, "graphicsShaderLevel", SystemInfo.graphicsShaderLevel);
            Property(writer, "graphicsUVStartsAtTop", SystemInfo.graphicsUVStartsAtTop);
            Property(writer, "copyTextureSupport", SystemInfo.copyTextureSupport);
            Property(writer, "maxCubemapSize", SystemInfo.maxCubemapSize);
            Property(writer, "maxTextureSize", SystemInfo.maxTextureSize);
            Property(writer, "npotSupport", SystemInfo.npotSupport);
            Property(writer, "supportedRenderTargetCount", SystemInfo.supportedRenderTargetCount);
            Property(writer, "supports2DArrayTextures", SystemInfo.supports2DArrayTextures);
            Property(writer, "supports3DRenderTextures", SystemInfo.supports3DRenderTextures);
            Property(writer, "supports3DTextures", SystemInfo.supports3DTextures);
            Property(writer, "supportsAsyncCompute", SystemInfo.supportsAsyncCompute);
            Property(writer, "supportsComputeShaders", SystemInfo.supportsComputeShaders);
            Property(writer, "supportsCubemapArrayTextures", SystemInfo.supportsCubemapArrayTextures);
            Property(writer, "supportsInstancing", SystemInfo.supportsInstancing);
            Property(writer, "supportsMotionVectors", SystemInfo.supportsMotionVectors);
            Property(writer, "supportsRawShadowDepthSampling", SystemInfo.supportsRawShadowDepthSampling);
            Property(writer, "supportsShadows", SystemInfo.supportsShadows);
            Property(writer, "supportsSparseTextures", SystemInfo.supportsSparseTextures);
            Property(writer, "usesReversedZBuffer", SystemInfo.usesReversedZBuffer);

            #if UNITY_2017_2_OR_NEWER
            Property(writer, "supportsTextureWrapMirrorOnce", SystemInfo.supportsTextureWrapMirrorOnce);
            #endif

            #if UNITY_2017_3_OR_NEWER
            Property(writer, "supportsMultisampledTextures", SystemInfo.supportsMultisampledTextures);
            #endif

            #if UNITY_2018_1_OR_NEWER
            Property(writer, "supports32bitsIndexBuffer", SystemInfo.supports32bitsIndexBuffer);
            Property(writer, "supportsAsyncGPUReadback", SystemInfo.supportsAsyncGPUReadback);
            Property(writer, "supportsHardwareQuadTopology", SystemInfo.supportsHardwareQuadTopology);
            #endif // UNITY_2018_1_OR_NEWER

            #if UNITY_2018_2_OR_NEWER
            Property(writer, "supportsMipStreaming", SystemInfo.supportsMipStreaming);
            Property(writer, "supportsMultisampleAutoResolve", SystemInfo.supportsMultisampleAutoResolve);
            #endif

            #if UNITY_2018_3_OR_NEWER
            Property(writer, "hasDynamicUniformArrayIndexingInFragmentShaders", SystemInfo.hasDynamicUniformArrayIndexingInFragmentShaders);
            Property(writer, "hasHiddenSurfaceRemovalOnGPU", SystemInfo.hasHiddenSurfaceRemovalOnGPU);
            Property(writer, "supportsSeparatedRenderTargetsBlend", SystemInfo.supportsSeparatedRenderTargetsBlend);
            #endif

            #if UNITY_2019_1_OR_NEWER
            Property(writer, "minConstantBufferOffsetAlignment", SystemInfo.minConstantBufferOffsetAlignment);
            Property(writer, "supportsGraphicsFence", SystemInfo.supportsGraphicsFence);
            Property(writer, "supportsSetConstantBuffer", SystemInfo.supportsSetConstantBuffer);
            #endif

            #if UNITY_2019_2_OR_NEWER
            Property(writer, "hasMipMaxLevel", SystemInfo.hasMipMaxLevel);
            #endif

            #if UNITY_2019_3_OR_NEWER
            Property(writer, "maxComputeBufferInputsCompute", SystemInfo.maxComputeBufferInputsCompute);
            Property(writer, "maxComputeBufferInputsDomain", SystemInfo.maxComputeBufferInputsDomain);
            Property(writer, "maxComputeBufferInputsFragment", SystemInfo.maxComputeBufferInputsFragment);
            Property(writer, "maxComputeBufferInputsGeometry", SystemInfo.maxComputeBufferInputsGeometry);
            Property(writer, "maxComputeBufferInputsHull", SystemInfo.maxComputeBufferInputsHull);
            Property(writer, "maxComputeBufferInputsVertex", SystemInfo.maxComputeBufferInputsVertex);
            Property(writer, "maxComputeWorkGroupSize", SystemInfo.maxComputeWorkGroupSize);
            Property(writer, "maxComputeWorkGroupSizeX", SystemInfo.maxComputeWorkGroupSizeX);
            Property(writer, "maxComputeWorkGroupSizeY", SystemInfo.maxComputeWorkGroupSizeY);
            Property(writer, "maxComputeWorkGroupSizeZ", SystemInfo.maxComputeWorkGroupSizeZ);
            Property(writer, "renderingThreadingMode", SystemInfo.renderingThreadingMode);
            Property(writer, "supportedRandomWriteTargetCount", SystemInfo.supportedRandomWriteTargetCount);
            Property(writer, "supportsGeometryShaders", SystemInfo.supportsGeometryShaders);
            Property(writer, "supportsRayTracing", SystemInfo.supportsRayTracing);
            Property(writer, "supportsTessellationShaders", SystemInfo.supportsTessellationShaders);
            Property(writer, "usesLoadStoreActions", SystemInfo.usesLoadStoreActions);
            #endif

            #if UNITY_2020_3_OR_NEWER
            Property(writer, "supportsStoreAndResolveAction", SystemInfo.supportsStoreAndResolveAction);
            #endif

            // handle trailing newline
            writer.Length--;
        }

        static public string Report(bool includeSecureInfo = false) {
            StringBuilder sb = new StringBuilder(256);
            Report(sb, includeSecureInfo);
            return sb.ToString();
        }

        static public void Report(TextWriter writer, bool includeSecureInfo = false) {
            // os
            Property(writer, "runtimePlatform", Application.platform);
            Property(writer, "operatingSystem", SystemInfo.operatingSystem);
            Property(writer, "operatingSystemFamily", SystemInfo.operatingSystemFamily);

            // device
            Property(writer, "deviceModel", SystemInfo.deviceModel);
            Property(writer, "deviceType", SystemInfo.deviceType);
            if (includeSecureInfo) {
                Property(writer, "deviceName", SystemInfo.deviceName);
                Property(writer, "deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier);
            }

            // processor
            Property(writer, "processorCount", SystemInfo.processorCount);
            Property(writer, "processorFrequency", SystemInfo.processorFrequency);
            Property(writer, "processorType", SystemInfo.processorType);

            // battery
            Property(writer, "batteryLevel", SystemInfo.batteryLevel);
            Property(writer, "batteryStatus", SystemInfo.batteryStatus);

            // memory
            Property(writer, "systemMemorySize", SystemInfo.systemMemorySize);
            Property(writer, "graphicsMemorySize", SystemInfo.graphicsMemorySize);

            // features
            Property(writer, "supportsAudio", SystemInfo.supportsAudio);
            Property(writer, "supportsAccelerometer", SystemInfo.supportsAccelerometer);
            Property(writer, "supportsGyroscope", SystemInfo.supportsGyroscope);
            Property(writer, "supportsLocationService", SystemInfo.supportsLocationService);
            Property(writer, "supportsVibration", SystemInfo.supportsVibration);

            // graphics
            Property(writer, "graphicsDeviceID", SystemInfo.graphicsDeviceID);
            Property(writer, "graphicsDeviceName", SystemInfo.graphicsDeviceName);
            Property(writer, "graphicsDeviceType", SystemInfo.graphicsDeviceType);
            Property(writer, "graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
            Property(writer, "graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID);
            Property(writer, "graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);
            Property(writer, "graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded);
            Property(writer, "graphicsShaderLevel", SystemInfo.graphicsShaderLevel);
            Property(writer, "graphicsUVStartsAtTop", SystemInfo.graphicsUVStartsAtTop);
            Property(writer, "copyTextureSupport", SystemInfo.copyTextureSupport);
            Property(writer, "maxCubemapSize", SystemInfo.maxCubemapSize);
            Property(writer, "maxTextureSize", SystemInfo.maxTextureSize);
            Property(writer, "npotSupport", SystemInfo.npotSupport);
            Property(writer, "supportedRenderTargetCount", SystemInfo.supportedRenderTargetCount);
            Property(writer, "supports2DArrayTextures", SystemInfo.supports2DArrayTextures);
            Property(writer, "supports3DRenderTextures", SystemInfo.supports3DRenderTextures);
            Property(writer, "supports3DTextures", SystemInfo.supports3DTextures);
            Property(writer, "supportsAsyncCompute", SystemInfo.supportsAsyncCompute);
            Property(writer, "supportsComputeShaders", SystemInfo.supportsComputeShaders);
            Property(writer, "supportsCubemapArrayTextures", SystemInfo.supportsCubemapArrayTextures);
            Property(writer, "supportsInstancing", SystemInfo.supportsInstancing);
            Property(writer, "supportsMotionVectors", SystemInfo.supportsMotionVectors);
            Property(writer, "supportsRawShadowDepthSampling", SystemInfo.supportsRawShadowDepthSampling);
            Property(writer, "supportsShadows", SystemInfo.supportsShadows);
            Property(writer, "supportsSparseTextures", SystemInfo.supportsSparseTextures);
            Property(writer, "usesReversedZBuffer", SystemInfo.usesReversedZBuffer);

            #if UNITY_2017_2_OR_NEWER
            Property(writer, "supportsTextureWrapMirrorOnce", SystemInfo.supportsTextureWrapMirrorOnce);
            #endif

            #if UNITY_2017_3_OR_NEWER
            Property(writer, "supportsMultisampledTextures", SystemInfo.supportsMultisampledTextures);
            #endif

            #if UNITY_2018_1_OR_NEWER
            Property(writer, "supports32bitsIndexBuffer", SystemInfo.supports32bitsIndexBuffer);
            Property(writer, "supportsAsyncGPUReadback", SystemInfo.supportsAsyncGPUReadback);
            Property(writer, "supportsHardwareQuadTopology", SystemInfo.supportsHardwareQuadTopology);
            #endif // UNITY_2018_1_OR_NEWER

            #if UNITY_2018_2_OR_NEWER
            Property(writer, "supportsMipStreaming", SystemInfo.supportsMipStreaming);
            Property(writer, "supportsMultisampleAutoResolve", SystemInfo.supportsMultisampleAutoResolve);
            #endif

            #if UNITY_2018_3_OR_NEWER
            Property(writer, "hasDynamicUniformArrayIndexingInFragmentShaders", SystemInfo.hasDynamicUniformArrayIndexingInFragmentShaders);
            Property(writer, "hasHiddenSurfaceRemovalOnGPU", SystemInfo.hasHiddenSurfaceRemovalOnGPU);
            Property(writer, "supportsSeparatedRenderTargetsBlend", SystemInfo.supportsSeparatedRenderTargetsBlend);
            #endif

            #if UNITY_2019_1_OR_NEWER
            Property(writer, "minConstantBufferOffsetAlignment", SystemInfo.minConstantBufferOffsetAlignment);
            Property(writer, "supportsGraphicsFence", SystemInfo.supportsGraphicsFence);
            Property(writer, "supportsSetConstantBuffer", SystemInfo.supportsSetConstantBuffer);
            #endif

            #if UNITY_2019_2_OR_NEWER
            Property(writer, "hasMipMaxLevel", SystemInfo.hasMipMaxLevel);
            #endif

            #if UNITY_2019_3_OR_NEWER
            Property(writer, "maxComputeBufferInputsCompute", SystemInfo.maxComputeBufferInputsCompute);
            Property(writer, "maxComputeBufferInputsDomain", SystemInfo.maxComputeBufferInputsDomain);
            Property(writer, "maxComputeBufferInputsFragment", SystemInfo.maxComputeBufferInputsFragment);
            Property(writer, "maxComputeBufferInputsGeometry", SystemInfo.maxComputeBufferInputsGeometry);
            Property(writer, "maxComputeBufferInputsHull", SystemInfo.maxComputeBufferInputsHull);
            Property(writer, "maxComputeBufferInputsVertex", SystemInfo.maxComputeBufferInputsVertex);
            Property(writer, "maxComputeWorkGroupSize", SystemInfo.maxComputeWorkGroupSize);
            Property(writer, "maxComputeWorkGroupSizeX", SystemInfo.maxComputeWorkGroupSizeX);
            Property(writer, "maxComputeWorkGroupSizeY", SystemInfo.maxComputeWorkGroupSizeY);
            Property(writer, "maxComputeWorkGroupSizeZ", SystemInfo.maxComputeWorkGroupSizeZ);
            Property(writer, "renderingThreadingMode", SystemInfo.renderingThreadingMode);
            Property(writer, "supportedRandomWriteTargetCount", SystemInfo.supportedRandomWriteTargetCount);
            Property(writer, "supportsGeometryShaders", SystemInfo.supportsGeometryShaders);
            Property(writer, "supportsRayTracing", SystemInfo.supportsRayTracing);
            Property(writer, "supportsTessellationShaders", SystemInfo.supportsTessellationShaders);
            Property(writer, "usesLoadStoreActions", SystemInfo.usesLoadStoreActions);
            #endif

            #if UNITY_2020_3_OR_NEWER
            Property(writer, "supportsStoreAndResolveAction", SystemInfo.supportsStoreAndResolveAction);
            #endif
        }

        #region Helpers

        static private void Property(StringBuilder writer, string propertyName, object value) {
            writer.Append(propertyName).Append(": ").Append(value).Append('\n');
        }

        static private void Property(StringBuilder writer, string propertyName, bool value) {
            writer.Append(propertyName).Append(": ").Append(value).Append('\n');
        }

        static private void Property(StringBuilder writer, string propertyName, int value) {
            writer.Append(propertyName).Append(": ").Append(value).Append('\n');
        }

        static private void Property(StringBuilder writer, string propertyName, string value) {
            writer.Append(propertyName).Append(": ").Append(value).Append('\n');
        }

        static private void Property(TextWriter writer, string propertyName, object value) {
            writer.Write(propertyName);
            writer.Write(": ");
            writer.Write(value);
            writer.Write('\n');
        }

        static private void Property(TextWriter writer, string propertyName, bool value) {
            writer.Write(propertyName);
            writer.Write(": ");
            writer.Write(value);
            writer.Write('\n');
        }

        static private void Property(TextWriter writer, string propertyName, int value) {
            writer.Write(propertyName);
            writer.Write(": ");
            writer.Write(value);
            writer.Write('\n');
        }

        static private void Property(TextWriter writer, string propertyName, string value) {
            writer.Write(propertyName);
            writer.Write(": ");
            writer.Write(value);
            writer.Write('\n');
        }

        #endregion // Helpers
    }
}