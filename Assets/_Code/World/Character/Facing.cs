using System;
using Aqua.Scripting;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    public enum FacingId : byte {
        Left,
        Right,
        Up,
        Down,
        Forward,
        Back,
        Invalid,
    }
    
    static public class Facing {
        static public int X(FacingId facing) {
            switch(facing) {
                case FacingId.Left: {
                    return -1;
                }
                case FacingId.Right: {
                    return 1;
                }
                default: {
                    return 0;
                }
            }
        }

        static public int Y(FacingId facing) {
            switch(facing) {
                case FacingId.Down: {
                    return -1;
                }
                case FacingId.Up: {
                    return 1;
                }
                default: {
                    return 0;
                }
            }
        }

        static public int Z(FacingId facing) {
            switch(facing) {
                case FacingId.Back: {
                    return -1;
                }
                case FacingId.Forward: {
                    return 1;
                }
                default: {
                    return 0;
                }
            }
        }
    
        static public Vector3 Look(FacingId facing) {
            Vector3 vec = default;
            switch(facing) {
                case FacingId.Left: {
                    vec.x = -1;
                    break;
                }
                case FacingId.Right: {
                    vec.x = 1;
                    break;
                }
                case FacingId.Up: {
                    vec.y = 1;
                    break;
                }
                case FacingId.Down: {
                    vec.y = -1;
                    break;
                }
                case FacingId.Back: {
                    vec.z = -1;
                    break;
                }
                case FacingId.Forward: {
                    vec.z = 1;
                    break;
                }
            }
            return vec;
        }
    }
}