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
        IEnumerable<DMInfo> ConstructDebugMenus(FindOrCreateMenu findOrCreate);
        #endif // DEVELOPMENT
    }

    #if DEVELOPMENT
    public delegate DMInfo FindOrCreateMenu(string label, int capacity = 0);
    #endif // DEVELOPMENT
}