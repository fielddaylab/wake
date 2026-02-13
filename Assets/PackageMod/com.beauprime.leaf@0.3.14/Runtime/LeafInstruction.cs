/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    31 Oct 2021
 * 
 * File:    LeafInstruction.cs
 * Purpose: Leaf instruction stream utilities.
 */

#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Text;
using BeauUtil;
using BeauUtil.Variants;

namespace Leaf.Runtime
{
    /// <summary>
    /// Leaf instruction stream utilities.
    /// </summary>
    static internal unsafe class LeafInstruction
    {
        internal const uint EmptyIndex = uint.MaxValue;

        #region Integrals

        static internal uint WriteByte(RingBuffer<byte> ioBytes, byte inValue)
        {
            ioBytes.PushBack(inValue);
            return 1;
        }

        static internal void OverwriteByte(RingBuffer<byte> ioBytes, int inOffset, byte inValue)
        {
            ioBytes[inOffset] = inValue;
        }

        static internal byte ReadByte(byte[] inBytes, ref uint ioProgramCounter)
        {
            return inBytes[ioProgramCounter++];
        }

        static internal uint WriteUInt32(RingBuffer<byte> ioBytes, uint inValue)
        {
            ioBytes.PushBack((byte) inValue);
            ioBytes.PushBack((byte) (inValue >> 8));
            ioBytes.PushBack((byte) (inValue >> 16));
            ioBytes.PushBack((byte) (inValue >> 24));
            return 4;
        }

        static internal void OverwriteUInt32(RingBuffer<byte> ioBytes, int inOffset, uint inValue)
        {
            ioBytes[inOffset] = (byte) inValue;
            ioBytes[inOffset + 1] = (byte) (inValue >> 8);
            ioBytes[inOffset + 2] = (byte) (inValue >> 16);
            ioBytes[inOffset + 3] = (byte) (inValue >> 24);
        }

        static internal uint ReadUInt32(byte[] inBytes, ref uint ioProgramCounter)
        {
            fixed(byte* ptr = &inBytes[ioProgramCounter])
            {
                ioProgramCounter += 4;
                if (((uint) ptr % 4) == 0)
                {
                    return *(uint*)(ptr);
                }
                else
                {
                    return ((uint) (*ptr) | (uint) (ptr[1] << 8) | (uint) (ptr[2] << 16) | (uint) (ptr[3] << 24));
                }
            }
        }

        static internal uint WriteInt32(RingBuffer<byte> ioBytes, int inValue)
        {
            ioBytes.PushBack((byte) inValue);
            ioBytes.PushBack((byte) (inValue >> 8));
            ioBytes.PushBack((byte) (inValue >> 16));
            ioBytes.PushBack((byte) (inValue >> 24));
            return 4;
        }

        static internal void OverwriteInt32(RingBuffer<byte> ioBytes, int inOffset, int inValue)
        {
            ioBytes[inOffset] = (byte) inValue;
            ioBytes[inOffset + 1] = (byte) (inValue >> 8);
            ioBytes[inOffset + 2] = (byte) (inValue >> 16);
            ioBytes[inOffset + 3] = (byte) (inValue >> 24);
        }

        static internal int ReadInt32(byte[] inBytes, ref uint ioProgramCounter)
        {
            uint read = ReadUInt32(inBytes, ref ioProgramCounter);
            return *(int*)&read;
        }

        static internal uint WriteInt16(RingBuffer<byte> ioBytes, short inValue)
        {
            ioBytes.PushBack((byte) inValue);
            ioBytes.PushBack((byte) (inValue >> 8));
            return 2;
        }

        static internal void OverwriteInt16(RingBuffer<byte> ioBytes, int inOffset, short inValue)
        {
            ioBytes[inOffset] = (byte) inValue;
            ioBytes[inOffset + 1] = (byte) (inValue >> 8);
        }

