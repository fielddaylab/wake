using System.Collections.Generic;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua.Profile
{
    public class MapData
    {
        private string currentStationId = "Station1";
        private Transform playerTransform = null;

        public void setStationId(string newStationId) {
            currentStationId = newStationId;
        }

        public string getStationId() {
            return currentStationId;
        }

        public void setPlayerTransform(Transform currentTransform) {
            playerTransform = currentTransform;
        }

        public Transform getPlayerTransform() {
            return playerTransform;
        }
        
    }
}