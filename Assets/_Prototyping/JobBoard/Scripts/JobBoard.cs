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

        [SerializeField] private GameObject jobButtonPrefab;
        [SerializeField] private GameObject listHeaderPrefab;


        [SerializeField] private GameObject playerObject;
        

        private Transform panel;
        private Transform grid;
        private Transform jobButtonTemplate;
        private Transform selectedJob;
        private Player player;

        private Image selectedButton;

        private void Awake() {

            //Load the template and store it in variables to use later
            panel = transform.Find("jobPanel");
            grid = panel.Find("jobGrid");
            selectedJob = transform.Find("selectedJob");
            //disable the right side initially
            selectedJob.gameObject.SetActive(false);

            player = playerObject.GetComponent<Player>();

            //Add all jobs to available jobs hard coded for now
            player.addAvailableJob("job1");
            player.addAvailableJob("job2");
            player.addAvailableJob("job3");
            player.addAvailableJob("job4");

        }

        private void Start() {

            //Add Active Jobs

            //Add Available jobs

            //Add completed jobs

            //Create The jobs (Will loop through sometihng later)
            CreateListHeader("Active");
            CreateJobButton("job1");
            CreateJobButton("job2");
            CreateJobButton("job3");
            CreateJobButton("job4");
            CreateListHeader("Available");
            CreateJobButton("job1");
            CreateJobButton("job2");
            CreateJobButton("job3");
            CreateJobButton("job4");
            CreateJobButton("job1");
            CreateJobButton("job2");
            CreateJobButton("job3");
            CreateJobButton("job4");
            
        }

         public void SelectJob(string jobId, Image currentButton) {
            selectedJob.gameObject.SetActive(true);

            //Used to select and deselect buttons. Set the "last selected button" to white if it exists then set the new button to gray
            if(selectedButton) {
                selectedButton.color = Color.white;
            }
            selectedButton = currentButton;
            selectedButton.color = Color.gray;

            //Change text for the selected side
            selectedJob.Find("selectedJobName").GetComponent<TextMeshProUGUI>().SetText(Job.getJobName(jobId));
            selectedJob.Find("selectedJobPostee").GetComponent<TextMeshProUGUI>().SetText("Posted By: " + Job.getJobPostee(jobId));
            //Set Difficulty ? have to decide how to judge this
            selectedJob.Find("selectedJobReward").GetComponent<TextMeshProUGUI>().SetText(Job.getJobReward(jobId).ToString());
            selectedJob.Find("selectedJobDescription").GetComponent<TextMeshProUGUI>().SetText(Job.getJobDescription(jobId));
            

            //TODO Adjust Button if Active job/Available job/Completed Job
            ArrayList availableJobs = player.getAvailableJobs();
            ArrayList acceptedJobs = player.getAcceptedJobs();

            //Is this the best way to do this?
            selectedJob.Find("selectedJobButton").GetComponent<Button>().onClick.RemoveAllListeners(); //To make sure it is only listening to the current job
            selectedJob.Find("selectedJobButton").GetComponent<Button>().onClick.AddListener(() => AcceptJob(jobId));
           
            //TODO add functionality to change a "accepted button"
        

        }

        private void AcceptJob(string jobId) {
            player.acceptAvailableJob(jobId);
            Debug.Log("Accepted Job" + jobId);

            //Need to reload list after this
        }

        private void CreateListHeader(string listHeader) {
            GameObject listHeaderObject = Instantiate(listHeaderPrefab);
            Transform listHeaderTransform = listHeaderObject.transform;
            listHeaderTransform.parent = grid.transform;

            listHeaderTransform.Find("listText").GetComponent<TextMeshProUGUI>().SetText(listHeader);
        }
        

        //Creates a job button by taking in a jobId and getting the data from "job.cs"
        private void CreateJobButton(string jobId) {

            //Instatiate a new job button and set its parent to the grid
            GameObject jobButton = Instantiate(jobButtonPrefab);
            Transform jobButtonTransform = jobButton.transform;
            jobButtonTransform.parent = grid.transform;


            //Find The components to replace their texts
            jobButtonTransform.Find("jobName").GetComponent<TextMeshProUGUI>().SetText(Job.getJobName(jobId));
            jobButtonTransform.Find("jobReward").GetComponent<TextMeshProUGUI>().SetText(Job.getJobReward(jobId).ToString());
            //jobButtonTransform.Find("jobPicture").GetComponent<Image>.sprite = defaultSprite;
            
            //Add the listener for selecting a job to show on the right side
            jobButtonTransform.GetComponent<Button>().onClick.AddListener(() => SelectJob(jobId, jobButtonTransform.GetComponent<Image>()));

            jobButtonTransform.gameObject.SetActive(true); 

        
        }


    }
}
