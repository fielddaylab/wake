using System.Collections.Generic;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua.Profile
{
    public class MapData
    {
        private string currentStationId = "Station1";

        public void setStationId(string newStationId) {
            currentStationId = newStationId;
        }

        public string getStationId() {
            return currentStationId;
        }
        
    }
}