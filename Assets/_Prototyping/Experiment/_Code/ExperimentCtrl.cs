using UnityEngine;
using BeauUtil;
using BeauUtil.Variants;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class ExperimentCtrl : MonoBehaviour, ISceneLoadHandler, ISceneUnloadHandler
    {
        static public readonly StringHash32 TableId = "experiment";

        #region Inspector

        [SerializeField] private ExperimentSettings m_Settings = null;

        #endregion // Inspector

        private VariantTable m_VarTable;

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            m_VarTable = new VariantTable("experiment");
            Services.Data.BindTable(TableId, m_VarTable);

            Services.Audio.SetMusic("ExperimentBGM", 2);

            Services.Tweaks.Load(m_Settings);
        }

        public void OnSceneUnload(SceneBinding inScene, object inContext)
        {
            Services.Data.UnbindTable(TableId);
            Ref.Dispose(ref m_VarTable);

            Services.Tweaks.Unload(m_Settings);
        }
    }
}