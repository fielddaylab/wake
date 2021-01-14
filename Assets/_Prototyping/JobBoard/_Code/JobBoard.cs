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

        private void Awake() {

            //Load the template and store it in variables to use later
            // panel = transform.Find("jobPanel");
            // grid = panel.Find("jobGrid");
            // selectedJob = transform.Find("selectedJob");
            // //disable the right side initially
            // selectedJob.gameObject.SetActive(false);

            CurrentJob = JobSelected.GetComponent<JobSelect>();
            JobSelected.gameObject.SetActive(false);
            JobButtons = JobPanel.GetComponentsInChildren<JobButton>(true);
            Headers = JobPanel.GetComponentsInChildren<ListHeader>(true);
            CurrentJob.GetStatusButton().GetComponent<Button>().onClick.AddListener(
                () => UpdateButtonByStatus(currentJobId));


            // acceptJobButton = selectedJob.Find("acceptJobButton");
            // // completeJobButton = selectedJob.Find("completeJobButton");
            // acceptJobButton.GetComponent<Button>().onClick.AddListener(() => AcceptAvailableJob(currentJobId));
            // // completeJobButton.GetComponent<Button>().onClick.AddListener(() => CompleteJob(currentJobId));
            // //Get the player component
            // player = playerObject.GetComponent<Player>();

            //Add all jobs to available jobs hard coded for now
            // player.addAvailableJob("job1");
            // player.addAvailableJob("job2");
            // player.addAvailableJob("job3");
            // player.addAvailableJob("job4");
            // PopulatePlayerJobs();
            

        }

        private void Start() {
            //TODO adjust headers if no active jobs/ completed jobs?
            // CreateJobList("Active");
            // CreateJobList("Available");
            // CreateJobList("Completed");
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
            foreach (PlayerJobStatus status in (PlayerJobStatus[]) Enum.GetValues(typeof(PlayerJobStatus)))
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

        // private void CompleteJob(StringHash32 JobId)
        // {
        //     foreach (JobButton jobButton in JobButtons)
        //     {
        //         if (jobButton.JobId.Equals(JobId) && jobButton.Status.Equals(PlayerJobStatus.InProgress))
        //         {
        //             jobButton.Status = PlayerJobStatus.Completed;
        //         }
        //     }
        //     UpdateJobOrders();
        // }

        #region LoadJobs

        //Loads the job list from JSON into the jobLIst object
        // private void LoadJobList()
        // {
        //     jobList = new JobList();
        //     Debug.Log("SERVICES ASSETS JOBS OBJECTS" + Services.Assets.Jobs.Objects.Count);
        //     foreach (JobDesc job in Services.Assets.Jobs.Objects)
        //     {
        //         jobList.Add(job);
        //     }
        // }

        //TODO add more logic to this
        //Takes the list of jobs and puts them within the player object
        // private void PopulatePlayerJobs() {
        //     foreach (JobDesc job in jobList.GetJobList())
        //     {
        //         player.addAvailableJob(job);
        //     }
        // }

        #endregion //JsonLoadJobs 

        #region SelectJob

        //Function added as a listener to each of the job buttons
        //Takes in the currentButton that is pressed and what jobId it holds
        //Updates the global variables and makes the button grey to signify it is selected
        //Most importantly changes the information in the selectJob section to reflect that a job was actually selected, and to display more information
        private void SelectJob(JobButton currentButton) {
            JobSelected.gameObject.SetActive(true); //Set right side to be active

            JobDesc currentJob = currentButton.Job;

            //Assign Global Variables
            selectedButton = currentButton.GetTransform();
            currentJobId = currentJob.Id();

            //Used to select and deselect buttons. Set the "last selected button" to white if it exists then set the new button to gray
            if(selectedButtonImage) {
                selectedButtonImage.color = Color.white;
            }
            selectedButtonImage = selectedButton.GetComponent<Image>();
            selectedButtonImage.color = Color.gray;

            //TODO change the description based on if the job is accepted or not?
            CurrentJob.SetupJobSelect(currentButton);
            CurrentJob.SetupStatusButton();

            //Change text for the selected side
            // selectedJob.Find("selectedJobName").GetComponent<LocText>().SetText(currentJob.NameId());
            // selectedJob.Find("selectedJobPostee").GetComponent<TextMeshProUGUI>().SetText("Posted By: " + currentJob.PosterId().ToDebugString());
            // selectedJob.Find("selectedJobReward").GetComponent<TextMeshProUGUI>().SetText(currentJob.GetRewardsStr());
            // selectedJob.Find("selectedJobDescription").GetComponent<TextMeshProUGUI>().SetText(currentJob.DescId().ToDebugString());

            // AdjustDifficulty(currentJob.ExperimentDifficulty() != 0, currentJob.ExperimentDifficulty(), selectedJob.Find("difficultyContainer").Find("experimentDifficulty"));
            // AdjustDifficulty(currentJob.ModelingDifficulty() != 0, currentJob.ModelingDifficulty(), selectedJob.Find("difficultyContainer").Find("modelDifficulty"));
            // AdjustDifficulty(currentJob.ArgumentationDifficulty() != 0, currentJob.ArgumentationDifficulty(), selectedJob.Find("difficultyContainer").Find("argumentDifficulty"));


            // UpdateButton(currentButton);


        }

        //Updates bottom button, depending what state the job is in. Active/Available/Complete
        // private void UpdateButton(JobButton job) // TODO : update to jobselect
        // {
        //     JobSelect.SetupJobSelect(job);



        //     if (job.Status.Equals(PlayerJobStatus.InProgress)) {
        //         // completeJobButton.gameObject.SetActive(true);
        //         acceptJobButton.gameObject.SetActive(false);
        //     } else if (job.Status.Equals(PlayerJobStatus.NotStarted)) {
        //         acceptJobButton.gameObject.SetActive(true);
        //         // completeJobButton.gameObject.SetActive(false)
        //     } else {
        //         acceptJobButton.gameObject.SetActive(false);
        //         // completeJobButton.gameObject.SetActive(false);
        //     }
        // }

        // private void AdjustDifficulty(bool active, int difficulty, Transform difficultyObject) {
        //     difficultyObject.gameObject.SetActive(active);
        //     Transform starContainer = difficultyObject.Find("starContainer");
            
        //     for(int i = 1; i <= 5; i++) {
        //         Image star = starContainer.Find("star" + i).GetComponent<Image>();
        //         if(i <= difficulty) {
        //             star.color = Color.yellow;
        //         } else {
        //             star.color = Color.white;
        //         }
        //     }
        // }

        #endregion //Select Job
        
        #region AcceptOrCompleteJob
        //Listener Function added to the button for when a job is accepted
        //Updates the list of jobs as well as the list that the player holds
        // private void AcceptJob(StringHash32 jobId) {
        //     player.acceptAvailableJob(jobId);

        //     Debug.Log("Accepted Job" + jobId);

        //     UpdateJobOrders();
        // }

        // private void CompleteJob(StringHash32 jobId) {
        //     player.completeJob(jobId);

        //     UpdateJobOrders();
        // }

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
        private void UpdateJobOrders() { // TODO : change to PlayerJobList

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

        //Sorts job order
        // private int SortByJobOrder(string left, string right) {
        //     int leftOrder = GetJobOrder(left);
        //     int rightOrder = GetJobOrder(right);
        //     return left.CompareTo(right);
        // }

        // //Returns index of job to sort
        // private int GetJobOrder(string jobId) {
        //     return jobList.findJob(jobId).jobIndex;

        // }
        
        //Helper function to update the index of the header text
        private int UpdateHeaderText(PlayerJobStatus status, int siblingIdx) {
            Transform headerText = FindHeader(status).GetTransform();
            // listHeaderToButton.TryGetValue(header, out headerText);
            headerText.SetSiblingIndex(siblingIdx++);

            return siblingIdx; 
        }

        //Helper function to update the sibling indexes for each button in a specficic category
        private int UpdateJobList(List<JobButton> jobList, int siblingIdx) {
            foreach(JobButton job in jobList) {
                Transform jobButtonTransform = job.GetTransform();
                jobButtonTransform.SetSiblingIndex(siblingIdx++);
            }

            return siblingIdx;

        }

        //Helper function to get the Transform out of the dicionary
        // private Transform GetButtonForJobId(StringHash32 jobId) {
        //     Transform jobButtonTransform = null;
        //     if(!jobIdToButton.TryGetValue(jobId.ToString(), out jobButtonTransform)) {
        //         Debug.Log("ERROR: No Job button for jobId " + jobId.ToString());
        //     }
        //     return jobButtonTransform;
        // }

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
                // else
                // {
                //     jBtn.GetTransform().gameObject.SetActive(false);
                // }
            }
        }
        // private void CreateJobList(string type) {
        //     CreateListHeader(type); //Create the "header"

        //     List<JobDesc> jobList = new List<JobDesc>();

        //     //Check what part of the List being made, and get those jobs
        //     if(type.Equals("Active")) {
        //         jobList = player.getActiveJobs();
        //     } else if(type.Equals("Available")) {
        //         jobList = player.getAvailableJobs();
        //     } else if (type.Equals("Completed")) {
        //         jobList = player.getCompletedJobs();
        //     }
        //     //Loop through each job and create a button for it
        //     foreach(JobDesc job in jobList) {
        //         CreateJobButton(job);
        //     }

        // }


        //Creates a header for the list taking in a listHeader and creating a object for it
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
        // private void CreateListHeader(string listHeader) {
        //     GameObject listHeaderObject = Instantiate(listHeaderPrefab);
        //     Transform listHeaderTransform = listHeaderObject.transform;
        //     listHeaderTransform.SetParent(grid.transform);

        //     listHeaderTransform.Find("listText").GetComponent<TextMeshProUGUI>().SetText(listHeader);

        //     listHeaderToButton.Add(listHeader,listHeaderTransform); //Add header to dictionary
        // }

        //Creates a job button by taking in a jobId and getting the data from "job.cs"

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
        //old

        // private void CreateJobButton(JobDesc job) 
        // {


        //     //Get Job out of jobList
        //     JobDesc currentJob = job;

        //     //Instatiate a new job button and set its parent to the grid
        //     GameObject jobButton = Instantiate(jobButtonPrefab);
        //     Transform jobButtonTransform = jobButton.transform;
        //     jobButtonTransform.SetParent(grid.transform);

        //     LocText jobName = jobButtonTransform.Find("jobName").GetComponent<LocText>();

        //     //Find The components to replace their texts
        //     jobName.SetText(currentJob.NameId());
        //     jobButtonTransform.Find("jobReward").GetComponent<TextMeshProUGUI>().SetText(currentJob.CashReward().ToString());
        //     jobButtonTransform.Find("jobPicture").GetComponent<Image>().sprite = currentJob.Icon();

        //     //Add the listener for selecting a job to show on the right side
        //     jobButtonTransform.GetComponent<Button>().onClick.AddListener(() => SelectJob(currentJob, jobButtonTransform));

        //     jobIdToButton.Add(currentJob.Id().ToString(), jobButtonTransform);  //Add button to dictionary
        //     jobButtonTransform.gameObject.SetActive(true); //Show the new object

        // }

        #endregion //List Creation

        #region Iserlializer

        // bool ISerializerContext.TryGetAssetId<T>(T inObject, out string outId)
        // { 
        //     outId = null;
        //     return false;
        // }

        // bool ISerializerContext.TryResolveAsset<T>(string inId, out T outObject)
        // {
        //     if (typeof(T) == typeof(Sprite))
        //     {
        //         foreach (var spr in spriteRefs)
        //         {
        //             if (spr.name == inId)
        //             {
        //                 outObject = spr as T;
        //                 return true;
        //             }
        //         }
        //     }

        //     outObject = null;
        //     return false;
        // }

        #endregion //Iserializer
    }
}
