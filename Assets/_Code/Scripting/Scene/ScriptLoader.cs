using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;

namespace Aqua.Scripting
{
    [AddComponentMenu("Aqualab/Scripting/Script Loader")]
    public class ScriptLoader : MonoBehaviour, IScenePreloader, ISceneUnloadHandler
    {
        #region Inspector

        [SerializeField, Required] private LeafAsset[] m_Scripts = null;

        #endregion // Inspector

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext)
        {
            for(int i = 0; i < m_Scripts.Length; ++i)
            {
                LeafAsset file = m_Scripts[i];
                Services.Script.LoadScript(file);
            }


            return null;
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext)
        {
            for(int i = 0; i < m_Scripts.Length; ++i)
            {
                LeafAsset file = m_Scripts[i];
                Services.Script.UnloadScript(file);
            }
        }
    }
}