using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace Aqua {
    static public class BFType {
        #region Consts

        public const int TypeCount = (int)BFTypeId._COUNT;

        static private readonly BFTypeId[] TypeSortingOrder = new BFTypeId[] {
            BFTypeId.Model, BFTypeId.Population, BFTypeId.PopulationHistory, BFTypeId.WaterProperty, BFTypeId.WaterPropertyHistory,
            BFTypeId.State, BFTypeId.Parasite, BFTypeId.Eat, BFTypeId.Produce, BFTypeId.Consume, BFTypeId.Grow, BFTypeId.Reproduce, BFTypeId.Death
        };
        static private readonly int[] TypeSortingOrderIndices = new int[TypeCount];

        static BFType() {
            for (int i = 0; i < TypeCount; i++) {
                TypeSortingOrderIndices[i] = Array.IndexOf(TypeSortingOrder, (BFTypeId)i);
                s_DefaultDiscoveredFlags[i] = BFDiscoveredFlags.All;
            }
        }

        #endregion // Consts

        #region Types

        internal delegate void CollectReferencesDelegate(BFBase inFact, HashSet<StringHash32> outCritterIds);
        internal delegate BFDetails GenerateDetailsDelegate(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference);
        internal delegate IEnumerable<BFFragment> GenerateFragmentsDelegate(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference);
        internal delegate BestiaryDesc GetTargetDelegate(BFBase inFact);
        internal delegate WaterPropertyId GetPropertyDelegate(BFBase inFact);
        internal delegate Sprite DefaultIconDelegate(BFBase inFact);
        internal delegate BFMode ModeDelegate(BFBase inFact);

        #endregion // Types

        static private readonly BFDiscoveredFlags[] s_DefaultDiscoveredFlags = new BFDiscoveredFlags[TypeCount];
        static private readonly BFShapeId[] s_Shapes = new BFShapeId[TypeCount];
        static private readonly BFFlags[] s_Flags = new BFFlags[TypeCount];
        static private readonly CollectReferencesDelegate[] s_CollectReferencesDelegates = new CollectReferencesDelegate[TypeCount];
        static private readonly GenerateDetailsDelegate[] s_GenerateDetailsDelegates = new GenerateDetailsDelegate[TypeCount];
        static private readonly GenerateFragmentsDelegate[] s_GenerateFragmentsDelegates = new GenerateFragmentsDelegate[TypeCount];
        static private readonly GetTargetDelegate[] s_GetTargetDelegates = new GetTargetDelegate[TypeCount];
        static private readonly GetPropertyDelegate[] s_GetPropertyDelegates = new GetPropertyDelegate[TypeCount];
        static private readonly Comparison<BFBase>[] s_ComparisonDelegates = new Comparison<BFBase>[TypeCount];

        static public bool IsBehavior(BFTypeId inTypeId) {
            return (s_Flags[(int)inTypeId] & BFFlags.IsBehavior) != 0;
        }

        static public bool IsBehavior(BFBase inFact) {
            return (s_Flags[(int)inFact.Type] & BFFlags.IsBehavior) != 0;
        }

        static public bool IsSelfTargeting(BFTypeId inTypeId) {
            return (s_Flags[(int)inTypeId] & BFFlags.SelfTarget) != 0;
        }

        static public bool IsSelfTargeting(BFBase inFact) {
            return (s_Flags[(int)inFact.Type] & BFFlags.SelfTarget) != 0;
        }

        static public bool IsEnvironment(BFTypeId inTypeId) {
            return (s_Flags[(int)inTypeId] & BFFlags.EnvironmentFact) != 0;
        }

        static public bool IsEnvironment(BFBase inFact) {
            return (s_Flags[(int)inFact.Type] & BFFlags.EnvironmentFact) != 0;
        }

        static public bool IsOrganism(BFTypeId inTypeId) {
            return (s_Flags[(int)inTypeId] & BFFlags.EnvironmentFact) == 0;
        }

        static public bool IsOrganism(BFBase inFact) {
            return (s_Flags[(int)inFact.Type] & BFFlags.EnvironmentFact) == 0;
        }

        #region Attributes

        static public BFShapeId Shape(BFBase inFact) {
            return s_Shapes[(int)inFact.Type];
        }

        static public BFShapeId Shape(BFTypeId inFactType) {
            return s_Shapes[(int)inFactType];
        }

        static public BFFlags Flags(BFBase inFact) {
            return s_Flags[(int)inFact.Type];
        }

        static public BFFlags Flags(BFTypeId inFactType) {
            return s_Flags[(int)inFactType];
        }

        static public BFDiscoveredFlags DefaultDiscoveredFlags(BFBase inFact) {
            if (inFact.Parent.HasFlags(BestiaryDescFlags.Human | BestiaryDescFlags.IsSpecter))
                return BFDiscoveredFlags.All;
            return s_DefaultDiscoveredFlags[(int)inFact.Type];
        }

        static public BFDiscoveredFlags DefaultDiscoveredFlags(BFTypeId inFactType) {
            return s_DefaultDiscoveredFlags[(int)inFactType];
        }

        static public BestiaryDesc Target(BFBase inFact) {
            GetTargetDelegate custom = s_GetTargetDelegates[(int)inFact.Type];
            if (custom != null)
                return custom(inFact);

            return null;
        }

        static public WaterPropertyId WaterProperty(BFBase inFact) {
            GetPropertyDelegate custom = s_GetPropertyDelegates[(int)inFact.Type];
            if (custom != null)
                return custom(inFact);

            return WaterPropertyId.NONE;
        }

        static public bool OnlyWhenStressed(BFBase inFact) {
            if (IsBehavior(inFact)) {
                return ((BFBehavior)inFact).OnlyWhenStressed;
            }
            return false;
        }

        /// <summary>
        /// Returns if the given fact is owned by someone else.
        /// </summary>
        [MethodImpl(256)]
        static public bool IsBorrowed(BFBase inFact, BestiaryDesc inReference) {
            return inReference != null && inReference != inFact.Parent;
        }

        #endregion // Attributes

        #region Methods

        static public BFDetails GenerateDetails(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference) {
            GenerateDetailsDelegate custom = s_GenerateDetailsDelegates[(int)inFact.Type];
            if (custom != null)
                return custom(inFact, inFlags, inReference);

            BFDetails details;
            details.Image = inFact.Icon;
            details.Description = null;
            details.Header = null;
            return details;
        }

        static public BFDetails GenerateDetails(BFBase inFact) {
            return GenerateDetails(inFact, s_DefaultDiscoveredFlags[(int)inFact.Type], null);
        }

        static public IEnumerable<BFFragment> GenerateFragments(BFBase inFact, BFDiscoveredFlags inFlags, BestiaryDesc inReference) {
            GenerateFragmentsDelegate custom = s_GenerateFragmentsDelegates[(int)inFact.Type];
            if (custom != null)
                return custom(inFact, inFlags, inReference);

            return null;
        }

        /// <summary>
        /// Sorts facts by their visual order.
        /// </summary>
        static public Comparison<BFBase> SortByVisualOrder = (x, y) => {
            int compareTypeSorting = TypeSortingOrderIndices[(int)x.Type] - TypeSortingOrderIndices[(int)y.Type];
            if (compareTypeSorting != 0)
                return Math.Sign(compareTypeSorting);

            int compareTypes = x.Type - y.Type;
            if (compareTypes != 0)
                return Math.Sign(compareTypes);

            Comparison<BFBase> comparer = s_ComparisonDelegates[(int)x.Type];
            if (comparer != null) {
                int compareCustom = comparer(x, y);
                if (compareCustom != 0)
                    return compareCustom;
            }

            return x.Id.CompareTo(y.Id);
        };

        #endregion // Methods

        #region Definitions

        static internal void DefineAttributes(BFTypeId inType, BFShapeId inShape, BFFlags inFlags, BFDiscoveredFlags inDefaultDiscoveredFlags, Comparison<BFBase> inComparison) {
            s_Shapes[(int)inType] = inShape;
            s_Flags[(int)inType] = inFlags;
            s_DefaultDiscoveredFlags[(int)inType] = inDefaultDiscoveredFlags;
            s_ComparisonDelegates[(int)inType] = inComparison;
        }

        static internal void DefineMethods(BFTypeId inType, CollectReferencesDelegate inCollectReferences, GenerateDetailsDelegate inGenerateDetails, GenerateFragmentsDelegate inGenerateFragments, GetTargetDelegate inGetTarget, GetPropertyDelegate inGetProperty) {
            s_CollectReferencesDelegates[(int)inType] = inCollectReferences;
            s_GenerateDetailsDelegates[(int)inType] = inGenerateDetails;
            s_GenerateFragmentsDelegates[(int)inType] = inGenerateFragments;
            s_GetTargetDelegates[(int)inType] = inGetTarget;
            s_GetPropertyDelegates[(int)inType] = inGetProperty;
        }

        #endregion // Definitions

        #region Extensions

        [MethodImpl(256)]
        static public bool HasRate(BFDiscoveredFlags flags) {
            return (flags & BFDiscoveredFlags.Rate) == BFDiscoveredFlags.Rate;
        }

        [MethodImpl(256)]
        static public bool HasAll(BFDiscoveredFlags flags) {
            return (flags & BFDiscoveredFlags.All) == BFDiscoveredFlags.All;
        }

        #endregion // Extensions

#if UNITY_EDITOR

        static private readonly Type[] TypeToSystemTypeMapping = new Type[] {
            typeof(BFBody), typeof(BFState),
            typeof(BFEat), typeof(BFConsume), typeof(BFProduce), typeof(BFParasite),
            typeof(BFDeath), typeof(BFGrow), typeof(BFReproduce),
            typeof(BFPopulation), typeof(BFPopulationHistory),
            typeof(BFWaterProperty), typeof(BFWaterPropertyHistory),
            typeof(BFModel), typeof(BFSim)
        };

        static private readonly DefaultIconDelegate[] s_DefaultIconDelegates = new DefaultIconDelegate[TypeCount];
        static private readonly ModeDelegate[] s_ModeDelegates = new ModeDelegate[TypeCount];

        static internal void DefineEditor(BFTypeId inType, DefaultIconDelegate inIconDelegate, BFMode inMode) {
            s_DefaultIconDelegates[(int)inType] = inIconDelegate;
            s_ModeDelegates[(int)inType] = (b) => inMode;
        }

        static internal void DefineEditor(BFTypeId inType, DefaultIconDelegate inIconDelegate, ModeDelegate inModeDelegate) {
            s_DefaultIconDelegates[(int)inType] = inIconDelegate;
            s_ModeDelegates[(int)inType] = inModeDelegate;
        }

        static internal Sprite ResolveIcon(BFBase inFact, Sprite inOverride) {
            if (inOverride != null)
                return inOverride;

            Sprite icon = s_DefaultIconDelegates[(int)inFact.Type]?.Invoke(inFact);
            if (icon == null)
                icon = ValidationUtils.FindAsset<BestiaryDB>().DefaultIcon(inFact);
            return icon;
        }

        static public BFMode ResolveMode(BFBase inFact) {
            BFBehavior behavior = inFact as BFBehavior;
            if (behavior != null && behavior.AutoGive)
                return BFMode.Always;
            BFMode mode = s_ModeDelegates[(int)inFact.Type]?.Invoke(inFact) ?? BFMode.Player;
            if (mode == BFMode.Player && inFact.Parent.HasFlags(BestiaryDescFlags.IsSpecter))
                mode = BFMode.Always;
            return mode;
        }

        static internal Type ResolveSystemType(BFTypeId inType) {
            return TypeToSystemTypeMapping[(int)inType];
        }

        static internal BFTypeId ResolveFactType(Type inType) {
            return (BFTypeId)Array.IndexOf(TypeToSystemTypeMapping, inType);
        }

        static internal string AutoNameFact(BFBase inFact) {
            string parentName = inFact.Parent.name;
            switch (inFact.Type) {
                case BFTypeId.Body: {
                        return string.Format("{0}.Body", parentName);
                    }

                case BFTypeId.Consume: {
                        BFConsume consume = (BFConsume)inFact;
                        if (consume.OnlyWhenStressed) {
                            return string.Format("{0}.Consume.{1}.Stressed", parentName, consume.Property.ToString());
                        } else {
                            return string.Format("{0}.Consume.{1}", parentName, consume.Property.ToString());
                        }
                    }

                case BFTypeId.Death: {
                        BFDeath death = (BFDeath)inFact;
                        if (death.OnlyWhenStressed) {
                            return string.Format("{0}.Death.Stressed", parentName);
                        } else {
                            return string.Format("{0}.Death", parentName);
                        }
                    }

                case BFTypeId.Eat: {
                        BFEat eat = (BFEat)inFact;
                        if (eat.OnlyWhenStressed) {
                            return string.Format("{0}.Eats.{1}.Stressed", parentName, !eat.Critter ? "Unknown" : eat.Critter.name);
                        } else {
                            return string.Format("{0}.Eats.{1}", parentName, !eat.Critter ? "Unknown" : eat.Critter.name);
                        }
                    }

                case BFTypeId.Parasite: {
                        BFParasite parasite = (BFParasite)inFact;
                        return string.Format("{0}.Stresses.{1}", parentName, !parasite.Critter ? "Unknown" : parasite.Critter.name);
                    }

                case BFTypeId.Grow: {
                        BFGrow grow = (BFGrow)inFact;
                        if (grow.OnlyWhenStressed) {
                            return string.Format("{0}.Grows.Stressed", parentName);
                        } else {
                            return string.Format("{0}.Grows", parentName);
                        }
                    }

                case BFTypeId.Population: {
                        BFPopulation population = (BFPopulation)inFact;
                        return string.Format("{0}.Population.{1}", parentName, !population.Critter ? "Unknown" : population.Critter.name);
                    }

                case BFTypeId.PopulationHistory: {
                        BFPopulationHistory population = (BFPopulationHistory)inFact;
                        return string.Format("{0}.PopulationHistory.{1}", parentName, !population.Critter ? "Unknown" : population.Critter.name);
                    }

                case BFTypeId.Produce: {
                        BFProduce produce = (BFProduce)inFact;
                        if (produce.OnlyWhenStressed) {
                            return string.Format("{0}.Produce.{1}.Stressed", parentName, produce.Property.ToString());
                        } else {
                            return string.Format("{0}.Produce.{1}", parentName, produce.Property.ToString());
                        }
                    }

                case BFTypeId.Reproduce: {
                        BFReproduce reproduce = (BFReproduce)inFact;
                        if (reproduce.OnlyWhenStressed) {
                            return string.Format("{0}.Reproduce.Stressed", parentName);
                        } else {
                            return string.Format("{0}.Reproduce", parentName);
                        }
                    }

                case BFTypeId.State: {
                        BFState state = (BFState)inFact;
                        return string.Format("{0}.{1}.Stressed", parentName, state.Property.ToString());
                    }

                case BFTypeId.WaterProperty: {
                        BFWaterProperty water = (BFWaterProperty)inFact;
                        return string.Format("{0}.{1}", parentName, water.Property.ToString());
                    }

                case BFTypeId.WaterPropertyHistory: {
                        BFWaterPropertyHistory water = (BFWaterPropertyHistory)inFact;
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