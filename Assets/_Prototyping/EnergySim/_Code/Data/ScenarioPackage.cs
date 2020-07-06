using System;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Energy
{
    public class ScenarioPackage : ISerializedObject, ISerializedVersion
    {
        public ScenarioPackageHeader Header;
        public RuntimeSimScenario Scenario;
        
        // non-serialized
        internal EnergySimScenario Source;

        #region ISerializedObject

        ushort ISerializedVersion.Version { get { return 1; }}

        void ISerializedObject.Serialize(Serializer ioSerializer)
        {
            ioSerializer.Object("header", ref Header);
            ioSerializer.Object("scenario", ref Scenario);
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
                    Debug.Log("Loaded scenario package from string");
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