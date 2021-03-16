using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;

namespace Aqua
{
    public class TweakLoader : MonoBehaviour, IScenePreloader, ISceneUnloadHandler
    {
        #region Inspector

        [SerializeField, Required] private TweakAsset[] m_Tweaks = null;

        #endregion // Inspector

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext)
        {
            using(Profiling.Time("loading tweaks"))
            {
                for(int i = 0; i < m_Tweaks.Length; ++i)
                {
                    Services.Tweaks.Load(m_Tweaks[i]);
                    yield return null;
                }
            }
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext)
        {
            for(int i = 0; i < m_Tweaks.Length; ++i)
            {
                Services.Tweaks.Unload(m_Tweaks[i]);
            }
        }
    }
}