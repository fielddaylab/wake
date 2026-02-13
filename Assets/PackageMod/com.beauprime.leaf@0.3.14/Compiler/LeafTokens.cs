/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    16 Feb 2021
 * 
 * File:    LeafTokens.cs
 * Purpose: Tokens used for parsing leaf files.
 */

using BeauUtil;

namespace Leaf.Compiler
{
    /// <summary>
    /// Tokens for parsing leaf files.
    /// </summary>
    static public class LeafTokens
    {
        static public readonly StringHash32 Answer = "answer";
        static public readonly StringHash32 Branch = "branch";
        static public readonly StringHash32 Break = "break";
        static public readonly StringHash32 Call = "call";
        static public readonly StringHash32 Choice = "choice";
        static public readonly StringHash32 Choose = "choose";
        static public readonly StringHash32 Continue = "continue";
        static public readonly StringHash32 Data = "data";
        static public readonly StringHash32 Else = "else";
        static public readonly StringHash32 ElseIf = "elseif";
        static public readonly StringHash32 EndIf = "endif";
        static public readonly StringHash32 EndWhile = "endwhile";
        static public readonly StringHash32 Fork = "fork";
        static public readonly StringHash32 Goto = "goto";
        static public readonly StringHash32 If = "if";
        static public readonly StringHash32 Include = "include";
        static public readonly StringHash32 Join = "join";
        static public readonly StringHash32 Loop = "loop";
        static public readonly StringHash32 Return = "return";
        static public readonly StringHash32 Set = "set";
        static public readonly StringHash32 Start = "start";
        static public readonly StringHash32 Stop = "stop";
        static public readonly StringHash32 While = "while";
        static public readonly StringHash32 Yield = "yield";

        static public readonly StringHash32 Const = "const";
        static public readonly StringHash32 Macro = "macro";
    }
}