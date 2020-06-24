using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BeauPools;
using BeauRoutine;
using BeauUtil;

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
        [SerializeField] private Sprite m_DefaultActorSprite = null;

        #endregion // Inspector

        #region Unity Events

        private void Awake()
        {
            m_ActorPool.Initialize();
        }

        #endregion // Unity Events

        public void Display(in EnergySimContext inContext)
        {
            System.Random random = new System.Random((int) (inContext.Current.NextSeed ^ uint.MaxValue));

            DisplayEnvVars(inContext, random);
            DisplayActorCounts(inContext, random);
            DisplayActorDots(inContext, random);
        }

        private void DisplayEnvVars(in EnergySimContext inContext, System.Random inRandom)
        {
            int textIdx = 0;
            int resCount = inContext.Database.ResourceTypeCount();
            for(int i = 0; i < resCount; ++i)
            {
                TMP_Text element = m_EnvVarCounts[textIdx++];

                ushort count = inContext.Current.Environment.OwnedResources[i];
                element.SetText(string.Format("{0}: {1}", inContext.Database.ResourceVarIds()[i].ToString(), count));
                element.gameObject.SetActive(true);
            }

            int propCount = inContext.Database.PropertyTypeCount();
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

        private void DisplayActorCounts(in EnergySimContext inContext, System.Random inRandom)
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

        private void DisplayActorDots(in EnergySimContext inContext, System.Random inRandom)
        {
            m_ActorPool.Reset();

            int actorCount = inContext.Database.ActorTypeCount();
            for(int i = 0; i < actorCount; ++i)
            {
                ActorType type = inContext.Database.ActorType(i);
                ushort count = inContext.Current.Populations[i];
                ActorType.DisplayConfig display = type.DisplaySettings();

                if (count == 0)
                    continue;

                bool bHerd = (type.Flags() & ActorTypeFlags.TreatAsHerd) == ActorTypeFlags.TreatAsHerd;
                for(int actorIdx = 0; actorIdx < inContext.Current.ActorCount; ++actorIdx)
                {
                    ref ActorState actor = ref inContext.Current.Actors[actorIdx];
                    if (actor.Type != type.Id())
                        continue;

                    ushort mass = actor.Mass;
                    if (mass == 0)
                        continue;

                    if (bHerd)
                    {
                        int fullHerds = mass / display.MassScale;
                        int partialHerd = mass % display.MassScale;
                        
                        while(--fullHerds >= 0)
                        {
                            SpawnActor(display, display.MassScale, inRandom);
                        }

                        if (partialHerd > 0)
                        {
                            SpawnActor(display, (ushort) partialHerd, inRandom);
                        }
                    }
                    else
                    {
                        SpawnActor(display, mass, inRandom);
                    }
                }
            }
        }

        private Image SpawnActor(ActorType.DisplayConfig inConfig, ushort inMass, System.Random inRandom)
        {
            Image img = m_ActorPool.InnerPool.Alloc();
            
            Sprite spr = inConfig.Image;
            if (spr == null)
                spr = m_DefaultActorSprite;
            
            img.sprite = spr;
            img.color = inConfig.Color;

            RectTransform rect = img.rectTransform;

            float scale = (float) inMass / inConfig.MassScale;
            rect.SetScale(scale, Axis.XY);

            Vector2 pos = inRandom.NextVector2(Vector2.zero, Vector2.one);
            rect.anchorMin = rect.anchorMax = pos;
            rect.anchoredPosition = Vector2.zero;

            rect.gameObject.SetActive(true);

            return img;
        }
    }
}