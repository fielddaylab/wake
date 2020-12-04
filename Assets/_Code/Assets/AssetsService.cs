using BeauData;
using UnityEngine;

namespace Aqua
{
    public class AssetsService : ServiceBehaviour
    {
        #region Inspector

        [SerializeField] private JobDB m_Jobs = null;
        [SerializeField] private BestiaryDB m_Bestiary = null;

        #endregion // Inspector

        public JobDB Jobs { get { return m_Jobs; } }
        public BestiaryDB Bestiary { get { return m_Bestiary; } }

        #region IService

        public override FourCC ServiceId()
        {
            return ServiceIds.Assets;
        }

        #endregion // IService
    }
}