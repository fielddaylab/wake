using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;

namespace Aqua.Portable
{
    public abstract class PortableMenuApp : BasePanel
    {
        #region Inspector

        [Header("Portable App")]
        [SerializeField] private SerializedHash32 m_Id = null;

        #endregion // Inspector

        public StringHash32 Id() { return m_Id; }

        public virtual bool TryHandle(IPortableRequest inRequest)
        {
            return false;
        }
    }
}