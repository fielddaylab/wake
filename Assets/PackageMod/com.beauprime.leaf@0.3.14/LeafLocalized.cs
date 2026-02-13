/*
 * Copyright (C) 2022. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    17 Jan 2022
 * 
 * File:    LeafLocalized.cs
 * Purpose: Localized string attribute.
 */

using System;

namespace Leaf.Runtime
{
    /// <summary>
    /// Attribute indicating that a passed string or StringSlice parameter should be localized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class LeafLocalized : Attribute { }
}