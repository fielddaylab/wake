#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Collections.Generic;
using BeauUtil.Debugger;
using UnityEngine.Scripting;

namespace Aqua
{
    [Preserve]
    public interface IDebuggable
    {
        #if DEVELOPMENT
        IEnumerable<DMInfo> ConstructDebugMenus();
        #endif // DEVELOPMENT
    }
}