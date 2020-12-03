using System.Collections.Generic;
using UnityEngine;

namespace Aqua
{
    public abstract class BestiaryFactBase : DBObject
    {
        #region Inspector

        [SerializeField] private Sprite m_Icon = null;

        #endregion // Inspector

        public Sprite Icon() { return m_Icon; }

        public virtual void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public abstract IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null);
    }
}