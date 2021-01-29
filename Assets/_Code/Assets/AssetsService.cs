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

        #endregion // Inspector

        public ActDB Acts { get { return m_Acts; } }
        public JobDB Jobs { get { return m_Jobs; } }
        public BestiaryDB Bestiary { get { return m_Bestiary; } }
    }
}