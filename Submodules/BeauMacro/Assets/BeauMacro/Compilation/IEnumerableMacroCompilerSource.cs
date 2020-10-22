/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 July 2019
 * 
 * File:    IEnumerableMacroCompilerSource.cs
 * Purpose: Wrapper implementation of a compiler source.
 */

using System.Collections.Generic;

namespace BeauMacro
{
    /// <summary>
    /// IEnumerable wrapper.
    /// </summary>
    public class IEnumerableMacroCompilerSource : IMacroCompilerSource
    {
        private readonly IEnumerable<KeyValuePair<string, string>> m_Source;

        public IEnumerableMacroCompilerSource(IEnumerable<KeyValuePair<string, string>> inSource)
        {
            m_Source = inSource;
        }

        IEnumerable<KeyValuePair<string, string>> IMacroCompilerSource.Entries
        {
            get
            {
                return m_Source;
            }
        }
    }
}