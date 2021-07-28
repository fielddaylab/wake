using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil.Debugger;
using BeauUtil.Variants;

namespace Aqua
{
    public class SharedManager : MonoBehaviour
    {
        protected virtual void Awake()
        {
            Services.State.RegisterManager(this);
        }

        protected virtual void OnDestroy()
        {
            Services.State?.DeregisterManager(this);
        }

        static public T Find<T>() where T : SharedManager
        {
            return Services.State?.FindManager<T>();
        }
    }
}