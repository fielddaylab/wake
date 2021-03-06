using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauUtil;
using Aqua;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aqua
{
    public interface IInputHandler
    {
        void HandleInput(DeviceInput inInput);
    }
}