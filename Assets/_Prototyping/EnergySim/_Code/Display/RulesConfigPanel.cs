using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauData;
using BeauPools;
using BeauUtil;
using ProtoCP;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class RulesConfigPanel : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Button m_ResetButton = null;
        [SerializeField] private Button m_SolveButton = null;

        [SerializeField] private CPRoot m_Properties = null;

        #endregion // Inspector

        [NonSerialized] private ISimDatabase m_Database;
        [NonSerialized] private ScenarioPackage m_Scenario;
        [NonSerialized] private ContentArea m_ContentMask;

        #region Unity Events

        private void Awake()
        {
            m_ResetButton.onClick.AddListener(ResetRules);
            m_SolveButton.onClick.AddListener(SolveRules);
        }

        #endregion // Unity Events

        public void Repopulate()
        {
            m_Properties.BeginControls();

            IEnumerable<ActorType> targetTypes = m_Database.Actors.GetAll(m_Scenario.Data.StartingActorIds());
            foreach (var type in targetTypes)
            {
                PopulateActorType(type);
            }

            m_Properties.EndControls();
        }

        public void Populate(ScenarioPackage inScenario, ISimDatabase inDatabase)
        {
            m_Scenario = inScenario;
            m_Database = inDatabase;
            m_ContentMask = m_Scenario.Header.ContentAreas;

            Repopulate();
        }

        private void PopulateActorType(ActorType inType)
        {
            m_Properties.BeginGroup(inType.Id().ToString(true), inType.ScriptName());
            {
                if ((m_ContentMask & ContentArea.Photosynthesis) != 0)
                {
                    m_Properties.BeginGroup("resources", "Resources");
                    {
                        var resourceSettings = inType.Requirements();

                        // resources
                        for (int i = 0; i < resourceSettings.DesiredResources.Length; ++i)
                        {
                            int cachedIdx = i;
                            var req = resourceSettings.DesiredResources[cachedIdx];
                            VarType reqVar = m_Database.Vars[req.ResourceId];
                            string propId = "consume:" + reqVar.Id().ToString(true);
                            if (!ShouldHideRule(propId))
                            {
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
                                        Set = (v) => { resourceSettings.DesiredResources[cachedIdx].BaseValue = (ushort)v; Dirty(); }
                                    };
                                    m_Properties.EnumField(propId, reqConfig)
                                        .State.SetInteractable(!ShouldLockRule(propId));
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
                                        Set = (v) => { resourceSettings.DesiredResources[cachedIdx].BaseValue = (ushort)v; Dirty(); }
                                    };
                                    m_Properties.NumberSpinner(propId, reqConfig)
                                        .State.SetInteractable(!ShouldLockRule(propId));
                                }
                            }
                        }

                        for (int i = 0; i < resourceSettings.ProducingResources.Length; ++i)
                        {
                            int cachedIdx = i;
                            var req = resourceSettings.ProducingResources[cachedIdx];
                            VarType reqVar = m_Database.Vars[req.ResourceId];
                            string propId = "produce:" + reqVar.Id().ToString(true);
                            if (!ShouldHideRule(propId))
                            {
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
                                        Set = (v) => { resourceSettings.ProducingResources[cachedIdx].BaseValue = (ushort)v; Dirty(); }
                                    };
                                    m_Properties.EnumField(propId, reqConfig)
                                        .State.SetInteractable(!ShouldLockRule(propId));
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
                                        Set = (v) => { resourceSettings.ProducingResources[cachedIdx].BaseValue = (ushort)v; Dirty(); }
                                    };
                                    m_Properties.NumberSpinner(propId, reqConfig)
                                        .State.SetInteractable(!ShouldLockRule(propId));
                                }
                            }
                        }
                    }
                    m_Properties.EndGroup();
                }

                if ((m_ContentMask & ContentArea.FoodWeb) != 0)
                {
                    // eating
                    var eatSettings = inType.EatSettings();

                    if (eatSettings.EdibleActors.Length > 0)
                    {
                        m_Properties.BeginGroup("eating", "Eating");

                        for (int i = 0; i < eatSettings.EdibleActors.Length; ++i)
                        {
                            int cachedIdx = i;
                            var conversion = eatSettings.EdibleActors[i];
                            ActorType actorType = m_Database.Actors[conversion.ActorType];
                            string propId = actorType.Id().ToString(true);
                            if (!ShouldHideRule(propId))
                            {
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
                                        Set = (v) => { eatSettings.EdibleActors[cachedIdx].Rate = (float) v; Dirty(); }
                                    };

                                    m_Properties.EnumField(propId, conversionConfig)
                                        .State.SetInteractable(!ShouldLockRule(propId));
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
                                        Set = (v) => { eatSettings.EdibleActors[cachedIdx].Rate = v; Dirty(); }
                                    };

                                    m_Properties.NumberSpinner(propId, conversionConfig)
                                        .State.SetInteractable(!ShouldLockRule(propId));
                                }
                            }
                        }

                        m_Properties.EndGroup();
                    }
                }

                m_Properties.BeginGroup("lifecycle", "Life Cycle");
                {
                    // growth
                    var growthSettings = inType.GrowthSettings();
                    if (growthSettings.MinGrowth > 0 && growthSettings.StartingMass < growthSettings.MaxMass)
                    {
                        if (!ShouldHideRule("growthInterval"))
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
                                    Set = (v) => { growthSettings.Interval = (ushort)v; Dirty(); }
                                };

                                m_Properties.EnumField("growthInterval", growthFrequencyConfig)
                                    .State.SetInteractable(!ShouldLockRule("growthInterval"));
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
                                    Set = (v) => { growthSettings.Interval = (ushort)v; Dirty(); }
                                };
                                m_Properties.NumberSpinner("growthInterval", growthFrequencyConfig)
                                    .State.SetInteractable(!ShouldLockRule("growthInterval"));
                            }
                        }
                    }

                    // repro
                    var reproSettings = inType.ReproductionSettings();
                    if (reproSettings.Count > 0)
                    {
                        if (!ShouldHideRule("reproInterval"))
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
                                    Set = (v) => { reproSettings.Interval = (ushort)v; Dirty(); }
                                };

                                m_Properties.EnumField("reproInterval", reproFrequencyConfig)
                                    .State.SetInteractable(!ShouldLockRule("reproInterval"));
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
                                    Set = (v) => { reproSettings.Interval = (ushort)v; Dirty(); }
                                };
                                m_Properties.NumberSpinner("reproInterval", reproFrequencyConfig)
                                    .State.SetInteractable(!ShouldLockRule("reproInterval"));
                            }
                        }
                    }

                    // death
                    var deathSettings = inType.DeathSettings();
                    if (deathSettings.Age > 0)
                    {
                        if (!ShouldHideRule("deathAge"))
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
                                    Set = (v) => { deathSettings.Age = (ushort)v; Dirty(); }
                                };

                                m_Properties.EnumField("deathAge", reproFrequencyConfig)
                                    .State.SetInteractable(!ShouldLockRule("deathAge"));
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
                                    Set = (v) => { deathSettings.Age = (ushort)v; Dirty(); }
                                };
                                m_Properties.NumberSpinner("deathAge", deathAgeConfig)
                                    .State.SetInteractable(!ShouldLockRule("deathAge"));
                            }
                        }
                    }
                }
                m_Properties.EndGroup();
            }
            m_Properties.EndGroup();
        }

        private void ResetRules()
        {
            m_Scenario.ApplyRules(m_Database);
            m_Properties.SyncAll();
            Dirty();
        }

        private void SolveRules()
        {
            m_Database.ClearOverrides();
            m_Properties.SyncAll();
            Dirty();
        }

        public void Clear()
        {
            m_Properties.Clear();
            m_Database = null;
        }

        private void Dirty()
        {
            m_Database?.Dirty();
        }

        private bool ShouldHideRule(string inRelativePath)
        {
            string realPath = m_Properties.CurrentPath() + "/" + inRelativePath;
            return (m_Scenario.GetRule(realPath).Flags & SerializedRuleFlags.Hidden) != 0;
        }

        private bool ShouldLockRule(string inRelativePath)
        {
            string realPath = m_Properties.CurrentPath() + "/" + inRelativePath;
            return (m_Scenario.GetRule(realPath).Flags & SerializedRuleFlags.Locked) != 0;
        }
    }
}