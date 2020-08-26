using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeauData;

namespace ProtoAqua.JobBoard
{
    public class JobBoard : MonoBehaviour, ISerializerContext
    {
        // public Sprite defaultSprite;

        [SerializeField] private TextAsset jobListJSON = null;

        //Prefabs
        [SerializeField] private GameObject jobButtonPrefab = null;
        [SerializeField] private GameObject listHeaderPrefab = null;
        [SerializeField] private Sprite[] spriteRefs = null;

        //Player wil be adjusted later
        [SerializeField] private GameObject playerObject = null;
        private Player player;

        private JobList jobList;

        private string currentJobId;

        //Transforms used in Awake to create Buttons and adjust selectedJob
        private Transform panel;
        private Transform grid;
        private Transform jobButtonTemplate;
        private Transform selectedJob;

  
        //Used to change color of selectedButton and adjust button when new one is pressed
        private Transform selectedButton = null;
        private Image selectedButtonImage = null;

        private Transform acceptJobButton = null;
        private Transform completeJobButton = null;

        //Dictionary to match IDs used when updating the lists
        private Dictionary<string, Transform> jobIdToButton = new Dictionary<string, Transform>(); //Used to translate JobId to button transform
        private Dictionary<string, Transform> listHeaderToButton = new Dictionary<string, Transform>(); //Used to translate the listHeaders to buttons



        private void Awake() {

            //Load the template and store it in variables to use later
            panel = transform.Find("jobPanel");
            grid = panel.Find("jobGrid");
            selectedJob = transform.Find("selectedJob");
            //disable the right side initially
            selectedJob.gameObject.SetActive(false);

            acceptJobButton = selectedJob.Find("acceptJobButton");
            completeJobButton = selectedJob.Find("completeJobButton");
            acceptJobButton.GetComponent<Button>().onClick.AddListener(() => AcceptJob(currentJobId));
            completeJobButton.GetComponent<Button>().onClick.AddListener(() => CompleteJob(currentJobId));
            //Get the player component
            player = playerObject.GetComponent<Player>();

            //Add all jobs to available jobs hard coded for now
            // player.addAvailableJob("job1");
            // player.addAvailableJob("job2");
            // player.addAvailableJob("job3");
            // player.addAvailableJob("job4");
            
            loadJobList();
            populatePlayerJobs();

        }

        private void Start() {
            //TODO adjust headers if no active jobs/ completed jobs?
            CreateJobList("Active");
            CreateJobList("Available");
            CreateJobList("Completed");
        }

        //Loads the job list from JSON into the jobLIst object
        private void loadJobList() {
            Serializer.Read(ref jobList, jobListJSON, Serializer.Format.JSON, this);
        }

        //TODO add more logic to this
        //Takes the list of jobs and puts them within the player object
        private void populatePlayerJobs() {
            Job[] jobs = jobList.getJobList();
            for(int i = 0; i < jobs.Length; i++) {
                player.addAvailableJob(jobs[i].jobId);
            }
        }
        
