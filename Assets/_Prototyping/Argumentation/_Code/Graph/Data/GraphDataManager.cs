using BeauUtil.Blocks;
using UnityEngine;

namespace ProtoAqua.Argumentation
{
    [CreateAssetMenu(menuName = "Prototype/Argumentation/Graph Data Manager")]
    public class GraphDataManager : TweakAsset
    {
        [SerializeField] private TextAsset[] m_DefaultAssets = null;

        private GraphDataPackage m_MasterPackage = null;
        private GraphDataPackage.Generator m_Generator = new GraphDataPackage.Generator();

        public GraphDataPackage MasterPackage
        {
            get { return m_MasterPackage; }
        }

        #region TweakAsset

        protected override void Apply()
        {
            foreach (var asset in m_DefaultAssets)
            {
                BlockParser.Parse(ref m_MasterPackage, asset.name, asset.text, BlockParsingRules.Default, m_Generator);
            }
        }

        #endregion // TweakAsset
    }
}
