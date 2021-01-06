using System;
using BeauUtil;

namespace Aqua.Portable
{
    public interface IPortableRequest : IDisposable
    {
        StringHash32 AppId();
        bool CanNavigateApps();
        bool CanClose();
    }
}