using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using Leaf.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;
using System.Reflection;
using System;

namespace Aqua {
    static public partial class Assets {
        [LeafMember("Lookup"), Preserve]
        static private Variant LeafAssetProperty(StringHash32 assetId, StringHash32 propertyId) {
            var asset = Assets.Find(assetId);
            var lookup = LookupCache.Find(asset.GetType(), propertyId);
            if (lookup.Lookup != null) {
                object o = lookup.Lookup(asset);
                if (!Variant.TryConvertFrom(o, out Variant v)) {
                    Log.Error("[Assets.Leaf] Unable to convert return value of '{0}' on type '{1}' to Variant", propertyId, asset.GetType().Name);
                }
                return v;
            }

            return default;
        }

        #region Lookup

        static internal class LookupCache {
            internal delegate object LookupDelegate(object target);

            internal struct LookupMember {
                public StringHash32 Id;
                public LookupDelegate Lookup;
            }

            static private readonly Dictionary<Type, LookupMember[]> s_Lookups = new Dictionary<Type, LookupMember[]>(8);
            static private readonly List<LookupMember> s_LookupBuilder = new List<LookupMember>();

            static public LookupMember Find(Type type, StringHash32 id) {
                LookupMember[] members;
                if (!s_Lookups.TryGetValue(type, out members)) {
                    s_LookupBuilder.Clear();

                    LookupMember member;
                    foreach(var pair in Reflect.FindMembers<LeafLookupAttribute>(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                        pair.Attribute.AssignId(pair.Info);
                        member.Id = pair.Attribute.Id;
                        if (pair.Info.MemberType == MemberTypes.Field) {
                            FieldInfo info = (FieldInfo) pair.Info;
                            member.Lookup = info.GetValue;
                        } else if (pair.Info.MemberType == MemberTypes.Property) {
                            PropertyInfo info = (PropertyInfo) pair.Info;
                            member.Lookup = info.GetValue;
                        } else if (pair.Info.MemberType == MemberTypes.Method) {
                            MethodInfo info = (MethodInfo) pair.Info;
                            member.Lookup = (o) => info.Invoke(o, Array.Empty<object>());
                        } else {
                            continue;
                        }

                        s_LookupBuilder.Add(member);
                    }

                    members = s_LookupBuilder.ToArray();
                    s_LookupBuilder.Clear();
                    s_Lookups.Add(type, members);
                }

                foreach(var member in members) {
                    if (member.Id == id) {
                        return member;
                    }
                }

                Log.Error("[Assets.Leaf] No property with id '{0}' defined on asset type '{1}'", id, type.Name);
                return default;
            }
        }

        #endregion // Lookup
    }
}