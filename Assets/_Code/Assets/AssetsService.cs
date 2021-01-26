using BeauData;
using UnityEngine;

namespace Aqua
{
    public class AssetsService : ServiceBehaviour
    {
        #region Inspector

        [SerializeField] private JobDB m_Jobs = null;
        [SerializeField] private BestiaryDB m_Bestiary = null;

        [SerializeField] private InventoryDB m_Inventory = null;

        #endregion // Inspector

        public JobDB Jobs { get { return m_Jobs; } }
        public BestiaryDB Bestiary { get { return m_Bestiary; } }
        public InventoryDB Inventory { get { return m_Inventory; } }

        #region IService

        public override FourCC ServiceId()
        {
            return ServiceIds.Assets;
        }

        #endregion // IService
    }
}