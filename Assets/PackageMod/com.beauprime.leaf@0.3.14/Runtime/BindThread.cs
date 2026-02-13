/*
 * Copyright (C) 2022. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    30 Jan 2022
 * 
 * File:    BindThread.cs
 * Purpose: Attribute marking a leaf thread argument binding.
 */

using System;
using BeauUtil;

namespace Leaf.Runtime
{
    /// <summary>
    /// Binds the leaf call's thread object.
    /// </summary>
    public class BindThreadAttribute : BindContextAttribute
    {
        public override object Bind(object inSource)
        {
            if (inSource is LeafEvalContext)
                return ((LeafEvalContext) inSource).Thread;
            return inSource as LeafThreadState;
        }
    }
}