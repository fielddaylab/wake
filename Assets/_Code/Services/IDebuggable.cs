using System.Collections.Generic;
using BeauUtil.Debugger;
using UnityEngine.Scripting;

namespace Aqua
{
    [Preserve]
    public interface IDebuggable
    {
        IEnumerable<DMInfo> ConstructDebugMenus();
    }
}