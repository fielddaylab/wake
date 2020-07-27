using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauData;
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
        [NonSerialized] private ScenarioPackage m_Scenario;
        [NonSerialized] private ContentArea m_ContentMask;

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

        public void Repopulate()
        {
            m_Properties.BeginControls();

            IEnumerable<ActorType> targetTypes = m_Database.Actors.GetAll(m_Scenario.Data.StartingActorIds());
            foreach(var type in targetTypes)
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
            m_Properties.BeginGroup(inType.Id().ToString(), inType.ScriptName());
            {
                if ((m_ContentMask & ContentArea.Photosynthesis) != 0)
                {
                    m_Properties.BeginGroup("resources", "Resources");
                    {
                        var resourceSettings = inType.Requirements();

                        // resources
                        for(int i = 0; i < resourceSettings.DesiredResources.Length; ++i)
                        {
                            int cachedIdx = i;
                            var req = resourceSettings.DesiredResources[cachedIdx];
                            VarType reqVar = m_Database.Vars[req.ResourceId];
                            ConfigPropertySpinner.Configuration reqConfig = new ConfigPropertySpinner.Configuration()
                            {
                                Name = "Consumes " + reqVar.ScriptName(),

                                Min = reqVar.ConfigSettings().Min,
                                Max = reqVar.ConfigSettings().Max,
                                Increment = Mathf.Max(reqVar.ConfigSettings().Increment / 10, 1),
                                WholeNumbers = true,

                                Get = () => resourceSettings.DesiredResources[cachedIdx].BaseValue,
                                Set = (v) => { resourceSettings.DesiredResources[cachedIdx].BaseValue = (ushort) v; Dirty(); }
                            };
                            
                            m_Properties.Spinner("consume:" + reqVar.Id().ToString(true), reqConfig);
                        }
                        for(int i = 0; i < resourceSettings.ProducingResources.Length; ++i)
                        {
                            int cachedIdx = i;
                            var req = resourceSettings.ProducingResources[cachedIdx];
                            VarType reqVar = m_Database.Vars[req.ResourceId];
                            ConfigPropertySpinner.Configuration reqConfig = new ConfigPropertySpinner.Configuration()
                            {
                                Name = "Produces " + reqVar.ScriptName(),

                                Min = reqVar.ConfigSettings().Min,
                                Max = reqVar.ConfigSettings().Max,
                                Increment = Mathf.Max(reqVar.ConfigSettings().Increment / 10, 1),
                                WholeNumbers = true,

                                Get = () => resourceSettings.ProducingResources[cachedIdx].BaseValue,
                                Set = (v) => { resourceSettings.ProducingResources[cachedIdx].BaseValue = (ushort) v; Dirty(); }
                            };
                            
                            m_Properties.Spinner("produce:" + reqVar.Id().ToString(true), reqConfig);
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

                        for(int i = 0; i < eatSettings.EdibleActors.Length; ++i)
                        {
                            int cachedIdx = i;
                            var conversion = eatSettings.EdibleActors[i];
                            ActorType actorType = m_Database.Actors[conversion.ActorType];
                            if (m_Scenario.Header.Qualitative)
                            {
                                ConfigPropertyEnum.Configuration conversionConfig = new ConfigPropertyEnum.Configuration()
                                {
                                    Name = "Eats How Much " + actorType.ScriptName(),

                                    Values = new LabeledValue[] { LabeledValue.Make(0, "None"), LabeledValue.Make(conversion.QualitativeSmall, "A Little"),
                                        LabeledValue.Make(conversion.QualitativeMed, "Some"), LabeledValue.Make(conversion.QualitativeHigh, "A Lot")},
                                    DefaultValue = 0,

                                    Get = () => eatSettings.EdibleActors[cachedIdx].Rate,
                                    Set = (v) => { eatSettings.EdibleActors[cachedIdx].Rate = (float) v; Dirty(); }
                                };

                                m_Properties.Enum(actorType.Id().ToString(), conversionConfig);
                            }
                            else
                            {
                                ConfigPropertySpinner.Configuration conversionConfig = new ConfigPropertySpinner.Configuration()
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

                                m_Properties.Spinner(actorType.Id().ToString(), conversionConfig);
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
                        ConfigPropertySpinner.Configuration growthFrequencyConfig = new ConfigPropertySpinner.Configuration()
                        {
                            Name = "Growth Interval",

                            Suffix = " ticks",
                            SingularText = "1 tick",

                            Min = 0,
                            Max = 64,
                            Increment = 1,
                            WholeNumbers = true,

                            Get = () => growthSettings.Interval,
                            Set = (v) => { growthSettings.Interval = (ushort) v; Dirty(); }
                        };
                        m_Properties.Spinner("growthInterval", growthFrequencyConfig);
                    }

                    // repro
                    var reproSettings = inType.ReproductionSettings();
                    if (reproSettings.Count > 0)
                    {
                        ConfigPropertySpinner.Configuration reproFrequencyConfig = new ConfigPropertySpinner.Configuration()
                        {
                            Name = "Reproduction Interval",

                            Suffix = " ticks",
                            SingularText = "1 tick",

                            Min = 0,
                            Max = 64,
                            Increment = 1,
                            WholeNumbers = true,

                            Get = () => reproSettings.Interval,
                            Set = (v) => { reproSettings.Interval = (ushort) v; Dirty(); }
                        };
                        m_Properties.Spinner("reproInterval", reproFrequencyConfig);
                    }

                    // death
                    var deathSettings = inType.DeathSettings();
                    if (deathSettings.Age > 0)
                    {
                        ConfigPropertySpinner.Configuration deathAgeConfig = new ConfigPropertySpinner.Configuration()
                        {
                            Name = "Death Age",

                            Suffix = " ticks",
                            SingularText = "1 tick",

                            Min = 1,
                            Max = 64,
                            Increment = 1,
                            WholeNumbers = true,

                            Get = () => deathSettings.Age,
                            Set = (v) => { deathSettings.Age = (ushort) v; Dirty(); }
                        };
                        m_Properties.Spinner("deathAge", deathAgeConfig);
                    }
                }
                m_Properties.EndGroup();
            }
            m_Properties.EndGroup();
        }

        private void RandomizeAll()
        {
            RandomizeDatabase(m_Database, m_ContentMask, m_Scenario.Data.StartingActorIds());
            Services.Audio.PostEvent("ImpossibleDifficulty");
            m_Properties.SyncAll();
            Dirty();
        }

        private void RandomizeEasy()
        {
            RandomizeDatabase(m_Database, m_ContentMask, m_Scenario.Data.StartingActorIds(), 1);
            m_Properties.SyncAll();
            Services.Audio.PostEvent("EasyDifficulty");
            Dirty();
        }

        private void RandomizeMedium()
        {
            RandomizeDatabase(m_Database, m_ContentMask, m_Scenario.Data.StartingActorIds(), 2);
            Services.Audio.PostEvent("MediumDifficulty");
            m_Properties.SyncAll();
            Dirty();
        }

        private void RandomizeHard()
        {
            RandomizeDatabase(m_Database, m_ContentMask, m_Scenario.Data.StartingActorIds(), 3);
            Services.Audio.PostEvent("HardDifficulty");
            m_Properties.SyncAll();
            Dirty();
        }

        private void ResetRules()
        {
            m_Database.ClearOverrides();
            Services.Audio.PostEvent("ResetRules");
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

        static public void RandomizeDatabase(ISimDatabase inDatabase, ContentArea inContentMask, IEnumerable<FourCC> inAllowedActorTypes, int inRulesToRandomize = -1)
        {
            Debug.LogFormat("[RulesConfigPanel] Randomizing {0} rules", inRulesToRandomize);

            inDatabase.ClearOverrides();

            if (inRulesToRandomize == 0)
            {
                return;
            }

            int idx = 0;
            ICollection<int> indices = null;

            if (inRulesToRandomize > 0)
            {
                int totalRules = CountRules(inDatabase, inContentMask, inAllowedActorTypes);
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

            IEnumerable<ActorType> targetTypes;
            if (inAllowedActorTypes != null)
            {
                targetTypes = inDatabase.Actors.GetAll(inAllowedActorTypes);
            }
            else
            {
                targetTypes = inDatabase.Actors.Types();
            }

            foreach(var type in targetTypes)
            {
                RandomizeActorType(type, inDatabase, inContentMask, ref idx, indices);
            }
        }

        static private void RandomizeActorType(ActorType inType, ISimDatabase inDatabase, ContentArea inContentMask, ref int ioRuleIteratorIndex, ICollection<int> inRandomRuleIndices)
        {
            if ((inContentMask & ContentArea.Photosynthesis) != 0)
            {
                // resources
                var resourceSettings = inType.Requirements();
                for(int i = 0; i < resourceSettings.DesiredResources.Length; ++i)
                {
                    if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                    {
                        ref ushort val = ref resourceSettings.DesiredResources[i].BaseValue;
                        ushort oldVal = val;
                        Randomize(ref val, 1, 16);
                        Debug.LogFormat("[RulesConfigPanel] {0}: Randomized desired resource {1} from {2} to {3}", inType.Id(), resourceSettings.DesiredResources[i].ResourceId, oldVal, val);
                    }
                    ++ioRuleIteratorIndex;
                }
                for(int i = 0; i < resourceSettings.ProducingResources.Length; ++i)
                {
                    if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                    {
                        ref ushort val = ref resourceSettings.ProducingResources[i].BaseValue;
                        ushort oldVal = val;
                        Randomize(ref val, 1, 16);
                        Debug.LogFormat("[RulesConfigPanel] {0}: Randomized producing resource {1} from {2} to {3}", inType.Id(), resourceSettings.DesiredResources[i].ResourceId, oldVal, val);
                    }
                    ++ioRuleIteratorIndex;
                }
            }

            if ((inContentMask & ContentArea.FoodWeb) != 0)
            {
                // eating
                var eatSettings = inType.EatSettings();
                for(int i = 0; i < eatSettings.EdibleActors.Length; ++i)
                {
                    if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                    {
                        ref float val = ref eatSettings.EdibleActors[i].Rate;
                        float oldVal = val;
                        Randomize(ref val, 0, 2, 0.25f);
                        Debug.LogFormat("[RulesConfigPanel] {0}: Randomized eating rate {1} from {2} to {3}", inType.Id(), eatSettings.EdibleActors[i].ActorType, oldVal, val);
                    }
                    ++ioRuleIteratorIndex;
                }
            }

            // growth
            var growthSettings = inType.GrowthSettings();
            if (growthSettings.MinGrowth > 0 && growthSettings.StartingMass < growthSettings.MaxMass)
            {
                if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                {
                    ref ushort val = ref growthSettings.Interval;
                    ushort oldVal = val;
                    Randomize(ref val, 1, 4);
                    Debug.LogFormat("[RulesConfigPanel] {0}: Randomized growth interval from {1} to {2}", inType.Id(), oldVal, val);
                }
                ++ioRuleIteratorIndex;
            }

            // repro
            var reproSettings = inType.ReproductionSettings();
            if (reproSettings.Count > 0)
            {
                if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                {
                    ref ushort val = ref reproSettings.Interval;
                    ushort oldVal = val;
                    Randomize(ref val, 1, 8);
                    Debug.LogFormat("[RulesConfigPanel] {0}: Randomized reproduction interval from {1} to {2}", inType.Id(), oldVal, val);
                }
                ++ioRuleIteratorIndex;
            }

            // death
            var deathSettings = inType.DeathSettings();
            if (deathSettings.Age > 0)
            {
                if (ShouldRandomize(ioRuleIteratorIndex, inRandomRuleIndices))
                {
                    ref ushort val = ref deathSettings.Age;
                    ushort oldVal = val;
                    Randomize(ref val, 4, 10);
                    Debug.LogFormat("[RulesConfigPanel] {0}: Randomized death age from {1} to {2}", inType.Id(), oldVal, val);
                }
                ++ioRuleIteratorIndex;
            }
        }

        static private bool ShouldRandomize(int inIndex, ICollection<int> inIndices)
        {
            return inIndices == null || inIndices.Contains(inIndex);
        }

        #region Randomization

        static private void Randomize(ref ushort ioVal, ushort inMin, ushort inMax)
        {
            ushort old = ioVal;
            while(ioVal == old)
            {
                ioVal = (ushort) RNG.Instance.Next(inMin, inMax);
            }
        }

        static private void Randomize(ref int ioVal, int inMin, int inMax)
        {
            int old = ioVal;
            while(ioVal == old)
            {
                ioVal = (int) RNG.Instance.Next(inMin, inMax);
            }
        }

        static private void Randomize(ref float ioVal, float inMin, float inMax, float inIncrement)
        {
            float old = ioVal;
            while(ioVal == old)
            {
                float val = RNG.Instance.NextFloat(inMin, inMax);
                if (inIncrement > 0)
                    val = Mathf.Round(val / inIncrement) * inIncrement;
                ioVal = val;
            }
        }

        #endregion // Randomization
    
        static private int CountRules(ISimDatabase inDatabase, ContentArea inContentMask, IEnumerable<FourCC> inAllowedActorTypes)
        {
            int totalCount = 0;
            IEnumerable<ActorType> targetTypes;
            if (inAllowedActorTypes != null)
            {
                targetTypes = inDatabase.Actors.GetAll(inAllowedActorTypes);
            }
            else
            {
                targetTypes = inDatabase.Actors.Types();
            }

            foreach(var type in targetTypes)
            {
                int countForType = 0;

                if ((inContentMask & ContentArea.Photosynthesis) != 0)
                {
                    var resourceSettings = type.Requirements();
                    countForType += resourceSettings.DesiredResources.Length;
                    countForType += resourceSettings.ProducingResources.Length;
                }

                if ((inContentMask & ContentArea.FoodWeb) != 0)
                {
                    var eatSettings = type.EatSettings();
                    countForType += eatSettings.EdibleActors.Length;
                }

                var growthSettings = type.GrowthSettings();
                if (growthSettings.MinGrowth > 0 && growthSettings.StartingMass < growthSettings.MaxMass)
                    countForType += 1;

                var reproSettings = type.ReproductionSettings();
                if (reproSettings.Count > 0)
                    countForType += 1;

                var deathSettings = type.DeathSettings();
                if (deathSettings.Age > 0)
                    countForType += 1;

                totalCount += countForType;
            }
            return totalCount;
        }
    }
}