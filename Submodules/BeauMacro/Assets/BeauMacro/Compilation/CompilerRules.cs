/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    24 July 2019
 * 
 * File:    CompilerRules.cs
 * Purpose: Common replace and regular expression rules.
 */

using Pair = System.Collections.Generic.KeyValuePair<string, string>;

namespace BeauMacro
{
    /// <summary>
    /// Common compiler rules.
    /// </summary>
    static public class CompilerRules
    {
        /// <summary>
        /// Generates a key-value pair for the given find-replace rule.
        /// </summary>
        static public Pair Rule(string inFind, string inReplace)
        {
            return new Pair(inFind, inReplace);
        }

        /// <summary>
        /// Regular expression rules.
        /// </summary>
        static public class RegexReplace
        {
            #region Macros

            /// <summary>
            /// Replaces {MACRO} with $(MACRO)
            /// </summary>
            static public readonly Pair CurlyBraceMacros = CustomMacros("{", "}");

            /// <summary>
            /// Replaces [MACRO] with $(MACRO)
            /// </summary>
            static public readonly Pair BracketMacros = CustomMacros("[", "]");

            /// <summary>
            /// Replaces OPEN MACRO CLOSE with $(MACRO)
            /// </summary>
            static public Pair CustomMacros(string inOpen, string inClose)
            {
                return new Pair(string.Format(@"{0}{1}{2}", Escape(inOpen), AnyNonWhitespaceCaptureGroup, Escape(inClose)), @"$($1)");
            }

            #endregion // Macros

            #region Toggle Groups

            /// <summary>
            /// Replaces [A] with ?(if VAR_A)A?(endif)
            /// </summary>
            static public Pair BracketConditional(string inVariable)
            {
                return CustomConditional("[", "]", inVariable, null);
            }

            /// <summary>
            /// Replaces [A|B] with ?(if VAR_A)A?(else)B?(endif)
            /// </summary>
            static public Pair BracketToggle(string inVariableA)
            {
                return CustomToggle("[", "|", "]", inVariableA, null, null);
            }

            /// <summary>
            /// Replaces [A|B|C] with ?(if VAR_A)A?(elif VAR_B)B?(else)C?(endif)
            /// </summary>
            static public Pair BracketTriad(string inVariableA, string inVariableB)
            {
                return CustomTriad("[", "|", "]", inVariableA, null, inVariableB, null, null);
            }

            /// <summary>
            /// Replaces P[A] with ?(if VAR_A)A?(endif)
            /// </summary>
            static public Pair PrefixedBracketConditional(string inPrefix, string inVariable)
            {
                return CustomConditional(inPrefix + "[", "]", inVariable, null);
            }

            /// <summary>
            /// Replaces P[A|B] with ?(if VAR_A)A?(else)B?(endif)
            /// </summary>
            static public Pair PrefixedBracketToggle(string inPrefix, string inVariableA)
            {
                return CustomToggle(inPrefix + "[", "|", "]", inVariableA, null, null);
            }

            /// <summary>
            /// Replaces P[A|B|C] with ?(if VAR_A)A?(elif VAR_B)B?(else)C?(endif)
            /// </summary>
            static public Pair PrefixedBracketTriad(string inPrefix, string inVariableA, string inVariableB)
            {
                return CustomTriad(inPrefix + "[", "|", "]", inVariableA, null, inVariableB, null, null);
            }

            /// <summary>
            /// Replaces PREFIX OPEN MACRO CLOSE with a conditional based on variable.
            /// </summary>
            static public Pair CustomConditional(string inOpen, string inClose, string inVariable, string inFormat)
            {
                if (string.IsNullOrEmpty(inFormat))
                    inFormat = "{0}";
                string find = string.Format(@"{0}{1}{2}", Escape(inOpen), AnyCaptureGroup, Escape(inClose));
                string replace = string.Format("?(if {0}){1}?(endif)", inVariable, string.Format(inFormat, "$1"));
                return new Pair(find, replace);
            }

            /// <summary>
            /// Replaces PREFIX OPEN MACRO SEPARATOR MACRO CLOSE with a toggle based on variable.
            /// </summary>
            static public Pair CustomToggle(string inOpen, string inSeparator, string inClose, string inVariableA, string inFormatA, string inFormatB)
            {
                if (string.IsNullOrEmpty(inFormatA))
                    inFormatA = "{0}";
                if (string.IsNullOrEmpty(inFormatB))
                    inFormatB = "{0}";
                string find = string.Format(@"{0}{1}{2}{1}{3}", Escape(inOpen), AnyCaptureGroup, Escape(inSeparator), Escape(inClose));
                string replace = string.Format("?(if {0}){1}?(else){2}?(endif)", inVariableA, string.Format(inFormatA, "$1"), string.Format(inFormatB, "$2"));
                return new Pair(find, replace);
            }

            /// <summary>
            /// Replaces PREFIX OPEN MACRO SEPARATOR MACRO SEPARATOR MACRO CLOSE with a branch based on two variables.
            /// </summary>
            static public Pair CustomTriad(string inOpen, string inSeparator, string inClose, string inVariableA, string inFormatA, string inVariableB, string inFormatB, string inFormatC)
            {
                if (string.IsNullOrEmpty(inFormatA))
                    inFormatA = "{0}";
                if (string.IsNullOrEmpty(inFormatB))
                    inFormatB = "{0}";
                if (string.IsNullOrEmpty(inFormatC))
                    inFormatC = "{0}";
                string find = string.Format(@"{0}{1}{2}{1}{2}{1}{3}", Escape(inOpen), AnyCaptureGroup, Escape(inSeparator), Escape(inClose));
                string replace = string.Format("?(if {0}){1}?(elif {2}){3}?(else){4}?(endif)", inVariableA, string.Format(inFormatA, "$1"), inVariableB, string.Format(inFormatB, "$2"), string.Format(inFormatC, "$3"));
                return new Pair(find, replace);
            }

            #endregion // Toggle Groups

            #region Regex

            /// <summary>
            /// Captures a sequence of non-whitespace characters.
            /// </summary>
            public const string AnyNonWhitespaceCaptureGroup = @"([^\s\\]*?)";

            /// <summary>
            /// Captures a sequence of any characters.
            /// </summary>
            public const string AnyCaptureGroup = @"(.*?)";

            /// <summary>
            /// Escapes the given string to work as a direct match in regex.
            /// </summary>
            static public string Escape(string inString)
            {
                return System.Text.RegularExpressions.Regex.Escape(inString);
            }

            #endregion // Regex
        }
    }
}