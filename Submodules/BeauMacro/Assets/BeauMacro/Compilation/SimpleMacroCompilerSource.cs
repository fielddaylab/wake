/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    SimpleMacroCompilerSource.cs
 * Purpose: Default implementation of macro source emitter.
 */

using System.Collections.Generic;

namespace BeauMacro
{
    /// <summary>
    /// Simple string format.
    /// </summary>
    public class SimpleMacroCompilerSource : IMacroCompilerSource
    {
        private readonly string m_Text;

        public SimpleMacroCompilerSource(string inText)
        {
            m_Text = inText;
        }

        IEnumerable<KeyValuePair<string, string>> IMacroCompilerSource.Entries
        {
            get
            {
                string[] split = m_Text.Split(SPLIT_CHARS, System.StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < split.Length; ++i)
                {
                    string line = split[i].TrimStart();
                    if (line.StartsWith("//"))
                        continue;
                    
                    int equalsIdx = line.IndexOf('=');
                    if (equalsIdx >= 0)
                    {
                        string key = line.Substring(0, equalsIdx).Trim();
                        string value = line.Substring(equalsIdx + 1).TrimStart().Replace("\\n", "\n").Replace("\\t", "\t");
                        yield return new KeyValuePair<string, string>(key, value);
                    }
                }
            }
        }

        static private readonly char[] SPLIT_CHARS = new char[] { '\r', '\n' };
    }
}