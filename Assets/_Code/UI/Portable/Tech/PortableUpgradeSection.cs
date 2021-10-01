using UnityEngine;
using BeauPools;
using Aqua.Profile;
using UnityEngine.UI;
using BeauUtil;
using BeauRoutine;
using System.Collections;

namespace Aqua
{
    public class PortableUpgradeSection : MonoBehaviour, ISceneOptimizable
    {
        #region Inspector

        [SerializeField] private GameObject m_NoUpgradesGroup = null;
        [SerializeField] private GameObject m_HasUpgradesGroup = null;
        [SerializeField, HideInInspector] private PortableUpgradeIcon[] m_UpgradeIcons = null;

        #endregion // Inspector

        public void Clear()
        {
            m_HasUpgradesGroup.SetActive(false);
            m_NoUpgradesGroup.SetActive(true);
        }

        public void Load(ListSlice<InvItem> inUpgrades)
        {
            if (inUpgrades.Length == 0)
            {
                Clear();
                return;
            }

            m_NoUpgradesGroup.SetActive(false);
            m_HasUpgradesGroup.SetActive(true);

            for(int i = 0; i < inUpgrades.Length; i++)
            {
                Populate(m_UpgradeIcons[i], inUpgrades[i]);
            }

            for(int i = inUpgrades.Length; i < m_UpgradeIcons.Length; i++)
            {
                m_UpgradeIcons[i].gameObject.SetActive(false);
            }
        }

        static private void Populate(PortableUpgradeIcon ioIcon, InvItem inInv)
        {
            ioIcon.Cursor.TooltipId = inInv.DescriptionTextId();
            ioIcon.Text.SetText(inInv.NameTextId());
            ioIcon.Icon.sprite = inInv.Icon();
            ioIcon.gameObject.SetActive(true);
        }

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            m_UpgradeIcons = m_HasUpgradesGroup.GetComponentsInChildren<PortableUpgradeIcon>(true);
        }

        #endif // UNITY_EDITOR
    }
}