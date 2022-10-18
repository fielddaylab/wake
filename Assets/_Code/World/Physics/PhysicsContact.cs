using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using BeauUtil.Tags;
using UnityEngine;
using UnityEngine.UI;

namespace Aqua
{
    public struct PhysicsContact
    {
        public RuntimeObjectHandle Object;
        public KinematicState2D State;
        public RuntimeObjectHandle Collider;
        public Vector2 Point;
        public Vector2 Normal;

        public PhysicsContact(KinematicObject2D inObject, KinematicState2D inState, Collider2D inCollider, Vector2 inPoint, Vector2 inNormal)
        {
            Object = inObject;
            State = inState;
            Collider = inCollider;
            Point = inPoint;
            Normal = inNormal;
        }

        public override string ToString()
        {
            return string.Format("[object {0}, velocity {1}, other {2}, point {3}, normal {4}", Object, State.Velocity, Collider, Point, Normal);
        }

        static public implicit operator bool(PhysicsContact inContact)
        {
            return inContact.Collider.Id != 0;
        }
    }
}