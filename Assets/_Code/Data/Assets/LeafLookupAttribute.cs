using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using TMPro;
using UnityEngine;

namespace Aqua {
    /// <summary>
    /// Marks a property as retrievable via leaf
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method)]
    public class LeafLookupAttribute : ExposedAttribute {
        public LeafLookupAttribute() : base() { }
        public LeafLookupAttribute(string name) : base(name) { }
    }
}