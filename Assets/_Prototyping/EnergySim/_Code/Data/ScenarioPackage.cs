using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class ScenarioPackage : ISerializedObject, ISerializedVersion
    {
        public ScenarioPackageHeader Header;
        public RuntimeSimScenario Data;
        private List<SerializedRule> m_RuleList;
        
        // non-serialized
        internal EnergySimScenario Source;
        internal Dictionary<string, SerializedRule> RuleMap = new Dictionary<string, SerializedRule>(32);

        #region ISerializedObject

        ushort ISerializedVersion.Version { get { return 2; }}

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Object("header", ref Header);
            ioSerializer.Object("data", ref Data);

            if (ioSerializer.ObjectVersion >= 2)
            {
                if (ioSerializer.IsReading)
                {
                    ioSerializer.ObjectArray("rules", ref m_RuleList);
                    RegenMap();
                }
                else
                {
                    GenList();
                    ioSerializer.ObjectArray("rules", ref m_RuleList);
                }
            }
            else
            {
                m_RuleList = new List<SerializedRule>();
            }
        }

        #endregion // ISerializedObject

        #region Applying Rules

        public void ApplyRules(ISimDatabase inDatabase)
        {
            inDatabase.ClearOverrides();

            foreach(var rule in RuleMap.Values)
            {
                if (rule.Delta == 0)
                    continue;

                ApplyDelta(inDatabase, rule.Id, rule.Delta);
            }
        }

        public void CalculateRuleDeltas(ISimDatabase inDatabase)
        {
            foreach(var rule in RuleMap.Values)
            {
                rule.Delta = CalculateDelta(inDatabase, rule.Id);
            }
        }

        private float CalculateDelta(ISimDatabase inDatabase, string inPath)
        {
            StringSlice extractedActorId = new StringSlice(inPath, 0, 4);
            StringSlice remainder = new StringSlice(inPath, 5);

            FourCC actorId = new FourCC(extractedActorId.ToString());
            ActorType actor = inDatabase.Actors[actorId];

            float currentValue, baseValue;
            actor.TryGetProperty(remainder, out currentValue);
            actor.OriginalType().TryGetProperty(remainder, out baseValue);

            return currentValue - baseValue;
        }

        private void ApplyDelta(ISimDatabase inDatabase, string inPath, float inDelta)
        {
            StringSlice extractedActorId = new StringSlice(inPath, 0, 4);
            StringSlice remainder = new StringSlice(inPath, 5);

            FourCC actorId = new FourCC(extractedActorId.ToString());
            ActorType actor = inDatabase.Actors[actorId];

            float baseValue;
            actor.OriginalType().TryGetProperty(remainder, out baseValue);
            
            actor.TrySetProperty(remainder, baseValue + inDelta);
        }

        public SerializedRule GetRule(string inId)
        {
            SerializedRule rule;
            if (!RuleMap.TryGetValue(inId, out rule))
            {
                rule = new SerializedRule(inId);
                RuleMap.Add(rule.Id, rule);
            }
            return rule;
        }

        public void ClearRules()
        {
            RuleMap.Clear();
        }

        private void GenList()
        {
            m_RuleList = new List<SerializedRule>(RuleMap.Values);
            for(int i = m_RuleList.Count - 1; i >= 0; --i)
            {
                SerializedRule rule = m_RuleList[i];
                if (rule.CanBeExcluded())
                {
                    ListUtils.FastRemoveAt(m_RuleList, i);
                }
            }
        }

        private void RegenMap()
        {
            if (RuleMap == null)
            {
                RuleMap = new Dictionary<string, SerializedRule>(32);
            }
            else
            {
                RuleMap.Clear();
            }

            if (m_RuleList != null && m_RuleList.Count > 0)
            {
                foreach(var rule in m_RuleList)
                {
                    RuleMap.Add(rule.Id, rule);
                }
            }
        }

        #endregion // Applying Rules

        #region Parsing

        static public string Export(ScenarioPackage inPackage)
        {
            return Serializer.Write(inPackage, OutputOptions.None, Serializer.Format.GZIP);
        }
        
        static public bool TryParse(string inData, out ScenarioPackage outPackage)
        {
            if (!string.IsNullOrEmpty(inData))
            {
                ScenarioPackage scenario = null;
                if (!Serializer.Read(ref scenario, inData))
                {
                    Debug.LogError("Could not read scenario data from string");
                }
                else
                {
                    Debug.LogFormat("Loaded scenario package '{0}' from string", scenario.Header.Id);
                    outPackage = scenario;
                    return true;
                }
            }

            outPackage = null;
            return false;
        }

        #endregion // Parsing
    }
}