using System;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProtoCP;

namespace ProtoAqua.Energy
{
    public class EditPanelTabText : EditPanelTab
    {
        protected override void Populate()
        {
            m_PropertyBox.BeginControls();
            {
                CPTextField.Configuration idConfig = new CPTextField.Configuration()
                {
                    Name = "Id",
                    MaxLength = 32,
                    Get = () => m_Scenario.Header.Id,
                    Set = (v) => { m_Scenario.Header.Id = v; }
                };
                m_PropertyBox.TextField("id", idConfig);

                CPTextField.Configuration nameConfig = new CPTextField.Configuration()
                {
                    Name = "Name",
                    MaxLength = -1,
                    Get = () => m_Scenario.Header.Name,
                    Set = (v) => { m_Scenario.Header.Name = v; }
                };
                m_PropertyBox.TextField("name", nameConfig);

                CPTextField.Configuration authorConfig = new CPTextField.Configuration()
                {
                    Name = "Author",
                    MaxLength = -1,
                    Get = () => m_Scenario.Header.Author,
                    Set = (v) => { m_Scenario.Header.Author = v; }
                };
                m_PropertyBox.TextField("author", authorConfig);

                CPTextField.Configuration descriptionConfig = new CPTextField.Configuration()
                {
                    Name = "Description",
                    MaxLength = -1,
                    Get = () => m_Scenario.Header.Description,
                    Set = (v) => { m_Scenario.Header.Description = v; }
                };
                m_PropertyBox.TextField("multiline", "description", descriptionConfig);

                m_PropertyBox.BeginGroup("partner", "Helper Dialogue");
                {
                    CPTextField.Configuration introConfig = new CPTextField.Configuration()
                    {
                        Name = "On Start",
                        MaxLength = -1,
                        Get = () => m_Scenario.Header.PartnerIntroQuote,
                        Set = (v) => { m_Scenario.Header.PartnerIntroQuote = v; }
                    };
                    m_PropertyBox.TextField("multiline", "intro", introConfig);

                    CPTextField.Configuration helpConfig = new CPTextField.Configuration()
                    {
                        Name = "On Help Screen",
                        MaxLength = -1,
                        Get = () => m_Scenario.Header.PartnerHelpQuote,
                        Set = (v) => { m_Scenario.Header.PartnerHelpQuote = v; }
                    };
                    m_PropertyBox.TextField("multiline", "help", helpConfig);

                    CPTextField.Configuration completeConfig = new CPTextField.Configuration()
                    {
                        Name = "On Complete",
                        MaxLength = -1,
                        Get = () => m_Scenario.Header.PartnerCompleteQuote,
                        Set = (v) => { m_Scenario.Header.PartnerCompleteQuote = v; }
                    };
                    m_PropertyBox.TextField("multiline", "complete", completeConfig);
                }
                m_PropertyBox.EndGroup();
            }
            m_PropertyBox.EndControls();
        }
    }
}