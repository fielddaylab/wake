/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    31 Oct 2021
 * 
 * File:    LeafExpression.cs
 * Purpose: Single leaf expression.
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Runtime.InteropServices;
using System.Text;
using BeauUtil;
using BeauUtil.Variants;

namespace Leaf.Runtime
{
    /// <summary>
    /// Single expression.
    /// </summary>
    public struct LeafExpression
    #if USING_BEAUDATA
        : BeauData.ISerializedObject
    #endif // USING_BEAUDATA
    {
        #region Types

        public enum TypeFlags : byte
        {
            IsLogical = 0x01,
            IsAnd = 0x02,
            IsOr = 0x04
        }

        public enum OperandType : byte
        {
            Value,
            Read,
            Method
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct OperandData
        {
            [FieldOffset(0)] public Variant Value;
            [FieldOffset(0)] public TableKeyPair TableKey;
            [FieldOffset(0)] public StringHash32 MethodId;
            [FieldOffset(4)] public uint MethodArgsIndex;

            public OperandData(Variant inValue)
                : this()
            {
                Value = inValue;
            }

            public OperandData(TableKeyPair inTableKey)
                : this()
            {
                TableKey = inTableKey;
            }

            public OperandData(StringHash32 inMethodId, uint inArgsIndex)
                : this()
            {
                MethodId = inMethodId;
                MethodArgsIndex = inArgsIndex;
            }

            #if USING_BEAUDATA

            public void Serialize(BeauData.Serializer ioSerializer, OperandType inType)
            {
                switch(inType)
                {
                    case OperandType.Value:
                        ioSerializer.Object("value", ref Value);
                        break;

                    case OperandType.Read:
                        ioSerializer.UInt32Proxy("tableId", ref TableKey.TableId);
                        ioSerializer.UInt32Proxy("varId", ref TableKey.VariableId);
                        break;

                    case OperandType.Method:
                        ioSerializer.UInt32Proxy("methodId", ref MethodId);
                        ioSerializer.Serialize("methodArgsIndex", ref MethodArgsIndex);
                        break;
                }
            }

            #endif // USING_BEAUDATA
        }

        #endregion // Types

        public TypeFlags Flags;
        public VariantCompareOperator Operator;
        public OperandType LeftType;
        public OperandType RightType;
        public OperandData Left;
        public OperandData Right;

        #if USING_BEAUDATA

        public void Serialize(BeauData.Serializer ioSerializer)
        {
            ioSerializer.Enum("flags", ref Flags);
            ioSerializer.Enum("leftType", ref LeftType);
            ioSerializer.BeginGroup("left");
            {
                Left.Serialize(ioSerializer, LeftType);
            }
            ioSerializer.EndGroup();
            
            if ((Flags & TypeFlags.IsLogical) != 0)
            {
                ioSerializer.Enum("operator", ref Operator);
                if (Operator <= VariantCompareOperator.GreaterThan)
                {
                    ioSerializer.Enum("rightType", ref RightType);
                    ioSerializer.BeginGroup("right");
                    {
                        Right.Serialize(ioSerializer, RightType);
                    }
                    ioSerializer.EndGroup();
                }
            }
        }

        #endif // USING_BEAUDATA

        public string ToDebugString(LeafNode inNode)
        {
            return ToDebugString(inNode.Package());
        }

        public string ToDebugString(LeafNodePackage inPackage)
        {
            #if DEVELOPMENT
            StringBuilder sb = new StringBuilder();
            LeafInstruction.DisassembleExpression(inPackage.m_Instructions, this, sb);
            return sb.Flush();
            #else
            return "LeafExpression cannot be decompiled in non-development builds";
            #endif // DEVELOPMENT
        }
    }
}