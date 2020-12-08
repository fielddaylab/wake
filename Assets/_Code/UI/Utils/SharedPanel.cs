using UnityEngine;
using BeauRoutine.Extensions;
using BeauRoutine;
using System.Collections;
using BeauUtil;

namespace Aqua
{
    public class SharedPanel : BasePanel
    {
        protected override void Awake()
        {
            base.Awake();

            Services.UI.RegisterPanel(this);
        }

        protected virtual void OnDestroy()
        {
            Services.UI?.DeregisterPanel(this);
        }
    }
}