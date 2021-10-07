using UnityEngine;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using BeauPools;
using System;
using Aqua;

namespace Aqua.Portable
{
    public class PortableListHeader : MonoBehaviour
    {
        [Serializable] public class Pool : SerializablePool<PortableListHeader> { }

        #region Inspector

        public LocText Header;
        public LocText SubHeader;
        
        #endregion // Inspector
    }
}