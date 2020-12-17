using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public abstract class BestiaryFactBase : DBObject
    {
        #region Inspector

        [SerializeField] private string m_Title = null;
        [SerializeField] private string m_Description = null;
        [SerializeField] private Sprite m_Icon = null;

        #endregion // Inspector

        [NonSerialized] private BestiaryDesc m_Parent;

        public BestiaryDesc Parent() { return m_Parent; }

        public string Title() { return m_Title; }
        public string Description() { return m_Description; }
        public Sprite Icon() { return m_Icon; }

        public virtual void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public abstract IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null);
        public abstract string GenerateSentence(PlayerFactParams inParams = null);

        public virtual void Hook(BestiaryDesc inParent)
        {
            m_Parent = inParent;
        }
    }
}