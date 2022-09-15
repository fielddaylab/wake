using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Aqua
{
    public struct PreloadManifest : ISerializedObject {
        public PreloadGroup[] Groups;

        public void Serialize(Serializer ioSerializer) {
            ioSerializer.ObjectArray("groups", ref Groups);
        }
    }

    public class PreloadGroup : ISerializedObject {
        // Serialized
        public string Id;
        public string[] Include;
        public string[] LowPriority;
        public string[] Paths;

        // Non-serialized
        public int RefCount;

        [Preserve]
        public PreloadGroup() { }

        public void Serialize(Serializer ioSerializer) {
            ioSerializer.Serialize("id", ref Id, FieldOptions.PreferAttribute);
            ioSerializer.Array("include", ref Include, FieldOptions.Optional);
            ioSerializer.Array("lowPriority", ref LowPriority, FieldOptions.Optional);
            ioSerializer.Array("paths", ref Paths, FieldOptions.Optional);
        }
    }
}