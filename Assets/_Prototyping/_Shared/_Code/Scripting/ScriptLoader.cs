using BeauUtil;
using BeauUtil.Blocks;
using UnityEngine;
using ProtoAqua.Scripting;

namespace ProtoAqua.Scripting
{
    public class ScriptLoader : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private TextAsset[] m_ScriptFiles = null;

        #endregion // Inspector

        private ScriptNodePackage[] m_LoadedPackages;

        private void OnEnable()
        {
            ScriptNodePackage.Generator generator = new ScriptNodePackage.Generator();
            m_LoadedPackages = new ScriptNodePackage[m_ScriptFiles.Length];
            for(int i = 0; i < m_ScriptFiles.Length; ++i)
            {
                TextAsset textFile = m_ScriptFiles[i];
                ScriptNodePackage package = BlockParser.Parse(textFile.name, textFile.text, BlockParsingRules.Default, generator);
                m_LoadedPackages[i] = package;
                Services.Script.Load(package);
            }
        }

        private void OnDisable()
        {
            if (Services.Script && m_LoadedPackages != null)
            {
                for(int i = 0; i < m_LoadedPackages.Length; ++i)
                {
                    Services.Script.Unload(m_LoadedPackages[i]);
                }
            }

            ArrayUtils.Dispose(ref m_LoadedPackages);
        }
    }
}