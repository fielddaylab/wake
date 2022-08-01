using UnityEngine;
using BeauUtil;
using BeauUtil.UI;
using System;
using TMPro;
using UnityEngine.UI;
using Aqua.Cameras;
using EasyAssetStreaming;
using System.Collections.Generic;

namespace Aqua.Shop
{
    public sealed class ShopPreviewShipItem : MonoBehaviour {

        [ItemId(InvItemCategory.Upgrade)] public StringHash32 ItemId;
        
        [Space]
        public float Rotation;

        [Header("Meshes")]
        public MeshRenderer[] Meshes;
        public Material ActiveMaterial;
        public Material PreviewMaterial;

        [Header("Effects")]
        public ParticleSystem[] WeldParticles;

        [NonSerialized] public bool IsPurchased;

        #if UNITY_EDITOR

        private void Reset() {
            Meshes = GetComponentsInChildren<MeshRenderer>(true);
            List<ParticleSystem> children = new List<ParticleSystem>();
            this.GetImmediateComponentsInChildren<ParticleSystem>(this, children);
            WeldParticles = children.ToArray();
            ActiveMaterial = ValidationUtils.FindAsset<Material>("MiniSub00_Interior_Upgrades");
            PreviewMaterial = ValidationUtils.FindAsset<Material>("HolographicPreview");
        }

        #endif // UNITY_EDITOR
    }
}