/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 Oct 2020
 * 
 * File:    LeafNode.cs
 * Purpose: Abstract Leaf node.
 */

using BeauUtil;
using BeauUtil.Blocks;
using Leaf.Runtime;

namespace Leaf
{
    /// <summary>
    /// Leaf node.
    /// </summary>
    public class LeafNode : IDataBlock
    {
        protected StringHash32 m_Id;
        protected LeafNodePackage m_Package;
        
        internal uint m_InstructionOffset;
        internal uint m_InstructionCount;

        public StringHash32 Id() { return m_Id; }
        public LeafNodePackage Package() { return m_Package; }

        public LeafNode(StringHash32 inId, LeafNodePackage inPackage)
        {
            m_Id = inId;
            m_Package = inPackage;
        }

        internal void SetInstructionOffsets(uint inOffset, uint inCount)
        {
            m_InstructionOffset = inOffset;
            m_InstructionCount = inCount;
        }
    }
}