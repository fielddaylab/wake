using System;
using UnityEngine;

namespace Aqua
{
    public static class ComponentUtils
    {
        public static bool HasComponent<T>(this GameObject obj) where T : Component
        {
            return obj.GetComponent<T>() != null;
        }
    }
}