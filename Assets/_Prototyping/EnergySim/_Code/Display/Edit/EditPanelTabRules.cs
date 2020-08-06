using System;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProtoCP;
using System.Collections.Generic;

namespace ProtoAqua.Energy
{
    public class EditPanelTabRules : EditPanelTab
    {
        #region Inspector

        [SerializeField] private Button m_ResetButton = null;

        #endregion // Inspector

        [NonSerialized] private ContentArea m_ContentMask;

        private void Awake()
        {
            m_ResetButton.onClick.AddListener(ResetRules);
        }

        protected override void Populate()
        {
            m_ContentMask = m_Scenario.Header.ContentAreas;

            m_PropertyBox.BeginControls();
            IEnumerable<ActorType> targetTypes = m_Database.Actors.GetAll(m_Scenario.Data.StartingActorIds());
            foreach (var type in targetTypes)
            {
                PopulateActorType(type);
            }
            m_PropertyBox.EndControls();
        }

        private void PopulateActorType(ActorType inType)
        {
            m_PropertyBox.BeginGroup(inType.Id().ToString(true), inType.ScriptName());
            {
                if ((m_ContentMask & ContentArea.Photosynthesis) != 0)
                {
                    m_PropertyBox.BeginGroup("resources", "Resources");
                    {
                        var resourceSettings = inType.Requirements();

                        // resources
                        for (int i = 0; i < resourceSettings.DesiredResources.Length; ++i)
                        {
                            int cachedIdx = i;
                            var req = resourceSettings.DesiredResources[cachedIdx];
                            VarType reqVar = m_Database.Vars[req.ResourceId];
                            if (m_Scenario.Header.Qualitative)
                            {
                                CPEnumSpinner.Configuration reqConfig = new CPEnumSpinner.Configuration()
                                {
                                    Name = "Consumes " + reqVar.ScriptName(),

                                    Values = new CPLabeledValue[] { CPLabeledValue.Make(0, "None"), CPLabeledValue.Make(req.Qualitative.Low, "A Little"),
                                        CPLabeledValue.Make(req.Qualitative.Low, "Some"), CPLabeledValue.Make(req.Qualitative.High, "A Lot")},
                                    DefaultValue = 0,
                                    ValueType = typeof(ushort),

                                    Get = () => resourceSettings.DesiredResources[cachedIdx].BaseValue,
                                    Set = (v) => { resourceSettings.DesiredResources[cachedIdx].BaseValue = (ushort)v; DirtyDatabase(); }
                                };
                                var control = m_PropertyBox.EnumField("rule_editor", "consume:" + reqVar.Id().ToString(true), reqConfig);
                                ConfigureToggles(control, inType);
                            }
                            else
                            {
                                CPSpinner.Configuration reqConfig = new CPSpinner.Configuration()
                                {
                                    Name = "Consumes " + reqVar.ScriptName(),

                                    Min = reqVar.ConfigSettings().Min,
                                    Max = reqVar.ConfigSettings().Max,
                                    Increment = Mathf.Max(reqVar.ConfigSettings().Increment / 10, 1),
                                    WholeNumbers = true,

                                    Get = () => resourceSettings.DesiredResources[cachedIdx].BaseValue,
                                    Set = (v) => { resourceSettings.DesiredResources[cachedIdx].BaseValue = (ushort)v; DirtyDatabase(); }
                                };
                                var control = m_PropertyBox.NumberSpinner("rule_editor", "consume:" + reqVar.Id().ToString(true), reqConfig);
                                ConfigureToggles(control, inType);
                            }
                        }

                        for (int i = 0; i < resourceSettings.ProducingResources.Length; ++i)
                        {
                            int cachedIdx = i;
                            var req = resourceSettings.ProducingResources[cachedIdx];
                            VarType reqVar = m_Database.Vars[req.ResourceId];
                            if (m_Scenario.Header.Qualitative)
                            {
                                CPEnumSpinner.Configuration reqConfig = new CPEnumSpinner.Configuration()
                                {
                                    Name = "Produces " + reqVar.ScriptName(),

                                    Values = new CPLabeledValue[] { CPLabeledValue.Make(0, "None"), CPLabeledValue.Make(req.Qualitative.Low, "A Little"),
                                        CPLabeledValue.Make(req.Qualitative.Low, "Some"), CPLabeledValue.Make(req.Qualitative.High, "A Lot")},
                                    DefaultValue = 0,
                                    ValueType = typeof(ushort),

                                    Get = () => resourceSettings.ProducingResources[cachedIdx].BaseValue,
                                    Set = (v) => { resourceSettings.ProducingResources[cachedIdx].BaseValue = (ushort)v; DirtyDatabase(); }
                                };
                                var control = m_PropertyBox.EnumField("rule_editor", "produce:" + reqVar.Id().ToString(true), reqConfig);
                                ConfigureToggles(control, inType);
                            }
                            else
                            {
                                CPSpinner.Configuration reqConfig = new CPSpinner.Configuration()
                                {
                                    Name = "Produces " + reqVar.ScriptName(),

                                    Min = reqVar.ConfigSettings().Min,
                                    Max = reqVar.ConfigSettings().Max,
                                    Increment = Mathf.Max(reqVar.ConfigSettings().Increment / 10, 1),
                                    WholeNumbers = true,

                                    Get = () => resourceSettings.ProducingResources[cachedIdx].BaseValue,
                                    Set = (v) => { resourceSettings.ProducingResources[cachedIdx].BaseValue = (ushort)v; DirtyDatabase(); }
                                };
                                var control = m_PropertyBox.NumberSpinner("rule_editor", "produce:" + reqVar.Id().ToString(true), reqConfig);
                                ConfigureToggles(control, inType);
                            }
                        }
                    }
                    m_PropertyBox.EndGroup();
                }

                if ((m_ContentMask & ContentArea.FoodWeb) != 0)
                {
                    // eating
                    var eatSettings = inType.EatSettings();

                    if (eatSettings.EdibleActors.Length > 0)
                    {
                        m_PropertyBox.BeginGroup("eating", "Eating");

                        for (int i = 0; i < eatSettings.EdibleActors.Length; ++i)
                        {
                            int cachedIdx = i;
                            var conversion = eatSettings.EdibleActors[i];
                            ActorType actorType = m_Database.Actors[conversion.ActorType];
                            if (m_Scenario.Header.Qualitative)
                            {
                                CPEnumSpinner.Configuration conversionConfig = new CPEnumSpinner.Configuration()
                                {
                                    Name = "Eats How Much " + actorType.ScriptName(),

                                    Values = new CPLabeledValue[] { CPLabeledValue.Make(0f, "None"), CPLabeledValue.Make(conversion.Qualitative.Low, "A Little"),
                                        CPLabeledValue.Make(conversion.Qualitative.Medium, "Some"), CPLabeledValue.Make(conversion.Qualitative.High, "A Lot")},
                                    DefaultValue = 0f,
                                    ValueType = typeof(float),

                                    Get = () => eatSettings.EdibleActors[cachedIdx].Rate,
                                    Set = (v) => { eatSettings.EdibleActors[cachedIdx].Rate = (float) v; DirtyDatabase(); }
                                };

                                var control = m_PropertyBox.EnumField("rule_editor", actorType.Id().ToString(), conversionConfig);
                                ConfigureToggles(control, inType);
                            }
                            else
                            {
                                CPSpinner.Configuration conversionConfig = new CPSpinner.Configuration()
                                {
                                    Name = "Preference for " + actorType.ScriptName(),

                                    Min = 0,
                                    Max = 2,
                                    Increment = 0.1f,
                                    SnapToIncrement = true,
                                    WholeNumbers = false,

                                    Suffix = "x",

                                    Get = () => eatSettings.EdibleActors[cachedIdx].Rate,
                                    Set = (v) => { eatSettings.EdibleActors[cachedIdx].Rate = v; DirtyDatabase(); }
                                };

                                var control = m_PropertyBox.NumberSpinner("rule_editor", actorType.Id().ToString(), conversionConfig);
                                ConfigureToggles(control, inType);
                            }
                        }

                        m_PropertyBox.EndGroup();
                    }
                }

                m_PropertyBox.BeginGroup("lifecycle", "Life Cycle");
                {
                    // growth
                    var growthSettings = inType.GrowthSettings();
                    if (growthSettings.MinGrowth > 0 && growthSettings.StartingMass < growthSettings.MaxMass)
                    {
                        if (m_Scenario.Header.Qualitative)
                        {
                            CPEnumSpinner.Configuration growthFrequencyConfig = new CPEnumSpinner.Configuration()
                            {
                                Name = "Grows How Often",

                                Values = new CPLabeledValue[] {
                                    CPLabeledValue.Make(0, "Never"),
                                    CPLabeledValue.Make(growthSettings.QualitativeInterval.High, "Rarely"),
                                    CPLabeledValue.Make(growthSettings.QualitativeInterval.Medium, "Sometimes"),
                                    CPLabeledValue.Make(growthSettings.QualitativeInterval.Low, "Always"),
                                },
                                DefaultValue = 0,
                                ValueType = typeof(ushort),

                                Get = () => growthSettings.Interval,
                                Set = (v) => { growthSettings.Interval = (ushort)v; DirtyDatabase(); }
                            };

                            var control = m_PropertyBox.EnumField("rule_editor", "growthInterval", growthFrequencyConfig);
                            ConfigureToggles(control, inType);
                        }
                        else
                        {
                            CPSpinner.Configuration growthFrequencyConfig = new CPSpinner.Configuration()
                            {
                                Name = "Growth Interval",

                                Suffix = " ticks",
                                SingularText = "1 tick",

                                Min = 0,
                                Max = 64,
                                Increment = 1,
                                WholeNumbers = true,

                                Get = () => growthSettings.Interval,
                                Set = (v) => { growthSettings.Interval = (ushort)v; DirtyDatabase(); }
                            };
                            var control = m_PropertyBox.NumberSpinner("rule_editor", "growthInterval", growthFrequencyConfig);
                            ConfigureToggles(control, inType);
                        }
                    }

                    // repro
                    var reproSettings = inType.ReproductionSettings();
                    if (reproSettings.Count > 0)
                    {
                        if (m_Scenario.Header.Qualitative)
                        {
                            CPEnumSpinner.Configuration reproFrequencyConfig = new CPEnumSpinner.Configuration()
                            {
                                Name = "Reproduces How Often",

                                Values = new CPLabeledValue[] {
                                    CPLabeledValue.Make(0, "Never"),
                                    CPLabeledValue.Make(reproSettings.QualitativeInterval.High, "Rarely"),
                                    CPLabeledValue.Make(reproSettings.QualitativeInterval.Medium, "Sometimes"), 
                                    CPLabeledValue.Make(reproSettings.QualitativeInterval.Low, "Always"),
                                },
                                DefaultValue = 0,
                                ValueType = typeof(ushort),

                                Get = () => reproSettings.Interval,
                                Set = (v) => { reproSettings.Interval = (ushort)v; DirtyDatabase(); }
                            };

                            var control = m_PropertyBox.EnumField("rule_editor", "reproInterval", reproFrequencyConfig);
                            ConfigureToggles(control, inType);
                        }
                        else
                        {
                            CPSpinner.Configuration reproFrequencyConfig = new CPSpinner.Configuration()
                            {
                                Name = "Reproduction Interval",

                                Suffix = " ticks",
                                SingularText = "1 tick",

                                Min = 0,
                                Max = 64,
                                Increment = 1,
                                WholeNumbers = true,

                                Get = () => reproSettings.Interval,
                                Set = (v) => { reproSettings.Interval = (ushort)v; DirtyDatabase(); }
                            };
                            var control = m_PropertyBox.NumberSpinner("rule_editor", "reproInterval", reproFrequencyConfig);
                            ConfigureToggles(control, inType);
                        }
                    }

                    // death
                    var deathSettings = inType.DeathSettings();
                    if (deathSettings.Age > 0)
                    {
                        if (m_Scenario.Header.Qualitative)
                        {
                            CPEnumSpinner.Configuration reproFrequencyConfig = new CPEnumSpinner.Configuration()
                            {
                                Name = "Dies How Often",

                                Values = new CPLabeledValue[] { CPLabeledValue.Make(0, "Never"),
                                    CPLabeledValue.Make(deathSettings.QualitativeAge.High, "Rarely"),
                                    CPLabeledValue.Make(deathSettings.QualitativeAge.Medium, "Sometimes"),
                                    CPLabeledValue.Make(deathSettings.QualitativeAge.Low, "Always"),
                                },
                                DefaultValue = 0,
                                ValueType = typeof(ushort),

                                Get = () => deathSettings.Age,
                                Set = (v) => { deathSettings.Age = (ushort)v; DirtyDatabase(); }
                            };

                            var control = m_PropertyBox.EnumField("rule_editor", "deathAge", reproFrequencyConfig);
                            ConfigureToggles(control, inType);
                        }
                        else
                        {
                            CPSpinner.Configuration deathAgeConfig = new CPSpinner.Configuration()
                            {
                                Name = "Death Age",

                                Suffix = " ticks",
                                SingularText = "1 tick",

                                Min = 1,
                                Max = 64,
                                Increment = 1,
                                WholeNumbers = true,

                                Get = () => deathSettings.Age,
                                Set = (v) => { deathSettings.Age = (ushort)v; DirtyDatabase(); }
                            };
                            var control = m_PropertyBox.NumberSpinner("rule_editor", "deathAge", deathAgeConfig);
                            ConfigureToggles(control, inType);
                        }
                    }
                }
                m_PropertyBox.EndGroup();
            }
            m_PropertyBox.EndGroup();
        }

        private void ConfigureToggles(CPControl inControl, ActorType inType)
        {
            var toggles = inControl.GetComponentInChildren<EditPanelRuleToggles>();
            if (toggles)
            {
                toggles.Initialize(m_Scenario, DirtyRules, inType);
            }
        }

        private void ResetRules()
        {
            m_Database.ClearOverrides();
            m_Scenario.ClearRules();
            m_PropertyBox.SyncAll();
            DirtyRules();
        }

        protected override void DirtyDatabase()
        {
            base.DirtyDatabase();
            DirtyScenario();
        }
    }
}