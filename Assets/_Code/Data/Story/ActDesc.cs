using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Story/Act Description", fileName = "NewActDesc")]
    public class ActDesc : DBObject
    {
        #region Inspector

        [Header("Assets")]
        [SerializeField] private LeafAsset m_Scripting = null;

        #endregion // Inspector

        public LeafAsset Scripting() { return m_Scripting; }
    }
}