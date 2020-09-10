using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoCP
{
    [RequireComponent(typeof(CPControlState))]
    [DisallowMultipleComponent]
    public abstract class CPControl : UIBehaviour, IPooledObject<CPControl>
    {
        #region Inspector

        [UnityEngine.Serialization.FormerlySerializedAs("m_Expandable")]
        [SerializeField] private CPControlState m_State = null;
        [SerializeField] private CPIndent m_Indent = null;
        
        [Header("Style")]

        [SerializeField] private string m_VariantId = null;

        #endregion // Inspector

        [NonSerialized] private string m_Id;
        [NonSerialized] private IPool<CPControl> m_Pool;
        [NonSerialized] private CPStyle m_Style;

        [NonSerialized] private List<CPControl> m_Children;

        [NonSerialized] private CPControl m_Parent;
        [NonSerialized] private int m_Depth;

        public string Id() { return m_Id; }
        public abstract FourCC Type();
        public string VariantId() { return m_VariantId; }

        public CPControlState State { get { return m_State; } }

        public event Action OnUpdate;
        public event Action OnSync;

        public void PreInitialize(string inId, CPControl inParent = null)
        {
            m_Id = inId;

            if (inParent != null)
            {
                m_Parent = inParent;
                m_Depth = inParent.m_Depth + 1;
                m_Id = string.Format("{0}/{1}", inParent.m_Id, m_Id);

                inParent.AddChild(this);
            }
            else
            {
                m_Depth = 0;
            }

            m_Indent.SetIndent(m_Depth * m_Style.IndentSize());
        }

        public virtual void Sync()
        {
            OnSync?.Invoke();
        }

        protected void InvokeUpdate()
        {
            OnUpdate?.Invoke();
        }

        #region Pool

        public void Recycle()
        {
            if (m_Parent != null)
            {
                m_Parent.RemoveChild(this);
            }
            
            m_Pool.Free(this);
        }

        #endregion // Pool

        #region Children

        public ListSlice<CPControl> Children { get { return new ListSlice<CPControl>(m_Children); } }

        private int AddChild(CPControl inChild)
        {
            if (m_Children == null)
                m_Children = new List<CPControl>();

            int currentIdx = m_Children.IndexOf(inChild);
            if (currentIdx < 0)
            {
                currentIdx = m_Children.Count;
                m_Children.Add(inChild);
                m_State.AddChild(inChild.m_State);
            }

            return currentIdx;
        }

        private void RemoveChild(CPControl inChild)
        {
            if (m_Children != null)
            {
                m_Children.Remove(inChild);
            }
        }

        #endregion // Children

        #region IPooledObject

        protected virtual void OnConstruct() { }
        protected virtual void OnDestruct() { }

        protected virtual void OnAlloc() { }
        protected virtual void OnFree() { }

        void IPooledObject<CPControl>.OnConstruct(IPool<CPControl> inPool)
        {
            m_Pool = inPool;
            OnConstruct();
        }

        void IPooledObject<CPControl>.OnDestruct()
        {
            m_Style = null;
            OnDestruct();
        }

        void IPooledObject<CPControl>.OnAlloc()
        {
            OnAlloc();
        }

        void IPooledObject<CPControl>.OnFree()
        {
            m_Indent.SetIndent(0);
            m_Children?.Clear();
            m_Parent = null;
            m_Style = null;

            OnFree();
        }

        internal void SetStyle(CPStyle inStyle)
        {
            m_Style = inStyle;
        }

        #endregion // IPooledObject

        #if UNITY_EDITOR

        protected override void Reset()
        {
            base.Reset();

            m_State = GetComponent<CPControlState>();
            m_Indent = GetComponentInChildren<CPIndent>();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            m_Indent = GetComponentInChildren<CPIndent>();
        }

        #endif // UNITY_EDITOR
    }
}