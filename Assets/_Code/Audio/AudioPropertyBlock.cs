using System.Runtime.InteropServices;

namespace AquaAudio
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct AudioPropertyBlock
    {
        public float Volume;
        public float Pitch;
        public bool Pause;
        public bool Mute;

        /// <summary>
        /// Combines two AudioPropertyBlocks.
        /// </summary>
        static public void Combine(in AudioPropertyBlock inSourceA, in AudioPropertyBlock inSourceB, ref AudioPropertyBlock ioTarget)
        {
            ioTarget.Volume = inSourceA.Volume * inSourceB.Volume;
            ioTarget.Pitch = inSourceA.Pitch * inSourceB.Pitch;
            ioTarget.Pause = inSourceA.Pause || inSourceB.Pause;
            ioTarget.Mute = inSourceA.Mute || inSourceB.Mute;
        }

        /// <summary>
        /// Returns if this AudioPropertyBlock results in an audible sound.
        /// </summary>
        public bool IsAudible()
        {
            return Volume > 0 && Pitch != 0 && !Mute && !Pause;
        }

        /// <summary>
        /// Resets to default settings.
        /// </summary>
        public void Reset()
        {
            this = s_Default;
        }

        #region Default

        static private readonly AudioPropertyBlock s_Default = new AudioPropertyBlock()
        {
            Volume = 1,
            Pitch = 1,
            Pause = false,
            Mute = false
        };

        static public AudioPropertyBlock Default { get { return s_Default; } }

        #endregion // Default
    }
}