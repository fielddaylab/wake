using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using AquaAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;
using Aqua;

namespace ProtoAqua.Experiment
{
    public class GlassWall : Climbable
    {
        #region Inspector

        [SerializeField] private Transform m_Body;
        [SerializeField] private Collider2D m_Collider = null;

        #endregion // Inspector

        public void Initialize() {
            position = m_Body.GetPosition(Axis.XY, Space.World);
            height = position.y;
            root = position.x;
        }

        public void ResetPosition(Vector3 point) {
            root = point.x;
            position = point;
        }

    }
}