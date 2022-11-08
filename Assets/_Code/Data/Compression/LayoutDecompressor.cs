using System;
using BeauPools;
using BeauUtil;
using EasyAssetStreaming;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua.Compression {
    public class LayoutDecompressor : MonoBehaviour {
        #region Types

        [Serializable] private class TextPool : SerializablePool<TMP_Text> { }
        [Serializable] private class RectGraphicPool : SerializablePool<RectGraphic> { }
        [Serializable] private class ImagePool : SerializablePool<Image> { }
        [Serializable] private class StreamingUGUIPool : SerializablePool<StreamingUGUITexture> { }
        [Serializable] private class LocTextPool : SerializablePool<LocText> { }

        #endregion // Types
        
        #region Inspector

        [SerializeField] private Transform m_PoolRoot = null;

        [Header("Pools")]
        [SerializeField] private TextPool m_TextPool = null;
        [SerializeField] private RectGraphicPool m_RectGraphicPool = null;
        [SerializeField] private ImagePool m_ImagePool = null;
        [SerializeField] private StreamingUGUIPool m_StreamingUGUIPool = null;
        [SerializeField] private LocTextPool m_LocTextPool = null;

        #endregion // Inspector

        private PrefabDecompressor m_Decompressor;
        [NonSerialized] private GameObject m_DecompressionTarget = null;

        protected LayoutDecompressor() {
            m_Decompressor.NewRoot = (string name, CompressedPrefabFlags flags, CompressedComponentTypes componentTypes, GameObject parent) => {
                GameObject go = null;
                if ((flags & CompressedPrefabFlags.IsRoot) != 0) {
                    go = m_DecompressionTarget;
                } else if ((componentTypes & CompressedComponentTypes.LocText) != 0) {
                    go = m_LocTextPool.Alloc(parent.transform).gameObject;
                } else if ((componentTypes & CompressedComponentTypes.RectGraphic) != 0) {
                    go = m_RectGraphicPool.Alloc(parent.transform).gameObject;
                } else if ((componentTypes & CompressedComponentTypes.Image) != 0) {
                    go = m_ImagePool.Alloc(parent.transform).gameObject;
                } else if ((componentTypes & CompressedComponentTypes.TextMeshPro) != 0) {
                    go = m_TextPool.Alloc(parent.transform).gameObject;
                } else if ((componentTypes & CompressedComponentTypes.StreamingUGUITexture) != 0) {
                    go = m_StreamingUGUIPool.Alloc(parent.transform).gameObject;
                } else {
                    return null;
                }
                go.name = name;
                return go;
            };
            m_Decompressor.NewComponent = PrefabDecompressor.DefaultNewComponent;
        }

        private void Awake() {
            m_TextPool.ConfigureTransforms(m_PoolRoot, null, false);
            m_RectGraphicPool.ConfigureTransforms(m_PoolRoot, null, false);
            m_ImagePool.ConfigureTransforms(m_PoolRoot, null, false);
            m_StreamingUGUIPool.ConfigureTransforms(m_PoolRoot, null, false);
            m_LocTextPool.ConfigureTransforms(m_PoolRoot, null, false);
        }

        private void ResetPools() {
            m_TextPool.Reset();
            m_ImagePool.Reset();
            m_LocTextPool.Reset();
            m_RectGraphicPool.Reset();
            m_StreamingUGUIPool.Reset();
        }

        public GameObject Decompress(LayoutPrefabPackage pkg, StringHash32 id, GameObject root) {
            m_DecompressionTarget = root;
            GameObject spawned = pkg.Decompress(id, m_Decompressor);
            m_DecompressionTarget = null;
            return spawned;
        }

        public void ClearAll() {
            ResetPools();
        }
    }
}