using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.ExperimentV2
{
    [CreateAssetMenu(menuName = "Aqualab/ExperimentV2/Actor Definitions")]
    public class ActorDefinitions : ScriptableObject, IOptimizableAsset
    {
        public ActorDefinition[] CritterDefinitions;

        [NonSerialized] private Dictionary<StringHash32, ActorDefinition> m_DefinitionMap;

        public ActorDefinition FindDefinition(StringHash32 inCritterId)
        {
            if (inCritterId.IsEmpty)
                return null;

            if (m_DefinitionMap == null)
            {
                m_DefinitionMap = new Dictionary<StringHash32, ActorDefinition>();
                foreach(var critter in CritterDefinitions)
                {
                    m_DefinitionMap.Add(critter.Id, critter);
                }
            }

            ActorDefinition def;
            if (!m_DefinitionMap.TryGetValue(inCritterId, out def))
                Assert.Fail("Critter id '{0}' not recognized as a valid actor type for experimentation", inCritterId);
            return def;
        }

        #if UNITY_EDITOR

        #region IOptimizableAsset

        int IOptimizableAsset.Order { get { return 20; } }

        bool IOptimizableAsset.Optimize()
        {
            foreach(var definition in CritterDefinitions)
            {
                ActorDefinition.LoadFromBestiary(definition, definition.Type);
            }

            return true;
        }

        #endregion // IOptimizableAsset

        [UnityEditor.CustomEditor(typeof(ActorDefinitions))]
        private class Inspector : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                UnityEditor.EditorGUILayout.Space();

                if (GUILayout.Button("Load All For Experimentation"))
                {
                    ActorDefinitions definitions = (ActorDefinitions) target;
                    UnityEditor.Undo.RecordObject(definitions, "Loading Definitions");
                    definitions.LoadDefinitions();
                }
            }
        }

        private ActorDefinition FindOrCreateDefinition(BestiaryDesc inCritter)
        {
            foreach(var critter in CritterDefinitions)
            {
                if (critter == null)
                    continue;
                
                if (critter.Type == inCritter)
                    return critter;
            }

            ActorDefinition newDef = new ActorDefinition();
            newDef.Type = inCritter;
            return newDef;
        }

        private void LoadDefinitions()
        {
            List<ActorDefinition> definitions = new List<ActorDefinition>();
            var bestiaryDB = ValidationUtils.FindAsset<BestiaryDB>();
            foreach(var obj in bestiaryDB.Objects)
            {
                if (obj.Category() != BestiaryDescCategory.Critter)
                    continue;

                if (obj.HasFlags(BestiaryDescFlags.DoNotUseInExperimentation))
                    continue;

                definitions.Add(FindOrCreateDefinition(obj));
            }

            CritterDefinitions = definitions.ToArray();
        }

        #endif // UNITY_EDITOR
    }
}