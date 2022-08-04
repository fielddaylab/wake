using Aqua.Compression;
using UnityEngine;
using BeauUtil;
using System.Runtime.CompilerServices;
using UnityEngine.UI;
using EasyAssetStreaming;
using TMPro;
using System.Runtime.InteropServices;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Aqua.Compression {
    public class CompressiblePrefab : MonoBehaviour {
        #region Types

        [StructLayout(LayoutKind.Sequential)]
        public struct PrefabHeader {
            public ushort ObjectCount;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ObjectHeader {
            [FieldOffset(0)] public CompressedPrefabFlags Flags;
            [FieldOffset(2)] public ushort NameIdx;
            [FieldOffset(4)] public CompressedComponentTypes ComponentTypes;
        }

        #endregion // Types

        #region Compress

        #if UNITY_EDITOR

        internal byte[] Compress(PackageBuilder compressor, CompressedRectTransformBounds rectBounds) {
            unsafe {
                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();

                // flatten
                int immediateChildCount = transform.childCount;
                Transform[] children = new Transform[immediateChildCount];
                for(int i = 0; i < immediateChildCount; i++) {
                    children[i] = transform.GetChild(i);
                }
                foreach(var child in children) {
                    FlattenChildren(child);
                }

                // candidates
                RuntimeObjectHandle* layoutBuffer = stackalloc RuntimeObjectHandle[32];
                CompressedComponentTypes* typeBuffer = stackalloc CompressedComponentTypes[32];

                layoutBuffer[0] = transform;
                typeBuffer[0] = ComponentsOfInterest(transform);

                int layoutElementCount = 1;

                immediateChildCount = transform.childCount;
                for(int i = 0; i < immediateChildCount; i++) {
                    Transform child = transform.GetChild(i);
                    CompressedComponentTypes types = ComponentsOfInterest(child);
                    if ((types & ~CompressedComponentTypes.AnyTransform) != 0) {
                        layoutBuffer[layoutElementCount] = child;
                        typeBuffer[layoutElementCount] = types;
                        layoutElementCount++;
                    }
                }

                // write to buffer
                byte* tempBuffer = stackalloc byte[2048];
                int bufferLength = 0;

                byte* bufferHead = tempBuffer;

                PrefabHeader prefabHeader;
                prefabHeader.ObjectCount = (ushort) layoutElementCount;

                UnsafeExt.Write(&bufferHead, &bufferLength, prefabHeader);

                for(int i = 0; i < layoutElementCount; i++) {
                    CompressedPrefabFlags flags = i == 0 ? CompressedPrefabFlags.IsRoot : 0;
                    WriteObject(&bufferHead, &bufferLength, (Transform) layoutBuffer[i].Object, flags, typeBuffer[i], compressor, rectBounds);
                }

                // revert
                Undo.RevertAllInCurrentGroup();

                // turn buffer into byte array
                byte[] byteArr = new byte[bufferLength];
                Unsafe.CopyArray(tempBuffer, bufferLength, byteArr);
                return byteArr;
            }
        }

        static private unsafe void WriteObject(byte** buffer, int* size, Transform obj, CompressedPrefabFlags flags, CompressedComponentTypes components, PackageBuilder compressor, CompressedRectTransformBounds rectBounds) {
            ObjectHeader objHeader;
            objHeader.NameIdx = compressor.AddString(obj.name);
            objHeader.ComponentTypes = components;
            objHeader.Flags = flags;

            UnsafeExt.Write(buffer, size, objHeader);

            if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.RectTransform)) {
                RectTransform rect = (RectTransform) obj;
                CompressedRectTransform.Compress(rectBounds, rect, out CompressedRectTransform data);
                UnsafeExt.Write(buffer, size, data);
            }

            if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.RectGraphic)) {
                RectGraphic graphic = obj.GetComponent<RectGraphic>();
                CompressedRectGraphic.Compress(graphic, out CompressedRectGraphic data);
                UnsafeExt.Write(buffer, size, data);
            }

            if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.Image)) {
                Image graphic = obj.GetComponent<Image>();
                CompressedImage.Compress(compressor, graphic, out CompressedImage data);
                UnsafeExt.Write(buffer, size, data);
            }

            if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.StreamingUGUITexture)) {
                StreamingUGUITexture graphic = obj.GetComponent<StreamingUGUITexture>();
                CompressedStreamingUGUITexture.Compress(compressor, graphic, out CompressedStreamingUGUITexture data);
                UnsafeExt.Write(buffer, size, data);
            }

            if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.TextMeshPro)) {
                TMP_Text graphic = obj.GetComponent<TMP_Text>();
                CompressedTextMeshPro.Compress(compressor, graphic, out CompressedTextMeshPro data);
                UnsafeExt.Write(buffer, size, data);
            }

            if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.LocText)) {
                LocText locText = obj.GetComponent<LocText>();
                CompressedLocText.Compress(locText, out CompressedLocText data);
                UnsafeExt.Write(buffer, size, data);
            }
        }

        static private CompressedComponentTypes ComponentsOfInterest(Transform transform) {
            CompressedComponentTypes types = 0;

            if (transform.GetType() == typeof(RectTransform)) {
                types |= CompressedComponentTypes.RectTransform;
            } else {
                types |= CompressedComponentTypes.Transform;
            }

            if (HasBehavior<RectGraphic>(transform)) {
                types |= CompressedComponentTypes.RectGraphic;
            } else if (HasBehavior<Image>(transform)) {
                types |= CompressedComponentTypes.Image;
            } else if (HasBehavior<StreamingUGUITexture>(transform)) {
                types |= CompressedComponentTypes.StreamingUGUITexture;
            } else if (HasBehavior<TMP_Text>(transform)) {
                types |= CompressedComponentTypes.TextMeshPro;
            }

            if (HasBehavior<LocText>(transform)) {
                types |= CompressedComponentTypes.LocText;
            }

            return types;
        }

        static private bool HasBehavior<T>(Transform transform) where T : Behaviour {
            T obj = transform.GetComponent<T>();
            return obj && obj.enabled;
        }

        static private bool HasRenderer<T>(Transform transform) where T : Renderer {
            T obj = transform.GetComponent<T>();
            return obj && obj.enabled;
        }

        #region Flatten

        static private void FlattenChildren(Transform transform) {
            int placeIdx = transform.GetSiblingIndex() + 1;
            FlattenHierarchyRecursive(transform, transform.parent, ref placeIdx);
        }

        static private void FlattenHierarchyRecursive(Transform inTransform, Transform inParent, ref int ioSiblingIndex) {
            Transform transform = inTransform;
            Transform child;
            int childCount = transform.childCount;
            while(childCount-- > 0)
            {
                child = transform.GetChild(0);
                Undo.RecordObject(child, "Flatten child");
                Undo.SetTransformParent(child, inParent, "Change child parent");
                child.SetSiblingIndex(ioSiblingIndex++);
                FlattenHierarchyRecursive(child, inParent, ref ioSiblingIndex);
            }
        }

        #endregion // Flatten

        #endif // UNITY_EDITOR

        #endregion // Compress

        #region Decompress

        static public GameObject Decompress(byte[] buffer, PackageBank bank, in PrefabDecompressor decompressor) {
            unsafe {
                fixed(byte* bufferPtr = buffer) {
                    return Decompress(bufferPtr, buffer.Length, bank, decompressor);
                }
            }
        }

        static public GameObject Decompress(byte[] buffer, int offset, int length, PackageBank bank, in PrefabDecompressor decompressor) {
            unsafe {
                fixed(byte* bufferPtr = buffer) {
                    return Decompress(bufferPtr + offset, length, bank, decompressor);
                }
            }
        }

        static public unsafe GameObject Decompress(byte* buffer, int bufferLength, PackageBank bank, in PrefabDecompressor decompressor) {
            byte* head = buffer;
            int size = bufferLength;

            PrefabHeader prefabHeader = UnsafeExt.Read<PrefabHeader>(&head, &size);
            GameObject root = null;
            for(int i = 0; i < prefabHeader.ObjectCount; i++) {
                ObjectHeader objHeader = UnsafeExt.Read<ObjectHeader>(&head, &size);
                string name = bank.GetString(objHeader.NameIdx);
                GameObject go = decompressor.NewRoot(name, objHeader.Flags, objHeader.ComponentTypes, root);
                if ((objHeader.Flags & CompressedPrefabFlags.IsRoot) != 0) {
                    root = go;
                    root.SetActive(false);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.RectTransform)) {
                    RectTransform rect = (RectTransform) decompressor.NewComponent(go, CompressedComponentTypes.RectTransform);
                    CompressedRectTransform data = UnsafeExt.Read<CompressedRectTransform>(&head, &size);
                    CompressedRectTransform.Decompress(decompressor.RectTransformBounds, data, rect);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.RectGraphic)) {
                    RectGraphic graphic = (RectGraphic) decompressor.NewComponent(go, CompressedComponentTypes.RectGraphic);
                    CompressedRectGraphic data = UnsafeExt.Read<CompressedRectGraphic>(&head, &size);
                    CompressedRectGraphic.Decompress(data, graphic);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.Image)) {
                    Image graphic = (Image) decompressor.NewComponent(go, CompressedComponentTypes.Image);
                    CompressedImage data = UnsafeExt.Read<CompressedImage>(&head, &size);
                    CompressedImage.Decompress(bank, data, graphic);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.StreamingUGUITexture)) {
                    StreamingUGUITexture graphic = (StreamingUGUITexture) decompressor.NewComponent(go, CompressedComponentTypes.StreamingUGUITexture);
                    CompressedStreamingUGUITexture data = UnsafeExt.Read<CompressedStreamingUGUITexture>(&head, &size);
                    CompressedStreamingUGUITexture.Decompress(bank, data, graphic);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.TextMeshPro)) {
                    TMP_Text graphic = (TMP_Text) decompressor.NewComponent(go, CompressedComponentTypes.TextMeshPro);
                    CompressedTextMeshPro data = UnsafeExt.Read<CompressedTextMeshPro>(&head, &size);
                    CompressedTextMeshPro.Decompress(bank, data, graphic);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.LocText)) {
                    LocText locText = (LocText) decompressor.NewComponent(go, CompressedComponentTypes.LocText);
                    CompressedLocText data = UnsafeExt.Read<CompressedLocText>(&head, &size);
                    CompressedLocText.Decompress(data, locText);
                }
            }

            return root;
        }

        #endregion // Decompress

        [MethodImpl(256)]
        static private bool HasComponent(CompressedComponentTypes all, CompressedComponentTypes type) {
            return (all & type) == type;
        }
    
        #region Testing

        #if UNITY_EDITOR

        [ContextMenu("Dry Run Compress and Decompress")]
        private void DryRun() {
            PackageBuilder pkg = new PackageBuilder();
            byte[] buffer = Compress(pkg, CompressedRectTransformBounds.Default);

            PrefabDecompressor decompressor = PrefabDecompressor.Default;

            PackageBank bank = new PackageBank(pkg);
            GameObject obj = Decompress(buffer, bank, decompressor);
            obj.transform.SetParent(transform.parent, false);
        }

        #endif // UNITY_EDITOR

        #endregion // Testing
    }

    public enum CompressedComponentTypes : uint {
        Transform = 0x01,
        RectTransform = 0x02,
        RectGraphic = 0x04,
        Image = 0x08,
        StreamingUGUITexture = 0x10,
        TextMeshPro = 0x20,
        LocText = 0x40,

        [Hidden] AnyTransform = Transform | RectTransform
    }

    public enum CompressedPrefabFlags : byte {
        IsRoot = 0x01
    }
}