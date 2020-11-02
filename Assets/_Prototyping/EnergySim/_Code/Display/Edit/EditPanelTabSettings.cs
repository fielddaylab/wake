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

                m_PropertyBox.BeginGroup("respirationRules", "Respiration Rules");
                {
                    CPToggle.Configuration photosynthesisConfig = new CPToggle.Configuration()
                    {
                        Name = "Display Photosynthesis Values",

                        Get = () => (m_Scenario.Header.ContentAreas & ContentArea.Photosynthesis) != 0,
                        Set = (v) => {
                            if (v) 
                            {
                                m_Scenario.Header.ContentAreas |= ContentArea.Photosynthesis;
                            } 
                            else 
                            {
                                m_Scenario.Header.ContentAreas &= ~ContentArea.Photosynthesis;
                            }

                            DirtyRules();
                        }
                    };
                    m_PropertyBox.Toggle("photosynthesis", photosynthesisConfig);

                    CPToggle.Configuration foodWebConfig = new CPToggle.Configuration()
                    {
                        Name = "Display Food Web Values",

                        Get = () => (m_Scenario.Header.ContentAreas & ContentArea.FoodWeb) != 0,
                        Set = (v) => {
                            if (v) 
                            {
                                m_Scenario.Header.ContentAreas |= ContentArea.FoodWeb;
                            } 
                            else 
                            {
                                m_Scenario.Header.ContentAreas &= ~ContentArea.FoodWeb;
                            }

                            DirtyRules();
                        }
                    };
                    m_PropertyBox.Toggle("foodWeb", foodWebConfig);

                    CPToggle.Configuration adaptationConfig = new CPToggle.Configuration()
                    {
                        Name = "Display Adaptation Values",

                        Get = () => (m_Scenario.Header.ContentAreas & ContentArea.Adaptation) != 0,
                        Set = (v) => {
                            if (v) 
                            {
                                m_Scenario.Header.ContentAreas |= ContentArea.Adaptation;
                            } 
                            else 
                            {
                                m_Scenario.Header.ContentAreas &= ~ContentArea.Adaptation;
                            }

                            DirtyRules();
                        }
                    };
                    m_PropertyBox.Toggle("adaptation", adaptationConfig);
                }
                m_PropertyBox.EndGroup();
            }
            m_PropertyBox.EndControls();
        }
    }
}