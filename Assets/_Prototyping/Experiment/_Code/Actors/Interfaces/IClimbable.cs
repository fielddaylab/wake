using Aqua;
using UnityEngine;
using System;

namespace ProtoAqua.Experiment
{
    
    public interface IClimbable
    {
        ClimbSettings Settings { get; }

        Transform Transform { get; }

        Collider2D Collider { get; }

        Vector2 position { get;}

        float root { get; }

        float height { get;}

        void Initialize(ActorCtrl inParent=null);

        void ResetPosition(Vector3 point);


    }
}