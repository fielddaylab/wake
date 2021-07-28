using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Aqua;

namespace ProtoAqua.Observation
{
    public class ScanDataLoader : MonoBehaviour, IScenePreloader, ISceneUnloadHandler
    {
        #region Inspector

        [SerializeField, Required] private ScanDataPackage[] m_ScanData = null;

        #endregion // Inspector

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext)
        {
            var scanMgr = ScanSystem.Find<ScanSystem>();
            using(Profiling.Time("loading scan data"))
            {
                for(int i = 0; i < m_ScanData.Length; ++i)
                {
                    scanMgr.Load(m_ScanData[i]);
                }
            }
            yield break;
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext)
        {
            var scanMgr = ScanSystem.Find<ScanSystem>();
            for(int i = 0; i < m_ScanData.Length; ++i)
            {
                scanMgr.Unload(m_ScanData[i]);
            }
        }
    }
}