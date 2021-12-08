using System;
using System.Collections.Generic;
using Aqua;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace ProtoAqua.ExperimentV2
{
    [CreateAssetMenu(menuName = "Aqualab System/Experiment Actor Definitions")]
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
                ActorDefinition.LoadFromBestiary(definition, definition.Type, ValidationUtils.FindPrefab<ActorInstance>(definition.Type.name, "Assets/_Content/Experiments/"));
            }

            return true;
        }

        #endregion // IOptimizableAsset

        private class SerializedDefinitionMap : ISerializedObject
        {
            public Dictionary<string, ActorDefinition> Definitions;

            public void Serialize(Serializer ioSerializer)
            {
                ioSerializer.ObjectMap("definitions", ref Definitions);
            }
        }

        [UnityEditor.CustomEditor(typeof(ActorDefinitions))]
        private class Inspector : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                UnityEditor.EditorGUILayout.Space();

                if (GUILayout.Button("Create All For Experimentation"))
                {
                    ActorDefinitions definitions = (ActorDefinitions) target;
                    UnityEditor.Undo.RecordObject(definitions, "Loading Definitions");
                    definitions.LoadDefinitions();
                    EditorUtility.SetDirty(definitions);
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("Import Critter Settings"))
                {
                    ActorDefinitions definitions = (ActorDefinitions) target;
                    definitions.ImportDefinitions();
                }

                if (GUILayout.Button("Export Critter Settings"))
                {
                    ActorDefinitions definitions = (ActorDefinitions) target;
                    definitions.ExportDefinitions();
                }
            }
        }

        private void ExportDefinitions()
        {
            string currentPath = AssetDatabase.GetAssetPath(this);
            string currentDirectory = Path.GetDirectoryName(currentPath);
            string currentFileName = Path.GetFileNameWithoutExtension(currentPath);
            string exportedLocation = Path.Combine(currentDirectory, string.Format("{0}_Export.json", currentFileName));
            SerializedDefinitionMap serializedMap = new SerializedDefinitionMap();
            serializedMap.Definitions = new Dictionary<string, ActorDefinition>();
            foreach(var def in CritterDefinitions)
            {
                if (def.Type)
                {
                    string name = def.Type.name;
                    serializedMap.Definitions.Add(name, def);
                }
            }
            Serializer.WriteFile(serializedMap, exportedLocation, OutputOptions.PrettyPrint, Serializer.Format.JSON);
            Log.Msg("[ActorDefinitions] Exported to '{0}'", exportedLocation);
            AssetDatabase.ImportAsset(exportedLocation);
        }

        private void ImportDefinitions()
        {
            string currentPath = AssetDatabase.GetAssetPath(this);
            string currentDirectory = Path.GetDirectoryName(currentPath);
            string currentFileName = Path.GetFileNameWithoutExtension(currentPath);
            string exportedLocation = Path.Combine(currentDirectory, string.Format("{0}_Export.json", currentFileName));

            SerializedDefinitionMap map = Serializer.ReadFile<SerializedDefinitionMap>(exportedLocation, Serializer.Format.JSON);
            if (map != null)
            {
                UnityEditor.Undo.RecordObject(this, "Overwriting Definitions");
                EditorUtility.SetDirty(this);

                Log.Msg("[ActorDefinitions] Importing from '{0}'...", exportedLocation);

                foreach(var kv in map.Definitions)
                {
                    ActorDefinition target = FindOrCreateDefinition(kv.Key);
                    ActorDefinition.OverwriteFromSerialized(kv.Value, target);

                    Log.Msg("... imported '{0}'", kv.Key);
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

        private ActorDefinition FindOrCreateDefinition(string inCritterId)
        {
            foreach(var critter in CritterDefinitions)
            {
                if (critter == null)
                    continue;
                
                if (critter.Id == inCritterId)
                    return critter;
            }

            ActorDefinition newDef = new ActorDefinition();
            newDef.Type = ValidationUtils.FindAsset<BestiaryDesc>(inCritterId);
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