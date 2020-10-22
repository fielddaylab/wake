/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    IMacroCompilerSource.cs
 * Purpose: Source object for the macro compiler. Emits key-value pairs.
 */

using System.Collections.Generic;

namespace BeauMacro
{
    /// <summary>
    /// Data source for macro compilation.
    /// </summary>
    public interface IMacroCompilerSource
    {
        /// <summary>
        /// Enumerable containing key-value pairs of macro names and strings to compile.
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> Entries { get; }
    }
}