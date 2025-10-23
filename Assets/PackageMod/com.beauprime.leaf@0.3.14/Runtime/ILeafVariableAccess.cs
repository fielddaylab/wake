/*
 * Copyright (C) 2021. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    7 June 2021
 * 
 * File:    ILeafVariableAccess.cs
 * Purpose: Variable Access interface and extension methods.
 */

using System.Collections;
using BeauUtil;
using BeauUtil.Variants;
using Leaf.Runtime;
using UnityEngine;
using BeauUtil.Tags;
using BeauRoutine;
using System;
using BeauUtil.Debugger;

namespace Leaf.Runtime
{
    public interface ILeafVariableAccess
    {
        IVariantResolver Resolver { get; }
    }

    static public class ILeafVariableAccessExtensions
    {
        #region Variables

        /// <summary>
        /// Returns the variable with the given identifier.
        /// </summary>
        static public Variant GetVariable(this ILeafVariableAccess inAccess, StringSlice inIdentifier, object inContext = null)
        {
            TableKeyPair key;
            if (!TableKeyPair.TryParse(inIdentifier, out key))
            {
                Log.Error("[ILeafVariableAccess] Cannot parse variable identifier '{0}'", inIdentifier);
                return default(Variant);
            }

            Variant variant;
            inAccess.Resolver.TryResolve(inContext, TableKeyPair.Parse(inIdentifier), out variant);
            return variant;
        }

        /// <summary>
        /// Returns the variable with the given identifier.
        /// </summary>
        static public Variant GetVariable(this ILeafVariableAccess inAccess, TableKeyPair inIdentifier, object inContext = null)
        {
            Variant variant;
            inAccess.Resolver.TryResolve(inContext, inIdentifier, out variant);
            return variant;
        }

        /// <summary>
        /// Returns the variable with the given identifier.
        /// </summary>
        static public Variant TryGetVariable(this ILeafVariableAccess inAccess, StringSlice inIdentifier, object inContext, out Variant outValue)
        {
            TableKeyPair key;
            if (!TableKeyPair.TryParse(inIdentifier, out key))
            {
                Log.Error("[ILeafVariableAccess] Cannot parse variable identifier '{0}'", inIdentifier);
                outValue = default(Variant);
                return false;
            }

            return inAccess.Resolver.TryResolve(inContext, TableKeyPair.Parse(inIdentifier), out outValue);
        }

        /// <summary>
        /// Returns the variable with the given identifier.
        /// </summary>
        static public bool TryGetVariable(this ILeafVariableAccess inAccess, TableKeyPair inIdentifier, object inContext, out Variant outValue)
        {
            return inAccess.Resolver.TryResolve(inContext, inIdentifier, out outValue);
        }

        /// <summary>
        /// Sets the variable with the given identifier.
        /// </summary>
        static public void SetVariable(this ILeafVariableAccess inAccess, StringSlice inIdentifier, Variant inValue, object inContext = null)
        {
            TableKeyPair key;
            if (!TableKeyPair.TryParse(inIdentifier, out key))
            {
                Log.Error("[ILeafVariableAccess] Cannot parse variable identifier '{0}'", inIdentifier);
                return;
            }

            inAccess.Resolver.TryModify(inContext, key, VariantModifyOperator.Set, inValue);
        }

        /// <summary>
        /// Sets the variable with the given identifier.
        /// </summary>
        static public void SetVariable(this ILeafVariableAccess inAccess, TableKeyPair inIdentifier, Variant inValue, object inContext = null)
        {
            inAccess.Resolver.TryModify(inContext, inIdentifier, VariantModifyOperator.Set, inValue);
        }

        /// <summary>
        /// Increments the variable with the given identifier.
        /// </summary>
        static public void IncrementVariable(this ILeafVariableAccess inAccess, StringSlice inIdentifier, Variant inAmount, object inContext = null)
        {
            TableKeyPair key;
            if (!TableKeyPair.TryParse(inIdentifier, out key))
            {
                Log.Error("[ILeafVariableAccess] Cannot parse variable identifier '{0}'", inIdentifier);
                return;
            }

            inAccess.Resolver.TryModify(inContext, key, VariantModifyOperator.Add, inAmount);
        }

        /// <summary>
        /// Increments the variable with the given identifier.
        /// </summary>
        static public void IncrementVariable(this ILeafVariableAccess inAccess, TableKeyPair inIdentifier, Variant inAmount, object inContext = null)
        {
            inAccess.Resolver.TryModify(inContext, inIdentifier, VariantModifyOperator.Add, inAmount);
        }

        /// <summary>
        /// Retrieves a variable with the given identifier.
        /// If the value equals the old value, the variable is set to the new value.
        /// </summary>
        static public bool CompareExchange(this ILeafVariableAccess inAccess, StringSlice inIdentifier, Variant inOldValue, Variant inNewValue, object inContext = null)
        {
            TableKeyPair keyPair;
            if (!TableKeyPair.TryParse(inIdentifier, out keyPair))
            {
                Log.Error("[ILeafVariableAccess] Cannot parse variable identifier '{0}'", inIdentifier);
                return false;
            }

            Variant result = default(Variant);
            inAccess.Resolver.TryResolve(inContext, keyPair, out result);
            if (result == inOldValue)
            {
                SetVariable(inAccess, keyPair, inNewValue, inContext);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Retrieves a variable with the given identifier.
        /// If the value equals the old value, the variable is set to the new value.
        /// </summary>
        static public bool CompareExchange(this ILeafVariableAccess inAccess, TableKeyPair inIdentifier, Variant inOldValue, Variant inNewValue, object inContext = null)
        {
            Variant result = default(Variant);
            inAccess.Resolver.TryResolve(inContext, inIdentifier, out result);
            if (result == inOldValue)
            {
                SetVariable(inAccess, inIdentifier, inNewValue, inContext);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Returns if the conditions described by the string are true.
        /// If given an empty string, this will also return true.
        /// </summary>
        static public bool CheckConditions(this ILeafVariableAccess inAccess, StringSlice inConditions, object inContext = null, IMethodCache inInvoker = null)
        {
            return inAccess.Resolver.TryEvaluate(inContext, inConditions, inInvoker);
        }

        #endregion // Variables
    }
}