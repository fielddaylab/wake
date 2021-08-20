using BeauUtil;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Inventory/Inventory Database", fileName = "InventoryDB")]
    public class InventoryDB : DBObjectCollection<InvItem>, IOptimizableAsset
    {
        [SerializeField, HideInInspector] private int m_CurrencyCount;
        [SerializeField, HideInInspector] private int m_UpgradeCount;
        [SerializeField, HideInInspector] private int m_ArtifactCount;

        public ListSlice<InvItem> Currencies { get { return new ListSlice<InvItem>(m_Objects, 0, m_CurrencyCount); } }
        public ListSlice<InvItem> Upgrades { get { return new ListSlice<InvItem>(m_Objects, m_CurrencyCount, m_UpgradeCount); } }
        public ListSlice<InvItem> Artifacts { get { return new ListSlice<InvItem>(m_Objects, m_CurrencyCount + m_UpgradeCount, m_ArtifactCount); } }

        #if UNITY_EDITOR

        int IOptimizableAsset.Order { get { return 0; } }

        bool IOptimizableAsset.Optimize()
        {
            SortObjects((a, b) => a.Category().CompareTo(b.Category()));

            m_CurrencyCount = 0;
            m_UpgradeCount = 0;
            m_ArtifactCount = 0;

            foreach(var obj in m_Objects)
            {
                switch(obj.Category())
                {
                    case InvItemCategory.Currency:
                        m_CurrencyCount++;
                        break;

                    case InvItemCategory.Upgrade:
                        m_UpgradeCount++;
                        break;

                    case InvItemCategory.Artifact:
                        m_ArtifactCount++;
                        break;
                }
            }

            return true;
        }

        [UnityEditor.CustomEditor(typeof(InventoryDB))]
        private class Inspector : BaseInspector
        {}

        #endif // UNITY_EDITOR
    }
}