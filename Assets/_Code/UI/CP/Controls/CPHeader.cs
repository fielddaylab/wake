using System;
using System.Collections;
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
    public class CPHeader : CPControl
    {
        #region Inspector
        
        [SerializeField] private TMP_Text m_Label = null;
        [SerializeField] private Button m_CollapseButton = null;

        #endregion // Inspector

        private Routine m_ButtonAnimateRoutine;

        protected override void OnConstruct()
        {
            base.OnConstruct();

            if (m_CollapseButton != null)
            {
                m_CollapseButton.onClick.AddListener(OnCollapseClicked);
            }
        }

        public void Configure(string inLabel)
        {
            m_Label.SetText(inLabel);
        }

        private void OnCollapseClicked()
        {
            if (State.ChildrenExpanded())
            {
                State.CollapseChildren();
                m_ButtonAnimateRoutine.Replace(this, m_CollapseButton.transform.RotateTo(0, 0.2f, Axis.Z, Space.Self).Ease(Curve.CubeOut)).ExecuteWhileDisabled();
            }
            else
            {
                State.ExpandChildren();
                m_ButtonAnimateRoutine.Replace(this, m_CollapseButton.transform.RotateTo(-90, 0.2f, Axis.Z, Space.Self).Ease(Curve.CubeOut)).ExecuteWhileDisabled();
            }
        }

        protected override void OnFree()
        {
            base.OnFree();
            
            m_Label.SetText(string.Empty);
            m_ButtonAnimateRoutine.Stop();
        }

        public override void Sync()
        {
            base.Sync();

            if (m_CollapseButton != null)
            {
                m_CollapseButton.transform.SetRotation(State.ChildrenExpanded() ? -90 : 0, Axis.Z, Space.Self);
            }
            else
            {
                if (!State.ChildrenExpanded())
                {
                    State.ExpandChildren(true);
                }
            }
        }

        public override FourCC Type()
        {
            return CPControlType.Header;
        }
    }
}