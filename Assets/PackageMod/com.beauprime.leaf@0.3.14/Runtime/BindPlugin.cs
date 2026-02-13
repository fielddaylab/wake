/*
 * Copyright (C) 2022. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 May 2022
 * 
 * File:    BindPlugin.cs
 * Purpose: Attribute marking a leaf plugin argument binding.
 */

using System;
using BeauUtil;

namespace Leaf.Runtime
{
    /// <summary>
    /// Binds the leaf call's plugin object.
    /// </summary>
    public class BindPluginAttribute : BindContextAttribute
    {
        public override object Bind(object inSource)
        {
            if (inSource is LeafEvalContext)
                return ((LeafEvalContext) inSource).Plugin;
            return (inSource as LeafThreadState)?.Plugin ?? inSource as ILeafPlugin;
        }
    }
}