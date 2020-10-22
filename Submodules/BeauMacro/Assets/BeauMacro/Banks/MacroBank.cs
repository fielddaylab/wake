/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    MacroBank.cs
 * Purpose: Base class for macro storage/lookup.
 */

using System.Collections.Generic;

namespace BeauMacro
{
    /// <summary>
    /// String macro bank.
    /// </summary>
    public abstract class MacroBank
    {
        #region Internal

        internal abstract Dictionary<int, Macro> GetMacros();
        internal abstract string[] GetStrings();

        internal bool TryGetMacro(int inMacroID, out Macro outMacro)
        {
            return GetMacros().TryGetValue(inMacroID, out outMacro);
        }

        internal bool TryGetString(int inStringID, out string outString)
        {
            string[] strings = GetStrings();
            if (inStringID < 0 || inStringID >= strings.Length)
            {
                outString = string.Empty;
                return false;
            }

            outString = strings[inStringID];
            return true;
        }

        #endregion // Internal

        public bool Contains(string inKey)
        {
            return GetMacros().ContainsKey(MacroUtil.Hash(inKey));
        }

        public bool Contains(int inID)
        {
            return GetMacros().ContainsKey(inID);
        }

        public int MacroCount()
        {
            return GetMacros().Count;
        }
    }
}