using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Aqua;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using BeauData;
using BeauPools;
using BeauRoutine;

namespace Aqua.JobBoard
{
    public class JobBoard : MonoBehaviour, ISceneLoadHandler
    {
        [Serializable] private class HeaderPool : SerializablePool<ListHeader> { }
        [Serializable] private class ButtonPool : SerializablePool<JobButton> { }

        #region Inspector

        [Header("Pools")]
        [SerializeField] private HeaderPool m_HeaderPool = null;
        [SerializeField] private ButtonPool m_ButtonPool = null;
        
        [Header("Groups")]
        [SerializeField] private ToggleGroup m_JobToggle = null;
        [SerializeField] private JobInfo m_Info = null;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        [SerializeField] private JobButton.ButtonAppearanceConfig m_ButtonAppearance = default;
        
        [Header("Station Label")]
        [SerializeField] private Image m_StationIcon = null;
        [SerializeField] private LocText m_StationName = null;

        #endregion

        [NonSerialized] private JobButton m_SelectedJobButton = null;
        [NonSerialized] private ListHeader[] m_StatusHeaderMap = new ListHeader[4];
        [NonSerialized] private Dictionary<StringHash32, JobButton> m_JobButtonMap = new Dictionary<StringHash32, JobButton>(32);

        #region Unity Events

        private void Awake()
        {
            Services.Events.Register(GameEvents.JobSwitched, RefreshButtons, this)
                .Register(GameEvents.JobCompleted, RefreshButtons, this);

            m_Info.OnActionClicked.AddListener(OnJobAction);
        }

        private void OnDestroy()
        {
            Services.Events?.DeregisterAll(this);
        }

        #endregion // Unity Events

        #region ISceneLoadHandler

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            AllocateButtons();
            UpdateButtonStatuses();
            OrderButtons();

            MapDesc currentStation = Assets.Map(Save.Map.CurrentStationId());
            m_StationIcon.sprite = currentStation.Icon();
            m_StationName.SetText(currentStation.LabelId());

            m_Info.Clear();
        }

        #endregion // ISceneLoadHandler

        #region Handlers

        private void OnButtonSelected(JobButton inJobButton)
        {
            m_SelectedJobButton = inJobButton;
            m_Info.Populate(inJobButton.Job, inJobButton.Status);
        }

        private void OnJobAction()
        {
            var profileJobData = Save.Jobs;
            switch(m_SelectedJobButton.Status)
            {
                case PlayerJobStatus.NotStarted:
                case PlayerJobStatus.InProgress:
                    {
                        profileJobData.SetCurrentJob(m_SelectedJobButton.Job.Id());
                        Routine.Start(WaitToExit()).TryManuallyUpdate(0);
                        break;
                    }
            }
        }

        static private IEnumerator WaitToExit()
        {
            Services.UI.ShowLetterbox();
            while(Script.ShouldBlockIgnoreLetterbox())
            {
                yield return null;
            }
            InlineJobTaskList.RequestDisplayOnSceneLoad();
            yield return StateUtil.LoadPreviousSceneWithWipe();
            Services.UI.HideLetterbox();
        }

        #endregion // Handlers

        #region Job Buttons

        private void AllocateButtons()
        {
            StringHash32 id;
            JobButton button;
            foreach(var job in Services.Assets.Jobs.VisibleJobs())
            {
                id = job.Id();
                if (!m_JobButtonMap.TryGetValue(id, out button))
                {
                    button = m_ButtonPool.Alloc();
                    button.Initialize(m_JobToggle, OnButtonSelected);
                    button.Populate(job, PlayerJobStatus.NotStarted);
                    m_JobButtonMap[id] = button;
                }
            }
        }

        private void RefreshButtons()
        {
            if (UpdateButtonStatuses())
            {
                OrderButtons();
                if (m_SelectedJobButton != null)
                    m_Info.UpdateStatus(m_SelectedJobButton.Job, m_SelectedJobButton.Status);
            }
        }

        private bool UpdateButtonStatuses()
        {
            bool bUpdated = false;

            var profileJobData = Save.Jobs;
            foreach(var button in m_ButtonPool.ActiveObjects)
            {
                bUpdated |= button.UpdateStatus(profileJobData.GetStatus(button.Job.Id()), m_ButtonAppearance );
            }

            return bUpdated;
        }

        private void OrderButtons()
        {
            JobButton active = null;

            using(PooledList<JobButton> progress = PooledList<JobButton>.Create())
            using(PooledList<JobButton> completed = PooledList<JobButton>.Create())
            using(PooledList<JobButton> available = PooledList<JobButton>.Create())
            {
                foreach(var button in m_ButtonPool.ActiveObjects)
                {
                    switch(button.Status)
                    {
                        case PlayerJobStatus.Active:
                            active = button;
                            break;

                        case PlayerJobStatus.Completed:
                            completed.Add(button);
                            break;

                        case PlayerJobStatus.InProgress:
                            progress.Add(button);
                            break;

                        case PlayerJobStatus.NotStarted:
                            available.Add(button);
                            break;
                    }
                }

                int siblingIndex = 0;
                OrderList(PlayerJobStatus.Active, active, ref siblingIndex);
                OrderList(PlayerJobStatus.InProgress, progress, ref siblingIndex);
                OrderList(PlayerJobStatus.NotStarted, available, ref siblingIndex);
                OrderList(PlayerJobStatus.Completed, completed, ref siblingIndex);
            }
        }

        private void OrderList(PlayerJobStatus inStatus, JobButton inButton, ref int ioSiblingIndex)
        {
            if (inButton == null)
            {
                FindHeader(inStatus, false)?.gameObject.SetActive(false);
                return;
            }

            ListHeader header = FindHeader(inStatus, true);
            header.gameObject.SetActive(true);
            header.Transform.SetSiblingIndex(ioSiblingIndex++);

            inButton.Transform.SetSiblingIndex(ioSiblingIndex++);
        }

        private void OrderList(PlayerJobStatus inStatus, List<JobButton> inButtons, ref int ioSiblingIndex)
        {
            if (inButtons.Count <= 0)
            {
                FindHeader(inStatus, false)?.gameObject.SetActive(false);
                return;
            }

            ListHeader header = FindHeader(inStatus, true);
            header.gameObject.SetActive(true);
            header.Transform.SetSiblingIndex(ioSiblingIndex++);

            foreach(var button in inButtons)
            {
                button.Transform.SetSiblingIndex(ioSiblingIndex++);
            }
        }

        private ListHeader FindHeader(PlayerJobStatus inStatus, bool inbCreate)
        {
            ref ListHeader header = ref m_StatusHeaderMap[(int) inStatus];
            if (header == null && inbCreate)
            {
                header = m_HeaderPool.Alloc();
                switch(inStatus)
                {
                    case PlayerJobStatus.Active:
                        header.SetText("Active");
                        break;

                    case PlayerJobStatus.Completed:
                        header.SetText("Completed");
                        break;

                    case PlayerJobStatus.InProgress:
                        header.SetText("In Progress");
                        break;

                    case PlayerJobStatus.NotStarted:
                        header.SetText("Available");
                        break;
                }
            }

            return header;
        }

        #endregion // Job Buttons
    }
}