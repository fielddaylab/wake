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
    public class ListHeader : MonoBehaviour
    {
        [SerializeField] public Transform HeaderTransform = null;
        [SerializeField] public LocText m_Label = null;

        public PlayerJobStatus Status { get; set; }

        public Transform GetTransform()
        {
            return HeaderTransform;
        }

        public void SetText(string text)
        {
            if (text != null)
            {
                m_Label.SetText(text);
            }

        }

        public void Update()
        {
            if (Status.Equals(PlayerJobStatus.InProgress))
            {
                SetText("Active"); // TODO : clean this to a dict or enum
            }
            else if (Status.Equals(PlayerJobStatus.Completed))
            {
                SetText("Completed");
            }
            else
            {
                SetText("Available");
            }
        }

        
    }



}
