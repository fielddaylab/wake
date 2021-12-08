using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;
using System.Collections;
using BeauRoutine;

namespace ProtoAqua.Observation
{
    public class AutoFactGiver : MonoBehaviour, ISceneLoadHandler {
        [FilterBestiaryId] public SerializedHash32[] EntityIds;
        [FactId] public SerializedHash32[] FactIds;

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            Routine.Start(this, GiveRoutine());
        }

        private IEnumerator GiveRoutine() {
            yield return null;
            while(Script.ShouldBlock()) {
                yield return null;
            }

            var bestiaryData = Save.Bestiary;
            BestiaryDesc newEnvironment = null;
            foreach(StringHash32 entityId in EntityIds) {
                if (bestiaryData.RegisterEntity(entityId)) {
                    newEnvironment = Assets.Bestiary(entityId);
                    if (newEnvironment.Category() != BestiaryDescCategory.Environment) {
                        newEnvironment = null;
                    }
                }
            }
            foreach(StringHash32 factId in FactIds) {
                bestiaryData.RegisterFact(factId);
            }

            if (newEnvironment != null) {
                Script.PopupNewEntity(newEnvironment);
            }
        }
    }
}