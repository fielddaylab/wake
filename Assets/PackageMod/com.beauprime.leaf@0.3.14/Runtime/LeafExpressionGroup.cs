/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    29 Jan 2022
 * 
 * File:    LeafExpressionGroup.cs
 * Purpose: Block of conditions
 */

using BeauUtil;
using BeauUtil.Variants;
using System.Text;

namespace Leaf.Runtime {
    /// <summary>
    /// Group of expressions.
    /// </summary>
    public struct LeafExpressionGroup : IDebugString
    {
        internal uint m_Offset;
        internal ushort m_Count;
        internal LeafExpression.TypeFlags m_Type;
        internal LeafNodePackage m_Package;

        public int Count
        {
            get { return m_Count; }
        }

        public Variant Evaluate(LeafEvalContext inContext)
        {
            if (m_Count == 0)
                return Variant.True;

            var expTable = m_Package.m_Instructions.ExpressionTable;
            var stringTable = m_Package.m_Instructions.StringTable;

            if (m_Count == 1)
                return LeafRuntime.EvaluateValueExpression(inContext, ref expTable[m_Offset], stringTable);

            // TODO: Implement "type"
            for(int i = 0; i < m_Count; i++)
            {
                if (!LeafRuntime.EvaluateLogicalExpression(inContext, ref expTable[m_Offset + i], stringTable))
                    return Variant.False;
            }

            return Variant.True;
        }

        public Variant Evaluate(LeafEvalContext inContext, out LeafExpression outFailure)
        {
            if (m_Count == 0)
            {
                outFailure = default;
                return Variant.True;
            }

            var expTable = m_Package.m_Instructions.ExpressionTable;
            var stringTable = m_Package.m_Instructions.StringTable;

            if (m_Count == 1)
            {
                Variant value = LeafRuntime.EvaluateValueExpression(inContext, ref expTable[m_Offset], stringTable);
                if (!value.AsBool())
                    outFailure = expTable[m_Offset];
                else
                    outFailure = default;
                return value;
            }

            // TODO: Implement "type"
            for(int i = 0; i < m_Count; i++)
            {
                if (!LeafRuntime.EvaluateLogicalExpression(inContext, ref expTable[m_Offset + i], stringTable))
                {
                    outFailure = expTable[m_Offset + 1];
                    return Variant.False;
                }
            }

            outFailure = default;
            return Variant.True;
        }

        public string ToDebugString()
        {
            if (m_Package == null || m_Count == 0)
                return string.Empty;

            #if DEVELOPMENT
            StringBuilder builder = new StringBuilder();
            LeafInstruction.DisassembleExpressionGroup(m_Package.m_Instructions, this, builder);
            return builder.Flush();
            #else
            return "LeafExpressionGroup cannot be decompiled in non-development builds";
            #endif // DEVELOPMENT
        }
    }
}