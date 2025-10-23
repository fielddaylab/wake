/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    17 Feb 2021
 * 
 * File:    LeafMember.cs
 * Purpose: Leaf member attribute.
 */

using BeauUtil;

namespace Leaf.Runtime
{
    public class LeafMember : ExposedAttribute
    {
        public LeafMember() { }
        
        public LeafMember(string inName)
            : base(inName)
        {
        }

        static public MethodCache<LeafMember> CreateCache()
        {
            return new MethodCache<LeafMember>();
        }
    }
}