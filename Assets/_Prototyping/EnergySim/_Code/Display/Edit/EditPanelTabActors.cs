using System;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProtoCP;

namespace ProtoAqua.Energy
{
    public class EditPanelTabActors : EditPanelTab
    {
        protected override void Populate()
        {
            m_PropertyBox.BeginControls();
            {
                {
                    CPSpinner.Configuration durationConfig = new CPSpinner.Configuration()
                    {
                        Name = "Duration",

                        Suffix = " ticks",
                        SingularText = "1 tick",

                        Min = 1,
                        Max = 30,
                        Increment = 1,
                        WholeNumbers = true,

                        Get = () => m_Scenario.Data.Duration,
                        Set = (v) => { m_Scenario.Data.Duration = (ushort)v; DirtyScenario(); }
                    };
                    m_PropertyBox.NumberSpinner("duration", durationConfig);

                    CPSpinner.Configuration randomConfig = new CPSpinner.Configuration()
                    {
                        Name = "Random Seed",
                        Min = 0,
                        Max = ushort.MaxValue,
                        Increment = 1,
                        WholeNumbers = true,

                        Get = () => m_Scenario.Data.Seed,
                        Set = (v) => { m_Scenario.Data.Seed = (ushort)v; DirtyScenario(); }
                    };

                    m_PropertyBox.NumberSpinner("randomSeed", randomConfig);
                }

                m_PropertyBox.BeginGroup("actors", "Actors");
                {
                    for (int i = 0; i < m_Scenario.Data.InitialActors.Length; ++i)
                    {
                        int cachedIdx = i;
                        ActorCount count = m_Scenario.Data.InitialActors[cachedIdx];
                        ActorType actorType = m_Database.Actors[count.Id];
                        ActorType.ConfigRange config = actorType.ConfigSettings();

                        CPSpinner.Configuration spinnerConfig = new CPSpinner.Configuration()
                        {
                            Name = actorType.ScriptName().ToString(),

                            Min = 0,
                            Max = config.SoftCap,
                            Increment = config.Increment,
                            WholeNumbers = true,

                            Get = () => m_Scenario.Data.InitialActors[cachedIdx].Count,
                            Set = (v) => {
                                bool bPrev = m_Scenario.Data.InitialActors[cachedIdx].Count > 0;
                                bool bNow = v > 0;
                                m_Scenario.Data.InitialActors[cachedIdx].Count = (ushort)v;
                                DirtyScenario();
                                if (bPrev != bNow)
                                    DirtyRules();
                            }
                        };

                        m_PropertyBox.NumberSpinner(actorType.Id().ToString(true), spinnerConfig);
                    }
                }
                m_PropertyBox.EndGroup();

                if (m_Scenario.Data.InitialResources.Length > 0)
                {
                    m_PropertyBox.BeginGroup("resources", "Resources");
                    {
                        for (int i = 0; i < m_Scenario.Data.InitialResources.Length; ++i)
                        {
                            int cachedIdx = i;
                            VarPair amount = m_Scenario.Data.InitialResources[cachedIdx];
                            VarType varType = m_Database.Vars[amount.Id];
                            VarType.ConfigRange config = varType.ConfigSettings();

                            CPSpinner.Configuration spinnerConfig = new CPSpinner.Configuration()
                            {
                                Name = varType.ScriptName().ToString(),

                                Min = config.Min,
                                Max = config.Max,
                                Increment = config.Increment,
                                WholeNumbers = true,

                                Get = () => m_Scenario.Data.InitialResources[cachedIdx].Value,
                                Set = (v) => { m_Scenario.Data.InitialResources[cachedIdx].Value = (short)v; DirtyScenario(); }
                            };

                            m_PropertyBox.NumberSpinner(varType.Id().ToString(true), spinnerConfig);
                        }
                    }
                    m_PropertyBox.EndGroup();
                }

                if (m_Scenario.Data.InitialProperties.Length > 0)
                {
                    m_PropertyBox.BeginGroup("properties", "Properties");
                    {
                        for (int i = 0; i < m_Scenario.Data.InitialProperties.Length; ++i)
                        {
                            int cachedIdx = i;
                            VarPairF amount = m_Scenario.Data.InitialProperties[cachedIdx];
                            VarType varType = m_Database.Vars[amount.Id];
                            VarType.ConfigRange config = varType.ConfigSettings();

                            CPSpinner.Configuration spinnerConfig = new CPSpinner.Configuration()
                            {
                                Name = varType.ScriptName().ToString(),

                                Min = config.Min,
                                Max = config.Max,
                                Increment = config.Increment,
                                WholeNumbers = false,

                                Get = () => m_Scenario.Data.InitialProperties[cachedIdx].Value,
                                Set = (v) => { m_Scenario.Data.InitialProperties[cachedIdx].Value = v; DirtyScenario(); }
                            };

                            m_PropertyBox.NumberSpinner(varType.Id().ToString(true), spinnerConfig);
                        }
                    }
                    m_PropertyBox.EndGroup();
                }
            }
            m_PropertyBox.EndControls();
        }
    }
}