        static internal short ReadInt16(byte[] inBytes, ref uint ioProgramCounter)
        {
            fixed(byte* ptr = &inBytes[ioProgramCounter])
            {
                ioProgramCounter += 2;
                if (((uint) ptr % 2) == 0)
                {
                    return *(short*)(ptr);
                }
                else
                {
                    return (short) ((*ptr) | (ptr[1] << 8));
                }
            }
        }

        static internal uint WriteUInt16(RingBuffer<byte> ioBytes, ushort inValue)
        {
            ioBytes.PushBack((byte) inValue);
            ioBytes.PushBack((byte) (inValue >> 8));
            return 2;
        }

        static internal void OverwriteUInt16(RingBuffer<byte> ioBytes, int inOffset, ushort inValue)
        {
            ioBytes[inOffset] = (byte) inValue;
            ioBytes[inOffset + 1] = (byte) (inValue >> 8);
        }

        static internal ushort ReadUInt16(byte[] inBytes, ref uint ioProgramCounter)
        {
            fixed(byte* ptr = &inBytes[ioProgramCounter])
            {
                ioProgramCounter += 2;
                if (((uint) ptr % 2) == 0)
                {
                    return *(ushort*)(ptr);
                }
                else
                {
                    return (ushort) ((*ptr) | (ptr[1] << 8));
                }
            }
        }

        #endregion // Integrals

        #region Leaf

        static internal uint WriteOpcode(RingBuffer<byte> ioBytes, LeafOpcode inValue)
        {
            return WriteByte(ioBytes, (byte) inValue);
        }

        static internal void OverwriteOpcode(RingBuffer<byte> ioBytes, int inOffset, LeafOpcode inValue)
        {
            OverwriteByte(ioBytes, inOffset, (byte) inValue);
        }

        static internal LeafOpcode ReadOpcode(byte[] inBytes, ref uint ioProgramCounter)
        {
            return (LeafOpcode) ReadByte(inBytes, ref ioProgramCounter);
        }

        static internal uint WriteStringHash32(RingBuffer<byte> ioBytes, StringHash32 inValue)
        {
            return WriteUInt32(ioBytes, inValue.HashValue);
        }

        static internal void OverwriteStringHash32(RingBuffer<byte> ioBytes, int inOffset, StringHash32 inValue)
        {
            OverwriteUInt32(ioBytes, inOffset, inValue.HashValue);
        }

        static internal StringHash32 ReadStringHash32(byte[] inBytes, ref uint ioProgramCounter)
        {
            return new StringHash32(ReadUInt32(inBytes, ref ioProgramCounter));
        }

        static internal uint WriteTableKeyPair(RingBuffer<byte> ioBytes, TableKeyPair inValue)
        {
            WriteUInt32(ioBytes, inValue.TableId.HashValue);
            WriteUInt32(ioBytes, inValue.VariableId.HashValue);
            return 8;
        }

        static internal void OverwriteTableKeyPair(RingBuffer<byte> ioBytes, int inOffset, TableKeyPair inValue)
        {
            OverwriteUInt32(ioBytes, inOffset, inValue.TableId.HashValue);
            OverwriteUInt32(ioBytes, inOffset + 4, inValue.VariableId.HashValue);
        }

        static internal TableKeyPair ReadTableKeyPair(byte[] inBytes, ref uint ioProgramCounter)
        {
            uint tableHash = ReadUInt32(inBytes, ref ioProgramCounter);
            uint keyHash = ReadUInt32(inBytes, ref ioProgramCounter);
            return new TableKeyPair(new StringHash32(tableHash), new StringHash32(keyHash));
        }

        static internal uint WriteVariant(RingBuffer<byte> ioBytes, Variant inValue)
        {
            WriteByte(ioBytes, (byte) inValue.Type);
            WriteUInt32(ioBytes, Variant.ToRaw(inValue));
            return 5;
        }

