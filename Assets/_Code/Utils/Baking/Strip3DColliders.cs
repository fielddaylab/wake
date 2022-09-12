using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil;
using System.Collections.Generic;
using BeauUtil.Debugger;
using ScriptableBake;

namespace Aqua
{
    public sealed class Strip3DColliders : MonoBehaviour, IBaked {

        #if UNITY_EDITOR

        public int Order { get { return ScriptableBake.FlattenHierarchy.Order - 1; } }

        public bool Bake(BakeFlags flags) {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach(var collider in colliders) {
                ScriptableBake.Bake.Destroy(collider);
            }
            ScriptableBake.Bake.Destroy(this);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}