using System;
using UnityEngine;

namespace Aqua
{
    public abstract class TimeAnimatedObject : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            Services.Time.Register(this);
        }

        protected virtual void OnDisable()
        {
            Services.Time?.Deregister(this);
        }

        public abstract void OnTimeChanged(InGameTime inGameTime);
    }
}