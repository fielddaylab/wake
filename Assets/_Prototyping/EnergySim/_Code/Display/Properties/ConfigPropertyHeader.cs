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
    public class ConfigPropertyHeader : ConfigPropertyControl
    {
        #region Inspector
        
        [SerializeField] private TMP_Text m_Label = null;
        [SerializeField] private Button m_CollapseButton = null;

        #endregion // Inspector

        private Routine m_ButtonAnimateRoutine;

        private void Awake()
        {
            m_CollapseButton.onClick.AddListener(OnCollapseClicked);
        }

        public void Configure(string inLabel)
        {
            m_Label.SetText(inLabel);
        }

        private void OnCollapseClicked()
        {
            if (Expandable.ChildrenExpanded())
            {
                Expandable.CollapseChildren();
                m_ButtonAnimateRoutine.Replace(this, m_CollapseButton.transform.RotateTo(0, 0.2f, Axis.Z, Space.Self).Ease(Curve.CubeOut)).ExecuteWhileDisabled();
            }
            else
            {
                Expandable.ExpandChildren();
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

            m_CollapseButton.transform.SetRotation(Expandable.ChildrenExpanded() ? -90 : 0, Axis.Z, Space.Self);
        }
    }
}