using UnityEngine;
using ScriptableBake;
using System;

namespace Aqua {
    public class BakedLODSwap : MonoBehaviour, IBaked {
        
        [Serializable]
        public struct SwapSettings {
            public Mesh Mesh;
            public Material Material;

            public bool Enabled {
                get { return Mesh || Material; }
            }
        }

        [Serializable]
        public struct UnskinSettings {
            public bool Reparent;
            public Transform NewParent;

            public Transform DeleteRoot;
        }

        [Header("Low Swap")]
        public float LowDistance = 40;
        public SwapSettings LowSwap = default;

        [Header("Fog Swap")]
        public SwapSettings FogSwap = default;

        [Header("Unskinning")]
        public float UnskinDistance = 0;
        public UnskinSettings Unskin;

        #if UNITY_EDITOR

        private struct RendererComponents {
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public SkinnedMeshRenderer Skinned;
            public Bounds Bounds;

            static internal RendererComponents FromGameObject(GameObject go) {
                RendererComponents components;
                components.Filter = go.GetComponent<MeshFilter>();
                components.Renderer = go.GetComponent<MeshRenderer>();
                components.Skinned = go.GetComponent<SkinnedMeshRenderer>();
                if (components.Skinned) {
                    components.Bounds = components.Skinned.bounds;
                } else if (components.Renderer) {
                    components.Bounds = components.Renderer.bounds;
                } else {
                    components.Bounds = default;
                }
                return components;
            }
        }

        private struct RendererSettings {
            public Mesh Mesh;
            public Material Material;

            static internal RendererSettings FromComponents(RendererComponents components) {
                RendererSettings settings;
                if (components.Filter) {
                    settings.Mesh = components.Filter.sharedMesh;
                } else if (components.Skinned) {
                    settings.Mesh = components.Skinned.sharedMesh;
                } else {
                    settings.Mesh = null;
                }

                if (components.Renderer) {
                    settings.Material = components.Renderer.sharedMaterial;
                } else if (components.Skinned) {
                    settings.Material = components.Skinned.sharedMaterial;
                } else {
                    settings.Material = null;
                }

                return settings;
            }
        }

        public int Order { get { return 100; } }

        public bool Bake(BakeFlags flags, BakeContext context) {
            RendererComponents components = RendererComponents.FromGameObject(gameObject);
            RendererSettings settings = RendererSettings.FromComponents(components);

            float minZ = components.Bounds.min.z;
            float cameraZ = context.MainCamera.transform.position.z;
            float dist = Math.Abs(minZ - cameraZ);

            bool bChanged = false;

            if (UnskinDistance > 0 && dist >= UnskinDistance) {
                UnskinMesh(gameObject, ref components, Unskin);
                bChanged = true;
            }

            if (FogSwap.Enabled && dist >= context.FogEndDistance - 0.01f) {
                Override(ref settings, FogSwap);
                ApplySettings(ref components, settings);
                bChanged = true;
            } else if (LowSwap.Enabled && dist >= LowDistance) {
                Override(ref settings, LowSwap);
                ApplySettings(ref components, settings);
                bChanged = true;
            }

            Baking.Destroy(this);
            return bChanged;
        }

        private void Reset() {
            LowDistance = 40;

            Material silhouetteMaterial = ValidationUtils.FindAsset<Material>("FogSilhouette");
            MeshFilter filter = GetComponent<MeshFilter>();
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            SkinnedMeshRenderer skinnedRenderer = GetComponent<SkinnedMeshRenderer>();

            Mesh mesh;
            if (skinnedRenderer != null) {
                mesh = skinnedRenderer.sharedMesh;
            } else if (filter) {
                mesh = filter.sharedMesh;
            } else {
                return;
            }

            string meshName = mesh.name;
            string lod1Name = meshName + "_LOD1";
            string lod2Name = meshName + "_LOD2";

            Mesh lodMesh1 = ValidationUtils.FindAsset<Mesh>(lod1Name);
            Mesh lodMesh2 = ValidationUtils.FindAsset<Mesh>(lod2Name);

            if (lodMesh1) {
                LowSwap.Mesh = lodMesh1;
                FogSwap.Mesh = lodMesh1;
            }

            if (lodMesh2) {
                FogSwap.Mesh = lodMesh2;
            }

            if (silhouetteMaterial) {
                FogSwap.Material = silhouetteMaterial;
            }
        }

        static private void Override(ref RendererSettings settings, SwapSettings swap) {
            if (swap.Mesh != null) {
                settings.Mesh = swap.Mesh;
            }

            if (swap.Material != null) {
                settings.Material = swap.Material;
            }
        }

        static private void UnskinMesh(GameObject go, ref RendererComponents components, UnskinSettings unskin) {
            RendererSettings renderSettings = RendererSettings.FromComponents(components);

            if (unskin.Reparent) {
                go.transform.SetParent(unskin.NewParent, true);
            }

            Baking.Destroy(components.Skinned.rootBone.gameObject);
            Baking.Destroy(components.Skinned);

            if (unskin.DeleteRoot) {
                Baking.Destroy(unskin.DeleteRoot.gameObject);
            }

            components.Skinned = null;

            components.Filter = go.AddComponent<MeshFilter>();
            components.Renderer = go.AddComponent<MeshRenderer>();

            components.Bounds = components.Renderer.bounds;
            ApplySettings(ref components, renderSettings);

        }

        static private void ApplySettings(ref RendererComponents components, RendererSettings settings) {
            if (settings.Mesh != null) {
                if (components.Filter != null) {
                    components.Filter.sharedMesh = settings.Mesh;
                } else if (components.Skinned != null) {
                    components.Skinned.sharedMesh = settings.Mesh;
                }
            }

            if (settings.Material != null) {
                if (components.Renderer != null) {
                    components.Renderer.sharedMaterial = settings.Material;
                } else if (components.Skinned != null) {
                    components.Skinned.sharedMaterial = settings.Material;
                }
            }
        }

        #endif // UNITY_EDITOR
    }
}