/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    IMacroCompilerConfig.cs
 * Purpose: Optional configuration object for the macro compiler.
 */

using System.Collections.Generic;
using System.Text;

namespace BeauMacro
{
    /// <summary>
    /// Configuration for the macro compiler.
    /// </summary>
    public interface IMacroCompilerConfig
    {
        /// <summary>
        /// Enumerable containing pre-recognized macros.
        /// </summary>
        IEnumerable<string> RecognizedMacros { get; }

        /// <summary>
        /// Step to run before replace rules are executed.
        /// </summary>
        void PreReplace(StringBuilder ioString);

        /// <summary>
        /// Key-value pairs of simple replace rules.
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> SimpleReplace { get; }

        /// <summary>
        /// Key-value pairs of regular expression replace rules.
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> RegexReplace { get; }
        
        /// <summary>
        /// Step to run after replace rules are executed.
        /// </summary>
        void PostReplace(StringBuilder ioString);
    }
}