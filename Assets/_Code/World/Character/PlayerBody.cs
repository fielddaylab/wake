using System;
using UnityEngine;

namespace Aqua.Character
{
    public abstract class PlayerBody : CharacterBody
    {
        private void FixedUpdate()
        {
            if (!Services.Physics.Enabled)
                return;

            Tick(Time.fixedDeltaTime);
        }

        protected abstract void Tick(float inDeltaTime);
    }

    [Flags]
    public enum PlayerBodyStatus : uint {
        Normal = 0,
        Stunned = 0x01
    }
}