        static internal void OverwriteVariant(RingBuffer<byte> ioBytes, int inOffset, Variant inValue)
        {
            OverwriteByte(ioBytes, inOffset, (byte) inValue.Type);
            OverwriteUInt32(ioBytes, inOffset + 1, Variant.ToRaw(inValue));
        }

        static internal Variant ReadVariant(byte[] inBytes, ref uint ioProgramCounter)
        {
            VariantType varType = (VariantType) ReadByte(inBytes, ref ioProgramCounter);
            uint rawValue = ReadUInt32(inBytes, ref ioProgramCounter);
            return Variant.FromRaw(varType, rawValue);
        }

        static internal string ReadStringTableString(byte[] inBytes, ref uint ioProgramCounter, string[] inTable)
        {
            uint index = ReadUInt32(inBytes, ref ioProgramCounter);
            return index == EmptyIndex ? null : inTable[index];
        } 

        #endregion // Leaf

        static internal void Disassemble(LeafInstructionBlock inBlock, StringBuilder ioBuilder)
        {
            Disassemble(inBlock, 0, (uint) inBlock.InstructionStream.Length, ioBuilder);
        }

        #if DEVELOPMENT

        static internal void Disassemble(LeafInstructionBlock inBlock, uint inInstructionOffset, uint inInstructionLength, StringBuilder ioBuilder)
        {
            byte[] stream = inBlock.InstructionStream;
            uint pc = inInstructionOffset;
            uint end = pc + inInstructionLength;

            ioBuilder.Append("Instructions ").Append(pc.ToString("X4")).Append(" - ").Append(end.ToString("X4"))
                .Append(" (").Append(inInstructionLength).Append(" bytes)");

            LeafOpcode op;
            while(pc < end)
            {
                op = ReadOpcode(stream, ref pc);

                ioBuilder.Append("\n[").Append(pc.ToString("X4")).Append("] ");
                ioBuilder.Append(op.ToString());

                switch(op)
                {
                    case LeafOpcode.RunLine:
                        {
                            StringHash32 lineCode = ReadStringHash32(stream, ref pc);
                            ioBuilder.Append(' ').Append(lineCode.ToDebugString());
                            break;
                        }

                    case LeafOpcode.EvaluateSingleExpression:
                        {
                            uint expressionIdx = ReadUInt32(stream, ref pc);
                            ioBuilder.Append(" (");
                            DisassembleExpression(inBlock, inBlock.ExpressionTable[expressionIdx], ioBuilder);
                            ioBuilder.Append(" )");
                            break;
                        }

                    case LeafOpcode.EvaluateExpressionsAnd:
                        {
                            uint expressionOffset = ReadUInt32(stream, ref pc);
                            ushort expressionCount = ReadUInt16(stream, ref pc);

                            ioBuilder.Append(" (");
                            for(ushort i = 0; i < expressionCount; i++)
                            {
                                if (i > 0)
                                    ioBuilder.Append(" && ");
                                DisassembleExpression(inBlock, inBlock.ExpressionTable[expressionOffset + i], ioBuilder);
                            }
                            ioBuilder.Append(")");
                            break;
                        }

                    case LeafOpcode.EvaluateExpressionsOr:
                        {
                            uint expressionOffset = ReadUInt32(stream, ref pc);
                            ushort expressionCount = ReadUInt16(stream, ref pc);

                            ioBuilder.Append(" (");
                            for(ushort i = 0; i < expressionCount; i++)
                            {
                                if (i > 0)
                                    ioBuilder.Append(" || ");
                                DisassembleExpression(inBlock, inBlock.ExpressionTable[expressionOffset + i], ioBuilder);
                            }
                            ioBuilder.Append(")");
                            break;
                        }

                    case LeafOpcode.EvaluateExpressionsGroup:
                        {
                            // TODO: Implement
                            break;
                        }

                    case LeafOpcode.Invoke_Unoptimized:
                    case LeafOpcode.InvokeWithTarget_Unoptimized:
                    case LeafOpcode.InvokeWithReturn_Unoptimized:
                        {
                            MethodCall invocation;
                            invocation.Id = ReadStringHash32(stream, ref pc);
                            invocation.Args = ReadStringTableString(stream, ref pc, inBlock.StringTable);

                            ioBuilder.Append(' ').Append(invocation.ToDebugString());
                            break;
                        }

                    case LeafOpcode.PushValue:
                        {
                            Variant value = ReadVariant(stream, ref pc);
                            ioBuilder.Append(' ').Append(value.ToDebugString());
                            break;
                        }

                    case LeafOpcode.LoadTableValue:
                    case LeafOpcode.StoreTableValue:
                    case LeafOpcode.IncrementTableValue:
                    case LeafOpcode.DecrementTableValue:
                        {
                            TableKeyPair keyPair = ReadTableKeyPair(stream, ref pc);
                            ioBuilder.Append(' ').Append(keyPair.ToDebugString());
                            break;
                        }

                    case LeafOpcode.Jump:
                    case LeafOpcode.JumpIfFalse:
                        {
                            short jump = ReadInt16(stream, ref pc);
                            ioBuilder.Append(' ').Append(jump.ToString("X4"));
                            break;
                        }

                    case LeafOpcode.GotoNode:
                    case LeafOpcode.BranchNode:
                    case LeafOpcode.ForkNode:
                    case LeafOpcode.ForkNodeUntracked:
                        {
                            StringHash32 nodeId = ReadStringHash32(stream, ref pc);
                            ioBuilder.Append(' ').Append(nodeId.ToDebugString());
                            break;
                        }

                    case LeafOpcode.AddChoiceOption:
                        {
                            StringHash32 textId = ReadStringHash32(stream, ref pc);
                            LeafChoice.OptionFlags flags = (LeafChoice.OptionFlags) ReadByte(stream, ref pc);

                            ioBuilder.Append(' ').Append(textId.ToDebugString()).Append(", ").Append(flags);
                            break;
                        }

                    case LeafOpcode.AddChoiceAnswer:
                        {
                            StringHash32 answerId = ReadStringHash32(stream, ref pc);
                            ioBuilder.Append(' ').Append(answerId.ToDebugString());
                            break;
                        }

                    case LeafOpcode.AddChoiceData:
                        {
                            StringHash32 dataId = ReadStringHash32(stream, ref pc);
                            ioBuilder.Append(' ').Append(dataId.ToDebugString());
                            break;
                        }
                }
            }
        }

