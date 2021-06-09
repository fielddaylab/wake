using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public class AssetsService : ServiceBehaviour
    {
        #region Inspector

        [SerializeField, Required] private ActDB m_Acts = null;
        [SerializeField, Required] private JobDB m_Jobs = null;
        [SerializeField, Required] private BestiaryDB m_Bestiary = null;
        [SerializeField, Required] private MapDB m_Map = null;
        [SerializeField, Required] private InventoryDB m_Inventory = null;
        [SerializeField, Required] private WaterPropertyDB m_WaterProperties = null;
        [SerializeField, Required] private ScriptCharacterDB m_ScriptCharacters = null; 

        #endregion // Inspector

        public ActDB Acts { get { return m_Acts; } }
        public JobDB Jobs { get { return m_Jobs; } }
        public BestiaryDB Bestiary { get { return m_Bestiary; } }
        public MapDB Map { get { return m_Map; } }
        public InventoryDB Inventory { get { return m_Inventory; } }
        public WaterPropertyDB WaterProp { get { return m_WaterProperties; } }
        public ScriptCharacterDB Characters { get { return m_ScriptCharacters; } }

        protected override void Initialize()
        {
            base.Initialize();

            m_Acts.Initialize();
            m_Jobs.Initialize();
            m_Bestiary.Initialize();
            m_Map.Initialize();
            m_Inventory.Initialize();
            m_WaterProperties.Initialize();
            m_ScriptCharacters.Initialize();
        }
    }
}