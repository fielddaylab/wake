#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Collections;
using System.Globalization;
using BeauUtil;
using Aqua.DebugConsole;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Aqua
{
    public class ServiceInitializer : MonoBehaviour
    {
        private void Awake()
        {
            Services.AutoSetup(gameObject);
        }
    }
}