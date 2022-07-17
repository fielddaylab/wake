using System;
using Aqua.Scripting;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    [LabeledEnum]
    public enum FacingId : byte {
        Left,
        Right,
        Up,
        Down,
        Forward,
        Back,

        [Label("Do Not Change")]
        Invalid = 255
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

        private const float VectorComponentThreshold = 0.5f;

        static public FacingId FromVector(Vector3 vector) {
            if (Mathf.Approximately(vector.x, 0) && Mathf.Approximately(vector.y, 0) && Mathf.Approximately(vector.z, 0)) {
                return FacingId.Invalid;
            }

            vector.Normalize();
            if (vector.x < -VectorComponentThreshold) {
                return FacingId.Left;
            } else if (vector.x > VectorComponentThreshold) {
                return FacingId.Right;
            } else if (vector.y < -VectorComponentThreshold) {
                return FacingId.Down;
            } else if (vector.y > VectorComponentThreshold) {
                return FacingId.Up;
            } else if (vector.z < -VectorComponentThreshold) {
                return FacingId.Back;
            } else {
                return FacingId.Forward;
            }
        }
    }
}