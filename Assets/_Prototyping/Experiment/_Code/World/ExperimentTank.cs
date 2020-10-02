using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using System.Reflection;
using BeauUtil.Variants;
using BeauRoutine.Extensions;
using ProtoCP;

namespace ProtoAqua.Experiment
{
    public abstract class ExperimentTank : MonoBehaviour
    {
        #region Inspector

        #endregion // Inspector

        public abstract bool TryHandle(TankSelectionData inSelection);
        public abstract void Hide();
    }
}