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
}