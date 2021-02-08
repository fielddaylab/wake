using UnityEngine;
using Aqua;
using BeauUtil;
using BeauData;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

namespace Aqua.Portable
{
    public class DifficultyCategory : MonoBehaviour
    {
        [SerializeField] public Image Icon = null;
        [SerializeField] public Transform StarContainer = null;
        [SerializeField] public DifficultyType DType;

        private List<Transform> Stars;
        public int Value { get; set; }

        public void DifficultySetup(JobDesc job, DifficultyType dtype)
        {
            Stars = new List<Transform>();

            Value = 0;


            // StarContainer.gameObject.GetImmediateComponentsInChildren<Transform>(false, Stars);
            // if (Stars.Count != 5)
            // {
            //     throw new Exception("not enough or too many stars : " + Stars.Count + DType);
            // }

            Value = job.Difficulty(dtype);

            int i = 0;
            foreach (Transform star in StarContainer)
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