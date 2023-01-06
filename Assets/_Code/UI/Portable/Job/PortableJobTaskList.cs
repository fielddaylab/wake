using UnityEngine;
using BeauPools;
using Aqua.Profile;
using UnityEngine.UI;
using BeauUtil;
using BeauRoutine;
using System.Collections;

namespace Aqua
{
    public class PortableJobTaskList : MonoBehaviour
    {
        #region Inspector

        [Header("Task List")]
        [SerializeField] private JobTaskDisplay.Pool m_TaskDisplays = null;
        [SerializeField] private CanvasGroup m_Group = null;
        [SerializeField] private ScrollRect m_ScrollView = null;
        [SerializeField] private GameObject m_TopArrow = null;

        [Header("Colors")]
        [SerializeField] private Color m_TopOutlineColor = Color.white;
        [SerializeField] private Color m_ActiveOutlineColor = Color.white;
        [SerializeField] private Color m_CompletedOutlineColor = Color.white;

        #endregion // Inspector
        
        #region Events

        private void OnDisable()
        {
            m_TaskDisplays.Reset();
        }

        #endregion // Events
    
        #region Tasks

        public IEnumerator LoadTasks(JobDesc inJob, JobsData inData)
        {
            m_TaskDisplays.Reset();

            using(PooledList<JobTask> completedTasks = PooledList<JobTask>.Create())
            using(PooledList<JobTask> activeTasks = PooledList<JobTask>.Create())
            {
                foreach(var task in inJob.Tasks())
                {
                    if (inData.IsTaskComplete(task.Id))
                        completedTasks.Add(task);
                    else if (inData.IsTaskActive(task.Id))
                        activeTasks.Add(task);
                }

                bool firstJob = true;
                foreach(var activeTask in activeTasks)
                {
                    AllocTaskDisplay(activeTask, false, firstJob ? m_TopOutlineColor : m_ActiveOutlineColor);
                    firstJob = false;
                    yield return null;
                }

                m_TopArrow.gameObject.SetActive(!firstJob);

                completedTasks.Reverse();

                foreach(var completedTask in completedTasks)
                {
                    AllocTaskDisplay(completedTask, true, m_CompletedOutlineColor);
                    yield return null;
                }
            }

            yield return null;

            m_ScrollView.ForceRebuild();
            m_ScrollView.verticalNormalizedPosition = 1;
            m_Group.alpha = 1;
        }

        public void AnimateTasks(float delay) {
            foreach(var obj in m_TaskDisplays.ActiveObjects) {
                if (!m_ScrollView.IsVisible(obj.Root)) {
                    break;
                }

                delay += obj.Appear.Ping(delay) * 0.1f;
            }
        }

        private JobTaskDisplay AllocTaskDisplay(JobTask inTask, bool inbComplete, Color inOutlineColor)
        {
            JobTaskDisplay taskDisplay = m_TaskDisplays.Alloc();
            taskDisplay.Populate(inTask, inbComplete);
            taskDisplay.Outline.color = inOutlineColor;
            return taskDisplay;
        }

        #endregion // Tasks
    }
}