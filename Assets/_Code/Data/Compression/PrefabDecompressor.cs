using Aqua.Compression;
using UnityEngine;
using BeauUtil;
using UnityEngine.UI;
using EasyAssetStreaming;
using TMPro;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua.Compression {
    public struct PrefabDecompressor {
        public delegate GameObject NewRootDelegate(string name, CompressedPrefabFlags flags, CompressedComponentTypes componentTypes, GameObject parent);
        public delegate Component NewComponentDelegate(GameObject obj, CompressedComponentTypes componentType);

        public CompressedTransformBounds TransformBounds;
        public CompressedRectTransformBounds RectTransformBounds;

        public NewRootDelegate NewRoot;
        public NewComponentDelegate NewComponent;

        static public NewRootDelegate DefaultNewRoot = (n, f, c, p) => {
            GameObject go = new GameObject(n);
            if (p != null) {
                go.transform.SetParent(p.transform);
            }
            return go;
        };

        static public NewComponentDelegate DefaultNewComponent = (o, t) => {
            switch(t) {
                case CompressedComponentTypes.Transform: {
                    return o.transform;
                }
                case CompressedComponentTypes.RectTransform: {
                    return o.EnsureComponent<RectTransform>();
                }
                case CompressedComponentTypes.RectGraphic: {
                    return o.EnsureComponent<RectGraphic>();
                }
                case CompressedComponentTypes.Image: {
                    return o.EnsureComponent<Image>();
                }
                case CompressedComponentTypes.StreamingUGUITexture: {
                    return o.EnsureComponent<StreamingUGUITexture>();
                }
                case CompressedComponentTypes.TextMeshPro: {
                    return o.EnsureComponent<TextMeshProUGUI>();
                }
                case CompressedComponentTypes.LocText: {
                    return o.EnsureComponent<LocText>();
                }
                default: {
                    throw new ArgumentException("Unable to instantiate component of type " + t.ToString());
                }
            }
        };

        static private readonly PrefabDecompressor s_Default = new PrefabDecompressor() {
            TransformBounds = CompressedTransformBounds.Default,
            RectTransformBounds = CompressedRectTransformBounds.Default,
            NewRoot = DefaultNewRoot,
            NewComponent = DefaultNewComponent
        };

        static public PrefabDecompressor Default { get { return s_Default; } }
    }
}