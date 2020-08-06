using System;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProtoCP;

namespace ProtoAqua.Energy
{
    public class EditPanelTabSettings : EditPanelTab
    {
        protected override void Populate()
        {
            m_PropertyBox.BeginControls();
            {
                CPToggle.Configuration qualitativeConfig = new CPToggle.Configuration()
                {
                    Name = "Use Qualitative Values",

                    Get = () => m_Scenario.Header.Qualitative,
                    Set = (v) => { m_Scenario.Header.Qualitative = v; DirtyRules(); }
                };
                m_PropertyBox.Toggle("qualitative", qualitativeConfig);
            }
            m_PropertyBox.EndControls();
        }
    }
}