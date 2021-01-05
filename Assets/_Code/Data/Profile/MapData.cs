using System.Collections.Generic;
using BeauData;
using BeauUtil;

namespace Aqua.Profile
{
    public class MapData
    {
        private string currentStationId = "Station1";

        public int setStationId(string newStationId) {
            currentStationId = newStationId;
            return 2;
        }

        public string getStationId() {
            return currentStationId;
        }
        
    }
}