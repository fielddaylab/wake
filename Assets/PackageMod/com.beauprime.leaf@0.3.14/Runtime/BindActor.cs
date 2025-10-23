/*
 * Copyright (C) 2017-2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    27 June 2021
 * 
 * File:    BindActor.cs
 * Purpose: Attribute marking a leaf thread's "this" argument binding.
 */

using System;
using BeauUtil;

namespace Leaf.Runtime
{
    /// <summary>
    /// Binds the thread's "this" actor object.
    /// </summary>
    public class BindActorAttribute : BindThreadAttribute
    {
        public override object Bind(object inSource)
        {
            LeafThreadState thread = (LeafThreadState) base.Bind(inSource);
            return thread?.Actor;
        }
    }
}