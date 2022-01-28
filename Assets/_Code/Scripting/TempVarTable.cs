using System;
using BeauPools;
using BeauUtil;
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

        public void Dispose()
        {
            m_Table?.Dispose();
        }

        static public implicit operator VariantTable(TempVarTable inTable)
        {
            return inTable.m_Table?.Object;
        }

        static public TempVarTable Alloc()
        {
            return Services.Script.GetTempTable();
        }
    }
}