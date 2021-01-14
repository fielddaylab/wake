using UnityEngine;
using Aqua;
using BeauUtil;
using BeauData;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace ProtoAqua.JobBoard
{
    public class DifficultyCategory : MonoBehaviour
    {
        [SerializeField] public Image Icon = null;
        [SerializeField] public GameObject StarContainer = null;
        [SerializeField] public DifficultyType DType;

        private List<Transform> Stars;
        public int Value { get; set; }

        public void DifficultySetup(JobDesc job, DifficultyType dtype)
        {
            Stars = new List<Transform>();

            Value = 0;


            StarContainer.GetImmediateComponentsInChildren<Transform>(false, Stars);
            if (Stars.Count != 5)
            {
                throw new Exception("not enough or too many stars :" + Stars.Count + DType);
            }

            if (dtype.Equals(DifficultyType.Argumentation)) {
                Value = job.ArgumentationDifficulty();
            }
            else if (dtype.Equals(DifficultyType.Experimentation)) {
                Value = job.ExperimentDifficulty();
            }
            else {
                Value = job.ArgumentationDifficulty();
            }

            int i = 0;
            foreach (Transform star in Stars)
            {
                Image starImage = star.GetComponent<Image>();
                if (i < Value)
                {
                    starImage.color = Color.yellow;
                }
                else
                {
                    starImage.color = Color.white;
                }
                i++;
            }

            return;

        }
        
    }
}