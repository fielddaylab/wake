using System;
using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace ProtoAqua.Energy
{
    public class RulesConfigPanel : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Button m_RandomizeButton = null;

        [SerializeField] private ConfigPropertyBox m_Properties;

        #endregion // Inspector

        [NonSerialized] private ISimDatabase m_Database;

        #region Unity Events

        private void Awake()
        {
            m_RandomizeButton.onClick.AddListener(Randomize);
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
                            Increment = reqVar.ConfigSettings().Increment,
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
                            Increment = reqVar.ConfigSettings().Increment,
                            WholeNumbers = true,

                            Get = () => resourceSettings.ProducingResources[cachedIdx].BaseValue,
                            Set = (v) => { resourceSettings.ProducingResources[cachedIdx].BaseValue = (ushort) v; Dirty(); }
                        };
                        
                        m_Properties.Spinner(reqConfig);
                    }
                }
                m_Properties.EndGroup();

                m_Properties.BeginGroup("Eating");
                {
                    // eating
                    var eatSettings = inType.EatSettings();
                    ConfigPropertySpinner.Configuration baseEatConfig = new ConfigPropertySpinner.Configuration()
                    {
                        Name = "Base Bite Size",

                        Min = 1,
                        Max = 100,
                        Increment = 1,
                        WholeNumbers = true,

                        Get = () => eatSettings.BaseEatSize,
                        Set = (v) => { eatSettings.BaseEatSize = (ushort) v; Dirty(); }
                    };
                    m_Properties.Spinner(baseEatConfig);

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
                }
                m_Properties.EndGroup();

                m_Properties.BeginGroup("Growth");
                {
                    // growth
                    var growthSettings = inType.GrowthSettings();

                    ConfigPropertySpinner.Configuration minGrowthConfig = new ConfigPropertySpinner.Configuration()
                    {
                        Name = "Min Growth",

                        Min = 0,
                        Max = 64,
                        Increment = 1,
                        WholeNumbers = true,

                        Get = () => growthSettings.MinGrowth,
                        Set = (v) => { growthSettings.MinGrowth = (ushort) v; Dirty(); }
                    };
                    m_Properties.Spinner(minGrowthConfig);

                    if (growthSettings.ImprovedGrowthPropertyThresholds.Length > 0 || growthSettings.ImprovedGrowthResourceThresholds.Length > 0)
                    {
                        ConfigPropertySpinner.Configuration improvedGrowthConfig = new ConfigPropertySpinner.Configuration()
                        {
                            Name = "Improved Growth",

                            Min = 0,
                            Max = 128,
                            Increment = 1,
                            WholeNumbers = true,

                            Get = () => growthSettings.ImprovedGrowth,
                            Set = (v) => { growthSettings.ImprovedGrowth = (ushort) v; Dirty(); }
                        };
                        m_Properties.Spinner(improvedGrowthConfig);
                    }

                    ConfigPropertySpinner.Configuration frequencyConfig = new ConfigPropertySpinner.Configuration()
                    {
                        Name = "Frequency",

                        Min = 0,
                        Max = 64,
                        Increment = 1,
                        WholeNumbers = true,

                        Get = () => growthSettings.Frequency,
                        Set = (v) => { growthSettings.Frequency = (ushort) v; Dirty(); }
                    };
                    m_Properties.Spinner(frequencyConfig);
                }
                m_Properties.EndGroup();

                m_Properties.BeginGroup("Reproduction");
                {
                    // repro
                    var reproSettings = inType.ReproductionSettings();
                    
                    ConfigPropertySpinner.Configuration frequencyConfig = new ConfigPropertySpinner.Configuration()
                    {
                        Name = "Frequency",

                        Min = 0,
                        Max = 64,
                        Increment = 1,
                        WholeNumbers = true,

                        Get = () => reproSettings.Frequency,
                        Set = (v) => { reproSettings.Frequency = (ushort) v; Dirty(); }
                    };
                    m_Properties.Spinner(frequencyConfig);

                    ConfigPropertySpinner.Configuration childConfig = new ConfigPropertySpinner.Configuration()
                    {
                        Name = "Child Count",

                        Min = 0,
                        Max = 64,
                        Increment = 1,
                        WholeNumbers = true,

                        Get = () => reproSettings.Count,
                        Set = (v) => { reproSettings.Count = (ushort) v; Dirty(); }
                    };
                    m_Properties.Spinner(childConfig);
                }
                m_Properties.EndGroup();

                m_Properties.BeginGroup("Death");
                {
                    // death
                    var deathSettings = inType.DeathSettings();

                    ConfigPropertySpinner.Configuration ageConfig = new ConfigPropertySpinner.Configuration()
                    {
                        Name = "Age",

                        Min = 0,
                        Max = 64,
                        Increment = 1,
                        WholeNumbers = true,

                        Get = () => deathSettings.Age,
                        Set = (v) => { deathSettings.Age = (ushort) v; Dirty(); }
                    };
                    m_Properties.Spinner(ageConfig);
                }
                m_Properties.EndGroup();
            }
            m_Properties.EndGroup();
        }

        private void Randomize()
        {
            RandomizeDatabase(m_Database);
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

        static public void RandomizeDatabase(ISimDatabase inDatabase)
        {
            foreach(var type in inDatabase.Actors.Types())
            {
                RandomizeActorType(type, inDatabase);
            }
        }

        static private void RandomizeActorType(ActorType inType, ISimDatabase inDatabase)
        {
            // resources
            var resourceSettings = inType.Requirements();
            for(int i = 0; i < resourceSettings.DesiredResources.Length; ++i)
            {
                resourceSettings.DesiredResources[i].BaseValue = (ushort) RNG.Instance.Next(1, 16);
            }
            for(int i = 0; i < resourceSettings.ProducingResources.Length; ++i)
            {
                resourceSettings.ProducingResources[i].BaseValue = (ushort) RNG.Instance.Next(1, 16);
            }

            // eating
            var eatSettings = inType.EatSettings();
            for(int i = 0; i < eatSettings.EdibleActors.Length; ++i)
            {
                eatSettings.EdibleActors[i].ConversionRate = RNG.Instance.NextFloat(0.5f, 2f);
            }
            eatSettings.BaseEatSize = (ushort) RNG.Instance.Next(1, 16);

            // growth
            var growthSettings = inType.GrowthSettings();
            growthSettings.ImprovedGrowth = (ushort) RNG.Instance.Next(16, 32);
            growthSettings.MinGrowth = (ushort) RNG.Instance.Next(1, 16);
            growthSettings.Frequency = (ushort) RNG.Instance.Next(1, 4);

            // repro
            var reproSettings = inType.ReproductionSettings();
            reproSettings.Frequency = (ushort) RNG.Instance.Next(0, 8);
            reproSettings.Count = (ushort) RNG.Instance.Next(1, 4);

            // death
            var deathSettings = inType.DeathSettings();
            deathSettings.Age = (ushort) RNG.Instance.Next(4, 10);
        }
    }
}