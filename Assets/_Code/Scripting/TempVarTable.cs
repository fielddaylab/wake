using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using Aqua;
using UnityEngine;
using BeauUtil.Variants;

namespace Aqua.Scripting
{
    public struct TempVarTable : IDisposable
    {
        private readonly TempAlloc<VariantTable> m_Table;

        internal TempVarTable(TempAlloc<VariantTable> inTable)
        {
            m_Table = inTable;
        }

        public void Set(StringHash32 inId, Variant inValue)
        {
            m_Table.Object?.Set(inId, inValue);
        }

        void IDisposable.Dispose()
        {
            m_Table.Dispose();
        }

        static public implicit operator VariantTable(TempVarTable inTable)
        {
            return inTable.m_Table?.Object;
        }
    }
}