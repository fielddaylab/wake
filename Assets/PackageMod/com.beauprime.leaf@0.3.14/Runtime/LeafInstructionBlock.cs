/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    31 Oct 2021
 * 
 * File:    LeafInstructionBlock.cs
 * Purpose: Instruction block.
 */

using BeauUtil;

namespace Leaf.Runtime
{
    public struct LeafInstructionBlock
    #if USING_BEAUDATA
        : BeauData.ISerializedObject
    #endif // USING_BEAUDATA
    {
        internal byte[] InstructionStream;
        internal string[] StringTable;
        internal LeafExpression[] ExpressionTable;

        #if USING_BEAUDATA

        public void Serialize(BeauData.Serializer ioSerializer)
        {
            ioSerializer.Binary("instructionStream", ref InstructionStream);
            ioSerializer.Array("stringTable", ref StringTable);
            ioSerializer.ObjectArray("expressionTable", ref ExpressionTable);
        }

        #endif // USING_BEAUDATA

        /// <summary>
        /// Calculates the approximate memory usage of the given LeafInstructionBlock.
        /// </summary>
        static public long CalculateMemoryUsage(LeafInstructionBlock inBlock)
        {
            // uninitialized block
            if (inBlock.InstructionStream == null || inBlock.StringTable == null || inBlock.ExpressionTable == null)
            {
                return 0;
            }

            long size = 0;
            size += inBlock.InstructionStream.Length;
            size += Unsafe.SizeOf<LeafExpression>() * inBlock.ExpressionTable.Length;
            size += (4 + Unsafe.PointerSize) * inBlock.StringTable.Length;

            StringSlice str;
            for(int i = 0; i < inBlock.StringTable.Length; i++)
            {
                str = inBlock.StringTable[i];
                size += sizeof(char) * str.Length;
            }

            return size;
        }
    }
}