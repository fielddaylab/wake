using Aqua;
using UnityEngine;
using System;

namespace ProtoAqua.Experiment
{
    
    public class Climbable : MonoBehaviour 
    {
        [SerializeField] private ClimbSettings climbSettings = ClimbSettings.NONE;

        public ClimbSettings Settings { get { return Settings; } set{ Settings = climbSettings; }}

        public Vector2 position { get; set; }

        public float root { get; set; }

        public float height { get; set; }


    }

    public enum ClimbSettings : byte
    {
        KelpStem,

        GlassWall,

        NONE
    }
}