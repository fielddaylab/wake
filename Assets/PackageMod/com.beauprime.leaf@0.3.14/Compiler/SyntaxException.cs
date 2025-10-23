/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    27 June 2021
 * 
 * File:    SyntaxException.cs
 * Purpose: Syntex exception
 */

using System;
using BeauUtil.Blocks;
using BeauUtil.Debugger;

namespace Leaf.Compiler
{
    public class SyntaxException : Exception
    {
        public SyntaxException(BlockFilePosition inPosition, string inMessage)
            : base(Log.Format("Syntax Error at {0}: {1}", inPosition, inMessage))
        { }

        public SyntaxException(BlockFilePosition inPosition, string inMessage, params object[] inArgs)
            : this(inPosition, Log.Format(inMessage, inArgs))
        { }
    }
}