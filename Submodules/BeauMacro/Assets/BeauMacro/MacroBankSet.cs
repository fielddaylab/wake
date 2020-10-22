/*
 * Copyright (C) 2017 - 2020. Filament Games, LLC. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    23 July 2019
 * 
 * File:    MacroBankSet.cs
 * Purpose: Set of macro banks. Used as context for the environment.
 */

namespace BeauMacro
{
    /// <summary>
    /// Set of string macro banks.
    /// Used as context for evaluation.
    /// </summary>
    public sealed class MacroBankSet
    {
        /// <summary>
        /// Global macros.
        /// </summary>
        public MacroBank Global;

        /// <summary>
        /// Localization macros. Overrides Global.
        /// </summary>
        public MacroBank LocalizationGlobal;

        /// <summary>
        /// Localization macros. Overrides LocalizationGlobal and Global.
        /// </summary>
        public MacroBank LocalizationLocal;

        /// <summary>
        /// Variable macros. Overrides LocalizationLocal, LocalizationGlobal, and Global.
        /// </summary>
        public MacroBank Variables;
    }
}