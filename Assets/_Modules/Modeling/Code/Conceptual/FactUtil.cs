using System;
using System.Collections.Generic;
using BeauPools;

namespace Aqua.Modeling {
    static public class FactUtil {
        static public int AllGraphedFacts(BestiaryDesc environment, HashSet<BFBase> graphedFacts) {
            BestiaryDesc critter;
            BFFlags flags;
            int factCount = 0;

            using(PooledSet<BestiaryDesc> critters = PooledSet<BestiaryDesc>.Create())
            using(PooledSet<BFBase> potentialFacts = PooledSet<BFBase>.Create()) {
                foreach(var critterId in environment.Organisms()) {
                    critter = Assets.Bestiary(critterId);
                    foreach(var fact in critter.PlayerFacts) {
                        flags = BFType.Flags(fact);
                        if ((flags & BFFlags.IsGraphable) != 0) {
                            potentialFacts.Add(fact);
                        }
                    }
                }

                foreach(var fact in potentialFacts) { 
                    switch(fact.Type) {
                        case BFTypeId.Eat: {
                            if (critters.Contains(((BFEat) fact).Critter)) {
                                graphedFacts.Add(fact);
                                factCount++;
                            }
                            break;
                        }
                    }
                }
            }

            return factCount;
        }
    }
}