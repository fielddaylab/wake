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
    public class ExposedPointerInputModule : StandaloneInputModule
    {
        public PointerEventData GetPointerEventData()
        {
            PointerEventData eventData;
            GetPointerData(kMouseLeftId, out eventData, true);
            return eventData;
        }

        public bool IsPointerOverCanvas()
        {
            PointerEventData eventData;
            GetPointerData(kMouseLeftId, out eventData, false);

            if (eventData != null)
            {
                var baseRaycaster = eventData.pointerCurrentRaycast.module;
                return !baseRaycaster.IsReferenceNull() && baseRaycaster is GraphicRaycaster;
            }

            return false;
        }
    }
}