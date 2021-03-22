using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Variants;
using Aqua.Profile;
using Aqua.Scripting;
using UnityEngine;
using Aqua.Debugging;

namespace Aqua
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
        /// Pops a variable with the given identifier.
        /// </summary>
        public Variant PopVariable(TableKeyPair inId, object inContext = null)
        {
            Variant result = GetVariable(inId, inContext);
            SetVariable(inId, null, inContext);
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
            DebugService.Log(LogMask.Loading | LogMask.DataService, "[DataService] Bound table '{0}'", inId.ToDebugString());
        }

        /// <summary>
        /// Unbinds a table.
        /// </summary>
        public void UnbindTable(StringHash32 inId)
        {
            m_VariableResolver.ClearTable(inId);
            DebugService.Log(LogMask.Loading | LogMask.DataService, "[DataService] Unbound table '{0}'", inId.ToDebugString());
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

            m_VariableResolver.SetVar(GameVars.Weekday, GetDayOfWeek);
            m_VariableResolver.SetVar(GameVars.SceneName, GetSceneName);

            m_VariableResolver.SetVar(GameVars.PlayerGender, GetPlayerGender);
            m_VariableResolver.SetVar(GameVars.CurrentJob, GetJobId);
            m_VariableResolver.SetVar(GameVars.CurrentStation, GetStationId);
            m_VariableResolver.SetVar(GameVars.ActNumber, GetActNumber);

            m_VariableResolver.SetTableVar("random", GetRandomByRarity);
        }

        private void HookSaveDataToVariableResolver(SaveData inData)
        {
            m_VariableResolver.SetTable("global", inData.Script.GlobalTable);
            m_VariableResolver.SetTable("jobs", inData.Script.JobsTable);
            m_VariableResolver.SetTable("player", inData.Script.PlayerTable);
            m_VariableResolver.SetTable("kevin", inData.Script.PartnerTable);

            m_VariableResolver.SetTableVar("scanned", (s) => inData.Inventory.WasScanned(s));
            m_VariableResolver.SetTableVar("has.entity", (s) => inData.Bestiary.HasEntity(s));
            m_VariableResolver.SetTableVar("has.fact", (s) => inData.Bestiary.HasFact(s));
            m_VariableResolver.SetTableVar("has.item", (s) => inData.Inventory.HasItem(s));
            m_VariableResolver.SetTableVar("item.count", (s) => inData.Inventory.ItemCount(s));
            m_VariableResolver.SetTableVar("seen", (s) => inData.Script.HasSeen(s, PersistenceLevel.Profile));
            m_VariableResolver.SetTableVar("job.isStartedOrComplete", (s) => inData.Jobs.IsStartedOrComplete(s));
            m_VariableResolver.SetTableVar("job.inProgress", (s) => inData.Jobs.IsInProgress(s));
            m_VariableResolver.SetTableVar("job.isComplete", (s) => inData.Jobs.IsComplete(s));
            m_VariableResolver.SetTableVar("job.isAvailable", (s) => Services.Assets.Jobs.IsAvailableAndUnstarted(s));
            m_VariableResolver.SetTableVar("job.anyAvailable", (s) => Services.Assets.Jobs.HasUnstartedJobs());
            m_VariableResolver.SetTableVar("job.anyInProgress", (s) => inData.Jobs.InProgressJobs().Length > 0);
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

        private Variant GetSceneName()
        {
            return SceneHelper.ActiveScene().Name;
        }

        private Variant GetJobId()
        {
            return Profile.Jobs.CurrentJobId;
        }

        private Variant GetStationId()
        {
            return Profile.Map.CurrentStationId();
        }

        private Variant GetActNumber()
        {
            return Profile.Script.ActIndex;
        }

        private Variant GetRandomByRarity(StringHash32 inId)
        {
            if (inId == RandomRare)
                return RNG.Instance.Chance(m_RareChance);
            if (inId == RandomUncommon)
                return RNG.Instance.Chance(m_UncommonChance);
            if (inId == RandomCommon)
                return RNG.Instance.Chance(m_CommonChance);
            
            UnityEngine.Debug.LogErrorFormat("[DataService] Unknown rarity '{0}'", inId.ToDebugString());
            return false;
        }

        static private readonly StringHash32 RandomRare = "rare";
        static private readonly StringHash32 RandomUncommon = "uncommon";
        static private readonly StringHash32 RandomCommon = "common";

        #endregion // Callbacks
    }
}