        static internal void DisassembleExpressionGroup(LeafInstructionBlock inBlock, LeafExpressionGroup inExpression, StringBuilder ioBuilder)
        {
            ioBuilder.Append("(");
            for(ushort i = 0; i < inExpression.m_Count; i++)
            {
                if (i > 0)
                    ioBuilder.Append(" || ");
                DisassembleExpression(inBlock, inBlock.ExpressionTable[inExpression.m_Offset + i], ioBuilder);
            }
            ioBuilder.Append(")");
        }

        static internal void DisassembleExpression(LeafInstructionBlock inBlock, LeafExpression inExpression, StringBuilder ioBuilder)
        {
            switch(inExpression.Operator)
            {
                case VariantCompareOperator.True:
                    {
                        DisassembleExpressionOperand(inBlock, inExpression.LeftType, inExpression.Left, ioBuilder);
                        break;
                    }

                case VariantCompareOperator.False:
                    {
                        ioBuilder.Append("!");
                        DisassembleExpressionOperand(inBlock, inExpression.LeftType, inExpression.Left, ioBuilder);
                        break;
                    }

                case VariantCompareOperator.LessThan:
                    {
                        DisassembleExpressionOperand(inBlock, inExpression.LeftType, inExpression.Left, ioBuilder);
                        ioBuilder.Append(" < ");
                        DisassembleExpressionOperand(inBlock, inExpression.RightType, inExpression.Right, ioBuilder);
                        break;
                    }

                case VariantCompareOperator.LessThanOrEqualTo:
                    {
                        DisassembleExpressionOperand(inBlock, inExpression.LeftType, inExpression.Left, ioBuilder);
                        ioBuilder.Append(" <= ");
                        DisassembleExpressionOperand(inBlock, inExpression.RightType, inExpression.Right, ioBuilder);
                        break;
                    }

                case VariantCompareOperator.EqualTo:
                    {
                        DisassembleExpressionOperand(inBlock, inExpression.LeftType, inExpression.Left, ioBuilder);
                        ioBuilder.Append(" == ");
                        DisassembleExpressionOperand(inBlock, inExpression.RightType, inExpression.Right, ioBuilder);
                        break;
                    }

                case VariantCompareOperator.NotEqualTo:
                    {
                        DisassembleExpressionOperand(inBlock, inExpression.LeftType, inExpression.Left, ioBuilder);
                        ioBuilder.Append(" != ");
                        DisassembleExpressionOperand(inBlock, inExpression.RightType, inExpression.Right, ioBuilder);
                        break;
                    }

                case VariantCompareOperator.GreaterThanOrEqualTo:
                    {
                        DisassembleExpressionOperand(inBlock, inExpression.LeftType, inExpression.Left, ioBuilder);
                        ioBuilder.Append(" >= ");
                        DisassembleExpressionOperand(inBlock, inExpression.RightType, inExpression.Right, ioBuilder);
                        break;
                    }

                case VariantCompareOperator.GreaterThan:
                    {
                        DisassembleExpressionOperand(inBlock, inExpression.LeftType, inExpression.Left, ioBuilder);
                        ioBuilder.Append(" > ");
                        DisassembleExpressionOperand(inBlock, inExpression.RightType, inExpression.Right, ioBuilder);
                        break;
                    }

                case VariantCompareOperator.Exists:
                    {
                        DisassembleExpressionOperand(inBlock, inExpression.LeftType, inExpression.Left, ioBuilder);
                        ioBuilder.Append(" exists");
                        break;
                    }

                case VariantCompareOperator.DoesNotExist:
                    {
                        DisassembleExpressionOperand(inBlock, inExpression.LeftType, inExpression.Left, ioBuilder);
                        ioBuilder.Append(" does not exist");
                        break;
                    }
            }
        }

