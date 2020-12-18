using System;
using System.Collections.Generic;
using Aqua;
using BeauData;
using BeauUtil;
using UnityEngine;

namespace Aqua.Portable
{
    public interface IPortableRequest
    {
        StringHash32 AppId();
        bool CanNavigateApps();
        bool CanClose();
    }
}