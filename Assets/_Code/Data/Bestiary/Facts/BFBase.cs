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

        [SerializeField] private Sprite m_Icon = null;
        [SerializeField, HideInInspector] private BestiaryDesc m_Parent;

        #endregion // Inspector

        public BestiaryDesc Parent() { return m_Parent; }
        public Sprite Icon() { return !m_Icon ? DefaultIcon() : m_Icon; }
        public virtual BFMode Mode() { return BFMode.Player; }

        public virtual Sprite GraphIcon() { return null; }
        protected virtual Sprite DefaultIcon() { return GraphIcon(); }

        public virtual void Accept(IFactVisitor inVisitor)
        {
            inVisitor.Visit(this);
        }

        public abstract string GenerateSentence();
        
        public virtual void CollectReferences(HashSet<StringHash32> outReferencedBestiary)
        {
            outReferencedBestiary.Add(m_Parent.Id());
        }

        public virtual BFDiscoveredFlags DefaultInformationFlags()
        {
            return BFDiscoveredFlags.All;
        }

        #region Sorting

        public virtual int CompareTo(BFBase other)
        {
            int sort = GetSortingOrder().CompareTo(other.GetSortingOrder());
            if (sort == 0)
                sort = Id().CompareTo(other.Id());
            return sort;
        }

        internal virtual int GetSortingOrder() { return GetType().GetHashCode(); }

        #endregion // Sorting

        #region Editor

        #if UNITY_EDITOR

        internal void SetParent(BestiaryDesc inParent)
        {
            if (Ref.Replace(ref m_Parent, inParent))
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        protected virtual void OnValidate() { }

        #endif // UNITY_EDITOR

        #endregion // Editor

        #region Utils

        static protected WaterPropertyDesc Property(WaterPropertyId inId)
        {
            return Services.Assets.WaterProp.Property(inId);
        }

        #endregion // Utils
    }

    public enum BFMode : byte
    {
        Player = 0,
        Always,
        Internal
    }
}