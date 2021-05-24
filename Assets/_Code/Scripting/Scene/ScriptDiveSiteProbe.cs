using BeauUtil;
using UnityEngine;
using System.Collections;
using BeauUtil.Debugger;
using Leaf;
using System;
using BeauUtil.Variants;
using Leaf.Runtime;
using Aqua.Portable;
using BeauRoutine;
using System.Collections.Generic;

namespace Aqua.Scripting
{
    public class ScriptDiveSiteProbe : ScriptComponent
    {
        #region Inspector

        #endregion // Inspector

        // List<SerializedHash32> AllowedEntities = new List<SerializedHash32>();
        // BestiaryDesc selectedCritter = null;
        // StringHash32 probeCritterId = null;
        // HashSet<StringHash32> ImproveFactIds = null;


        // [LeafMember("AskForBestiaryEntry")]
        // IEnumerator AskForBestiaryEntry()
        // {
        //     Future<StringHash32> entity = BestiaryApp.RequestEntity(
        //         BestiaryDescCategory.Critter, (critterId) => EntryHasValues(critterId));
        //     entity.OnComplete(StoreId);
        //     entity.OnFail(() => Services.Data.SetVariable("probeScan:critter", null));
            
        //     yield return entity.Wait();

        // }

        // [LeafMember("ImproveRules")]
        // public void ImproveRules()
        // {
        //     if(selectedCritter != null) AllowedEntities.Add(selectedCritter.Id());
        //     if(ImproveFactIds != null) {
        //         foreach(var factId in ImproveFactIds) {
        //             Services.Data.Profile.Bestiary.GetFact(factId).Add(PlayerFactFlags.KnowValue);
        //         }
        //     }
        //     selectedCritter = null;
        //     ImproveFactIds.Clear();

        // }

        // public bool EntryHasValues(BestiaryDesc critter) {
        //     return critter.HasFactWithValue(out ImproveFactIds);
        // }

        // public void StoreId(StringHash32 critterId)
        // {
        //     probeCritterId = critterId;
        //     selectedCritter = critterId != null ? Services.Assets.Bestiary.Get(critterId) : null;
        //     Services.Data.SetVariable("probeScan:critter", selectedCritter.CommonName().Hash());
        // }
    }
}