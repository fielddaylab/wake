using System;
using System.Collections;
using System.Runtime.InteropServices;
using Aqua;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public abstract class ConfigPropertyControl : MonoBehaviour, IPoolAllocHandler
    {
        #region Inspector
        
        [SerializeField] private ConfigPropertyExpandable m_Expandable = null;
        [SerializeField] private IndentGroup m_Indent = null;

        #endregion // Inspector

        [NonSerialized] private string m_Id;

        public ConfigPropertyExpandable Expandable { get { return m_Expandable; } }
        public IndentGroup Indent { get { return m_Indent; } }

        public string Id() { return m_Id; }

        public void PreInitialize(string inId, int inIndent, ConfigPropertyControl inParent)
        {
            m_Id = inId;
            if (inParent != null)
            {
                m_Id = inParent.m_Id + m_Id;
                inParent.Expandable.AddChild(this.m_Expandable);
            }
            m_Indent.SetIndent(inIndent);
        }

        public virtual void Sync() { }

        #region IPoolAllocHandler

        protected virtual void OnAlloc()
        {
        }

        protected virtual void OnFree()
        {
            m_Indent.SetIndent(0);
        }

        void IPoolAllocHandler.OnAlloc()
        {
            OnAlloc();
        }

        void IPoolAllocHandler.OnFree()
        {
            OnFree();
        }

        #endregion // IPoolAllocHandler
    }
}