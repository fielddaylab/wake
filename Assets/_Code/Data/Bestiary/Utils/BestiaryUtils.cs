using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace Aqua
{
    static public class BestiaryUtils
    {
        /// <summary>
        /// Returns the eating rule associated with this pair of creatures.
        /// </summary>
        static public BFEat FindEatingRule(StringHash32 inParentId, StringHash32 inTargetId, ActorStateId inState = ActorStateId.Alive)
        {
            return FindEatingRule(Services.Assets.Bestiary[inParentId], inTargetId, inState);
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

                if (eat.Target().Id() != inTargetId)
                    continue;

                if (eat.OnlyWhenStressed())
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

                if (prod.Target() != inPropertyId)
                    continue;

                if (prod.OnlyWhenStressed())
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

                if (consume.Target() != inPropertyId)
                    continue;

                if (consume.OnlyWhenStressed())
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
        /// Locates the range rule associated with the given creature and water property.
        /// </summary>
        static public BFState FindStateRangeRule(BestiaryDesc inParent, WaterPropertyId inPropertyId)
        {
            foreach (var fact in inParent.FactsOfType<BFState>())
            {
                if (fact.PropertyId() == inPropertyId)
                    return fact;
            }

            return null;
        }

        /// <summary>
        /// Locates the population rule associated with the given environment and critter type.
        /// </summary>
        static public BFPopulation FindPopulationRule(BestiaryDesc inEnvironment, StringHash32 inCritterId, byte inSiteVersion = 0)
        {
            return FindPopulationRule(inEnvironment, Services.Assets.Bestiary[inCritterId], inSiteVersion);
        }

        /// <summary>
        /// Locates the population rule associated with the given environment and critter type.
        /// </summary>
        static public BFPopulation FindPopulationRule(BestiaryDesc inEnvironment, BestiaryDesc inCritter, byte inSiteVersion = 0)
        {
            foreach(var fact in inEnvironment.FactsOfType<BFPopulation>())
            {
                if (fact.SiteVersion() == inSiteVersion && fact.Critter() == inCritter)
                    return fact;
            }

            return null;
        }
    }
}