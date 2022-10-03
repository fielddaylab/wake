using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil;
using System.Collections.Generic;
using BeauUtil.Debugger;
using ScriptableBake;

namespace Aqua
{
    public sealed class EditModeOnly : MonoBehaviour, IBaked {

        #if UNITY_EDITOR

        public int Order { get { return ScriptableBake.FlattenHierarchy.Order - 2; } }

        public bool Bake(BakeFlags flags, BakeContext context) {
            ScriptableBake.Bake.Destroy(gameObject);
            return true;
        }

        #endif // UNITY_EDITOR
    }
}