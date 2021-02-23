using BeauData;
using UnityEngine;

namespace Aqua
{
    public class AssetsService : ServiceBehaviour
    {
        #region Inspector

        [SerializeField] private ActDB m_Acts = null;
        [SerializeField] private JobDB m_Jobs = null;
        [SerializeField] private BestiaryDB m_Bestiary = null;
        [SerializeField] private MapDB m_Map = null;
        [SerializeField] private InventoryDB m_Inventory = null;
        [SerializeField] private WaterPropertyDB m_WaterProperties = null;

        #endregion // Inspector

        public ActDB Acts { get { return m_Acts; } }
        public JobDB Jobs { get { return m_Jobs; } }
        public BestiaryDB Bestiary { get { return m_Bestiary; } }
        public MapDB Map { get { return m_Map; } }
        public InventoryDB Inventory { get { return m_Inventory; } }
        public WaterPropertyDB WaterProp { get { return m_WaterProperties; } }

        protected override void Initialize()
        {
            base.Initialize();

            m_Acts.Initialize();
            m_Jobs.Initialize();
            m_Bestiary.Initialize();
            m_Map.Initialize();
            m_Inventory.Initialize();
            m_WaterProperties.Initialize();
        }
    }
}