/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    MacroEnvironment.cs
 * Purpose: Environment for macro evaluation.
 */

namespace BeauMacro
{
    /// <summary>
    /// String macro evaluation environment.
    /// </summary>
    public sealed class MacroEnvironment
    {
        /// <summary>
        /// Set of MacroBanks to use as context.
        /// </summary>
        public readonly MacroBankSet Banks;

        // evaluation runtime
        private readonly MacroRuntime m_Runtime;

        public MacroEnvironment()
        {
            Banks = new MacroBankSet();
            m_Runtime = new MacroRuntime();
        }

        #region Evaluation

        /// <summary>
        /// Attempts to evaluate the given string, with an optional context override.
        /// </summary>
        public bool TryEvaluate(ref string ioString, MacroBank inContext = null)
        {
            int hash;
            if (MacroUtil.IsMacroKey(ref ioString, out hash))
            {
                bool bSuccess;
                if (ioString == null)
                    ioString = m_Runtime.Evaluate(hash, Banks, inContext, out bSuccess);
                else
                    ioString = m_Runtime.Evaluate(ioString, Banks, inContext, out bSuccess);
                return bSuccess;
            }

            return true;
        }

        /// <summary>
        /// Attempts to evaluate the macro with the given id, with an optional context override.
        /// </summary>
        public bool TryEvaluate(int inMacroId, out string outValue, MacroBank inContext = null)
        {
            bool bSuccess;
            outValue = m_Runtime.Evaluate(inMacroId, Banks, inContext, out bSuccess);
            return bSuccess;
        }

        /// <summary>
        /// Evaluates the given string, with an optional context override.
        /// </summary>
        public string Evaluate(string inString, MacroBank inContext = null)
        {
            string str = inString;
            TryEvaluate(ref str, inContext);
            return str;
        }

        /// <summary>
        /// Evaluates the macro with the given id, with an optional context override.
        /// </summary>
        public string Evaluate(int inMacroId, MacroBank inContext = null)
        {
            string macro;
            TryEvaluate(inMacroId, out macro, inContext);
            return macro;
        }

        #endregion // Evaluation
    }
}