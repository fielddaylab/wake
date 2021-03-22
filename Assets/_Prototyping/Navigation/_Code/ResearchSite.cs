using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using UnityEngine.SceneManagement;
using Aqua;
using BeauUtil;
using System;

namespace ProtoAqua.Navigation
{
    public class ResearchSite : MonoBehaviour
    {
        #region Inspector

        [SerializeField, Required] private string m_siteId = null;
        [SerializeField, Required] private string m_siteLabel = null;

        [Header("Components")]
        [SerializeField, Required] private ColorGroup m_RenderGroup = null;
        [SerializeField, Required] private Transform m_BuoyGroup = null;
        [SerializeField, Required] private Collider2D m_Collider = null;
        [SerializeField, Required] private Transform m_FoamTransform = null;

        [Header("Animation")]
        [SerializeField] private Vector3 m_BobRotatePeriod = default(Vector3);
        [SerializeField] private float m_BobMovePeriod = 1;
        [SerializeField] private float m_FoamScalePeriod = 1;
        [SerializeField] private Vector3 m_BobRotateDistance = default(Vector3);
        [SerializeField] private float m_BobMoveDistance = 0.1f;
        [SerializeField] private float m_FoamScaleDistance = 0.1f;

        #endregion // Inspector

        [NonSerialized] private bool m_Highlighted;
        [NonSerialized] private Routine m_BobRoutine;
        [NonSerialized] private float m_OriginalFoamScale;

        public string SiteId { get { return m_siteId; } }

        private void Awake()
        {
            var listener = m_Collider.EnsureComponent<TriggerListener2D>();
            listener.FilterByComponentInParent<PlayerController>();
            listener.onTriggerEnter.AddListener(OnPlayerEnter);
            listener.onTriggerExit.AddListener(OnPlayerExit);
            m_OriginalFoamScale = m_FoamTransform.localScale.x;
        }

        private void OnEnable()
        {
            m_BobRoutine.Replace(this, BobAnimation());
        }

        private void OnDisable()
        {
            m_BobRoutine.Stop();
        }

        public void CheckAllowed()
        {
            var currentJob = Services.Data.CurrentJob()?.Job;
            if (currentJob != null && currentJob.UsesDiveSite(m_siteId))
            {
                m_Highlighted = true;
                m_RenderGroup.SetAlpha(1);
            }
            else
            {
                m_Highlighted = false;
                m_RenderGroup.SetAlpha(0.25f);
            }
        }

        private void OnPlayerEnter(Collider2D other)
        {
            Services.UI.FindPanel<UIController>().Display(m_siteLabel, m_siteId);
            using(var tempTable = Services.Script.GetTempTable())
            {
                tempTable.Set("siteId", m_siteId);
                tempTable.Set("siteHighlighted", m_Highlighted);
                Services.Script.TriggerResponse("ResearchSiteFound", null, null, tempTable);
            }
        }

        private void OnPlayerExit(Collider2D other)
        {
            Services.UI?.FindPanel<UIController>()?.Hide();
        }

        private IEnumerator BobAnimation()
        {
            Vector3 rotateOffsets = new Vector3(RNG.Instance.Next(0, 5), RNG.Instance.Next(1, 6), RNG.Instance.Next(2, 7));
            float bobOffset = RNG.Instance.NextFloat(3f);
            float foamOffset = RNG.Instance.NextFloat(2f);
            float time = 0;
            while(true)
            {
                time += Routine.DeltaTime;
                float xRot = m_BobRotateDistance.x * (float) Math.Sin(Mathf.PI * 2 * (rotateOffsets.x + time) / m_BobRotatePeriod.x);
                float yRot = m_BobRotateDistance.y * (float) Math.Sin(Mathf.PI * 2 * (rotateOffsets.y + time) / m_BobRotatePeriod.y);
                float zRot = m_BobRotateDistance.z * (float) Math.Sin(Mathf.PI * 2 * (rotateOffsets.z + time) / m_BobRotatePeriod.z);
                float zMove = m_BobMoveDistance * (float) Math.Sin(Mathf.PI * 2 * (bobOffset + time) / m_BobMovePeriod);
                float foamScale = m_OriginalFoamScale + m_FoamScaleDistance * (float) Math.Sin(Math.PI * 2 * (foamOffset + time) / m_FoamScalePeriod);
                m_BuoyGroup.SetPosition(zMove, Axis.Z, Space.Self);
                m_BuoyGroup.localEulerAngles = new Vector3(xRot, yRot, zRot);
                m_FoamTransform.localEulerAngles = new Vector3(0, 0, xRot + yRot - zRot);
                m_FoamTransform.SetScale(foamScale, Axis.XY);
                yield return null;
            }
        }
    }

}
