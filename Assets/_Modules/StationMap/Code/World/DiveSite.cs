using UnityEngine;
using BeauUtil;
using System;
using Aqua.Scripting;
using Aqua.Profile;
using System.Collections;
using Aqua.Character;

namespace Aqua.StationMap
{
    [RequireComponent(typeof(SceneInteractable))]
    public class DiveSite : ScriptComponent
    {
        static public readonly StringHash32 Event_Dive = "nav:dive";
        static public readonly StringHash32 Trigger_Found = "DiveSiteFound";

        #region Inspector

        [SerializeField, MapId(MapCategory.DiveSite)] private SerializedHash32 m_MapId = null;
        [SerializeField] private bool m_HideIfLocked = false;
        [SerializeField, Required] private DiveSiteMarker m_Marker = null;

        [Header("Components")]
        [SerializeField, Required] private Collider2D m_Collider = null;
        [SerializeField, Required] private Collider2D m_DistantCollider = null;

        #endregion // Inspector

        [NonSerialized] private bool m_Highlighted;

        public StringHash32 MapId { get { return m_MapId; } }

        public void Initialize(MapData inMapData, JobDesc inCurrentJob)
        {
            var interact = GetComponent<SceneInteractable>();
            interact.OnExecute = Dive;
            interact.OverrideTargetMap(m_MapId, "Surface");

            if (!inMapData.IsSiteUnlocked(m_MapId))
            {
                interact.Lock();
                if (m_HideIfLocked)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    m_Collider.enabled = false;
                    m_Highlighted = false;
                    // m_RenderGroup.Visible = false;
                }

                if (m_Marker != null)
                {
                    m_Marker.gameObject.SetActive(false);
                }

                return;
            }

            if (inCurrentJob != null && inCurrentJob.DiveSiteIds().Contains(m_MapId))
            {
                m_Highlighted = true;
                // m_RenderGroup.SetAlpha(1);
            }
            else
            {
                m_Highlighted = false;
                // m_RenderGroup.SetAlpha(0.25f);
            }

            WorldUtils.ListenForPlayer(m_Collider, OnPlayerEnter, OnPlayerExit);

            if (m_Marker != null)
            {
                m_Marker.gameObject.SetActive(true);
                m_Marker.Pin(transform, m_MapId);
            }
        }

        private void OnPlayerEnter(Collider2D other)
        {
            using(var tempTable = TempVarTable.Alloc())
            {
                tempTable.Set("siteId", m_MapId);
                tempTable.Set("siteHighlighted", m_Highlighted);
                Services.Script.TriggerResponse(Trigger_Found, tempTable);
            }

            if (m_Marker)
            {
                m_Marker.FadeOut();
            }
        }

        private void OnPlayerExit(Collider2D other)
        {
            if (m_Marker)
            {
                m_Marker.FadeIn();
            }
        }

        private void OnPlayerEnterDistant(Collider2D other)
        {

        }

        private void OnPlayerExitDistant(Collider2D other)
        {

        }

        static private IEnumerator Dive(SceneInteractable inspectable, PlayerBody player, ScriptThreadHandle thread) {
            player.Kinematics.State.Velocity *= 0.4f;
            player.Kinematics.Config.Drag *= 4;
            yield return thread.Wait();
            if (Script.PopCancel()) {
                player.Kinematics.Config.Drag /= 4;
                yield break;
            }
            
            Services.Events.Dispatch(Event_Dive);
            Services.UI.ShowLetterbox();
            Services.Events.Dispatch(GameEvents.BeginDive, Assets.Map(inspectable.TargetMapId()).name);
            yield return 6;
            Services.UI.HideLetterbox();
        }
    }

}
