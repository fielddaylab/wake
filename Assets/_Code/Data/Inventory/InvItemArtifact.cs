using System;
using System.Collections.Generic;
using BeauUtil;
using Leaf;
using UnityEngine;

namespace Aqua
{
    [CreateAssetMenu(menuName = "Aqualab/Inventory/Inventory Item Artifact", fileName = "NewInvItemArtifact")]
    public class InvItemArtifact : InvItem
    {
        #region Inspector

        [Header("Models")]

        [SerializeField] private Artifact[] m_Artifacts = null;

        #endregion


        public List<Artifact> Models() { return new List<Artifact>(m_Artifacts); }
    }
}