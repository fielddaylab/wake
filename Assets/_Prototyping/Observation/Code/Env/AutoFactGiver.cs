using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;

namespace ProtoAqua.Observation
{
    public class AutoFactGiver : MonoBehaviour, ISceneLoadHandler
    {
        public SerializedHash32[] EntityIds;
        public SerializedHash32[] FactIds;

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            var bestiaryData = Services.Data.Profile.Bestiary;
            foreach(StringHash32 entityId in EntityIds)
            {
                bestiaryData.RegisterEntity(entityId);
            }
            foreach(StringHash32 factId in FactIds)
            {
                bestiaryData.RegisterFact(factId);
            }
        }
    }
}