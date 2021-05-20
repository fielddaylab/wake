using UnityEngine;
using System;
using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using Aqua;
using AquaAudio;
using Aqua.Profile;

namespace Aqua.Option
{
    public class OptionsData : ISerializedObject, ISerializedVersion, IProfileChunk
    {
        [Flags]
        public enum Authority : byte
        {
            Session = 0x01,
            Profile = 0x02,

            All = 0x04
        }

        public OptionAudio Audio;
        public OptionGameplay Gameplay;

        private bool m_HasChanges = false;

        public void SetDefaults()
        {
            Audio.SetDefaults();
            Gameplay.SetDefaults();
        }

        /// <summary>
        /// Syncs settings from one options configuration to another.
        /// </summary>
        static public void SyncFrom(OptionsData inSource, OptionsData inTarget, Authority inAuthority)
        {
            if ((inAuthority & Authority.Session) != 0)
            {
                inTarget.Audio = inSource.Audio;
            }

            if ((inAuthority & Authority.Profile) != 0)
            {
                inTarget.Gameplay = inSource.Gameplay;
            }
        }

        #region ISerializedObject

        public ushort Version { get { return 1; } }

        public void Serialize(Serializer ioSerializer)
        {
            ioSerializer.Object("audio", ref Audio);
            ioSerializer.Object("gameplay", ref Gameplay);
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

        #endregion // IProfileChunk
    }
}