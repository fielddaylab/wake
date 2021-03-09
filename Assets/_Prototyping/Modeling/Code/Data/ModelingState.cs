using System;
using System.Collections.Generic;
using Aqua;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Modeling
{
    public struct ModelingState
    {
        public int ModelSync;
        public int PredictSync;
        public ModelingPhase Phase;
    }

    public enum ModelingPhase
    {
        Model,
        Predict,
        Completed
    }
}