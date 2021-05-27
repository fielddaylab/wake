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
        static public BFState FindRangeRule(BestiaryDesc inParent, WaterPropertyId inPropertyId)
        {
            foreach (var fact in inParent.StateFacts)
            {
                if (fact.PropertyId() != inPropertyId)
                    continue;

                return fact;
            }

            return null;
        }

        /// <summary>
        /// Locates the state change ranges associated with the given creature and water property.
        /// </summary>
        static public ActorStateTransitionRange FindStateTransition(BestiaryDesc inParent, WaterPropertyId inPropertyId)
        {
            foreach (var fact in inParent.StateFacts)
            {
                BFState range = fact as BFState;
                if (range == null)
                    continue;

                if (range.PropertyId() != inPropertyId)
                    continue;

                return range.Range();
            }

            return ActorStateTransitionRange.Default;
        }

        /// <summary>
        /// Generates the initial water chemistry properties for the given environment.
        /// </summary>
        static public WaterPropertyBlockF32 GenerateInitialState(BestiaryDesc inParent)
        {
            WaterPropertyBlockF32 properties = Services.Assets.WaterProp.DefaultValues();
            foreach (var fact in inParent.Facts)
            {
                BFWaterProperty bfWater = fact as BFWaterProperty;
                if (bfWater != null)
                {
                    properties[bfWater.PropertyId()] = bfWater.Value();
                }
            }
            return properties;
        }
    }
}