        static private void DisassembleExpressionOperand(LeafInstructionBlock inBlock, LeafExpression.OperandType inType, LeafExpression.OperandData inOperandData, StringBuilder ioBuilder)
        {
            switch(inType)
            {
                case LeafExpression.OperandType.Value:
                    {
                        ioBuilder.Append(inOperandData.Value.ToDebugString());
                        break;
                    }

                case LeafExpression.OperandType.Read:
                    {
                        ioBuilder.Append(inOperandData.TableKey.ToDebugString());
                        break;
                    }

                case LeafExpression.OperandType.Method:
                    {
                        MethodCall method;
                        method.Id = inOperandData.MethodId;
                        method.Args = inOperandData.MethodArgsIndex == EmptyIndex ? null : inBlock.StringTable[inOperandData.MethodArgsIndex];

                        ioBuilder.Append(method.ToDebugString());
                        break;
                    }
            }
        }
    
        #else

        static internal void Disassemble(LeafInstructionBlock inBlock, uint inInstructionOffset, uint inInstructionLength, StringBuilder ioBuilder)
        {
            uint pc = inInstructionOffset;
            uint end = pc + inInstructionLength;

            ioBuilder.Append("(Instructions )").Append(pc.ToString("X4")).Append(" - ").Append(end.ToString("X4"))
                .Append(" (").Append(inInstructionLength).Append(" bytes)")
                .Append("\n Disassembly unavailable in non-DEVELOPMENT builds");;
        }

        #endif // DEVELOPMENT
    }
}