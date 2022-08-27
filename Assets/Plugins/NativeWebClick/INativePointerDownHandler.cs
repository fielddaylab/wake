using UnityEngine;
using UnityEngine.EventSystems;

namespace NativeWebClick
{
    /// <summary>
    /// Handler for a native pointer down event.
    /// </summary>
    public interface INativePointerDownHandler : IEventSystemHandler {
        void OnNativePointerDown(PointerEventData eventData);
    }
}