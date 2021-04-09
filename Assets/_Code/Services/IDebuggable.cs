using System.Collections.Generic;
using BeauUtil.Debugger;

namespace Aqua
{
    public interface IDebuggable
    {
        IEnumerable<DMInfo> ConstructDebugMenus();
    }
}