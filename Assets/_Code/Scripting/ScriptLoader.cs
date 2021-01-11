using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine;
using Aqua.Scripting;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using UnityEngine.Profiling;
using BeauUtil.Debugger;
using Leaf;

namespace Aqua.Scripting
{
    public class ScriptLoader : MonoBehaviour, IScenePreloader, ISceneUnloadHandler
    {
        #region Inspector

        [SerializeField, Required] private LeafAsset[] m_Scripts = null;

        #endregion // Inspector

        private List<ScriptNodePackage> m_LoadedPackages;

        public IEnumerator OnPreloadScene(SceneBinding inScene, object inContext)
        {
            using(Profiling.Time("loading scripts"))
            {
                m_LoadedPackages = new List<ScriptNodePackage>();
                for(int i = 0; i < m_Scripts.Length; ++i)
                {
                    LeafAsset file = m_Scripts[i];
                    IEnumerator loader;
                    ScriptNodePackage package = BlockParser.ParseAsync(file.name, file.Source(), Parsing.Block, ScriptNodePackage.Generator.Instance, out loader);
                    yield return Async.Schedule(loader);
                    package.BindAsset(file);
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

                m_LoadedPackages.Clear();
                m_LoadedPackages = null;
            }
        }
    }
}