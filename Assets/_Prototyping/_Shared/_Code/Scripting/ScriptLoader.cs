using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine;
using ProtoAqua.Scripting;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using UnityEngine.Profiling;
using BeauUtil.Debugger;

namespace ProtoAqua.Scripting
{
    public class ScriptLoader : MonoBehaviour, IScenePreloader, ISceneUnloadHandler
    {
        #region Inspector

        [SerializeField, Required] private TextAsset[] m_ScriptFiles = null;

        #endregion // Inspector

        private List<ScriptNodePackage> m_LoadedPackages;

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext)
        {
            using(Profiling.Time("loading scripts"))
            {
                m_LoadedPackages = new List<ScriptNodePackage>();
                for(int i = 0; i < m_ScriptFiles.Length; ++i)
                {
                    TextAsset textFile = m_ScriptFiles[i];
                    IEnumerator loader;
                    ScriptNodePackage package = BlockParser.ParseAsync(textFile.name, textFile.text, Parsing.Block, ScriptNodePackage.Generator.Instance, out loader);
                    yield return Async.Schedule(loader);
                    package.BindAsset(textFile);
                    Services.Script.Load(package);
                    m_LoadedPackages.Add(package);
                }
            }
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext)
        {
            if (m_LoadedPackages != null)
            {
                foreach(var package in m_LoadedPackages)
                {
                    package.UnbindAsset();
                    Services.Script?.Unload(package);
                }
            }

            m_LoadedPackages.Clear();
            m_LoadedPackages = null;
        }
    }
}