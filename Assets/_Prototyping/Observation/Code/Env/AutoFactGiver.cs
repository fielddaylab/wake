using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;
using System.Collections;
using BeauRoutine;
using Leaf.Runtime;
using UnityEngine.Scripting;

namespace ProtoAqua.Observation
{
    public class AutoFactGiver : MonoBehaviour, ISceneLoadHandler, IScriptComponent {
        [FilterBestiaryId] public SerializedHash32[] EntityIds;
        [FactId] public SerializedHash32[] FactIds;

        private Routine m_GiveRoutine;

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext) {
            m_GiveRoutine = Routine.Start(this, GiveRoutine());
        }

        private IEnumerator GiveRoutine() {
            yield return null;
            while(Script.ShouldBlock()) {
                yield return null;
            }

            BestiaryDesc newEnvironment = GetNewEnvironment();

            var bestiaryData = Save.Bestiary;
            foreach(StringHash32 entityId in EntityIds) {
                bestiaryData.RegisterEntity(entityId);
            }
            foreach(StringHash32 factId in FactIds) {
                bestiaryData.RegisterFact(factId);
            }

            if (newEnvironment != null) {
                // so the popup doesn't happen while the game is still running
                if (Services.UI.IsTransitioning()) {
                    using(Script.DisableInput()) {
                        while(Services.UI.IsTransitioning()) {
                            yield return null;
                        }
                    }
                }

                Script.PopupNewEntity(newEnvironment);
            }
        }

        private BestiaryDesc GetNewEnvironment() {
            var bestiaryData = Save.Bestiary;
            foreach(StringHash32 entityId in EntityIds) {
                if (!bestiaryData.HasEntity(entityId)) {
                    BestiaryDesc entry = Assets.Bestiary(entityId);
                    if (entry.Category() == BestiaryDescCategory.Environment) {
                        return entry;
                    }
                }
            }
            return null;
        }

        [LeafMember("Suppress"), Preserve]
        public void Suppress() {
            m_GiveRoutine.Stop();
        }

        ScriptObject IScriptComponent.Parent { get { return null; } }    
        void IScriptComponent.OnRegister(ScriptObject inObject) { }
        void IScriptComponent.OnDeregister(ScriptObject inObject) { }
        void IScriptComponent.PostRegister() { }
    }
}