using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Variants;
using ProtoAqua.Profile;
using ProtoAqua.Scripting;
using UnityEngine;

namespace ProtoAqua
{
    public partial class DataService : ServiceBehaviour
    {
        public IVariantResolver VariableResolver { get { return m_VariableResolver; } }

        #region Variables

        /// <summary>
        /// Retrieves a variable with the given identifier and an optional context.
        /// </summary>
        public Variant GetVariable(StringSlice inId, object inContext = null)
        {
            TableKeyPair keyPair;
            Variant result = default(Variant);
            if (TableKeyPair.TryParse(inId, out keyPair))
            {
                VariableResolver.TryResolve(inContext, keyPair, out result);
            }
            return result;
        }

        /// <summary>
        /// Retrieves a variable with the given identifier and an optional context.
        /// </summary>
        public Variant GetVariable(TableKeyPair inId, object inContext = null)
        {
            Variant result = default(Variant);
            VariableResolver.TryResolve(inContext, inId, out result);
            return result;
        }

        /// <summary>
        /// Sets a variable with the given identifier and an optional context.
        /// </summary>
        public void SetVariable(StringSlice inId, Variant inValue, object inContext = null)
        {
            TableKeyPair keyPair;
            if (!TableKeyPair.TryParse(inId, out keyPair)
                || !VariableResolver.TryModify(inContext, keyPair, VariantModifyOperator.Set, inValue))
            {
                Debug.LogErrorFormat("[DataService] Unable to set variable '{0}' to {1}", inId, inValue.ToDebugString());
            }
        }

        /// <summary>
        /// Sets a variable with the given identifier and an optional context.
        /// </summary>
        public void SetVariable(TableKeyPair inId, Variant inValue, object inContext = null)
        {
            if (!VariableResolver.TryModify(inContext, inId, VariantModifyOperator.Set, inValue))
            {
                Debug.LogErrorFormat("[DataService] Unable to set variable '{0}' to {1}", inId.ToDebugString(), inValue.ToDebugString());
            }
        }

        /// <summary>
        /// Adds to a variable with the given identifier and an optional context.
        /// </summary>
        public void AddVariable(StringSlice inId, Variant inValue, object inContext = null)
        {
            TableKeyPair keyPair;
            if (!TableKeyPair.TryParse(inId, out keyPair)
                || !VariableResolver.TryModify(inContext, keyPair, VariantModifyOperator.Add, inValue))
            {
                Debug.LogErrorFormat("[DataService] Unable to add variable '{0}' to {1}", inId, inValue.ToDebugString());
            }
        }

        /// <summary>
        /// Adds to a variable with the given identifier and an optional context.
        /// </summary>
        public void AddVariable(TableKeyPair inId, Variant inValue, object inContext = null)
        {
            if (!VariableResolver.TryModify(inContext, inId, VariantModifyOperator.Add, inValue))
            {
                Debug.LogErrorFormat("[DataService] Unable to add variable '{0}' to {1}", inId.ToDebugString(), inValue.ToDebugString());
            }
        }

        #endregion // Variables

        #region Tables

        /// <summary>
        /// Binds a table.
        /// </summary>
        public void BindTable(StringHash32 inId, VariantTable inTable)
        {
            m_VariableResolver.SetTable(inId, inTable);
            Debug.LogFormat("[DataService] Bound table '{0}'", inId.ToDebugString());
        }

        /// <summary>
        /// Unbinds a table.
        /// </summary>
        public void UnbindTable(StringHash32 inId)
        {
            m_VariableResolver.ClearTable(inId);
            Debug.LogFormat("[DataService] Unbound table '{0}'", inId.ToDebugString());
        }

        #endregion // Tables

        #region Conditions

        /// <summary>
        /// Checks if the given conditions are true.
        /// If empty, will also return true.
        /// </summary>
        public bool CheckConditions(StringSlice inConditions, object inContext = null)
        {
            return VariableResolver.TryEvaluate(inContext, inConditions);
        }

        #endregion // Conditions

        private void InitVariableResolver()
        {
            m_VariableResolver = new CustomVariantResolver();

            m_VariableResolver.SetVar(new TableKeyPair("date", "weekday"), GetDayOfWeek);
            m_VariableResolver.SetVar(new TableKeyPair("player", "gender"), GetPlayerGender);
        }

        private void HookSaveDataToVariableResolver(SaveData inData)
        {
            m_VariableResolver.SetTable("global", inData.Script.GlobalTable);
            m_VariableResolver.SetTable("player", inData.Script.PlayerTable);
            m_VariableResolver.SetTable("kevin", inData.Script.PartnerTable);

            m_VariableResolver.SetTableVar("scanned", (s) => inData.Inventory.WasScanned(s));
            m_VariableResolver.SetTableVar("seen", (s) => inData.Script.HasSeen(s, PersistenceLevel.Profile));
        }

        #region Callbacks

        static private Variant GetDayOfWeek()
        {
            switch(DateTime.Now.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return "s";
                case DayOfWeek.Monday:
                    return "m";
                case DayOfWeek.Tuesday:
                    return "t";
                case DayOfWeek.Wednesday:
                    return "w";
                case DayOfWeek.Thursday:
                    return "th";
                case DayOfWeek.Friday:
                    return "f";
                case DayOfWeek.Saturday:
                    return "sa";
                default:
                    return Variant.Null;
            }
        }

        private Variant GetPlayerGender()
        {
            switch(CurrentCharacterPronouns())
            {
                case Pronouns.Masculine:
                    return "m";
                case Pronouns.Feminine:
                    return "f";
                default:
                    return "x";
            }
        }

        #endregion // Callbacks
    }
}