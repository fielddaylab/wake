using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BeauPools;

namespace ProtoAqua.Energy
{
    public class SimOutput : MonoBehaviour
    {
        #region Inspector

        [Header("Env Vars")]

        [SerializeField] private TMP_Text[] m_EnvVarCounts = null;

        [Header("Actor Counts")]

        [SerializeField] private TMP_Text[] m_ActorCounts = null;

        [Header("Populations")]

        [SerializeField] private ImagePool m_ActorPool = null;

        #endregion // Inspector

        #region Unity Events

        private void Awake()
        {
            m_ActorPool.Initialize();
        }

        #endregion // Unity Events

        public void Display(in EnergySimContext inContext)
        {
            DisplayEnvVars(inContext);
            DisplayActorCounts(inContext);
        }

        private void DisplayEnvVars(in EnergySimContext inContext)
        {
            int textIdx = 0;
            int resCount = inContext.Database.ResourceCount();
            for(int i = 0; i < resCount; ++i)
            {
                TMP_Text element = m_EnvVarCounts[textIdx++];

                ushort count = inContext.Current.Environment.OwnedResources[i];
                element.SetText(string.Format("{0}: {1}", inContext.Database.ResourceVarIds()[i].ToString(), count));
                element.gameObject.SetActive(true);
            }

            int propCount = inContext.Database.PropertyCount();
            for(int i = 0; i < propCount; ++i)
            {
                TMP_Text element = m_EnvVarCounts[textIdx++];

                float value = inContext.Current.Environment.Properties[i];
                element.SetText(string.Format("{0}: {1}", inContext.Database.PropertyVarIds()[i].ToString(), value));
                element.gameObject.SetActive(true);
            }

            for(; textIdx < m_EnvVarCounts.Length; ++textIdx)
            {
                TMP_Text element = m_EnvVarCounts[textIdx];
                element.SetText(string.Empty);
                element.gameObject.SetActive(false);
            }
        }

        private void DisplayActorCounts(in EnergySimContext inContext)
        {
            int textIdx = 0;
            int actorCount = inContext.Database.ActorTypeCount();
            for(int i = 0; i < actorCount; ++i)
            {
                TMP_Text element = m_ActorCounts[textIdx++];

                ushort count = inContext.Current.Populations[i];
                uint mass = inContext.Current.Masses[i];
                element.SetText(string.Format("{0}: {1} ({2})", inContext.Database.ActorTypeIds()[i], count, mass));
                element.gameObject.SetActive(true);
            }

            for(; textIdx < m_ActorCounts.Length; ++textIdx)
            {
                TMP_Text element = m_ActorCounts[textIdx];
                element.SetText(string.Empty);
                element.gameObject.SetActive(false);
            }
        }
    }
}