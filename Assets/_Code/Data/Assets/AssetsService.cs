using BeauData;
using BeauUtil;
using TMPro;
using UnityEngine;

namespace Aqua
{
    public class AssetsService : ServiceBehaviour
    {
        #region Inspector

        [Header("Databases")]
        [SerializeField, Required] private ActDB m_Acts = null;
        [SerializeField, Required] private JobDB m_Jobs = null;
        [SerializeField, Required] private BestiaryDB m_Bestiary = null;
        [SerializeField, Required] private MapDB m_Map = null;
        [SerializeField, Required] private InventoryDB m_Inventory = null;
        [SerializeField, Required] private WaterPropertyDB m_WaterProperties = null;
        [SerializeField, Required] private ScriptCharacterDB m_ScriptCharacters = null; 

        [Header("Fonts")]
        [SerializeField, Required] private TMP_FontAsset m_RegularFont = null;
        [SerializeField, Required] private TMP_FontAsset m_SemiBoldFont = null;
        [SerializeField, Required] private TMP_FontAsset m_BoldFont = null;

        #endregion // Inspector

        public ActDB Acts { get { return m_Acts; } }
        public JobDB Jobs { get { return m_Jobs; } }
        public BestiaryDB Bestiary { get { return m_Bestiary; } }
        public MapDB Map { get { return m_Map; } }
        public InventoryDB Inventory { get { return m_Inventory; } }
        public WaterPropertyDB WaterProp { get { return m_WaterProperties; } }
        public ScriptCharacterDB Characters { get { return m_ScriptCharacters; } }

        public TMP_FontAsset RegularFont { get { return m_RegularFont; } }
        public TMP_FontAsset SemiBoldFont { get { return m_SemiBoldFont; } }
        public TMP_FontAsset BoldFont { get { return m_BoldFont; } }

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

            Assets.Assign(this);
        }

        protected override void Shutdown()
        {
            Streaming.UnloadAll();

            base.Shutdown();
        }
    }
}