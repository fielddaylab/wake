using System;
using UnityEngine;

namespace Aqua
{
    public abstract class TimeAnimatedObject : MonoBehaviour, ITimeHandler
    {
        protected virtual void OnEnable()
        {
            Services.Time.Register(this);
        }

        protected virtual void OnDisable()
        {
            Services.Time?.Deregister(this);
        }

        public abstract void OnTimeChanged(GTDate inGameTime);
        public abstract TimeEvent EventMask();
    }
}