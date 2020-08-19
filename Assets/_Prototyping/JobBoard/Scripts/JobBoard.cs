using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace ProtoAqua.JobBoard
{
    public class JobBoard : MonoBehaviour
    {
        public Sprite defaultSprite;
        private Transform container;
        private Transform jobButtonTemplate;

        private void Awake() {
            container = transform.Find("jobList");
            jobButtonTemplate = container.Find("jobButtonTemplate");
            jobButtonTemplate.gameObject.SetActive(false);
        }

        private void Start() {

            //Create The jobs (Will loop through sometihng later)
            CreateJobButton("The First Job",Job.getJobReward(Job.JobName.Job1), 1);
            CreateJobButton("The Second Job",Job.getJobReward(Job.JobName.Job2), 2);
            CreateJobButton("The Third Job",Job.getJobReward(Job.JobName.Job2), 3);

            Debug.Log("test");
        }

         public void SelectJob(string jobName) {
            Debug.Log("test button " + jobName);
        }

        

        //Creates a job button by replicating the "template" and creating a duplicate
        private void CreateJobButton(string jobName, int jobReward, int positionIndex) {
            Transform jobButtonTransform = Instantiate(jobButtonTemplate, container);
            RectTransform jobButtonRectTransform = jobButtonTemplate.GetComponent<RectTransform>();

            float jobButtonHeight = 80f;
            jobButtonRectTransform.anchoredPosition = new Vector2(0, -jobButtonHeight * positionIndex);

            jobButtonTransform.Find("jobName").GetComponent<TextMeshProUGUI>().SetText(jobName);
            jobButtonTransform.Find("jobReward").GetComponent<TextMeshProUGUI>().SetText(jobReward.ToString());
            
            jobButtonTransform.gameObject.SetActive(true);



            //jobButtonTransform.Find("jobPicture").GetComponent<Image>.sprite = defaultSprite;

            jobButtonTransform.GetComponent<Button>().onClick.AddListener(() => SelectJob(jobName));

        
        }


    }
}
