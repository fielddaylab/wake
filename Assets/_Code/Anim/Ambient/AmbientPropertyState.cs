using System;

namespace Aqua.Animation
{
    /// <summary>
    /// State of a single ambient wave.
    /// </summary>
    public struct AmbientWaveState
    {
        public float Start;
        public float Length;
        
        /// <summary>
        /// Calculates progress through a given wave state.
        /// </summary>
        static public float CalculateProgress(in AmbientWaveState inState, float inTime, bool inbLoop)
        {
            if (inTime <= inState.Start || inState.Length <= 0)
                return 0;
            float prog = (inTime - inState.Start) / inState.Length;
            if (!inbLoop)
                prog = Math.Min(1, prog);
            return prog;
        }

        /// <summary>
        /// Advances the wave forward and sets its length.
        /// </summary>
        static public void AdvanceAndSetLength(ref AmbientWaveState ioState, float inDelay, float inNewLength)
        {
            ioState.Start += ioState.Length + inDelay;
            ioState.Length = inNewLength;
        }
    }

    /// <summary>
    /// State of an animated vector3.
    /// </summary>
    public struct AmbientVec3State
    {
        public AmbientWaveState XYZ;
        public AmbientWaveState X;
        public AmbientWaveState Y;
        public AmbientWaveState Z;
    }

    /// <summary>
    /// Ambient transform state.
    /// </summary>
    public struct AmbientTransformState
    {
        public AmbientVec3State PositionState;
        public AmbientVec3State ScaleState;
        public AmbientVec3State RotationState;
    }

    /// <summary>
    /// Ambient color state.
    /// </summary>
    public struct AmbientColorState
    {
        public AmbientWaveState ColorState;
        public AmbientWaveState AlphaState;
    }
}