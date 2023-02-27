using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace Aqua
{
    public class SiteSurveyData : ISerializedObject, ISerializedVersion, IKeyValuePair<StringHash32, SiteSurveyData>
    {
        public StringHash32 MapId;
        public HashSet<StringHash32> TaggedCritters = Collections.NewSet<StringHash32>(8);
        public HashSet<StringHash32> GraphedCritters = Collections.NewSet<StringHash32>(16);
        public HashSet<StringHash32> GraphedFacts = Collections.NewSet<StringHash32>(16);

        public Action OnChanged;

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, SiteSurveyData>.Key { get { return MapId; } }
        SiteSurveyData IKeyValuePair<StringHash32, SiteSurveyData>.Value { get { return this; } }

        #endregion // KeyValue

        #region ISerializedObject

        // v2: removed site version, added graphing
        public ushort Version { get { return 2; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32Proxy("mapId", ref MapId);
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
        }

        #endregion // ISerializedObject
    }
}