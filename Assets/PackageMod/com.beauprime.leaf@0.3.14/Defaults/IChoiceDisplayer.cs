/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    6 June 2021
 * 
 * File:    ITextDisplayer.cs
 * Purpose: Interface for displaying choices.
 */

using System.Collections;
using Leaf.Runtime;

namespace Leaf.Defaults
{
    /// <summary>
    /// Choice display interface.
    /// </summary>
    public interface IChoiceDisplayer
    {
        /// <summary>
        /// Displays a set of choices and allows the user to pick one.
        /// </summary>
        IEnumerator ShowChoice(LeafChoice inChoice, LeafThreadState inThread, ILeafPlugin inPlugin);
    }
}