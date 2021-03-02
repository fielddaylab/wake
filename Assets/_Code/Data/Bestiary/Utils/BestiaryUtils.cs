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
            foreach(var fact in inParent.Facts)
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
        /// Locates all range rules associated with the given creature and water property.
        /// </summary>
        static public int FindRangeRules(BestiaryDesc inParent, WaterPropertyId inPropertyId, ICollection<BFStateRange> outRanges)
        {
            int count = 0;

            foreach(var fact in inParent.StateFacts)
            {
                BFStateRange range = fact as BFStateRange;
                if (range == null)
                    continue;

                if (range.PropertyId() != inPropertyId)
                    continue;

                outRanges.Add(range);
                ++count;
            }

            return count;
        }

        /// <summary>
        /// Locates the state change ranges associated with the given creature and water property.
        /// </summary>
        static public ActorStateTransitionRange FindStateTransitions(BestiaryDesc inParent, WaterPropertyId inPropertyId)
        {
            ActorStateTransitionRange transitionRange = new ActorStateTransitionRange();
            transitionRange.Reset();

            foreach(var fact in inParent.StateFacts)
            {
                BFStateRange range = fact as BFStateRange;
                if (range == null)
                    continue;

                if (range.PropertyId() != inPropertyId)
                    continue;

                transitionRange.Encompass(range);
            }

            return transitionRange;
        }
    
        /// <summary>
        /// Generates the initial water chemistry properties for the given environment.
        /// </summary>
        static public WaterPropertyBlockF32 GenerateInitialState(BestiaryDesc inParent)
        {
            WaterPropertyBlockF32 properties = Services.Assets.WaterProp.DefaultValues();
            foreach(var fact in inParent.Facts)
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