/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    25 Oct 2020
 * 
 * File:    ILeafCompilerPlugin.cs
 * Purpose: Plugin interface for the LeafCompiler.
 */

namespace Leaf.Compiler
{
    /// <summary>
    /// Plugin for the compiler.
    /// </summary>
    public interface ILeafCompilerPlugin
    {
        char PathSeparator { get; }
        LeafCompilerFlags CompilerFlags { get; }
    }
}