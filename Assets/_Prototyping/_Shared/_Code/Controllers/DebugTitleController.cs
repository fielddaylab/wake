using System;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProtoAqua
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
                m_SceneButtonPool.Alloc().Initialize(scene);
            }

            Services.UI.HideLoadingScreen();
        }
    }
}