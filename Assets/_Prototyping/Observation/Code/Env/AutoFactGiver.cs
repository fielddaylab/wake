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