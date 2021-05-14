using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Variants;

namespace Aqua
{
    static public class SerializationUtils
    {
        static SerializationUtils()
        {
            TypeUtility.RegisterSerializer<VariantTable>(SerializeVariantTable);
            TypeUtility.RegisterSerializer<NamedVariant>(SerializeNamedVariant);
            TypeUtility.RegisterSerializer<Variant>(SerializeVariant);
        }

        #region StringHash32

        static public void Serialize(this Serializer ioSerializer, string inKey, ref StringHash32 ioHash, FieldOptions inOptions = FieldOptions.None)
        {
            uint hash = ioHash.HashValue;
            ioSerializer.Serialize(inKey, ref hash, inOptions);
            if (ioSerializer.IsReading)
                ioHash = new StringHash32(hash);
        }

        static public void Serialize(this Serializer ioSerializer, string inKey, ref StringHash32 ioHash, StringHash32 inDefault, FieldOptions inOptions = FieldOptions.None)
        {
            uint hash = ioHash.HashValue;
            ioSerializer.Serialize(inKey, ref hash, inDefault.HashValue, inOptions);
            if (ioSerializer.IsReading)
                ioHash = new StringHash32(hash);
        }

        static public void Set(this Serializer ioSerializer, string inKey, ref HashSet<StringHash32> ioSet, FieldOptions inOptions = FieldOptions.None)
        {
            HashSet<uint> hashset = new HashSet<uint>();
            if (ioSerializer.IsWriting)
            {
                foreach(var hash in ioSet)
                    hashset.Add(hash.HashValue);
            }
            
            ioSerializer.Set(inKey, ref hashset, inOptions);
            
            if (ioSerializer.IsReading)
            {
                ioSet.Clear();
                foreach(var id in hashset)
                    ioSet.Add(new StringHash32(id));
            }
        }

        #endregion // StringHash32
    
        #region Variant

        static public void Table(this Serializer ioSerializer, string inKey, ref VariantTable ioTable)
        {
            ioSerializer.Custom(inKey, ref ioTable);
        }

        static private void SerializeVariantTable(ref VariantTable ioTable, Serializer ioSerializer)
        {
            StringHash32 name = ioTable.Name;
            ioSerializer.Serialize("name", ref name);

            List<NamedVariant> namedVariants = new List<NamedVariant>(ioTable.Count);
            if (ioSerializer.IsWriting)
            {
                namedVariants.AddRange(ioTable);
            }

            ioSerializer.CustomArray("values", ref namedVariants);

            if (ioSerializer.IsWriting)
            {
                ioTable.Name = name;
                ioTable.Clear();

                foreach(var namedVariant in namedVariants)
                {
                    ioTable.Set(namedVariant.Id, namedVariant.Value);
                }
            }
        }

        static private void SerializeNamedVariant(ref NamedVariant ioVariant, Serializer ioSerializer)
        {
            ioSerializer.Serialize("key", ref ioVariant.Id);
            SerializeVariant(ref ioVariant.Value, ioSerializer);
        }

        static private void SerializeVariant(ref Variant ioVariant, Serializer ioSerializer)
        {
            VariantType type = ioVariant.Type;
            ioSerializer.Enum("type", ref type);
            switch(type)
            {
                case VariantType.Bool:
                    {
                        bool boolVal = default;
                        if (ioSerializer.IsWriting)
                        {
                            boolVal = ioVariant.AsBool();
                        }
                        ioSerializer.Serialize("value", ref boolVal);
                        if (ioSerializer.IsReading)
                        {
                            ioVariant = new Variant(boolVal);
                        }
                        break;
                    }

                case VariantType.Float:
                    {
                        float floatVal = default;
                        if (ioSerializer.IsWriting)
                        {
                            floatVal = ioVariant.AsFloat();
                        }
                        ioSerializer.Serialize("value", ref floatVal);
                        if (ioSerializer.IsReading)
                        {
                            ioVariant = new Variant(floatVal);
                        }
                        break;
                    }

                case VariantType.Int:
                    {
                        int intVal = default;
                        if (ioSerializer.IsWriting)
                        {
                            intVal = ioVariant.AsInt();
                        }
                        ioSerializer.Serialize("value", ref intVal);
                        if (ioSerializer.IsReading)
                        {
                            ioVariant = new Variant(intVal);
                        }
                        break;
                    }

                case VariantType.UInt:
                    {
                        uint uintVal = default;
                        if (ioSerializer.IsWriting)
                        {
                            uintVal = ioVariant.AsUInt();
                        }
                        ioSerializer.Serialize("value", ref uintVal);
                        if (ioSerializer.IsReading)
                        {
                            ioVariant = new Variant(uintVal);
                        }
                        break;
                    }

                case VariantType.StringHash:
                    {
                        StringHash32 hashVal = default;
                        if (ioSerializer.IsWriting)
                        {
                            hashVal = ioVariant.AsStringHash();
                        }
                        ioSerializer.Serialize("value", ref hashVal);
                        if (ioSerializer.IsReading)
                        {
                            ioVariant = new Variant(hashVal);
                        }
                        break;
                    }
            }
        }

        #endregion // Variant
    }
}