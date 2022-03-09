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

        [Header("Not Selected")]
        [SerializeField] private LocText m_NotSelectedLabel = null;
        [SerializeField] private TextId m_NotSelectedHasJobsText = null;
        [SerializeField] private TextId m_NotSelectedLockedJobsText = null;
        [SerializeField] private TextId m_NotSelectedNoJobsText = null;

        #endregion

        [NonSerialized] private JobButton m_SelectedJobButton = null;
        [NonSerialized] private ListHeader[] m_GroupHeaderMap = new ListHeader[5];
        [NonSerialized] private Dictionary<StringHash32, JobButton> m_JobButtonMap = new Dictionary<StringHash32, JobButton>(32);
        [NonSerialized] private bool m_HasLockedJobs = false;
        [NonSerialized] private bool m_HasAvailableJobs = false;

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
            UpdateUnselectedLabel();

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
            switch(m_SelectedJobButton.Group)
            {
                case JobProgressCategory.Available:
                case JobProgressCategory.InProgress:
                    {
                        profileJobData.SetCurrentJob(m_SelectedJobButton.Job.Id());
                        Routine.Start(WaitToExit()).Tick();
                        break;
                    }
            }
        }

        static private IEnumerator WaitToExit()
        {
            Services.UI.ShowLetterbox();
            yield return 0.5f;
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
            foreach(var job in JobUtils.VisibleJobs())
            {
                id = job.JobId;
                if (!m_JobButtonMap.TryGetValue(id, out button))
                {
                    button = m_ButtonPool.Alloc();
                    button.Initialize(m_JobToggle, OnButtonSelected);
                    button.Populate(job.Job, JobStatusFlags.Hidden);
                    m_JobButtonMap[id] = button;
                }
            }
        }

        private void RefreshButtons()
        {
            if (UpdateButtonStatuses())
            {
                OrderButtons();
                UpdateUnselectedLabel();

                if (m_SelectedJobButton != null)
                    m_Info.UpdateStatus(m_SelectedJobButton.Job, m_SelectedJobButton.Status);
            }
        }

        private bool UpdateButtonStatuses()
        {
            bool bUpdated = false;

            var profileJobData = Save.Jobs;
            PlayerJob job;
            foreach(var button in m_ButtonPool.ActiveObjects)
            {
                job = JobUtils.GetJobStatus(button.Job, Save.Current, true);
                bUpdated |= button.UpdateStatus(job.Status, m_ButtonAppearance);
                button.gameObject.SetActive(ShouldShowButton(job));
            }

            return bUpdated;
        }

        private bool ShouldShowButton(PlayerJob job)
        {
            if ((job.Status & JobStatusFlags.InProgress) != 0 && (job.Status & JobStatusFlags.Active) == 0) {
                return job.Job.StationId() == Save.Map.CurrentStationId();
            }

            return true;
        }

        private void OrderButtons()
        {
            JobButton active = null;

            m_HasLockedJobs = false;
            m_HasAvailableJobs = false;

            using(PooledList<JobButton> progress = PooledList<JobButton>.Create())
            using(PooledList<JobButton> completed = PooledList<JobButton>.Create())
            using(PooledList<JobButton> available = PooledList<JobButton>.Create())
            using(PooledList<JobButton> locked = PooledList<JobButton>.Create())
            {
                foreach(var button in m_ButtonPool.ActiveObjects)
                {
                    if (!button.gameObject.activeSelf)
                        continue;
                    
                    switch(button.Group)
                    {
                        case JobProgressCategory.Active:
                            active = button;
                            m_HasAvailableJobs = true;
                            break;

                        case JobProgressCategory.Completed:
                            completed.Add(button);
                            break;

                        case JobProgressCategory.InProgress:
                            progress.Add(button);
                            m_HasAvailableJobs = true;
                            break;

                        case JobProgressCategory.Available:
                            available.Add(button);
                            m_HasAvailableJobs = true;
                            break;

                        case JobProgressCategory.Locked:
                            locked.Add(button);
                            m_HasLockedJobs = false;
                            break;
                    }
                }

                int siblingIndex = 0;
                OrderList(JobProgressCategory.Active, active, ref siblingIndex);
                OrderList(JobProgressCategory.InProgress, progress, ref siblingIndex);
                OrderList(JobProgressCategory.Available, available, ref siblingIndex);
                OrderList(JobProgressCategory.Locked, locked, ref siblingIndex);
                OrderList(JobProgressCategory.Completed, completed, ref siblingIndex);
            }
        }

        private void OrderList(JobProgressCategory inGroup, JobButton inButton, ref int ioSiblingIndex)
        {
            if (inButton == null)
            {
                FindHeader(inGroup, false)?.gameObject.SetActive(false);
                return;
            }

            ListHeader header = FindHeader(inGroup, true);
            header.gameObject.SetActive(true);
            header.Transform.SetSiblingIndex(ioSiblingIndex++);

            inButton.Transform.SetSiblingIndex(ioSiblingIndex++);
        }

        private void OrderList(JobProgressCategory inGroup, List<JobButton> inButtons, ref int ioSiblingIndex)
        {
            if (inButtons.Count <= 0)
            {
                FindHeader(inGroup, false)?.gameObject.SetActive(false);
                return;
            }

            ListHeader header = FindHeader(inGroup, true);
            header.gameObject.SetActive(true);
            header.Transform.SetSiblingIndex(ioSiblingIndex++);

            foreach(var button in inButtons)
            {
                button.Transform.SetSiblingIndex(ioSiblingIndex++);
            }
        }

        private ListHeader FindHeader(JobProgressCategory inGroup, bool inbCreate)
        {
            ref ListHeader header = ref m_GroupHeaderMap[(int) inGroup];
            if (header == null && inbCreate)
            {
                header = m_HeaderPool.Alloc();
                switch(inGroup)
                {
                    case JobProgressCategory.Active:
                        header.SetText("ui.jobBoard.group.active");
                        break;

                    case JobProgressCategory.Completed:
                        header.SetText("ui.jobBoard.group.completed");
                        break;

                    case JobProgressCategory.Available:
                        header.SetText("ui.jobBoard.group.available");
                        break;

                    case JobProgressCategory.InProgress:
                        header.SetText("ui.jobBoard.group.inProgress");
                        break;

                    case JobProgressCategory.Locked:
                        header.SetText("ui.jobBoard.group.locked");
                        break;
                }
            }

            return header;
        }

        private void UpdateUnselectedLabel() {
            if (m_HasAvailableJobs) {
                m_NotSelectedLabel.SetText(m_NotSelectedHasJobsText);
            } else if (m_HasLockedJobs) {
                m_NotSelectedLabel.SetText(m_NotSelectedLockedJobsText);
            } else {
                m_NotSelectedLabel.SetText(m_NotSelectedNoJobsText);
            }
        }

        #endregion // Job Buttons
    }
}