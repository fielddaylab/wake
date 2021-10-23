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
    public class PortableStationHeader : MonoBehaviour
    {
        [Serializable] public class Pool : SerializablePool<PortableStationHeader> { }

        #region Inspector

        public LocText Header;
        public LocText SubHeader;
        
        #endregion // Inspector
    }
}