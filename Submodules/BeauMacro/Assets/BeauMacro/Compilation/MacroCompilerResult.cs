/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    MacroCompilerResult.cs
 * Purpose: Result of a macro compilation.
 */

using System.Collections.Generic;

namespace BeauMacro
{
    /// <summary>
    /// Result of macro compilation.
    /// </summary>
    public sealed class MacroCompilerResult
    {
        /// <summary>
        /// Resulting macro bank.
        /// </summary>
        public CompiledMacroBank Bank;

        /// <summary>
        /// Collection of referenced and unrecognized macros.
        /// </summary>
        public ICollection<string> UnrecognizedMacros;

        /// <summary>
        /// Collection of referenced and unrecognized macro ids.
        /// </summary>
        public ICollection<int> UnrecognizedMacroIds;

        /// <summary>
        /// Collection of compiled macro names.
        /// </summary>
        public ICollection<string> MacroNames;

        /// <summary>
        /// Total number of macros.
        /// </summary>
        public int MacroCount;

        /// <summary>
        /// Total number of strings.
        /// </summary>
        public int StringCount;

        /// <summary>
        /// Total number of times a string was reused.
        /// </summary>
        public int ReusedStrings;
    }
}