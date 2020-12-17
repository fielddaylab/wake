using UnityEngine;
using UnityEngine.UI;
using BeauRoutine;
using BeauRoutine.Extensions;
using BeauUtil;
using TMPro;
using BeauPools;
using System;
using Aqua;
using Aqua.Portable;

namespace ProtoAqua.Foodweb
{
    public class FoodWebApp : MonoBehaviour {

        [Serializable] private class FactPool : SerializablePool<FoodWebFactButton> { }

        [SerializeField] private FactPool m_FactPool = null;

        [SerializeField, Required] private RectTransform m_HasSelectionGroup = null;

        [NonSerialized] private bool m_FoodWebMode = true;


        public void Awake() {
            LoadEntry();
        }
        public void LoadEntry () {

            foreach(var entryId in Services.Assets.Bestiary.Ids)
            {
                Services.Data.Profile.Bestiary.RegisterEntity(entryId);
            }

            foreach(var entryId in Services.Assets.Bestiary.Ids) {
                foreach(var fact in Services.Data.Profile.Bestiary.GetFactsForEntity(entryId))
            {
                FoodWebFactButton factButton = m_FactPool.Alloc();
                factButton.InitializeFW(fact.Fact, fact, m_FoodWebMode);
            }
            }
            
        }
    }
}