        //Function added as a listener to each of the job buttons
        //Takes in the currentButton that is pressed and what jobId it holds
        //Updates the global variables and makes the button grey to signify it is selected
        //Most importantly changes the information in the selectJob section to reflect that a job was actually selected, and to display more information
         private void SelectJob(string jobId, Transform currentButton) {
            selectedJob.gameObject.SetActive(true); //Set right side to be active

            Job currentJob = jobList.findJob(jobId);

            //Assign Global Variables
            selectedButton = currentButton;
            currentJobId = jobId;

            //Used to select and deselect buttons. Set the "last selected button" to white if it exists then set the new button to gray
            if(selectedButtonImage) {
                selectedButtonImage.color = Color.white;
            }
            selectedButtonImage = currentButton.GetComponent<Image>();
            selectedButtonImage.color = Color.gray;

             //TODO change the description based on if the job is accepted or not?

            
            //Change text for the selected side
            selectedJob.Find("selectedJobName").GetComponent<TextMeshProUGUI>().SetText(currentJob.jobName);
            selectedJob.Find("selectedJobPostee").GetComponent<TextMeshProUGUI>().SetText("Posted By: " + currentJob.jobPostee);
            selectedJob.Find("selectedJobReward").GetComponent<TextMeshProUGUI>().SetText(currentJob.jobReward.ToString());
            selectedJob.Find("selectedJobDescription").GetComponent<TextMeshProUGUI>().SetText(currentJob.jobDescription);
            //Set image

            adjustDifficulty(currentJob.experimentation, currentJob.experimentationDifficulty, selectedJob.Find("difficultyContainer").Find("experimentDifficulty"));
            adjustDifficulty(currentJob.modeling, currentJob.modelingDifficulty, selectedJob.Find("difficultyContainer").Find("modelDifficulty"));
            adjustDifficulty(currentJob.argument, currentJob.argumentDifficulty, selectedJob.Find("difficultyContainer").Find("argumentDifficulty"));

            
            updateButton(jobId);


        }

        //Updates bottom button, depending what state the job is in. Active/Available/Complete
        private void updateButton(string jobId) {
            if (player.getActiveJobs().Contains(jobId)) {
                completeJobButton.gameObject.SetActive(true);
                acceptJobButton.gameObject.SetActive(false);
            } else if (player.getAvailableJobs().Contains(jobId)) {
                acceptJobButton.gameObject.SetActive(true);
                completeJobButton.gameObject.SetActive(false);
            } else {
                acceptJobButton.gameObject.SetActive(false);
                completeJobButton.gameObject.SetActive(false);
            }
        }

        private void adjustDifficulty(bool active, int difficulty, Transform difficultyObject) {
            difficultyObject.gameObject.SetActive(active);
            Transform starContainer = difficultyObject.Find("starContainer");
            
            for(int i = 1; i <= 5; i++) {
                Image star = starContainer.Find("star" + i).GetComponent<Image>();
                if(i <= difficulty) {
                    star.color = Color.yellow;
                } else {
                    star.color = Color.white;
                }
            }
            

        }

        //Listener Function added to the button for when a job is accepted
        //Updates the list of jobs as well as the list that the player holds
        private void AcceptJob(string jobId) {
            player.acceptAvailableJob(jobId);

            Debug.Log("Accepted Job" + jobId);

            UpdateJobOrders();
        }

        private void CompleteJob(string jobId) {
            player.completeJob(jobId);

            UpdateJobOrders();
        }

        //This function called anytime the lists are updated and will reorder the jobs of each category
        //Will sort the jobs of the lists to ensure that order is maintained
        private void UpdateJobOrders() {

            int siblingIdx = 0;

            //1. Reorder active jobs
            List<string> activeJobs = player.getActiveJobs();
            activeJobs.Sort(SortByJobOrder);
            siblingIdx = UpdateHeaderText("Active", siblingIdx);
            siblingIdx = UpdateJobList(activeJobs, siblingIdx);

            //2. Reorder Available jobs
            List<string> availableJobs = player.getAvailableJobs();
            availableJobs.Sort(SortByJobOrder);
            siblingIdx = UpdateHeaderText("Available", siblingIdx);
            siblingIdx = UpdateJobList(availableJobs, siblingIdx);

            //3. Reorder Completed Jobs
            List<string> completedJobs = player.getCompletedJobs();
            completedJobs.Sort(SortByJobOrder);
            siblingIdx = UpdateHeaderText("Completed", siblingIdx);
            siblingIdx = UpdateJobList(completedJobs, siblingIdx);

            updateButton(currentJobId);

        }

        //Sorts job order
        private int SortByJobOrder(string left, string right) {
            int leftOrder = GetJobOrder(left);
            int rightOrder = GetJobOrder(right);
            return left.CompareTo(right);
        }

        //Returns index of job to sort
        private int GetJobOrder(string jobId) {
            return jobList.findJob(jobId).jobIndex;

        }
        

