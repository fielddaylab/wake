using System;
using System.Collections;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;

namespace ProtoAqua
{
    public partial class DataService : ServiceBehaviour
    {
        #region IService

        public override FourCC ServiceId()
        {
            return ServiceIds.Data;
        }

        #endregion // IService
    }
}