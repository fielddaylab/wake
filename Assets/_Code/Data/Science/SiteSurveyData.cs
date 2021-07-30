using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace Aqua
{
    public class SiteSurveyData : ISerializedObject, ISerializedVersion, IKeyValuePair<StringHash32, SiteSurveyData>
    {
        public StringHash32 MapId;
        public HashSet<StringHash32> TaggedCritters = new HashSet<StringHash32>();
        public byte SiteVersion;

        public Action OnChanged;

        #region KeyValue

        StringHash32 IKeyValuePair<StringHash32, SiteSurveyData>.Key { get { return MapId; } }
        SiteSurveyData IKeyValuePair<StringHash32, SiteSurveyData>.Value { get { return this; } }

        #endregion // KeyValue

        #region ISerializedObject

        public ushort Version { get { return 1; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.UInt32Proxy("mapId", ref MapId);
            ioSerializer.UInt32ProxySet("taggedCritters", ref TaggedCritters);
            ioSerializer.Serialize("siteVersion", ref SiteVersion);
        }

        #endregion // ISerializedObject
    }
}