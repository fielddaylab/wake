using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public abstract class BFBase : DBObject, IComparable<BFBase>
    {
        #region Inspector

        [SerializeField] private string m_Title = null;
        [SerializeField] private string m_Description = null;
        [SerializeField] private Sprite m_Icon = null;

        #endregion // Inspector

        [NonSerialized] private BestiaryDesc m_Parent;
        [NonSerialized] private PlayerFactParams m_SelfParams;

        public BestiaryDesc Parent() { return m_Parent; }

        public string Title() { return m_Title; }
        public string Description() { return m_Description; }
        public Sprite Icon() { return m_Icon; }

        public virtual BFMode Mode() { return BFMode.Player; }

        public virtual void Accept(IFactVisitor inVisitor, PlayerFactParams inParams = null)
        {
            inVisitor.Visit(this, inParams);
        }

        public abstract IEnumerable<BestiaryFactFragment> GenerateFragments(PlayerFactParams inParams = null);
        public abstract string GenerateSentence(PlayerFactParams inParams = null);
        
        public virtual void CollectReferences(HashSet<StringHash32> outReferencedBestiary)
        {
            outReferencedBestiary.Add(m_Parent.Id());
        }

        public virtual void Hook(BestiaryDesc inParent)
        {
            m_Parent = inParent;
        }

        internal PlayerFactParams GetWrapper()
        {
            Assert.True(Mode() == BFMode.Always, "PlayerFactParams wrapper is not available for facts of type '{0}'", GetType().FullName);
            return m_SelfParams ?? (m_SelfParams = new PlayerFactParams(this));
        }

        public virtual bool HasValue()
        {
            return false;
        }

        public virtual int CompareTo(BFBase other)
        {
            int sort = GetSortingOrder().CompareTo(other.GetSortingOrder());
            if (sort == 0)
                sort = Id().CompareTo(other.Id());
            return sort;
        }

        protected virtual int GetSortingOrder() { return GetType().GetHashCode(); }

        #if UNITY_EDITOR

        protected virtual void OnValidate() { }

        #endif // UNITY_EDITOR
    }

    public enum BFMode : byte
    {
        Player = 0,
        Always,
        Internal
    }
}