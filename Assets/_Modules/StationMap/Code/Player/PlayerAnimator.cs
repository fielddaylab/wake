using System.Collections;
using UnityEngine;
using BeauRoutine;
using BeauUtil;
using Aqua.Cameras;

namespace Aqua.StationMap
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Animation m_Animation = null;

        private Routine m_DiveRoutine;

        private void OnEnable()
        {
            Services.Events.Register(DiveSite.Event_Dive, StartDiving, this);
        }

        private void OnDisable()
        {
            Services.Events?.DeregisterAll(this);
        }

        private void StartDiving()
        {
            m_DiveRoutine.Replace(this, DiveRoutine());
        }

        private IEnumerator DiveRoutine()
        {
            Services.Audio.PostEvent("LowerSubFoley");
            m_Animation.Play();
            while(m_Animation.isPlaying)
                yield return null;
        }
    }
}