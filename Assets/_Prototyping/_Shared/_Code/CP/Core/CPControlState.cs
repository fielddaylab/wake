using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProtoCP
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class CPControlState : MonoBehaviour, IPoolAllocHandler
    {
        [Flags]
        public enum State
        {
            ExpandSelf = 0x01,
            ExpandChildren = 0x02,

            DefaultExpanded = ExpandSelf,
            AllExpanded = ExpandSelf | ExpandChildren
        }

        #region Inspector

        [SerializeField] private RectTransform m_RectTransform = null;
        [SerializeField] private CanvasGroup m_CanvasGroup = null;
        [SerializeField] private ColorGroup m_ColorGroup = null;

        #endregion // Inspector

        [NonSerialized] private List<CPControlState> m_Children;
        
        // Expansion
        [NonSerialized] private PropagatedProperty<bool> m_Expanded;
        [NonSerialized] private Func<bool> m_ExpandDelegate; // TODO(Autumn): Implement
        [NonSerialized] private Routine m_ExpandStateAnim;

        // Interactible
        [NonSerialized] private PropagatedProperty<bool> m_Interactable;
        [NonSerialized] private Func<bool> m_InteractableDelegate; // TODO(Autumn): Implement
        [NonSerialized] private Routine m_InteractableStateAnim;

        #region Updates

        private void PropagateExpansion(in PropagatedProperty<bool> inParent, bool inbInstant, bool inbForce)
        {
            PropagationResult result = s_Propagator.Propagate(ref m_Expanded, inParent, inbForce);
            HandleExpandPropagation(result, inbInstant, inbForce);
        }

        private void UpdateExpansion(bool inbInstant, bool inbForce)
        {
            PropagationResult result = s_Propagator.Update(ref m_Expanded, inbForce);
            HandleExpandPropagation(result, inbInstant, inbForce);
        }

        private void HandleExpandPropagation(PropagationResult inResult, bool inbInstant, bool inbForce)
        {
            if ((inResult & PropagationResult.UpdateSelf) != 0)
            {
                if (inbInstant)
                {
                    m_ExpandStateAnim.Stop();
                    if (m_Expanded.PropagatedSelfState)
                        InstantExpand();
                    else
                        InstantCollapse();
                }
                else
                {
                    m_CanvasGroup.blocksRaycasts = false;

                    if (m_Expanded.PropagatedSelfState)
                        m_ExpandStateAnim.Replace(this, ExpandAnim()).ExecuteWhileDisabled();
                    else
                        m_ExpandStateAnim.Replace(this, CollapseAnim()).ExecuteWhileDisabled();
                }
            }

            if ((inResult & PropagationResult.UpdateChildren) != 0)
            {
                if (m_Children != null && m_Children.Count > 0)
                {
                    foreach(var child in m_Children)
                    {
                        child.PropagateExpansion(m_Expanded, inbInstant, inbForce);
                    }
                }
            }
        }

        private void PropagateInteractable(in PropagatedProperty<bool> inParent, bool inbForce)
        {
            PropagationResult result = s_Propagator.Propagate(ref m_Interactable, inParent, inbForce);
            HandleInteractablePropagation(result, inbForce);
        }

        private void UpdateInteractable(bool inbForce)
        {
            PropagationResult result = s_Propagator.Update(ref m_Interactable, inbForce);
            HandleInteractablePropagation(result, inbForce);
        }

        private void HandleInteractablePropagation(PropagationResult inResult, bool inbForce)
        {
            if ((inResult & PropagationResult.UpdateSelf) != 0)
            {
                m_CanvasGroup.interactable = m_Interactable.PropagatedSelfState;
                m_CanvasGroup.alpha = m_CanvasGroup.interactable ? 1 : 0.5f;
            }

            if ((inResult & PropagationResult.UpdateChildren) != 0)
            {
                if (m_Children != null && m_Children.Count > 0)
                {
                    foreach(var child in m_Children)
                    {
                        child.PropagateInteractable(m_Expanded, inbForce);
                    }
                }
            }
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

        #region Expanded State

        public bool SelfExpanded()
        {
            return m_Expanded.Self;
        }

        public void SetSelf(bool inbInstant = false)
        {
            m_Expanded.Self = true;
            UpdateExpansion(inbInstant, false);
        }

        public void CollapseSelf(bool inbInstant = false)
        {
            m_Expanded.Self = false;
            UpdateExpansion(inbInstant, false);
        }

        public bool ChildrenExpanded()
        {
            return m_Expanded.Children;
        }

        public void ExpandChildren(bool inbInstant = false)
        {
            m_Expanded.Children = true;
            UpdateExpansion(inbInstant, false);
        }

        public void CollapseChildren(bool inbInstant = false)
        {
            m_Expanded.Children = false;
            UpdateExpansion(inbInstant, false);
        }

        #endregion // State

        #region Interactable

        public bool Interactable()
        {
            return m_Interactable.Self;
        }

        public void SetInteractable(bool inbInteractable)
        {
            m_Interactable.Self = inbInteractable;
            UpdateInteractable(false);
        }

        #endregion // Interactables

        #region Children

        public void AddChild(CPControlState inChild)
        {
            if (m_Children == null)
            {
                m_Children = new List<CPControlState>();
            }

            m_Children.Add(inChild);
        }

        #endregion // Children

        #region Storage

        public State RetrieveState()
        {
            State state = 0;
            if (m_Expanded.Self)
                state |= State.ExpandSelf;
            if (m_Expanded.Children)
                state |= State.ExpandChildren;
            return state;
        }

        public void RestoreState(State inState)
        {
            m_Expanded.Self = (inState & State.ExpandSelf) != 0;
            m_Expanded.Children = (inState & State.ExpandChildren) != 0;
            m_Interactable.Children = true;

            UpdateExpansion(true, true);
            UpdateInteractable(true);
        }

        #endregion // Storage

        #region IPoolAllocHandler

        void IPoolAllocHandler.OnAlloc()
        {
            s_Propagator.Reset(ref m_Expanded, true);
            s_Propagator.Reset(ref m_Interactable, true);
        }

        void IPoolAllocHandler.OnFree()
        {
            m_Children?.Clear();
            s_Propagator.Reset(ref m_Expanded, false);
            s_Propagator.Reset(ref m_Interactable, false);
            m_ExpandStateAnim.Stop();
        }

        #endregion // IPoolAllocHandler
    
        private void Reset()
        {
            m_RectTransform = (RectTransform) transform;
            m_CanvasGroup = GetComponent<CanvasGroup>();
            m_ColorGroup = GetComponent<ColorGroup>();
        }
    
        static private readonly PropertyPropagator<bool> s_Propagator = new BoolPropagator(true, true);
    }
}