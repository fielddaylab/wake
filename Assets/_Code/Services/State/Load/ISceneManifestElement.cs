using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    #if UNITY_EDITOR
    public class SceneManifestBuilder  {
        public HashSet<string> Paths = new HashSet<string>();
        public HashSet<ScriptableObject> Assets = new HashSet<ScriptableObject>();
    }
    #endif // UNITY_EDITOR

    public interface ISceneManifestElement {
        #if UNITY_EDITOR
        void BuildManifest(SceneManifestBuilder manifest);
        #endif // UNITY_EDITOR
    }
}