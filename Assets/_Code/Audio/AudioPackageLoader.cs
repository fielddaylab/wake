using BeauUtil;
using Aqua;
using UnityEngine;

namespace AquaAudio
{
    [DefaultExecutionOrder(-100)]
    public class AudioPackageLoader : MonoBehaviour, ISceneManifestElement
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

        #if UNITY_EDITOR

        public void BuildManifest(SceneManifestBuilder builder) {
            foreach(var package in m_Packages) {
                builder.Assets.Add(package);
                foreach(var evt in package.Events()) {
                    if (evt.Mode() != AudioEvent.PlaybackMode.Stream) {
                        builder.Assets.Add(evt);
                    }
                }
            }
        }

        #endif // UNITY_EDITOR
    }
}