        //Helper function to update the index of the header text
        private int UpdateHeaderText(string header, int siblingIdx) {
            Transform headerText;
            listHeaderToButton.TryGetValue(header, out headerText);
            headerText.SetSiblingIndex(siblingIdx++);

            return siblingIdx; 
        }

        //Helper function to update the sibling indexes for each button in a specficic category
        private int UpdateJobList(List<string> jobList, int siblingIdx) {
            foreach(string jobId in jobList) {
                Transform jobButtonTransform = GetButtonForJobId(jobId);
                jobButtonTransform.SetSiblingIndex(siblingIdx++);
            }

            return siblingIdx;

        }

        //Helper function to get the Transform out of the dicionary
        private Transform GetButtonForJobId(string jobId) {
            Transform jobButtonTransform = null;
            if(!jobIdToButton.TryGetValue(jobId, out jobButtonTransform)) {
                Debug.Log("ERROR: No Job button for jobId " + jobId);
            }
            return jobButtonTransform;
        }   

        //Creates each seperate Job list, depending on what is passed in. Will get the list of jobs of that type and create all the buttons for them
        private void CreateJobList(string type) {
            CreateListHeader(type); //Create the "header"

            List<string> jobList = new List<string>();

            //Check what part of the List being made, and get those jobs
            if(type.Equals("Active")) {
                jobList = player.getActiveJobs();
            } else if(type.Equals("Available")) {
                jobList = player.getAvailableJobs();
            } else if (type.Equals("Completed")) {
                jobList = player.getCompletedJobs();
            }
            //Loop through each job and create a button for it
            foreach(string jobId in jobList) {
                CreateJobButton(jobId);
            }

        }
        
       //Creates a header for the list taking in a listHeader and creating a object for it
        private void CreateListHeader(string listHeader) {
            GameObject listHeaderObject = Instantiate(listHeaderPrefab);
            Transform listHeaderTransform = listHeaderObject.transform;
            listHeaderTransform.SetParent(grid.transform);

            listHeaderTransform.Find("listText").GetComponent<TextMeshProUGUI>().SetText(listHeader);

            listHeaderToButton.Add(listHeader,listHeaderTransform); //Add header to dictionary
        }
        

        //Creates a job button by taking in a jobId and getting the data from "job.cs"
        private void CreateJobButton(string jobId) {

            //Get Job out of jobList
            Job currentJob = jobList.findJob(jobId);

            //Instatiate a new job button and set its parent to the grid
            GameObject jobButton = Instantiate(jobButtonPrefab);
            Transform jobButtonTransform = jobButton.transform;
            jobButtonTransform.SetParent(grid.transform);


            //Find The components to replace their texts
            jobButtonTransform.Find("jobName").GetComponent<TextMeshProUGUI>().SetText(currentJob.jobName);
            jobButtonTransform.Find("jobReward").GetComponent<TextMeshProUGUI>().SetText(currentJob.jobReward.ToString());
            jobButtonTransform.Find("jobPicture").GetComponent<Image>().sprite = currentJob.sprite;
    
            //Add the listener for selecting a job to show on the right side
            jobButtonTransform.GetComponent<Button>().onClick.AddListener(() => SelectJob(jobId, jobButtonTransform));

            jobIdToButton.Add(jobId, jobButtonTransform);  //Add button to dictionary
            jobButtonTransform.gameObject.SetActive(true); //Show the new object
        
        }

        bool ISerializerContext.TryGetAssetId<T>(T inObject, out string outId)
        { 
            outId = null;
            return false;
        }

        bool ISerializerContext.TryResolveAsset<T>(string inId, out T outObject)
        {
            if (typeof(T) == typeof(Sprite))
            {
                foreach (var spr in spriteRefs)
                {
                    if (spr.name == inId)
                    {
                        outObject = spr as T;
                        return true;
                    }
                }
            }

            outObject = null;
            return false;
        }

    }
}
