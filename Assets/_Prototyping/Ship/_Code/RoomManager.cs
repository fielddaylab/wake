using System;
using System.Collections;
using System.Collections.Generic;
using Aqua;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace ProtoAqua.Ship
{
    public class RoomManager : SharedManager, ISceneLoadHandler, IScenePreloader
    {
        #region Inspector

        [SerializeField, Required] private Room m_DefaultRoom = null;

        #endregion // Inspector

        [NonSerialized] private Room[] m_Rooms;
        [NonSerialized] private Room m_CurrentRoom;
        private Routine m_Transition;

        #region Scene Load

        IEnumerator IScenePreloader.OnPreloadScene(SceneBinding inScene, object inContext)
        {
            List<Room> rooms = new List<Room>(8);
            inScene.Scene.GetAllComponents<Room>(true, rooms);
            m_Rooms = rooms.ToArray();

            yield return null;

            foreach(var room in m_Rooms)
            {
                room.Initialize();
                yield return null;
            }
        }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            StringHash32 currentSceneId = Services.Data.GetVariable(GameVars.ShipRoom).AsStringHash();
            Room room = GetRoom(currentSceneId);
            LoadRoom(room);
        }

        #endregion // ISceneLoad

        #region Room Transitions

        public void LoadNavRoom()
        {
            StateUtil.LoadSceneWithWipe(Services.Data.Profile.Map.CurrentStationId());
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
                m_CurrentRoom.Enter(Services.State.Camera);
            }
            else
            {
                m_Transition.Replace(this, RoomTransition(inRoom)).TryManuallyUpdate(0);
            }
        }

        private IEnumerator RoomTransition(Room inNextRoom)
        {
            Services.Input.PauseAll();

            using(var fader = Services.UI.WorldFaders.AllocWipe())
            {
                yield return fader.Object.Show();
                m_CurrentRoom.Exit();
                m_CurrentRoom = inNextRoom;
                m_CurrentRoom.Enter(Services.State.Camera);
                yield return 0.15f;
                yield return fader.Object.Hide(false);
            }

            Services.Input.ResumeAll();
        }

        #endregion // Room Transitions

        private Room GetRoom(StringHash32 inId)
        {
            Room room;
            if (inId.IsEmpty || !m_Rooms.TryGetValue(inId, out room))
            {
                room = m_DefaultRoom;
            }
            return room;
        }
    }
}