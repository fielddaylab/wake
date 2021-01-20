using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Aqua;
using BeauUtil;
using UnityEngine.UI;
using TMPro;
using BeauData;

namespace ProtoAqua.JobBoard
{
    public class JobBoard : MonoBehaviour, ISceneLoadHandler
    {

        #region Serialized Fields
        [Header("Prefabs")]
        [SerializeField] private GameObject listHeaderPrefab = null;



        [Header("Player")]
        [SerializeField] private GameObject playerObject = null;
        [SerializeField] private Transform JobPanel = null;

        [SerializeField] private Transform JobSelected = null;

        [Header("Sources")]
        // [SerializeField] private TextAsset jobListJSON = null;
        [SerializeField] private Sprite[] spriteRefs = null;

        #endregion

        #region PrivateVariables

        // private Player player;
        private StringHash32 currentJobId;

        //Transforms used in Awake to create Buttons and adjust selectedJob
        private Transform panel;
        private Transform grid;
        private Transform jobButtonTemplate;


        //Used to change color of selectedButton and adjust button when new one is pressed
        private Transform selectedButton = null;
        private Image selectedButtonImage = null;

        //Transforms used for the acccept/complete button
        private Transform acceptJobButton = null;
        // private Transform completeJobButton = null;

        //Dictionary to match IDs used when updating the lists
        // private Dictionary<string, Transform> jobIdToButton = new Dictionary<string, Transform>(); //Used to translate JobId to button transform
        // private Dictionary<string, Transform> listHeaderToButton = new Dictionary<string, Transform>(); //Used to translate the listHeaders to buttons

        private JobButton[] JobButtons = null;

        private List<JobDesc> Jobs = new List<JobDesc>();

        private ListHeader[] Headers = null;

        private JobSelect CurrentJob = null;

        #endregion //Private Variables

        private void Awake()
        {

            CurrentJob = JobSelected.GetComponent<JobSelect>();
            JobSelected.gameObject.SetActive(false);
            JobButtons = JobPanel.GetComponentsInChildren<JobButton>(true);
            Headers = JobPanel.GetComponentsInChildren<ListHeader>(true);
            CurrentJob.GetStatusButton().GetComponent<Button>().onClick.AddListener(
                () => UpdateButtonByStatus(currentJobId));

        }

        private void Start()
        {
            //TODO adjust headers if no active jobs/ completed jobs?
            foreach (PlayerJobStatus status in (PlayerJobStatus[])Enum.GetValues(typeof(PlayerJobStatus)))
            {
                SetJobListActiveStatus(status);
            }
        }

        private void SetupHeaders(ListHeader[] Heads)
        {
            if (Heads.Length < 3)
            {
                Debug.Log("Needs more listHeaders object"); // TODO : make this dynamic
            }

            int i = 0;
            foreach (PlayerJobStatus status in (PlayerJobStatus[])Enum.GetValues(typeof(PlayerJobStatus)))
            {
                Heads[i].Status = status;
                Heads[i].Update();
                // Heads[i].GetTransform().gameObject.SetActive(false);
                i++;
            }
            {
            }
        }
        private void SetupButtons(JobButton[] jButtons)
        {
            int buttonCount = jButtons.Length;
            int dbCount = Services.Assets.Jobs.Objects.Count;

            if (buttonCount < dbCount)
            {
                Debug.Log("Needs more JobButtons Please"); // TODO : make this dynamic
                return;
            }

            int i = 0;
            foreach (JobDesc job in Services.Assets.Jobs.VisibleJobs())
            {
                Jobs.Add(job);

                if (job.ShouldBeAvailable())
                {
                    jButtons[i].SetupJob(job, Services.Data.Profile.Jobs.GetProgress(job.Id()));
                    i++;
                }
            }

            for (; i < jButtons.Length; ++i)
            {
                jButtons[i].gameObject.SetActive(false);
            }

        }

        private void UpdateButtonByStatus(StringHash32 JobId)
        {
            PlayerJobStatus currStatus = Services.Data.Profile.Jobs.GetProgress(JobId).Status();
            StringHash32 currentJobId = Services.Data.CurrentJob()?.JobId ?? StringHash32.Null;
            foreach (JobButton jobButton in JobButtons)
            {
                if (jobButton.JobId.Equals(JobId))
                {
                    if (currStatus.Equals(PlayerJobStatus.NotStarted))
                    {
                        jobButton.Status = PlayerJobStatus.Active;
                        Services.Data.Profile.Jobs.SetCurrentJob(JobId);
                    }
                    else if (currStatus.Equals(PlayerJobStatus.InProgress))
                    {
                        jobButton.Status = PlayerJobStatus.Active;
                        Services.Data.Profile.Jobs.SetCurrentJob(JobId);
                    }
                }
                else if (jobButton.JobId == currentJobId)
                {
                    jobButton.Status = PlayerJobStatus.InProgress;
                }
            }

            UpdateJobOrders();
        }

