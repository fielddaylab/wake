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
    public class FoundationalTank : ExperimentTank
    {
        #region Inspector

        #endregion // Inspector

        public override bool TryHandle(TankSelectionData inSelection)
        {
            if (inSelection.Tank == TankType.Foundational)
            {
                gameObject.SetActive(true);
                return true;
            }

            return false;
        }

        public override void Hide()
        {
            gameObject.SetActive(false);
            Routine.StopAll(this);
        }
    }
}