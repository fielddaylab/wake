using System;
using BeauUtil;

namespace Aqua.Portable
{
    public interface IPortableRequest : IDisposable
    {
        PortableMenu.AppId AppId();
        bool CanNavigateApps();
        bool CanClose();
        bool ForceInputEnabled();
    }
}