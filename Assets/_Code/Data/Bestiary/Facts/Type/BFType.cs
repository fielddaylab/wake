using System;
using System.Collections.Generic;
using System.Diagnostics;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    static public class BFType
    {
        #region Consts

        public const int TypeCount = (int) BFTypeId._COUNT;

        static private readonly BFTypeId[] TypeSortingOrder = new BFTypeId[] {
            BFTypeId.WaterProperty, BFTypeId.WaterPropertyHistory, BFTypeId.Population, BFTypeId.PopulationHistory, BFTypeId.Model,
            BFTypeId.State, BFTypeId.Parasites, BFTypeId.Eat, BFTypeId.Produce, BFTypeId.Consume, BFTypeId.Grow, BFTypeId.Reproduce, BFTypeId.Death 
        };
        static private readonly int[] TypeSortingOrderIndices = new int[TypeCount];

        static BFType()
        {
            for(int i = 0; i < TypeCount; i++)
            {
                TypeSortingOrderIndices[i] = Array.IndexOf(TypeSortingOrder, (BFTypeId) i);
                s_DefaultDiscoveredFlags[i] = BFDiscoveredFlags.All;
            }
        }

        #endregion // Consts

        #region Types

        internal delegate void CollectReferencesDelegate(BFBase inFact, HashSet<StringHash32> outCritterIds);
        internal delegate string GenerateSentenceDelegate(BFBase inFact, BFDiscoveredFlags inFlags);
        internal delegate IEnumerable<BFFragment> GenerateFragmentsDelegate(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags);
        internal delegate Sprite DefaultIconDelegate(BFBase inFact);
        internal delegate BFMode ModeDelegate(BFBase inFact);

        #endregion // Types

        static private readonly BFDiscoveredFlags[] s_DefaultDiscoveredFlags = new BFDiscoveredFlags[TypeCount];
        static private readonly BFShapeId[] s_Shapes = new BFShapeId[TypeCount];
        static private readonly BFFlags[] s_Flags = new BFFlags[TypeCount];
        static private readonly CollectReferencesDelegate[] s_CollectReferencesDelegates = new CollectReferencesDelegate[TypeCount];
        static private readonly GenerateSentenceDelegate[] s_GenerateSentenceDelegates = new GenerateSentenceDelegate[TypeCount];
        static private readonly GenerateFragmentsDelegate[] s_GenerateFragmentsDelegates = new GenerateFragmentsDelegate[TypeCount];
        static private readonly Comparison<BFBase>[] s_ComparisonDelegates = new Comparison<BFBase>[TypeCount];

        static public bool IsBehavior(BFTypeId inTypeId) {
            switch(inTypeId) {
                case BFTypeId.Consume:
                case BFTypeId.Eat:
                case BFTypeId.Death:
                case BFTypeId.Grow:
                case BFTypeId.Parasites:
                case BFTypeId.Produce:
                case BFTypeId.Reproduce:
                    return true;
                
                default:
                    return false;
            }
        }

        #region Attributes

        static public BFShapeId Shape(BFBase inFact)
        {
            return s_Shapes[(int) inFact.Type];
        }

        static public BFShapeId Shape(BFTypeId inFactType)
        {
            return s_Shapes[(int) inFactType];
        }

        static public BFFlags Flags(BFBase inFact)
        {
            return s_Flags[(int) inFact.Type];
        }

        static public BFFlags Flags(BFTypeId inFactType)
        {
            return s_Flags[(int) inFactType];
        }

        static public BFDiscoveredFlags DefaultDiscoveredFlags(BFBase inFact)
        {
            if (inFact.Parent.HasFlags(BestiaryDescFlags.Human))
                return BFDiscoveredFlags.All;
            return s_DefaultDiscoveredFlags[(int) inFact.Type];
        }

        static public BFDiscoveredFlags DefaultDiscoveredFlags(BFTypeId inFactType)
        {
            return s_DefaultDiscoveredFlags[(int) inFactType];
        }

        #endregion // Attributes

        #region Methods

        static public void CollectReferences(BFBase inFact, HashSet<StringHash32> outCritterIds)
        {
            outCritterIds.Add(inFact.Parent.Id());
            s_CollectReferencesDelegates[(int) inFact.Type]?.Invoke(inFact, outCritterIds);
        }

        static public string GenerateSentence(BFBase inFact, BFDiscoveredFlags inFlags)
        {
            return s_GenerateSentenceDelegates[(int) inFact.Type]?.Invoke(inFact, inFlags);
        }

        static public string GenerateSentence(BFBase inFact)
        {
            return GenerateSentence(inFact, s_DefaultDiscoveredFlags[(int) inFact.Type]);
        }

        static public IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BestiaryDesc inReference, BFDiscoveredFlags inFlags)
        {
            GenerateFragmentsDelegate custom = s_GenerateFragmentsDelegates[(int) inFact.Type];
            if (custom != null)
                return custom(inFact, inReference, inFlags);

            return null;
        }

        /// <summary>
        /// Sorts facts by their visual order.
        /// </summary>
        static public Comparison<BFBase> SortByVisualOrder = (x, y) =>
        {
            int compareTypeSorting = TypeSortingOrderIndices[(int) x.Type] - TypeSortingOrderIndices[(int) y.Type];
            if (compareTypeSorting != 0)
                return Math.Sign(compareTypeSorting);

            int compareTypes = x.Type - y.Type;
            if (compareTypes != 0)
                return Math.Sign(compareTypes);
            
            Comparison<BFBase> comparer = s_ComparisonDelegates[(int) x.Type];
            if (comparer != null)
            {
                int compareCustom = comparer(x, y);
                if (compareCustom != 0)
                    return compareCustom;
            }

            return x.Id.Hash().CompareTo(y.Id.Hash());
        };

        #endregion // Methods

        #region Definitions

        static internal void DefineAttributes(BFTypeId inType, BFShapeId inShape, BFFlags inFlags, BFDiscoveredFlags inDefaultDiscoveredFlags, Comparison<BFBase> inComparison)
        {
            s_Shapes[(int) inType] = inShape;
            s_Flags[(int) inType] = inFlags;
            s_DefaultDiscoveredFlags[(int) inType] = inDefaultDiscoveredFlags;
            s_ComparisonDelegates[(int) inType] = inComparison;
        }

        static internal void DefineMethods(BFTypeId inType, CollectReferencesDelegate inCollectReferences, GenerateSentenceDelegate inGenerateSentences, GenerateFragmentsDelegate inGenerateFragments)
        {
            s_CollectReferencesDelegates[(int) inType] = inCollectReferences;
            s_GenerateSentenceDelegates[(int) inType] = inGenerateSentences;
            s_GenerateFragmentsDelegates[(int) inType] = inGenerateFragments;
        }

        #endregion // Definitions

        #if UNITY_EDITOR

        static private readonly Type[] TypeToSystemTypeMapping = new Type[] {
            typeof(BFBody), typeof(BFState),
            typeof(BFEat), typeof(BFConsume), typeof(BFProduce), null,
            typeof(BFDeath), typeof(BFGrow), typeof(BFReproduce),
            typeof(BFPopulation), typeof(BFPopulationHistory),
            typeof(BFWaterProperty), typeof(BFWaterPropertyHistory),
            typeof(BFModel), typeof(BFSim)
        };

        static private readonly DefaultIconDelegate[] s_DefaultIconDelegates = new DefaultIconDelegate[TypeCount];
        static private readonly ModeDelegate[] s_ModeDelegates = new ModeDelegate[TypeCount];

        static internal void DefineEditor(BFTypeId inType, DefaultIconDelegate inIconDelegate, BFMode inMode)
        {
            s_DefaultIconDelegates[(int) inType] = inIconDelegate;
            s_ModeDelegates[(int) inType] = (b) => inMode;
        }

        static internal void DefineEditor(BFTypeId inType, DefaultIconDelegate inIconDelegate, ModeDelegate inModeDelegate)
        {
            s_DefaultIconDelegates[(int) inType] = inIconDelegate;
            s_ModeDelegates[(int) inType] = inModeDelegate;
        }

        static internal Sprite ResolveIcon(BFBase inFact, Sprite inOverride)
        {
            if (inOverride != null)
                return inOverride;
            
            Sprite icon = s_DefaultIconDelegates[(int) inFact.Type]?.Invoke(inFact);
            if (icon == null)
                icon = ValidationUtils.FindAsset<BestiaryDB>().DefaultIcon(inFact);
            return icon;
        }

        static public BFMode ResolveMode(BFBase inFact)
        {
            BFBehavior behavior = inFact as BFBehavior;
            if (behavior != null && behavior.AutoGive)
                return BFMode.Always;
            return s_ModeDelegates[(int) inFact.Type]?.Invoke(inFact) ?? BFMode.Player;
        }

        static internal Type ResolveSystemType(BFTypeId inType) {
            return TypeToSystemTypeMapping[(int) inType];
        }

        static internal BFTypeId ResolveFactType(Type inType) {
            return (BFTypeId) Array.IndexOf(TypeToSystemTypeMapping, inType);
        }

        static internal string AutoNameFact(BFBase inFact) {
            string parentName = inFact.Parent.name;
            switch(inFact.Type) {
                case BFTypeId.Body: {
                    return string.Format("{0}.Body", parentName);
                }

                case BFTypeId.Consume: {
                    BFConsume consume = (BFConsume) inFact;
                    if (consume.OnlyWhenStressed) {
                        return string.Format("{0}.Consume.{1}.Stressed", parentName, consume.Property.ToString());
                    } else {
                        return string.Format("{0}.Consume.{1}", parentName, consume.Property.ToString());
                    }
                }

                case BFTypeId.Death: {
                    BFDeath death = (BFDeath) inFact;
                    if (death.OnlyWhenStressed) {
                        return string.Format("{0}.Death.Stressed", parentName);
                    } else {
                        return string.Format("{0}.Death", parentName);
                    }
                }

                case BFTypeId.Eat: {
                    BFEat eat = (BFEat) inFact;
                    if (eat.OnlyWhenStressed) {
                        return string.Format("{0}.Eats.{1}.Stressed", parentName, !eat.Critter ? "Unknown" : eat.Critter.name);
                    } else {
                        return string.Format("{0}.Eats.{1}", parentName, !eat.Critter ? "Unknown" : eat.Critter.name);
                    }
                }

                case BFTypeId.Grow: {
                    BFGrow grow = (BFGrow) inFact;
                    if (grow.OnlyWhenStressed) {
                        return string.Format("{0}.Grows.Stressed", parentName);
                    } else {
                        return string.Format("{0}.Grows", parentName);
                    }
                }

                case BFTypeId.Population: {
                    BFPopulation population = (BFPopulation) inFact;
                    return string.Format("{0}.Population.{1}", parentName, !population.Critter ? "Unknown" : population.Critter.name);
                }

                case BFTypeId.PopulationHistory: {
                    BFPopulationHistory population = (BFPopulationHistory) inFact;
                    return string.Format("{0}.PopulationHistory.{1}", parentName, !population.Critter ? "Unknown" : population.Critter.name);
                }

                case BFTypeId.Produce: {
                    BFProduce produce = (BFProduce) inFact;
                    if (produce.OnlyWhenStressed) {
                        return string.Format("{0}.Produce.{1}.Stressed", parentName, produce.Property.ToString());
                    } else {
                        return string.Format("{0}.Produce.{1}", parentName, produce.Property.ToString());
                    }
                }

                case BFTypeId.Reproduce: {
                    BFReproduce reproduce = (BFReproduce) inFact;
                    if (reproduce.OnlyWhenStressed) {
                        return string.Format("{0}.Reproduce.Stressed", parentName);
                    } else {
                        return string.Format("{0}.Reproduce", parentName);
                    }
                }

                case BFTypeId.State: {
                    BFState state = (BFState) inFact;
                    return string.Format("{0}.{1}.Stressed", parentName, state.Property.ToString());
                }

                case BFTypeId.WaterProperty: {
                    BFWaterProperty water = (BFWaterProperty) inFact;
                    return string.Format("{0}.{1}", parentName, water.Property.ToString());
                }

                case BFTypeId.WaterPropertyHistory: {
                    BFWaterPropertyHistory water = (BFWaterPropertyHistory) inFact;
                    return string.Format("{0}.{1}.History", parentName, water.Property.ToString());
                }

                case BFTypeId.Sim: {
                    return string.Format("{0}.Sim", parentName);
                }

                default: {
                    return null;
                }
            }
        }

        #else

        [Conditional("UNITY_EDITOR")]
        static internal void DefineEditor(BFTypeId inType, DefaultIconDelegate inIconDelegate, BFMode inMode)
        {
        }

        [Conditional("UNITY_EDITOR")]
        static internal void DefineEditor(BFTypeId inType, DefaultIconDelegate inIconDelegate, ModeDelegate inModeDelegate)
        {
        }

        #endif // UNITY_EDITOR
    }
}