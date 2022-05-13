using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    static public class BestiaryUtils
    {
        #region Find Facts

        /// <summary>
        /// Returns the eating rule associated with this pair of creatures.
        /// </summary>
        static public BFEat FindEatingRule(StringHash32 inParentId, StringHash32 inTargetId, ActorStateId inState = ActorStateId.Alive)
        {
            return FindEatingRule(Assets.Bestiary(inParentId), inTargetId, inState);
        }

        /// <summary>
        /// Returns the eating rule associated with this pair of creatures.
        /// </summary>
        static public BFEat FindEatingRule(BestiaryDesc inParent, StringHash32 inTargetId, ActorStateId inState = ActorStateId.Alive)
        {
            if (inParent == null)
                throw new ArgumentNullException("inParent");

            if (inState == ActorStateId.Dead)
                return null;

            BFEat defaultEat = null;
            foreach (var fact in inParent.Facts)
            {
                BFEat eat = fact as BFEat;
                if (eat == null)
                    continue;

                if (eat.Critter.Id() != inTargetId)
                    continue;

                if (eat.OnlyWhenStressed)
                {
                    if (inState == ActorStateId.Stressed)
                        return eat;
                }
                else
                {
                    if (inState == ActorStateId.Alive)
                        return eat;

                    defaultEat = eat;
                }
            }

            return defaultEat;
        }

        /// <summary>
        /// Returns the eating rule associated with this pair of creatures.
        /// </summary>
        static public BFEat FindEatingRule(BestiaryDesc inParent, BestiaryDesc inTarget, ActorStateId inState = ActorStateId.Alive)
        {
            if (inParent == null)
                throw new ArgumentNullException("inParent");

            if (inState == ActorStateId.Dead)
                return null;

            BFEat defaultEat = null;
            foreach (var fact in inParent.Facts)
            {
                BFEat eat = fact as BFEat;
                if (eat == null)
                    continue;

                if (eat.Critter != inTarget)
                    continue;

                if (eat.OnlyWhenStressed)
                {
                    if (inState == ActorStateId.Stressed)
                        return eat;
                }
                else
                {
                    if (inState == ActorStateId.Alive)
                        return eat;

                    defaultEat = eat;
                }
            }

            return defaultEat;
        }

        /// <summary>
        /// Returns the parasite rule associated with this pair of creatures.
        /// </summary>
        static public BFParasite FindParasiteRule(BestiaryDesc inParent, BestiaryDesc inTarget)
        {
            if (inParent == null)
                throw new ArgumentNullException("inParent");

            foreach (var fact in inParent.Facts)
            {
                BFParasite parasite = fact as BFParasite;
                if (parasite == null)
                    continue;

                if (parasite.Critter != inTarget)
                    continue;

                return parasite;
            }

            return null;
        }

        /// <summary>
        /// Locates produce rule associated with the given creature and water property.
        /// </summary>
        static public BFProduce FindProduceRule(BestiaryDesc inParent, WaterPropertyId inPropertyId, ActorStateId inState = ActorStateId.Alive)
        {
            if (inParent == null)
                throw new ArgumentNullException("inParent");

            if (inState == ActorStateId.Dead)
                return null;

            BFProduce defaultProduce = null;
            foreach (var fact in inParent.Facts)
            {
                BFProduce prod = fact as BFProduce;
                if (prod == null)
                    continue;

                if (prod.Property != inPropertyId)
                    continue;

                if (prod.OnlyWhenStressed)
                {
                    if (inState == ActorStateId.Stressed)
                        return prod;
                }
                else
                {
                    if (inState == ActorStateId.Alive)
                        return prod;

                    defaultProduce = prod;
                }
            }

            return defaultProduce;
        }

        /// <summary>
        /// Locates consume rule associated with the given creature and water property.
        /// </summary>
        static public BFConsume FindConsumeRule(BestiaryDesc inParent, WaterPropertyId inPropertyId, ActorStateId inState = ActorStateId.Alive)
        {
            if (inParent == null)
                throw new ArgumentNullException("inParent");

            if (inState == ActorStateId.Dead)
                return null;

            BFConsume defaultConsume = null;
            foreach (var fact in inParent.Facts)
            {
                BFConsume consume = fact as BFConsume;
                if (consume == null)
                    continue;

                if (consume.Property != inPropertyId)
                    continue;

                if (consume.OnlyWhenStressed)
                {
                    if (inState == ActorStateId.Stressed)
                        return consume;
                }
                else
                {
                    if (inState == ActorStateId.Alive)
                        return consume;

                    defaultConsume = consume;
                }
            }

            return defaultConsume;
        }

        /// <summary>
        /// Locates reproduce rule associated with the given creature.
        /// </summary>
        static public BFReproduce FindReproduceRule(BestiaryDesc inParent, ActorStateId inState = ActorStateId.Alive)
        {
            if (inParent == null)
                throw new ArgumentNullException("inParent");

            if (inState == ActorStateId.Dead)
                return null;

            BFReproduce defaultReproduce = null;
            foreach (var fact in inParent.Facts)
            {
                BFReproduce reproduce = fact as BFReproduce;
                if (reproduce == null)
                    continue;

                if (reproduce.OnlyWhenStressed)
                {
                    if (inState == ActorStateId.Stressed)
                        return reproduce;
                }
                else
                {
                    if (inState == ActorStateId.Alive)
                        return reproduce;

                    defaultReproduce = reproduce;
                }
            }

            return defaultReproduce;
        }

        /// <summary>
        /// Locates growth rule associated with the given creature.
        /// </summary>
        static public BFGrow FindGrowRule(BestiaryDesc inParent, ActorStateId inState = ActorStateId.Alive)
        {
            if (inParent == null)
                throw new ArgumentNullException("inParent");

            if (inState == ActorStateId.Dead)
                return null;

            BFGrow defaultGrow = null;
            foreach (var fact in inParent.Facts)
            {
                BFGrow grow = fact as BFGrow;
                if (grow == null)
                    continue;

                if (grow.OnlyWhenStressed)
                {
                    if (inState == ActorStateId.Stressed)
                        return grow;
                }
                else
                {
                    if (inState == ActorStateId.Alive)
                        return grow;

                    defaultGrow = grow;
                }
            }

            return defaultGrow;
        }

        /// <summary>
        /// Locates the range rule associated with the given creature and water property.
        /// </summary>
        static public BFState FindStateRangeRule(BestiaryDesc inParent, WaterPropertyId inPropertyId)
        {
            foreach (var fact in inParent.FactsOfType<BFState>())
            {
                if (fact.Property == inPropertyId)
                    return fact;
            }

            return null;
        }

        /// <summary>
        /// Locates the population rule associated with the given environment and critter type.
        /// </summary>
        static public BFPopulation FindPopulationRule(BestiaryDesc inEnvironment, StringHash32 inCritterId)
        {
            return FindPopulationRule(inEnvironment, Assets.Bestiary(inCritterId));
        }

        /// <summary>
        /// Locates the population rule associated with the given environment and critter type.
        /// </summary>
        static public BFPopulation FindPopulationRule(BestiaryDesc inEnvironment, BestiaryDesc inCritter)
        {
            foreach(var fact in inEnvironment.FactsOfType<BFPopulation>())
            {
                if (fact.Critter == inCritter)
                    return fact;
            }

            return null;
        }

        /// <summary>
        /// Locates the population history rule associated with the given environment and critter type.
        /// </summary>
        static public BFPopulationHistory FindPopulationHistoryRule(BestiaryDesc inEnvironment, StringHash32 inCritterId)
        {
            return FindPopulationHistoryRule(inEnvironment, Assets.Bestiary(inCritterId));
        }

        /// <summary>
        /// Locates the population history rule associated with the given environment and critter type.
        /// </summary>
        static public BFPopulationHistory FindPopulationHistoryRule(BestiaryDesc inEnvironment, BestiaryDesc inCritter)
        {
            foreach(var fact in inEnvironment.FactsOfType<BFPopulationHistory>())
            {
                if (fact.Critter == inCritter)
                    return fact;
            }

            return null;
        }

        /// <summary>
        /// Locates the water property history rule associated with the given environment and property id.
        /// </summary>
        static public BFWaterPropertyHistory FindWaterPropertyHistoryRule(BestiaryDesc inEnvironment, WaterPropertyId inPropertyId)
        {
            foreach(var fact in inEnvironment.FactsOfType<BFWaterPropertyHistory>())
            {
                if (fact.Property == inPropertyId)
                    return fact;
            }

            return null;
        }

        #endregion // Find Facts

        #region Text

        static private readonly TextId[] GraphTypeToTextMap = new TextId[]
        {
            "words.flatGraph.lower", "words.increaseGraph.lower", "words.decreaseGraph.lower", "words.cycleGraph.lower"
        };

        /// <summary>
        /// Returns the TextId associated with the given graph type.
        /// </summary>
        static public TextId GraphTypeToTextId(BFGraphType inType)
        {
            return GraphTypeToTextMap[(int) inType];
        }

        /// <summary>
        /// Formats population for a given body.
        /// </summary>
        static public string FormatPopulation(BFBody inBody, uint inPopulation)
        {
            if (inBody.Parent.HasFlags(BestiaryDescFlags.TreatAsHerd))
            {
                float mass = inBody.MassDisplayScale * inBody.MassPerPopulation * inPopulation;
                return Property(WaterPropertyId.Mass).FormatValue(mass);
            }
            else
            {
                return inPopulation.ToString();
            }
        }

        /// <summary>
        /// Formats a mass amount.
        /// </summary>
        static public string FormatMass(float inAmount)
        {
            return BestiaryUtils.Property(WaterPropertyId.Mass).FormatValue(inAmount);
        }

        /// <summary>
        /// Formats population for a given critter type.
        /// </summary>
        static public string FormatPopulation(BestiaryDesc inCritter, uint inPopulation)
        {
            if (inCritter.HasFlags(BestiaryDescFlags.TreatAsHerd))
            {
                BFBody body = inCritter.FactOfType<BFBody>();
                float mass = body.MassDisplayScale * body.MassPerPopulation * inPopulation;
                return Property(WaterPropertyId.Mass).FormatValue(mass);
            }
            else
            {
                return inPopulation.ToString();
            }
        }

        /// <summary>
        /// Formats a property amount.
        /// </summary>
        static public string FormatProperty(float inAmount, WaterPropertyId inPropertyId)
        {
            return BestiaryUtils.Property(inPropertyId).FormatValue(inAmount);
        }

        /// <summary>
        /// Formats a percentage.
        /// </summary>
        static public string FormatPercentage(float inAmount)
        {
            return string.Format("{0}%", (int) (inAmount * 100));
        }

        /// <summary>
        /// Calculates the mass for a given population.
        /// </summary>
        static public float PopulationToMass(StringHash32 inCritterId, uint inPopulation)
        {
            return PopulationToMass(Assets.Bestiary(inCritterId), inPopulation);
        }

        /// <summary>
        /// Calculates the mass for a given population.
        /// </summary>
        static public float PopulationToMass(BestiaryDesc inCritter, uint inPopulation)
        {
            BFBody body = inCritter.FactOfType<BFBody>();
            return body.MassDisplayScale * body.MassPerPopulation * inPopulation;
        }

        /// <summary>
        /// Returns the string to use for an organism.
        /// </summary>
        static public string FullLabel(BestiaryDesc inEntry, bool inbSeparateLines = false)
        {
            if (inEntry.Category() == BestiaryDescCategory.Critter)
            {
                return Loc.Find(inEntry.CommonName());
            }
            else
            {
                MapDesc map = Assets.Map(inEntry.DiveSiteId());
                if (inbSeparateLines)
                    return Loc.FormatFromString("{0}:\n{1}", Loc.Find(map.ShortLabelId()), Loc.Find(inEntry.CommonName()));
                return Loc.FormatFromString("{0}: {1}", Loc.Find(map.ShortLabelId()), Loc.Find(inEntry.CommonName()));
            }
        }

        /// <summary>
        /// Returns the text for the location of the given organism or environment.
        /// </summary>
        static public TextId LocationLabel(BestiaryDesc inEntry)
        {
            MapDesc map;
            if (inEntry.Category() == BestiaryDescCategory.Critter)
            {
                map = Assets.Map(inEntry.StationId());
            }
            else
            {
                map = Assets.Map(inEntry.DiveSiteId());
            }

            return map.ShortLabelId();
        }

        #endregion // Facts

        #region Water Properties

        /// <summary>
        /// Returns a set of values in which the given critter is healthy.
        /// </summary>
        static public WaterPropertyBlockF32 FindHealthyWaterValues(BestiaryDesc inCritter)
        {
            return FindHealthyWaterValues(inCritter.GetActorStateTransitions(), Services.Assets.WaterProp.DefaultValues());
        }

        /// <summary>
        /// Returns a set of values in which the given critter is healthy.
        /// </summary>
        static public WaterPropertyBlockF32 FindHealthyWaterValues(ActorStateTransitionSet inStates, WaterPropertyBlockF32 inDefaultValues)
        {
            WaterPropertyBlockF32 waterProperties;
            waterProperties.Oxygen = inDefaultValues.Oxygen;
            waterProperties.Temperature = FindHealthyWaterValue(inStates.Temperature, inDefaultValues.Temperature);
            waterProperties.Light = FindHealthyWaterValue(inStates.Light, inDefaultValues.Light);
            waterProperties.PH = FindHealthyWaterValue(inStates.PH, inDefaultValues.PH);
            waterProperties.CarbonDioxide = inDefaultValues.CarbonDioxide;
            return waterProperties;
        }

        /// <summary>
        /// Returns a value in which the given state evaluates to Alive.
        /// </summary>
        static public float FindHealthyWaterValue(in ActorStateTransitionRange inRange, float inDefaultValue)
        {
            if (float.IsInfinity(inRange.AliveMin))
            {
                if (float.IsInfinity(inRange.AliveMax))
                {
                    return inDefaultValue;
                }
                else
                {
                    return (inRange.AliveMax + inDefaultValue) / 2;
                }
            }
            else if (float.IsInfinity(inRange.AliveMax))
            {
                return (inRange.AliveMin + inDefaultValue) / 2;
            }
            else
            {
                return (inRange.AliveMin + inRange.AliveMax) / 2;
            }
        }

        /// <summary>
        /// Returns the property description associated with the given property id.
        /// </summary>
        static public WaterPropertyDesc Property(WaterPropertyId inId)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                return ValidationUtils.FindAsset<WaterPropertyDesc>(inId.ToString());
            #endif // UNITY_EDITOR
            return Assets.Property(inId);
        }

        #endregion // Water Properties
    }
}