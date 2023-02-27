using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Variants;

namespace Aqua {
    public class ArgueData : ISerializedObject, ISerializedVersion, IKeyValuePair<StringHash32, ArgueData> {
        public const int MaxFactsPerClaim = 4;

        public StringHash32 Id;
        public StringHash32 ClaimId;
        public StringHash32 ClaimLabel;
        public HashSet<StringHash32> ExpectedFacts = Collections.NewSet<StringHash32>(4);
        public BFShapeId[] FactSlots = new BFShapeId[MaxFactsPerClaim];
        public StringHash32[] SubmittedFacts = new StringHash32[MaxFactsPerClaim];
        public VariantTable Vars = new VariantTable();
        
        // Not serialized
        public Action OnChanged;

        #region IKeyValuePair

        StringHash32 IKeyValuePair<StringHash32, ArgueData>.Key { get { return Id; } }
        ArgueData IKeyValuePair<StringHash32, ArgueData>.Value { get { return this; } }

        #endregion // IKeyValuePair

        #region ISerializedObject

        ushort ISerializedVersion.Version { get { return 1; } }

        public void Serialize(Serializer ioSerializer) {
            ioSerializer.UInt32Proxy("id", ref Id);
            ioSerializer.UInt32Proxy("claim", ref ClaimId);
            ioSerializer.UInt32Proxy("label", ref ClaimLabel);
            ioSerializer.UInt32ProxySet("expected", ref ExpectedFacts);
            ioSerializer.EnumArray("slots", ref FactSlots);
            ioSerializer.UInt32ProxyArray("submitted", ref SubmittedFacts);
            ioSerializer.Object("vars", ref Vars);
        }

        #endregion // ISerializedObject
    }
}