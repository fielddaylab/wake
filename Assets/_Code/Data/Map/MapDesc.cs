using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Map/Map Description", fileName = "NewMapDesc")]
    public class MapDesc : DBObject
    {
        #region Inspector

        [Header("Assets")]
        [SerializeField] private string m_SceneName = null;

        #endregion // Inspector

        public string SceneName() { return m_SceneName; }
    }
}