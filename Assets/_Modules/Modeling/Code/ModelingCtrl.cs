using BeauUtil;

namespace Aqua.Modeling {
    
    public enum ModelPhases : byte {
        Ecosystem = 0x01,
        Concept = 0x02,
        Sync = 0x04,
        Predict = 0x08,
        Intervene = 0x10,
        Completed = 0x20
    }
}