using UnityEngine;
using UnityEngine.EventSystems;

namespace NativeUtils
{
    /// <summary>
    /// Handler for a native pointer up event.
    /// </summary>
    public interface INativePointerUpHandler : IEventSystemHandler {
        void OnNativePointerUp(PointerEventData eventData);
    }

    /// <summary>
    /// Handler for a native pointer down event.
    /// </summary>
    public interface INativePointerDownHandler : IEventSystemHandler {
        void OnNativePointerDown(PointerEventData eventData);
    }

    /// <summary>
    /// Handler for a native pointer down event.
    /// </summary>
    public interface INativePointerClickHandler : IEventSystemHandler {
        void OnNativePointerClick(PointerEventData eventData);
    }
}