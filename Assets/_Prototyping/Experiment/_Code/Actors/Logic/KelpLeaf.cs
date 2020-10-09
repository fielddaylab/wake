using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using BeauPools;
using BeauUtil.Variants;

namespace ProtoAqua.Experiment
{
    public class KelpLeaf : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private Transform m_RenderTransform = null;
        [SerializeField] private SpriteRenderer m_Renderer = null;

        #endregion // Inspector
    }
}