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
        
        // non-serialized
        internal EnergySimScenario Source = null;

        #region ISerializedObject

        ushort ISerializedVersion.Version { get { return 3; }}

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Object("header", ref Header);
            ioSerializer.Object("data", ref Data);
        }

        #endregion // ISerializedObject

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