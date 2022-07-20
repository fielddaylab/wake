using System;
using System.Collections.Generic;
using Aqua;
using Aqua.Cameras;
using Aqua.Entity;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace ProtoAqua.Observation {
    public sealed class CritterAIParams : ScriptableObject {
        [NonSerialized] private int m_Hash;

        public int Id {
            get { return m_Hash == 0 ? (m_Hash = (int) new StringHash32(name).HashValue) : m_Hash; }
        }

        public CritterAIFunction Function;
        public CritterAIStimulusReaction[] Reactions;

        [Header("Idle")]
        public float IdleDelay;
        public float IdleRandom;
    }

    public struct CritterAIStimulusReaction {
        [AutoEnum] public StimulusFlags All;
        [AutoEnum] public StimulusFlags Ignore;
        public float ReactionAdd;
        public float ReactionMultiplier;
    }

    public enum CritterAIFunction: uint {
        None,
        Gentle,
        School
    }
}