        #region SelectJob
        private void SelectJob(JobButton currentButton)
        {
            JobSelected.gameObject.SetActive(true); //Set right side to be active

            JobDesc currentJob = currentButton.Job;

            //Assign Global Variables
            selectedButton = currentButton.GetTransform();
            currentJobId = currentJob.Id();

            //Used to select and deselect buttons. Set the "last selected button" to white if it exists then set the new button to gray
            if (selectedButtonImage)
            {
                selectedButtonImage.color = Color.white;
            }
            selectedButtonImage = selectedButton.GetComponent<Image>();
            selectedButtonImage.color = Color.gray;

            //TODO change the description based on if the job is accepted or not?
            CurrentJob.SetupJobSelect(currentButton);
            CurrentJob.SetupStatusButton();

        }

        #endregion //Select Job

        #region AcceptOrCompleteJob

        private JobButton FindButton(StringHash32 JobId)
        {
            foreach (JobButton job in JobButtons)
            {
                if (job.JobId.Equals(JobId))
                {
                    return job;
                }
            }

            Debug.Log("NO BUTTONS FOUND!!!!~~~~"); // TODO : how to throw error
            return null;
        }

        private ListHeader FindHeader(PlayerJobStatus status)
        {
            foreach (ListHeader header in Headers)
            {
                if (header.Status.Equals(status))
                {
                    return header;
                }
            }

            Debug.Log("NO HEADER FOUND!!!!~~~~");
            return null;
        }

        private List<JobButton> GetJobButtonsByStatus(PlayerJobStatus status)
        {
            List<JobButton> result = new List<JobButton>();
            foreach (JobButton jobBtn in JobButtons)
            {
                if (jobBtn.Status.Equals(status))
                {
                    result.Add(jobBtn);
                }
            }
            return result;
        }

        #endregion //Aceept Complete

        #region UpdateJobList

        //This function called anytime the lists are updated and will reorder the jobs of each category
        //Will sort the jobs of the lists to ensure that order is maintained
        private void UpdateJobOrders()
        { // TODO : change to PlayerJobList

            int siblingIdx = 0;

            ReorderJobs(PlayerJobStatus.Active, ref siblingIdx);
            ReorderJobs(PlayerJobStatus.InProgress, ref siblingIdx);
            ReorderJobs(PlayerJobStatus.NotStarted, ref siblingIdx);
            ReorderJobs(PlayerJobStatus.Completed, ref siblingIdx);

            CurrentJob.SetupStatusButton();

        }

        private void ReorderJobs(PlayerJobStatus status, ref int siblingIdx)
        {
            List<JobButton> jobs = GetJobButtonsByStatus(status);
            siblingIdx = UpdateHeaderText(status, siblingIdx);
            siblingIdx = UpdateJobList(jobs, siblingIdx);
        }

        //Helper function to update the index of the header text
        private int UpdateHeaderText(PlayerJobStatus status, int siblingIdx)
        {
            Transform headerText = FindHeader(status).GetTransform();
            // listHeaderToButton.TryGetValue(header, out headerText);
            headerText.SetSiblingIndex(siblingIdx++);

            return siblingIdx;
        }

        //Helper function to update the sibling indexes for each button in a specficic category
        private int UpdateJobList(List<JobButton> jobList, int siblingIdx)
        {
            foreach (JobButton job in jobList)
            {
                Transform jobButtonTransform = job.GetTransform();
                jobButtonTransform.SetSiblingIndex(siblingIdx++);
            }

            return siblingIdx;

        }
        #endregion //UpdateJobList

        #region ListCreation

        //Creates each seperate Job list, depending on what is passed in. Will get the list of jobs of that type and create all the buttons for them

        private void SetJobListActiveStatus(PlayerJobStatus status)
        {
            //create list header
            ActivateListHeader(status);
            foreach (JobButton jBtn in JobButtons)
            {
                PlayerJobStatus BtnStatus = jBtn.Status;
                if (status == BtnStatus)
                {
                    ActivateJobButton(jBtn);
                }
            }
        }
        private void ActivateListHeader(PlayerJobStatus Status)
        {
            foreach (ListHeader header in Headers)
            {
                if (header.Status.Equals(Status))
                {
                    header.GetTransform().gameObject.SetActive(true);
                }
            }
        }
        private void ActivateJobButton(JobButton jobBtn)
        {
            JobDesc currJob = jobBtn.Job;
            jobBtn.GetTransform().GetComponent<Button>().onClick.AddListener(() => SelectJob(jobBtn));
            jobBtn.GetTransform().gameObject.SetActive(true);
        }

        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
            SetupButtons(JobButtons);
            SetupHeaders(Headers);
            UpdateJobOrders();
        }
    }

    #endregion
}