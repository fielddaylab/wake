using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace ProtoAqua.JobBoard
{
    public class JobBoard : MonoBehaviour
    {
        // public Sprite defaultSprite;

        //Prefabs
        [SerializeField] private GameObject jobButtonPrefab = null;
        [SerializeField] private GameObject listHeaderPrefab = null;

        //Player wil be adjusted later
        [SerializeField] private GameObject playerObject = null;
        private Player player;

        private string currentJobId;

        //Transforms used in Awake to create Buttons and adjust selectedJob
        private Transform panel;
        private Transform grid;
        private Transform jobButtonTemplate;
        private Transform selectedJob;

  
        //Used to change color of selectedButton and adjust button when new one is pressed
        private Transform selectedButton = null;
        private Image selectedButtonImage = null;

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
            selectedJob.Find("selectedJobButton").GetComponent<Button>().onClick.AddListener(() => AcceptJob(currentJobId));
            //Get the player component
            player = playerObject.GetComponent<Player>();

            //Add all jobs to available jobs hard coded for now
            player.addAvailableJob("job1");
            player.addAvailableJob("job2");
            player.addAvailableJob("job3");
            player.addAvailableJob("job4");

        }

        private void Start() {

            //Add Active Jobs
            CreateJobList("Active");

            //Add Available jobs
            CreateJobList("Available");

            //Add completed jobs?
            CreateJobList("Completed");

            
        }
        
        //Function added as a listener to each of the job buttons
        //Takes in the currentButton that is pressed and what jobId it holds
        //Updates the global variables and makes the button grey to signify it is selected
        //Most importantly changes the information in the selectJob section to reflect that a job was actually selected, and to display more information
         private void SelectJob(string jobId, Transform currentButton) {
            selectedJob.gameObject.SetActive(true); //Set right side to be active

            //Assign Global Variables
            selectedButton = currentButton;
            currentJobId = jobId;

            //TODO better way to do this
            //Used to select and deselect buttons. Set the "last selected button" to white if it exists then set the new button to gray
            if(selectedButtonImage) {
                selectedButtonImage.color = Color.white;
            }
            selectedButtonImage = currentButton.GetComponent<Image>();
            selectedButtonImage.color = Color.gray;

             //TODO change the description based on if the job is accepted or not?

            
            //Change text for the selected side
            selectedJob.Find("selectedJobName").GetComponent<TextMeshProUGUI>().SetText(Job.getJobName(jobId));
            selectedJob.Find("selectedJobPostee").GetComponent<TextMeshProUGUI>().SetText("Posted By: " + Job.getJobPostee(jobId));
            //Set Difficulty ? have to decide how to judge this
            selectedJob.Find("selectedJobReward").GetComponent<TextMeshProUGUI>().SetText(Job.getJobReward(jobId).ToString());
            selectedJob.Find("selectedJobDescription").GetComponent<TextMeshProUGUI>().SetText(Job.getJobDescription(jobId));
            

            //TODO Adjust Button if Active job/Available job/Completed Job


            //TODO add functionality to change a "accepted button"

           
        

        }

        //Listener Function added to the button for when a job is accepted
        //Updates the list of jobs as well as the list that the player holds
        private void AcceptJob(string jobId) {
            player.acceptAvailableJob(jobId);

            Debug.Log("Accepted Job" + jobId);

            UpdateJobOrders();
        }

        //This function called anytime the lists are updated and will reorder the jobs of each category
        //Will sort the jobs of the lists to ensure that order is maintained
        private void UpdateJobOrders() {

            int siblingIdx = 0;

            //1. Reorder active jobs
            List<string> activeJobs = player.getActiveJobs();
            //TODO sort active jobs
            siblingIdx = UpdateHeaderText("Active", siblingIdx);
            siblingIdx = UpdateJobList(activeJobs, siblingIdx);

            //2. Reorder Available jobs
            List<string> availableJobs = player.getAvailableJobs();
            //TODO sort Available jobs
            siblingIdx = UpdateHeaderText("Available", siblingIdx);
            siblingIdx = UpdateJobList(availableJobs, siblingIdx);

            //3. Reorder Completed Jobs
            List<string> completedJobs = player.getCompletedJobs();

            //TODO sort completed jobs
            siblingIdx = UpdateHeaderText("Completed", siblingIdx);
            siblingIdx = UpdateJobList(completedJobs, siblingIdx);


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

            //Instatiate a new job button and set its parent to the grid
            GameObject jobButton = Instantiate(jobButtonPrefab);
            Transform jobButtonTransform = jobButton.transform;
            jobButtonTransform.SetParent(grid.transform);


            //Find The components to replace their texts
            jobButtonTransform.Find("jobName").GetComponent<TextMeshProUGUI>().SetText(Job.getJobName(jobId));
            jobButtonTransform.Find("jobReward").GetComponent<TextMeshProUGUI>().SetText(Job.getJobReward(jobId).ToString());
            //jobButtonTransform.Find("jobPicture").GetComponent<Image>.sprite = defaultSprite;
    
            //Add the listener for selecting a job to show on the right side
            jobButtonTransform.GetComponent<Button>().onClick.AddListener(() => SelectJob(jobId, jobButtonTransform));

            jobIdToButton.Add(jobId, jobButtonTransform);  //Add button to dictionary
            jobButtonTransform.gameObject.SetActive(true); //Show the new object
        
        }


    }
}
