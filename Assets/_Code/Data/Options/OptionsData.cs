using System;
using BeauData;
using Aqua.Profile;

namespace Aqua.Option
{
    public class OptionsData : ISerializedObject, ISerializedVersion, IProfileChunk
    {
        [Flags]
        public enum Authority : byte
        {
            Local = 0x01,
            Profile = 0x02,

            All = Local | Profile
        }

        public OptionsAudio Audio;
        public OptionsGameplay Gameplay;
        public OptionsPerformance Performance;
        public OptionsAccessibility Accessibility;
        public OptionsLanguage Language;

        private bool m_HasChanges = false;

        public void SetDefaults(Authority inAuthority)
        {
            if ((inAuthority & Authority.Local) != 0)
            {
                Audio.SetDefaults();
                Performance.SetDefaults();
                Language.SetDefaults();
            }

            if ((inAuthority & Authority.Profile) != 0)
            {
                Gameplay.SetDefaults();
                Accessibility.SetDefaults();
            }
        }

        public ulong Hash() {
            ulong hash = UnsafeExt.Hash(Audio);
            hash = UnsafeExt.Hash(Gameplay, hash);
            hash = UnsafeExt.Hash(Performance, hash);
            hash = UnsafeExt.Hash(Accessibility, hash);
            hash = UnsafeExt.Hash(Language, hash);
            return hash;
        }

        /// <summary>
        /// Syncs settings from one options configuration to another.
        /// </summary>
        static public void SyncFrom(OptionsData inSource, OptionsData inTarget, Authority inAuthority)
        {
            if ((inAuthority & Authority.Local) != 0)
            {
                inTarget.Audio = inSource.Audio;
                inTarget.Performance = inSource.Performance;
                inTarget.Language = inSource.Language;
            }

            if ((inAuthority & Authority.Profile) != 0)
            {
                inTarget.Gameplay = inSource.Gameplay;
                inTarget.Accessibility = inSource.Accessibility;
            }
        }

        #region ISerializedObject

        public ushort Version { get { return 3; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Object("audio", ref Audio);
            ioSerializer.Object("gameplay", ref Gameplay);
            if (ioSerializer.ObjectVersion >= 2)
            {
                ioSerializer.Object("performance", ref Performance);
                ioSerializer.Object("accessibility", ref Accessibility);
                if (ioSerializer.ObjectVersion >= 3) {
                    ioSerializer.Object("language", ref Language);
                }
            }
            else
            {
                Performance.SetDefaults();
                Accessibility.SetDefaults();
            }
        }

        #endregion // ISerializedObject

        #region IProfileChunk

        public bool SetDirty()
        {
            if (!m_HasChanges)
            {
                m_HasChanges = true;
                return true;
            }

            return false;
        }

        public bool HasChanges()
        {
            return m_HasChanges;
        }

        public void MarkChangesPersisted()
        {
            m_HasChanges = false;
        }

        public void Dump(EasyBugReporter.IDumpWriter writer) {
            
        }

        #endregion // IProfileChunk
    }
}