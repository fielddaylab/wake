/*
 * Copyright (C) 2017-2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    27 June 2021
 * 
 * File:    ILeafActor.cs
 * Purpose: Leaf actor interface.
 */

using BeauUtil;
using BeauUtil.Variants;

namespace Leaf.Runtime
{
    public interface ILeafActor
    {
        StringHash32 Id { get; }
        VariantTable Locals { get; }
    }
}