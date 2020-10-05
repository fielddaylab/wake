using BeauUtil;
using ProtoAqua;
using UnityEngine;

namespace ProtoAudio
{
    public class AudioPackageLoader : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private AudioPackage[] m_Packages = null;

        #endregion // Inspector

        private void OnEnable()
        {
            AudioMgr mgr = Services.Audio;
            foreach(var package in m_Packages)
                mgr.Load(package);
        }

        private void OnDisable()
        {
            AudioMgr mgr = Services.Audio;
            if (!mgr)
                return;
            
            foreach(var package in m_Packages)
                mgr.Unload(package);
        }
    }
}