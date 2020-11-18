using System;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Portable
{
    [Serializable]
    public class BestiaryEntry : IKeyValuePair<StringHash32, BestiaryEntry>
    {
        public SerializedHash32 Id;
        public BestiaryEntryType Type;
        public string ScientificName;
        public string CommonName;
        public Sprite Icon;
        public Sprite Sketch;

        public BestiaryFactBase[] Facts;

        #region IKeyValue

        StringHash32 IKeyValuePair<StringHash32, BestiaryEntry>.Key { get { return Id; } }

        BestiaryEntry IKeyValuePair<StringHash32, BestiaryEntry>.Value { get { return this; } }
    
        #endregion // IKeyValue
    }

    public enum BestiaryEntryType
    {
        Critter,
        Ecosystem
    }
}