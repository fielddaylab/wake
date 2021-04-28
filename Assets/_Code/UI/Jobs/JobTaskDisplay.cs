using System;
using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauPools;

namespace Aqua
{
    public class JobTaskDisplay : MonoBehaviour, IPoolAllocHandler
    {
        [Required] public RectTransform Root;
        [Required] public CanvasGroup Group;

        [Header("Display")]
        [Required] public LocText Label;
        [Required] public Graphic Background;
        public LocText Description;
        public Graphic Checkmark;

        [Header("Animation")]
        public Graphic Flash;

        [NonSerialized] public JobTask Task;
        [NonSerialized] public bool Completed;
        [NonSerialized] internal float DesiredY;

        public void Populate(JobTask inTask, bool inbCompleted)
        {
            Task = inTask;
            Populate(inTask.LabelId, inTask.DescriptionId, inbCompleted);
        }

        public void Populate(TextId inLabelId, TextId inDescId, bool inbCompleted)
        {
            Label.SetText(inLabelId);
            if (Description)
                Description.SetText(inDescId);
            if (Checkmark)
                Checkmark.enabled = inbCompleted;

            Completed = inbCompleted;
        }

        void IPoolAllocHandler.OnAlloc()
        {
            if (Flash)
                Flash.enabled = false;
        }

        void IPoolAllocHandler.OnFree()
        {
            Task = null;
            
            if (Flash)
                Flash.enabled = false;
            if (Checkmark)
                Checkmark.enabled = false;
        }
    }
}