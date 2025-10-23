/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    6 June 2021
 * 
 * File:    ITextDisplayer.cs
 * Purpose: Interface for displaying text.
 */

using System.Collections;
using BeauUtil.Tags;

namespace Leaf.Defaults
{
    /// <summary>
    /// Text display interface.
    /// </summary>
    public interface ITextDisplayer
    {
        /// <summary>
        /// Prepares the given line to be displayed.
        /// </summary>
        /// <returns>A TagStringEventHandler to override event handling.</returns>
        TagStringEventHandler PrepareLine(TagString inString, TagStringEventHandler inBaseHandler);

        /// <summary>
        /// Types out a portion of the given line.
        /// </summary>
        IEnumerator TypeLine(TagString inSourceString, TagTextData inType);
        
        /// <summary>
        /// Completes the current line.
        /// Any "wait for input before continuing" should go here.
        /// </summary>
        IEnumerator CompleteLine();
    }
}