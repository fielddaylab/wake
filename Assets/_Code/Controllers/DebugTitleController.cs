using System;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aqua
{
    public class DebugTitleController : MonoBehaviour, ISceneLoadHandler
    {
        [Serializable] private class SceneButtonPool : SerializablePool<SceneButton> { }

        #region Inspector

        [SerializeField] private SceneButtonPool m_SceneButtonPool = null;

        #endregion // Inspector

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            foreach(var scene in SceneHelper.FindScenes(SceneCategories.Build))
            {
                Debug.LogFormat("[DebugTitleController] Found scene {0}", scene);

                if (scene == inScene)
                    continue;
                
                m_SceneButtonPool.Alloc().Initialize(scene);
            }
        }
    }
}