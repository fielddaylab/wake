/*
 * Copyright (C) 2020. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    27 June 2021
 * 
 * File:    LeafStringConverter.cs
 * Purpose: Leaf-specific string converter.
 */

using System;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;

namespace Leaf.Runtime
{
    public class LeafStringConverter : IStringConverter
    {
        public bool CanConvertTo(Type inType)
        {
            return LeafUtils.ActorType.IsAssignableFrom(inType) || StringParser.CanConvertTo(inType);
        }

        public bool TryConvertTo(StringSlice inData, Type inType, object inContext, out NonBoxedValue outObject)
        {
            LeafEvalContext context = LeafEvalContext.FromObject(inContext);
            return LeafUtils.TryParseArgument(context, inData, inType, out outObject);
        }

        public bool TryConvertToVariant(StringSlice inData, object inContext, out Variant outObject)
        {
            LeafEvalContext context = LeafEvalContext.FromObject(inContext);
            return LeafUtils.TryResolveVariant(context, inData, out outObject);
        }
    }
}