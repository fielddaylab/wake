/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    RuntimeMacroBank.cs
 * Purpose: Runtime-editable bank of macro keys and variables.
 */

using System;
using System.Collections.Generic;

namespace BeauMacro
{
    /// <summary>
    /// Runtime string macro bank.
    /// </summary>
    public sealed class RuntimeMacroBank : MacroBank
    {
        private const string TRUE_STRING = "t";
        private const string FALSE_STRING = "";

        private Dictionary<int, Macro> m_MacroDictionary;
        private string[] m_StringEntries;
        private int[] m_StringRefCount;

        public RuntimeMacroBank()
        {
            m_MacroDictionary = new Dictionary<int, Macro>();
            m_StringEntries = new string[4] { FALSE_STRING, TRUE_STRING, null, null };
            m_StringRefCount = new int[4] { 1, 1, 0, 0 };
        }

        #region Public Interface

        public void Clear()
        {
            m_MacroDictionary.Clear();
            for (int i = 0; i < m_StringEntries.Length; ++i)
            {
                m_StringEntries[i] = null;
                m_StringRefCount[i] = 0;
            }
        }

        public void CopyTo(RuntimeMacroBank inBank)
        {
            inBank.m_MacroDictionary = new Dictionary<int, Macro>(m_MacroDictionary);
            inBank.m_StringEntries = (string[]) m_StringEntries.Clone();
            inBank.m_StringRefCount = (int[]) m_StringRefCount.Clone();
        }

        public void Set(string inKey, string inValue)
        {
            int macroID = MacroUtil.Hash(inKey);
            PurgeStringRefsForMacro(macroID);
            m_MacroDictionary[macroID] = CompileSimpleMacro(inValue);
        }

        public void Set(int inID, string inValue)
        {
            PurgeStringRefsForMacro(inID);
            m_MacroDictionary[inID] = CompileSimpleMacro(inValue);
        }

        public void Set(string inKey, bool inbValue)
        {
            int macroID = MacroUtil.Hash(inKey);
            PurgeStringRefsForMacro(macroID);
            m_MacroDictionary[macroID] = CompileBoolMacro(inbValue);
        }

        public void Set(int inID, bool inbValue)
        {
            PurgeStringRefsForMacro(inID);
            m_MacroDictionary[inID] = CompileBoolMacro(inbValue);
        }

        public void Remove(string inKey)
        {
            int macroID = MacroUtil.Hash(inKey);
            PurgeStringRefsForMacro(macroID);
            m_MacroDictionary.Remove(MacroUtil.Hash(inKey));
        }

        public void Remove(int inID)
        {
            PurgeStringRefsForMacro(inID);
            m_MacroDictionary.Remove(inID);
        }

        #endregion // Public Interface

        #region String Management

        [ThreadStatic]
        static private List<int> s_ReferencedStringIds;

        private void PurgeStringRefsForMacro(int inMacroID)
        {
            if (s_ReferencedStringIds == null)
                s_ReferencedStringIds = new List<int>();

            s_ReferencedStringIds.Clear();
            Macro macro;
            if (m_MacroDictionary.TryGetValue(inMacroID, out macro))
            {
                macro.GetReferencedStringIds(ref s_ReferencedStringIds);
                for (int i = 0; i < s_ReferencedStringIds.Count; ++i)
                    RemoveString(s_ReferencedStringIds[i]);
                s_ReferencedStringIds.Clear();
            }
        }

        private int FindSlotForString(string inString, out bool outNew)
        {
            int firstOpenIndex = -1;
            for (int i = 0; i < m_StringRefCount.Length; ++i)
            {
                if (m_StringEntries[i] == inString)
                {
                    outNew = false;
                    return i;
                }
                else if (firstOpenIndex < 0 && m_StringEntries[i] == null && m_StringRefCount[i] == 0)
                    firstOpenIndex = i;
            }

            outNew = true;

            if (firstOpenIndex >= 0)
                return firstOpenIndex;

            int oldSize = m_StringEntries.Length;
            Array.Resize(ref m_StringEntries, oldSize * 2);
            Array.Resize(ref m_StringRefCount, m_StringEntries.Length);
            return oldSize;
        }

        private int AddString(string inString)
        {
            bool bNew;
            int slot = FindSlotForString(inString, out bNew);
            if (bNew)
            {
                m_StringEntries[slot] = inString;
                m_StringRefCount[slot] = 1;
            }
            else
            {
                m_StringRefCount[slot] = m_StringRefCount[slot] + 1;
            }

            return slot;
        }

        private void RemoveString(int inStringID)
        {
            if (--m_StringRefCount[inStringID] <= 0)
            {
                m_StringRefCount[inStringID] = 0;
                m_StringEntries[inStringID] = null;
            }
        }

        #endregion // String Management

        #region Compilation

        private Macro CompileSimpleMacro(string inString)
        {
            int macroId;
            if (MacroUtil.IsMacroKey(ref inString, out macroId))
                return new Macro(new CompilerInstruction(OpCode.CallMacroDirect, macroId));
            return new Macro(new CompilerInstruction(OpCode.AppendStringDirect, AddString(inString)));
        }

        private Macro CompileBoolMacro(bool inbValue)
        {
            return new Macro(new CompilerInstruction(OpCode.AppendStringDirect, AddString(inbValue ? TRUE_STRING : FALSE_STRING)));
        }

        #endregion // Compilation

        #region MacroBank

        internal override Dictionary<int, Macro> GetMacros()
        {
            return m_MacroDictionary;
        }

        internal override string[] GetStrings()
        {
            return m_StringEntries;
        }

        #endregion // MacroBank
    }
}