using System;
using UnityEngine;
using BeauUtil;
using Aqua.Scripting;
using Aqua;

namespace ProtoAqua.Observation
{
    public class AutoFactGiver : MonoBehaviour, ISceneLoadHandler
    {
        [FilterBestiaryId] public SerializedHash32[] EntityIds;
        [FactId] public SerializedHash32[] FactIds;

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            var bestiaryData = Save.Bestiary;
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