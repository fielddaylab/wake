#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace EasyAssetStreaming {

    /// <summary>
    /// Helper and utility methods for streaming.
    /// </summary>
    static internal class StreamingHelper {
        #region Resources

        static internal void DestroyResource<T>(ref T resource) where T : UnityEngine.Object {
            if (resource != null) {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    UnityEngine.Object.DestroyImmediate(resource);
                } else {
                    UnityEngine.Object.Destroy(resource);
                }
                #else
                UnityEngine.Object.Destroy(resource);
                #endif // UNITY_EDITOR

                resource = null;
            }
        }

        static internal void DestroyResource(UnityEngine.Object resource) {
            if (resource != null) {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    UnityEngine.Object.DestroyImmediate(resource);
                } else {
                    UnityEngine.Object.Destroy(resource);
                }
                #else
                UnityEngine.Object.Destroy(resource);
                #endif // UNITY_EDITOR
            }
        }

        static internal long CalculateMemoryUsage(UnityEngine.Object resource) {
            Texture2D tex = resource as Texture2D;
            if (tex != null) {
                return CalculateTextureMemoryUsage(tex);
            }

            AudioClip clip = resource as AudioClip;
            if (clip != null) {
                return CalculateAudioMemoryUsage(clip);
            }

            return 0;
        }

        /// copied over from BeauUtil to reduce dependencies
        static private long CalculateTextureMemoryUsage(Texture2D texture) {
            int numPixels = texture.width * texture.height;

            switch(texture.format) {
                case TextureFormat.Alpha8: return numPixels;

                case TextureFormat.ARGB4444: return numPixels * 2;
                case TextureFormat.RGB24: return numPixels * 3;
                case TextureFormat.RGBA32: return numPixels * 4;
                case TextureFormat.ARGB32: return numPixels * 4;

                case TextureFormat.RGB565: return numPixels * 2;
                case TextureFormat.R16: return numPixels * 2;

                case TextureFormat.DXT1: return numPixels / 2;
                case TextureFormat.DXT5: return numPixels;

                case TextureFormat.RGBA4444: return numPixels * 2;
                case TextureFormat.BGRA32: return numPixels * 4;

                case TextureFormat.RHalf: return numPixels * 2;
                case TextureFormat.RGHalf: return numPixels * 4;
                case TextureFormat.RGBAHalf: return numPixels * 8;

                case TextureFormat.RFloat: return numPixels * 4;
                case TextureFormat.RGFloat: return numPixels * 8;
                case TextureFormat.RGBAFloat: return numPixels * 16;

                case TextureFormat.YUY2: return numPixels;

                case TextureFormat.DXT1Crunched: return numPixels / 2;
                case TextureFormat.DXT5Crunched: return numPixels;

                case TextureFormat.PVRTC_RGB2: return numPixels / 4;
                case TextureFormat.PVRTC_RGBA2: return numPixels / 4;
                case TextureFormat.PVRTC_RGB4: return numPixels / 2;
                case TextureFormat.PVRTC_RGBA4: return numPixels / 4;

                case TextureFormat.ETC_RGB4: return numPixels / 2;
                case TextureFormat.EAC_R: return numPixels / 2;
                case TextureFormat.EAC_R_SIGNED: return numPixels / 2;
                case TextureFormat.EAC_RG: return numPixels;
                case TextureFormat.EAC_RG_SIGNED: return numPixels;

                case TextureFormat.ETC2_RGB: return numPixels / 2;
                case TextureFormat.ETC2_RGBA1: return numPixels * 5 / 8;
                case TextureFormat.ETC2_RGBA8: return numPixels;

                #if UNITY_5_5_OR_NEWER
                case TextureFormat.BC4: return numPixels / 2;
                case TextureFormat.BC5: return numPixels;
                case TextureFormat.BC6H: return numPixels;
                case TextureFormat.BC7: return numPixels;
                #endif // UNITY_5_5_OR_NEWER

                #if UNITY_5_6_OR_NEWER
                case TextureFormat.RGB9e5Float: return numPixels * 4;
                case TextureFormat.RG16: return numPixels / 2;
                case TextureFormat.R8: return numPixels;
                #endif // UNITY_5_6_OR_NEWER

                #if UNITY_2017_3_OR_NEWER
                case TextureFormat.ETC_RGB4Crunched: return numPixels / 2;
                case TextureFormat.ETC2_RGBA8Crunched: return numPixels;
                #endif // UNITY_2017_3_OR_NEWER

                #if UNITY_2019_1_OR_NEWER
                case TextureFormat.ASTC_RGB_4x4: return numPixels;
                case TextureFormat.ASTC_RGBA_4x4: return numPixels;
                case TextureFormat.ASTC_RGB_5x5: return numPixels * 16 / 25;
                case TextureFormat.ASTC_RGBA_5x5: return numPixels * 16 / 25;
                case TextureFormat.ASTC_RGB_6x6: return numPixels * 16 / 36;
                case TextureFormat.ASTC_RGBA_6x6: return numPixels * 16 / 36;
                case TextureFormat.ASTC_RGB_8x8: return numPixels * 16 / 64;
                case TextureFormat.ASTC_RGBA_8x8: return numPixels * 16 / 64;
                case TextureFormat.ASTC_RGB_10x10: return numPixels * 16 / 100;
                case TextureFormat.ASTC_RGBA_10x10: return numPixels * 16 / 100;
                case TextureFormat.ASTC_RGB_12x12: return numPixels * 16 / 144;
                case TextureFormat.ASTC_RGBA_12x12: return numPixels * 16 / 144;
                case TextureFormat.ASTC_HDR_4x4: return numPixels;
                case TextureFormat.ASTC_HDR_5x5: return numPixels * 16 / 25;
                case TextureFormat.ASTC_HDR_6x6: return numPixels * 16 / 36;
                case TextureFormat.ASTC_HDR_8x8: return numPixels * 16 / 64;
                case TextureFormat.ASTC_HDR_10x10: return numPixels * 16 / 100;
                case TextureFormat.ASTC_HDR_12x12: return numPixels * 16 / 144;
                #endif // UNITY_2019_1_OR_NEWER

                #if UNITY_2019_4_OR_NEWER
                case TextureFormat.RG32: return numPixels * 4;
                case TextureFormat.RGB48: return numPixels * 6;
                case TextureFormat.RGBA64: return numPixels * 8;
                #endif // UNITY_2019_4_OR_NEWER 

                default: return numPixels;
            }
        }

        static private long CalculateAudioMemoryUsage(AudioClip clip) {
            // TODO: Figure out how this should be calculated
            return clip.samples * sizeof(float);
        }
    
        #endregion // Resources

        #region Streamed Textures

        [Flags]
        internal enum UpdatedResizeProperty {
            Size = 0x01,
            Clip = 0x02,
            Pivot = 0x04
        }

        /// <summary>
        /// Calculates the size of a window into a given texture region.
        /// </summary>
        static internal Vector2 GetTextureRegionSize(Texture texture, Rect uvRect) {
            Vector2 size;
            size.x = texture.width * Math.Abs(uvRect.width);
            size.y = texture.height * Math.Abs(uvRect.height);
            return size;
        }

        /// <summary>
        /// Retrieves the parent size for the given Transform.
        /// </summary>
        static internal Vector2? GetParentSize(Transform transform) {
            RectTransform parent = transform.parent as RectTransform;
            if (parent) {
                return parent.rect.size;
            }

            RectTransform selfRect = transform as RectTransform;
            if (selfRect) {
                return selfRect.rect.size;
            }

            return null;
        }

        static internal bool IsAutoSizeHorizontal(AutoSizeMode sizeMode) {
            switch(sizeMode) {
                case AutoSizeMode.StretchX:
                case AutoSizeMode.FitToParent:
                case AutoSizeMode.FillParent:
                case AutoSizeMode.FillParentWithClipping:
                    return true;

                default:
                    return false;
            }
        }

        static internal bool IsAutoSizeVertical(AutoSizeMode sizeMode) {
            switch(sizeMode) {
                case AutoSizeMode.StretchY:
                    return true;

                default:
                    return false;
            }
        }

        static internal bool ControlsAnchors(AutoSizeMode sizeMode) {
            return sizeMode >= AutoSizeMode.FitToParent && sizeMode <= AutoSizeMode.FillParentWithClipping;
        }

        static internal UpdatedResizeProperty AutoSize(AutoSizeMode sizeMode, Texture texture, Rect sourceUV, Vector2 localPosition, Vector2 pivot, ref Vector2 size, ref Rect clippedUV, ref Vector2 appliedPivot, Vector2? parentSize) {
            if (sizeMode == AutoSizeMode.Disabled || !texture) {
                if (clippedUV != sourceUV) {
                    clippedUV = sourceUV;
                    return UpdatedResizeProperty.Clip;
                }
                return 0;
            }

            Vector2 textureSize = GetTextureRegionSize(texture, sourceUV);

            if (textureSize.x == 0 || textureSize.y == 0) {
                if (clippedUV != sourceUV) {
                    clippedUV = sourceUV;
                    return UpdatedResizeProperty.Clip;
                }
                return 0;
            }

            Vector2 originalSize = size;
            Vector2 originalPivot = appliedPivot;
            Vector2 parentSizeVector = parentSize.GetValueOrDefault();
            Rect originalUV = clippedUV;
            clippedUV = sourceUV;

            switch(sizeMode) {
                case AutoSizeMode.StretchX: {
                    size.x = size.y * (textureSize.x / textureSize.y);
                    break;
                }
                case AutoSizeMode.StretchY: {
                    size.y = size.x * (textureSize.y / textureSize.x);
                    break;
                }
                case AutoSizeMode.FitToParent: {
                    if (!parentSize.HasValue) {
                        break;
                    }

                    float aspect = textureSize.x / textureSize.y;
                    size.x = parentSizeVector.y * aspect;
                    size.y = parentSizeVector.y;

                    appliedPivot = pivot;

                    if (size.x > parentSizeVector.x) {
                        size.x = parentSizeVector.x;
                        size.y = size.x / aspect;
                    } else if (size.y > parentSizeVector.y) {
                        size.y = parentSizeVector.y;
                        size.x = size.y * aspect;
                    }
                    break;
                }
                case AutoSizeMode.FillParent: {
                    if (!parentSize.HasValue) {
                        break;
                    }

                    float aspect = textureSize.x / textureSize.y;
                    size.x = parentSizeVector.y * aspect;
                    size.y = parentSizeVector.y;

                    appliedPivot = pivot;

                    if (size.x < parentSizeVector.x) {
                        size.x = parentSizeVector.x;
                        size.y = size.x / aspect;
                    } else if (size.y < parentSizeVector.y) {
                        size.y = parentSizeVector.y;
                        size.x = size.y * aspect;
                    }
                    break;
                }

                case AutoSizeMode.FillParentWithClipping: {
                    if (!parentSize.HasValue) {
                        break;
                    }

                    float aspect = textureSize.x / textureSize.y;
                    size.x = parentSizeVector.y * aspect;
                    size.y = parentSizeVector.y;

                    appliedPivot = pivot;

                    if (size.x < parentSizeVector.x) {
                        size.x = parentSizeVector.x;
                        size.y = size.x / aspect;
                    } else if (size.y < parentSizeVector.y) {
                        size.y = parentSizeVector.y;
                        size.x = size.y * aspect;
                    }

                    float xRatio = parentSizeVector.x / size.x;
                    float yRatio = parentSizeVector.y / size.y;

                    float xOffset = (1 - xRatio) * pivot.x * clippedUV.width;
                    float yOffset = (1 - yRatio) * pivot.y * clippedUV.height;

                    clippedUV.x += xOffset;
                    clippedUV.y += yOffset;
                    clippedUV.width *= xRatio;
                    clippedUV.height *= yRatio;

                    size = parentSizeVector;

                    break;
                }
            }

            UpdatedResizeProperty prop = 0;
            if (size != originalSize) {
                prop |= UpdatedResizeProperty.Size;
            }
            if (clippedUV != originalUV) {
                prop |= UpdatedResizeProperty.Clip;
            }
            if (appliedPivot != originalPivot) {
                prop |= UpdatedResizeProperty.Pivot;
            }
            return prop;
        }

        #endregion // Streamed Textures

        #region Misc

        static internal bool FastRemove<T>(this List<T> list, T item) {
            int end = list.Count - 1;
            int index = list.IndexOf(item);
            if (index >= 0)
            {
                if (index != end)
                    list[index] = list[end];
                list.RemoveAt(end);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts an unmanaged buffer to a unity NativeArray.
        /// </summary>
        static internal unsafe NativeArray<T> ToNativeArray<T>(T* ptr, int length, AtomicSafetyHandle safetyHandle) where T : unmanaged {
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(ptr, length, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, safetyHandle);
            return arr;
        }

        /// <summary>
        /// Hashes the given unmanaged struct.
        /// </summary>
        static internal unsafe ulong Hash<T>(T value) where T : unmanaged {
            // fnv-1a
            ulong hash = 14695981039346656037;
            byte* ptr = (byte*) &value;
            int length = sizeof(T);
            while(length-- > 0) {
                hash = (hash ^ *ptr++) * 1099511628211;
            }
            return hash;
        }

        #endregion // Misc
    }
}