/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    MacroUtil.cs
 * Purpose: Macro utility functions.
 */

using UnityEngine;

namespace BeauMacro
{
    /// <summary>
    /// String macro utilities.
    /// </summary>
    static public class MacroUtil
    {
        /// <summary>
        /// Returns the hash value of the given string.
        /// </summary>
        static public int Hash(string inString)
        {
            return Animator.StringToHash(inString);
        }

        /// <summary>
        /// Returns if the given string is a macro string.
        /// Will modify the string into the macro name, and output the macro id.
        /// </summary>
        static public bool IsMacroKey(ref string ioKey, out int outStringHash)
        {
            if (string.IsNullOrEmpty(ioKey))
            {
                ioKey = string.Empty;
                outStringHash = 0;
                return false;
            }

            if (ioKey.Length >= 3 && ioKey[0] == '#' && ioKey[1] == '(' && ioKey[ioKey.Length - 1] == ')' && !Contains(ioKey, ')', 2, ioKey.Length - 3))
            {
                outStringHash = ParseInt(ioKey, 2, ioKey.Length - 3);
                ioKey = null;
                return true;
            }

            if (ioKey.Length >= 3 && ioKey[0] == '$' && ioKey[1] == '(' && ioKey[ioKey.Length - 1] == ')' && !Contains(ioKey, ')', 2, ioKey.Length - 3))
            {
                ioKey = ioKey.Substring(2, ioKey.Length - 3);
                outStringHash = Hash(ioKey);
                return true;
            }

            outStringHash = 0;
            return false;
        }

        /// <summary>
        /// Optimizes the given macro key.
        /// </summary>
        static public bool OptimizeKey(ref string ioKey)
        {
            string tempStr = ioKey;
            int tempHash;
            if (IsMacroKey(ref tempStr, out tempHash))
            {
                tempStr = "#(" + tempHash.ToString() + ")";
                if (tempStr != ioKey)
                {
                    ioKey = tempStr;
                    return true;
                }
            }

            return false;
        }

        #region Utils

        static private int ParseInt(string inString, int inIndex, int inLength)
        {
            if (inIndex >= inString.Length)
                return 0;

            int result = 0;
            bool bNegative = inString[inIndex] == '-';
            int stringLength = inString.Length;
            for (int i = bNegative ? 1 : 0; i < inLength && inIndex + i < stringLength; ++i)
            {
                int idx = inIndex + i;
                int digit = inString[idx] - '0';
                if (digit >= 0 && digit < 10)
                {
                    result *= 10;
                    result += digit;
                }
                else
                    break;
            }

            if (bNegative)
                result = -result;

            return result;
        }

        static private bool Contains(string inString, char inChar, int InIndex, int inLength)
        {
            if (InIndex >= inString.Length)
                return false;

            for (int i = 0; i < inLength; ++i)
            {
                int idx = InIndex + i;
                if (idx >= inString.Length)
                    return false;

                if (inString[idx] == inChar)
                    return true;
            }

            return false;
        }

        #endregion // Utils
    }
}