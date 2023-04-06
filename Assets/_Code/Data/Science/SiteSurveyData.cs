using System;
using System.Collections.Generic;
using Aqua.Profile;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace Aqua
{
    public class SiteSurveyData : ISerializedObject, ISerializedVersion, IKeyValuePair<StringHash32, SiteSurveyData>, ISerializedCallbacks
    {
        public StringHash32 EnvOrMapId;
        public HashSet<StringHash32> TaggedCritters = Collections.NewSet<StringHash32>(8);
        public HashSet<StringHash32> GraphedCritters = Collections.NewSet<StringHash32>(16);
        public HashSet<StringHash32> GraphedFacts = Collections.NewSet<StringHash32>(16);
        public uint GraphLayoutSeed;

        public Action OnChanged;

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, SiteSurveyData>.Key { get { return EnvOrMapId; } }
        SiteSurveyData IKeyValuePair<StringHash32, SiteSurveyData>.Value { get { return this; } }

        #endregion // KeyValue

        #region ISerializedObject

        // v2: removed site version, added graphing
        // v3: added graph layout seed
        public ushort Version { get { return 3; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32Proxy("mapId", ref EnvOrMapId);
            ioSerializer.UInt32ProxySet("taggedCritters", ref TaggedCritters);
            if (ioSerializer.ObjectVersion < 2)
            {
                byte version = 0;
                ioSerializer.Serialize("siteVersion", ref version);
            }
            else
            {
                ioSerializer.UInt32ProxySet("graphedCritters", ref GraphedCritters);
                ioSerializer.UInt32ProxySet("graphedFacts", ref GraphedFacts);
            }

            if (ioSerializer.ObjectVersion >= 3)
            {
                ioSerializer.Serialize("graphLayoutSeed", ref GraphLayoutSeed);
            }
            else
            {
                GraphLayoutSeed = 0;
            }
        }

        void ISerializedCallbacks.PostSerialize(Serializer.Mode inMode, ISerializerContext inContext)
        {
            if (inMode != Serializer.Mode.Read) {
                return;
            }

            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) {
                return;
            }
            #endif // UNITY_EDITOR

            SavePatcher.PatchIds(TaggedCritters);
            SavePatcher.PatchIds(GraphedCritters);
            SavePatcher.PatchIds(GraphedFacts);

            var bestiary = Services.Assets.Bestiary;

            TaggedCritters.RemoveWhere((critterId) => {
                if (!bestiary.HasId(critterId))
                {
                    Log.Warn("[SiteSurveyData] Unknown critter id '{0}'", critterId);
                    return true;
                }

                return false;
            });

            GraphedCritters.RemoveWhere((critterId) => {
                if (!bestiary.HasId(critterId))
                {
                    Log.Warn("[SiteSurveyData] Unknown critter id '{0}'", critterId);
                    return true;
                }

                return false;
            });

            GraphedFacts.RemoveWhere((factId) => {
                if (!bestiary.HasFactWithId(factId))
                {
                    Log.Warn("[SiteSurveyData] Unknown fact id '{0}'", factId);
                    return true;
                }

                return false;
            });

            if (!EnvOrMapId.IsEmpty) {
                BestiaryDesc env = GetBestiaryEntryForMapOrBestiary(EnvOrMapId);
                if (env != null) {
                    TaggedCritters.RemoveWhere((critterId) => {
                        if (!env.HasOrganism(critterId))
                        {
                            Log.Warn("[SiteSurveyData] Environment '{0}' no longer has critter '{1}'", env.name, critterId);
                            return true;
                        }

                        return false;
                    });

                    GraphedCritters.RemoveWhere((critterId) => {
                        if (!env.HasOrganism(critterId))
                        {
                            Log.Warn("[SiteSurveyData] Environment '{0}' no longer has critter '{1}'", env.name, critterId);
                            return true;
                        }

                        return false;
                    });

                    GraphedFacts.RemoveWhere((factId) => {
                        var fact = Assets.Fact(factId);
                        if (fact.Parent.Category() == BestiaryDescCategory.Critter && !env.HasOrganism(fact.Parent.Id()))
                        {
                            Log.Warn("[SiteSurveyData] Environment '{0}' no longer has critter '{1}' to support '{2}'", env.name, fact.Parent.name, factId);
                            return true;
                        }
                        var target = BFType.Target(fact);
                        if (target != null && target.Category() == BestiaryDescCategory.Critter && !env.HasOrganism(target.Id()))
                        {
                            Log.Warn("[SiteSurveyData] Environment '{0}' no longer has critter '{1}' to support '{2}'", env.name, target.name, factId);
                            return true;
                        }

                        return false;
                    });
                }
            }
        }

        static private BestiaryDesc GetBestiaryEntryForMapOrBestiary(StringHash32 mapOrBestiaryId) {
            ScriptableObject obj = Assets.Find(mapOrBestiaryId);
            MapDesc map = obj as MapDesc;
            if (map != null) {
                return Assets.Bestiary(map.EnvironmentId());
            }

            return obj as BestiaryDesc;
        }

        #endregion // ISerializedObject
    }
}