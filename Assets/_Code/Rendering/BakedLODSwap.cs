using UnityEngine;
using ScriptableBake;
using System;

namespace Aqua {
    [RequireComponent(typeof(Renderer))]
    public class BakedLODSwap : MonoBehaviour, IBaked {
        
        [Serializable]
        public struct Swap {
            public Mesh Mesh;
            public Material Material;

            public bool Enabled {
                get { return Mesh || Material; }
            }
        }

        [Header("Low Swap")]
        public float LowDistance = 40;
        public Swap LowSwap = default;

        [Header("Fog Swap")]
        public Swap FogSwap = default;

        #if UNITY_EDITOR

        public int Order { get { return 100; } }

        public bool Bake(BakeFlags flags, BakeContext context) {
            MeshFilter filter = GetComponent<MeshFilter>();
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            SkinnedMeshRenderer skinnedRenderer = GetComponent<SkinnedMeshRenderer>();

            Bounds rendererBounds;
            if (skinnedRenderer) {
                rendererBounds = skinnedRenderer.bounds;
            } else {
                rendererBounds = renderer.bounds;
            }

            float minZ = rendererBounds.min.z;
            float cameraZ = context.MainCamera.transform.position.z;
            float dist = Math.Abs(minZ - cameraZ);

            if (FogSwap.Enabled && dist >= context.FogEndDistance - 0.01f) {
                Apply(filter, renderer, skinnedRenderer, FogSwap);
                Baking.Destroy(this);
                return true;
            } else if (LowSwap.Enabled && dist >= LowDistance) {
                Apply(filter, renderer, skinnedRenderer, LowSwap);
                Baking.Destroy(this);
                return true;
            } else {
                Baking.Destroy(this);
                return false;
            }
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

        static private void Apply(MeshFilter filter, MeshRenderer renderer, SkinnedMeshRenderer skinnedRenderer, Swap swap) {
            if (swap.Mesh != null) {
                if (skinnedRenderer != null) {
                    skinnedRenderer.sharedMesh = swap.Mesh;
                    UnityEditor.EditorUtility.SetDirty(skinnedRenderer);
                } else if (filter) {
                    filter.sharedMesh = swap.Mesh;
                    UnityEditor.EditorUtility.SetDirty(filter);
                }
            }
            if (swap.Material != null) {
                if (skinnedRenderer) {
                    skinnedRenderer.sharedMaterial = swap.Material;
                    UnityEditor.EditorUtility.SetDirty(skinnedRenderer);
                } else if (renderer) {
                    renderer.sharedMaterial = swap.Material;
                    UnityEditor.EditorUtility.SetDirty(renderer);
                }
            }
        }

        #endif // UNITY_EDITOR
    }
}