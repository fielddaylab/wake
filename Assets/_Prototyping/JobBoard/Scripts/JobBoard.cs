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

        

        private Transform panel;
        private Transform grid;
        private Transform jobButtonTemplate;
        private Transform selectedJob;

        private Image selectedButton;

        private void Awake() {

            //Load the template and store it in variables to use later
            panel = transform.Find("jobPanel");
            grid = panel.Find("jobGrid");
            selectedJob = transform.Find("selectedJob");
            //disablet the right side initially
            selectedJob.gameObject.SetActive(false);
            //jobButtonTemplate = grid.Find("jobButtonTemplate");
            //jobButtonTemplate.gameObject.SetActive(false);

        }

        private void Start() {

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

            if(selectedButton) {
                selectedButton.color = Color.white;
            }
            selectedButton = currentButton;
            selectedButton.color = Color.gray;

            selectedJob.Find("selectedJobName").GetComponent<TextMeshProUGUI>().SetText(Job.getJobName(jobId));
            selectedJob.Find("selectedJobPostee").GetComponent<TextMeshProUGUI>().SetText("Posted By: " + Job.getJobPostee(jobId));
            //Set Difficulty ? have to decide how to judge this
            selectedJob.Find("selectedJobReward").GetComponent<TextMeshProUGUI>().SetText(Job.getJobReward(jobId).ToString());
            selectedJob.Find("selectedJobDescription").GetComponent<TextMeshProUGUI>().SetText(Job.getJobDescription(jobId));
            //Adjust Button if Active job/Available job/Completed Job



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
