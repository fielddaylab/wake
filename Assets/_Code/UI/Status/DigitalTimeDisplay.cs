using UnityEngine;
using TMPro;
using BeauUtil;

namespace Aqua
{
    public class DigitalTimeDisplay : MonoBehaviour, ITimeHandler
    {
        [SerializeField, Required] private TMP_Text m_Display = null;

        private void OnEnable()
        {
            Services.Time.Register(this);
        }

        private void OnDisable()
        {
            Services.Time?.Deregister(this);
        }

        public void OnTimeChanged(GTDate inGameTime)
        {
            int hour = inGameTime.Hour;
            m_Display.SetText(inGameTime.ToTimeString());
        }

        public TimeEvent EventMask()
        {
            return TimeEvent.Tick;
        }
    }
}