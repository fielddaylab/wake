using System;
using System.Collections;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Character
{
    public class SceneExit : ScriptComponent
    {
        #region Inspector

        [SerializeField, Required] private Collider2D m_Collider = null;
        [Space]
        [SerializeField] private SerializedHash32 m_TargetMap = null;
        [SerializeField] private SerializedHash32 m_TargetEntrance = null;

        #endregion // Inspector

        [NonSerialized] private Routine m_Routine;

        private void Awake()
        {
            WorldUtils.ListenForPlayer(m_Collider, OnPlayerEnter, null);
        }

        private void OnEnable()
        {
            m_Routine.Replace(this, WaitToActivate());
        }

        private void OnDisable()
        {
            m_Routine.Stop();
        }

        private void Enter()
        {
            StateUtil.LoadMapWithWipe(m_TargetMap, m_TargetEntrance);
        }

        private void OnPlayerEnter(Collider2D inCollider)
        {
            if (m_Routine)
                return;
            
            using(var table = TempVarTable.Alloc())
            {
                table.Set("exitId", Parent.Id());
                table.Set("targetMapId", m_TargetMap);
                table.Set("targetEntranceId", m_TargetEntrance);

                var response = Services.Script.TriggerResponse(GameTriggers.TryExitScene, null, Parent, table);
                if (response.IsRunning())
                {
                    m_Routine = Routine.Start(this, WaitToEnter(response));
                }
            }
        }

        private IEnumerator WaitToActivate()
        {
            while(Services.State.IsLoadingScene())
                yield return null;

            if (!IsNotCollidingWithPlayer())
            {
                yield return Routine.WaitCondition(IsNotCollidingWithPlayer, 0.2f);
            }
        }

        private bool IsNotCollidingWithPlayer()
        {
            return !PhysicsUtils.IsOverlapping(m_Collider, GameLayers.Player_Mask);
        }

        private IEnumerator WaitToEnter(ScriptThreadHandle inThread)
        {
            while(inThread.IsRunning())
                yield return null;

            Enter();
        }
    }
}