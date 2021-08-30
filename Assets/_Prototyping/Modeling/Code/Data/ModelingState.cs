namespace ProtoAqua.Modeling
{
    public struct ModelingState
    {
        public bool HasAllHistorical;
        public int ModelSync;
        public int PredictSync;
        public ModelingPhase Phase;
    }

    public enum ModelingPhase
    {
        Universal,
        Sync,
        Predict,
        Completed
    }
}