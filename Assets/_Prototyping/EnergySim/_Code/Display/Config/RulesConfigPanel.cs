using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauPools;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class RulesConfigPanel : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Button m_RandomizeAllButton = null;
        [SerializeField] private Button m_RandomizeEasyButton = null;
        [SerializeField] private Button m_RandomizeMediumButton = null;
        [SerializeField] private Button m_RandomizeHardButton = null;
        [SerializeField] private Button m_ResetButton = null;

        [SerializeField] private ConfigPropertyBox m_Properties;

        #endregion // Inspector

        [NonSerialized] private ISimDatabase m_Database;

        #region Unity Events

        private void Awake()
        {
            m_RandomizeAllButton.onClick.AddListener(RandomizeAll);
            m_RandomizeEasyButton.onClick.AddListener(RandomizeEasy);
            m_RandomizeMediumButton.onClick.AddListener(RandomizeMedium);
            m_RandomizeHardButton.onClick.AddListener(RandomizeHard);
            m_ResetButton.onClick.AddListener(ResetRules);
        }

        #endregion // Unity Events

        public void Populate(ISimDatabase inDatabase)
        {
            m_Database = inDatabase;

            m_Properties.Clear();

            foreach(var type in m_Database.Actors.Types())
            {
                PopulateActorType(type);
            }
        }

        private void PopulateActorType(ActorType inType)
        {
            m_Properties.BeginGroup(inType.ScriptName());
            {
                m_Properties.BeginGroup("Resources");
                {
                    // resources
                    var resourceSettings = inType.Requirements();
                    for(int i = 0; i < resourceSettings.DesiredResources.Length; ++i)
                    {
                        int cachedIdx = i;
                        var req = resourceSettings.DesiredResources[cachedIdx];
                        VarType reqVar = m_Database.Vars[req.ResourceId];
                        ConfigPropertySpinner.Configuration reqConfig = new ConfigPropertySpinner.Configuration()
                        {
                            Name = "Consume: " + reqVar.ScriptName(),

                            Min = reqVar.ConfigSettings().Min,
                            Max = reqVar.ConfigSettings().Max,
                            Increment = Mathf.Max(reqVar.ConfigSettings().Increment / 10, 1),
                            WholeNumbers = true,

                            Get = () => resourceSettings.DesiredResources[cachedIdx].BaseValue,
                            Set = (v) => { resourceSettings.DesiredResources[cachedIdx].BaseValue = (ushort) v; Dirty(); }
                        };
                        
                        m_Properties.Spinner(reqConfig);
                    }
                    for(int i = 0; i < resourceSettings.ProducingResources.Length; ++i)
                    {
                        int cachedIdx = i;
                        var req = resourceSettings.ProducingResources[cachedIdx];
                        VarType reqVar = m_Database.Vars[req.ResourceId];
                        ConfigPropertySpinner.Configuration reqConfig = new ConfigPropertySpinner.Configuration()
                        {
                            Name = "Produce: " + reqVar.ScriptName(),

                            Min = reqVar.ConfigSettings().Min,
                            Max = reqVar.ConfigSettings().Max,
                            Increment = Mathf.Max(reqVar.ConfigSettings().Increment / 10, 1),
                            WholeNumbers = true,

                            Get = () => resourceSettings.ProducingResources[cachedIdx].BaseValue,
                            Set = (v) => { resourceSettings.ProducingResources[cachedIdx].BaseValue = (ushort) v; Dirty(); }
                        };
                        
                        m_Properties.Spinner(reqConfig);
                    }
                }
                m_Properties.EndGroup();

                {
                    // eating
                    var eatSettings = inType.EatSettings();

                    if (eatSettings.EdibleActors.Length > 0)
                    {
                        m_Properties.BeginGroup("Eating");

                        for(int i = 0; i < eatSettings.EdibleActors.Length; ++i)
                        {
                            int cachedIdx = i;
                            var conversion = eatSettings.EdibleActors[i];
                            ConfigPropertySpinner.Configuration conversionConfig = new ConfigPropertySpinner.Configuration()
                            {
                                Name = "Conversion: " + m_Database.Actors[conversion.ActorType].ScriptName(),

                                Min = 0,
                                Max = 2,
                                Increment = 0.1f,
                                WholeNumbers = false,

                                Get = () => eatSettings.EdibleActors[cachedIdx].ConversionRate,
                                Set = (v) => { eatSettings.EdibleActors[cachedIdx].ConversionRate = v; Dirty(); }
                            };

                            m_Properties.Spinner(conversionConfig);
                        }

                        m_Properties.EndGroup();
                    }
                }

                m_Properties.BeginGroup("Life Cycle");
                {
                    // growth
                    var growthSettings = inType.GrowthSettings();
                    if (growthSettings.MinGrowth > 0 && growthSettings.StartingMass < growthSettings.MaxMass)
                    {
                        ConfigPropertySpinner.Configuration growthFrequencyConfig = new ConfigPropertySpinner.Configuration()
                        {
                            Name = "Growth Frequency",

                            Min = 0,
                            Max = 64,
                            Increment = 1,
                            WholeNumbers = true,

                            Get = () => growthSettings.Frequency,
                            Set = (v) => { growthSettings.Frequency = (ushort) v; Dirty(); }
                        };
                        m_Properties.Spinner(growthFrequencyConfig);
                    }

                    // repro
                    var reproSettings = inType.ReproductionSettings();
                    if (reproSettings.Count > 0)
                    {
                        ConfigPropertySpinner.Configuration reproFrequencyConfig = new ConfigPropertySpinner.Configuration()
                        {
                            Name = "Reproduction Frequency",

                            Min = 0,
                            Max = 64,
                            Increment = 1,
                            WholeNumbers = true,

                            Get = () => reproSettings.Frequency,
                            Set = (v) => { reproSettings.Frequency = (ushort) v; Dirty(); }
                        };
                        m_Properties.Spinner(reproFrequencyConfig);
                    }

                    // death
                    var deathSettings = inType.DeathSettings();

                    ConfigPropertySpinner.Configuration deathAgeConfig = new ConfigPropertySpinner.Configuration()
                    {
                        Name = "Death Age",

                        Min = 0,
                        Max = 64,
                        Increment = 1,
                        WholeNumbers = true,

                        Get = () => deathSettings.Age,
                        Set = (v) => { deathSettings.Age = (ushort) v; Dirty(); }
                    };
                    m_Properties.Spinner(deathAgeConfig);
                }
                m_Properties.EndGroup();
            }
            m_Properties.EndGroup();
        }

        private void RandomizeAll()
        {
            RandomizeDatabase(m_Database);
            m_Properties.SyncAll();
            Dirty();
        }

        private void RandomizeEasy()
        {
            RandomizeDatabase(m_Database, 1);
            m_Properties.SyncAll();
            Dirty();
        }

        private void RandomizeMedium()
        {
            RandomizeDatabase(m_Database, 2);
            m_Properties.SyncAll();
            Dirty();
        }

        private void RandomizeHard()
        {
            RandomizeDatabase(m_Database, 3);
            m_Properties.SyncAll();
            Dirty();
        }

        private void ResetRules()
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

        static public void RandomizeDatabase(ISimDatabase inDatabase, int inRulesToRandomize = -1)
        {
            inDatabase.ClearOverrides();

            if (inRulesToRandomize == 0)
            {
                return;
            }

            int idx = 0;
            ICollection<int> indices = null;

            if (inRulesToRandomize > 0)
            {
                int totalRules = CountRules(inDatabase);
                if (inRulesToRandomize < totalRules)
                {
                    indices = new HashSet<int>();
                    using(PooledList<int> allIndices = PooledList<int>.Create())
                    {
                        for(int i = totalRules - 1; i >= 0; --i)
                            allIndices.Add(i);
                        RNG.Instance.Shuffle(allIndices);

                        for(int i = 0; i < inRulesToRandomize; ++i)
                        {
                            indices.Add(allIndices[i]);
                        }
                    }
                }
            }

            foreach(var type in inDatabase.Actors.Types())
            {
                RandomizeActorType(type, inDatabase, ref idx, indices);
            }
        }

        static private void RandomizeActorType(ActorType inType, ISimDatabase inDatabase, ref int ioRuleIteratorIndex, ICollection<int> inRandomRuleIndices)
        {
            // resources
            var resourceSettings = inType.Requirements();
            for(int i = 0; i < resourceSettings.DesiredResources.Length; ++i)
            {
                if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                {
                    resourceSettings.DesiredResources[i].BaseValue = (ushort) RNG.Instance.Next(1, 16);
                }
                ++ioRuleIteratorIndex;
            }
            for(int i = 0; i < resourceSettings.ProducingResources.Length; ++i)
            {
                if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                {
                    resourceSettings.ProducingResources[i].BaseValue = (ushort) RNG.Instance.Next(1, 16);
                }
                ++ioRuleIteratorIndex;
            }

            // eating
            var eatSettings = inType.EatSettings();
            for(int i = 0; i < eatSettings.EdibleActors.Length; ++i)
            {
                if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                {
                    eatSettings.EdibleActors[i].ConversionRate = Mathf.Round(RNG.Instance.NextFloat(0f, 2f) / 0.1f) * 0.1f;
                }
                ++ioRuleIteratorIndex;
            }

            // growth
            var growthSettings = inType.GrowthSettings();
            if (growthSettings.MinGrowth > 0 && growthSettings.StartingMass < growthSettings.MaxMass)
            {
                if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                {
                    growthSettings.Frequency = (ushort) RNG.Instance.Next(1, 4);
                }
                ++ioRuleIteratorIndex;
            }

            // repro
            var reproSettings = inType.ReproductionSettings();
            if (reproSettings.Count > 0)
            {
                if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                {
                    reproSettings.Frequency = (ushort) RNG.Instance.Next(0, 8);
                }
                ++ioRuleIteratorIndex;
            }

            // death
            var deathSettings = inType.DeathSettings();
            if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
            {
                deathSettings.Age = (ushort) RNG.Instance.Next(4, 10);
            }
            ++ioRuleIteratorIndex;
        }

        static private bool ShouldRandomize(int inIndex, ICollection<int> inIndices)
        {
            return inIndices == null || inIndices.Contains(inIndex);
        }
    
        static private int CountRules(ISimDatabase inDatabase)
        {
            int totalCount = 0;
            foreach(var type in inDatabase.Actors.Types())
            {
                int countForType = 0;

                var resourceSettings = type.Requirements();
                countForType += resourceSettings.DesiredResources.Length;
                countForType += resourceSettings.ProducingResources.Length;

                var eatSettings = type.EatSettings();
                countForType += eatSettings.EdibleActors.Length;

                var growthSettings = type.GrowthSettings();
                if (growthSettings.MinGrowth > 0 && growthSettings.StartingMass < growthSettings.MaxMass)
                    countForType += 1;

                var reproSettings = type.ReproductionSettings();
                if (reproSettings.Count > 0)
                    countForType += 1;

                countForType += 1;

                totalCount += countForType;
            }
            return totalCount;
        }
    }
}