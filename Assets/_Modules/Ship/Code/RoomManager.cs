using System;
using System.Collections;
using System.Collections.Generic;
using Aqua.Profile;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace Aqua.Ship
{
    public class RoomManager : SharedManager, ISceneLoadHandler, IScenePreloader, ISceneOptimizable
    {
        static public readonly StringHash32 Trigger_RoomEnter = "RoomEnter";

        #region Inspector

        [SerializeField, Required] private Room m_DefaultRoom = null;
        [SerializeField, HideInInspector] private Room[] m_Rooms;
        [SerializeField, HideInInspector] private RoomLink[] m_Links;

        #endregion // Inspector

        [NonSerialized] private Room m_CurrentRoom;
        private Routine m_Transition;

        #region Scene Load

        protected override void Awake()
        {
            base.Awake();

            Services.Events.Register(GameEvents.RoomLockChanged, RefreshRoomLinks, this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Services.Events?.DeregisterAll(this);
        }

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext)
        {
            ParentToRoom[] allRoomRetargets = FindObjectsOfType<ParentToRoom>();

            foreach(var roomReparent in allRoomRetargets)
            {
                roomReparent.transform.SetParent(GetRoom(roomReparent.RoomId).transform, true);
                if (roomReparent.Flatten)
                {
                    roomReparent.transform.FlattenHierarchy(false);
                }
                Destroy(roomReparent);
                yield return null;
            }

            foreach(var room in m_Rooms)
            {
                room.Initialize();
                yield return null;
            }

            Save.Map.UnlockRoom(m_DefaultRoom.Id());
            RefreshRoomLinks();
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            StringHash32 currentStationId = Save.Map.CurrentStationId();
            Save.Map.RecordVisitedLocation(currentStationId);

            StringHash32 currentSceneId = Services.Data.GetVariable(GameVars.ShipRoom).AsStringHash();
            Room room = GetRoom(currentSceneId);
            LoadRoom(room);
        }

        #endregion // ISceneLoad

        #region Room Transitions

        public void LoadNavRoom()
        {
            StateUtil.LoadMapWithWipe(Save.Map.CurrentStationId(), "Ship");
        }

        public void LoadScene(string inScene)
        {
            StateUtil.LoadSceneWithWipe(inScene);
        }

        public void LoadRoom(Room inRoom)
        {
            if (m_CurrentRoom == inRoom)
                return;

            Services.Data.SetVariable(GameVars.ShipRoom, inRoom.Id());

            if (m_CurrentRoom == null)
            {
                m_CurrentRoom = inRoom;
                m_CurrentRoom.Enter();

                using(var table = TempVarTable.Alloc())
                {
                    table.Set("roomId", inRoom.Id());
                    Services.Script.TriggerResponse(Trigger_RoomEnter, table);
                }
            }
            else
            {
                m_Transition.Replace(this, RoomTransition(inRoom)).TryManuallyUpdate(0);
            }
        }

        private IEnumerator RoomTransition(Room inNextRoom)
        {
            Services.Input.PauseAll();
            Services.Script.KillLowPriorityThreads();

            using(var fader = Services.UI.WorldFaders.AllocWipe())
            {
                yield return fader.Object.Show();
                m_CurrentRoom.Exit();
                m_CurrentRoom = inNextRoom;
                yield return 0.15f;
                m_CurrentRoom.Enter();
                using(var table = TempVarTable.Alloc())
                {
                    table.Set("roomId", m_CurrentRoom.Id());
                    Services.Script.TriggerResponse(Trigger_RoomEnter, table);
                }
                yield return fader.Object.Hide(false);
            }

            AutoSave.Hint();
            Services.Input.ResumeAll();
        }

        #endregion // Room Transitions

        private void RefreshRoomLinks()
        {
            MapData map = Save.Map;
            foreach(var roomLink in m_Links)
            {
                if (map.IsRoomUnlocked(roomLink.LinkId))
                {
                    roomLink.Show();
                }
                else
                {
                    roomLink.Hide();
                }
            }
        }

        private Room GetRoom(StringHash32 inId)
        {
            Room room;
            if (inId.IsEmpty || !m_Rooms.TryGetValue(inId, out room))
            {
                room = m_DefaultRoom;
            }
            return room;
        }

        #if UNITY_EDITOR

        void ISceneOptimizable.Optimize()
        {
            List<Room> rooms = new List<Room>(8);
            SceneHelper.ActiveScene().Scene.GetAllComponents<Room>(true, rooms);
            m_Rooms = rooms.ToArray();
            m_Links = FindObjectsOfType<RoomLink>();
        }

        #endif // UNITY_EDITOR
    }
}