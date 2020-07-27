using System;
using System.Collections;
using System.Collections.Generic;
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
    public class ConfigPropertyExpandable : MonoBehaviour, IPoolAllocHandler
    {
        [Flags]
        public enum State
        {
            ExpandSelf = 0x01,
            ExpandChildren = 0x02,

            Default = ExpandSelf,
            AllExpanded = ExpandSelf | ExpandChildren
        }

        #region Inspector

        [SerializeField] private RectTransform m_RectTransform = null;
        [SerializeField] private CanvasGroup m_CanvasGroup = null;

        #endregion // Inspector

        [NonSerialized] private bool m_SelfExpanded;
        [NonSerialized] private bool m_ChildrenExpanded;

        [NonSerialized] private bool m_LastKnownParentState;
        [NonSerialized] private bool m_SelfExpandAnimState;
        [NonSerialized] private bool m_ChildExpandAnimState;

        [NonSerialized] private readonly List<ConfigPropertyExpandable> m_Children = new List<ConfigPropertyExpandable>();

        private Routine m_ExpandStateAnim;

        #region Updates

        private void PropagateState(bool inbParentState, bool inbInstant, bool inbForce)
        {
            m_LastKnownParentState = inbParentState;
            UpdateSelf(inbInstant, inbForce);
        }

        private bool UpdateSelf(bool inbInstant, bool inbForce)
        {
            bool bSelfExpanded = m_SelfExpanded && m_LastKnownParentState;
            
            if (bSelfExpanded != m_SelfExpandAnimState || inbForce)
            {
                m_SelfExpandAnimState = bSelfExpanded;
                
                if (inbInstant)
                {
                    m_ExpandStateAnim.Stop();
                    if (m_SelfExpandAnimState)
                        InstantExpand();
                    else
                        InstantCollapse();
                }
                else
                {
                    if (m_SelfExpandAnimState)
                        m_ExpandStateAnim.Replace(this, ExpandAnim()).ExecuteWhileDisabled();
                    else
                        m_ExpandStateAnim.Replace(this, CollapseAnim()).ExecuteWhileDisabled();
                }

                UpdateChildren(inbInstant, inbForce);
                return true;
            }

            return false;
        }

        private bool UpdateChildren(bool inbInstant, bool inbForce)
        {
            bool bChildrenExpanded = m_SelfExpandAnimState && m_ChildrenExpanded;

            if (bChildrenExpanded != m_ChildExpandAnimState || inbForce)
            {
                m_ChildExpandAnimState = bChildrenExpanded;
                foreach(var child in m_Children)
                {
                    child.PropagateState(m_ChildExpandAnimState, inbInstant, inbForce);
                }
                return true;
            }

            return false;
        }
    
        #endregion // Updates

        #region Animations

        private IEnumerator ExpandAnim()
        {
            m_CanvasGroup.blocksRaycasts = false;
            if (!m_CanvasGroup.gameObject.activeSelf)
            {
                m_CanvasGroup.alpha = 0;
                m_RectTransform.SetScale(0, Axis.Y);
                m_CanvasGroup.gameObject.SetActive(true);
            }

            yield return Routine.Combine(
                m_RectTransform.ScaleTo(1, 0.2f, Axis.Y).Ease(Curve.CubeOut),
                m_CanvasGroup.FadeTo(1, 0.2f)
            );

            m_CanvasGroup.blocksRaycasts = true;
        }

        private void InstantExpand()
        {
            m_CanvasGroup.gameObject.SetActive(true);
            m_CanvasGroup.alpha = 1;
            m_CanvasGroup.blocksRaycasts = true;
            m_RectTransform.SetScale(1, Axis.Y);
        }

        private IEnumerator CollapseAnim()
        {
            m_CanvasGroup.blocksRaycasts = false;
            if (m_CanvasGroup.gameObject.activeSelf)
            {
                yield return Routine.Combine(
                    m_RectTransform.ScaleTo(0, 0.2f, Axis.Y).Ease(Curve.CubeOut),
                    m_CanvasGroup.FadeTo(0, 0.2f)
                );

                m_CanvasGroup.gameObject.SetActive(false);
            }
        }

        private void InstantCollapse()
        {
            m_CanvasGroup.blocksRaycasts = false;
            m_CanvasGroup.gameObject.SetActive(false);
            m_CanvasGroup.alpha = 0;
            m_RectTransform.SetScale(0, Axis.Y);
        }

        #endregion // Animations

        #region State

        public bool SelfExpanded()
        {
            return m_SelfExpanded;
        }

        public void SetSelf(bool inbInstant = false)
        {
            m_SelfExpanded = true;
            UpdateSelf(inbInstant, false);
        }

        public void CollapseSelf(bool inbInstant = false)
        {
            m_SelfExpanded = false;
            UpdateSelf(inbInstant, false);
        }

        public bool ChildrenExpanded()
        {
            return m_ChildrenExpanded;
        }

        public void ExpandChildren(bool inbInstant = false)
        {
            m_ChildrenExpanded = true;
            UpdateChildren(inbInstant, false);
        }

        public void CollapseChildren(bool inbInstant = false)
        {
            m_ChildrenExpanded = false;
            UpdateChildren(inbInstant, false);
        }

        public State RetrieveState()
        {
            State state = 0;
            if (m_SelfExpanded)
                state |= State.ExpandSelf;
            if (m_ChildrenExpanded)
                state |= State.ExpandChildren;
            return state;
        }

        public void RestoreState(State inState)
        {
            m_SelfExpanded = (inState & State.ExpandSelf) != 0;
            m_ChildrenExpanded = (inState & State.ExpandChildren) != 0;
            UpdateSelf(true, true);
        }

        #endregion // State

        #region Children

        public void AddChild(ConfigPropertyExpandable inChild)
        {
            m_Children.Add(inChild);
        }

        #endregion // Children

        #region IPoolAllocHandler

        void IPoolAllocHandler.OnAlloc()
        {
            m_LastKnownParentState = true;
            m_SelfExpandAnimState = false;
            m_ChildExpandAnimState = false;
            m_SelfExpanded = false;
            m_ChildrenExpanded = false;
        }

        void IPoolAllocHandler.OnFree()
        {
            m_Children.Clear();
            m_LastKnownParentState = false;
            m_SelfExpandAnimState = false;
            m_ChildExpandAnimState = false;
            m_SelfExpanded = false;
            m_ChildrenExpanded = false;
            m_ExpandStateAnim.Stop();
        }

        #endregion // IPoolAllocHandler
    }
}